// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class OverloadResolution
    {
        // During overload resolution we need to map arguments to their corresponding 
        // parameters, but most of the time that map is going to be trivial:
        // argument 0 corresponds to parameter 0, argument 1 corresponds to parameter 1,
        // and so on. Only when the call involves named arguments, optional parameters or
        // expanded form params methods is that not the case.
        //
        // To avoid the GC pressure of allocating a lot of unnecessary trivial maps,
        // we have this immutable struct which maintains the map. If the mapping is
        // trivial then no array is ever allocated.

        private struct ParameterMap
        {
            private readonly int[] _parameters;
            private readonly int _length;

            public ParameterMap(int[] parameters, int length)
            {
                Debug.Assert(parameters == null || parameters.Length == length);
                _parameters = parameters;
                _length = length;
            }

            public bool IsTrivial { get { return _parameters == null; } }

            public int Length { get { return _length; } }

            public int this[int argument]
            {
                get
                {
                    Debug.Assert(0 <= argument && argument < _length);
                    return _parameters == null ? argument : _parameters[argument];
                }
            }

            public ImmutableArray<int> ToImmutableArray()
            {
                return _parameters.AsImmutableOrNull();
            }
        }

        private static ArgumentAnalysisResult AnalyzeArguments(
            Symbol symbol,
            AnalyzedArguments arguments,
            bool isMethodGroupConversion,
            bool expanded)
        {
            Debug.Assert((object)symbol != null);
            Debug.Assert(arguments != null);

            ImmutableArray<ParameterSymbol> parameters = symbol.GetParameters();
            bool isVararg = symbol.GetIsVararg();

            // The easy out is that we have no named arguments and are in normal form.
            if (!expanded && arguments.Names.Count == 0)
            {
                return AnalyzeArgumentsForNormalFormNoNamedArguments(parameters, arguments, isMethodGroupConversion, isVararg);
            }

            // We simulate an additional non-optional parameter for a vararg method.

            int argumentCount = arguments.Arguments.Count;

            int[] parametersPositions = null;
            int? unmatchedArgumentIndex = null;
            bool? unmatchedArgumentIsNamed = null;

            // Try to map every argument position to a formal parameter position:

            bool seenNamedParams = false;
            bool seenOutOfPositionNamedArgument = false;
            bool isValidParams = IsValidParams(symbol);
            for (int argumentPosition = 0; argumentPosition < argumentCount; ++argumentPosition)
            {
                // We use -1 as a sentinel to mean that no parameter was found that corresponded to this argument.
                bool isNamedArgument;
                int parameterPosition = CorrespondsToAnyParameter(parameters, expanded, arguments, argumentPosition,
                    isValidParams, isVararg, out isNamedArgument, ref seenNamedParams, ref seenOutOfPositionNamedArgument) ?? -1;

                if (parameterPosition == -1 && unmatchedArgumentIndex == null)
                {
                    unmatchedArgumentIndex = argumentPosition;
                    unmatchedArgumentIsNamed = isNamedArgument;
                }

                if (parameterPosition != argumentPosition && parametersPositions == null)
                {
                    parametersPositions = new int[argumentCount];
                    for (int i = 0; i < argumentPosition; ++i)
                    {
                        parametersPositions[i] = i;
                    }
                }

                if (parametersPositions != null)
                {
                    parametersPositions[argumentPosition] = parameterPosition;
                }
            }

            ParameterMap argsToParameters = new ParameterMap(parametersPositions, argumentCount);

            // We have analyzed every argument and tried to make it correspond to a particular parameter. 
            // We must now answer the following questions:
            //
            // (1) Is there any named argument used out-of-position and followed by unnamed arguments?
            // (2) Is there any argument without a corresponding parameter?
            // (3) Was there any named argument that specified a parameter that was already
            //     supplied with a positional parameter?
            // (4) Is there any non-optional parameter without a corresponding argument?
            // (5) Is there any named argument that were specified twice?
            //
            // If the answer to any of these questions is "yes" then the method is not applicable.
            // It is possible that the answer to any number of these questions is "yes", and so
            // we must decide which error condition to prioritize when reporting the error, 
            // should we need to report why a given method is not applicable. We prioritize
            // them in the given order.

            // (1) Is there any named argument used out-of-position and followed by unnamed arguments?

            int? badNonTrailingNamedArgument = CheckForBadNonTrailingNamedArgument(arguments, argsToParameters, parameters);
            if (badNonTrailingNamedArgument != null)
            {
                return ArgumentAnalysisResult.BadNonTrailingNamedArgument(badNonTrailingNamedArgument.Value);
            }

            // (2) Is there any argument without a corresponding parameter?

            if (unmatchedArgumentIndex != null)
            {
                if (unmatchedArgumentIsNamed.Value)
                {
                    return ArgumentAnalysisResult.NoCorrespondingNamedParameter(unmatchedArgumentIndex.Value);
                }
                else
                {
                    return ArgumentAnalysisResult.NoCorrespondingParameter(unmatchedArgumentIndex.Value);
                }
            }

            // (3) was there any named argument that specified a parameter that was already
            //     supplied with a positional parameter?

            int? nameUsedForPositional = NameUsedForPositional(arguments, argsToParameters);
            if (nameUsedForPositional != null)
            {
                return ArgumentAnalysisResult.NameUsedForPositional(nameUsedForPositional.Value);
            }

            // (4) Is there any non-optional parameter without a corresponding argument?

            int? requiredParameterMissing = CheckForMissingRequiredParameter(argsToParameters, parameters, isMethodGroupConversion, expanded);
            if (requiredParameterMissing != null)
            {
                return ArgumentAnalysisResult.RequiredParameterMissing(requiredParameterMissing.Value);
            }

            // __arglist cannot be used with named arguments (as it doesn't have a name)
            if (arguments.Names.Any() && arguments.Names.Last() != null && isVararg)
            {
                return ArgumentAnalysisResult.RequiredParameterMissing(parameters.Length);
            }

            // (5) Is there any named argument that were specified twice?

            int? duplicateNamedArgument = CheckForDuplicateNamedArgument(arguments);
            if (duplicateNamedArgument != null)
            {
                return ArgumentAnalysisResult.DuplicateNamedArgument(duplicateNamedArgument.Value);
            }

            // We're good; this one might be applicable in the given form.

            return expanded ?
                ArgumentAnalysisResult.ExpandedForm(argsToParameters.ToImmutableArray()) :
                ArgumentAnalysisResult.NormalForm(argsToParameters.ToImmutableArray());
        }

        private static int? CheckForBadNonTrailingNamedArgument(AnalyzedArguments arguments, ParameterMap argsToParameters, ImmutableArray<ParameterSymbol> parameters)
        {
            // Is there any named argument used out-of-position and followed by unnamed arguments?

            // If the map is trivial then clearly not.
            if (argsToParameters.IsTrivial)
            {
                return null;
            }

            // Find the first named argument which is used out-of-position
            int foundPosition = -1;
            int length = arguments.Arguments.Count;
            for (int i = 0; i < length; i++)
            {
                int parameter = argsToParameters[i];
                if (parameter != -1 && parameter != i && arguments.Name(i) != null)
                {
                    foundPosition = i;
                    break;
                }
            }

            if (foundPosition != -1)
            {
                // Verify that all the following arguments are named
                for (int i = foundPosition + 1; i < length; i++)
                {
                    if (arguments.Name(i) == null)
                    {
                        return foundPosition;
                    }
                }
            }

            return null;
        }

        private static int? CorrespondsToAnyParameter(
            ImmutableArray<ParameterSymbol> memberParameters,
            bool expanded,
            AnalyzedArguments arguments,
            int argumentPosition,
            bool isValidParams,
            bool isVararg,
            out bool isNamedArgument,
            ref bool seenNamedParams,
            ref bool seenOutOfPositionNamedArgument)
        {
            // Spec 7.5.1.1: Corresponding parameters:
            // For each argument in an argument list there has to be a corresponding parameter in
            // the function member or delegate being invoked. The parameter list used in the
            // following is determined as follows:
            // - For virtual methods and indexers defined in classes, the parameter list is picked from the most specific 
            //   declaration or override of the function member, starting with the static type of the receiver, and searching through its base classes.
            // - For interface methods and indexers, the parameter list is picked form the most specific definition of the member, 
            //   starting with the interface type and searching through the base interfaces. If no unique parameter list is found, 
            //   a parameter list with inaccessible names and no optional parameters is constructed, so that invocations cannot use 
            //   named parameters or omit optional arguments.
            // - For partial methods, the parameter list of the defining partial method declaration is used.
            // - For all other function members and delegates there is only a single parameter list, which is the one used.
            //
            // The position of an argument or parameter is defined as the number of arguments or
            // parameters preceding it in the argument list or parameter list.
            //
            // The corresponding parameters for function member arguments are established as follows:
            // 
            // Arguments in the argument-list of instance constructors, methods, indexers and delegates:

            isNamedArgument = arguments.Names.Count > argumentPosition && arguments.Names[argumentPosition] != null;

            if (!isNamedArgument)
            {
                // Spec:
                // - A positional argument where a fixed parameter occurs at the same position in the
                //   parameter list corresponds to that parameter.
                // - A positional argument of a function member with a parameter array invoked in its
                //   normal form corresponds to the parameter array, which must occur at the same
                //   position in the parameter list.
                // - A positional argument of a function member with a parameter array invoked in its
                //   expanded form, where no fixed parameter occurs at the same position in the
                //   parameter list, corresponds to an element in the parameter array.

                if (seenNamedParams)
                {
                    // Unnamed arguments after a named argument corresponding to a params parameter cannot correspond to any parameters
                    return null;
                }

                if (seenOutOfPositionNamedArgument)
                {
                    // Unnamed arguments after an out-of-position named argument cannot correspond to any parameters
                    return null;
                }

                int parameterCount = memberParameters.Length + (isVararg ? 1 : 0);
                if (argumentPosition >= parameterCount)
                {
                    return expanded ? parameterCount - 1 : (int?)null;
                }

                return argumentPosition;
            }
            else
            {
                // SPEC: A named argument corresponds to the parameter of the same name in the parameter list. 

                // SPEC VIOLATION: The intention of this line of the specification, when contrasted with
                // SPEC VIOLATION: the lines on positional arguments quoted above, was to disallow a named
                // SPEC VIOLATION: argument from corresponding to an element of a parameter array when 
                // SPEC VIOLATION: the method was invoked in its expanded form. That is to say that in
                // SPEC VIOLATION: this case:  M(params int[] x) ... M(x : 1234); the named argument 
                // SPEC VIOLATION: corresponds to x in the normal form (and is then inapplicable), but
                // SPEC VIOLATION: the named argument does *not* correspond to a member of params array
                // SPEC VIOLATION: x in the expanded form.
                // SPEC VIOLATION: Sadly that is not what we implemented in C# 4, and not what we are 
                // SPEC VIOLATION: implementing here. If you do that, we make x correspond to the 
                // SPEC VIOLATION: parameter array and allow the candidate to be applicable in its
                // SPEC VIOLATION: expanded form.

                var name = arguments.Names[argumentPosition];
                for (int p = 0; p < memberParameters.Length; ++p)
                {
                    // p is initialized to zero; it is ok for a named argument to "correspond" to
                    // _any_ parameter (not just the parameters past the point of positional arguments)
                    if (memberParameters[p].Name == name.Identifier.ValueText)
                    {
                        if (isValidParams && p == memberParameters.Length - 1)
                        {
                            seenNamedParams = true;
                        }

                        if (p != argumentPosition)
                        {
                            seenOutOfPositionNamedArgument = true;
                        }

                        return p;
                    }
                }
            }

            return null;
        }

        private static ArgumentAnalysisResult AnalyzeArgumentsForNormalFormNoNamedArguments(
            ImmutableArray<ParameterSymbol> parameters,
            AnalyzedArguments arguments,
            bool isMethodGroupConversion,
            bool isVararg)
        {
            Debug.Assert(!parameters.IsDefault);
            Debug.Assert(arguments != null);
            Debug.Assert(arguments.Names.Count == 0);

            // We simulate an additional non-optional parameter for a vararg method.
            int parameterCount = parameters.Length + (isVararg ? 1 : 0);
            int argumentCount = arguments.Arguments.Count;

            // If there are no named arguments then analyzing the argument and parameter
            // matching in normal form is simple: each argument corresponds exactly to
            // the matching parameter, and if there are not enough arguments then the
            // unmatched parameters had better all be optional. If there are too 
            // few parameters then one of the arguments has no matching parameter. 
            // Otherwise, everything is just right.

            if (argumentCount < parameterCount)
            {
                for (int parameterPosition = argumentCount; parameterPosition < parameterCount; ++parameterPosition)
                {
                    if (parameters.Length == parameterPosition || !CanBeOptional(parameters[parameterPosition], isMethodGroupConversion))
                    {
                        return ArgumentAnalysisResult.RequiredParameterMissing(parameterPosition);
                    }
                }
            }
            else if (parameterCount < argumentCount)
            {
                return ArgumentAnalysisResult.NoCorrespondingParameter(parameterCount);
            }

            // A null map means that every argument in the argument list corresponds exactly to
            // the same position in the formal parameter list.
            return ArgumentAnalysisResult.NormalForm(default(ImmutableArray<int>));
        }

        private static bool CanBeOptional(ParameterSymbol parameter, bool isMethodGroupConversion)
        {
            // NOTE: Section 6.6 will be slightly updated:
            //
            //   - The candidate methods considered are only those methods that are applicable in their
            //     normal form (§7.5.3.1), and do not omit any optional parameters. Thus, candidate methods
            //     are ignored if they are applicable only in their expanded form, or if one or more of their
            //     optional parameters do not have a corresponding parameter in the targeted delegate type.
            //   
            // Therefore, no parameters are optional when performing method group conversion.  Alternatively,
            // we could eliminate methods based on the number of arguments, but then we wouldn't be able to
            // fall back on them if no other candidates were available.

            return !isMethodGroupConversion && parameter.IsOptional;
        }

        private static int? NameUsedForPositional(AnalyzedArguments arguments, ParameterMap argsToParameters)
        {
            // Was there a named argument used for a previously-supplied positional argument?

            // If the map is trivial then clearly not. 
            if (argsToParameters.IsTrivial)
            {
                return null;
            }

            // PERFORMANCE: This is an O(n-squared) algorithm, but n will typically be small.  We could rewrite this
            // PERFORMANCE: as a linear algorithm if we wanted to allocate more memory.

            for (int argumentPosition = 0; argumentPosition < argsToParameters.Length; ++argumentPosition)
            {
                if (arguments.Name(argumentPosition) != null)
                {
                    for (int i = 0; i < argumentPosition; ++i)
                    {
                        if (arguments.Name(i) == null)
                        {
                            if (argsToParameters[argumentPosition] == argsToParameters[i])
                            {
                                // Error; we've got a named argument that corresponds to 
                                // a previously-given positional argument.
                                return argumentPosition;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static int? CheckForMissingRequiredParameter(
            ParameterMap argsToParameters,
            ImmutableArray<ParameterSymbol> parameters,
            bool isMethodGroupConversion,
            bool expanded)
        {
            Debug.Assert(!(expanded && isMethodGroupConversion));

            // If we're in the expanded form then the final parameter is always optional,
            // so we'll just skip it entirely.

            int count = expanded ? parameters.Length - 1 : parameters.Length;

            // We'll take an early out here. If the map from arguments to parameters is trivial
            // and there are as many arguments as parameters in that map, then clearly no 
            // required parameter is missing.

            if (argsToParameters.IsTrivial && count <= argsToParameters.Length)
            {
                return null;
            }

            // This is an O(n squared) algorithm, but (1) we avoid allocating any more heap memory, and
            // (2) n is likely to be small, both because the number of parameters in a method is typically
            // small, and because methods with many parameters make most of them optional. We could make
            // this linear easily enough if we needed to but we'd have to allocate more heap memory and
            // we'd rather not pressure the garbage collector.

            for (int p = 0; p < count; ++p)
            {
                if (CanBeOptional(parameters[p], isMethodGroupConversion))
                {
                    continue;
                }

                bool found = false;
                for (int arg = 0; arg < argsToParameters.Length; ++arg)
                {
                    found = (argsToParameters[arg] == p);
                    if (found)
                    {
                        break;
                    }
                }
                if (!found)
                {
                    return p;
                }
            }

            return null;
        }

        private static int? CheckForDuplicateNamedArgument(AnalyzedArguments arguments)
        {
            if (arguments.Names.IsEmpty())
            {
                // No checks if there are no named arguments
                return null;
            }

            var alreadyDefined = PooledHashSet<string>.GetInstance();
            for (int i = 0; i < arguments.Names.Count; ++i)
            {
                string name = arguments.Name(i);

                if (name is null)
                {
                    // Skip unnamed arguments
                    continue;
                }

                if (!alreadyDefined.Add(name))
                {
                    alreadyDefined.Free();
                    return i;
                }
            }

            alreadyDefined.Free();
            return null;
        }
    }
}
