// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum BetterResult
    {
        Left,
        Right,
        Neither,
        Equal
    }

    internal sealed partial class OverloadResolution
    {
        private readonly Binder _binder;

        public OverloadResolution(Binder binder)
        {
            _binder = binder;
        }

        private CSharpCompilation Compilation
        {
            get { return _binder.Compilation; }
        }

        private Conversions Conversions
        {
            get { return _binder.Conversions; }
        }

        // lazily compute if the compiler is in "strict" mode (rather than duplicating bugs for compatibility)
        private bool? _strict;
        private bool Strict
        {
            get
            {
                if (_strict.HasValue) return _strict.Value;
                bool value = _binder.Compilation.FeatureStrictEnabled;
                _strict = value;
                return value;
            }
        }

        // UNDONE: This List<MethodResolutionResult> deal should probably be its own data structure.
        // We need an indexable collection of mappings from method candidates to their up-to-date
        // overload resolution status. It must be fast and memory efficient, but it will very often
        // contain just 1 candidate.      
        private static bool AnyValidResult<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results)
            where TMember : Symbol
        {
            foreach (var result in results)
            {
                if (result.IsValid)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SingleValidResult<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results)
            where TMember : Symbol
        {
            bool oneValid = false;
            foreach (var result in results)
            {
                if (result.IsValid)
                {
                    if (oneValid)
                    {
                        return false;
                    }

                    oneValid = true;
                }
            }

            return oneValid;
        }

        // Perform overload resolution on the given method group, with the given arguments and
        // names. The names can be null if no names were supplied to any arguments.
        public void ObjectCreationOverloadResolution(ImmutableArray<MethodSymbol> constructors, AnalyzedArguments arguments, OverloadResolutionResult<MethodSymbol> result, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var results = result.ResultsBuilder;

            // First, attempt overload resolution not getting complete results.
            PerformObjectCreationOverloadResolution(results, constructors, arguments, false, ref useSiteDiagnostics);

            if (!OverloadResolutionResultIsValid(results, arguments.HasDynamicArgument))
            {
                // We didn't get a single good result. Get full results of overload resolution and return those.
                result.Clear();
                PerformObjectCreationOverloadResolution(results, constructors, arguments, true, ref useSiteDiagnostics);
            }
        }

        // Perform overload resolution on the given method group, with the given arguments and
        // names. The names can be null if no names were supplied to any arguments.
        public void MethodInvocationOverloadResolution(
            ArrayBuilder<MethodSymbol> methods,
            ArrayBuilder<TypeWithAnnotations> typeArguments,
            BoundExpression receiver,
            AnalyzedArguments arguments,
            OverloadResolutionResult<MethodSymbol> result,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool isMethodGroupConversion = false,
            bool allowRefOmittedArguments = false,
            bool inferWithDynamic = false,
            bool allowUnexpandedForm = true,
            RefKind returnRefKind = default,
            TypeSymbol returnType = null)
        {
            MethodOrPropertyOverloadResolution(
                methods, typeArguments, receiver, arguments, result, isMethodGroupConversion,
                allowRefOmittedArguments, ref useSiteDiagnostics, inferWithDynamic: inferWithDynamic,
                allowUnexpandedForm: allowUnexpandedForm,
                returnRefKind: returnRefKind,
                returnType: returnType);
        }

        // Perform overload resolution on the given property group, with the given arguments and
        // names. The names can be null if no names were supplied to any arguments.
        public void PropertyOverloadResolution(
            ArrayBuilder<PropertySymbol> indexers,
            BoundExpression receiverOpt,
            AnalyzedArguments arguments,
            OverloadResolutionResult<PropertySymbol> result,
            bool allowRefOmittedArguments,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            ArrayBuilder<TypeWithAnnotations> typeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            MethodOrPropertyOverloadResolution(
                indexers, typeArguments, receiverOpt, arguments, result, isMethodGroupConversion: false,
                allowRefOmittedArguments: allowRefOmittedArguments, useSiteDiagnostics: ref useSiteDiagnostics);
            typeArguments.Free();
        }

        internal void MethodOrPropertyOverloadResolution<TMember>(
            ArrayBuilder<TMember> members,
            ArrayBuilder<TypeWithAnnotations> typeArguments,
            BoundExpression receiver,
            AnalyzedArguments arguments,
            OverloadResolutionResult<TMember> result,
            bool isMethodGroupConversion,
            bool allowRefOmittedArguments,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool inferWithDynamic = false,
            bool allowUnexpandedForm = true,
            RefKind returnRefKind = default,
            TypeSymbol returnType = null)
            where TMember : Symbol
        {
            var results = result.ResultsBuilder;

            // First, attempt overload resolution not getting complete results.
            PerformMemberOverloadResolution(
                results: results, members: members, typeArguments: typeArguments,
                receiver: receiver, arguments: arguments, completeResults: false,
                isMethodGroupConversion: isMethodGroupConversion, returnRefKind: returnRefKind, returnType: returnType,
                allowRefOmittedArguments: allowRefOmittedArguments, useSiteDiagnostics: ref useSiteDiagnostics,
                inferWithDynamic: inferWithDynamic, allowUnexpandedForm: allowUnexpandedForm);

            if (!OverloadResolutionResultIsValid(results, arguments.HasDynamicArgument))
            {
                // We didn't get a single good result. Get full results of overload resolution and return those.
                result.Clear();
                PerformMemberOverloadResolution(
                    results: results, members: members, typeArguments: typeArguments,
                    receiver: receiver, arguments: arguments, completeResults: true,
                    isMethodGroupConversion: isMethodGroupConversion, returnRefKind: returnRefKind, returnType: returnType,
                    allowRefOmittedArguments: allowRefOmittedArguments, useSiteDiagnostics: ref useSiteDiagnostics,
                    allowUnexpandedForm: allowUnexpandedForm);
            }
        }

        private static bool OverloadResolutionResultIsValid<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results, bool hasDynamicArgument)
            where TMember : Symbol
        {
            // If there were no dynamic arguments then overload resolution succeeds if there is exactly one method
            // that is applicable and not worse than another method.
            //
            // If there were dynamic arguments then overload resolution succeeds if there were one or more applicable
            // methods; which applicable method that will be invoked, if any, will be worked out at runtime.
            //
            // Note that we could in theory do a better job of detecting situations that we know will fail. We do not
            // treat methods that violate generic type constraints as inapplicable; rather, if such a method is chosen
            // as the best method we give an error during the "final validation" phase. In the dynamic argument
            // scenario there could be two methods, both applicable, ambiguous as to which is better, and neither
            // would pass final validation. In that case we could give the error at compile time, but we do not.

            if (hasDynamicArgument)
            {
                foreach (var curResult in results)
                {
                    if (curResult.Result.IsApplicable)
                    {
                        return true;
                    }
                }

                return false;
            }

            return SingleValidResult(results);
        }

        // Perform method/indexer overload resolution, storing the results into "results". If
        // completeResults is false, then invalid results don't have to be stored. The results will
        // still contain all possible successful resolution.
        private void PerformMemberOverloadResolution<TMember>(
            ArrayBuilder<MemberResolutionResult<TMember>> results,
            ArrayBuilder<TMember> members,
            ArrayBuilder<TypeWithAnnotations> typeArguments,
            BoundExpression receiver,
            AnalyzedArguments arguments,
            bool completeResults,
            bool isMethodGroupConversion,
            RefKind returnRefKind,
            TypeSymbol returnType,
            bool allowRefOmittedArguments,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool inferWithDynamic = false,
            bool allowUnexpandedForm = true)
            where TMember : Symbol
        {
            // SPEC: The binding-time processing of a method invocation of the form M(A), where M is a 
            // SPEC: method group (possibly including a type-argument-list), and A is an optional 
            // SPEC: argument-list, consists of the following steps:

            // NOTE: We use a quadratic algorithm to determine which members override/hide
            // each other (i.e. we compare them pairwise).  We could move to a linear
            // algorithm that builds the closure set of overridden/hidden members and then
            // uses that set to filter the candidate, but that would still involve realizing
            // a lot of PE symbols.  Instead, we partition the candidates by containing type.
            // With this information, we can efficiently skip checks where the (potentially)
            // overriding or hiding member is not in a subtype of the type containing the
            // (potentially) overridden or hidden member.
            Dictionary<NamedTypeSymbol, ArrayBuilder<TMember>> containingTypeMapOpt = null;
            if (members.Count > 50) // TODO: fine-tune this value
            {
                containingTypeMapOpt = PartitionMembersByContainingType(members);
            }

            // SPEC: The set of candidate methods for the method invocation is constructed.
            for (int i = 0; i < members.Count; i++)
            {
                AddMemberToCandidateSet(
                    members[i], results, members, typeArguments, receiver, arguments, completeResults,
                    isMethodGroupConversion, allowRefOmittedArguments, containingTypeMapOpt, inferWithDynamic: inferWithDynamic,
                    useSiteDiagnostics: ref useSiteDiagnostics, allowUnexpandedForm: allowUnexpandedForm);
            }

            // CONSIDER: use containingTypeMapOpt for RemoveLessDerivedMembers?

            ClearContainingTypeMap(ref containingTypeMapOpt);

            // Remove methods that are inaccessible because their inferred type arguments are inaccessible.
            // It is not clear from the spec how or where this is supposed to occur.
            RemoveInaccessibleTypeArguments(results, ref useSiteDiagnostics);

            // SPEC: The set of candidate methods is reduced to contain only methods from the most derived types.
            RemoveLessDerivedMembers(results, ref useSiteDiagnostics);

            if (Compilation.LanguageVersion.AllowImprovedOverloadCandidates())
            {
                RemoveStaticInstanceMismatches(results, arguments, receiver);

                RemoveConstraintViolations(results);

                if (isMethodGroupConversion)
                {
                    RemoveDelegateConversionsWithWrongReturnType(results, ref useSiteDiagnostics, returnRefKind, returnType);
                }
            }

            // NB: As in dev12, we do this AFTER removing less derived members.
            // Also note that less derived members are not actually removed - they are simply flagged.
            ReportUseSiteDiagnostics(results, ref useSiteDiagnostics);

            // SPEC: If the resulting set of candidate methods is empty, then further processing along the following steps are abandoned,
            // SPEC: and instead an attempt is made to process the invocation as an extension method invocation. If this fails, then no
            // SPEC: applicable methods exist, and a binding-time error occurs.
            if (!AnyValidResult(results))
            {
                return;
            }

            // SPEC: The best method of the set of candidate methods is identified. If a single best method cannot be identified,
            // SPEC: the method invocation is ambiguous, and a binding-time error occurs.

            RemoveWorseMembers(results, arguments, ref useSiteDiagnostics);

            // Note, the caller is responsible for "final validation",
            // as that is not part of overload resolution.
        }

        private void RemoveStaticInstanceMismatches<TMember>(
            ArrayBuilder<MemberResolutionResult<TMember>> results,
            AnalyzedArguments arguments,
            BoundExpression receiverOpt) where TMember : Symbol
        {
            // When the feature 'ImprovedOverloadCandidates' is enabled, we do not include instance members when the receiver
            // is a type, or static members when the receiver is an instance. This does not apply to extension method invocations,
            // because extension methods are only considered when the receiver is an instance. It also does not apply when the
            // receiver is a TypeOrValueExpression, which is used to handle the receiver of a Color-Color ambiguity, where either
            // an instance or a static member would be acceptable.
            if (arguments.IsExtensionMethodInvocation || Binder.IsTypeOrValueExpression(receiverOpt))
            {
                return;
            }

            bool isImplicitReceiver = Binder.WasImplicitReceiver(receiverOpt);
            // isStaticContext includes both places where `this` isn't available, and places where it
            // cannot be used (e.g. a field initializer or a constructor-initializer)
            bool isStaticContext = !_binder.HasThis(!isImplicitReceiver, out bool inStaticContext) || inStaticContext;
            if (isImplicitReceiver && !isStaticContext)
            {
                return;
            }

            // We are in a context where only instance (or only static) methods are permitted. We reject the others.
            bool keepStatic = isImplicitReceiver && isStaticContext || Binder.IsMemberAccessedThroughType(receiverOpt);

            for (int f = 0; f < results.Count; ++f)
            {
                var result = results[f];
                TMember member = result.Member;
                if (result.Result.IsValid && member.RequiresInstanceReceiver() == keepStatic)
                {
                    results[f] = new MemberResolutionResult<TMember>(member, result.LeastOverriddenMember, MemberAnalysisResult.StaticInstanceMismatch());
                }
            }
        }

        private void RemoveConstraintViolations<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results) where TMember : Symbol
        {
            // When the feature 'ImprovedOverloadCandidates' is enabled, we do not include methods for which the type arguments
            // violate the constraints of the method's type parameters.

            // Constraint violations apply to method in a method group, not to properties in a "property group".
            if (typeof(TMember) != typeof(MethodSymbol))
            {
                return;
            }

            for (int f = 0; f < results.Count; ++f)
            {
                var result = results[f];
                var member = (MethodSymbol)(Symbol)result.Member;
                // a constraint failure on the method trumps (for reporting purposes) a previously-detected
                // constraint failure on the constructed type of a parameter
                if ((result.Result.IsValid || result.Result.Kind == MemberResolutionKind.ConstructedParameterFailedConstraintCheck) &&
                    FailsConstraintChecks(member, out ArrayBuilder<TypeParameterDiagnosticInfo> constraintFailureDiagnosticsOpt))
                {
                    results[f] = new MemberResolutionResult<TMember>(
                        result.Member, result.LeastOverriddenMember,
                        MemberAnalysisResult.ConstraintFailure(constraintFailureDiagnosticsOpt.ToImmutableAndFree()));
                }
            }
        }

        private bool FailsConstraintChecks(MethodSymbol method, out ArrayBuilder<TypeParameterDiagnosticInfo> constraintFailureDiagnosticsOpt)
        {
            if (method.Arity == 0 || method.OriginalDefinition == (object)method)
            {
                constraintFailureDiagnosticsOpt = null;
                return false;
            }

            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            bool constraintsSatisfied = ConstraintsHelper.CheckMethodConstraints(
                method,
                this.Conversions,
                includeNullability: false,
                this.Compilation,
                diagnosticsBuilder,
                nullabilityDiagnosticsBuilderOpt: null,
                ref useSiteDiagnosticsBuilder);

            if (!constraintsSatisfied)
            {
                if (useSiteDiagnosticsBuilder != null)
                {
                    diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder);
                    useSiteDiagnosticsBuilder.Free();
                }

                constraintFailureDiagnosticsOpt = diagnosticsBuilder;
                return true;
            }

            diagnosticsBuilder.Free();
            useSiteDiagnosticsBuilder?.Free();
            constraintFailureDiagnosticsOpt = null;
            return false;
        }

        /// <summary>
        /// Remove candidates to a delegate conversion where the method's return ref kind or return type is wrong.
        /// </summary>
        /// <param name="returnRefKind">The ref kind of the delegate's return, if known. This is only unknown in
        /// error scenarios, such as a delegate type that has no invoke method.</param>
        /// <param name="returnType">The return type of the delegate, if known. It isn't
        /// known when we're attempting to infer the return type of a method group for type inference.</param>
        private void RemoveDelegateConversionsWithWrongReturnType<TMember>(
            ArrayBuilder<MemberResolutionResult<TMember>> results,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            RefKind? returnRefKind,
            TypeSymbol returnType) where TMember : Symbol
        {
            // When the feature 'ImprovedOverloadCandidates' is enabled, then a delegate conversion overload resolution
            // rejects candidates that have the wrong return ref kind or return type.

            // Delegate conversions apply to method in a method group, not to properties in a "property group".
            Debug.Assert(typeof(TMember) == typeof(MethodSymbol));

            for (int f = 0; f < results.Count; ++f)
            {
                var result = results[f];
                if (!result.Result.IsValid)
                {
                    continue;
                }

                var method = (MethodSymbol)(Symbol)result.Member;
                bool returnsMatch =
                    (object)returnType == null ||
                    method.ReturnType.Equals(returnType, TypeCompareKind.AllIgnoreOptions) ||
                    returnRefKind == RefKind.None && Conversions.HasIdentityOrImplicitReferenceConversion(method.ReturnType, returnType, ref useSiteDiagnostics);
                if (!returnsMatch)
                {
                    results[f] = new MemberResolutionResult<TMember>(
                        result.Member, result.LeastOverriddenMember, MemberAnalysisResult.WrongReturnType());
                }
                else if (method.RefKind != returnRefKind)
                {
                    results[f] = new MemberResolutionResult<TMember>(
                        result.Member, result.LeastOverriddenMember, MemberAnalysisResult.WrongRefKind());
                }
            }
        }

        private static Dictionary<NamedTypeSymbol, ArrayBuilder<TMember>> PartitionMembersByContainingType<TMember>(ArrayBuilder<TMember> members) where TMember : Symbol
        {
            Dictionary<NamedTypeSymbol, ArrayBuilder<TMember>> containingTypeMap = new Dictionary<NamedTypeSymbol, ArrayBuilder<TMember>>();
            for (int i = 0; i < members.Count; i++)
            {
                TMember member = members[i];
                NamedTypeSymbol containingType = member.ContainingType;
                ArrayBuilder<TMember> builder;
                if (!containingTypeMap.TryGetValue(containingType, out builder))
                {
                    builder = ArrayBuilder<TMember>.GetInstance();
                    containingTypeMap[containingType] = builder;
                }
                builder.Add(member);
            }
            return containingTypeMap;
        }

        private static void ClearContainingTypeMap<TMember>(ref Dictionary<NamedTypeSymbol, ArrayBuilder<TMember>> containingTypeMapOpt) where TMember : Symbol
        {
            if ((object)containingTypeMapOpt != null)
            {
                foreach (ArrayBuilder<TMember> builder in containingTypeMapOpt.Values)
                {
                    builder.Free();
                }
                containingTypeMapOpt = null;
            }
        }

        private void AddConstructorToCandidateSet(MethodSymbol constructor, ArrayBuilder<MemberResolutionResult<MethodSymbol>> results,
            AnalyzedArguments arguments, bool completeResults, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Filter out constructors with unsupported metadata.
            if (constructor.HasUnsupportedMetadata)
            {
                Debug.Assert(!MemberAnalysisResult.UnsupportedMetadata().HasUseSiteDiagnosticToReportFor(constructor));
                if (completeResults)
                {
                    results.Add(new MemberResolutionResult<MethodSymbol>(constructor, constructor, MemberAnalysisResult.UnsupportedMetadata()));
                }
                return;
            }

            var normalResult = IsConstructorApplicableInNormalForm(constructor, arguments, completeResults, ref useSiteDiagnostics);
            var result = normalResult;
            if (!normalResult.IsValid)
            {
                if (IsValidParams(constructor))
                {
                    var expandedResult = IsConstructorApplicableInExpandedForm(constructor, arguments, completeResults, ref useSiteDiagnostics);
                    if (expandedResult.IsValid || completeResults)
                    {
                        result = expandedResult;
                    }
                }
            }

            // If the constructor has a use site diagnostic, we don't want to discard it because we'll have to report the diagnostic later.
            if (result.IsValid || completeResults || result.HasUseSiteDiagnosticToReportFor(constructor))
            {
                results.Add(new MemberResolutionResult<MethodSymbol>(constructor, constructor, result));
            }
        }

        private MemberAnalysisResult IsConstructorApplicableInNormalForm(
            MethodSymbol constructor,
            AnalyzedArguments arguments,
            bool completeResults,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var argumentAnalysis = AnalyzeArguments(constructor, arguments, isMethodGroupConversion: false, expanded: false); // Constructors are never involved in method group conversion.
            if (!argumentAnalysis.IsValid)
            {
                return MemberAnalysisResult.ArgumentParameterMismatch(argumentAnalysis);
            }

            // Check after argument analysis, but before more complicated type inference and argument type validation.
            if (constructor.HasUseSiteError)
            {
                return MemberAnalysisResult.UseSiteError();
            }

            var effectiveParameters = GetEffectiveParametersInNormalForm(
                constructor,
                arguments.Arguments.Count,
                argumentAnalysis.ArgsToParamsOpt,
                arguments.RefKinds,
                isMethodGroupConversion: false,
                allowRefOmittedArguments: false);

            return IsApplicable(
                constructor,
                effectiveParameters,
                arguments,
                argumentAnalysis.ArgsToParamsOpt,
                isVararg: constructor.IsVararg,
                hasAnyRefOmittedArgument: false,
                ignoreOpenTypes: false,
                completeResults: completeResults,
                useSiteDiagnostics: ref useSiteDiagnostics);
        }

        private MemberAnalysisResult IsConstructorApplicableInExpandedForm(
            MethodSymbol constructor,
            AnalyzedArguments arguments,
            bool completeResults,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var argumentAnalysis = AnalyzeArguments(constructor, arguments, isMethodGroupConversion: false, expanded: true);
            if (!argumentAnalysis.IsValid)
            {
                return MemberAnalysisResult.ArgumentParameterMismatch(argumentAnalysis);
            }

            // Check after argument analysis, but before more complicated type inference and argument type validation.
            if (constructor.HasUseSiteError)
            {
                return MemberAnalysisResult.UseSiteError();
            }

            var effectiveParameters = GetEffectiveParametersInExpandedForm(
                constructor,
                arguments.Arguments.Count,
                argumentAnalysis.ArgsToParamsOpt,
                arguments.RefKinds,
                isMethodGroupConversion: false,
                allowRefOmittedArguments: false);

            // A vararg ctor is never applicable in its expanded form because
            // it is never a params method.
            Debug.Assert(!constructor.IsVararg);
            var result = IsApplicable(
                constructor,
                effectiveParameters,
                arguments,
                argumentAnalysis.ArgsToParamsOpt,
                isVararg: false,
                hasAnyRefOmittedArgument: false,
                ignoreOpenTypes: false,
                completeResults: completeResults,
                useSiteDiagnostics: ref useSiteDiagnostics);

            return result.IsValid ? MemberAnalysisResult.ExpandedForm(result.ArgsToParamsOpt, result.ConversionsOpt, hasAnyRefOmittedArgument: false) : result;
        }

        private void AddMemberToCandidateSet<TMember>(
            TMember member, // method or property
            ArrayBuilder<MemberResolutionResult<TMember>> results,
            ArrayBuilder<TMember> members,
            ArrayBuilder<TypeWithAnnotations> typeArguments,
            BoundExpression receiverOpt,
            AnalyzedArguments arguments,
            bool completeResults,
            bool isMethodGroupConversion,
            bool allowRefOmittedArguments,
            Dictionary<NamedTypeSymbol, ArrayBuilder<TMember>> containingTypeMapOpt,
            bool inferWithDynamic,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool allowUnexpandedForm)
            where TMember : Symbol
        {
            // SPEC VIOLATION:
            //
            // The specification states that the method group that resulted from member lookup has
            // already had all the "override" methods removed; according to the spec, only the
            // original declaring type declarations remain. 
            //
            // However, for IDE purposes ("go to definition") we *want* member lookup and overload
            // resolution to identify the overriding method. And the same for the purposes of code
            // generation. (For example, if you have 123.ToString() then we want to make a call to
            // Int32.ToString() directly, passing the int, rather than boxing and calling
            // Object.ToString() on the boxed object.)
            // 
            // Therefore, in member lookup we do *not* eliminate the "override" methods, even though
            // the spec says to. When overload resolution is handed a method group, it contains both
            // the overriding methods and the overridden methods.
            //
            // This is bad; it means that we're going to be doing a lot of extra work. We don't need
            // to analyze every overload of every method to determine if it is applicable; we
            // already know that if one of them is applicable then they all will be. And we don't
            // want to be in a situation where we're comparing two identical methods for which is
            // "better" either.
            //
            // What we'll do here is first eliminate all the "duplicate" overriding methods.
            // However, because we want to give the result as the more derived method, we'll do the
            // opposite of what the member lookup spec says; we'll eliminate the less-derived
            // methods, not the more-derived overrides. This means that we'll have to be a bit more
            // clever in filtering out methods from less-derived classes later, but we'll cross that
            // bridge when we come to it.

            if (members.Count < 2)
            {
                // No hiding or overriding possible.
            }
            else if (containingTypeMapOpt == null)
            {
                if (MemberGroupContainsOverride(members, member))
                {
                    // Don't even add it to the result set.  We'll add only the most-overriding members.
                    return;
                }

                if (MemberGroupHidesByName(members, member, ref useSiteDiagnostics))
                {
                    return;
                }
            }
            else if (containingTypeMapOpt.Count == 1)
            {
                // No hiding or overriding since all members are in the same type.
            }
            else
            {
                // NOTE: only check for overriding/hiding in subtypes of f.ContainingType.
                NamedTypeSymbol memberContainingType = member.ContainingType;
                foreach (var pair in containingTypeMapOpt)
                {
                    NamedTypeSymbol otherType = pair.Key;
                    if (otherType.IsDerivedFrom(memberContainingType, TypeCompareKind.ConsiderEverything, useSiteDiagnostics: ref useSiteDiagnostics))
                    {
                        ArrayBuilder<TMember> others = pair.Value;

                        if (MemberGroupContainsOverride(others, member))
                        {
                            // Don't even add it to the result set.  We'll add only the most-overriding members.
                            return;
                        }

                        if (MemberGroupHidesByName(others, member, ref useSiteDiagnostics))
                        {
                            return;
                        }
                    }
                }
            }

            var leastOverriddenMember = (TMember)member.GetLeastOverriddenMember(_binder.ContainingType);

            // Filter out members with unsupported metadata.
            if (member.HasUnsupportedMetadata)
            {
                Debug.Assert(!MemberAnalysisResult.UnsupportedMetadata().HasUseSiteDiagnosticToReportFor(member));
                if (completeResults)
                {
                    results.Add(new MemberResolutionResult<TMember>(member, leastOverriddenMember, MemberAnalysisResult.UnsupportedMetadata()));
                }
                return;
            }

            // First deal with eliminating generic-arity mismatches.

            // SPEC: If F is generic and M includes a type argument list, F is a candidate when:
            // SPEC: * F has the same number of method type parameters as were supplied in the type argument list, and
            //
            // This is specifying an impossible condition; the member lookup algorithm has already filtered
            // out methods from the method group that have the wrong generic arity.

            Debug.Assert(typeArguments.Count == 0 || typeArguments.Count == member.GetMemberArity());

            // Second, we need to determine if the method is applicable in its normal form or its expanded form.

            var normalResult = (allowUnexpandedForm || !IsValidParams(leastOverriddenMember))
                ? IsMemberApplicableInNormalForm(
                    member,
                    leastOverriddenMember,
                    typeArguments,
                    arguments,
                    isMethodGroupConversion: isMethodGroupConversion,
                    allowRefOmittedArguments: allowRefOmittedArguments,
                    inferWithDynamic: inferWithDynamic,
                    completeResults: completeResults,
                    useSiteDiagnostics: ref useSiteDiagnostics)
                : default(MemberResolutionResult<TMember>);

            var result = normalResult;
            if (!normalResult.Result.IsValid)
            {
                // Whether a virtual method [indexer] is a "params" method [indexer] or not depends solely on how the
                // *original* declaration was declared. There are a variety of C# or MSIL
                // tricks you can pull to make overriding methods [indexers] inconsistent with overridden
                // methods [indexers] (or implementing methods [indexers] inconsistent with interfaces). 

                if (!isMethodGroupConversion && IsValidParams(leastOverriddenMember))
                {
                    var expandedResult = IsMemberApplicableInExpandedForm(
                        member,
                        leastOverriddenMember,
                        typeArguments,
                        arguments,
                        allowRefOmittedArguments: allowRefOmittedArguments,
                        completeResults: completeResults,
                        useSiteDiagnostics: ref useSiteDiagnostics);

                    if (PreferExpandedFormOverNormalForm(normalResult.Result, expandedResult.Result))
                    {
                        result = expandedResult;
                    }
                }
            }

            // Retain candidates with use site diagnostics for later reporting.
            if (result.Result.IsValid || completeResults || result.HasUseSiteDiagnosticToReport)
            {
                results.Add(result);
            }
        }

        // If the normal form is invalid and the expanded form is valid then obviously we prefer
        // the expanded form. However, there may be error-reporting situations where we
        // prefer to report the error on the expanded form rather than the normal form. 
        // For example, if you have something like Goo<T>(params T[]) and a call
        // Goo(1, "") then the error for the normal form is "too many arguments"
        // and the error for the expanded form is "failed to infer T". Clearly the
        // expanded form error is better.
        private static bool PreferExpandedFormOverNormalForm(MemberAnalysisResult normalResult, MemberAnalysisResult expandedResult)
        {
            Debug.Assert(!normalResult.IsValid);
            if (expandedResult.IsValid)
            {
                return true;
            }
            switch (normalResult.Kind)
            {
                case MemberResolutionKind.RequiredParameterMissing:
                case MemberResolutionKind.NoCorrespondingParameter:
                    switch (expandedResult.Kind)
                    {
                        case MemberResolutionKind.BadArgumentConversion:
                        case MemberResolutionKind.NameUsedForPositional:
                        case MemberResolutionKind.TypeInferenceFailed:
                        case MemberResolutionKind.TypeInferenceExtensionInstanceArgument:
                        case MemberResolutionKind.ConstructedParameterFailedConstraintCheck:
                        case MemberResolutionKind.NoCorrespondingNamedParameter:
                        case MemberResolutionKind.UseSiteError:
                        case MemberResolutionKind.BadNonTrailingNamedArgument:
                        case MemberResolutionKind.DuplicateNamedArgument:
                            return true;
                    }
                    break;
            }
            return false;
        }

        // We need to know if this is a valid formal parameter list with a parameter array
        // as the final formal parameter. We might be in an error recovery scenario
        // where the params array is not an array type.
        public static bool IsValidParams(Symbol member)
        {
            // A varargs method is never a valid params method.
            if (member.GetIsVararg())
            {
                return false;
            }

            int paramCount = member.GetParameterCount();
            if (paramCount == 0)
            {
                return false;
            }

            // Note: we need to confirm the "arrayness" on the original definition because
            // it's possible that the type becomes an array as a result of substitution.
            ParameterSymbol final = member.GetParameters().Last();
            return final.IsParams && ((ParameterSymbol)final.OriginalDefinition).Type.IsSZArray();
        }

        private static bool IsOverride(Symbol overridden, Symbol overrider)
        {
            if (TypeSymbol.Equals(overridden.ContainingType, overrider.ContainingType, TypeCompareKind.ConsiderEverything2) ||
                !MemberSignatureComparer.SloppyOverrideComparer.Equals(overridden, overrider))
            {
                // Easy out.
                return false;
            }

            // Does overrider override overridden?
            var current = overrider;
            while (true)
            {
                if (!current.IsOverride)
                {
                    return false;
                }
                current = current.GetOverriddenMember();

                // We could be in error recovery.
                if ((object)current == null)
                {
                    return false;
                }

                if (current == overridden)
                {
                    return true;
                }

                // Don't search beyond the overridden member.
                if (TypeSymbol.Equals(current.ContainingType, overridden.ContainingType, TypeCompareKind.ConsiderEverything2))
                {
                    return false;
                }
            }
        }

        private static bool MemberGroupContainsOverride<TMember>(ArrayBuilder<TMember> members, TMember member)
            where TMember : Symbol
        {
            if (!member.IsVirtual && !member.IsAbstract && !member.IsOverride)
            {
                return false;
            }

            for (var i = 0; i < members.Count; ++i)
            {
                if (IsOverride(member, members[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MemberGroupHidesByName<TMember>(ArrayBuilder<TMember> members, TMember member, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            NamedTypeSymbol memberContainingType = member.ContainingType;
            foreach (var otherMember in members)
            {
                NamedTypeSymbol otherContainingType = otherMember.ContainingType;
                if (HidesByName(otherMember) && otherContainingType.IsDerivedFrom(memberContainingType, TypeCompareKind.ConsiderEverything, useSiteDiagnostics: ref useSiteDiagnostics))
                {
                    return true;
                }
            }

            return false;
        }

        /// <remarks>
        /// This is specifically a private helper function (rather than a public property or extension method)
        /// because applying this predicate to a non-method member doesn't have a clear meaning.  The goal was
        /// simply to avoid repeating ad-hoc code in a group of related collections.
        /// </remarks>
        private static bool HidesByName(Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).HidesBaseMethodsByName;
                case SymbolKind.Property:
                    return ((PropertySymbol)member).HidesBasePropertiesByName;
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        private void RemoveInaccessibleTypeArguments<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            for (int f = 0; f < results.Count; ++f)
            {
                var result = results[f];
                if (result.Result.IsValid && !TypeArgumentsAccessible(result.Member.GetMemberTypeArgumentsNoUseSiteDiagnostics(), ref useSiteDiagnostics))
                {
                    results[f] = new MemberResolutionResult<TMember>(result.Member, result.LeastOverriddenMember, MemberAnalysisResult.InaccessibleTypeArgument());
                }
            }
        }

        private bool TypeArgumentsAccessible(ImmutableArray<TypeSymbol> typeArguments, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            foreach (TypeSymbol arg in typeArguments)
            {
                if (!_binder.IsAccessible(arg, ref useSiteDiagnostics)) return false;
            }
            return true;
        }

        private static void RemoveLessDerivedMembers<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            // 7.6.5.1 Method invocations
            // SPEC: For each method C.F in the set, where C is the type in which the method F is declared, 
            // SPEC: all methods declared in a base type of C are removed from the set. Furthermore, if C 
            // SPEC: is a class type other than object, all methods declared in an interface type are removed
            // SPEC: from the set. (This latter rule only has affect when the method group was the result of 
            // SPEC: a member lookup on a type parameter having an effective base class other than object 
            // SPEC: and a non-empty effective interface set.)

            // This is going to get a bit complicated.
            //
            // Call the "original declaring type" of a method the type which first declares the
            // method, rather than overriding it. 
            //
            // The specification states that the method group that resulted from member lookup has
            // already had all the "override" methods removed; according to the spec, only the
            // original declaring type declarations remain. This means that when we do this
            // filtering, we're not suppose to remove methods of a base class just because there was
            // some override in a more derived class. Whether there is an override or not is an
            // implementation detail of the derived class; it shouldn't affect overload resolution.
            // The point of overload resolution is to determine the *slot* that is going to be
            // invoked, not the specific overriding method body. 
            //
            // However, for IDE purposes ("go to definition") we *want* member lookup and overload
            // resolution to identify the overriding method. And the same for the purposes of code
            // generation. (For example, if you have 123.ToString() then we want to make a call to
            // Int32.ToString() directly, passing the int, rather than boxing and calling
            // Object.ToString() on the boxed object.)
            // 
            // Therefore, in member lookup we do *not* eliminate the "override" methods, even though
            // the spec says to. When overload resolution is handed a method group, it contains both
            // the overriding methods and the overridden methods.  We eliminate the *overridden*
            // methods during applicable candidate set construction.
            //
            // Let's look at an example. Suppose we have in the method group:
            //
            // virtual Animal.M(T1),
            // virtual Mammal.M(T2), 
            // virtual Mammal.M(T3), 
            // override Giraffe.M(T1),
            // override Giraffe.M(T2)
            //
            // According to the spec, the override methods should not even be there. But they are.
            //
            // When we constructed the applicable candidate set we already removed everything that
            // was less-overridden. So the applicable candidate set contains:
            //
            // virtual Mammal.M(T3), 
            // override Giraffe.M(T1),
            // override Giraffe.M(T2)
            //
            // Again, that is not what should be there; what should be there are the three non-
            // overriding methods. For the purposes of removing more stuff, we need to behave as
            // though that's what was there.
            //
            // The presence of Giraffe.M(T2) does *not* justify the removal of Mammal.M(T3); it is
            // not to be considered a method of Giraffe, but rather a method of Mammal for the
            // purposes of removing other methods. 
            //
            // However, the presence of Mammal.M(T3) does justify the removal of Giraffe.M(T1). Why?
            // Because the presence of Mammal.M(T3) justifies the removal of Animal.M(T1), and that
            // is what is supposed to be in the set instead of Giraffe.M(T1).
            //
            // The resulting candidate set after the filtering according to the spec should be:
            //
            // virtual Mammal.M(T3), virtual Mammal.M(T2)
            //
            // But what we actually want to be there is:
            //
            // virtual Mammal.M(T3), override Giraffe.M(T2)
            //
            // So that a "go to definition" (should the latter be chosen as best) goes to the override.
            //
            // OK, so what are we going to do here?
            //
            // First, deal with this business about object and interfaces.

            RemoveAllInterfaceMembers(results);

            // Second, apply the rule that we eliminate any method whose *original declaring type*
            // is a base type of the original declaring type of any other method.

            // Note that this (and several of the other algorithms in overload resolution) is
            // O(n^2). (We expect that n will be relatively small. Also, we're trying to do these
            // algorithms without allocating hardly any additional memory, which pushes us towards
            // walking data structures multiple times rather than caching information about them.)

            for (int f = 0; f < results.Count; ++f)
            {
                var result = results[f];

                // As in dev12, we want to drop use site errors from less-derived types.
                // NOTE: Because of use site warnings, a result with a diagnostic to report
                // might not have kind UseSiteError.  This could result in a kind being
                // switched to LessDerived (i.e. loss of information), but it is the most
                // straightforward way to suppress use site diagnostics from less-derived
                // members.
                if (!(result.Result.IsValid || result.HasUseSiteDiagnosticToReport))
                {
                    continue;
                }

                // Note that we are doing something which appears a bit dodgy here: we're modifying
                // the validity of elements of the set while inside an outer loop which is filtering
                // the set based on validity. This means that we could remove an item from the set
                // that we ought to be processing later. However, because the "is a base type of"
                // relationship is transitive, that's OK. For example, suppose we have members
                // Cat.M, Mammal.M and Animal.M in the set. The first time through the outer loop we
                // eliminate Mammal.M and Animal.M, and therefore we never process Mammal.M the
                // second time through the outer loop. That's OK, because we have already done the
                // work necessary to eliminate methods on base types of Mammal when we eliminated
                // methods on base types of Cat.

                if (IsLessDerivedThanAny(result.LeastOverriddenMember.ContainingType, results, ref useSiteDiagnostics))
                {
                    results[f] = new MemberResolutionResult<TMember>(result.Member, result.LeastOverriddenMember, MemberAnalysisResult.LessDerived());
                }
            }
        }

        // Is this type a base type of any valid method on the list?
        private static bool IsLessDerivedThanAny<TMember>(TypeSymbol type, ArrayBuilder<MemberResolutionResult<TMember>> results, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            for (int f = 0; f < results.Count; ++f)
            {
                var result = results[f];

                if (!result.Result.IsValid)
                {
                    continue;
                }

                var currentType = result.LeastOverriddenMember.ContainingType;

                // For purposes of removing less-derived methods, object is considered to be a base
                // type of any type other than itself.

                // UNDONE: Do we also need to special-case System.Array being a base type of array,
                // and so on?

                if (type.SpecialType == SpecialType.System_Object && currentType.SpecialType != SpecialType.System_Object)
                {
                    return true;
                }

                if (currentType.IsInterfaceType() && type.IsInterfaceType() && currentType.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics).Contains((NamedTypeSymbol)type))
                {
                    return true;
                }
                else if (currentType.IsClassType() && type.IsClassType() && currentType.IsDerivedFrom(type, TypeCompareKind.ConsiderEverything, useSiteDiagnostics: ref useSiteDiagnostics))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveAllInterfaceMembers<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results)
            where TMember : Symbol
        {
            // Consider the following case:
            // 
            // interface IGoo { string ToString(); }
            // class C { public override string ToString() { whatever } }
            // class D : C, IGoo 
            // { 
            //     public override string ToString() { whatever }
            //     string IGoo.ToString() { whatever }
            // }
            // ...
            // void M<U>(U u) where U : C, IGoo { u.ToString(); } // ???
            // ...
            // M(new D());
            //
            // What should overload resolution do on the call to u.ToString()?
            // 
            // We will have IGoo.ToString and C.ToString (which is an override of object.ToString)
            // in the candidate set. Does the rule apply to eliminate all interface methods?  NO.  The
            // rule only applies if the candidate set contains a method which originally came from a
            // class type other than object. The method C.ToString is the "slot" for
            // object.ToString, so this counts as coming from object.  M should call the explicit
            // interface implementation.
            //
            // If, by contrast, that said 
            //
            // class C { public new virtual string ToString() { whatever } }
            //
            // Then the candidate set contains a method ToString which comes from a class type other
            // than object. The interface method should be eliminated and M should call virtual
            // method C.ToString().

            bool anyClassOtherThanObject = false;
            for (int f = 0; f < results.Count; f++)
            {
                var result = results[f];
                if (!result.Result.IsValid)
                {
                    continue;
                }

                var type = result.LeastOverriddenMember.ContainingType;
                if (type.IsClassType() && type.GetSpecialTypeSafe() != SpecialType.System_Object)
                {
                    anyClassOtherThanObject = true;
                    break;
                }
            }

            if (!anyClassOtherThanObject)
            {
                return;
            }

            for (int f = 0; f < results.Count; f++)
            {
                var result = results[f];
                if (!result.Result.IsValid)
                {
                    continue;
                }

                var member = result.Member;
                if (member.ContainingType.IsInterfaceType())
                {
                    results[f] = new MemberResolutionResult<TMember>(member, result.LeastOverriddenMember, MemberAnalysisResult.LessDerived());
                }
            }
        }

        // Perform instance constructor overload resolution, storing the results into "results". If
        // completeResults is false, then invalid results don't have to be stored. The results will
        // still contain all possible successful resolution.
        private void PerformObjectCreationOverloadResolution(
            ArrayBuilder<MemberResolutionResult<MethodSymbol>> results,
            ImmutableArray<MethodSymbol> constructors,
            AnalyzedArguments arguments,
            bool completeResults,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // SPEC: The instance constructor to invoke is determined using the overload resolution 
            // SPEC: rules of 7.5.3. The set of candidate instance constructors consists of all 
            // SPEC: accessible instance constructors declared in T which are applicable with respect 
            // SPEC: to A (7.5.3.1). If the set of candidate instance constructors is empty, or if a 
            // SPEC: single best instance constructor cannot be identified, a binding-time error occurs.

            foreach (MethodSymbol constructor in constructors)
            {
                AddConstructorToCandidateSet(constructor, results, arguments, completeResults, ref useSiteDiagnostics);
            }

            ReportUseSiteDiagnostics(results, ref useSiteDiagnostics);

            // The best method of the set of candidate methods is identified. If a single best
            // method cannot be identified, the method invocation is ambiguous, and a binding-time
            // error occurs. 
            RemoveWorseMembers(results, arguments, ref useSiteDiagnostics);

            return;
        }

        private static void ReportUseSiteDiagnostics<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            foreach (MemberResolutionResult<TMember> result in results)
            {
                if (result.HasUseSiteDiagnosticToReport)
                {
                    useSiteDiagnostics = useSiteDiagnostics ?? new HashSet<DiagnosticInfo>();
                    useSiteDiagnostics.Add(result.Member.GetUseSiteDiagnostic());
                }
            }
        }

        private int GetTheBestCandidateIndex<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results, AnalyzedArguments arguments, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            int currentBestIndex = -1;
            for (int index = 0; index < results.Count; index++)
            {
                if (!results[index].IsValid)
                {
                    continue;
                }

                // Assume that the current candidate is the best if we don't have any
                if (currentBestIndex == -1)
                {
                    currentBestIndex = index;
                }
                else if (results[currentBestIndex].Member == results[index].Member)
                {
                    currentBestIndex = -1;
                }
                else
                {
                    var better = BetterFunctionMember(results[currentBestIndex], results[index], arguments.Arguments, ref useSiteDiagnostics);
                    if (better == BetterResult.Right)
                    {
                        // The current best is worse
                        currentBestIndex = index;
                    }
                    else if (better != BetterResult.Left)
                    {
                        // The current best is not better
                        currentBestIndex = -1;
                    }
                }
            }

            // Make sure that every candidate up to the current best is worse
            for (int index = 0; index < currentBestIndex; index++)
            {
                if (!results[index].IsValid)
                {
                    continue;
                }

                if (results[currentBestIndex].Member == results[index].Member)
                {
                    return -1;
                }

                var better = BetterFunctionMember(results[currentBestIndex], results[index], arguments.Arguments, ref useSiteDiagnostics);
                if (better != BetterResult.Left)
                {
                    // The current best is not better
                    return -1;
                }
            }

            return currentBestIndex;
        }

        private void RemoveWorseMembers<TMember>(ArrayBuilder<MemberResolutionResult<TMember>> results, AnalyzedArguments arguments, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            // SPEC: Given the set of applicable candidate function members, the best function member in
            // SPEC: that set is located. Otherwise, the best function member is the one function member
            // SPEC: that is better than all other function members with respect to the given argument
            // SPEC: list. 

            // Note that the above rules require that the best member be *better* than all other 
            // applicable candidates. Consider three overloads such that:
            //
            // 3 beats 2
            // 2 beats 1
            // 3 is neither better than nor worse than 1
            //
            // It is tempting to say that overload 3 is the winner because it is the one method
            // that beats something, and is beaten by nothing. But that would be incorrect;
            // method 3 needs to beat all other methods, including method 1.
            //
            // We work up a full analysis of every member of the set. If it is worse than anything
            // then we need to do no more work; we know it cannot win. But it is also possible that
            // it is not worse than anything but not better than everything. 

            if (SingleValidResult(results))
            {
                return;
            }

            // See if we have a winner, otherwise we might need to perform additional analysis
            // in order to improve diagnostics
            int bestIndex = GetTheBestCandidateIndex(results, arguments, ref useSiteDiagnostics);
            if (bestIndex != -1)
            {
                // Mark all other candidates as worse
                for (int index = 0; index < results.Count; index++)
                {
                    if (results[index].IsValid && index != bestIndex)
                    {
                        results[index] = results[index].Worse();
                    }
                }

                return;
            }

            const int unknown = 0;
            const int worseThanSomething = 1;
            const int notBetterThanEverything = 2;

            var worse = ArrayBuilder<int>.GetInstance(results.Count, unknown);

            int countOfNotBestCandidates = 0;
            int notBestIdx = -1;

            for (int c1Idx = 0; c1Idx < results.Count; c1Idx++)
            {
                var c1Result = results[c1Idx];

                // If we already know this is worse than something else, no need to check again.
                if (!c1Result.IsValid || worse[c1Idx] == worseThanSomething)
                {
                    continue;
                }

                for (int c2Idx = 0; c2Idx < results.Count; c2Idx++)
                {
                    var c2Result = results[c2Idx];
                    if (!c2Result.IsValid || c1Idx == c2Idx || c1Result.Member == c2Result.Member)
                    {
                        continue;
                    }

                    var better = BetterFunctionMember(c1Result, c2Result, arguments.Arguments, ref useSiteDiagnostics);
                    if (better == BetterResult.Left)
                    {
                        worse[c2Idx] = worseThanSomething;
                    }
                    else if (better == BetterResult.Right)
                    {
                        worse[c1Idx] = worseThanSomething;
                        break;
                    }
                }

                if (worse[c1Idx] == unknown)
                {
                    // c1 was not worse than anything
                    worse[c1Idx] = notBetterThanEverything;
                    countOfNotBestCandidates++;
                    notBestIdx = c1Idx;
                }
            }

            if (countOfNotBestCandidates == 0)
            {
                for (int i = 0; i < worse.Count; ++i)
                {
                    Debug.Assert(!results[i].IsValid || worse[i] != unknown);
                    if (worse[i] == worseThanSomething)
                    {
                        results[i] = results[i].Worse();
                    }
                }
            }
            else if (countOfNotBestCandidates == 1)
            {
                for (int i = 0; i < worse.Count; ++i)
                {
                    Debug.Assert(!results[i].IsValid || worse[i] != unknown);
                    if (worse[i] == worseThanSomething)
                    {
                        // Mark those candidates, that are worse than the single notBest candidate, as Worst in order to improve error reporting.
                        results[i] = BetterResult.Left == BetterFunctionMember(results[notBestIdx], results[i], arguments.Arguments, ref useSiteDiagnostics)
                            ? results[i].Worst() : results[i].Worse();
                    }
                    else
                    {
                        Debug.Assert(worse[i] != notBetterThanEverything || i == notBestIdx);
                    }
                }

                Debug.Assert(worse[notBestIdx] == notBetterThanEverything);
                results[notBestIdx] = results[notBestIdx].Worse();
            }
            else
            {
                Debug.Assert(countOfNotBestCandidates > 1);

                for (int i = 0; i < worse.Count; ++i)
                {
                    Debug.Assert(!results[i].IsValid || worse[i] != unknown);
                    if (worse[i] == worseThanSomething)
                    {
                        // Mark those candidates, that are worse than something, as Worst in order to improve error reporting.
                        results[i] = results[i].Worst();
                    }
                    else if (worse[i] == notBetterThanEverything)
                    {
                        results[i] = results[i].Worse();
                    }
                }
            }

            worse.Free();
        }

        // Merge upstream/dev15.6.x
#if false
        // Return the parameter type corresponding to the given argument index.
        private TypeSymbol GetParameterType(int argIndex, MemberAnalysisResult result, ImmutableArray<ParameterSymbol> parameters)
        {
            RefKind discarded;
            return GetParameterType(argIndex, result, parameters, out discarded);
        }

        // Return the parameter type corresponding to the given argument index.
        private TypeSymbol GetParameterType(int argIndex, MemberAnalysisResult result, ImmutableArray<ParameterSymbol> parameters, out RefKind refKind)
        {
            int paramIndex = result.ParameterFromArgument(argIndex);
            ParameterSymbol parameter = parameters[paramIndex];
            refKind = parameter.RefKind;
            var type = _binder.GetTypeOrReturnTypeWithAdjustedNullableAnnotations(parameter).TypeSymbol;

#endif
        /// <summary>
        /// Returns the parameter type (considering params).
        /// </summary>
        private TypeSymbol GetParameterType(ParameterSymbol parameter, MemberAnalysisResult result)
        {
            var type = parameter.Type;
            if (result.Kind == MemberResolutionKind.ApplicableInExpandedForm &&
                parameter.IsParams && type.IsSZArray())
            {
                return ((ArrayTypeSymbol)type).ElementType;
            }
            else
            {
                return type;
            }
        }

        /// <summary>
        /// Returns the parameter corresponding to the given argument index.
        /// </summary>
        private static ParameterSymbol GetParameter(int argIndex, MemberAnalysisResult result, ImmutableArray<ParameterSymbol> parameters)
        {
            int paramIndex = result.ParameterFromArgument(argIndex);
            return parameters[paramIndex];
        }

        private BetterResult BetterFunctionMember<TMember>(
            MemberResolutionResult<TMember> m1,
            MemberResolutionResult<TMember> m2,
            ArrayBuilder<BoundExpression> arguments,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            Debug.Assert(m1.Result.IsValid);
            Debug.Assert(m2.Result.IsValid);
            Debug.Assert(arguments != null);

            // Omit ref feature for COM interop: We can pass arguments by value for ref parameters if we are calling a method/property on an instance of a COM imported type.
            // We should have ignored the 'ref' on the parameter while determining the applicability of argument for the given method call.
            // As per Devdiv Bug #696573: '[Interop] Com omit ref overload resolution is incorrect', we must prefer non-ref omitted methods over ref omitted methods
            // when determining the BetterFunctionMember.
            // During argument rewriting, we will replace the argument value with a temporary local and pass that local by reference.

            bool hasAnyRefOmittedArgument1 = m1.Result.HasAnyRefOmittedArgument;
            bool hasAnyRefOmittedArgument2 = m2.Result.HasAnyRefOmittedArgument;
            if (hasAnyRefOmittedArgument1 != hasAnyRefOmittedArgument2)
            {
                return hasAnyRefOmittedArgument1 ? BetterResult.Right : BetterResult.Left;
            }
            else
            {
                return BetterFunctionMember(m1, m2, arguments, considerRefKinds: hasAnyRefOmittedArgument1, useSiteDiagnostics: ref useSiteDiagnostics);
            }
        }

        private BetterResult BetterFunctionMember<TMember>(
            MemberResolutionResult<TMember> m1,
            MemberResolutionResult<TMember> m2,
            ArrayBuilder<BoundExpression> arguments,
            bool considerRefKinds,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            Debug.Assert(m1.Result.IsValid);
            Debug.Assert(m2.Result.IsValid);
            Debug.Assert(arguments != null);

            // SPEC:
            //   Parameter lists for each of the candidate function members are constructed in the following way: 
            //   The expanded form is used if the function member was applicable only in the expanded form.
            //   Optional parameters with no corresponding arguments are removed from the parameter list
            //   The parameters are reordered so that they occur at the same position as the corresponding argument in the argument list.
            // We don't actually create these lists, for efficiency reason. But we iterate over the arguments
            // and get the correspond parameter types.

            BetterResult result = BetterResult.Neither;
            bool okToDowngradeResultToNeither = false;
            bool ignoreDowngradableToNeither = false;

            // Given an argument list A with a set of argument expressions { E1, E2, ..., EN } and two 
            // applicable function members MP and MQ with parameter types { P1, P2, ..., PN } and { Q1, Q2, ..., QN }, 
            // MP is defined to be a better function member than MQ if

            // for each argument, the implicit conversion from EX to QX is not better than the
            // implicit conversion from EX to PX, and for at least one argument, the conversion from
            // EX to PX is better than the conversion from EX to QX.

            var m1LeastOverridenParameters = m1.LeastOverriddenMember.GetParameters();
            var m2LeastOverridenParameters = m2.LeastOverriddenMember.GetParameters();

            bool allSame = true; // Are all parameter types equivalent by identify conversions, ignoring Task-like differences?
            int i;
            for (i = 0; i < arguments.Count; ++i)
            {
                var argumentKind = arguments[i].Kind;

                // If these are both applicable varargs methods and we're looking at the __arglist argument
                // then clearly neither of them is going to be better in this argument.
                if (argumentKind == BoundKind.ArgListOperator)
                {
                    Debug.Assert(i == arguments.Count - 1);
                    Debug.Assert(m1.Member.GetIsVararg() && m2.Member.GetIsVararg());
                    continue;
                }

                var parameter1 = GetParameter(i, m1.Result, m1LeastOverridenParameters);
                var type1 = GetParameterType(parameter1, m1.Result);

                var parameter2 = GetParameter(i, m2.Result, m2LeastOverridenParameters);
                var type2 = GetParameterType(parameter2, m2.Result);

                bool okToDowngradeToNeither;
                BetterResult r;

                r = BetterConversionFromExpression(arguments[i],
                                                   type1,
                                                   m1.Result.ConversionForArg(i),
                                                   parameter1.RefKind,
                                                   type2,
                                                   m2.Result.ConversionForArg(i),
                                                   parameter2.RefKind,
                                                   considerRefKinds,
                                                   ref useSiteDiagnostics,
                                                   out okToDowngradeToNeither);

                var type1Normalized = type1;
                var type2Normalized = type2;

                // Normalizing task types can cause attributes to be bound on the type,
                // and attribute arguments may call overloaded methods in error cases.
                // To avoid a stack overflow, we must not normalize task types within attribute arguments.
                if (!_binder.InAttributeArgument)
                {
                    type1Normalized = type1.NormalizeTaskTypes(Compilation);
                    type2Normalized = type2.NormalizeTaskTypes(Compilation);
                }

                if (r == BetterResult.Neither)
                {
                    if (allSame && Conversions.ClassifyImplicitConversionFromType(type1Normalized, type2Normalized, ref useSiteDiagnostics).Kind != ConversionKind.Identity)
                    {
                        allSame = false;
                    }

                    // We learned nothing from this one. Keep going.
                    continue;
                }

                if (Conversions.ClassifyImplicitConversionFromType(type1Normalized, type2Normalized, ref useSiteDiagnostics).Kind != ConversionKind.Identity)
                {
                    allSame = false;
                }

                // One of them was better, even if identical up to Task-likeness. Does that contradict a previous result or add a new fact?
                if (result == BetterResult.Neither)
                {
                    if (!(ignoreDowngradableToNeither && okToDowngradeToNeither))
                    {
                        // Add a new fact; we know that one of them is better when we didn't know that before.
                        result = r;
                        okToDowngradeResultToNeither = okToDowngradeToNeither;
                    }
                }
                else if (result != r)
                {
                    // We previously got, say, Left is better in one place. Now we have that Right
                    // is better in one place. We know we can bail out at this point; neither is
                    // going to be better than the other.

                    // But first, let's see if we can ignore the ambiguity due to an undocumented legacy behavior of the compiler.
                    // This is not part of the language spec.
                    if (okToDowngradeResultToNeither)
                    {
                        if (okToDowngradeToNeither)
                        {
                            // Ignore the new information and the current result. Going forward,
                            // continue ignoring any downgradable information.
                            result = BetterResult.Neither;
                            okToDowngradeResultToNeither = false;
                            ignoreDowngradableToNeither = true;
                            continue;
                        }
                        else
                        {
                            // Current result can be ignored, but the new information cannot be ignored.
                            // Let's ignore the current result.
                            result = r;
                            okToDowngradeResultToNeither = false;
                            continue;
                        }
                    }
                    else if (okToDowngradeToNeither)
                    {
                        // Current result cannot be ignored, but the new information can be ignored.
                        // Let's ignore it and continue with the current result.
                        continue;
                    }

                    result = BetterResult.Neither;
                    break;
                }
                else
                {
                    Debug.Assert(result == r);
                    Debug.Assert(result == BetterResult.Left || result == BetterResult.Right);

                    okToDowngradeResultToNeither = (okToDowngradeResultToNeither && okToDowngradeToNeither);
                }
            }

            // Was one unambiguously better? Return it.
            if (result != BetterResult.Neither)
            {
                return result;
            }

            // In case the parameter type sequences {P1, P2, …, PN} and {Q1, Q2, …, QN} are
            // equivalent ignoring Task-like differences (i.e. each Pi has an identity conversion to the corresponding Qi), the
            // following tie-breaking rules are applied, in order, to determine the better function
            // member. 

            int m1ParameterCount;
            int m2ParameterCount;
            int m1ParametersUsedIncludingExpansionAndOptional;
            int m2ParametersUsedIncludingExpansionAndOptional;

            GetParameterCounts(m1, arguments, out m1ParameterCount, out m1ParametersUsedIncludingExpansionAndOptional);
            GetParameterCounts(m2, arguments, out m2ParameterCount, out m2ParametersUsedIncludingExpansionAndOptional);

            // We might have got out of the loop above early and allSame isn't completely calculated.
            // We need to ensure that we are not going to skip over the next 'if' because of that.
            // One way we can break out of the above loop early is when the corresponding method parameters have identical types
            // but different ref kinds. See RefOmittedComCall_OverloadResolution_MultipleArguments_ErrorCases for an example.
            if (allSame && m1ParametersUsedIncludingExpansionAndOptional == m2ParametersUsedIncludingExpansionAndOptional)
            {
                // Complete comparison for the remaining parameter types
                for (i = i + 1; i < arguments.Count; ++i)
                {
                    var argumentKind = arguments[i].Kind;

                    // If these are both applicable varargs methods and we're looking at the __arglist argument
                    // then clearly neither of them is going to be better in this argument.
                    if (argumentKind == BoundKind.ArgListOperator)
                    {
                        Debug.Assert(i == arguments.Count - 1);
                        Debug.Assert(m1.Member.GetIsVararg() && m2.Member.GetIsVararg());
                        continue;
                    }

                    var parameter1 = GetParameter(i, m1.Result, m1LeastOverridenParameters);
                    var type1 = GetParameterType(parameter1, m1.Result);

                    var parameter2 = GetParameter(i, m2.Result, m2LeastOverridenParameters);
                    var type2 = GetParameterType(parameter2, m2.Result);

                    var type1Normalized = type1;
                    var type2Normalized = type2;
                    if (!_binder.InAttributeArgument)
                    {
                        type1Normalized = type1.NormalizeTaskTypes(Compilation);
                        type2Normalized = type2.NormalizeTaskTypes(Compilation);
                    }

                    if (Conversions.ClassifyImplicitConversionFromType(type1Normalized, type2Normalized, ref useSiteDiagnostics).Kind != ConversionKind.Identity)
                    {
                        allSame = false;
                        break;
                    }
                }
            }

            // SPEC VIOLATION: When checking for matching parameter type sequences {P1, P2, …, PN} and {Q1, Q2, …, QN},
            //                 native compiler includes types of optional parameters. We partially duplicate this behavior
            //                 here by comparing the number of parameters used taking params expansion and 
            //                 optional parameters into account.
            if (!allSame || m1ParametersUsedIncludingExpansionAndOptional != m2ParametersUsedIncludingExpansionAndOptional)
            {
                // SPEC VIOLATION: Even when parameter type sequences {P1, P2, …, PN} and {Q1, Q2, …, QN} are
                //                 not equivalent, we have tie-breaking rules.
                //
                // Relevant code in the native compiler is at the end of
                //                       BetterTypeEnum ExpressionBinder::WhichMethodIsBetter(
                //                                           const CandidateFunctionMember &node1,
                //                                           const CandidateFunctionMember &node2,
                //                                           Type* pTypeThrough,
                //                                           ArgInfos*args)
                //

                if (m1ParametersUsedIncludingExpansionAndOptional != m2ParametersUsedIncludingExpansionAndOptional)
                {
                    if (m1.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm)
                    {
                        if (m2.Result.Kind != MemberResolutionKind.ApplicableInExpandedForm)
                        {
                            return BetterResult.Right;
                        }
                    }
                    else if (m2.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm)
                    {
                        Debug.Assert(m1.Result.Kind != MemberResolutionKind.ApplicableInExpandedForm);
                        return BetterResult.Left;
                    }

                    // Here, if both methods needed to use optionals to fill in the signatures,
                    // then we are ambiguous. Otherwise, take the one that didn't need any 
                    // optionals.

                    if (m1ParametersUsedIncludingExpansionAndOptional == arguments.Count)
                    {
                        return BetterResult.Left;
                    }
                    else if (m2ParametersUsedIncludingExpansionAndOptional == arguments.Count)
                    {
                        return BetterResult.Right;
                    }
                }

                return PreferValOverInParameters(arguments, m1, m1LeastOverridenParameters, m2, m2LeastOverridenParameters);
            }

            // If MP is a non-generic method and MQ is a generic method, then MP is better than MQ.
            if (m1.Member.GetMemberArity() == 0)
            {
                if (m2.Member.GetMemberArity() > 0)
                {
                    return BetterResult.Left;
                }
            }
            else if (m2.Member.GetMemberArity() == 0)
            {
                return BetterResult.Right;
            }

            // Otherwise, if MP is applicable in its normal form and MQ has a params array and is
            // applicable only in its expanded form, then MP is better than MQ.

            if (m1.Result.Kind == MemberResolutionKind.ApplicableInNormalForm && m2.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm)
            {
                return BetterResult.Left;
            }

            if (m1.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm && m2.Result.Kind == MemberResolutionKind.ApplicableInNormalForm)
            {
                return BetterResult.Right;
            }

            // SPEC ERROR: The spec has a minor error in working here. It says:
            //
            // Otherwise, if MP has more declared parameters than MQ, then MP is better than MQ. 
            // This can occur if both methods have params arrays and are applicable only in their
            // expanded forms.
            //
            // The explanatory text actually should be normative. It should say:
            //
            // Otherwise, if both methods have params arrays and are applicable only in their
            // expanded forms, and if MP has more declared parameters than MQ, then MP is better than MQ. 

            if (m1.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm && m2.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm)
            {
                if (m1ParameterCount > m2ParameterCount)
                {
                    return BetterResult.Left;
                }

                if (m1ParameterCount < m2ParameterCount)
                {
                    return BetterResult.Right;
                }
            }

            // Otherwise if all parameters of MP have a corresponding argument whereas default
            // arguments need to be substituted for at least one optional parameter in MQ then MP is
            // better than MQ. 

            bool hasAll1 = m1.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm || m1ParameterCount == arguments.Count;
            bool hasAll2 = m2.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm || m2ParameterCount == arguments.Count;
            if (hasAll1 && !hasAll2)
            {
                return BetterResult.Left;
            }

            if (!hasAll1 && hasAll2)
            {
                return BetterResult.Right;
            }

            // Otherwise, if MP has more specific parameter types than MQ, then MP is better than
            // MQ. Let {R1, R2, …, RN} and {S1, S2, …, SN} represent the uninstantiated and
            // unexpanded parameter types of MP and MQ. MP's parameter types are more specific than
            // MQ's if, for each parameter, RX is not less specific than SX, and, for at least one
            // parameter, RX is more specific than SX

            // NB: OriginalDefinition, not ConstructedFrom.  Substitutions into containing symbols
            // must also be ignored for this tie-breaker.

            var uninst1 = ArrayBuilder<TypeSymbol>.GetInstance();
            var uninst2 = ArrayBuilder<TypeSymbol>.GetInstance();
            var m1Original = m1.LeastOverriddenMember.OriginalDefinition.GetParameters();
            var m2Original = m2.LeastOverriddenMember.OriginalDefinition.GetParameters();
            for (i = 0; i < arguments.Count; ++i)
            {
                // If these are both applicable varargs methods and we're looking at the __arglist argument
                // then clearly neither of them is going to be better in this argument.
                if (arguments[i].Kind == BoundKind.ArgListOperator)
                {
                    Debug.Assert(i == arguments.Count - 1);
                    Debug.Assert(m1.Member.GetIsVararg() && m2.Member.GetIsVararg());
                    continue;
                }

                var parameter1 = GetParameter(i, m1.Result, m1Original);
                uninst1.Add(GetParameterType(parameter1, m1.Result));

                var parameter2 = GetParameter(i, m2.Result, m2Original);
                uninst2.Add(GetParameterType(parameter2, m2.Result));
            }

            result = MoreSpecificType(uninst1, uninst2, ref useSiteDiagnostics);
            uninst1.Free();
            uninst2.Free();

            if (result != BetterResult.Neither)
            {
                return result;
            }

            // UNDONE: Otherwise if one member is a non-lifted operator and  the other is a lifted
            // operator, the non-lifted one is better.

            // Otherwise: Position in interactive submission chain. The last definition wins.
            if (m1.Member.ContainingType.TypeKind == TypeKind.Submission && m2.Member.ContainingType.TypeKind == TypeKind.Submission)
            {
                // script class is always defined in source:
                var compilation1 = m1.Member.DeclaringCompilation;
                var compilation2 = m2.Member.DeclaringCompilation;
                int submissionId1 = compilation1.GetSubmissionSlotIndex();
                int submissionId2 = compilation2.GetSubmissionSlotIndex();

                if (submissionId1 > submissionId2)
                {
                    return BetterResult.Left;
                }

                if (submissionId1 < submissionId2)
                {
                    return BetterResult.Right;
                }
            }

            // Otherwise, if one has fewer custom modifiers, that is better
            int m1ModifierCount = m1.LeastOverriddenMember.CustomModifierCount();
            int m2ModifierCount = m2.LeastOverriddenMember.CustomModifierCount();
            if (m1ModifierCount != m2ModifierCount)
            {
                return (m1ModifierCount < m2ModifierCount) ? BetterResult.Left : BetterResult.Right;
            }

            // Otherwise, prefer methods with 'val' parameters over 'in' parameters.
            return PreferValOverInParameters(arguments, m1, m1LeastOverridenParameters, m2, m2LeastOverridenParameters);
        }

        private static BetterResult PreferValOverInParameters<TMember>(
            ArrayBuilder<BoundExpression> arguments,
            MemberResolutionResult<TMember> m1,
            ImmutableArray<ParameterSymbol> parameters1,
            MemberResolutionResult<TMember> m2,
            ImmutableArray<ParameterSymbol> parameters2)
            where TMember : Symbol
        {
            BetterResult valOverInPreference = BetterResult.Neither;

            for (int i = 0; i < arguments.Count; ++i)
            {
                if (arguments[i].Kind != BoundKind.ArgListOperator)
                {
                    var p1 = GetParameter(i, m1.Result, parameters1);
                    var p2 = GetParameter(i, m2.Result, parameters2);

                    if (p1.RefKind == RefKind.None && p2.RefKind == RefKind.In)
                    {
                        if (valOverInPreference == BetterResult.Right)
                        {
                            return BetterResult.Neither;
                        }
                        else
                        {
                            valOverInPreference = BetterResult.Left;
                        }
                    }
                    else if (p2.RefKind == RefKind.None && p1.RefKind == RefKind.In)
                    {
                        if (valOverInPreference == BetterResult.Left)
                        {
                            return BetterResult.Neither;
                        }
                        else
                        {
                            valOverInPreference = BetterResult.Right;
                        }
                    }
                }
            }

            return valOverInPreference;
        }

        private static void GetParameterCounts<TMember>(MemberResolutionResult<TMember> m, ArrayBuilder<BoundExpression> arguments, out int declaredParameterCount, out int parametersUsedIncludingExpansionAndOptional) where TMember : Symbol
        {
            declaredParameterCount = m.Member.GetParameterCount();

            if (m.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm)
            {
                if (arguments.Count < declaredParameterCount)
                {
                    ImmutableArray<int> argsToParamsOpt = m.Result.ArgsToParamsOpt;

                    if (argsToParamsOpt.IsDefaultOrEmpty || !argsToParamsOpt.Contains(declaredParameterCount - 1))
                    {
                        // params parameter isn't used (see ExpressionBinder::TryGetExpandedParams in the native compiler)
                        parametersUsedIncludingExpansionAndOptional = declaredParameterCount - 1;
                    }
                    else
                    {
                        // params parameter is used by a named argument
                        parametersUsedIncludingExpansionAndOptional = declaredParameterCount;
                    }
                }
                else
                {
                    parametersUsedIncludingExpansionAndOptional = arguments.Count;
                }
            }
            else
            {
                parametersUsedIncludingExpansionAndOptional = declaredParameterCount;
            }
        }

        private static BetterResult MoreSpecificType(ArrayBuilder<TypeSymbol> t1, ArrayBuilder<TypeSymbol> t2, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(t1.Count == t2.Count);

            // For t1 to be more specific than t2, it has to be not less specific in every member,
            // and more specific in at least one.

            var result = BetterResult.Neither;
            for (int i = 0; i < t1.Count; ++i)
            {
                var r = MoreSpecificType(t1[i], t2[i], ref useSiteDiagnostics);
                if (r == BetterResult.Neither)
                {
                    // We learned nothing. Do nothing.
                }
                else if (result == BetterResult.Neither)
                {
                    // We have found the first more specific type. See if
                    // all the rest on this side are not less specific.
                    result = r;
                }
                else if (result != r)
                {
                    // We have more specific types on both left and right, so we 
                    // cannot succeed in picking a better type list. Bail out now.
                    return BetterResult.Neither;
                }
            }

            return result;
        }

        private static BetterResult MoreSpecificType(TypeSymbol t1, TypeSymbol t2, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Spec 7.5.3.2:
            // - A type parameter is less specific than a non-type parameter. 

            var t1IsTypeParameter = t1.IsTypeParameter();
            var t2IsTypeParameter = t2.IsTypeParameter();

            if (t1IsTypeParameter && !t2IsTypeParameter)
            {
                return BetterResult.Right;
            }

            if (!t1IsTypeParameter && t2IsTypeParameter)
            {
                return BetterResult.Left;
            }

            if (t1IsTypeParameter && t2IsTypeParameter)
            {
                return BetterResult.Neither;
            }

            // Spec:
            // - An array type is more specific than another array type (with the same number of dimensions) 
            //   if the element type of the first is more specific than the element type of the second.

            if (t1.IsArray())
            {
                var arr1 = (ArrayTypeSymbol)t1;
                var arr2 = (ArrayTypeSymbol)t2;

                // We should not have gotten here unless there were identity conversions
                // between the two types.
                Debug.Assert(arr1.HasSameShapeAs(arr2));

                return MoreSpecificType(arr1.ElementType, arr2.ElementType, ref useSiteDiagnostics);
            }

            // SPEC EXTENSION: We apply the same rule to pointer types. 

            if (t1.TypeKind == TypeKind.Pointer)
            {
                var p1 = (PointerTypeSymbol)t1;
                var p2 = (PointerTypeSymbol)t2;
                return MoreSpecificType(p1.PointedAtType, p2.PointedAtType, ref useSiteDiagnostics);
            }

            if (t1.IsDynamic() || t2.IsDynamic())
            {
                Debug.Assert(t1.IsDynamic() && t2.IsDynamic() ||
                             t1.IsDynamic() && t2.SpecialType == SpecialType.System_Object ||
                             t2.IsDynamic() && t1.SpecialType == SpecialType.System_Object);

                return BetterResult.Neither;
            }

            // Spec:
            // - A constructed type is more specific than another
            //   constructed type (with the same number of type arguments) if at least one type
            //   argument is more specific and no type argument is less specific than the
            //   corresponding type argument in the other. 

            var n1 = t1.TupleUnderlyingTypeOrSelf() as NamedTypeSymbol;
            var n2 = t2.TupleUnderlyingTypeOrSelf() as NamedTypeSymbol;
            Debug.Assert(((object)n1 == null) == ((object)n2 == null));

            if ((object)n1 == null)
            {
                return BetterResult.Neither;
            }

            // We should not have gotten here unless there were identity conversions between the
            // two types, or they are different Task-likes. Ideally we'd assert that the two types (or
            // Task equivalents) have the same OriginalDefinition but we don't have a Compilation
            // here for NormalizeTaskTypes.

            var allTypeArgs1 = ArrayBuilder<TypeSymbol>.GetInstance();
            var allTypeArgs2 = ArrayBuilder<TypeSymbol>.GetInstance();
            n1.GetAllTypeArguments(allTypeArgs1, ref useSiteDiagnostics);
            n2.GetAllTypeArguments(allTypeArgs2, ref useSiteDiagnostics);

            var result = MoreSpecificType(allTypeArgs1, allTypeArgs2, ref useSiteDiagnostics);

            allTypeArgs1.Free();
            allTypeArgs2.Free();
            return result;
        }

        // Determine whether t1 or t2 is a better conversion target from node.
        private BetterResult BetterConversionFromExpression(BoundExpression node, TypeSymbol t1, TypeSymbol t2, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(node.Kind != BoundKind.UnboundLambda);
            bool ignore;
            return BetterConversionFromExpression(
                node,
                t1,
                Conversions.ClassifyImplicitConversionFromExpression(node, t1, ref useSiteDiagnostics),
                t2,
                Conversions.ClassifyImplicitConversionFromExpression(node, t2, ref useSiteDiagnostics),
                ref useSiteDiagnostics,
                out ignore);
        }

        // Determine whether t1 or t2 is a better conversion target from node, possibly considering parameter ref kinds.
        private BetterResult BetterConversionFromExpression(
            BoundExpression node,
            TypeSymbol t1,
            Conversion conv1,
            RefKind refKind1,
            TypeSymbol t2,
            Conversion conv2,
            RefKind refKind2,
            bool considerRefKinds,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            out bool okToDowngradeToNeither)
        {
            okToDowngradeToNeither = false;

            if (considerRefKinds)
            {
                // We may need to consider the ref kinds of the parameters while determining the better conversion from the given expression to the respective parameter types.
                // This is needed for the omit ref feature for COM interop: We can pass arguments by value for ref parameters if we are calling a method within a COM imported type.
                // We can reach here only if we had at least one ref omitted argument for the given call, which must be a call to a method within a COM imported type.

                // Algorithm for determining the better conversion from expression when ref kinds need to be considered is NOT provided in the C# language specification,
                // see section 7.5.3.3 'Better Conversion From Expression'.
                // We match native compiler's behavior for determining the better conversion as follows:
                //  1) If one of the contending parameters is a 'ref' parameter, say p1, and other is a non-ref parameter, say p2,
                //     then p2 is a better result if the argument has an identity conversion to p2's type. Otherwise, neither result is better.
                //  2) Otherwise, if both the contending parameters are 'ref' parameters, neither result is better.
                //  3) Otherwise, we use the algorithm in 7.5.3.3 for determining the better conversion without considering ref kinds.

                // NOTE:    Native compiler does not explicitly implement the above algorithm, but gets it by default. This is due to the fact that the RefKind of a parameter
                // NOTE:    gets considered while classifying conversions between parameter types when computing better conversion target in the native compiler.
                // NOTE:    Roslyn correctly follows the specification and ref kinds are not considered while classifying conversions between types, see method BetterConversionTarget.

                Debug.Assert(refKind1 == RefKind.None || refKind1 == RefKind.Ref);
                Debug.Assert(refKind2 == RefKind.None || refKind2 == RefKind.Ref);

                if (refKind1 != refKind2)
                {
                    if (refKind1 == RefKind.None)
                    {
                        return conv1.Kind == ConversionKind.Identity ? BetterResult.Left : BetterResult.Neither;
                    }
                    else
                    {
                        return conv2.Kind == ConversionKind.Identity ? BetterResult.Right : BetterResult.Neither;
                    }
                }
                else if (refKind1 == RefKind.Ref)
                {
                    return BetterResult.Neither;
                }
            }

            return BetterConversionFromExpression(node, t1, conv1, t2, conv2, ref useSiteDiagnostics, out okToDowngradeToNeither);
        }

        // Determine whether t1 or t2 is a better conversion target from node.
        private BetterResult BetterConversionFromExpression(BoundExpression node, TypeSymbol t1, Conversion conv1, TypeSymbol t2, Conversion conv2, ref HashSet<DiagnosticInfo> useSiteDiagnostics, out bool okToDowngradeToNeither)
        {
            okToDowngradeToNeither = false;

            if (Conversions.HasIdentityConversion(t1, t2))
            {
                // Both parameters have the same type.
                return BetterResult.Neither;
            }

            var lambdaOpt = node as UnboundLambda;

            var nodeKind = node.Kind;
            if (nodeKind == BoundKind.OutVariablePendingInference ||
                nodeKind == BoundKind.OutDeconstructVarPendingInference ||
                (nodeKind == BoundKind.DiscardExpression && !node.HasExpressionType()))
            {
                // Neither conversion from expression is better when the argument is an implicitly-typed out variable declaration.
                okToDowngradeToNeither = false;
                return BetterResult.Neither;
            }

            // Given an implicit conversion C1 that converts from an expression E to a type T1, 
            // and an implicit conversion C2 that converts from an expression E to a type T2,
            // C1 is a better conversion than C2 if E does not exactly match T2 and one of the following holds:
            bool t1MatchesExactly = ExpressionMatchExactly(node, t1, ref useSiteDiagnostics);
            bool t2MatchesExactly = ExpressionMatchExactly(node, t2, ref useSiteDiagnostics);

            if (t1MatchesExactly)
            {
                if (!t2MatchesExactly)
                {
                    // - E exactly matches T1
                    okToDowngradeToNeither = lambdaOpt != null && CanDowngradeConversionFromLambdaToNeither(BetterResult.Left, lambdaOpt, t1, t2, ref useSiteDiagnostics, false);
                    return BetterResult.Left;
                }
            }
            else if (t2MatchesExactly)
            {
                // - E exactly matches T2
                okToDowngradeToNeither = lambdaOpt != null && CanDowngradeConversionFromLambdaToNeither(BetterResult.Right, lambdaOpt, t1, t2, ref useSiteDiagnostics, false);
                return BetterResult.Right;
            }

            // - T1 is a better conversion target than T2
            return BetterConversionTarget(node, t1, conv1, t2, conv2, ref useSiteDiagnostics, out okToDowngradeToNeither);
        }

        private bool ExpressionMatchExactly(BoundExpression node, TypeSymbol t, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Given an expression E and a type T, E exactly matches T if one of the following holds:

            // - E has a type S, and an identity conversion exists from S to T 
            if ((object)node.Type != null && Conversions.HasIdentityConversion(node.Type, t))
            {
                return true;
            }

            if (node.Kind == BoundKind.TupleLiteral)
            {
                // Recurse into tuple constituent arguments.
                // Even if the tuple literal has a natural type and conversion 
                // from that type is not identity, we still have to do this 
                // because we might be converting to a tuple type backed by
                // different definition of ValueTuple type.
                return ExpressionMatchExactly((BoundTupleLiteral)node, t, ref useSiteDiagnostics);
            }

            // - E is an anonymous function, T is either a delegate type D or an expression tree 
            //   type Expression<D>, D has a return type Y, and one of the following holds:
            NamedTypeSymbol d;
            MethodSymbol invoke;
            TypeSymbol y;

            if (node.Kind == BoundKind.UnboundLambda &&
                (object)(d = t.GetDelegateType()) != null &&
                (object)(invoke = d.DelegateInvokeMethod) != null &&
                !(y = invoke.ReturnType).IsVoidType())
            {
                BoundLambda lambda = ((UnboundLambda)node).BindForReturnTypeInference(d);

                // - an inferred return type X exists for E in the context of the parameter list of D(§7.5.2.12), and an identity conversion exists from X to Y
                var x = lambda.GetInferredReturnType(ref useSiteDiagnostics);
                if (x.HasType && Conversions.HasIdentityConversion(x.Type, y))
                {
                    return true;
                }

                if (lambda.Symbol.IsAsync)
                {
                    // Dig through Task<...> for an async lambda.
                    if (y.OriginalDefinition.IsGenericTaskType(Compilation))
                    {
                        y = ((NamedTypeSymbol)y).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
                    }
                    else
                    {
                        y = null;
                    }
                }

                if ((object)y != null)
                {
                    // - The body of E is an expression that exactly matches Y, or
                    //   has a return statement with expression and all return statements have expression that 
                    //   exactly matches Y.

                    // Handle trivial cases first
                    switch (lambda.Body.Statements.Length)
                    {
                        case 0:
                            break;

                        case 1:
                            if (lambda.Body.Statements[0].Kind == BoundKind.ReturnStatement)
                            {
                                var returnStmt = (BoundReturnStatement)lambda.Body.Statements[0];
                                if (returnStmt.ExpressionOpt != null && ExpressionMatchExactly(returnStmt.ExpressionOpt, y, ref useSiteDiagnostics))
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                goto default;
                            }

                            break;

                        default:
                            var returnStatements = ArrayBuilder<BoundReturnStatement>.GetInstance();
                            var walker = new ReturnStatements(returnStatements);

                            walker.Visit(lambda.Body);

                            bool result = false;
                            foreach (BoundReturnStatement r in returnStatements)
                            {
                                if (r.ExpressionOpt == null || !ExpressionMatchExactly(r.ExpressionOpt, y, ref useSiteDiagnostics))
                                {
                                    result = false;
                                    break;
                                }
                                else
                                {
                                    result = true;
                                }
                            }

                            returnStatements.Free();

                            if (result)
                            {
                                return true;
                            }
                            break;
                    }
                }
            }

            return false;
        }

        // check every argument of a tuple vs corresponding type in destination tuple type
        private bool ExpressionMatchExactly(BoundTupleLiteral tupleSource, TypeSymbol targetType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (targetType.Kind != SymbolKind.NamedType)
            {
                // tuples can only match to tuples or tuple underlying types and either is a named type
                return false;
            }

            var destination = (NamedTypeSymbol)targetType;
            var sourceArguments = tupleSource.Arguments;

            // check if the type is actually compatible type for a tuple of given cardinality
            if (!destination.IsTupleOrCompatibleWithTupleOfCardinality(sourceArguments.Length))
            {
                return false;
            }

            var destTypes = destination.GetElementTypesOfTupleOrCompatible();
            Debug.Assert(sourceArguments.Length == destTypes.Length);

            for (int i = 0; i < sourceArguments.Length; i++)
            {
                if (!ExpressionMatchExactly(sourceArguments[i], destTypes[i].Type, ref useSiteDiagnostics))
                {
                    return false;
                }
            }

            return true;
        }

        private class ReturnStatements : BoundTreeWalker
        {
            private readonly ArrayBuilder<BoundReturnStatement> _returns;

            public ReturnStatements(ArrayBuilder<BoundReturnStatement> returns)
            {
                _returns = returns;
            }

            public override BoundNode Visit(BoundNode node)
            {
                if (!(node is BoundExpression))
                {
                    return base.Visit(node);
                }

                return null;
            }

            protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
            {
                // Do not recurse into nested local functions; we don't want their returns.
                return null;
            }

            public override BoundNode VisitReturnStatement(BoundReturnStatement node)
            {
                _returns.Add(node);
                return null;
            }
        }

        private const int BetterConversionTargetRecursionLimit = 100;

        private BetterResult BetterConversionTarget(
            TypeSymbol type1,
            TypeSymbol type2,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            bool okToDowngradeToNeither;
            return BetterConversionTargetCore(null, type1, default(Conversion), type2, default(Conversion), ref useSiteDiagnostics, out okToDowngradeToNeither, BetterConversionTargetRecursionLimit);
        }

        private BetterResult BetterConversionTargetCore(
            TypeSymbol type1,
            TypeSymbol type2,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            int betterConversionTargetRecursionLimit)
        {
            if (betterConversionTargetRecursionLimit < 0)
            {
                return BetterResult.Neither;
            }

            bool okToDowngradeToNeither;
            return BetterConversionTargetCore(null, type1, default(Conversion), type2, default(Conversion), ref useSiteDiagnostics, out okToDowngradeToNeither, betterConversionTargetRecursionLimit - 1);
        }

        private BetterResult BetterConversionTarget(
            BoundExpression node,
            TypeSymbol type1,
            Conversion conv1,
            TypeSymbol type2,
            Conversion conv2,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            out bool okToDowngradeToNeither)
        {
            return BetterConversionTargetCore(node, type1, conv1, type2, conv2, ref useSiteDiagnostics, out okToDowngradeToNeither, BetterConversionTargetRecursionLimit);
        }

        private BetterResult BetterConversionTargetCore(
            BoundExpression node,
            TypeSymbol type1,
            Conversion conv1,
            TypeSymbol type2,
            Conversion conv2,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            out bool okToDowngradeToNeither,
            int betterConversionTargetRecursionLimit)
        {
            okToDowngradeToNeither = false;

            if (Conversions.HasIdentityConversion(type1, type2))
            {
                // Both types are the same type.
                return BetterResult.Neither;
            }

            // Given two different types T1 and T2, T1 is a better conversion target than T2 if no implicit conversion from T2 to T1 exists, 
            // and at least one of the following holds:
            bool type1ToType2 = Conversions.ClassifyImplicitConversionFromType(type1, type2, ref useSiteDiagnostics).IsImplicit;
            bool type2ToType1 = Conversions.ClassifyImplicitConversionFromType(type2, type1, ref useSiteDiagnostics).IsImplicit;
            UnboundLambda lambdaOpt = node as UnboundLambda;

            if (type1ToType2)
            {
                if (type2ToType1)
                {
                    // An implicit conversion both ways.
                    return BetterResult.Neither;
                }

                // - An implicit conversion from T1 to T2 exists 
                okToDowngradeToNeither = lambdaOpt != null && CanDowngradeConversionFromLambdaToNeither(BetterResult.Left, lambdaOpt, type1, type2, ref useSiteDiagnostics, true);
                return BetterResult.Left;
            }
            else if (type2ToType1)
            {
                // - An implicit conversion from T1 to T2 exists 
                okToDowngradeToNeither = lambdaOpt != null && CanDowngradeConversionFromLambdaToNeither(BetterResult.Right, lambdaOpt, type1, type2, ref useSiteDiagnostics, true);
                return BetterResult.Right;
            }

            bool type1IsGenericTask = type1.OriginalDefinition.IsGenericTaskType(Compilation);
            bool type2IsGenericTask = type2.OriginalDefinition.IsGenericTaskType(Compilation);

            if (type1IsGenericTask)
            {
                if (type2IsGenericTask)
                {
                    // - T1 is Task<S1>, T2 is Task<S2>, and S1 is a better conversion target than S2
                    return BetterConversionTargetCore(((NamedTypeSymbol)type1).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type,
                                                      ((NamedTypeSymbol)type2).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type,
                                                      ref useSiteDiagnostics, betterConversionTargetRecursionLimit);
                }

                // A shortcut, Task<T> type cannot satisfy other rules.
                return BetterResult.Neither;
            }
            else if (type2IsGenericTask)
            {
                // A shortcut, Task<T> type cannot satisfy other rules.
                return BetterResult.Neither;
            }

            NamedTypeSymbol d1;

            if ((object)(d1 = type1.GetDelegateType()) != null)
            {
                NamedTypeSymbol d2;

                if ((object)(d2 = type2.GetDelegateType()) != null)
                {
                    // - T1 is either a delegate type D1 or an expression tree type Expression<D1>,
                    //   T2 is either a delegate type D2 or an expression tree type Expression<D2>,
                    //   D1 has a return type S1 and one of the following holds:
                    MethodSymbol invoke1 = d1.DelegateInvokeMethod;
                    MethodSymbol invoke2 = d2.DelegateInvokeMethod;

                    if ((object)invoke1 != null && (object)invoke2 != null)
                    {
                        TypeSymbol r1 = invoke1.ReturnType;
                        TypeSymbol r2 = invoke2.ReturnType;
                        BetterResult delegateResult = BetterResult.Neither;

                        if (!r1.IsVoidType())
                        {
                            if (r2.IsVoidType())
                            {
                                // - D2 is void returning
                                delegateResult = BetterResult.Left;
                            }
                        }
                        else if (!r2.IsVoidType())
                        {
                            // - D2 is void returning
                            delegateResult = BetterResult.Right;
                        }

                        if (delegateResult == BetterResult.Neither)
                        {
                            //  - D2 has a return type S2, and S1 is a better conversion target than S2
                            delegateResult = BetterConversionTargetCore(r1, r2, ref useSiteDiagnostics, betterConversionTargetRecursionLimit);
                        }

                        // Downgrade result to Neither if conversion used by the winner isn't actually valid method group conversion.
                        // This is necessary to preserve compatibility, otherwise we might dismiss "worse", but truly applicable candidate
                        // based on a "better", but, in reality, erroneous one.
                        if (node?.Kind == BoundKind.MethodGroup)
                        {
                            var group = (BoundMethodGroup)node;

                            if (delegateResult == BetterResult.Left)
                            {
                                if (IsMethodGroupConversionIncompatibleWithDelegate(group, d1, conv1))
                                {
                                    return BetterResult.Neither;
                                }
                            }
                            else if (delegateResult == BetterResult.Right && IsMethodGroupConversionIncompatibleWithDelegate(group, d2, conv2))
                            {
                                return BetterResult.Neither;
                            }
                        }

                        return delegateResult;
                    }
                }

                // A shortcut, a delegate or an expression tree cannot satisfy other rules.
                return BetterResult.Neither;
            }
            else if ((object)type2.GetDelegateType() != null)
            {
                // A shortcut, a delegate or an expression tree cannot satisfy other rules.
                return BetterResult.Neither;
            }

            // -T1 is a signed integral type and T2 is an unsigned integral type.Specifically:
            //    - T1 is sbyte and T2 is byte, ushort, uint, or ulong
            //    - T1 is short and T2 is ushort, uint, or ulong
            //    - T1 is int and T2 is uint, or ulong
            //    - T1 is long and T2 is ulong
            if (IsSignedIntegralType(type1))
            {
                if (IsUnsignedIntegralType(type2))
                {
                    return BetterResult.Left;
                }
            }
            else if (IsUnsignedIntegralType(type1) && IsSignedIntegralType(type2))
            {
                return BetterResult.Right;
            }

            return BetterResult.Neither;
        }

        private bool IsMethodGroupConversionIncompatibleWithDelegate(BoundMethodGroup node, NamedTypeSymbol delegateType, Conversion conv)
        {
            if (conv.IsMethodGroup)
            {
                DiagnosticBag ignore = DiagnosticBag.GetInstance();
                bool result = !_binder.MethodGroupIsCompatibleWithDelegate(node.ReceiverOpt, conv.IsExtensionMethod, conv.Method, delegateType, Location.None, ignore);
                ignore.Free();
                return result;
            }

            return false;
        }

        private bool CanDowngradeConversionFromLambdaToNeither(BetterResult currentResult, UnboundLambda lambda, TypeSymbol type1, TypeSymbol type2, ref HashSet<DiagnosticInfo> useSiteDiagnostics, bool fromTypeAnalysis)
        {
            // DELIBERATE SPEC VIOLATION: See bug 11961.
            // The native compiler uses one algorithm for determining betterness of lambdas and another one
            // for everything else. This is wrong; the correct behavior is to do the type analysis of
            // the parameter types first, and then if necessary, do the lambda analysis. Native compiler
            // skips analysis of the parameter types when they are delegate types with identical parameter
            // lists and the corresponding argument is a lambda. 
            // There is a real-world code that breaks if we follow the specification, so we will try to fall
            // back to the original behavior to avoid an ambiguity that wasn't an ambiguity before.

            NamedTypeSymbol d1;

            if ((object)(d1 = type1.GetDelegateType()) != null)
            {
                NamedTypeSymbol d2;

                if ((object)(d2 = type2.GetDelegateType()) != null)
                {
                    MethodSymbol invoke1 = d1.DelegateInvokeMethod;
                    MethodSymbol invoke2 = d2.DelegateInvokeMethod;

                    if ((object)invoke1 != null && (object)invoke2 != null)
                    {
                        if (!IdenticalParameters(invoke1.Parameters, invoke2.Parameters))
                        {
                            return true;
                        }

                        TypeSymbol r1 = invoke1.ReturnType;
                        TypeSymbol r2 = invoke2.ReturnType;

#if DEBUG
                        if (fromTypeAnalysis)
                        {
                            Debug.Assert((r1.IsVoidType()) == (r2.IsVoidType()));

                            // Since we are dealing with variance delegate conversion and delegates have identical parameter
                            // lists, return types must be different and neither can be void.
                            Debug.Assert(!r1.IsVoidType());
                            Debug.Assert(!r2.IsVoidType());
                            Debug.Assert(!Conversions.HasIdentityConversion(r1, r2));
                        }
#endif

                        if (r1.IsVoidType())
                        {
                            if (r2.IsVoidType())
                            {
                                return true;
                            }

                            Debug.Assert(currentResult == BetterResult.Right);
                            return false;
                        }
                        else if (r2.IsVoidType())
                        {
                            Debug.Assert(currentResult == BetterResult.Left);
                            return false;
                        }

                        if (Conversions.HasIdentityConversion(r1, r2))
                        {
                            return true;
                        }

                        var x = lambda.InferReturnType(Conversions, d1, ref useSiteDiagnostics);
                        if (!x.HasType)
                        {
                            return true;
                        }

#if DEBUG
                        if (fromTypeAnalysis)
                        {
                            // Since we are dealing with variance delegate conversion and delegates have identical parameter
                            // lists, return types must be implicitly convertible in the same direction.
                            // Or we might be dealing with error return types and we may have one error delegate matching exactly
                            // while another not being an error and not convertible.
                            Debug.Assert(
                                r1.IsErrorType() ||
                                r2.IsErrorType() ||
                                currentResult == BetterConversionTarget(r1, r2, ref useSiteDiagnostics));
                        }
#endif
                    }
                }
            }

            return false;
        }

        private static bool IdenticalParameters(ImmutableArray<ParameterSymbol> p1, ImmutableArray<ParameterSymbol> p2)
        {
            if (p1.IsDefault || p2.IsDefault)
            {
                // This only happens in error scenarios.
                return false;
            }

            if (p1.Length != p2.Length)
            {
                return false;
            }

            for (int i = 0; i < p1.Length; ++i)
            {
                var param1 = p1[i];
                var param2 = p2[i];

                if (param1.RefKind != param2.RefKind)
                {
                    return false;
                }

                if (!Conversions.HasIdentityConversion(param1.Type, param2.Type))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSignedIntegralType(TypeSymbol type)
        {
            if ((object)type != null && type.IsNullableType())
            {
                type = type.GetNullableUnderlyingType();
            }

            switch (type.GetSpecialTypeSafe())
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsUnsignedIntegralType(TypeSymbol type)
        {
            if ((object)type != null && type.IsNullableType())
            {
                type = type.GetNullableUnderlyingType();
            }

            switch (type.GetSpecialTypeSafe())
            {
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    return true;

                default:
                    return false;
            }
        }

        internal static void GetEffectiveParameterTypes(
            MethodSymbol method,
            int argumentCount,
            ImmutableArray<int> argToParamMap,
            ArrayBuilder<RefKind> argumentRefKinds,
            bool isMethodGroupConversion,
            bool allowRefOmittedArguments,
            Binder binder,
            bool expanded,
            out ImmutableArray<TypeWithAnnotations> parameterTypes,
            out ImmutableArray<RefKind> parameterRefKinds)
        {
            bool hasAnyRefOmittedArgument;
            EffectiveParameters effectiveParameters = expanded ?
                GetEffectiveParametersInExpandedForm(method, argumentCount, argToParamMap, argumentRefKinds, isMethodGroupConversion, allowRefOmittedArguments, binder, out hasAnyRefOmittedArgument) :
                GetEffectiveParametersInNormalForm(method, argumentCount, argToParamMap, argumentRefKinds, isMethodGroupConversion, allowRefOmittedArguments, binder, out hasAnyRefOmittedArgument);
            parameterTypes = effectiveParameters.ParameterTypes;
            parameterRefKinds = effectiveParameters.ParameterRefKinds;
        }

        private struct EffectiveParameters
        {
            internal readonly ImmutableArray<TypeWithAnnotations> ParameterTypes;
            internal readonly ImmutableArray<RefKind> ParameterRefKinds;

            internal EffectiveParameters(ImmutableArray<TypeWithAnnotations> types, ImmutableArray<RefKind> refKinds)
            {
                ParameterTypes = types;
                ParameterRefKinds = refKinds;
            }
        }

        private EffectiveParameters GetEffectiveParametersInNormalForm<TMember>(
            TMember member,
            int argumentCount,
            ImmutableArray<int> argToParamMap,
            ArrayBuilder<RefKind> argumentRefKinds,
            bool isMethodGroupConversion,
            bool allowRefOmittedArguments)
            where TMember : Symbol
        {
            bool discarded;
            return GetEffectiveParametersInNormalForm(member, argumentCount, argToParamMap, argumentRefKinds, isMethodGroupConversion, allowRefOmittedArguments, _binder, hasAnyRefOmittedArgument: out discarded);
        }

        private static EffectiveParameters GetEffectiveParametersInNormalForm<TMember>(
            TMember member,
            int argumentCount,
            ImmutableArray<int> argToParamMap,
            ArrayBuilder<RefKind> argumentRefKinds,
            bool isMethodGroupConversion,
            bool allowRefOmittedArguments,
            Binder binder,
            out bool hasAnyRefOmittedArgument) where TMember : Symbol
        {
            Debug.Assert(argumentRefKinds != null);

            hasAnyRefOmittedArgument = false;
            ImmutableArray<ParameterSymbol> parameters = member.GetParameters();

            // We simulate an extra parameter for vararg methods
            int parameterCount = member.GetParameterCount() + (member.GetIsVararg() ? 1 : 0);

            if (argumentCount == parameterCount && argToParamMap.IsDefaultOrEmpty)
            {
                ImmutableArray<RefKind> parameterRefKinds = member.GetParameterRefKinds();
                if (parameterRefKinds.IsDefaultOrEmpty)
                {
                    return new EffectiveParameters(member.GetParameterTypes(), parameterRefKinds);
                }
            }

            var types = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            ArrayBuilder<RefKind> refs = null;
            bool hasAnyRefArg = argumentRefKinds.Any();

            for (int arg = 0; arg < argumentCount; ++arg)
            {
                int parm = argToParamMap.IsDefault ? arg : argToParamMap[arg];
                // If this is the __arglist parameter, or an extra argument in error situations, just skip it.
                if (parm >= parameters.Length)
                {
                    continue;
                }
                var parameter = parameters[parm];
                types.Add(parameter.TypeWithAnnotations);

                RefKind argRefKind = hasAnyRefArg ? argumentRefKinds[arg] : RefKind.None;
                RefKind paramRefKind = GetEffectiveParameterRefKind(parameter, argRefKind, isMethodGroupConversion, allowRefOmittedArguments, binder, ref hasAnyRefOmittedArgument);

                if (refs == null)
                {
                    if (paramRefKind != RefKind.None)
                    {
                        refs = ArrayBuilder<RefKind>.GetInstance(arg, RefKind.None);
                        refs.Add(paramRefKind);
                    }
                }
                else
                {
                    refs.Add(paramRefKind);
                }
            }

            var refKinds = refs != null ? refs.ToImmutableAndFree() : default(ImmutableArray<RefKind>);
            return new EffectiveParameters(types.ToImmutableAndFree(), refKinds);
        }

        private static RefKind GetEffectiveParameterRefKind(
            ParameterSymbol parameter,
            RefKind argRefKind,
            bool isMethodGroupConversion,
            bool allowRefOmittedArguments,
            Binder binder,
            ref bool hasAnyRefOmittedArgument)
        {
            var paramRefKind = parameter.RefKind;

            // 'None' argument is allowed to match 'In' parameter and should behave like 'None' for the purpose of overload resolution
            // unless this is a method group conversion where 'In' must match 'In'
            if (!isMethodGroupConversion && argRefKind == RefKind.None && paramRefKind == RefKind.In)
            {
                return RefKind.None;
            }

            // Omit ref feature for COM interop: We can pass arguments by value for ref parameters if we are calling a method/property on an instance of a COM imported type.
            // We must ignore the 'ref' on the parameter while determining the applicability of argument for the given method call.
            // During argument rewriting, we will replace the argument value with a temporary local and pass that local by reference.
            if (allowRefOmittedArguments && paramRefKind == RefKind.Ref && argRefKind == RefKind.None && !binder.InAttributeArgument)
            {
                hasAnyRefOmittedArgument = true;
                return RefKind.None;
            }

            return paramRefKind;
        }

        private EffectiveParameters GetEffectiveParametersInExpandedForm<TMember>(
            TMember member,
            int argumentCount,
            ImmutableArray<int> argToParamMap,
            ArrayBuilder<RefKind> argumentRefKinds,
            bool isMethodGroupConversion,
            bool allowRefOmittedArguments) where TMember : Symbol
        {
            bool discarded;
            return GetEffectiveParametersInExpandedForm(member, argumentCount, argToParamMap, argumentRefKinds, isMethodGroupConversion, allowRefOmittedArguments, _binder, hasAnyRefOmittedArgument: out discarded);
        }

        private static EffectiveParameters GetEffectiveParametersInExpandedForm<TMember>(
            TMember member,
            int argumentCount,
            ImmutableArray<int> argToParamMap,
            ArrayBuilder<RefKind> argumentRefKinds,
            bool isMethodGroupConversion,
            bool allowRefOmittedArguments,
            Binder binder,
            out bool hasAnyRefOmittedArgument) where TMember : Symbol
        {
            Debug.Assert(argumentRefKinds != null);

            var types = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            var refs = ArrayBuilder<RefKind>.GetInstance();
            bool anyRef = false;
            var parameters = member.GetParameters();
            bool hasAnyRefArg = argumentRefKinds.Any();
            hasAnyRefOmittedArgument = false;

            for (int arg = 0; arg < argumentCount; ++arg)
            {
                var parm = argToParamMap.IsDefault ? arg : argToParamMap[arg];
                var parameter = parameters[parm];
                var type = parameter.TypeWithAnnotations;

                types.Add(parm == parameters.Length - 1 ? ((ArrayTypeSymbol)type.Type).ElementTypeWithAnnotations : type);

                var argRefKind = hasAnyRefArg ? argumentRefKinds[arg] : RefKind.None;
                var paramRefKind = GetEffectiveParameterRefKind(parameter, argRefKind, isMethodGroupConversion, allowRefOmittedArguments, binder, ref hasAnyRefOmittedArgument);

                refs.Add(paramRefKind);
                if (paramRefKind != RefKind.None)
                {
                    anyRef = true;
                }
            }

            var refKinds = anyRef ? refs.ToImmutable() : default(ImmutableArray<RefKind>);
            refs.Free();
            return new EffectiveParameters(types.ToImmutableAndFree(), refKinds);
        }

        private MemberResolutionResult<TMember> IsMemberApplicableInNormalForm<TMember>(
            TMember member,                // method or property
            TMember leastOverriddenMember, // method or property
            ArrayBuilder<TypeWithAnnotations> typeArguments,
            AnalyzedArguments arguments,
            bool isMethodGroupConversion,
            bool allowRefOmittedArguments,
            bool inferWithDynamic,
            bool completeResults,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            // AnalyzeArguments matches arguments to parameter names and positions. 
            // For that purpose we use the most derived member.
            var argumentAnalysis = AnalyzeArguments(member, arguments, isMethodGroupConversion, expanded: false);
            if (!argumentAnalysis.IsValid)
            {
                switch (argumentAnalysis.Kind)
                {
                    case ArgumentAnalysisResultKind.RequiredParameterMissing:
                    case ArgumentAnalysisResultKind.NoCorrespondingParameter:
                    case ArgumentAnalysisResultKind.DuplicateNamedArgument:
                        if (!completeResults) goto default;
                        // When we are producing more complete results, and we have the wrong number of arguments, we push on
                        // through type inference so that lambda arguments can be bound to their delegate-typed parameters,
                        // thus improving the API and intellisense experience.
                        break;
                    default:
                        return new MemberResolutionResult<TMember>(member, leastOverriddenMember, MemberAnalysisResult.ArgumentParameterMismatch(argumentAnalysis));
                }
            }

            // Check after argument analysis, but before more complicated type inference and argument type validation.
            // NOTE: The diagnostic may not be reported (e.g. if the member is later removed as less-derived).
            if (member.HasUseSiteError)
            {
                return new MemberResolutionResult<TMember>(member, leastOverriddenMember, MemberAnalysisResult.UseSiteError());
            }

            bool hasAnyRefOmittedArgument;

            // To determine parameter types we use the originalMember.
            EffectiveParameters originalEffectiveParameters = GetEffectiveParametersInNormalForm(
                GetConstructedFrom(leastOverriddenMember),
                arguments.Arguments.Count,
                argumentAnalysis.ArgsToParamsOpt,
                arguments.RefKinds,
                isMethodGroupConversion,
                allowRefOmittedArguments,
                _binder,
                out hasAnyRefOmittedArgument);

            Debug.Assert(!hasAnyRefOmittedArgument || allowRefOmittedArguments);

            // To determine parameter types we use the originalMember.
            EffectiveParameters constructedEffectiveParameters = GetEffectiveParametersInNormalForm(
                leastOverriddenMember,
                arguments.Arguments.Count,
                argumentAnalysis.ArgsToParamsOpt,
                arguments.RefKinds,
                isMethodGroupConversion,
                allowRefOmittedArguments);

            // The member passed to the following call is returned in the result (possibly a constructed version of it).
            // The applicability is checked based on effective parameters passed in.
            var applicableResult = IsApplicable(
                member, leastOverriddenMember,
                typeArguments, arguments, originalEffectiveParameters, constructedEffectiveParameters,
                argumentAnalysis.ArgsToParamsOpt,
                hasAnyRefOmittedArgument: hasAnyRefOmittedArgument,
                inferWithDynamic: inferWithDynamic,
                completeResults: completeResults,
                useSiteDiagnostics: ref useSiteDiagnostics);

            // If we were producing complete results and had missing arguments, we pushed on in order to call IsApplicable for
            // type inference and lambda binding. In that case we still need to return the argument mismatch failure here.
            if (completeResults && !argumentAnalysis.IsValid)
            {
                return new MemberResolutionResult<TMember>(member, leastOverriddenMember, MemberAnalysisResult.ArgumentParameterMismatch(argumentAnalysis));
            }

            return applicableResult;
        }

        private MemberResolutionResult<TMember> IsMemberApplicableInExpandedForm<TMember>(
            TMember member,                // method or property
            TMember leastOverriddenMember, // method or property
            ArrayBuilder<TypeWithAnnotations> typeArguments,
            AnalyzedArguments arguments,
            bool allowRefOmittedArguments,
            bool completeResults,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            // AnalyzeArguments matches arguments to parameter names and positions. 
            // For that purpose we use the most derived member.
            var argumentAnalysis = AnalyzeArguments(member, arguments, isMethodGroupConversion: false, expanded: true);
            if (!argumentAnalysis.IsValid)
            {
                return new MemberResolutionResult<TMember>(member, leastOverriddenMember, MemberAnalysisResult.ArgumentParameterMismatch(argumentAnalysis));
            }

            // Check after argument analysis, but before more complicated type inference and argument type validation.
            // NOTE: The diagnostic may not be reported (e.g. if the member is later removed as less-derived).
            if (member.HasUseSiteError)
            {
                return new MemberResolutionResult<TMember>(member, leastOverriddenMember, MemberAnalysisResult.UseSiteError());
            }

            bool hasAnyRefOmittedArgument;

            // To determine parameter types we use the least derived member.
            EffectiveParameters originalEffectiveParameters = GetEffectiveParametersInExpandedForm(
                GetConstructedFrom(leastOverriddenMember),
                arguments.Arguments.Count,
                argumentAnalysis.ArgsToParamsOpt,
                arguments.RefKinds,
                isMethodGroupConversion: false,
                allowRefOmittedArguments,
                _binder,
                out hasAnyRefOmittedArgument);

            Debug.Assert(!hasAnyRefOmittedArgument || allowRefOmittedArguments);

            // To determine parameter types we use the least derived member.
            EffectiveParameters constructedEffectiveParameters = GetEffectiveParametersInExpandedForm(
                leastOverriddenMember,
                arguments.Arguments.Count,
                argumentAnalysis.ArgsToParamsOpt,
                arguments.RefKinds,
                isMethodGroupConversion: false,
                allowRefOmittedArguments);

            // The member passed to the following call is returned in the result (possibly a constructed version of it).
            // The applicability is checked based on effective parameters passed in.
            var result = IsApplicable(
                member, leastOverriddenMember,
                typeArguments, arguments, originalEffectiveParameters, constructedEffectiveParameters,
                argumentAnalysis.ArgsToParamsOpt,
                hasAnyRefOmittedArgument: hasAnyRefOmittedArgument,
                inferWithDynamic: false,
                completeResults: completeResults,
                useSiteDiagnostics: ref useSiteDiagnostics);

            return result.Result.IsValid ?
                new MemberResolutionResult<TMember>(
                    result.Member,
                    result.LeastOverriddenMember,
                    MemberAnalysisResult.ExpandedForm(result.Result.ArgsToParamsOpt, result.Result.ConversionsOpt, hasAnyRefOmittedArgument)) :
                result;
        }

        private MemberResolutionResult<TMember> IsApplicable<TMember>(
            TMember member,                // method or property
            TMember leastOverriddenMember, // method or property 
            ArrayBuilder<TypeWithAnnotations> typeArgumentsBuilder,
            AnalyzedArguments arguments,
            EffectiveParameters originalEffectiveParameters,
            EffectiveParameters constructedEffectiveParameters,
            ImmutableArray<int> argsToParamsMap,
            bool hasAnyRefOmittedArgument,
            bool inferWithDynamic,
            bool completeResults,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            where TMember : Symbol
        {
            bool ignoreOpenTypes;
            MethodSymbol method;
            EffectiveParameters effectiveParameters;
            if (member.Kind == SymbolKind.Method && (method = (MethodSymbol)(Symbol)member).Arity > 0)
            {
                if (typeArgumentsBuilder.Count == 0 && arguments.HasDynamicArgument && !inferWithDynamic)
                {
                    // Spec 7.5.4: Compile-time checking of dynamic overload resolution:
                    // * First, if F is a generic method and type arguments were provided, 
                    //   then those are substituted for the type parameters in the parameter list. 
                    //   However, if type arguments were not provided, no such substitution happens.
                    // * Then, any parameter whose type contains a an unsubstituted type parameter of F 
                    //   is elided, along with the corresponding arguments(s).

                    // We don't need to check constraints of types of the non-elided parameters since they 
                    // have no effect on applicability of this candidate.
                    ignoreOpenTypes = true;
                    effectiveParameters = constructedEffectiveParameters;
                }
                else
                {
                    MethodSymbol leastOverriddenMethod = (MethodSymbol)(Symbol)leastOverriddenMember;

                    ImmutableArray<TypeWithAnnotations> typeArguments;
                    if (typeArgumentsBuilder.Count > 0)
                    {
                        // generic type arguments explicitly specified at call-site:
                        typeArguments = typeArgumentsBuilder.ToImmutable();
                    }
                    else
                    {
                        // infer generic type arguments:
                        MemberAnalysisResult inferenceError;
                        typeArguments = InferMethodTypeArguments(method,
                                            leastOverriddenMethod.ConstructedFrom.TypeParameters,
                                            arguments,
                                            originalEffectiveParameters,
                                            out inferenceError,
                                            ref useSiteDiagnostics);
                        if (typeArguments.IsDefault)
                        {
                            return new MemberResolutionResult<TMember>(member, leastOverriddenMember, inferenceError);
                        }
                    }

                    member = (TMember)(Symbol)method.Construct(typeArguments);
                    leastOverriddenMember = (TMember)(Symbol)leastOverriddenMethod.ConstructedFrom.Construct(typeArguments);

                    // Spec (§7.6.5.1)
                    //   Once the (inferred) type arguments are substituted for the corresponding method type parameters, 
                    //   all constructed types in the parameter list of F satisfy *their* constraints (§4.4.4), 
                    //   and the parameter list of F is applicable with respect to A (§7.5.3.1).
                    //
                    // This rule is a bit complicated; let's take a look at an example. Suppose we have
                    // class X<U> where U : struct {}
                    // ...
                    // void M<T>(T t, X<T> xt) where T : struct {}
                    // void M(object o1, object o2) {}
                    //
                    // Suppose there is a call M("", null). Type inference infers that T is string.
                    // M<string> is then not an applicable candidate *NOT* because string violates the
                    // constraint on T. That is not checked until "final validation" (although when
                    // feature 'ImprovedOverloadCandidates' is enabled in later language versions
                    // it is checked on the candidate before overload resolution). Rather, the 
                    // method is not a candidate because string violates the constraint *on U*. 
                    // The constructed method has formal parameter type X<string>, which is not legal.
                    // In the case given, the generic method is eliminated and the object version wins.
                    //
                    // Note also that the constraints need to be checked on *all* the formal parameter
                    // types, not just the ones in the *effective parameter list*. If we had:
                    // void M<T>(T t, X<T> xt = null) where T : struct {}
                    // void M<T>(object o1, object o2 = null) where T : struct {}
                    // and a call M("") then type inference still works out that T is string, and
                    // the generic method still needs to be discarded, even though type inference
                    // never saw the second formal parameter.

                    var parameterTypes = leastOverriddenMember.GetParameterTypes();
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        if (!parameterTypes[i].Type.CheckAllConstraints(Compilation, Conversions))
                        {
                            return new MemberResolutionResult<TMember>(member, leastOverriddenMember, MemberAnalysisResult.ConstructedParameterFailedConstraintsCheck(i));
                        }
                    }

                    // Types of constructed effective parameters might originate from a virtual/abstract method 
                    // that the current "method" overrides. If the virtual/abstract method is generic we constructed it 
                    // using the generic parameters of "method", so we can now substitute these type parameters 
                    // in the constructed effective parameters.

                    var map = new TypeMap(method.TypeParameters, typeArguments, allowAlpha: true);

                    effectiveParameters = new EffectiveParameters(
                        map.SubstituteTypes(constructedEffectiveParameters.ParameterTypes),
                        constructedEffectiveParameters.ParameterRefKinds);

                    ignoreOpenTypes = false;
                }
            }
            else
            {
                effectiveParameters = constructedEffectiveParameters;
                ignoreOpenTypes = false;
            }

            var applicableResult = IsApplicable(
                member,
                effectiveParameters,
                arguments,
                argsToParamsMap,
                isVararg: member.GetIsVararg(),
                hasAnyRefOmittedArgument: hasAnyRefOmittedArgument,
                ignoreOpenTypes: ignoreOpenTypes,
                completeResults: completeResults,
                useSiteDiagnostics: ref useSiteDiagnostics);
            return new MemberResolutionResult<TMember>(member, leastOverriddenMember, applicableResult);
        }

        private ImmutableArray<TypeWithAnnotations> InferMethodTypeArguments(
            MethodSymbol method,
            ImmutableArray<TypeParameterSymbol> originalTypeParameters,
            AnalyzedArguments arguments,
            EffectiveParameters originalEffectiveParameters,
            out MemberAnalysisResult error,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var args = arguments.Arguments.ToImmutable();

            // The reason why we pass the type parameters and formal parameter types
            // from the original definition, not the method as it exists as a member of 
            // a possibly constructed generic type, is exceedingly subtle. See the comments
            // in "Infer" for details.

            var inferenceResult = MethodTypeInferrer.Infer(
                _binder,
                _binder.Conversions,
                originalTypeParameters,
                method.ContainingType,
                originalEffectiveParameters.ParameterTypes,
                originalEffectiveParameters.ParameterRefKinds,
                args,
                ref useSiteDiagnostics);

            if (inferenceResult.Success)
            {
                error = default(MemberAnalysisResult);
                return inferenceResult.InferredTypeArguments;
            }

            if (arguments.IsExtensionMethodInvocation)
            {
                var inferredFromFirstArgument = MethodTypeInferrer.InferTypeArgumentsFromFirstArgument(
                    _binder.Conversions,
                    method,
                    args,
                    useSiteDiagnostics: ref useSiteDiagnostics);
                if (inferredFromFirstArgument.IsDefault)
                {
                    error = MemberAnalysisResult.TypeInferenceExtensionInstanceArgumentFailed();
                    return default(ImmutableArray<TypeWithAnnotations>);
                }
            }

            error = MemberAnalysisResult.TypeInferenceFailed();
            return default(ImmutableArray<TypeWithAnnotations>);
        }

        private MemberAnalysisResult IsApplicable(
            Symbol candidate, // method or property
            EffectiveParameters parameters,
            AnalyzedArguments arguments,
            ImmutableArray<int> argsToParameters,
            bool isVararg,
            bool hasAnyRefOmittedArgument,
            bool ignoreOpenTypes,
            bool completeResults,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // The effective parameters are in the right order with respect to the arguments.
            //
            // The difference between "parameters" and "original parameters" is as follows. Suppose
            // we have class C<V> { static void M<T, U>(T t, U u, V v) { C<T>.M(1, t, t); } }
            // In the call, the "original parameters" are (T, U, V). The "constructed parameters",
            // not passed in here, are (T, U, T) because T is substituted for V; type inference then
            // infers that T is int and U is T.  The "parameters" are therefore (int, T, T).
            //
            // We add a "virtual parameter" for the __arglist.
            int paramCount = parameters.ParameterTypes.Length + (isVararg ? 1 : 0);

            if (arguments.Arguments.Count < paramCount)
            {
                // For improved error recovery, we perform type inference even when the argument
                // list is of the wrong length. The caller is expected to detect and handle that,
                // treating the method as inapplicable.
                paramCount = arguments.Arguments.Count;
            }

            // For each argument in A, the parameter passing mode of the argument (i.e., value, ref, or out) is 
            // identical to the parameter passing mode of the corresponding parameter, and
            // * for a value parameter or a parameter array, an implicit conversion exists from the 
            //   argument to the type of the corresponding parameter, or
            // * for a ref or out parameter, the type of the argument is identical to the type of the corresponding 
            //   parameter. After all, a ref or out parameter is an alias for the argument passed.
            ArrayBuilder<Conversion> conversions = null;
            ArrayBuilder<int> badArguments = null;
            for (int argumentPosition = 0; argumentPosition < paramCount; argumentPosition++)
            {
                BoundExpression argument = arguments.Argument(argumentPosition);
                Conversion conversion;

                if (isVararg && argumentPosition == paramCount - 1)
                {
                    // Only an __arglist() expression is convertible.
                    if (argument.Kind == BoundKind.ArgListOperator)
                    {
                        conversion = Conversion.Identity;
                    }
                    else
                    {
                        badArguments = badArguments ?? ArrayBuilder<int>.GetInstance();
                        badArguments.Add(argumentPosition);
                        conversion = Conversion.NoConversion;
                    }
                }
                else
                {
                    RefKind argumentRefKind = arguments.RefKind(argumentPosition);
                    RefKind parameterRefKind = parameters.ParameterRefKinds.IsDefault ? RefKind.None : parameters.ParameterRefKinds[argumentPosition];
                    bool forExtensionMethodThisArg = arguments.IsExtensionMethodThisArgument(argumentPosition);

                    if (forExtensionMethodThisArg)
                    {
                        Debug.Assert(argumentRefKind == RefKind.None);
                        if (parameterRefKind == RefKind.Ref)
                        {
                            // For ref extension methods, we omit the "ref" modifier on the receiver arguments
                            // Passing the parameter RefKind for finding the correct conversion.
                            // For ref-readonly extension methods, argumentRefKind is always None.
                            argumentRefKind = parameterRefKind;
                        }
                    }

                    conversion = CheckArgumentForApplicability(
                        candidate,
                        argument,
                        argumentRefKind,
                        parameters.ParameterTypes[argumentPosition].Type,
                        parameterRefKind,
                        ignoreOpenTypes,
                        ref useSiteDiagnostics,
                        forExtensionMethodThisArg);

                    if (forExtensionMethodThisArg && !Conversions.IsValidExtensionMethodThisArgConversion(conversion))
                    {
                        // Return early, without checking conversions of subsequent arguments,
                        // if the instance argument is not convertible to the 'this' parameter,
                        // even when 'completeResults' is requested. This avoids unnecessary
                        // lambda binding in particular, for instance, with LINQ expressions.
                        // Note that BuildArgumentsForErrorRecovery will still bind some number
                        // of overloads for the semantic model.
                        Debug.Assert(badArguments == null);
                        Debug.Assert(conversions == null);
                        return MemberAnalysisResult.BadArgumentConversions(argsToParameters, ImmutableArray.Create(argumentPosition), ImmutableArray.Create(conversion));
                    }

                    if (!conversion.Exists)
                    {
                        badArguments = badArguments ?? ArrayBuilder<int>.GetInstance();
                        badArguments.Add(argumentPosition);
                    }
                }

                if (conversions != null)
                {
                    conversions.Add(conversion);
                }
                else if (!conversion.IsIdentity)
                {
                    conversions = ArrayBuilder<Conversion>.GetInstance(paramCount);
                    conversions.AddMany(Conversion.Identity, argumentPosition);
                    conversions.Add(conversion);
                }

                if (badArguments != null && !completeResults)
                {
                    break;
                }
            }

            MemberAnalysisResult result;
            var conversionsArray = conversions != null ? conversions.ToImmutableAndFree() : default(ImmutableArray<Conversion>);
            if (badArguments != null)
            {
                result = MemberAnalysisResult.BadArgumentConversions(argsToParameters, badArguments.ToImmutableAndFree(), conversionsArray);
            }
            else
            {
                result = MemberAnalysisResult.NormalForm(argsToParameters, conversionsArray, hasAnyRefOmittedArgument);
            }

            return result;
        }

        private Conversion CheckArgumentForApplicability(
            Symbol candidate, // method or property
            BoundExpression argument,
            RefKind argRefKind,
            TypeSymbol parameterType,
            RefKind parRefKind,
            bool ignoreOpenTypes,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool forExtensionMethodThisArg)
        {
            // Spec 7.5.3.1
            // For each argument in A, the parameter passing mode of the argument (i.e., value, ref, or out) is identical
            // to the parameter passing mode of the corresponding parameter, and
            // - for a value parameter or a parameter array, an implicit conversion (§6.1)
            //   exists from the argument to the type of the corresponding parameter, or
            // - for a ref or out parameter, the type of the argument is identical to the type of the corresponding parameter. 

            // effective RefKind has to match unless argument expression is of the type dynamic. 
            // This is a bug in Dev11 which we also implement. 
            //       The spec is correct, this is not an intended behavior. We don't fix the bug to avoid a breaking change.
            if (!(argRefKind == parRefKind ||
                 (argRefKind == RefKind.None && argument.HasDynamicType())))
            {
                return Conversion.NoConversion;
            }

            // TODO (tomat): the spec wording isn't final yet

            // Spec 7.5.4: Compile-time checking of dynamic overload resolution:
            // - Then, any parameter whose type is open (i.e. contains a type parameter; see §4.4.2) is elided, along with its corresponding parameter(s).
            // and
            // - The modified parameter list for F is applicable to the modified argument list in terms of section §7.5.3.1
            if (ignoreOpenTypes && parameterType.ContainsTypeParameter(parameterContainer: (MethodSymbol)candidate))
            {
                // defer applicability check to runtime:
                return Conversion.ImplicitDynamic;
            }

            var argType = argument.Type;
            if (argument.Kind == BoundKind.OutVariablePendingInference ||
                argument.Kind == BoundKind.OutDeconstructVarPendingInference ||
                (argument.Kind == BoundKind.DiscardExpression && (object)argType == null))
            {
                Debug.Assert(argRefKind != RefKind.None);

                // Any parameter type is good, we'll use it for the var local.
                return Conversion.Identity;
            }

            if (argRefKind == RefKind.None)
            {
                var conversion = forExtensionMethodThisArg ?
                    Conversions.ClassifyImplicitExtensionMethodThisArgConversion(argument, argument.Type, parameterType, ref useSiteDiagnostics) :
                    Conversions.ClassifyImplicitConversionFromExpression(argument, parameterType, ref useSiteDiagnostics);
                Debug.Assert((!conversion.Exists) || conversion.IsImplicit, "ClassifyImplicitConversion should only return implicit conversions");
                return conversion;
            }

            if ((object)argType != null && Conversions.HasIdentityConversion(argType, parameterType))
            {
                return Conversion.Identity;
            }
            else
            {
                return Conversion.NoConversion;
            }
        }

        private static TMember GetConstructedFrom<TMember>(TMember member) where TMember : Symbol
        {
            switch (member.Kind)
            {
                case SymbolKind.Property:
                    return member;
                case SymbolKind.Method:
                    return (TMember)(Symbol)(member as MethodSymbol).ConstructedFrom;
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }
    }
}
