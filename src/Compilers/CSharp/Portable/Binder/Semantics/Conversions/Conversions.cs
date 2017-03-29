// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Conversions : ConversionsBase
    {
        private readonly Binder _binder;

        public Conversions(Binder binder)
            : this(binder, currentRecursionDepth: 0)
        {
        }

        private Conversions(Binder binder, int currentRecursionDepth)
            : base(binder.Compilation.Assembly.CorLibrary, currentRecursionDepth)
        {
            _binder = binder;
        }

        protected override ConversionsBase CreateInstance(int currentRecursionDepth)
        {
            return new Conversions(_binder, currentRecursionDepth);
        }

        private CSharpCompilation Compilation { get { return _binder.Compilation; } }

        public override Conversion GetMethodGroupConversion(BoundMethodGroup source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Must be a bona fide delegate type, not an expression tree type.
            if (!destination.IsDelegateType())
            {
                return Conversion.NoConversion;
            }

            var methodSymbol = GetDelegateInvokeMethodIfAvailable(destination);
            if ((object)methodSymbol == null)
            {
                return Conversion.NoConversion;
            }

            var resolution = ResolveDelegateMethodGroup(_binder, source, methodSymbol, ref useSiteDiagnostics);
            var conversion = (resolution.IsEmpty || resolution.HasAnyErrors) ?
                Conversion.NoConversion :
                ToConversion(resolution.OverloadResolutionResult, resolution.MethodGroup, (NamedTypeSymbol)destination);
            resolution.Free();
            return conversion;
        }

        protected override Conversion GetImplicitTupleLiteralConversion(BoundTupleLiteral source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var arguments = source.Arguments;

            // check if the type is actually compatible type for a tuple of given cardinality
            if (!destination.IsTupleOrCompatibleWithTupleOfCardinality(arguments.Length))
            {
                return Conversion.NoConversion;
            }

            ImmutableArray<TypeSymbol> targetElementTypes = destination.GetElementTypesOfTupleOrCompatible();
            Debug.Assert(arguments.Length == targetElementTypes.Length);

            // check arguments against flattened list of target element types 
            var argumentConversions = ArrayBuilder<Conversion>.GetInstance(arguments.Length);
            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var result = ClassifyImplicitConversionFromExpression(argument, targetElementTypes[i], ref useSiteDiagnostics);
                if (!result.Exists)
                {
                    argumentConversions.Free();
                    return Conversion.NoConversion;
                }

                argumentConversions.Add(result);
            }

            return new Conversion(ConversionKind.ImplicitTupleLiteral, argumentConversions.ToImmutableAndFree());
        }

        protected override Conversion GetExplicitTupleLiteralConversion(BoundTupleLiteral source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool forCast)
        {
            var arguments = source.Arguments;

            // check if the type is actually compatible type for a tuple of given cardinality
            if (!destination.IsTupleOrCompatibleWithTupleOfCardinality(arguments.Length))
            {
                return Conversion.NoConversion;
            }

            ImmutableArray<TypeSymbol> targetElementTypes = destination.GetElementTypesOfTupleOrCompatible();
            Debug.Assert(arguments.Length == targetElementTypes.Length);

            // SPEC: If F contains an anonymous-function-signature, then D and F have the same number of parameters.
            // SPEC: If F does not contain an anonymous-function-signature, then D may have zero or more parameters 
            // SPEC: of any type, as long as no parameter of D has the out parameter modifier.

            if (anonymousFunction.HasSignature)
            {
                if (anonymousFunction.ParameterCount != invokeMethod.ParameterCount)
                {
                    return LambdaConversionResult.BadParameterCount;
                }

                // SPEC: If F has an explicitly typed parameter list, each parameter in D has the same type 
                // SPEC: and modifiers as the corresponding parameter in F.
                // SPEC: If F has an implicitly typed parameter list, D has no ref or out parameters.

                if (anonymousFunction.HasExplicitlyTypedParameterList)
                {
                    for (int p = 0; p < delegateParameters.Length; ++p)
                    {
                        if (delegateParameters[p].RefKind != anonymousFunction.RefKind(p) ||
                            !delegateParameters[p].Type.TypeSymbol.Equals(anonymousFunction.ParameterType(p).TypeSymbol, TypeSymbolEqualityOptions.SameType))
                        {
                            return LambdaConversionResult.MismatchedParameterType;
                        }
                    }
                }
                else
                {
                    for (int p = 0; p < delegateParameters.Length; ++p)
                    {
                        if (delegateParameters[p].RefKind != RefKind.None)
                        {
                            return LambdaConversionResult.RefInImplicitlyTypedLambda;
                        }
                    }

                    // In C# it is not possible to make a delegate type
                    // such that one of its parameter types is a static type. But static types are 
                    // in metadata just sealed abstract types; there is nothing stopping someone in
                    // another language from creating a delegate with a static type for a parameter,
                    // though the only argument you could pass for that parameter is null.
                    // 
                    // In the native compiler we forbid conversion of an anonymous function that has
                    // an implicitly-typed parameter list to a delegate type that has a static type
                    // for a formal parameter type. However, we do *not* forbid it for an explicitly-
                    // typed lambda (because we already require that the explicitly typed parameter not
                    // be static) and we do not forbid it for an anonymous method with the entire
                    // parameter list missing (because the body cannot possibly have a parameter that
                    // is of static type, even though this means that we will be generating a hidden
                    // method with a parameter of static type.)
                    //
                    // We also allow more exotic situations to work in the native compiler. For example,
                    // though it is not possible to convert x=>{} to Action<GC>, it is possible to convert
                    // it to Action<List<GC>> should there be a language that allows you to construct 
                    // a variable of that type.
                    //
                    // We might consider beefing up this rule to disallow a conversion of *any* anonymous
                    // function to *any* delegate that has a static type *anywhere* in the parameter list.

                    for (int p = 0; p < delegateParameters.Length; ++p)
                    {
                        if (delegateParameters[p].Type.IsStatic)
                        {
                            return LambdaConversionResult.StaticTypeInImplicitlyTypedLambda;
                        }
                    }
                }
            }
            else
            {
                var result = ClassifyConversionFromExpression(arguments[i], targetElementTypes[i], ref useSiteDiagnostics, forCast);
                if (!result.Exists)
                {
                    argumentConversions.Free();
                    return Conversion.NoConversion;
                }

                argumentConversions.Add(result);
            }

            return new Conversion(ConversionKind.ExplicitTupleLiteral, argumentConversions.ToImmutableAndFree());
        }

        protected override Conversion GetInterpolatedStringConversion(BoundInterpolatedString source, TypeSymbol destination, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // An interpolated string expression may be converted to the types
            // System.IFormattable and System.FormattableString
            return (destination == Compilation.GetWellKnownType(WellKnownType.System_IFormattable) ||
                    destination == Compilation.GetWellKnownType(WellKnownType.System_FormattableString))
                ? Conversion.InterpolatedString : Conversion.NoConversion;
        }

        /// <summary>
        /// Resolve method group based on the optional delegate invoke method.
        /// If the invoke method is null, ignore arguments in resolution.
        /// </summary>
        private static MethodGroupResolution ResolveDelegateMethodGroup(Binder binder, BoundMethodGroup source, MethodSymbol delegateInvokeMethodOpt, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((object)delegateInvokeMethodOpt != null)
            {
                var analyzedArguments = new AnalyzedArguments();
                GetDelegateArguments(source.Syntax, analyzedArguments, delegateInvokeMethodOpt.Parameters, binder.Compilation);
                var resolution = binder.ResolveMethodGroup(source, analyzedArguments, isMethodGroupConversion: true, inferWithDynamic: true, useSiteDiagnostics: ref useSiteDiagnostics);
                return resolution;
            }
            else
            {
                return binder.ResolveMethodGroup(source, null, isMethodGroupConversion: true, useSiteDiagnostics: ref useSiteDiagnostics);
            }
        }

        /// <summary>
        /// Return the Invoke method symbol if the type is a delegate
        /// type and the Invoke method is available, otherwise null.
        /// </summary>
        private static MethodSymbol GetDelegateInvokeMethodIfAvailable(TypeSymbol type)
        {
            var delegateType = type.GetDelegateType();
            if ((object)delegateType == null)
            {
                return null;
            }

            MethodSymbol methodSymbol = delegateType.DelegateInvokeMethod;
            if ((object)methodSymbol == null || methodSymbol.HasUseSiteError)
            {
                return null;
            }

            return methodSymbol;
        }

        public static bool ReportDelegateMethodGroupDiagnostics(Binder binder, BoundMethodGroup expr, TypeSymbol targetType, DiagnosticBag diagnostics)
        {
            var invokeMethodOpt = GetDelegateInvokeMethodIfAvailable(targetType);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var resolution = ResolveDelegateMethodGroup(binder, expr, invokeMethodOpt, ref useSiteDiagnostics);
            diagnostics.Add(expr.Syntax, useSiteDiagnostics);

            bool hasErrors = resolution.HasAnyErrors;

            diagnostics.AddRange(resolution.Diagnostics);

            // SPEC VIOLATION: Unfortunately, we cannot exactly implement the specification for
            // the scenario in which an extension method that extends a value type is converted
            // to a delegate. The code we generate that captures a delegate to a static method
            // that is "partially evaluated" with the bound-to-the-delegate first argument
            // requires that the first argument be of reference type.
            //
            // SPEC VIOLATION: Similarly, we cannot capture a method of Nullable<T>, because
            // boxing a Nullable<T> gives a T, not a boxed Nullable<T>.
            //
            // We give special error messages in these situations.

            if (resolution.MethodGroup != null)
            {
                var result = resolution.OverloadResolutionResult;
                if (result != null)
                {
                    if (result.Succeeded)
                    {
                        var method = result.BestResult.Member;
                        Debug.Assert((object)method != null);
                        if (resolution.MethodGroup.IsExtensionMethodGroup)
                        {
                            Debug.Assert(method.IsExtensionMethod);

                            var thisParameter = method.Parameters[0];
                            if (!thisParameter.Type.IsReferenceType)
                            {
                                // Extension method '{0}' defined on value type '{1}' cannot be used to create delegates
                                diagnostics.Add(
                                    ErrorCode.ERR_ValueTypeExtDelegate,
                                    expr.Syntax.Location,
                                    method,
                                    thisParameter.Type.TypeSymbol);
                                hasErrors = true;
                            }
                        }
                        else if (method.OriginalDefinition.ContainingType.SpecialType == SpecialType.System_Nullable_T && !method.IsOverride)
                        {
                            // CS1728: Cannot bind delegate to '{0}' because it is a member of 'System.Nullable<T>'
                            diagnostics.Add(
                                ErrorCode.ERR_DelegateOnNullable,
                                expr.Syntax.Location,
                                method);
                            hasErrors = true;
                        }
                    }
                    else if (!hasErrors &&
                            !resolution.IsEmpty &&
                            resolution.ResultKind == LookupResultKind.Viable)
                    {
                        var overloadDiagnostics = DiagnosticBag.GetInstance();

                        result.ReportDiagnostics(binder, expr.Syntax.Location, overloadDiagnostics,
                            expr.Name,
                            resolution.MethodGroup.Receiver, resolution.AnalyzedArguments, resolution.MethodGroup.Methods.ToImmutable(),
                            typeContainingConstructor: null, delegateTypeBeingInvoked: null, isMethodGroupConversion: true);

                        if (!overloadDiagnostics.IsEmptyWithoutResolution)
                        {
                            hasErrors = overloadDiagnostics.HasAnyErrors();
                            diagnostics.AddRange(overloadDiagnostics);
                        }

                        overloadDiagnostics.Free();
                    }
                }
            }

            resolution.Free();
            return hasErrors;
        }

        public Conversion MethodGroupConversion(SyntaxNode syntax, MethodGroup methodGroup, NamedTypeSymbol delegateType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var analyzedArguments = AnalyzedArguments.GetInstance();
            var result = OverloadResolutionResult<MethodSymbol>.GetInstance();

            Debug.Assert((object)delegateType.DelegateInvokeMethod != null && !delegateType.DelegateInvokeMethod.HasUseSiteError,
                         "This method should only be called for valid delegate types");
            GetDelegateArguments(syntax, analyzedArguments, delegateType.DelegateInvokeMethod.Parameters, Compilation);
            _binder.OverloadResolution.MethodInvocationOverloadResolution(
                methodGroup.Methods, methodGroup.TypeArguments, analyzedArguments, result, ref useSiteDiagnostics, isMethodGroupConversion: true);
            var conversion = ToConversion(result, methodGroup, delegateType);

            analyzedArguments.Free();
            result.Free();
            return conversion;
        }

        public static void GetDelegateArguments(SyntaxNode syntax, AnalyzedArguments analyzedArguments, ImmutableArray<ParameterSymbol> delegateParameters, CSharpCompilation compilation)
        {
            foreach (var p in delegateParameters)
            {
                ParameterSymbol parameter = p;

                // In ExpressionBinder::BindGrpConversion, the native compiler substitutes object in place of dynamic.  This is
                // necessary because conversions from expressions of type dynamic always succeed, whereas conversions from the
                // type generally fail (modulo identity conversions).  This is not reflected in the C# 4 spec, but will be
                // incorporated going forward.  See DevDiv #742345 for additional details.
                // NOTE: Dev11 does a deep substitution (e.g. C<C<C<dynamic>>> -> C<C<C<object>>>), but that seems redundant.
                if (parameter.Type.IsDynamic())
                {
                    // If we don't have System.Object, then we'll get an error type, which will cause overload resolution to fail, 
                    // which will cause some error to be reported.  That's sufficient (i.e. no need to specifically report its absence here).
                    parameter = new SignatureOnlyParameterSymbol(
                        TypeSymbolWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Object), parameter.Type.CustomModifiers), parameter.RefCustomModifiers, parameter.IsParams, parameter.RefKind);
            }

            analyzedArguments.Arguments.Add(new BoundParameter(syntax, parameter) { WasCompilerGenerated = true });
                analyzedArguments.RefKinds.Add(parameter.RefKind);
            }
        }

        private static Conversion ToConversion(OverloadResolutionResult<MethodSymbol> result, MethodGroup methodGroup, NamedTypeSymbol delegateType)
        {
            // 6.6 An implicit conversion (6.1) exists from a method group (7.1) to a compatible
            // delegate type. Given a delegate type D and an expression E that is classified as
            // a method group, an implicit conversion exists from E to D if E contains at least
            // one method that is applicable in its normal form (7.5.3.1) to an argument list
            // constructed by use of the parameter types and modifiers of D...

            // SPEC VIOLATION: Unfortunately, we cannot exactly implement the specification for
            // the scenario in which an extension method that extends a value type is converted
            // to a delegate. The code we generate that captures a delegate to a static method
            // that is "partially evaluated" with the bound-to-the-delegate first argument
            // requires that the first argument be of reference type.

            // SPEC VIOLATION: Similarly, we cannot capture a method of Nullable<T>, because
            // boxing a Nullable<T> gives a T, not a boxed Nullable<T>. (We can capture methods
            // of object on a nullable receiver, but not GetValueOrDefault.)

            if (!result.Succeeded)
            {
                return Conversion.NoConversion;
            }

            MethodSymbol method = result.BestResult.Member;

            if (methodGroup.IsExtensionMethodGroup && !method.Parameters[0].Type.IsReferenceType)
            {
                return Conversion.NoConversion;
            }

            if (method.OriginalDefinition.ContainingType.SpecialType == SpecialType.System_Nullable_T &&
                !method.IsOverride)
            {
                return Conversion.NoConversion;
            }

            // NOTE: Section 6.6 will be slightly updated:
            //
            //   - The candidate methods considered are only those methods that are applicable in their
            //     normal form (§7.5.3.1), and do not omit any optional parameters. Thus, candidate methods
            //     are ignored if they are applicable only in their expanded form, or if one or more of their
            //     optional parameters do not have a corresponding parameter in the targeted delegate type.
            //   
            // Therefore, we shouldn't get here unless the parameter count matches.

            // NOTE: Delegate type compatibility is important, but is not part of the existence check.

            Debug.Assert(method.ParameterCount == delegateType.DelegateInvokeMethod.ParameterCount + (methodGroup.IsExtensionMethodGroup ? 1 : 0));

            return new Conversion(ConversionKind.MethodGroup, method, methodGroup.IsExtensionMethodGroup);
        }
    }
}
