// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

#if DEBUG
using System.Text;
#endif

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Summarizes the results of an overload resolution analysis, as described in section 7.5 of
    /// the language specification. Describes whether overload resolution succeeded, and which
    /// method was selected if overload resolution succeeded, as well as detailed information about
    /// each method that was considered. 
    /// </summary>
    internal class OverloadResolutionResult<TMember> where TMember : Symbol
    {
        private MemberResolutionResult<TMember> bestResult;
        private ThreeState bestResultState;
        internal readonly ArrayBuilder<MemberResolutionResult<TMember>> ResultsBuilder;

        // Create an overload resolution result from a single result.
        internal OverloadResolutionResult()
        {
            this.ResultsBuilder = new ArrayBuilder<MemberResolutionResult<TMember>>();
        }

        internal void Clear()
        {
            this.bestResult = default(MemberResolutionResult<TMember>);
            this.bestResultState = ThreeState.Unknown;
            this.ResultsBuilder.Clear();
        }

        /// <summary>
        /// True if overload resolution successfully selected a single best method.
        /// </summary>
        public bool Succeeded
        {
            get
            {
                if (!this.bestResultState.HasValue())
                {
                    this.bestResultState = TryGetBestResult(this.ResultsBuilder, out this.bestResult);
                }

                return this.bestResultState == ThreeState.True && bestResult.Result.IsValid;
            }
        }

        /// <summary>
        /// If overload resolution successfully selected a single best method, returns information
        /// about that method. Otherwise returns null.
        /// </summary>
        public MemberResolutionResult<TMember> ValidResult
        {
            get
            {
                Debug.Assert(this.bestResultState == ThreeState.True && bestResult.Result.IsValid);
                return bestResult;
            }
        }

        /// <summary>
        /// If there was a method that overload resolution considered better than all others,
        /// returns information about that method. A method may be returned even if that method was
        /// not considered a successful overload resolution, as long as it was better that any other
        /// potential method considered.
        /// </summary>
        public MemberResolutionResult<TMember> BestResult
        {
            get
            {
                Debug.Assert(this.bestResultState == ThreeState.True);
                return bestResult;
            }
        }

        private bool HasBestResult
        {
            get
            {
                return bestResultState.Value();
            }
        }

        /// <summary>
        /// Returns information about each method that was considered during overload resolution,
        /// and what the results of overload resolution were for that method.
        /// </summary>
        public ImmutableArray<MemberResolutionResult<TMember>> Results
        {
            get
            {
                return this.ResultsBuilder.ToImmutable();
            }
        }

        /// <summary>
        /// Returns true if one or more of the members in the group are applicable. (Note that
        /// Succeeded implies IsApplicable but IsApplicable does not imply Succeeded.  It is possible
        /// that no applicable member was better than all others.)
        /// </summary>
        internal bool HasAnyApplicableMember
        {
            get
            {
                foreach (var res in this.ResultsBuilder)
                {
                    if (res.Result.IsApplicable)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns all methods in the group that are applicable, <see cref="HasAnyApplicableMember"/>.
        /// </summary>
        internal ImmutableArray<TMember> GetAllApplicableMembers()
        {
            var result = ArrayBuilder<TMember>.GetInstance();
            foreach (var res in this.ResultsBuilder)
            {
                if (res.Result.IsApplicable)
                {
                    result.Add(res.Member);
                }
            }

            return result.ToImmutableAndFree();
        }

        private static ThreeState TryGetBestResult(ArrayBuilder<MemberResolutionResult<TMember>> allResults, out MemberResolutionResult<TMember> best)
        {
            ThreeState haveBest = ThreeState.False;

            foreach (var pair in allResults)
            {
                if (pair.Result.IsValid)
                {
                    if (haveBest == ThreeState.True)
                    {
                        Debug.Assert(false, "How did we manage to get two methods in the overload resolution results that were both better than every other method?");
                        best = default(MemberResolutionResult<TMember>);
                        return ThreeState.False;
                    }

                    haveBest = ThreeState.True;
                    best = pair;
                }
            }

            // TODO: There might be a situation in which there were no valid results but we still want to identify a "best of a bad lot" result for
            // TODO: error reporting.
            return haveBest;
        }

        // Reports an error if one has not already been reported.
        internal void ReportDiagnostics<T>(
            Binder binder,
            Location location,
            DiagnosticBag diagnostics,
            string name,
            BoundExpression receiver,
            AnalyzedArguments arguments,
            ImmutableArray<T> memberGroup, // the T is just a convenience for the caller
            NamedTypeSymbol typeContainingConstructor,
            NamedTypeSymbol delegateTypeBeingInvoked,
            CSharpSyntaxNode queryClause = null,
            bool isMethodGroupConversion = false) where T : Symbol
        {
            Debug.Assert(!this.Succeeded, "Don't ask for diagnostic info on a successful overload resolution result.");

            var symbols = StaticCast<Symbol>.From(memberGroup);

            // We want to report errors on the thing that was logically "closest to success". For
            // example, if we have two methods that were applicable and both candidates for "best"
            // then the fact that there were a dozen other inapplicable methods that were discarded
            // is irrelevant. We want to report the error about the ambiguous methods, not the
            // inapplicable methods.

            // Note that "final validation" is performed after overload resolution,
            // so final validation errors are not seen here. Final validation errors include
            // violations of constraints on method type parameters, static/instance mismatches,
            // and so on.

            // Section One: There were applicable methods. Two or more of them were not worse than
            // anything else. That is, two or more were equally valid "best" methods, but neither
            // was better than the other.
            //
            // This can happen if methods 1 and 2 both beat method 3, but methods 1 and 2
            // do not beat each other. Both 1 and 2 are unbeaten and beat one other thing, 
            // so both are candidates for best. But the best method must be the unique
            // method that has this property.

            if (HadAmbiguousBestMethods(diagnostics, symbols, location))
            {
                return;
            }

            // Section Two: There were applicable methods; all of them were worse than something
            // else.
            //
            // This might sound like a paradox, but it is in fact possible. Because there are
            // intransitivities in convertibility (where A-->B, B-->C and C-->A but none of the
            // opposite conversions are legal) there are also intransitivities in betterness. 

            if (HadAmbiguousWorseMethods(diagnostics, symbols, location, queryClause != null, receiver, name))
            {
                return;
            }

            // Section Three: We know that there was not an unambiguous best method (because we are 
            // reporting an error) and that there were not multiple applicable candidates (because 
            // we would have produced an ambiguity error and returned already.) That means that 
            // every method in the method group was either (1) inapplicable, or (2) not even a 
            // candidate because type inference failed. 
            //
            // If the user provided a number of arguments that works for no possible method in the method
            // group then we give an error saying that.  Every method will have an error of the form
            // "missing required parameter" or "argument corresponds to no parameter", and therefore we
            // have no way of choosing a "best bad method" to report the error on. We should simply
            // say that no possible method can take the given number of arguments.
            //
            // Note that we only want to report that "no overload takes n arguments" if in 
            // fact *no* method could *possibly* take n arguments. If a method has
            // a params array or optional parameters that make it possible for it to take 
            // n arguments then we do not report the error.

            if (AllHadBadParameterCount(diagnostics, name, arguments, symbols, location, typeContainingConstructor, delegateTypeBeingInvoked, isMethodGroupConversion))
            {
                return;
            }

            // Section Four: We now know the following facts: (1) every method in the method group is
            // either inapplicable or not a candidate due to type inference failure, and (2) at least
            // one method in that group could possibly take the number of supplied arguments.
            //
            // Why then were there no applicable methods? For each method that could take the supplied
            // number of arguments, there must be some reason why the method failed to be an
            // applicable candidate. The reason could be:
            //
            // * The method is in a type in an unreferenced assembly; even if it is the "right" 
            //   method we should not be using it until the developer fixes the references.
            //
            // * There was a problem mapping arguments to formal parameters -- arguments without
            //   corresponding parameters, required parameters without corresponding arguments,
            //   two arguments corresponding to the same parameter, and so on. In these cases
            //   we have not attempted type inference, and have not attempted to convert arguments 
            //   to corresponding formal parameter types.
            //
            // * Arguments and formal parameters all match, but type inference somehow failed.
            //   Either type inference failed to infer a consistent set of types, or the
            //   types inferred were bad in some way (inaccessible or constraint-violating).
            //   We may have attempted to convert lambda arguments to delegate types for the
            //   purpose of inferring delegate return types.
            //
            // * Type inference succeeded (or was not necessary), but arguments were not 
            //   compatible with corresponding formal parameter types. In this case we have
            //   attempted to convert lambdas to delegate types.

            // If we got as far as converting a lambda to a delegate type, and we failed to
            // do so, then odds are extremely good that the failure is the ultimate cause
            // of the overload resolution failing to find any applicable method. Report
            // the errors out of each lambda argument, if there were any.

            if (HadLambdaConversionError(diagnostics, arguments))
            {
                return;
            }

            // 
            // We wish to identify one of those methods (ie, the methods that could take the 
            // number of supplied arguments) as the "best bad method" to give an error on.
            // We use the following heuristic to identify the best of such methods:
            // 

            // If there is any such method that has a bad conversion or out/ref mismatch 
            // then the first such method found is the best bad method.

            if (HadBadArguments(diagnostics, binder.Compilation, name, arguments, symbols, location, binder.Flags, isMethodGroupConversion))
            {
                return;
            }

            // Otherwise, if there is any such method where type inference succeeded but inferred
            // a type that violates its own constraints then the first such method is 
            // the best bad method.

            if (ConstraintsCheckFailed(binder.Conversions, binder.Compilation, diagnostics, arguments, location))
            {
                return;
            }

            // Otherwise, if there is any such method where type inference succeeded but inferred
            // an inaccessible type then the first such method found is the best bad method.

            if (InaccessibleTypeArgument(diagnostics, symbols, arguments, location))
            {
                return;
            }

            // Otherwise, if there is any such method where type inference failed then the
            // first such method is the best bad method.

            if (TypeInferenceFailed(binder, diagnostics, symbols, receiver, arguments, location, queryClause))
            {
                return;
            }

            // Otherwise, if there is any such method that has a named argument and a positional 
            // argument for the same parameter then the first such method is the best bad method.

            if (HadNameUsedForPositional(diagnostics, arguments, symbols))
            {
                return;
            }

            // Otherwise, if there is any such method that has a named argument that corresponds
            // to no parameter then the first such method is the best bad method.

            if (HadNoCorrespondingNamedParameter(name, diagnostics, arguments, delegateTypeBeingInvoked, symbols))
            {
                return;
            }

            // Otherwise, if there is any such method that has a required parameter
            // but no argument was supplied for it then the first such method is 
            // the best bad method.

            if (MissingRequiredParameter(diagnostics, arguments, delegateTypeBeingInvoked, symbols, location))
            {
                return;
            }

            // Otherwise, if there is any such method that cannot be used because it is
            // in an unreferenced assembly then the first such method is the best bad method.

            if (UseSiteError(diagnostics, symbols, location))
            {
                return;
            }

            // Otherwise, if there is any such method that cannot be used because it is
            // unsupported by the language then the first such method is the best bad method.

            if (UnsupportedMetadata(diagnostics, symbols, location))
            {
                return;
            }

            // If we got here then something is wrong; we should have found an error to report.
            throw ExceptionUtilities.Unreachable;
        }

        private bool UseSiteError(DiagnosticBag diagnostics, ImmutableArray<Symbol> symbols, Location location)
        {
            var bad = GetFirstMemberKind(MemberResolutionKind.UseSiteError);
            if (bad.IsNull)
            {
                return false;
            }

            Debug.Assert(bad.Member.GetUseSiteDiagnostic().Severity == DiagnosticSeverity.Error,
                "Why did we use MemberResolutionKind.UseSiteError if we didn't have a use site error?");

            // Use site errors are reported unconditionally in PerformMemberOverloadResolution/PerformObjectCreationOverloadResolution.

            return true;
        }

        private bool UnsupportedMetadata(DiagnosticBag diagnostics, ImmutableArray<Symbol> symbols, Location location)
        {
            var bad = GetFirstMemberKind(MemberResolutionKind.UnsupportedMetadata);
            if (bad.IsNull)
            {
                return false;
            }

            DiagnosticInfo diagnostic = bad.Member.GetUseSiteDiagnostic();

            Debug.Assert(diagnostic != null);

            var di = new DiagnosticInfoWithSymbols(
                (ErrorCode)diagnostic.Code,
                diagnostic.Arguments,
                symbols);

            Symbol.ReportUseSiteDiagnostic(di, diagnostics, location);

            return di.Severity == DiagnosticSeverity.Error;
        }

        private bool InaccessibleTypeArgument(
            DiagnosticBag diagnostics,
            ImmutableArray<Symbol> symbols,
            AnalyzedArguments arguments,
            Location location)
        {
            var inaccessible = GetFirstMemberKind(MemberResolutionKind.InaccessibleTypeArgument);
            if (inaccessible.IsNull)
            {
                return false;
            }

            Debug.Assert(inaccessible.MemberCouldTakeArgumentCount(arguments.Arguments.Count));

            // error CS0122: 'M<X>(I<X>)' is inaccessible due to its protection level
            diagnostics.Add(new DiagnosticInfoWithSymbols(
                ErrorCode.ERR_BadAccess,
                new object[] { inaccessible.Member },
                symbols), location);
            return true;
        }

        private bool TypeInferenceFailed(
            Binder binder,
            DiagnosticBag diagnostics,
            ImmutableArray<Symbol> symbols,
            BoundExpression receiver,
            AnalyzedArguments arguments,
            Location location,
            CSharpSyntaxNode queryClause = null)
        {
            var inferenceFailed = GetFirstMemberKind(MemberResolutionKind.TypeInferenceFailed);
            if (!inferenceFailed.IsNull)
            {
                Debug.Assert(inferenceFailed.MemberCouldTakeArgumentCount(arguments.Arguments.Count));

                if (queryClause != null)
                {
                    Binder.ReportQueryInferenceFailed(queryClause, inferenceFailed.Member.Name, receiver, arguments, symbols, diagnostics);
                }
                else
                {
                    // error CS0411: The type arguments for method 'M<T>(T)' cannot be inferred
                    // from the usage. Try specifying the type arguments explicitly.
                    diagnostics.Add(new DiagnosticInfoWithSymbols(
                        ErrorCode.ERR_CantInferMethTypeArgs,
                        new object[] { inferenceFailed.Member },
                        symbols), location);
                }

                return true;
            }

            inferenceFailed = GetFirstMemberKind(MemberResolutionKind.TypeInferenceExtensionInstanceArgument);
            if (inferenceFailed.IsNotNull)
            {
                Debug.Assert(arguments.Arguments.Count > 0);
                var instanceArgument = arguments.Arguments[0];
                if (queryClause != null)
                {
                    binder.ReportQueryLookupFailed(queryClause, instanceArgument, inferenceFailed.Member.Name, symbols, diagnostics);
                }
                else
                {
                    diagnostics.Add(new DiagnosticInfoWithSymbols(
                        ErrorCode.ERR_NoSuchMemberOrExtension,
                        new object[] { instanceArgument.Type, inferenceFailed.Member.Name },
                        symbols), location);
                }

                return true;
            }

            return false;
        }

        private bool AllHadBadParameterCount(int expectedCount, bool isMethodGroupConversion = false)
        {
            // Suppose n arguments are supplied; knowing whether to report "no method takes 
            // n arguments" or not requires a more sophisticated analysis than merely checking 
            // to see if any method in the candidate set takes exactly n arguments. Rather, we should check
            // if there is *any* way the method can be called with *only* n arguments, keeping
            // in mind that "params" and optional parameters play a role in deciding that.
            if (!isMethodGroupConversion)
            {
                foreach (var res in this.ResultsBuilder)
                {
                    if (res.MemberCouldTakeArgumentCount(expectedCount))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (var res in this.ResultsBuilder)
                {
                    if (res.Result.Kind != MemberResolutionKind.NoCorrespondingParameter &&
                        res.Result.Kind != MemberResolutionKind.RequiredParameterMissing)
                    {
                        Debug.Assert(res.Result.Kind != MemberResolutionKind.NoCorrespondingNamedParameter);
                        Debug.Assert(res.MemberCouldTakeArgumentCount(expectedCount));
                        return false;
                    }
                }
            }

            return true;
        }

        private bool HadNameUsedForPositional(
            DiagnosticBag diagnostics, AnalyzedArguments arguments,
            ImmutableArray<Symbol> symbols)
        {
            IdentifierNameSyntax badName = null;

            foreach (var mrr in this.ResultsBuilder)
            {
                if (mrr.Result.Kind == MemberResolutionKind.NameUsedForPositional)
                {
                    if (mrr.MemberCouldTakeArgumentCount(arguments.Arguments.Count))
                    {
                        int badArg = mrr.Result.BadArgumentsOpt[0];
                        // We would not have gotten this error had there not been a named argument.
                        Debug.Assert(arguments.Names.Count > 0);
                        badName = arguments.Names[badArg];
                        Debug.Assert(badName != null);
                        break;
                    }
                }
            }

            if (badName == null)
            {
                return false;
            }

            // Named argument 'x' specifies a parameter for which a positional argument has already been given
            Location location = new SourceLocation(badName);

            diagnostics.Add(new DiagnosticInfoWithSymbols(
                ErrorCode.ERR_NamedArgumentUsedInPositional,
                new object[] { badName.Identifier.ValueText },
                symbols), location);
            return true;
        }

        private bool HadNoCorrespondingNamedParameter(
             string methodName, DiagnosticBag diagnostics, AnalyzedArguments arguments,
            NamedTypeSymbol delegateTypeBeingInvoked, ImmutableArray<Symbol> symbols)
        {
            // We know that there is at least one method that had a number of arguments
            // passed that was valid for *some* method in the candidate set. Given that
            // fact, we seek the *best* method in the candidate set to report the error
            // on. If we have a method that has a valid number of arguments, but the
            // call was inapplicable because there was a bad name, that's a candidate
            // for the "best" overload.

            IdentifierNameSyntax badName = null;

            foreach (var mrr in this.ResultsBuilder)
            {
                if (mrr.Result.Kind == MemberResolutionKind.NoCorrespondingNamedParameter)
                {
                    if (mrr.MemberCouldTakeArgumentCount(arguments.Arguments.Count))
                    {
                        int badArg = mrr.Result.BadArgumentsOpt[0];
                        if (arguments.Names.Count > 0)
                        {
                            badName = arguments.Names[badArg];
                        }

                        if (badName != null)
                        {
                            break;
                        }
                    }
                }
            }

            if (badName == null)
            {
                return false;
            }

            // error CS1739: The best overload for 'M' does not have a parameter named 'x'
            // Error CS1746: The delegate 'D' does not have a parameter named 'x'

            Location location = new SourceLocation(badName);

            ErrorCode code = (object)delegateTypeBeingInvoked != null ?
                ErrorCode.ERR_BadNamedArgumentForDelegateInvoke :
                ErrorCode.ERR_BadNamedArgument;

            object obj = (object)delegateTypeBeingInvoked ?? methodName;

            diagnostics.Add(new DiagnosticInfoWithSymbols(
                code,
                new object[] { obj, badName.Identifier.ValueText },
                symbols), location);
            return true;
        }

        private bool MissingRequiredParameter(
            DiagnosticBag diagnostics,
            AnalyzedArguments arguments,
            NamedTypeSymbol delegateTypeBeingInvoked,
            ImmutableArray<Symbol> symbols,
            Location location)
        {
            // We know that there is at least one method that had a number of arguments
            // passed that was valid for *some* method in the candidate set. Given that
            // fact, we seek the *best* method in the candidate set to report the error
            // on. If we have a method that has a valid number of arguments, but the
            // call was inapplicable because a required parameter does not have a 
            // corresponding argument then that's a candidate for the "best" overload.
            //
            // For example, you might have M(int x, int y, int z = 3) and a call
            // M(1, z:4) -- the error cannot be "no overload of M takes 2 arguments"
            // because M does take two arguments; M(1, 2) would be legal. The
            // error instead has to be that there was no argument corresponding
            // to required formal parameter 'y'.

            ParameterSymbol parameter = null;

            foreach (var mrr in this.ResultsBuilder)
            {
                if (mrr.Result.Kind == MemberResolutionKind.RequiredParameterMissing)
                {
                    if (mrr.MemberCouldTakeArgumentCount(arguments.Arguments.Count))
                    {
                        parameter = mrr.Member.GetParameters()[mrr.Result.BadParameter];
                        break;
                    }
                }
            }

            if ((object)parameter == null)
            {
                return false;
            }

            // There is no argument given that corresponds to the required formal parameter '{0}' of '{1}'

            object obj = (object)delegateTypeBeingInvoked ?? parameter.ContainingSymbol;

            diagnostics.Add(new DiagnosticInfoWithSymbols(
                ErrorCode.ERR_NoCorrespondingArgument,
                new object[] { parameter.Name, obj },
                symbols), location);
            return true;
        }

        private bool AllHadBadParameterCount(DiagnosticBag diagnostics, string name, AnalyzedArguments arguments,
            ImmutableArray<Symbol> symbols, Location location, NamedTypeSymbol typeContainingConstructor, NamedTypeSymbol delegateTypeBeingInvoked,
            bool isMethodGroupConversion)
        {
            if (!AllHadBadParameterCount(arguments.Arguments.Count, isMethodGroupConversion))
            {
                return false;
            }

            if (!isMethodGroupConversion)
            {
                // error CS1501: No overload for method 'M' takes n arguments
                // error CS1729: 'M' does not contain a constructor that takes n arguments
                // error CS1593: Delegate 'M' does not take n arguments

                var code =
                    (object)typeContainingConstructor != null ? ErrorCode.ERR_BadCtorArgCount :
                    (object)delegateTypeBeingInvoked != null ? ErrorCode.ERR_BadDelArgCount :
                    ErrorCode.ERR_BadArgCount;
                var target = (object)typeContainingConstructor ?? (object)delegateTypeBeingInvoked ?? name;

                int argCount = arguments.Arguments.Count;
                if (arguments.IsExtensionMethodInvocation)
                {
                    argCount--;
                }

                diagnostics.Add(new DiagnosticInfoWithSymbols(
                    code,
                    new object[] { target, argCount },
                    symbols), location);
            }

            return true;
        }

        private bool ConstraintsCheckFailed(
            ConversionsBase conversions,
            Compilation compilation,
            DiagnosticBag diagnostics,
            AnalyzedArguments arguments,
            Location location)
        {
            // We know that there is at least one method that had a number of arguments
            // passed that was valid for *some* method in the candidate set. Given that
            // fact, we seek the *best* method in the candidate set to report the error
            // on. If we have a generic method that has a valid number of arguments, but the
            // call was inapplicable because a formal parameter type failed to meet its
            // constraints, give an error.
            //
            // That could happen like this:
            //
            // void Q<T>(T t1, Nullable<T> t2) where T : struct
            //
            // Q("", null);
            //
            // Every required parameter has a corresponding argument. Type inference succeeds and infers
            // that T is string. Each argument is convertible to the corresponding formal parameter type.
            // What makes this a not-applicable candidate is not that the constraint on T is violated, but
            // rather that the constraint on *Nullable<T>* is violated; Nullable<string> is not a legal 
            // type, and so this is not an applicable candidate.
            //
            // Checking whether constraints are violated *on T in Q<T>* happens *after* overload resolution
            // successfully chooses a unique best method.
            //
            // Note that this failure need not involve type inference; Q<string>(null, null) would also be
            // illegal for the same reason. 
            // 
            // The question then arises as to what error to report here. The native compiler reports that
            // the constraint is violated on the method, even though the fact that precipitates the 
            // failure of overload resolution to classify this as an applicable candidate is the constraint
            // violation on Nullable<T>. Most of the time this is actually a pretty sensible error message;
            // if you say Q<string>(...) then it seems reasonable to give an error that says that string is
            // bad for Q, not that it is bad for its formal parameters under construction. Since the compiler
            // will not allow Q<T> to be declared without a constraint that ensures that Nullable<T>'s 
            // constraints are met, typically a failure to provide a type argument that works for the 
            // formal parameter type will also be a failure for the method type parameter.
            //
            // However, there could be error recovery scenarios. Suppose instead we had said
            //
            // void Q<T>(T t1, Nullable<T> t2) 
            //
            // with no constraint on T. We will give an error at declaration time, but if later we
            // are asked to provide an analysis of Q<string>("", null), the right thing to do is NOT
            // to say "constraint is violated on T in Q<T>" because there is no constraint to be 
            // violated here. The error is (1) that the constraint is violated on Nullable<T> and
            // (2) that there is a constraint missing on Q<T>. 
            //
            // Another error-recovery scenario in which the method's constraint is not violated:
            //
            // struct C<U> where U : struct {}
            // ...
            // void Q<T>(Nullable<T> nt) where T : struct {}
            // ...
            // Q<C<string>>(null);
            //
            // C<string> is clearly an error, but equally clearly it does not violate the constraint
            // on T because it is a struct. If we attempt overload resolution then overload resolution
            // will say that Q<C<string>> is not an applicable candidate because N<C<string>> is not
            // a valid type. N is not the problem; C<string> is a struct. C<string> is the problem.
            //
            // See test case CS0310ERR_NewConstraintNotSatisfied02 for an even more complex version
            // of this flavour of error recovery.

            MemberResolutionResult<TMember> result = default(MemberResolutionResult<TMember>);
            bool hasResult = false;

            foreach (var mrr in this.ResultsBuilder)
            {
                if (mrr.Result.Kind == MemberResolutionKind.ConstructedParameterFailedConstraintCheck)
                {
                    hasResult = true;
                    result = mrr;
                    break;
                }
            }

            if (!hasResult)
            {
                return false;
            }

            // We would not have gotten as far as type inference succeeding if the argument count
            // was invalid.

            Debug.Assert(result.MemberCouldTakeArgumentCount(arguments.Arguments.Count));

            // Normally a failure to meet constraints on a formal parameter type is also a failure
            // to meet constraints on the method's type argument. See if that's the case; if it
            // is, then just report that error.

            MethodSymbol method = (MethodSymbol)(Symbol)result.Member;
            if (!method.CheckConstraints(conversions, location, compilation, diagnostics))
            {
                // The error is already reported into the diagnostics bag.
                return true;
            }

            // We are in the unusual position that a constraint has been violated on a formal parameter type
            // without being violated on the method. Report that the constraint is violated on the 
            // formal parameter type.

            TypeSymbol formalParameterType = method.ParameterTypes[result.Result.BadParameter];
            formalParameterType.CheckAllConstraints(conversions, location, diagnostics);

            return true;
        }

        private static bool HadLambdaConversionError(DiagnosticBag diagnostics, AnalyzedArguments arguments)
        {
            bool hadError = false;
            foreach (var argument in arguments.Arguments)
            {
                if (argument.Kind == BoundKind.UnboundLambda)
                {
                    hadError |= ((UnboundLambda)argument).GenerateSummaryErrors(diagnostics);
                }
            }

            return hadError;
        }

        private bool HadBadArguments(
            DiagnosticBag diagnostics,
            Compilation compilation,
            string name,
            AnalyzedArguments arguments,
            ImmutableArray<Symbol> symbols,
            Location location,
            BinderFlags flags,
            bool isMethodGroupConversion)
        {
            MemberResolutionResult<TMember> badArg;

            // Just pick one of them.
            if (!TryGetFirstResultWithBadArgument(out badArg))
            {
                return false;
            }

            Debug.Assert(badArg.MemberCouldTakeArgumentCount(arguments.Arguments.Count));

            if (!isMethodGroupConversion)
            {
                var method = badArg.Member;

                // The best overloaded method match for '{0}' has some invalid arguments
                // Since we have bad arguments to report, there is no need to report an error on the invocation itself.
                //var di = new DiagnosticInfoWithSymbols(
                //    ErrorCode.ERR_BadArgTypes,
                //    new object[] { badArg.Method },
                //    symbols);
                //

                if (flags.Includes(BinderFlags.CollectionInitializerAddMethod))
                {
                    // However, if we are binding the collection initializer Add method, we do want to generate
                    // ErrorCode.ERR_BadArgTypesForCollectionAdd or ErrorCode.ERR_InitializerAddHasParamModifiers
                    // as there is no explicit call to Add method.

                    foreach (var parameter in method.GetParameters())
                    {
                        if (parameter.RefKind != RefKind.None)
                        {
                            //  The best overloaded method match '{0}' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
                            diagnostics.Add(ErrorCode.ERR_InitializerAddHasParamModifiers, location, symbols, method);
                            return true;
                        }
                    }

                    //  The best overloaded Add method '{0}' for the collection initializer has some invalid arguments
                    diagnostics.Add(ErrorCode.ERR_BadArgTypesForCollectionAdd, location, symbols, method);
                }

                foreach (var arg in badArg.Result.BadArgumentsOpt)
                {
                    ReportBadArgumentError(diagnostics, compilation, name, arguments, symbols, location, badArg, method, arg);
                }
            }

            return true;
        }

        private static void ReportBadArgumentError(
            DiagnosticBag diagnostics,
            Compilation compilation,
            string name,
            AnalyzedArguments arguments,
            ImmutableArray<Symbol> symbols,
            Location location,
            MemberResolutionResult<TMember> badArg,
            TMember method,
            int arg)
        {
            BoundExpression argument = arguments.Argument(arg);
            int parm = badArg.Result.ParameterFromArgument(arg);
            SourceLocation sourceLocation = new SourceLocation(argument.Syntax);

            // Early out: if the bad argument is an __arglist parameter then simply report that:

            if (method.GetIsVararg() && parm == method.GetParameterCount())
            {
                // NOTE: No SymbolDistinguisher required, since one of the arguments is "__arglist".

                // CS1503: Argument {0}: cannot convert from '{1}' to '{2}'
                diagnostics.Add(
                    ErrorCode.ERR_BadArgType,
                    sourceLocation,
                    symbols,
                    arg + 1,
                    argument.Display,
                    "__arglist");
                return;
            }

            ParameterSymbol parameter = method.GetParameters()[parm];
            RefKind refArg = arguments.RefKind(arg);
            RefKind refParm = parameter.RefKind;

            // If the expression is untyped because it is a lambda, anonymous method, method group or null
            // then we never want to report the error "you need a ref on that thing". Rather, we want to
            // say that you can't convert "null" to "ref int".
            if (!argument.HasExpressionType())
            {
                // If the problem is that a lambda isn't convertible to the given type, also report why.
                if (argument.Kind == BoundKind.UnboundLambda)
                {
                    ((UnboundLambda)argument).GenerateAnonymousFunctionConversionError(diagnostics, parameter.Type);
                }
                else
                {
                    // There's no symbol for the argument, so we don't need a SymbolDistinguisher.

                    // Argument 1: cannot convert from '<null>' to 'ref int'
                    diagnostics.Add(
                        ErrorCode.ERR_BadArgType,
                        sourceLocation,
                        symbols,
                        arg + 1,
                        argument.Display, //'<null>' doesn't need refkind
                        UnwrapIfParamsArray(parameter));
                }
            }
            else if (refArg != refParm)
            {
                if (refParm == RefKind.None)
                {
                    //  Argument {0} should not be passed with the {1} keyword
                    diagnostics.Add(
                        ErrorCode.ERR_BadArgExtraRef,
                        sourceLocation,
                        symbols,
                        arg + 1,
                        refArg.ToDisplayString());
                }
                else
                {
                    //  Argument {0} must be passed with the '{1}' keyword
                    diagnostics.Add(
                        ErrorCode.ERR_BadArgRef,
                        sourceLocation,
                        symbols,
                        arg + 1,
                        refParm.ToDisplayString());
                }
            }
            else
            {
                TypeSymbol argType = argument.Display as TypeSymbol;
                Debug.Assert((object)argType != null);

                if (arguments.IsExtensionMethodThisArgument(arg))
                {
                    Debug.Assert((arg == 0) && (parm == arg));
                    var conversion = badArg.Result.ConversionForArg(parm);

                    if (conversion.IsImplicit)
                    {
                        // CS1928: '{0}' does not contain a definition for '{1}' and the best extension method overload '{2}' has some invalid arguments
                        diagnostics.Add(
                            ErrorCode.ERR_BadExtensionArgTypes,
                            location,
                            symbols,
                            argType,
                            name,
                            method);
                    }
                    else
                    {
                        // CS1929: '{0}' does not contain a definition for '{1}' and the best extension method overload '{2}' requires a receiver of type '{3}'
                        diagnostics.Add(
                            ErrorCode.ERR_BadInstanceArgType,
                            sourceLocation,
                            symbols,
                            argType,
                            name,
                            method,
                            parameter);
                        Debug.Assert((object)parameter == UnwrapIfParamsArray(parameter), "If they ever differ, just call the method when constructing the diagnostic.");
                    }
                }
                else
                {
                    // There's only one slot in the error message for the refkind + arg type, but there isn't a single
                    // object that contains both values, so we have to construct our own.
                    // NOTE: since this is a symbol, it will use the SymbolDisplay options for parameters (i.e. will
                    // have the same format as the display value of the parameter).
                    SignatureOnlyParameterSymbol displayArg = new SignatureOnlyParameterSymbol(
                        argType,
                        ImmutableArray<CustomModifier>.Empty,
                        isParams: false,
                        refKind: refArg);

                    SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, displayArg, UnwrapIfParamsArray(parameter));

                    // CS1503: Argument {0}: cannot convert from '{1}' to '{2}'
                    diagnostics.Add(
                        ErrorCode.ERR_BadArgType,
                        sourceLocation,
                        symbols,
                        arg + 1,
                        distinguisher.First,
                        distinguisher.Second);
                }
            }
        }

        /// <summary>
        /// If an argument fails to convert to the type of the corresponding parameter and that
        /// parameter is a params array, then the error message should reflect the element type
        /// of the params array - not the array type.
        /// </summary>
        private static Symbol UnwrapIfParamsArray(ParameterSymbol parameter)
        {
            if (parameter.IsParams)
            {
                ArrayTypeSymbol arrayType = parameter.Type as ArrayTypeSymbol;
                if ((object)arrayType != null)
                {
                    return arrayType.ElementType;
                }
            }
            return parameter;
        }

        private bool TryGetFirstResultWithBadArgument(out MemberResolutionResult<TMember> result)
        {
            foreach (var res in this.ResultsBuilder)
            {
                if (res.Result.Kind == MemberResolutionKind.BadArguments)
                {
                    result = res;
                    return true;
                }
            }

            result = default(MemberResolutionResult<TMember>);
            return false;
        }

        private bool HadAmbiguousWorseMethods(DiagnosticBag diagnostics, ImmutableArray<Symbol> symbols, Location location, bool isQuery, BoundExpression receiver, string name)
        {
            MemberResolutionResult<TMember> worseResult1;
            MemberResolutionResult<TMember> worseResult2;

            // UNDONE: It is unfortunate that we simply choose the first two methods as the 
            // UNDONE: two to say that are ambiguous; they might not actually be ambiguous
            // UNDONE: with each other. We might consider building a better heuristic here.

            int nWorse = TryGetFirstTwoWorseResults(out worseResult1, out worseResult2);
            if (nWorse <= 1)
            {
                Debug.Assert(nWorse == 0, "How is it that there is exactly one applicable but worse method, and exactly zero applicable best methods?  What was better than this thing?");
                return false;
            }

            if (isQuery)
            {
                // Multiple implementations of the query pattern were found for source type '{0}'.  Ambiguous call to '{1}'.
                diagnostics.Add(ErrorCode.ERR_QueryMultipleProviders, location, receiver.Type, name);
            }
            else
            {
                // error CS0121: The call is ambiguous between the following methods or properties: 'P.W(A)' and 'P.W(B)'
                diagnostics.Add(new DiagnosticInfoWithSymbols(
                    ErrorCode.ERR_AmbigCall,
                    new object[]
                {
                    worseResult1.LeastOverriddenMember.OriginalDefinition,
                    worseResult2.LeastOverriddenMember.OriginalDefinition
                },
                    symbols), location);
            }

            return true;
        }

        private int TryGetFirstTwoWorseResults(out MemberResolutionResult<TMember> first, out MemberResolutionResult<TMember> second)
        {
            int count = 0;
            bool foundFirst = false;
            bool foundSecond = false;
            first = default(MemberResolutionResult<TMember>);
            second = default(MemberResolutionResult<TMember>);

            foreach (var res in this.ResultsBuilder)
            {
                if (res.Result.Kind == MemberResolutionKind.Worse)
                {
                    count++;
                    if (!foundFirst)
                    {
                        first = res;
                        foundFirst = true;
                    }
                    else if (!foundSecond)
                    {
                        second = res;
                        foundSecond = true;
                    }
                }
            }

            return count;
        }

        private bool HadAmbiguousBestMethods(DiagnosticBag diagnostics, ImmutableArray<Symbol> symbols, Location location)
        {
            MemberResolutionResult<TMember> validResult1;
            MemberResolutionResult<TMember> validResult2;
            var nValid = TryGetFirstTwoValidResults(out validResult1, out validResult2);
            if (nValid <= 1)
            {
                Debug.Assert(nValid == 0, "Why are we doing error reporting on an overload resolution problem that had one valid result?");
                return false;
            }

            // error CS0121: The call is ambiguous between the following methods or properties:
            // 'P.Ambiguous(object, string)' and 'P.Ambiguous(string, object)'
            diagnostics.Add(new DiagnosticInfoWithSymbols(
                ErrorCode.ERR_AmbigCall,
                new object[]
                {
                    validResult1.LeastOverriddenMember.OriginalDefinition,
                    validResult2.LeastOverriddenMember.OriginalDefinition
                },
                symbols), location);
            return true;
        }

        private int TryGetFirstTwoValidResults(out MemberResolutionResult<TMember> first, out MemberResolutionResult<TMember> second)
        {
            int count = 0;
            bool foundFirst = false;
            bool foundSecond = false;
            first = default(MemberResolutionResult<TMember>);
            second = default(MemberResolutionResult<TMember>);

            foreach (var res in this.ResultsBuilder)
            {
                if (res.Result.IsValid)
                {
                    count++;
                    if (!foundFirst)
                    {
                        first = res;
                        foundFirst = true;
                    }
                    else if (!foundSecond)
                    {
                        second = res;
                        foundSecond = true;
                    }
                }
            }

            return count;
        }

        private MemberResolutionResult<TMember> GetFirstMemberKind(MemberResolutionKind kind)
        {
            foreach (var result in this.ResultsBuilder)
            {
                if (result.Result.Kind == kind)
                {
                    return result;
                }
            }

            return default(MemberResolutionResult<TMember>);
        }

#if DEBUG
        internal string Dump()
        {
            if (ResultsBuilder.Count == 0)
            {
                return "Overload resolution failed because the method group was empty.";
            }

            var sb = new StringBuilder();
            if (this.Succeeded)
            {
                sb.AppendLine("Overload resolution succeeded and chose " + this.ValidResult.Member.ToString());
            }
            else if (System.Linq.Enumerable.Count(ResultsBuilder, x => x.Result.IsValid) > 1)
            {
                sb.AppendLine("Overload resolution failed because of ambiguous possible best methods.");
            }
            else if (System.Linq.Enumerable.Any(ResultsBuilder, x => (x.Result.Kind == MemberResolutionKind.TypeInferenceFailed) || (x.Result.Kind == MemberResolutionKind.TypeInferenceExtensionInstanceArgument)))
            {
                sb.AppendLine("Overload resolution failed (possibly) because type inference was unable to infer type parameters.");
            }

            sb.AppendLine("Detailed results:");
            foreach (var result in ResultsBuilder)
            {
                sb.AppendFormat("method: {0} reason: {1}\n", result.Member.ToString(), result.Result.Kind.ToString());
            }

            return sb.ToString();
        }
#endif

        #region "Poolable"

        internal static OverloadResolutionResult<TMember> GetInstance()
        {
            return Pool.Allocate();
        }

        internal void Free()
        {
            this.Clear();
            Pool.Free(this);
        }

        //2) Expose the pool or the way to create a pool or the way to get an instance.
        //       for now we will expose both and figure which way works better
        private static readonly ObjectPool<OverloadResolutionResult<TMember>> Pool = CreatePool();

        private static ObjectPool<OverloadResolutionResult<TMember>> CreatePool()
        {
            ObjectPool<OverloadResolutionResult<TMember>> pool = null;
            pool = new ObjectPool<OverloadResolutionResult<TMember>>(() => new OverloadResolutionResult<TMember>(), 10);
            return pool;
        }

        #endregion

        internal CommonOverloadResolutionResult<TSymbol> ToCommon<TSymbol>()
            where TSymbol : ISymbol
        {
            return new CommonOverloadResolutionResult<TSymbol>(
                this.Succeeded,
                this.Succeeded ? this.ValidResult.ToCommon<TSymbol>() : default(CommonMemberResolutionResult<TSymbol>?),
                this.HasBestResult ? this.BestResult.ToCommon<TSymbol>() : default(CommonMemberResolutionResult<TSymbol>?),
                this.Results.SelectAsArray(r => r.ToCommon<TSymbol>()));
        }
    }
}
