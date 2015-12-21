// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private MemberResolutionResult<TMember> _bestResult;
        private ThreeState _bestResultState;
        internal readonly ArrayBuilder<MemberResolutionResult<TMember>> ResultsBuilder;

        // Create an overload resolution result from a single result.
        internal OverloadResolutionResult()
        {
            this.ResultsBuilder = new ArrayBuilder<MemberResolutionResult<TMember>>();
        }

        internal void Clear()
        {
            _bestResult = default(MemberResolutionResult<TMember>);
            _bestResultState = ThreeState.Unknown;
            this.ResultsBuilder.Clear();
        }

        /// <summary>
        /// True if overload resolution successfully selected a single best method.
        /// </summary>
        public bool Succeeded
        {
            get
            {
                if (!_bestResultState.HasValue())
                {
                    _bestResultState = TryGetBestResult(this.ResultsBuilder, out _bestResult);
                }

                return _bestResultState == ThreeState.True && _bestResult.Result.IsValid;
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
                Debug.Assert(_bestResultState == ThreeState.True && _bestResult.Result.IsValid);
                return _bestResult;
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
                Debug.Assert(_bestResultState == ThreeState.True);
                return _bestResult;
            }
        }

        private bool HasBestResult
        {
            get
            {
                return _bestResultState.Value();
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
            best = default(MemberResolutionResult<TMember>);
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

        /// <summary>
        /// Called when overload resolution has failed.  Figures out the best way to describe what went wrong.
        /// </summary>
        /// <remarks>
        /// Overload resolution (effectively) starts out assuming that all candidates are valid and then
        /// gradually disqualifies them.  Therefore, our strategy will be to perform our checks in the
        /// reverse order - the farther a candidate got through the process without being flagged, the
        /// "better" it was.
        /// 
        /// Note that "final validation" is performed after overload resolution,
        /// so final validation errors are not seen here. Final validation errors include
        /// violations of constraints on method type parameters, static/instance mismatches,
        /// and so on.
        /// </remarks>
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

            // This kind is only used for default(MemberResolutionResult<T>), so we should never see it in
            // the candidate list.
            AssertNone(MemberResolutionKind.None);

            var symbols = StaticCast<Symbol>.From(memberGroup);

            //// PHASE 1: Valid candidates ////

            // Since we're here, we know that there isn't exactly one applicable candidate.  There may,
            // however, be more than one.  We'll check for that first, since applicable candidates are
            // always better than inapplicable candidates.

            if (HadAmbiguousBestMethods(diagnostics, symbols, location))
            {
                return;
            }

            // Since we didn't return, we know that there aren't two or more applicable candidates.
            // From above, we know there isn't exactly one either.  Therefore, there must not be any
            // applicable candidates.
            AssertNone(MemberResolutionKind.ApplicableInNormalForm);
            AssertNone(MemberResolutionKind.ApplicableInExpandedForm);

            // There are two ways that otherwise-applicable candidates can be ruled out by overload resolution:
            //   a) there is another applicable candidate that is strictly better, or
            //   b) there is another applicable candidate from a more derived type.
            // There can't be exactly one such candidate, since that would the existence of some better 
            // applicable candidate, which would have either won or been detected above.  It is possible,
            // however, that there are multiple candidates that are worse than each other in a cycle.
            // This might sound like a paradox, but it is in fact possible. Because there are
            // intransitivities in convertibility (where A-->B, B-->C and C-->A but none of the
            // opposite conversions are legal) there are also intransitivities in betterness. 
            // (Obviously, there can't be a LessDerived cycle, since we break type hierarchy cycles during
            // symbol table construction.)

            if (HadAmbiguousWorseMethods(diagnostics, symbols, location, queryClause != null, receiver, name))
            {
                return;
            }

            // Since we didn't return, we know that there aren't two or "worse" candidates.  As above,
            // there also can't be a single one.  Therefore, there are none.
            AssertNone(MemberResolutionKind.Worse);

            // If there's a less-derived candidate, it must be less derived than some applicable or
            // "worse" candidate.  Since there are none of those, there must not be any less-derived
            // candidates either.
            AssertNone(MemberResolutionKind.LessDerived);


            //// PHASE 2: Applicability failures ////

            // Overload resolution performed these checks just before weeding out less-derived and worse candidates.

            // If we got as far as converting a lambda to a delegate type, and we failed to
            // do so, then odds are extremely good that the failure is the ultimate cause
            // of the overload resolution failing to find any applicable method. Report
            // the errors out of each lambda argument, if there were any.

            if (HadLambdaConversionError(diagnostics, arguments))
            {
                return;
            }

            // NOTE: There isn't a MemberResolutionKind for this error condition.

            // If there is any such method that has a bad conversion or out/ref mismatch 
            // then the first such method found is the best bad method.

            if (HadBadArguments(diagnostics, binder.Compilation, name, arguments, symbols, location, binder.Flags, isMethodGroupConversion))
            {
                return;
            }

            // Since we didn't return...
            AssertNone(MemberResolutionKind.BadArguments);

            // Otherwise, if there is any such method where type inference succeeded but inferred
            // a type that violates its own constraints then the first such method is 
            // the best bad method.

            if (ConstraintsCheckFailed(binder.Conversions, binder.Compilation, diagnostics, arguments, location))
            {
                return;
            }

            // Since we didn't return...
            AssertNone(MemberResolutionKind.ConstructedParameterFailedConstraintCheck);

            // Otherwise, if there is any such method where type inference succeeded but inferred
            // an inaccessible type then the first such method found is the best bad method.

            if (InaccessibleTypeArgument(diagnostics, symbols, arguments, location))
            {
                return;
            }

            // Since we didn't return...
            AssertNone(MemberResolutionKind.InaccessibleTypeArgument);

            // Otherwise, if there is any such method where type inference failed then the
            // first such method is the best bad method.

            if (TypeInferenceFailed(binder, diagnostics, symbols, receiver, arguments, location, queryClause))
            {
                return;
            }

            // Since we didn't return...
            AssertNone(MemberResolutionKind.TypeInferenceFailed);
            AssertNone(MemberResolutionKind.TypeInferenceExtensionInstanceArgument);


            //// PHASE 3: Use site errors ////

            // Overload resolution checks for use site errors between argument analysis and applicability testing.

            // Otherwise, if there is any such method that cannot be used because it is
            // in an unreferenced assembly then the first such method is the best bad method.

            if (UseSiteError(diagnostics, symbols, location))
            {
                return;
            }

            // Since we didn't return...
            AssertNone(MemberResolutionKind.UseSiteError);


            //// PHASE 4: Argument analysis failures and unsupported metadata ////

            // The first to checks in overload resolution are for unsupported metadata (Symbol.HasUnsupportedMetadata)
            // and argument analysis.  We don't want to report unsupported metadata unless nothing else went wrong -
            // otherwise we'd report errors about losing candidates, effectively "pulling in" unnecessary assemblies.

            bool supportedRequiredParameterMissingConflicts = false;
            MemberResolutionResult<TMember> firstSupported = default(MemberResolutionResult<TMember>);
            MemberResolutionResult<TMember> firstUnsupported = default(MemberResolutionResult<TMember>);

            var supportedInPriorityOrder = new MemberResolutionResult<TMember>[4]; // from highest to lowest priority
            const int requiredParameterMissingPriority = 0;
            const int nameUsedForPositionalPriority = 1;
            const int noCorrespondingNamedParameterPriority = 2;
            const int noCorrespondingParameterPriority = 3;

            foreach (MemberResolutionResult<TMember> result in this.ResultsBuilder)
            {
                switch (result.Result.Kind)
                {
                    case MemberResolutionKind.UnsupportedMetadata:
                        if (firstSupported.IsNull)
                        {
                            firstUnsupported = result;
                        }
                        break;
                    case MemberResolutionKind.NoCorrespondingNamedParameter:
                        if (supportedInPriorityOrder[noCorrespondingNamedParameterPriority].IsNull ||
                            result.Result.BadArgumentsOpt[0] > supportedInPriorityOrder[noCorrespondingNamedParameterPriority].Result.BadArgumentsOpt[0])
                        {
                            supportedInPriorityOrder[noCorrespondingNamedParameterPriority] = result;
                        }
                        break;
                    case MemberResolutionKind.NoCorrespondingParameter:
                        if (supportedInPriorityOrder[noCorrespondingParameterPriority].IsNull)
                        {
                            supportedInPriorityOrder[noCorrespondingParameterPriority] = result;
                        }
                        break;
                    case MemberResolutionKind.RequiredParameterMissing:
                        if (supportedInPriorityOrder[requiredParameterMissingPriority].IsNull)
                        {
                            Debug.Assert(!supportedRequiredParameterMissingConflicts);
                            supportedInPriorityOrder[requiredParameterMissingPriority] = result;
                        }
                        else
                        {
                            supportedRequiredParameterMissingConflicts = true;
                        }
                        break;
                    case MemberResolutionKind.NameUsedForPositional:
                        if (supportedInPriorityOrder[nameUsedForPositionalPriority].IsNull ||
                            result.Result.BadArgumentsOpt[0] > supportedInPriorityOrder[nameUsedForPositionalPriority].Result.BadArgumentsOpt[0])
                        {
                            supportedInPriorityOrder[nameUsedForPositionalPriority] = result;
                        }
                        break;
                    default:
                        // Based on the asserts above, we know that only the kinds above
                        // are possible at this point.  This should only throw if a new
                        // kind is added without appropriate checking above.
                        throw ExceptionUtilities.UnexpectedValue(result.Result.Kind);
                }
            }

            foreach (var supported in supportedInPriorityOrder)
            {
                if (supported.IsNotNull)
                {
                    firstSupported = supported;
                    break;
                }
            }

            // If there are any supported candidates, we don't care about unsupported candidates.
            if (firstSupported.IsNotNull)
            {
                // If there are multiple supported candidates, we don't have a good way to choose the best
                // one so we report a general diagnostic (below).
                if (!(firstSupported.Result.Kind == MemberResolutionKind.RequiredParameterMissing && supportedRequiredParameterMissingConflicts) && !isMethodGroupConversion)
                {
                    switch (firstSupported.Result.Kind)
                    {
                        // Otherwise, if there is any such method that has a named argument and a positional 
                        // argument for the same parameter then the first such method is the best bad method.
                        case MemberResolutionKind.NameUsedForPositional:
                            ReportNameUsedForPositional(firstSupported, diagnostics, arguments, symbols);
                            return;

                        // Otherwise, if there is any such method that has a named argument that corresponds
                        // to no parameter then the first such method is the best bad method.
                        case MemberResolutionKind.NoCorrespondingNamedParameter:
                            ReportNoCorrespondingNamedParameter(firstSupported, name, diagnostics, arguments, delegateTypeBeingInvoked, symbols);
                            return;

                        // Otherwise, if there is any such method that has a required parameter
                        // but no argument was supplied for it then the first such method is 
                        // the best bad method.
                        case MemberResolutionKind.RequiredParameterMissing:
                            // CONSIDER: for consistency with dev12, we would goto default except in omitted ref cases.
                            ReportMissingRequiredParameter(firstSupported, diagnostics, arguments, delegateTypeBeingInvoked, symbols, location);
                            return;

                        // NOTE: For some reason, there is no specific handling for this result kind.
                        case MemberResolutionKind.NoCorrespondingParameter:
                            break;
                    }
                }
            }
            else if (firstUnsupported.IsNotNull)
            {
                // Otherwise, if there is any such method that cannot be used because it is                
                // unsupported by the language then the first such method is the best bad method.
                // This is the first kind of problem overload resolution checks for, so it should
                // be the last MemberResolutionKind we check for.  Candidates with this kind
                // failed the soonest.

                // CONSIDER: report his on every unsupported candidate?
                ReportUnsupportedMetadata(location, diagnostics, symbols, firstUnsupported);
                return;
            }

            // If the user provided a number of arguments that works for no possible method in the method
            // group then we give an error saying that.  Every method will have an error of the form
            // "missing required parameter" or "argument corresponds to no parameter", and therefore we
            // have no way of choosing a "best bad method" to report the error on. We should simply
            // say that no possible method can take the given number of arguments.

            // CAVEAT: For method group conversions, the caller reports a different diagnostics.

            if (!isMethodGroupConversion)
            {
                ReportBadParameterCount(diagnostics, name, arguments, symbols, location, typeContainingConstructor, delegateTypeBeingInvoked);
            }
        }

        private static void ReportUnsupportedMetadata(Location location, DiagnosticBag diagnostics, ImmutableArray<Symbol> symbols, MemberResolutionResult<TMember> firstUnsupported)
        {
            DiagnosticInfo diagInfo = firstUnsupported.Member.GetUseSiteDiagnostic();
            Debug.Assert(diagInfo != null);
            Debug.Assert(diagInfo.Severity == DiagnosticSeverity.Error);

            // Attach symbols to the diagnostic info.
            diagInfo = new DiagnosticInfoWithSymbols(
                (ErrorCode)diagInfo.Code,
                diagInfo.Arguments,
                symbols);

            Symbol.ReportUseSiteDiagnostic(diagInfo, diagnostics, location);
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
            if (inferenceFailed.IsNotNull)
            {
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

        private static void ReportNameUsedForPositional(
            MemberResolutionResult<TMember> bad,
            DiagnosticBag diagnostics,
            AnalyzedArguments arguments,
            ImmutableArray<Symbol> symbols)
        {
            int badArg = bad.Result.BadArgumentsOpt[0];
            // We would not have gotten this error had there not been a named argument.
            Debug.Assert(arguments.Names.Count > badArg);
            IdentifierNameSyntax badName = arguments.Names[badArg];
            Debug.Assert(badName != null);

            // Named argument 'x' specifies a parameter for which a positional argument has already been given
            Location location = new SourceLocation(badName);

            diagnostics.Add(new DiagnosticInfoWithSymbols(
                ErrorCode.ERR_NamedArgumentUsedInPositional,
                new object[] { badName.Identifier.ValueText },
                symbols), location);
        }

        private static void ReportNoCorrespondingNamedParameter(
            MemberResolutionResult<TMember> bad,
            string methodName,
            DiagnosticBag diagnostics,
            AnalyzedArguments arguments,
            NamedTypeSymbol delegateTypeBeingInvoked,
            ImmutableArray<Symbol> symbols)
        {
            // We know that there is at least one method that had a number of arguments
            // passed that was valid for *some* method in the candidate set. Given that
            // fact, we seek the *best* method in the candidate set to report the error
            // on. If we have a method that has a valid number of arguments, but the
            // call was inapplicable because there was a bad name, that's a candidate
            // for the "best" overload.

            int badArg = bad.Result.BadArgumentsOpt[0];
            // We would not have gotten this error had there not been a named argument.
            Debug.Assert(arguments.Names.Count > badArg);
            IdentifierNameSyntax badName = arguments.Names[badArg];
            Debug.Assert(badName != null);

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
        }

        private static void ReportMissingRequiredParameter(
            MemberResolutionResult<TMember> bad,
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

            TMember badMember = bad.Member;
            ImmutableArray<ParameterSymbol> parameters = badMember.GetParameters();
            int badParamIndex = bad.Result.BadParameter;
            string badParamName;
            if (badParamIndex == parameters.Length)
            {
                Debug.Assert(badMember.Kind == SymbolKind.Method);
                Debug.Assert(((MethodSymbol)(object)badMember).IsVararg);
                badParamName = SyntaxFacts.GetText(SyntaxKind.ArgListKeyword);
            }
            else
            {
                badParamName = parameters[badParamIndex].Name;
            }

            // There is no argument given that corresponds to the required formal parameter '{0}' of '{1}'

            object obj = (object)delegateTypeBeingInvoked ?? badMember;

            diagnostics.Add(new DiagnosticInfoWithSymbols(
                ErrorCode.ERR_NoCorrespondingArgument,
                new object[] { badParamName, obj },
                symbols), location);
        }

        private static void ReportBadParameterCount(
            DiagnosticBag diagnostics,
            string name,
            AnalyzedArguments arguments,
            ImmutableArray<Symbol> symbols,
            Location location,
            NamedTypeSymbol typeContainingConstructor,
            NamedTypeSymbol delegateTypeBeingInvoked)
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

            return;
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
            // of this flavor of error recovery.

            var result = GetFirstMemberKind(MemberResolutionKind.ConstructedParameterFailedConstraintCheck);
            if (result.IsNull)
            {
                return false;
            }

            // We would not have gotten as far as type inference succeeding if the argument count
            // was invalid.

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
            var badArg = GetFirstMemberKind(MemberResolutionKind.BadArguments);
            if (badArg.IsNull)
            {
                return false;
            }

            if (isMethodGroupConversion)
            {
                return true;
            }

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
                // The argument and parameter type might match, but may not have same in/out modifiers
                if (argument.Kind == BoundKind.UnboundLambda && refArg == refParm)
                {
                    ((UnboundLambda)argument).GenerateAnonymousFunctionConversionError(diagnostics, parameter.Type.TypeSymbol);
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
                        TypeSymbolWithAnnotations.Create(argType),
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
                ArrayTypeSymbol arrayType = parameter.Type.TypeSymbol as ArrayTypeSymbol;
                if ((object)arrayType != null && arrayType.IsSZArray)
                {
                    return arrayType.ElementType.TypeSymbol;
                }
            }
            return parameter;
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
                var first = worseResult1.LeastOverriddenMember.OriginalDefinition;
                var second = worseResult2.LeastOverriddenMember.OriginalDefinition;

                if (first.ContainingNamespace != second.ContainingNamespace)
                {
                    diagnostics.Add(new DiagnosticInfoWithSymbols(
                        ErrorCode.ERR_AmbigCall,
                        new object[]
                            {
                                new FormattedSymbol(first, SymbolDisplayFormat.CSharpErrorMessageFormat),
                                new FormattedSymbol(second, SymbolDisplayFormat.CSharpErrorMessageFormat)
                            },
                        symbols), location);
                }
                else
                {
                    diagnostics.Add(new DiagnosticInfoWithSymbols(
                        ErrorCode.ERR_AmbigCall,
                        new object[]
                            {
                                first,
                                second
                            },
                        symbols), location);
                }
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
            var first = validResult1.LeastOverriddenMember.OriginalDefinition;
            var second = validResult2.LeastOverriddenMember.OriginalDefinition;

            if (first.ContainingNamespace != second.ContainingNamespace)
            {
                diagnostics.Add(new DiagnosticInfoWithSymbols(
                ErrorCode.ERR_AmbigCall,
                new object[]
                    {
                        new FormattedSymbol(first, SymbolDisplayFormat.CSharpErrorMessageFormat),
                        new FormattedSymbol(second, SymbolDisplayFormat.CSharpErrorMessageFormat)
                    },
                symbols), location);
            }
            else
            {
                diagnostics.Add(new DiagnosticInfoWithSymbols(
                ErrorCode.ERR_AmbigCall,
                new object[]
                    {
                        first,
                        second
                    },
                symbols), location);
            }

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

        [Conditional("DEBUG")]
        private void AssertNone(MemberResolutionKind kind)
        {
            foreach (var result in this.ResultsBuilder)
            {
                if (result.Result.Kind == kind)
                {
                    throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
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
            return s_pool.Allocate();
        }

        internal void Free()
        {
            this.Clear();
            s_pool.Free(this);
        }

        //2) Expose the pool or the way to create a pool or the way to get an instance.
        //       for now we will expose both and figure which way works better
        private static readonly ObjectPool<OverloadResolutionResult<TMember>> s_pool = CreatePool();

        private static ObjectPool<OverloadResolutionResult<TMember>> CreatePool()
        {
            ObjectPool<OverloadResolutionResult<TMember>> pool = null;
            pool = new ObjectPool<OverloadResolutionResult<TMember>>(() => new OverloadResolutionResult<TMember>(), 10);
            return pool;
        }

        #endregion
    }
}
