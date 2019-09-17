// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// Performs name lookup for simple generic or non-generic name
        /// within an optional qualifier namespace or type symbol.
        /// If LookupOption.AttributeTypeOnly is set, then it performs
        /// attribute type lookup which involves attribute name lookup
        /// with and without "Attribute" suffix.
        /// </summary>
        internal void LookupSymbolsSimpleName(
            LookupResult result,
            NamespaceOrTypeSymbol qualifierOpt,
            string plainName,
            int arity,
            ConsList<TypeSymbol> basesBeingResolved,
            LookupOptions options,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (options.IsAttributeTypeLookup())
            {
                this.LookupAttributeType(result, qualifierOpt, plainName, arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);
            }
            else
            {
                this.LookupSymbolsOrMembersInternal(result, qualifierOpt, plainName, arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);
            }
        }

        internal void LookupExtensionMethods(LookupResult result, string name, int arity, LookupOptions options, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            foreach (var scope in new ExtensionMethodScopes(this))
            {
                this.LookupExtensionMethodsInSingleBinder(scope, result, name, arity, options, ref useSiteDiagnostics);
            }
        }

        /// <summary>
        /// Look for any symbols in scope with the given name and arity.
        /// </summary>
        /// <remarks>
        /// Makes a second attempt if the results are not viable, in order to produce more detailed failure information (symbols and diagnostics).
        /// </remarks>
        private Binder LookupSymbolsWithFallback(LookupResult result, string name, int arity, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved = null, LookupOptions options = LookupOptions.Default)
        {
            Debug.Assert(options.AreValid());

            // don't create diagnosis instances unless lookup fails
            var binder = this.LookupSymbolsInternal(result, name, arity, basesBeingResolved, options, diagnose: false, useSiteDiagnostics: ref useSiteDiagnostics);
            Debug.Assert((binder != null) || result.IsClear);

            if (result.Kind != LookupResultKind.Viable && result.Kind != LookupResultKind.Empty)
            {
                result.Clear();
                // retry to get diagnosis
                var otherBinder = this.LookupSymbolsInternal(result, name, arity, basesBeingResolved, options, diagnose: true, useSiteDiagnostics: ref useSiteDiagnostics);
                Debug.Assert(binder == otherBinder);
            }

            Debug.Assert(result.IsMultiViable || result.IsClear || result.Error != null);
            return binder;
        }

        private Binder LookupSymbolsInternal(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(result.IsClear);
            Debug.Assert(options.AreValid());

            Binder binder = null;
            for (var scope = this; scope != null && !result.IsMultiViable; scope = scope.Next)
            {
                if (binder != null)
                {
                    var tmp = LookupResult.GetInstance();
                    scope.LookupSymbolsInSingleBinder(tmp, name, arity, basesBeingResolved, options, this, diagnose, ref useSiteDiagnostics);
                    result.MergeEqual(tmp);
                    tmp.Free();
                }
                else
                {
                    scope.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, this, diagnose, ref useSiteDiagnostics);
                    if (!result.IsClear)
                    {
                        binder = scope;
                    }
                }
            }
            return binder;
        }

        internal virtual void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
        }

        /// <summary>
        /// If qualifierOpt is null, look for any symbols in
        /// scope with the given name and arity.
        /// Otherwise look for symbols that are members of the specified qualifierOpt.
        /// </summary>
        private void LookupSymbolsOrMembersInternal(
            LookupResult result,
            NamespaceOrTypeSymbol qualifierOpt,
            string name,
            int arity,
            ConsList<TypeSymbol> basesBeingResolved,
            LookupOptions options,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((object)qualifierOpt == null)
            {
                this.LookupSymbolsInternal(result, name, arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);
            }
            else
            {
                this.LookupMembersInternal(result, qualifierOpt, name, arity, basesBeingResolved, options, this, diagnose, ref useSiteDiagnostics);
            }
        }

        /// <summary>
        /// Look for symbols that are members of the specified namespace or type.
        /// </summary>
        private void LookupMembersWithFallback(LookupResult result, NamespaceOrTypeSymbol nsOrType, string name, int arity, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved = null, LookupOptions options = LookupOptions.Default)
        {
            Debug.Assert(options.AreValid());

            // don't create diagnosis unless lookup fails
            this.LookupMembersInternal(result, nsOrType, name, arity, basesBeingResolved, options, originalBinder: this, diagnose: false, useSiteDiagnostics: ref useSiteDiagnostics);
            if (!result.IsMultiViable && !result.IsClear)
            {
                result.Clear();
                // retry to get diagnosis
                this.LookupMembersInternal(result, nsOrType, name, arity, basesBeingResolved, options, originalBinder: this, diagnose: true, useSiteDiagnostics: ref useSiteDiagnostics);
            }

            Debug.Assert(result.IsMultiViable || result.IsClear || result.Error != null);
        }

        protected void LookupMembersInternal(LookupResult result, NamespaceOrTypeSymbol nsOrType, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(options.AreValid());

            Debug.Assert(arity >= 0);
            if (nsOrType.IsNamespace)
            {
                LookupMembersInNamespace(result, (NamespaceSymbol)nsOrType, name, arity, options, originalBinder, diagnose, ref useSiteDiagnostics);
            }
            else
            {
                this.LookupMembersInType(result, (TypeSymbol)nsOrType, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
            }
        }

        // Looks up a member of given name and arity in a particular type.
        protected void LookupMembersInType(LookupResult result, TypeSymbol type, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            switch (type.TypeKind)
            {
                case TypeKind.TypeParameter:
                    this.LookupMembersInTypeParameter(result, (TypeParameterSymbol)type, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
                    break;

                case TypeKind.Interface:
                    this.LookupMembersInInterface(result, (NamedTypeSymbol)type, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
                    break;

                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Enum:
                case TypeKind.Delegate:
                case TypeKind.Array:
                case TypeKind.Dynamic:
                    this.LookupMembersInClass(result, type, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
                    break;

                case TypeKind.Submission:
                    this.LookupMembersInSubmissions(result, type, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
                    break;

                case TypeKind.Error:
                    LookupMembersInErrorType(result, (ErrorTypeSymbol)type, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
                    break;

                case TypeKind.Pointer:
                    result.Clear();
                    break;

                case TypeKind.Unknown:
                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }

            // TODO: Diagnose ambiguity problems here, and conflicts between non-method and method? Or is that
            // done in the caller?
        }

        private void LookupMembersInErrorType(LookupResult result, ErrorTypeSymbol errorType, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (!errorType.CandidateSymbols.IsDefault && errorType.CandidateSymbols.Length == 1)
            {
                // The dev11 IDE experience provided meaningful information about members of inaccessible types,
                // so we should do the same (DevDiv #633340).
                // TODO: generalize to other result kinds and/or candidate counts?
                if (errorType.ResultKind == LookupResultKind.Inaccessible)
                {
                    TypeSymbol candidateType = errorType.CandidateSymbols.First() as TypeSymbol;
                    if ((object)candidateType != null)
                    {
                        LookupMembersInType(result, candidateType, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
                        return; // Bypass call to Clear()
                    }
                }
            }

            result.Clear();
        }

        /// <summary>
        /// Lookup a member name in a submission chain.
        /// </summary>
        /// <remarks>
        /// We start with the current submission class and walk the submission chain back to the first submission.
        /// The search has two phases
        /// 1) We are looking for any symbol matching the given name, arity, and options. If we don't find any the search is over.
        ///    If we find and overloadable symbol(s) (a method or an indexer) we start looking for overloads of this kind 
        ///    (lookingForOverloadsOfKind) of symbol in phase 2.
        /// 2) If a visited submission contains a matching member of a kind different from lookingForOverloadsOfKind we stop 
        ///    looking further. Otherwise, if we find viable overload(s) we add them into the result.
        ///    
        /// Note that indexers are not supported in script but we deal with them here to handle errors.
        /// </remarks>
        private void LookupMembersInSubmissions(LookupResult result, TypeSymbol submissionClass, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            LookupResult submissionSymbols = LookupResult.GetInstance();
            LookupResult nonViable = LookupResult.GetInstance();
            SymbolKind? lookingForOverloadsOfKind = null;

            // TODO: optimize lookup (there might be many interactions in the chain)
            for (CSharpCompilation submission = Compilation; submission != null; submission = submission.PreviousSubmission)
            {
                submissionSymbols.Clear();

                var isCurrentSubmission = submission == Compilation;
                var considerUsings = !(isCurrentSubmission && this.Flags.Includes(BinderFlags.InScriptUsing));

                Imports submissionImports;
                if (!considerUsings)
                {
                    submissionImports = Imports.Empty;
                }
                else if (!this.Flags.Includes(BinderFlags.InLoadedSyntaxTree))
                {
                    submissionImports = submission.GetSubmissionImports();
                }
                else if (isCurrentSubmission)
                {
                    submissionImports = this.GetImports(basesBeingResolved);
                }
                else
                {
                    submissionImports = Imports.Empty;
                }

                // If a viable using alias and a matching member are both defined in the submission an error is reported elsewhere.
                // Ignore the member in such case.
                if ((options & LookupOptions.NamespaceAliasesOnly) == 0 && (object)submission.ScriptClass != null)
                {
                    LookupMembersWithoutInheritance(submissionSymbols, submission.ScriptClass, name, arity, options, originalBinder, submissionClass, diagnose, ref useSiteDiagnostics, basesBeingResolved);

                    // NB: It doesn't matter that submissionImports hasn't been expanded since we're not actually using the alias target. 
                    if (submissionSymbols.IsMultiViable &&
                        considerUsings &&
                        submissionImports.IsUsingAlias(name, originalBinder.IsSemanticModelBinder))
                    {
                        // using alias is ambiguous with another definition within the same submission iff the other definition is a 0-ary type or a non-type:
                        Symbol existingDefinition = submissionSymbols.Symbols.First();
                        if (existingDefinition.Kind != SymbolKind.NamedType || arity == 0)
                        {
                            CSDiagnosticInfo diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_ConflictingAliasAndDefinition, name, existingDefinition.GetKindText());
                            var error = new ExtendedErrorTypeSymbol((NamespaceOrTypeSymbol)null, name, arity, diagInfo, unreported: true);
                            result.SetFrom(LookupResult.Good(error)); // force lookup to be done w/ error symbol as result
                            break;
                        }
                    }
                }

                if (!submissionSymbols.IsMultiViable && considerUsings)
                {
                    if (!isCurrentSubmission)
                    {
                        submissionImports = Imports.ExpandPreviousSubmissionImports(submissionImports, Compilation);
                    }

                    // NB: We diverge from InContainerBinder here and only look in aliases.
                    // In submissions, regular usings are bubbled up to the outermost scope.
                    submissionImports.LookupSymbolInAliases(originalBinder, submissionSymbols, name, arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);
                }

                if (lookingForOverloadsOfKind == null)
                {
                    if (!submissionSymbols.IsMultiViable)
                    {
                        // skip non-viable members, but remember them in case no viable members are found in previous submissions:
                        nonViable.MergePrioritized(submissionSymbols);
                        continue;
                    }

                    result.MergeEqual(submissionSymbols);

                    Symbol firstSymbol = submissionSymbols.Symbols.First();
                    if (!IsMethodOrIndexer(firstSymbol))
                    {
                        break;
                    }

                    // we are now looking for any kind of member regardless of the original binding restrictions:
                    options = options & ~(LookupOptions.MustBeInvocableIfMember | LookupOptions.NamespacesOrTypesOnly);
                    lookingForOverloadsOfKind = firstSymbol.Kind;
                }
                else
                {
                    // found a non-method - the overload set is final now
                    if (submissionSymbols.Symbols.Count > 0 && submissionSymbols.Symbols.First().Kind != lookingForOverloadsOfKind.Value)
                    {
                        break;
                    }

                    // found a viable overload:
                    if (submissionSymbols.IsMultiViable)
                    {
                        // merge overloads:
                        Debug.Assert(result.Symbols.All(IsMethodOrIndexer));
                        result.MergeEqual(submissionSymbols);
                    }
                }
            }

            if (result.Symbols.Count == 0)
            {
                result.SetFrom(nonViable);
            }

            submissionSymbols.Free();
            nonViable.Free();
        }

        private static void LookupMembersInNamespace(LookupResult result, NamespaceSymbol ns, string name, int arity, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var members = GetCandidateMembers(ns, name, options, originalBinder);

            foreach (Symbol member in members)
            {
                SingleLookupResult resultOfThisMember = originalBinder.CheckViability(member, arity, options, null, diagnose, ref useSiteDiagnostics);
                result.MergeEqual(resultOfThisMember);
            }
        }

        /// <summary>
        /// Lookup extension methods by name and arity in the given binder and
        /// check viability in this binder. The lookup is performed on a single
        /// binder because extension method search stops at the first applicable
        /// method group from the nearest enclosing namespace.
        /// </summary>
        private void LookupExtensionMethodsInSingleBinder(ExtensionMethodScope scope, LookupResult result, string name, int arity, LookupOptions options, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var methods = ArrayBuilder<MethodSymbol>.GetInstance();
            var binder = scope.Binder;
            binder.GetCandidateExtensionMethods(scope.SearchUsingsNotNamespace, methods, name, arity, options, this);

            foreach (var method in methods)
            {
                SingleLookupResult resultOfThisMember = this.CheckViability(method, arity, options, null, diagnose: true, useSiteDiagnostics: ref useSiteDiagnostics);
                result.MergeEqual(resultOfThisMember);
            }

            methods.Free();
        }

        #region "AttributeTypeLookup"

        /// <summary>
        /// Lookup attribute name in the given binder. By default two name lookups are performed:
        ///     (1) With the provided name
        ///     (2) With an Attribute suffix added to the provided name
        /// Lookup with Attribute suffix is performed only if LookupOptions.VerbatimAttributeName is not set.
        /// 
        /// If either lookup is ambiguous, we return the corresponding result with ambiguous symbols.
        /// Else if exactly one result is single viable attribute type, we return that result.
        /// Otherwise, we return a non-viable result with LookupResult.NotAnAttributeType or an empty result.
        /// </summary>
        private void LookupAttributeType(
            LookupResult result,
            NamespaceOrTypeSymbol qualifierOpt,
            string name,
            int arity,
            ConsList<TypeSymbol> basesBeingResolved,
            LookupOptions options,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(result.IsClear);
            Debug.Assert(options.AreValid());
            Debug.Assert(options.IsAttributeTypeLookup());

            //  SPEC:   By convention, attribute classes are named with a suffix of Attribute. 
            //  SPEC:   An attribute-name of the form type-name may either include or omit this suffix.
            //  SPEC:   If an attribute class is found both with and without this suffix, an ambiguity 
            //  SPEC:   is present, and a compile-time error results. If the attribute-name is spelled
            //  SPEC:   such that its right-most identifier is a verbatim identifier (§2.4.2), then only
            //  SPEC:   an attribute without a suffix is matched, thus enabling such an ambiguity to be resolved.

            // Roslyn Bug 9681: Compilers incorrectly use the *failure* of binding some subexpression to indicate some other strategy is applicable (attributes, 'var')

            // Roslyn reproduces Dev10 compiler behavior which doesn't report an error if one of the 
            // lookups is single viable and other lookup is ambiguous. If one of the lookup results 
            // (either with or without "Attribute" suffix) is single viable and is an attribute type we 
            // use it  disregarding the second result which may be ambiguous. 

            // Note: if both are single and attribute types, we still report ambiguity.

            // Lookup symbols without attribute suffix.
            LookupSymbolsOrMembersInternal(result, qualifierOpt, name, arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);

            // Result without 'Attribute' suffix added.
            Symbol symbolWithoutSuffix;
            bool resultWithoutSuffixIsViable = IsSingleViableAttributeType(result, out symbolWithoutSuffix);

            // Generic types are not allowed.
            Debug.Assert(arity == 0 || !result.IsMultiViable);

            // Result with 'Attribute' suffix added.
            LookupResult resultWithSuffix = null;
            Symbol symbolWithSuffix = null;
            bool resultWithSuffixIsViable = false;
            if (!options.IsVerbatimNameAttributeTypeLookup())
            {
                resultWithSuffix = LookupResult.GetInstance();
                this.LookupSymbolsOrMembersInternal(resultWithSuffix, qualifierOpt, name + "Attribute", arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);
                resultWithSuffixIsViable = IsSingleViableAttributeType(resultWithSuffix, out symbolWithSuffix);

                // Generic types are not allowed.
                Debug.Assert(arity == 0 || !result.IsMultiViable);
            }

            if (resultWithoutSuffixIsViable && resultWithSuffixIsViable)
            {
                // Single viable lookup symbol found both with and without Attribute suffix.
                // We merge both results, ambiguity error will be reported later in ResultSymbol.
                result.MergeEqual(resultWithSuffix);
            }
            else if (resultWithoutSuffixIsViable)
            {
                // single viable lookup symbol only found without Attribute suffix, return result.
            }
            else if (resultWithSuffixIsViable)
            {
                Debug.Assert(resultWithSuffix != null);

                // Single viable lookup symbol only found with Attribute suffix, return resultWithSuffix.
                result.SetFrom(resultWithSuffix);
            }
            else
            {
                // Both results are clear, non-viable or ambiguous.

                if (!result.IsClear)
                {
                    if ((object)symbolWithoutSuffix != null) // was not ambiguous, but not viable
                    {
                        result.SetFrom(GenerateNonViableAttributeTypeResult(symbolWithoutSuffix, result.Error, diagnose));
                    }
                }

                if (resultWithSuffix != null)
                {
                    if (!resultWithSuffix.IsClear)
                    {
                        if ((object)symbolWithSuffix != null)
                        {
                            resultWithSuffix.SetFrom(GenerateNonViableAttributeTypeResult(symbolWithSuffix, resultWithSuffix.Error, diagnose));
                        }
                    }

                    result.MergePrioritized(resultWithSuffix);
                }
            }

            resultWithSuffix?.Free();
        }

        private bool IsAmbiguousResult(LookupResult result, out Symbol resultSymbol)
        {
            resultSymbol = null;
            var symbols = result.Symbols;

            switch (symbols.Count)
            {
                case 0:
                    return false;
                case 1:
                    resultSymbol = symbols[0];
                    return false;
                default:
                    // If there are two or more symbols in the result, one from source and others from PE,
                    // then the source symbol overrides the PE symbols and must be chosen.
                    // NOTE: Kind of the symbol doesn't matter here. If the resolved symbol is not an attribute type, we will subsequently generate a lookup error.

                    // CONSIDER: If this source symbol is the eventual result symbol for attribute type lookup and it is not a valid attribute type,
                    // CONSIDER: we generate an error but don't generate warning CS0436 for source/PE name conflict.
                    // CONSIDER: We may want to also generate CS0436 for this case.

                    resultSymbol = ResolveMultipleSymbolsInAttributeTypeLookup(symbols);
                    return (object)resultSymbol == null;
            }
        }

        private Symbol ResolveMultipleSymbolsInAttributeTypeLookup(ArrayBuilder<Symbol> symbols)
        {
            Debug.Assert(symbols.Count >= 2);

            var originalSymbols = symbols.ToImmutable();

            for (int i = 0; i < symbols.Count; i++)
            {
                symbols[i] = UnwrapAliasNoDiagnostics(symbols[i]);
            }

            BestSymbolInfo secondBest;
            BestSymbolInfo best = GetBestSymbolInfo(symbols, out secondBest);

            Debug.Assert(!best.IsNone);
            Debug.Assert(!secondBest.IsNone);

            if (best.IsFromCompilation && !secondBest.IsFromCompilation)
            {
                var srcSymbol = symbols[best.Index];
                var mdSymbol = symbols[secondBest.Index];

                //if names match, arities match, and containing symbols match (recursively), ...
                if (srcSymbol.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat) ==
                    mdSymbol.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat))
                {
                    return originalSymbols[best.Index];
                }
            }

            return null;
        }

        private bool IsSingleViableAttributeType(LookupResult result, out Symbol symbol)
        {
            if (IsAmbiguousResult(result, out symbol))
            {
                return false;
            }

            if (result == null || result.Kind != LookupResultKind.Viable || (object)symbol == null)
            {
                return false;
            }

            DiagnosticInfo discarded = null;
            return CheckAttributeTypeViability(UnwrapAliasNoDiagnostics(symbol), diagnose: false, diagInfo: ref discarded);
        }

        private SingleLookupResult GenerateNonViableAttributeTypeResult(Symbol symbol, DiagnosticInfo diagInfo, bool diagnose)
        {
            Debug.Assert((object)symbol != null);

            symbol = UnwrapAliasNoDiagnostics(symbol);
            CheckAttributeTypeViability(symbol, diagnose, ref diagInfo);
            return LookupResult.NotAnAttributeType(symbol, diagInfo);
        }

        private bool CheckAttributeTypeViability(Symbol symbol, bool diagnose, ref DiagnosticInfo diagInfo)
        {
            Debug.Assert((object)symbol != null);

            if (symbol.Kind == SymbolKind.NamedType)
            {
                var namedType = (NamedTypeSymbol)symbol;
                if (namedType.IsGenericType)
                {
                    // Attribute classes cannot be generic.
                    diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_AttributeCantBeGeneric, symbol) : null;
                    return false;
                }
                else if (namedType.IsAbstract)
                {
                    // Attribute class cannot be abstract.
                    diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_AbstractAttributeClass, symbol) : null;
                    return false;
                }
                else
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                    if (Compilation.IsEqualOrDerivedFromWellKnownClass(namedType, WellKnownType.System_Attribute, ref useSiteDiagnostics))
                    {
                        // Reuse existing diagnostic info.
                        return true;
                    }

                    if (diagnose && !useSiteDiagnostics.IsNullOrEmpty())
                    {
                        foreach (var info in useSiteDiagnostics)
                        {
                            if (info.Severity == DiagnosticSeverity.Error)
                            {
                                diagInfo = info;
                                return false;
                            }
                        }
                    }
                }
            }

            diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_NotAnAttributeClass, symbol) : null;
            return false;
        }

        #endregion

        internal virtual bool SupportsExtensionMethods
        {
            get { return false; }
        }

        /// <summary>
        /// Return the extension methods from this specific binding scope that match the name and optional
        /// arity. Since the lookup of extension methods is iterative, proceeding one binding scope at a time,
        /// GetCandidateExtensionMethods should not defer to the next binding scope. Instead, the caller is
        /// responsible for walking the nested binding scopes from innermost to outermost. This method is overridden
        /// to search the available members list in binding types that represent types, namespaces, and usings.
        /// </summary>
        internal virtual void GetCandidateExtensionMethods(
            bool searchUsingsNotNamespace,
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            Binder originalBinder)
        {
        }

        // Does a member lookup in a single type, without considering inheritance.
        protected static void LookupMembersWithoutInheritance(LookupResult result, TypeSymbol type, string name, int arity,
            LookupOptions options, Binder originalBinder, TypeSymbol accessThroughType, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved)
        {
            var members = GetCandidateMembers(type, name, options, originalBinder);

            foreach (Symbol member in members)
            {
                // Do we need to exclude override members, or is that done later by overload resolution. It seems like
                // not excluding them here can't lead to problems, because we will always find the overridden method as well.
                SingleLookupResult resultOfThisMember = originalBinder.CheckViability(member, arity, options, accessThroughType, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                result.MergeEqual(resultOfThisMember);
            }
        }

        // Lookup member in a class, struct, enum, delegate.
        private void LookupMembersInClass(
            LookupResult result,
            TypeSymbol type,
            string name,
            int arity,
            ConsList<TypeSymbol> basesBeingResolved,
            LookupOptions options,
            Binder originalBinder,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            LookupMembersInClass(result, type, name, arity, basesBeingResolved, options, originalBinder, type, diagnose, ref useSiteDiagnostics);
        }

        // Lookup member in a class, struct, enum, delegate.
        private void LookupMembersInClass(
            LookupResult result,
            TypeSymbol type,
            string name,
            int arity,
            ConsList<TypeSymbol> basesBeingResolved,
            LookupOptions options,
            Binder originalBinder,
            TypeSymbol accessThroughType,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(!type.IsInterfaceType() && type.TypeKind != TypeKind.TypeParameter);

            TypeSymbol currentType = type;

            var tmp = LookupResult.GetInstance();
            PooledHashSet<NamedTypeSymbol> visited = null;
            while ((object)currentType != null)
            {
                tmp.Clear();
                LookupMembersWithoutInheritance(tmp, currentType, name, arity, options, originalBinder, accessThroughType, diagnose, ref useSiteDiagnostics, basesBeingResolved);

                MergeHidingLookupResults(result, tmp, basesBeingResolved, ref useSiteDiagnostics);

                // If the type is from a winmd and implements any of the special WinRT collection
                // projections then we may need to add underlying interface members. 
                NamedTypeSymbol namedType = currentType as NamedTypeSymbol;
                if (namedType?.ShouldAddWinRTMembers == true)
                {
                    AddWinRTMembers(result, namedType, name, arity, options, originalBinder, diagnose, ref useSiteDiagnostics);
                }

                // any viable non-methods [non-indexers] found here will hide viable methods [indexers] (with the same name) in any further base classes
                bool tmpHidesMethodOrIndexers = tmp.IsMultiViable && !IsMethodOrIndexer(tmp.Symbols[0]);

                // short circuit looking up bases if we already have a viable result and we won't be adding on more
                if (result.IsMultiViable && (tmpHidesMethodOrIndexers || !IsMethodOrIndexer(result.Symbols[0])))
                {
                    break;
                }

                if (basesBeingResolved != null && basesBeingResolved.ContainsReference(type.OriginalDefinition))
                {
                    var other = GetNearestOtherSymbol(basesBeingResolved, type);
                    var diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_CircularBase, type, other);
                    var error = new ExtendedErrorTypeSymbol(this.Compilation, name, arity, diagInfo, unreported: true);
                    result.SetFrom(LookupResult.Good(error)); // force lookup to be done w/ error symbol as result
                }

                // As in dev11, we don't consider inherited members within crefs.
                // CAVEAT: dev11 appears to ignore this rule within parameter types and return types,
                // so we're checking Cref, rather than Cref and CrefParameterOrReturnType.
                if (originalBinder.InCrefButNotParameterOrReturnType)
                {
                    break;
                }

                currentType = currentType.GetNextBaseTypeNoUseSiteDiagnostics(basesBeingResolved, this.Compilation, ref visited);
                if ((object)currentType != null)
                {
                    currentType.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
                }
            }

            visited?.Free();
            tmp.Free();
        }

        /// <summary>
        /// If the type implements one of a select few WinRT interfaces, the interface type is
        /// projected to the CLR collection type (e.g., IVector to IList).
        /// When importing a winmd type it may implement one or more winmd collection
        /// interfaces. When the collection interfaces are projected, we may need
        /// to add the projected members to the imported type so that calls to those
        /// members succeed as normal. This method adds the interface methods to
        /// the lookup, if necessary. The CLR understands that a call to the .NET interface
        /// should be projected onto the WinRT interface method.
        /// </summary>
        private void AddWinRTMembers(
            LookupResult result,
            NamedTypeSymbol type,
            string name,
            int arity,
            LookupOptions options,
            Binder originalBinder,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // While the fundamental idea is simple, the implementation has issues.
            // If we have no conflict with existing members, we also have to check
            // if we have a conflict with other interface members. An example would be
            // a type which implements both IIterable (IEnumerable) and IMap 
            // (IDictionary).There are two different GetEnumerator methods from each
            // interface. Thus, we don't know which method to choose. The solution?
            // Don't add any GetEnumerator method.

            var comparer = MemberSignatureComparer.CSharpOverrideComparer;

            var allMembers = new HashSet<Symbol>(comparer);
            var conflictingMembers = new HashSet<Symbol>(comparer);

            // Add all viable members from type lookup
            if (result.IsMultiViable)
            {
                foreach (var sym in result.Symbols)
                {
                    // Fields can't be present in the HashSet because they can't be compared
                    // with a MemberSignatureComparer
                    if (sym.Kind == SymbolKind.Method || sym.Kind == SymbolKind.Property)
                    {
                        allMembers.Add(sym);
                    }
                }
            }

            var tmp = LookupResult.GetInstance();

            NamedTypeSymbol idictSymbol, iroDictSymbol, iListSymbol, iCollectionSymbol, inccSymbol, inpcSymbol;
            GetWellKnownWinRTMemberInterfaces(out idictSymbol, out iroDictSymbol, out iListSymbol, out iCollectionSymbol, out inccSymbol, out inpcSymbol);

            // Dev11 searches all declared and undeclared base interfaces
            foreach (var iface in type.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (ShouldAddWinRTMembersForInterface(iface, idictSymbol, iroDictSymbol, iListSymbol, iCollectionSymbol, inccSymbol, inpcSymbol))
                {
                    LookupMembersWithoutInheritance(tmp, iface, name, arity, options, originalBinder, iface, diagnose, ref useSiteDiagnostics, basesBeingResolved: null);
                    // only add viable members
                    if (tmp.IsMultiViable)
                    {
                        foreach (var sym in tmp.Symbols)
                        {
                            if (!allMembers.Add(sym))
                            {
                                conflictingMembers.Add(sym);
                            }
                        }
                    }
                    tmp.Clear();
                }
            }
            tmp.Free();
            if (result.IsMultiViable)
            {
                foreach (var sym in result.Symbols)
                {
                    if (sym.Kind == SymbolKind.Method || sym.Kind == SymbolKind.Property)
                    {
                        allMembers.Remove(sym);
                        conflictingMembers.Remove(sym);
                    }
                }
            }
            foreach (var sym in allMembers)
            {
                if (!conflictingMembers.Contains(sym))
                {
                    // since we only added viable members, every lookupresult should be viable
                    result.MergeEqual(new SingleLookupResult(LookupResultKind.Viable, sym, null));
                }
            }
        }

        private void GetWellKnownWinRTMemberInterfaces(out NamedTypeSymbol idictSymbol, out NamedTypeSymbol iroDictSymbol, out NamedTypeSymbol iListSymbol, out NamedTypeSymbol iCollectionSymbol, out NamedTypeSymbol inccSymbol, out NamedTypeSymbol inpcSymbol)
        {
            idictSymbol = Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IDictionary_KV);
            iroDictSymbol = Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IReadOnlyDictionary_KV);
            iListSymbol = Compilation.GetWellKnownType(WellKnownType.System_Collections_IList);
            iCollectionSymbol = Compilation.GetWellKnownType(WellKnownType.System_Collections_ICollection);
            inccSymbol = Compilation.GetWellKnownType(WellKnownType.System_Collections_Specialized_INotifyCollectionChanged);
            inpcSymbol = Compilation.GetWellKnownType(WellKnownType.System_ComponentModel_INotifyPropertyChanged);
        }

        private static bool ShouldAddWinRTMembersForInterface(NamedTypeSymbol iface, NamedTypeSymbol idictSymbol, NamedTypeSymbol iroDictSymbol, NamedTypeSymbol iListSymbol, NamedTypeSymbol iCollectionSymbol, NamedTypeSymbol inccSymbol, NamedTypeSymbol inpcSymbol)
        {
            var iFaceOriginal = iface.OriginalDefinition;
            var iFaceSpecial = iFaceOriginal.SpecialType;

            // Types match the list given in dev11 IMPORTER::GetWindowsRuntimeInterfacesToFake
            return iFaceSpecial == SpecialType.System_Collections_Generic_IEnumerable_T ||
                   iFaceSpecial == SpecialType.System_Collections_Generic_IList_T ||
                   iFaceSpecial == SpecialType.System_Collections_Generic_ICollection_T ||
                   TypeSymbol.Equals(iFaceOriginal, idictSymbol, TypeCompareKind.ConsiderEverything2) ||
                   iFaceSpecial == SpecialType.System_Collections_Generic_IReadOnlyList_T ||
                   iFaceSpecial == SpecialType.System_Collections_Generic_IReadOnlyCollection_T ||
                   TypeSymbol.Equals(iFaceOriginal, iroDictSymbol, TypeCompareKind.ConsiderEverything2) ||
                   iFaceSpecial == SpecialType.System_Collections_IEnumerable ||
                   TypeSymbol.Equals(iFaceOriginal, iListSymbol, TypeCompareKind.ConsiderEverything2) ||
                   TypeSymbol.Equals(iFaceOriginal, iCollectionSymbol, TypeCompareKind.ConsiderEverything2) ||
                   TypeSymbol.Equals(iFaceOriginal, inccSymbol, TypeCompareKind.ConsiderEverything2) ||
                   TypeSymbol.Equals(iFaceOriginal, inpcSymbol, TypeCompareKind.ConsiderEverything2);
        }


        // find the nearest symbol in list to the symbol 'type'.  It may be the same symbol if its the only one.
        private static Symbol GetNearestOtherSymbol(ConsList<TypeSymbol> list, TypeSymbol type)
        {
            TypeSymbol other = type;

            for (; list != null && list != ConsList<TypeSymbol>.Empty; list = list.Tail)
            {
                if (TypeSymbol.Equals(list.Head, type.OriginalDefinition, TypeCompareKind.ConsiderEverything2))
                {
                    if (TypeSymbol.Equals(other, type, TypeCompareKind.ConsiderEverything2) && list.Tail != null && list.Tail != ConsList<TypeSymbol>.Empty)
                    {
                        other = list.Tail.Head;
                    }
                    break;
                }
                else
                {
                    other = list.Head;
                }
            }

            return other;
        }

        // Lookup member in interface, and any base interfaces.
        private static void LookupMembersInInterfaceOnly(
            LookupResult current,
            NamedTypeSymbol type,
            string name,
            int arity,
            ConsList<TypeSymbol> basesBeingResolved,
            LookupOptions options,
            Binder originalBinder,
            TypeSymbol accessThroughType,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(type.IsInterface);

            LookupMembersWithoutInheritance(current, type, name, arity, options, originalBinder, accessThroughType, diagnose, ref useSiteDiagnostics, basesBeingResolved);
            if ((options & LookupOptions.NamespaceAliasesOnly) == 0 && !originalBinder.InCrefButNotParameterOrReturnType)
            {
                LookupMembersInInterfacesWithoutInheritance(current, GetBaseInterfaces(type, basesBeingResolved, ref useSiteDiagnostics),
                    name, arity, basesBeingResolved, options, originalBinder, accessThroughType, diagnose, ref useSiteDiagnostics);
            }

        }

        private static ImmutableArray<NamedTypeSymbol> GetBaseInterfaces(NamedTypeSymbol type, ConsList<TypeSymbol> basesBeingResolved, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (basesBeingResolved?.Any() != true)
            {
                return type.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            if (basesBeingResolved.ContainsReference(type.OriginalDefinition))
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            var interfaces = type.GetDeclaredInterfaces(basesBeingResolved);

            if (interfaces.IsEmpty)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            var cycleGuard = ImmutableHashSet.Create(type.OriginalDefinition);

            // Consumers of the result depend on the sorting performed by AllInterfacesWithDefinitionUseSiteDiagnostics.
            // Let's use similar sort algorithm.
            var result = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            var visited = new HashSet<NamedTypeSymbol>(TypeSymbol.EqualsConsiderEverything);

            for (int i = interfaces.Length - 1; i >= 0; i--)
            {
                addAllInterfaces(interfaces[i], visited, result, basesBeingResolved, cycleGuard);
            }

            result.ReverseContents();

            foreach (var candidate in result)
            {
                candidate.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result.ToImmutableAndFree();

            static void addAllInterfaces(NamedTypeSymbol @interface, HashSet<NamedTypeSymbol> visited, ArrayBuilder<NamedTypeSymbol> result, ConsList<TypeSymbol> basesBeingResolved, ImmutableHashSet<NamedTypeSymbol> cycleGuard)
            {
                if (@interface.IsInterface && !cycleGuard.Contains(@interface.OriginalDefinition) && visited.Add(@interface))
                {
                    if (!basesBeingResolved.ContainsReference(@interface.OriginalDefinition))
                    {
                        ImmutableArray<NamedTypeSymbol> baseInterfaces = @interface.GetDeclaredInterfaces(basesBeingResolved);

                        if (!baseInterfaces.IsEmpty)
                        {
                            cycleGuard = cycleGuard.Add(@interface.OriginalDefinition);
                            for (int i = baseInterfaces.Length - 1; i >= 0; i--)
                            {
                                var baseInterface = baseInterfaces[i];
                                addAllInterfaces(baseInterface, visited, result, basesBeingResolved, cycleGuard);
                            }
                        }
                    }

                    result.Add(@interface);
                }
            }
        }

        private static void LookupMembersInInterfacesWithoutInheritance(
            LookupResult current,
            ImmutableArray<NamedTypeSymbol> interfaces,
            string name,
            int arity,
            ConsList<TypeSymbol> basesBeingResolved,
            LookupOptions options,
            Binder originalBinder,
            TypeSymbol accessThroughType,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (interfaces.Length > 0)
            {
                var tmp = LookupResult.GetInstance();
                foreach (TypeSymbol baseInterface in interfaces)
                {
                    LookupMembersWithoutInheritance(tmp, baseInterface, name, arity, options, originalBinder, accessThroughType, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                    MergeHidingLookupResults(current, tmp, basesBeingResolved, ref useSiteDiagnostics);
                    tmp.Clear();
                }
                tmp.Free();
            }
        }

        // Lookup member in interface, and any base interfaces, and System.Object.
        private void LookupMembersInInterface(LookupResult current, NamedTypeSymbol type, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(type.IsInterface);

            LookupMembersInInterfaceOnly(current, type, name, arity, basesBeingResolved, options, originalBinder, type, diagnose, ref useSiteDiagnostics);

            if (!originalBinder.InCrefButNotParameterOrReturnType)
            {
                var tmp = LookupResult.GetInstance();
                // NB: we assume use-site-errors on System.Object, if any, have been reported earlier.
                this.LookupMembersInClass(tmp, this.Compilation.GetSpecialType(SpecialType.System_Object), name, arity, basesBeingResolved, options, originalBinder, type, diagnose, ref useSiteDiagnostics);
                MergeHidingLookupResults(current, tmp, basesBeingResolved, ref useSiteDiagnostics);
                tmp.Free();
            }
        }

        // Lookup member in type parameter
        private void LookupMembersInTypeParameter(LookupResult current, TypeParameterSymbol typeParameter, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)typeParameter != null);

            if ((options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly)) != 0)
            {
                return;
            }

            // If this ever happens, just return immediately since cref lookup ignores inherited members.
            Debug.Assert(!originalBinder.InCref, "Can't dot into type parameters, so how can this happen?");

            // The result is the accessible members from the effective base class and
            // effective interfaces. AllEffectiveInterfaces is used rather than AllInterfaces
            // to avoid including explicit implementations from the effective base class.
            LookupMembersInClass(current, typeParameter.EffectiveBaseClass(ref useSiteDiagnostics), name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
            LookupMembersInInterfacesWithoutInheritance(current, typeParameter.AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics), name, arity, basesBeingResolved: null, options, originalBinder, typeParameter, diagnose, ref useSiteDiagnostics);
        }

        private static bool IsDerivedType(NamedTypeSymbol baseType, NamedTypeSymbol derivedType, ConsList<TypeSymbol> basesBeingResolved, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(!TypeSymbol.Equals(baseType, derivedType, TypeCompareKind.ConsiderEverything2));
            for (NamedTypeSymbol b = derivedType.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics); (object)b != null; b = b.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (TypeSymbol.Equals(b, baseType, TypeCompareKind.ConsiderEverything2)) return true;
            }
            return baseType.IsInterface && GetBaseInterfaces(derivedType, basesBeingResolved, ref useSiteDiagnostics).Contains(baseType);
        }

        // Merge resultHidden into resultHiding, whereby viable results in resultHiding should hide results
        // in resultHidden if the owner of the symbol in resultHiding is a subtype of the owner of the symbol
        // in resultHidden. We merge together methods [indexers], but non-methods [non-indexers] hide everything and methods [indexers] hide non-methods [non-indexers].
        private static void MergeHidingLookupResults(LookupResult resultHiding, LookupResult resultHidden, ConsList<TypeSymbol> basesBeingResolved, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Methods hide non-methods, non-methods hide everything.

            if (resultHiding.IsMultiViable && resultHidden.IsMultiViable)
            {
                // Check if resultHiding has any non-methods. If so, it hides everything in resultHidden.
                var hidingSymbols = resultHiding.Symbols;
                var hidingCount = hidingSymbols.Count;
                var hiddenSymbols = resultHidden.Symbols;
                var hiddenCount = hiddenSymbols.Count;
                for (int i = 0; i < hiddenCount; i++)
                {
                    var sym = hiddenSymbols[i];
                    var hiddenContainer = sym.ContainingType;

                    // see if sym is hidden
                    for (int j = 0; j < hidingCount; j++)
                    {
                        var hidingSym = hidingSymbols[j];
                        var hidingContainer = hidingSym.ContainingType;
                        var hidingContainerIsInterface = hidingContainer.IsInterface;

                        if (hidingContainerIsInterface)
                        {
                            // SPEC: For the purposes of member lookup [...] if T is an
                            // SPEC: interface type, the base types of T are the base interfaces
                            // SPEC: of T and the class type object. 

                            if (!IsDerivedType(baseType: hiddenContainer, derivedType: hidingSym.ContainingType, basesBeingResolved, useSiteDiagnostics: ref useSiteDiagnostics) &&
                                hiddenContainer.SpecialType != SpecialType.System_Object)
                            {
                                continue; // not in inheritance relationship, so it cannot hide
                            }
                        }

                        if (!IsMethodOrIndexer(hidingSym) || !IsMethodOrIndexer(sym))
                        {
                            // any non-method [non-indexer] hides everything in the hiding scope
                            // any method [indexer] hides non-methods [non-indexers].
                            goto symIsHidden;
                        }

                        // Note: We do not implement hiding by signature in non-interfaces here; that is handled later in overload lookup.
                    }

                    hidingSymbols.Add(sym); // not hidden
symIsHidden:;
                }
            }
            else
            {
                resultHiding.MergePrioritized(resultHidden);
            }
        }

        /// <summary>
        /// This helper is used to determine whether this symbol hides / is hidden
        /// based on its signature, as opposed to its name.
        /// </summary>
        /// <remarks>
        /// CONSIDER: It might be nice to generalize this - maybe an extension method
        /// on Symbol (e.g. IsOverloadable or HidesByName).
        /// </remarks>
        private static bool IsMethodOrIndexer(Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Method || symbol.IsIndexer();
        }

        internal static ImmutableArray<Symbol> GetCandidateMembers(NamespaceOrTypeSymbol nsOrType, string name, LookupOptions options, Binder originalBinder)
        {
            if ((options & LookupOptions.NamespacesOrTypesOnly) != 0 && nsOrType is TypeSymbol)
            {
                return nsOrType.GetTypeMembers(name).Cast<NamedTypeSymbol, Symbol>();
            }
            else if (nsOrType.Kind == SymbolKind.NamedType && originalBinder.IsEarlyAttributeBinder)
            {
                return ((NamedTypeSymbol)nsOrType).GetEarlyAttributeDecodingMembers(name);
            }
            else if ((options & LookupOptions.LabelsOnly) != 0)
            {
                return ImmutableArray<Symbol>.Empty;
            }
            else
            {
                return nsOrType.GetMembers(name);
            }
        }

        internal static ImmutableArray<Symbol> GetCandidateMembers(NamespaceOrTypeSymbol nsOrType, LookupOptions options, Binder originalBinder)
        {
            if ((options & LookupOptions.NamespacesOrTypesOnly) != 0 && nsOrType is TypeSymbol)
            {
                return StaticCast<Symbol>.From(nsOrType.GetTypeMembersUnordered());
            }
            else if (nsOrType.Kind == SymbolKind.NamedType && originalBinder.IsEarlyAttributeBinder)
            {
                return ((NamedTypeSymbol)nsOrType).GetEarlyAttributeDecodingMembers();
            }
            else if ((options & LookupOptions.LabelsOnly) != 0)
            {
                return ImmutableArray<Symbol>.Empty;
            }
            else
            {
                return nsOrType.GetMembersUnordered();
            }
        }

        /// <remarks>
        /// Distinguish from <see cref="CanAddLookupSymbolInfo"/>, which performs an analogous task for Add*LookupSymbolsInfo*.
        /// </remarks>
        internal SingleLookupResult CheckViability(Symbol symbol, int arity, LookupOptions options, TypeSymbol accessThroughType, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved = null)
        {
            bool inaccessibleViaQualifier;
            DiagnosticInfo diagInfo;

            // General pattern: checks and diagnostics refer to unwrapped symbol,
            // but lookup results refer to symbol.

            var unwrappedSymbol = symbol.Kind == SymbolKind.Alias
                ? ((AliasSymbol)symbol).GetAliasTarget(basesBeingResolved)
                : symbol;

            // Check for symbols marked with 'Microsoft.CodeAnalysis.Embedded' attribute
            if (!this.Compilation.SourceModule.Equals(unwrappedSymbol.ContainingModule) && unwrappedSymbol.IsHiddenByCodeAnalysisEmbeddedAttribute())
            {
                return LookupResult.Empty();
            }
            else if (WrongArity(symbol, arity, diagnose, options, out diagInfo))
            {
                return LookupResult.WrongArity(symbol, diagInfo);
            }
            else if (!InCref && !unwrappedSymbol.CanBeReferencedByNameIgnoringIllegalCharacters)
            {
                // Strictly speaking, this test should actually check CanBeReferencedByName.
                // However, we don't want to pay that cost in cases where the lookup is based
                // on a provided name.  As a result, we skip the character check here and let
                // SemanticModel.LookupNames filter out invalid names before returning.

                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_CantCallSpecialMethod, unwrappedSymbol) : null;
                return LookupResult.NotReferencable(symbol, diagInfo);
            }
            else if ((options & LookupOptions.NamespacesOrTypesOnly) != 0 && !(unwrappedSymbol is NamespaceOrTypeSymbol))
            {
                return LookupResult.NotTypeOrNamespace(unwrappedSymbol, symbol, diagnose);
            }
            else if ((options & LookupOptions.MustBeInvocableIfMember) != 0
                && IsNonInvocableMember(unwrappedSymbol))
            {
                return LookupResult.NotInvocable(unwrappedSymbol, symbol, diagnose);
            }
            else if (InCref && !this.IsCrefAccessible(unwrappedSymbol))
            {
                var unwrappedSymbols = ImmutableArray.Create<Symbol>(unwrappedSymbol);
                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_BadAccess, new[] { unwrappedSymbol }, unwrappedSymbols, additionalLocations: ImmutableArray<Location>.Empty) : null;
                return LookupResult.Inaccessible(symbol, diagInfo);
            }
            else if (!InCref &&
                     !this.IsAccessible(unwrappedSymbol,
                                        RefineAccessThroughType(options, accessThroughType),
                                        out inaccessibleViaQualifier,
                                        ref useSiteDiagnostics,
                                        basesBeingResolved))
            {
                if (!diagnose)
                {
                    diagInfo = null;
                }
                else if (inaccessibleViaQualifier)
                {
                    diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadProtectedAccess, unwrappedSymbol, accessThroughType, this.ContainingType);
                }
                else if (IsBadIvtSpecification())
                {
                    diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_FriendRefNotEqualToThis, unwrappedSymbol.ContainingAssembly.Identity.ToString(), AssemblyIdentity.PublicKeyToString(this.Compilation.Assembly.PublicKey));
                }
                else
                {
                    diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_BadAccess, new[] { unwrappedSymbol }, ImmutableArray.Create<Symbol>(unwrappedSymbol), additionalLocations: ImmutableArray<Location>.Empty);
                }

                return LookupResult.Inaccessible(symbol, diagInfo);
            }
            else if (!InCref && unwrappedSymbol.MustCallMethodsDirectly())
            {
                diagInfo = diagnose ? MakeCallMethodsDirectlyDiagnostic(unwrappedSymbol) : null;
                return LookupResult.NotReferencable(symbol, diagInfo);
            }
            else if ((options & LookupOptions.MustBeInstance) != 0 && !IsInstance(unwrappedSymbol))
            {
                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_ObjectRequired, unwrappedSymbol) : null;
                return LookupResult.StaticInstanceMismatch(symbol, diagInfo);
            }
            else if ((options & LookupOptions.MustNotBeInstance) != 0 && IsInstance(unwrappedSymbol))
            {
                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_ObjectProhibited, unwrappedSymbol) : null;
                return LookupResult.StaticInstanceMismatch(symbol, diagInfo);
            }
            else if ((options & LookupOptions.MustNotBeNamespace) != 0 && unwrappedSymbol.Kind == SymbolKind.Namespace)
            {
                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_BadSKunknown, unwrappedSymbol, unwrappedSymbol.GetKindText()) : null;
                return LookupResult.NotTypeOrNamespace(symbol, diagInfo);
            }
            else if ((options & LookupOptions.LabelsOnly) != 0 && unwrappedSymbol.Kind != SymbolKind.Label)
            {
                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_LabelNotFound, unwrappedSymbol.Name) : null;
                return LookupResult.NotLabel(symbol, diagInfo);
            }
            else
            {
                return LookupResult.Good(symbol);
            }

            bool IsBadIvtSpecification()
            {
                // Ensures that during binding we don't ask for public key which results in attribute binding and stack overflow.
                // If looking up attributes, don't ask for public key.
                if ((unwrappedSymbol.DeclaredAccessibility == Accessibility.Internal ||
                    unwrappedSymbol.DeclaredAccessibility == Accessibility.ProtectedAndInternal ||
                    unwrappedSymbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                    && !options.IsAttributeTypeLookup())
                {
                    var assemblyName = this.Compilation.AssemblyName;
                    if (assemblyName == null)
                    {
                        return false;
                    }
                    var keys = unwrappedSymbol.ContainingAssembly.GetInternalsVisibleToPublicKeys(assemblyName);
                    if (!keys.Any())
                    {
                        return false;
                    }
                    foreach (ImmutableArray<byte> key in keys)
                    {
                        if (key.SequenceEqual(this.Compilation.Assembly.Identity.PublicKey))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
        }

        private CSDiagnosticInfo MakeCallMethodsDirectlyDiagnostic(Symbol symbol)
        {
            Debug.Assert(symbol.MustCallMethodsDirectly());

            MethodSymbol method1;
            MethodSymbol method2;

            switch (symbol.Kind)
            {
                case SymbolKind.Property:
                    {
                        var property = ((PropertySymbol)symbol).GetLeastOverriddenProperty(this.ContainingType);
                        method1 = property.GetMethod;
                        method2 = property.SetMethod;
                    }
                    break;
                case SymbolKind.Event:
                    {
                        var @event = ((EventSymbol)symbol).GetLeastOverriddenEvent(this.ContainingType);
                        method1 = @event.AddMethod;
                        method2 = @event.RemoveMethod;
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }

            return (((object)method1 != null) && ((object)method2 != null)) ?
                new CSDiagnosticInfo(ErrorCode.ERR_BindToBogusProp2, symbol, method1, method2) :
                new CSDiagnosticInfo(ErrorCode.ERR_BindToBogusProp1, symbol, method1 ?? method2);
        }

        internal void CheckViability<TSymbol>(LookupResult result, ImmutableArray<TSymbol> symbols, int arity, LookupOptions options, TypeSymbol accessThroughType, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved) where TSymbol : Symbol
        {
            foreach (var symbol in symbols)
            {
                var res = this.CheckViability(symbol, arity, options, accessThroughType, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                result.MergeEqual(res);
            }
        }

        /// <summary>
        /// Used by Add*LookupSymbolsInfo* to determine whether the symbol is of interest.
        /// Distinguish from <see cref="CheckViability"/>, which performs an analogous task for LookupSymbols*.
        /// </summary>
        /// <remarks>
        /// Does not consider <see cref="Symbol.CanBeReferencedByName"/> - that is left to the caller.
        /// </remarks>
        internal bool CanAddLookupSymbolInfo(Symbol symbol, LookupOptions options, LookupSymbolsInfo info, TypeSymbol accessThroughType, AliasSymbol aliasSymbol = null)
        {
            Debug.Assert(symbol.Kind != SymbolKind.Alias, "It is the caller's responsibility to unwrap aliased symbols.");
            Debug.Assert(aliasSymbol == null || aliasSymbol.GetAliasTarget(basesBeingResolved: null) == symbol);
            Debug.Assert(options.AreValid());
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            var name = aliasSymbol != null ? aliasSymbol.Name : symbol.Name;
            if (!info.CanBeAdded(name))
            {
                return false;
            }

            if ((options & LookupOptions.NamespacesOrTypesOnly) != 0 && !(symbol is NamespaceOrTypeSymbol))
            {
                return false;
            }
            else if ((options & LookupOptions.MustBeInvocableIfMember) != 0
                && IsNonInvocableMember(symbol))
            {
                return false;
            }
            else if (InCref ? !this.IsCrefAccessible(symbol)
                            : !this.IsAccessible(symbol, ref useSiteDiagnostics, RefineAccessThroughType(options, accessThroughType)))
            {
                return false;
            }
            else if ((options & LookupOptions.MustBeInstance) != 0 && !IsInstance(symbol))
            {
                return false;
            }
            else if ((options & LookupOptions.MustNotBeInstance) != 0 && IsInstance(symbol))
            {
                return false;
            }
            else if ((options & LookupOptions.MustNotBeNamespace) != 0 && (symbol.Kind == SymbolKind.Namespace))
            {
                return false;
            }
            else
            {
                // This viability check is only used by SemanticModel.LookupSymbols, which does its own
                // filtering of not-referenceable symbols.  Hence, we do not check CanBeReferencedByName
                // here.
                return true;
            }
        }

        private static TypeSymbol RefineAccessThroughType(LookupOptions options, TypeSymbol accessThroughType)
        {
            // Normally, when we access a protected instance member, we need to know the type of the receiver so we
            // can determine whether the member is actually accessible in the containing type.  There is one exception:
            // If the receiver is "base", then it's okay if the receiver type isn't derived from the containing type.
            return ((options & LookupOptions.UseBaseReferenceAccessibility) != 0)
                ? null
                : accessThroughType;
        }

        /// <summary>
        /// A symbol is accessible for referencing in a cref if it is in the same assembly as the reference
        /// or the symbols's effective visibility is not private.
        /// </summary>
        private bool IsCrefAccessible(Symbol symbol)
        {
            return !IsEffectivelyPrivate(symbol) || symbol.ContainingAssembly == this.Compilation.Assembly;
        }

        private static bool IsEffectivelyPrivate(Symbol symbol)
        {
            for (Symbol s = symbol; (object)s != null; s = s.ContainingSymbol)
            {
                if (s.DeclaredAccessibility == Accessibility.Private)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether "symbol" is accessible from this binder.
        /// Also checks protected access via "accessThroughType".
        /// </summary>
        internal bool IsAccessible(Symbol symbol, ref HashSet<DiagnosticInfo> useSiteDiagnostics, TypeSymbol accessThroughType = null, ConsList<TypeSymbol> basesBeingResolved = null)
        {
            bool failedThroughTypeCheck;
            return IsAccessible(symbol, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);
        }

        /// <summary>
        /// Check whether "symbol" is accessible from this binder.
        /// Also checks protected access via "accessThroughType", and sets "failedThroughTypeCheck" if fails
        /// the protected access check.
        /// </summary>
        internal bool IsAccessible(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved = null)
        {
            if (this.Flags.Includes(BinderFlags.IgnoreAccessibility))
            {
                failedThroughTypeCheck = false;
                return true;
            }

            return IsAccessibleHelper(symbol, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);
        }

        /// <remarks>
        /// Should only be called by <see cref="IsAccessible(Symbol, TypeSymbol, out bool, ref HashSet{DiagnosticInfo}, ConsList{TypeSymbol})"/>,
        /// which will already have checked for <see cref="BinderFlags.IgnoreAccessibility"/>.
        /// </remarks>
        internal virtual bool IsAccessibleHelper(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved)
        {
            // By default, just delegate to containing binder.
            return Next.IsAccessibleHelper(symbol, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);
        }

        internal bool IsNonInvocableMember(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.NamedType:
                case SymbolKind.Event:
                    return !IsInvocableMember(symbol);

                default:
                    return false;
            }
        }

        private bool IsInvocableMember(Symbol symbol)
        {
            // If a member is a method or event, or if it is a constant, field or property of 
            // either a delegate type or the type dynamic, then the member is said to be invocable.

            TypeSymbol type = null;

            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.Event: // Spec says it doesn't matter whether it is field-like
                    return true;

                case SymbolKind.Field:
                    type = ((FieldSymbol)symbol).GetFieldType(this.FieldsBeingBound).Type;
                    break;

                case SymbolKind.Property:
                    type = ((PropertySymbol)symbol).Type;
                    break;
            }

            return (object)type != null && (type.IsDelegateType() || type.IsDynamic());
        }

        private static bool IsInstance(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Method:
                case SymbolKind.Event:
                    return symbol.RequiresInstanceReceiver();
                default:
                    return false;
            }
        }

        // Check if the given symbol can be accessed with the given arity. If OK, return false.
        // If not OK, return true and return a diagnosticinfo. Note that methods with type arguments
        // can be accesses with arity zero due to type inference (but non types).
        private static bool WrongArity(Symbol symbol, int arity, bool diagnose, LookupOptions options, out DiagnosticInfo diagInfo)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    if (arity != 0 || (options & LookupOptions.AllNamedTypesOnArityZero) == 0)
                    {
                        NamedTypeSymbol namedType = (NamedTypeSymbol)symbol;
                        // non-declared types only appear as using aliases (aliases are arity 0)
                        Debug.Assert(object.ReferenceEquals(namedType.ConstructedFrom, namedType));
                        if (namedType.Arity != arity || options.IsAttributeTypeLookup() && arity != 0)
                        {
                            if (namedType.Arity == 0)
                            {
                                // The non-generic {1} '{0}' cannot be used with type arguments
                                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_HasNoTypeVars, namedType, MessageID.IDS_SK_TYPE.Localize()) : null;
                            }
                            else
                            {
                                // Using the generic {1} '{0}' requires {2} type arguments
                                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_BadArity, namedType, MessageID.IDS_SK_TYPE.Localize(), namedType.Arity) : null;
                            }
                            return true;
                        }
                    }
                    break;

                case SymbolKind.Method:
                    if (arity != 0 || (options & LookupOptions.AllMethodsOnArityZero) == 0)
                    {
                        MethodSymbol method = (MethodSymbol)symbol;
                        if (method.Arity != arity)
                        {
                            if (method.Arity == 0)
                            {
                                // The non-generic {1} '{0}' cannot be used with type arguments
                                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_HasNoTypeVars, method, MessageID.IDS_SK_METHOD.Localize()) : null;
                            }
                            else
                            {
                                // Using the generic {1} '{0}' requires {2} type arguments
                                diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_BadArity, method, MessageID.IDS_SK_METHOD.Localize(), method.Arity) : null;
                            }
                            return true;
                        }
                    }
                    break;

                default:
                    if (arity != 0)
                    {
                        diagInfo = diagnose ? new CSDiagnosticInfo(ErrorCode.ERR_TypeArgsNotAllowed, symbol, symbol.Kind.Localize()) : null;
                        return true;
                    }
                    break;
            }

            diagInfo = null;
            return false;
        }

        /// <summary>
        /// Look for names in scope
        /// </summary>
        internal void AddLookupSymbolsInfo(LookupSymbolsInfo result, LookupOptions options = LookupOptions.Default)
        {
            for (var scope = this; scope != null; scope = scope.Next)
            {
                scope.AddLookupSymbolsInfoInSingleBinder(result, options, originalBinder: this);
            }
        }

        protected virtual void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo info, LookupOptions options, Binder originalBinder)
        {
            // overridden in other binders
        }

        /// <summary>
        /// Look for names of members
        /// </summary>
        internal void AddMemberLookupSymbolsInfo(LookupSymbolsInfo result, NamespaceOrTypeSymbol nsOrType, LookupOptions options, Binder originalBinder)
        {
            if (nsOrType.IsNamespace)
            {
                AddMemberLookupSymbolsInfoInNamespace(result, (NamespaceSymbol)nsOrType, options, originalBinder);
            }
            else
            {
                this.AddMemberLookupSymbolsInfoInType(result, (TypeSymbol)nsOrType, options, originalBinder);
            }
        }

        private void AddMemberLookupSymbolsInfoInType(LookupSymbolsInfo result, TypeSymbol type, LookupOptions options, Binder originalBinder)
        {
            switch (type.TypeKind)
            {
                case TypeKind.TypeParameter:
                    this.AddMemberLookupSymbolsInfoInTypeParameter(result, (TypeParameterSymbol)type, options, originalBinder);
                    break;

                case TypeKind.Interface:
                    this.AddMemberLookupSymbolsInfoInInterface(result, type, options, originalBinder, type);
                    break;

                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Enum:
                case TypeKind.Delegate:
                case TypeKind.Array:
                case TypeKind.Dynamic:
                    this.AddMemberLookupSymbolsInfoInClass(result, type, options, originalBinder, type);
                    break;

                case TypeKind.Submission:
                    this.AddMemberLookupSymbolsInfoInSubmissions(result, type, options, originalBinder);
                    break;
            }
        }

        private void AddMemberLookupSymbolsInfoInSubmissions(LookupSymbolsInfo result, TypeSymbol scriptClass, LookupOptions options, Binder originalBinder)
        {
            // TODO: we need tests
            // TODO: optimize lookup (there might be many interactions in the chain)
            for (CSharpCompilation submission = Compilation; submission != null; submission = submission.PreviousSubmission)
            {
                if ((object)submission.ScriptClass != null)
                {
                    AddMemberLookupSymbolsInfoWithoutInheritance(result, submission.ScriptClass, options, originalBinder, scriptClass);
                }

                bool isCurrentSubmission = submission == Compilation;

                // If we are looking only for labels we do not need to search through the imports.
                if ((options & LookupOptions.LabelsOnly) == 0 && !(isCurrentSubmission && this.Flags.Includes(BinderFlags.InScriptUsing)))
                {
                    var submissionImports = submission.GetSubmissionImports();
                    if (!isCurrentSubmission)
                    {
                        submissionImports = Imports.ExpandPreviousSubmissionImports(submissionImports, Compilation);
                    }

                    // NB: We diverge from InContainerBinder here and only look in aliases.
                    // In submissions, regular usings are bubbled up to the outermost scope.
                    submissionImports.AddLookupSymbolsInfoInAliases(result, options, originalBinder);
                }
            }
        }

        private static void AddMemberLookupSymbolsInfoInNamespace(LookupSymbolsInfo result, NamespaceSymbol ns, LookupOptions options, Binder originalBinder)
        {
            var candidateMembers = result.FilterName != null ? GetCandidateMembers(ns, result.FilterName, options, originalBinder) : GetCandidateMembers(ns, options, originalBinder);
            foreach (var symbol in candidateMembers)
            {
                if (originalBinder.CanAddLookupSymbolInfo(symbol, options, result, null))
                {
                    result.AddSymbol(symbol, symbol.Name, symbol.GetArity());
                }
            }
        }

        private static void AddMemberLookupSymbolsInfoWithoutInheritance(LookupSymbolsInfo result, TypeSymbol type, LookupOptions options, Binder originalBinder, TypeSymbol accessThroughType)
        {
            var candidateMembers = result.FilterName != null ? GetCandidateMembers(type, result.FilterName, options, originalBinder) : GetCandidateMembers(type, options, originalBinder);
            foreach (var symbol in candidateMembers)
            {
                if (originalBinder.CanAddLookupSymbolInfo(symbol, options, result, accessThroughType))
                {
                    result.AddSymbol(symbol, symbol.Name, symbol.GetArity());
                }
            }
        }

        private void AddWinRTMembersLookupSymbolsInfo(LookupSymbolsInfo result, NamedTypeSymbol type, LookupOptions options, Binder originalBinder, TypeSymbol accessThroughType)
        {
            NamedTypeSymbol idictSymbol, iroDictSymbol, iListSymbol, iCollectionSymbol, inccSymbol, inpcSymbol;
            GetWellKnownWinRTMemberInterfaces(out idictSymbol, out iroDictSymbol, out iListSymbol, out iCollectionSymbol, out inccSymbol, out inpcSymbol);

            // Dev11 searches all declared and undeclared base interfaces
            foreach (var iface in type.AllInterfacesNoUseSiteDiagnostics)
            {
                if (ShouldAddWinRTMembersForInterface(iface, idictSymbol, iroDictSymbol, iListSymbol, iCollectionSymbol, inccSymbol, inpcSymbol))
                {
                    AddMemberLookupSymbolsInfoWithoutInheritance(result, iface, options, originalBinder, accessThroughType);
                }
            }
        }

        private void AddMemberLookupSymbolsInfoInClass(LookupSymbolsInfo result, TypeSymbol type, LookupOptions options, Binder originalBinder, TypeSymbol accessThroughType)
        {
            PooledHashSet<NamedTypeSymbol> visited = null;
            // We need a check for SpecialType.System_Void as its base type is
            // ValueType but we don't wish to return any members for void type
            while ((object)type != null && !type.IsVoidType())
            {
                AddMemberLookupSymbolsInfoWithoutInheritance(result, type, options, originalBinder, accessThroughType);

                // If the type is from a winmd and implements any of the special WinRT collection
                // projections then we may need to add underlying interface members. 
                NamedTypeSymbol namedType = type as NamedTypeSymbol;
                if ((object)namedType != null && namedType.ShouldAddWinRTMembers)
                {
                    AddWinRTMembersLookupSymbolsInfo(result, namedType, options, originalBinder, accessThroughType);
                }

                // As in dev11, we don't consider inherited members within crefs.
                // CAVEAT: dev11 appears to ignore this rule within parameter types and return types,
                // so we're checking Cref, rather than Cref and CrefParameterOrReturnType.
                if (originalBinder.InCrefButNotParameterOrReturnType)
                {
                    break;
                }

                type = type.GetNextBaseTypeNoUseSiteDiagnostics(null, this.Compilation, ref visited);
            }

            visited?.Free();
        }

        private void AddMemberLookupSymbolsInfoInInterface(LookupSymbolsInfo result, TypeSymbol type, LookupOptions options, Binder originalBinder, TypeSymbol accessThroughType)
        {
            AddMemberLookupSymbolsInfoWithoutInheritance(result, type, options, originalBinder, accessThroughType);

            if (!originalBinder.InCrefButNotParameterOrReturnType)
            {
                foreach (var baseInterface in type.AllInterfacesNoUseSiteDiagnostics)
                {
                    AddMemberLookupSymbolsInfoWithoutInheritance(result, baseInterface, options, originalBinder, accessThroughType);
                }

                this.AddMemberLookupSymbolsInfoInClass(result, Compilation.GetSpecialType(SpecialType.System_Object), options, originalBinder, accessThroughType);
            }
        }

        private void AddMemberLookupSymbolsInfoInTypeParameter(LookupSymbolsInfo result, TypeParameterSymbol type, LookupOptions options, Binder originalBinder)
        {
            if (type.TypeParameterKind == TypeParameterKind.Cref)
            {
                return;
            }

            NamedTypeSymbol effectiveBaseClass = type.EffectiveBaseClassNoUseSiteDiagnostics;
            this.AddMemberLookupSymbolsInfoInClass(result, effectiveBaseClass, options, originalBinder, effectiveBaseClass);

            foreach (var baseInterface in type.AllEffectiveInterfacesNoUseSiteDiagnostics)
            {
                // accessThroughType matches LookupMembersInTypeParameter.
                AddMemberLookupSymbolsInfoWithoutInheritance(result, baseInterface, options, originalBinder, accessThroughType: type);
            }
        }
    }
}
