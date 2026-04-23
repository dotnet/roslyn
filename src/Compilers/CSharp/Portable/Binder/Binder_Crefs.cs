// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        internal ImmutableArray<Symbol> BindCref(CrefSyntax syntax, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            ImmutableArray<Symbol> symbols = BindCrefInternal(syntax, out ambiguityWinner, diagnostics);
            Debug.Assert(!symbols.IsDefault, "Prefer empty to null.");
            Debug.Assert((symbols.Length > 1) == ((object?)ambiguityWinner != null), "ambiguityWinner should be set iff more than one symbol is returned.");
            return symbols;
        }

        private ImmutableArray<Symbol> BindCrefInternal(CrefSyntax syntax, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            switch (syntax.Kind())
            {
                case SyntaxKind.TypeCref:
                    return BindTypeCref((TypeCrefSyntax)syntax, out ambiguityWinner, diagnostics);
                case SyntaxKind.QualifiedCref:
                    return BindQualifiedCref((QualifiedCrefSyntax)syntax, out ambiguityWinner, diagnostics);
                case SyntaxKind.NameMemberCref:
                case SyntaxKind.IndexerMemberCref:
                case SyntaxKind.OperatorMemberCref:
                case SyntaxKind.ConversionOperatorMemberCref:
                case SyntaxKind.ExtensionMemberCref:
                    return BindMemberCref((MemberCrefSyntax)syntax, containerOpt: null, ambiguityWinner: out ambiguityWinner, diagnostics: diagnostics);
                default:
                    throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
            }
        }

        private ImmutableArray<Symbol> BindTypeCref(TypeCrefSyntax syntax, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            NamespaceOrTypeSymbol result = BindNamespaceOrTypeSymbolInCref(syntax.Type);

            // NOTE: we don't have to worry about the case where a non-error type is constructed
            // with erroneous type arguments, because only MemberCrefs have type arguments -
            // all other crefs only have type parameters.
            if (result.Kind == SymbolKind.ErrorType)
            {
                var noTrivia = syntax.WithLeadingTrivia(null).WithTrailingTrivia(null);
                diagnostics.Add(ErrorCode.WRN_BadXMLRef, syntax.Location, noTrivia.ToFullString());
            }

            // We'll never have more than one type, but it is conceivable that result could
            // be an ExtendedErrorTypeSymbol with multiple candidates.
            ambiguityWinner = null;
            return ImmutableArray.Create<Symbol>(result);
        }

        private ImmutableArray<Symbol> BindQualifiedCref(QualifiedCrefSyntax syntax, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            // NOTE: we won't check whether container is an error type - we'll just let BindMemberCref fail
            // and report a blanket diagnostic.
            NamespaceOrTypeSymbol container = BindNamespaceOrTypeSymbolInCref(syntax.Container);
            return BindMemberCref(syntax.Member, container, out ambiguityWinner, diagnostics);
        }

        /// <summary>
        /// We can't use BindNamespaceOrTypeSymbol, since it doesn't return inaccessible symbols (directly).
        /// </summary>
        /// <remarks>
        /// Guaranteed not to return null.
        /// 
        /// CONSIDER: As in dev11, we don't handle ambiguity at this level.  Hypothetically,
        /// we could just pick one, though an "ideal" solution would probably involve a search
        /// down all ambiguous branches.
        /// </remarks>
        private NamespaceOrTypeSymbol BindNamespaceOrTypeSymbolInCref(TypeSyntax syntax)
        {
            Debug.Assert(Flags.Includes(BinderFlags.Cref));

            // BREAK: Dev11 used to do a second lookup, ignoring accessibility, if the first lookup failed.
            //   VS BUG#3321137: we need to try to find accessible members first
            //   especially for compiler generated events (the backing field is private
            //   but has the same name as the public event, and there is no easy way to
            //   set the isEvent field on imported MEMBVARSYMs)

            // Diagnostics that don't prevent us from getting a symbol don't matter - the caller will report
            // an umbrella diagnostic if the result is an error type.
            NamespaceOrTypeSymbol namespaceOrTypeSymbol = BindNamespaceOrTypeSymbol(syntax, BindingDiagnosticBag.Discarded).NamespaceOrTypeSymbol;

            Debug.Assert((object)namespaceOrTypeSymbol != null);
            return namespaceOrTypeSymbol;
        }

        private ImmutableArray<Symbol> BindMemberCref(MemberCrefSyntax syntax, NamespaceOrTypeSymbol? containerOpt, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            if ((object?)containerOpt != null && containerOpt.Kind == SymbolKind.TypeParameter)
            {
                // As in normal lookup (see CreateErrorIfLookupOnTypeParameter), you can't dot into a type parameter
                // (though you can dot into an expression of type parameter type).
                CrefSyntax crefSyntax = GetRootCrefSyntax(syntax);
                var noTrivia = syntax.WithLeadingTrivia(null).WithTrailingTrivia(null);
                diagnostics.Add(ErrorCode.WRN_BadXMLRef, crefSyntax.Location, noTrivia.ToFullString());

                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            ImmutableArray<Symbol> result;
            switch (syntax.Kind())
            {
                case SyntaxKind.NameMemberCref:
                    result = BindNameMemberCref((NameMemberCrefSyntax)syntax, containerOpt, out ambiguityWinner, diagnostics);
                    break;
                case SyntaxKind.IndexerMemberCref:
                    result = BindIndexerMemberCref((IndexerMemberCrefSyntax)syntax, containerOpt, out ambiguityWinner, diagnostics);
                    break;
                case SyntaxKind.OperatorMemberCref:
                    result = BindOperatorMemberCref((OperatorMemberCrefSyntax)syntax, containerOpt, out ambiguityWinner, diagnostics);
                    break;
                case SyntaxKind.ConversionOperatorMemberCref:
                    result = BindConversionOperatorMemberCref((ConversionOperatorMemberCrefSyntax)syntax, containerOpt, out ambiguityWinner, diagnostics);
                    break;
                case SyntaxKind.ExtensionMemberCref:
                    result = BindExtensionMemberCref((ExtensionMemberCrefSyntax)syntax, containerOpt, out ambiguityWinner, diagnostics);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
            }

            if (!result.Any())
            {
                CrefSyntax crefSyntax = GetRootCrefSyntax(syntax);
                var noTrivia = syntax.WithLeadingTrivia(null).WithTrailingTrivia(null);
                diagnostics.Add(ErrorCode.WRN_BadXMLRef, crefSyntax.Location, noTrivia.ToFullString());
            }

            return result;
        }

        private ImmutableArray<Symbol> BindNameMemberCref(NameMemberCrefSyntax syntax, NamespaceOrTypeSymbol? containerOpt, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            SimpleNameSyntax? nameSyntax = syntax.Name as SimpleNameSyntax;

            int arity;
            string memberName;
            string memberNameText;

            if (nameSyntax != null)
            {
                arity = nameSyntax.Arity;
                memberName = nameSyntax.Identifier.ValueText;
                memberNameText = nameSyntax.Identifier.Text;
            }
            else
            {
                // If the name isn't a SimpleNameSyntax, then we must have a type name followed by a parameter list.
                // Thus, we're looking for a constructor.
                Debug.Assert((object?)containerOpt == null);

                // Could be an error type, but we'll just lookup fail below.
                containerOpt = BindNamespaceOrTypeSymbolInCref(syntax.Name);

                arity = 0;
                memberName = memberNameText = WellKnownMemberNames.InstanceConstructorName;
            }

            if (string.IsNullOrEmpty(memberName))
            {
                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            ImmutableArray<Symbol> sortedSymbols = ComputeSortedCrefMembers(syntax, containerOpt, memberName, memberNameText, arity, syntax.Parameters != null, diagnostics);

            if (sortedSymbols.IsEmpty)
            {
                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            return ProcessCrefMemberLookupResults(
                sortedSymbols,
                arity,
                syntax,
                typeArgumentListSyntax: arity == 0 ? null : ((GenericNameSyntax)nameSyntax!).TypeArgumentList,
                parameterListSyntax: syntax.Parameters,
                ambiguityWinner: out ambiguityWinner,
                diagnostics: diagnostics);
        }

        private ImmutableArray<Symbol> BindIndexerMemberCref(IndexerMemberCrefSyntax syntax, NamespaceOrTypeSymbol? containerOpt, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            const int arity = 0;

            ImmutableArray<Symbol> sortedSymbols = ComputeSortedCrefMembers(syntax, containerOpt, WellKnownMemberNames.Indexer, memberNameText: WellKnownMemberNames.Indexer, arity, syntax.Parameters != null, diagnostics);

            if (sortedSymbols.IsEmpty)
            {
                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            // Since only indexers are named WellKnownMemberNames.Indexer.
            Debug.Assert(sortedSymbols.All(SymbolExtensions.IsIndexer));

            // NOTE: guaranteed to be a property, because only indexers are considered.
            return ProcessCrefMemberLookupResults(
                sortedSymbols,
                arity,
                syntax,
                typeArgumentListSyntax: null,
                parameterListSyntax: syntax.Parameters,
                ambiguityWinner: out ambiguityWinner,
                diagnostics: diagnostics);
        }

        private ImmutableArray<Symbol> BindExtensionMemberCref(ExtensionMemberCrefSyntax syntax, NamespaceOrTypeSymbol? containerOpt, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            CheckFeatureAvailability(syntax, MessageID.IDS_FeatureExtensions, diagnostics);

            int arity = 0;
            TypeArgumentListSyntax? typeArgumentListSyntax = null;
            CrefParameterListSyntax? parameters = null;
            string? memberName = null;

            if (syntax.Member is NameMemberCrefSyntax { Name: SimpleNameSyntax simpleName } nameMember)
            {
                arity = simpleName.Arity;
                typeArgumentListSyntax = simpleName is GenericNameSyntax genericName ? genericName.TypeArgumentList : null;
                parameters = nameMember.Parameters;
                memberName = simpleName.Identifier.ValueText;
            }
            else if (syntax.Member is OperatorMemberCrefSyntax operatorSyntax)
            {
                memberName = GetOperatorMethodName(operatorSyntax);
                parameters = operatorSyntax.Parameters;
            }

            if (memberName == null)
            {
                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            TypeArgumentListSyntax? extensionTypeArguments = syntax.TypeArgumentList;
            int extensionArity = extensionTypeArguments?.Arguments.Count ?? 0;
            ImmutableArray<Symbol> sortedSymbols = computeSortedAndFilteredCrefExtensionMembers(containerOpt, memberName, extensionArity, arity, extensionTypeArguments, diagnostics, syntax);

            if (sortedSymbols.IsDefaultOrEmpty)
            {
                ambiguityWinner = null;
                return [];
            }

            Debug.Assert(sortedSymbols.All(s => s.IsExtensionBlockMember()));

            return ProcessCrefMemberLookupResults(sortedSymbols, arity, syntax, typeArgumentListSyntax, parameters, out ambiguityWinner, diagnostics);

            ImmutableArray<Symbol> computeSortedAndFilteredCrefExtensionMembers(NamespaceOrTypeSymbol? containerOpt, string name, int extensionArity, int arity, TypeArgumentListSyntax? extensionTypeArguments, BindingDiagnosticBag diagnostics, ExtensionMemberCrefSyntax syntax)
            {
                Debug.Assert(name is not null);

                Debug.Assert(syntax.Parameters is not null);
                ImmutableArray<ParameterSymbol> extensionParameterSymbols = BindCrefParameters(syntax.Parameters, diagnostics);

                // Use signature method symbols to match extension blocks
                var providedExtensionSignature = new SignatureOnlyMethodSymbol(
                     methodKind: MethodKind.Ordinary,
                     typeParameters: IndexedTypeParameterSymbol.TakeSymbols(extensionArity),
                     parameters: extensionParameterSymbols,
                     callingConvention: Cci.CallingConvention.Default,
                     // These are ignored by this specific MemberSignatureComparer.
                     containingType: null,
                     name: null,
                     refKind: RefKind.None,
                     isInitOnly: false,
                     isStatic: false,
                     returnType: default,
                     refCustomModifiers: [],
                     explicitInterfaceImplementations: []);

                LookupOptions options = LookupOptions.AllMethodsOnArityZero | LookupOptions.MustNotBeParameter;
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = this.GetNewCompoundUseSiteInfo(diagnostics);
                ArrayBuilder<Symbol>? sortedSymbolsBuilder = null;

                foreach (var nested in candidateTypes(containerOpt))
                {
                    if (!nested.IsExtension
                        || nested.Arity != extensionArity
                        || nested.ExtensionParameter is null
                        || nested is not { ContainingType: { ContainingType: null } }) // only consider extension blocks in top-level types
                    {
                        continue;
                    }

                    var constructedNested = (NamedTypeSymbol)ConstructWithCrefTypeParameters(extensionArity, extensionTypeArguments, nested);

                    var candidateExtensionSignature = new SignatureOnlyMethodSymbol(
                         methodKind: MethodKind.Ordinary,
                         typeParameters: IndexedTypeParameterSymbol.TakeSymbols(constructedNested.Arity),
                         parameters: [constructedNested.ExtensionParameter],
                         callingConvention: Cci.CallingConvention.Default,
                         // These are ignored by this specific MemberSignatureComparer.
                         containingType: null,
                         name: null,
                         refKind: RefKind.None,
                         isInitOnly: false,
                         isStatic: false,
                         returnType: default,
                         refCustomModifiers: [],
                         explicitInterfaceImplementations: []);

                    if (!MemberSignatureComparer.CrefComparer.Equals(candidateExtensionSignature, providedExtensionSignature))
                    {
                        continue;
                    }

                    var candidates = constructedNested.GetMembers(name);

                    foreach (var candidate in candidates)
                    {
                        if (!SourceMemberContainerTypeSymbol.IsAllowedExtensionMember(candidate))
                        {
                            continue;
                        }

                        if (arity != 0 && candidate.GetArity() != arity)
                        {
                            continue;
                        }

                        // Note: we bypass the arity check here, as it would check for total arity (extension + member arity)
                        SingleLookupResult result = this.CheckViability(candidate, arity: 0, options, accessThroughType: null, diagnose: true, useSiteInfo: ref useSiteInfo);

                        if (result.Kind == LookupResultKind.Viable)
                        {
                            Debug.Assert(result.Symbol is not null);
                            sortedSymbolsBuilder ??= ArrayBuilder<Symbol>.GetInstance();
                            sortedSymbolsBuilder.Add(result.Symbol);
                        }
                    }
                }

                diagnostics.Add(syntax, useSiteInfo);

                if (sortedSymbolsBuilder is null)
                {
                    return ImmutableArray<Symbol>.Empty;
                }

                // Since we resolve ambiguities by just picking the first symbol we encounter,
                // the order of the symbols matters for repeatability.
                if (sortedSymbolsBuilder.Count > 1)
                {
                    sortedSymbolsBuilder.Sort(ConsistentSymbolOrder.Instance);
                }

                return sortedSymbolsBuilder.ToImmutableAndFree();

                ImmutableArray<NamedTypeSymbol> candidateTypes(NamespaceOrTypeSymbol? containerOpt)
                {
                    if (containerOpt is NamedTypeSymbol namedType)
                    {
                        return namedType.GetTypeMembers("");
                    }

                    NamedTypeSymbol? containingType = ContainingType;
                    if (containingType is null)
                    {
                        return [];
                    }

                    NamedTypeSymbol? enclosingType = containingType.IsExtension ? containingType.ContainingType : containingType;
                    return enclosingType?.GetTypeMembers("") ?? [];
                }
            }
        }

        // NOTE: not guaranteed to be a method (e.g. class op_Addition)
        // NOTE: constructor fallback logic applies
        private ImmutableArray<Symbol> BindOperatorMemberCref(OperatorMemberCrefSyntax syntax, NamespaceOrTypeSymbol? containerOpt, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            const int arity = 0;

            string? memberName = GetOperatorMethodName(syntax);

            if (memberName == null)
            {
                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            ImmutableArray<Symbol> sortedSymbols = ComputeSortedCrefMembers(syntax, containerOpt, memberName, memberNameText: memberName, arity, syntax.Parameters != null, diagnostics);

            if (sortedSymbols.IsEmpty)
            {
                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            return ProcessCrefMemberLookupResults(
                sortedSymbols,
                arity,
                syntax,
                typeArgumentListSyntax: null,
                parameterListSyntax: syntax.Parameters,
                ambiguityWinner: out ambiguityWinner,
                diagnostics: diagnostics);
        }

        private static string? GetOperatorMethodName(OperatorMemberCrefSyntax syntax)
        {
            bool isChecked = syntax.CheckedKeyword.IsKind(SyntaxKind.CheckedKeyword);

            SyntaxKind operatorTokenKind = syntax.OperatorToken.Kind();
            string? memberName;

            if (SyntaxFacts.IsOverloadableCompoundAssignmentOperator(operatorTokenKind))
            {
                memberName = OperatorFacts.CompoundAssignmentOperatorNameFromSyntaxKind(operatorTokenKind, isChecked);
            }
            else
            {
                CrefParameterListSyntax? parameterListSyntax = syntax.Parameters;

                // NOTE: Prefer binary to unary, unless there is exactly one parameter.
                // CONSIDER: we're following dev11 by never using a binary operator name if there's
                // exactly one parameter, but doing so would allow us to match single-parameter constructors.
                memberName = parameterListSyntax != null && parameterListSyntax.Parameters.Count == 1
                    ? null
                    : OperatorFacts.BinaryOperatorNameFromSyntaxKindIfAny(operatorTokenKind, isChecked);

                if (memberName is null)
                {
                    if (operatorTokenKind is SyntaxKind.PlusPlusToken or SyntaxKind.MinusMinusToken && parameterListSyntax?.Parameters.Count == 0)
                    {
                        memberName = OperatorFacts.CompoundAssignmentOperatorNameFromSyntaxKind(operatorTokenKind, isChecked);
                    }
                    else
                    {
                        memberName = OperatorFacts.UnaryOperatorNameFromSyntaxKindIfAny(operatorTokenKind, isChecked: isChecked);
                    }
                }
            }

            if (memberName != null &&
                (isChecked && !syntax.OperatorToken.IsMissing && !SyntaxFacts.IsCheckedOperator(memberName))) // the operator cannot be checked
            {
                memberName = null;
            }

            return memberName;
        }

        // NOTE: not guaranteed to be a method (e.g. class op_Implicit)
        private ImmutableArray<Symbol> BindConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax syntax, NamespaceOrTypeSymbol? containerOpt, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            const int arity = 0;
            bool isChecked = syntax.CheckedKeyword.IsKind(SyntaxKind.CheckedKeyword);

            string memberName;

            if (syntax.ImplicitOrExplicitKeyword.Kind() == SyntaxKind.ImplicitKeyword)
            {
                if (isChecked)
                {
                    // checked form is not supported
                    ambiguityWinner = null;
                    return ImmutableArray<Symbol>.Empty;
                }

                memberName = WellKnownMemberNames.ImplicitConversionName;
            }
            else if (isChecked)
            {
                memberName = WellKnownMemberNames.CheckedExplicitConversionName;
            }
            else
            {
                memberName = WellKnownMemberNames.ExplicitConversionName;
            }

            ImmutableArray<Symbol> sortedSymbols = ComputeSortedCrefMembers(syntax, containerOpt, memberName, memberNameText: memberName, arity, syntax.Parameters != null, diagnostics);

            if (sortedSymbols.IsEmpty)
            {
                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            TypeSymbol returnType = BindCrefParameterOrReturnType(syntax.Type, syntax, diagnostics);

            // Filter out methods with the wrong return type, since overload resolution won't catch these.
            sortedSymbols = sortedSymbols.WhereAsArray((symbol, returnType) =>
                symbol.Kind != SymbolKind.Method || TypeSymbol.Equals(((MethodSymbol)symbol).ReturnType, returnType, TypeCompareKind.ConsiderEverything2), returnType);

            if (!sortedSymbols.Any())
            {
                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            return ProcessCrefMemberLookupResults(
                sortedSymbols,
                arity,
                syntax,
                typeArgumentListSyntax: null,
                parameterListSyntax: syntax.Parameters,
                ambiguityWinner: out ambiguityWinner,
                diagnostics: diagnostics);
        }

        /// <summary>
        /// Perform lookup (optionally, in a specified container).  If nothing is found and the member name matches the containing type
        /// name, then use the instance constructors of the type instead.  The resulting symbols are sorted since tie-breaking is based
        /// on order and we want cref binding to be repeatable.
        /// </summary>
        /// <remarks>
        /// Never returns null.
        /// </remarks>
        private ImmutableArray<Symbol> ComputeSortedCrefMembers(CSharpSyntaxNode syntax, NamespaceOrTypeSymbol? containerOpt, string memberName, string memberNameText, int arity, bool hasParameterList, BindingDiagnosticBag diagnostics)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var result = ComputeSortedCrefMembers(containerOpt, memberName, memberNameText, arity, hasParameterList, syntax, diagnostics, ref useSiteInfo);
            diagnostics.Add(syntax, useSiteInfo);
            return result;
        }

        private ImmutableArray<Symbol> ComputeSortedCrefMembers(NamespaceOrTypeSymbol? containerOpt, string memberName, string memberNameText, int arity, bool hasParameterList, CSharpSyntaxNode syntax,
            BindingDiagnosticBag diagnostics, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // Since we may find symbols without going through the lookup API,
            // expose the symbols via an ArrayBuilder.
            ArrayBuilder<Symbol> builder;
            {
                LookupResult result = LookupResult.GetInstance();
                this.LookupSymbolsOrMembersInternal(
                    result,
                    containerOpt,
                    name: memberName,
                    arity: arity,
                    basesBeingResolved: null,
                    options: LookupOptions.AllMethodsOnArityZero | LookupOptions.MustNotBeParameter,
                    diagnose: false,
                    useSiteInfo: ref useSiteInfo);

                // CONSIDER: Dev11 also checks for a constructor in the event of an ambiguous result.
                if (result.IsMultiViable)
                {
                    // Dev11 doesn't consider members from System.Object when the container is an interface.
                    // Lookup should already have dropped such members.
                    builder = ArrayBuilder<Symbol>.GetInstance();
                    builder.AddRange(result.Symbols);
                    result.Free();
                }
                else if (memberNameText is "nint" or "nuint"
                    && containerOpt is null
                    && arity == 0
                    && !hasParameterList)
                {
                    result.Free(); // Won't be using this.
                    Debug.Assert(memberName == memberNameText);
                    CheckFeatureAvailability(syntax, MessageID.IDS_FeatureNativeInt, diagnostics);
                    builder = ArrayBuilder<Symbol>.GetInstance();
                    builder.Add(this.GetSpecialType(memberName == "nint" ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr, diagnostics, syntax).AsNativeInteger());
                }
                else
                {
                    result.Free(); // Won't be using this.

                    // Dev11 has a complicated two-stage process for determining when a cref is really referring to a constructor.
                    // Under two sets of conditions, XmlDocCommentBinder::bindXMLReferenceName will decide that a name refers
                    // to a constructor and under one set of conditions, the calling method, XmlDocCommentBinder::bindXMLReference,
                    // will roll back that decision and return null.

                    // In XmlDocCommentBinder::bindXMLReferenceName:
                    //   1) If an unqualified, non-generic name didn't bind to anything and the name matches the name of the type
                    //      to which the doc comment is applied, then bind to a constructor.
                    //   2) If a qualified, non-generic name didn't bind to anything and the LHS of the qualified name is a type
                    //      with the same name, then bind to a constructor.

                    // Quoted from XmlDocCommentBinder::bindXMLReference:
                    //   Filtering out the case where specifying the name of a generic type without specifying
                    //   any arity returns a constructor. This case shouldn't return anything. Note that
                    //   returning the constructors was a fix for the wonky constructor behavior, but in order
                    //   to not introduce a regression and breaking change we return NULL in this case.
                    //   e.g.
                    //   
                    //   /// <see cref="Goo"/>
                    //   class Goo<T> { }
                    //   
                    //   This cref used not to bind to anything, because before it was looking for a type and
                    //   since there was no arity, it didn't find Goo<T>. Now however, it finds Goo<T>.ctor,
                    //   which is arguably correct, but would be a breaking change (albeit with minimal impact)
                    //   so we catch this case and chuck out the symbol found.

                    // In Roslyn, we're doing everything in one pass, rather than guessing and rolling back.  

                    // As in the native compiler, we treat this as a fallback case - something that actually has the
                    // specified name is preferred.

                    NamedTypeSymbol? constructorType = null;

                    if (arity == 0) // Member arity
                    {
                        NamedTypeSymbol? containerType = containerOpt as NamedTypeSymbol;
                        if ((object?)containerType != null)
                        {
                            // Case 1: If the name is qualified by a type with the same name, then we want a 
                            // constructor (unless the type is generic, the cref is on/in the type (but not 
                            // on/in a nested type), and there were no parens after the member name).

                            if (containerType.Name == memberName && (hasParameterList || containerType.Arity == 0 || !TypeSymbol.Equals(this.ContainingType, containerType.OriginalDefinition, TypeCompareKind.ConsiderEverything2)))
                            {
                                constructorType = containerType;
                            }
                        }
                        else if ((object?)containerOpt == null && hasParameterList)
                        {
                            // Case 2: If the name is not qualified by anything, but we're in the scope
                            // of a type with the same name (regardless of arity), then we want a constructor,
                            // as long as there were parens after the member name.

                            NamedTypeSymbol? binderContainingType = this.ContainingType;
                            if ((object?)binderContainingType != null && memberName == binderContainingType.Name)
                            {
                                constructorType = binderContainingType;
                            }
                        }
                    }

                    if ((object?)constructorType != null)
                    {
                        ImmutableArray<MethodSymbol> instanceConstructors = constructorType.InstanceConstructors;
                        int numInstanceConstructors = instanceConstructors.Length;

                        if (numInstanceConstructors == 0)
                        {
                            return ImmutableArray<Symbol>.Empty;
                        }

                        builder = ArrayBuilder<Symbol>.GetInstance(numInstanceConstructors);
                        builder.AddRange(instanceConstructors);
                    }
                    else
                    {
                        return ImmutableArray<Symbol>.Empty;
                    }
                }
            }

            Debug.Assert(builder != null);

            // Since we resolve ambiguities by just picking the first symbol we encounter,
            // the order of the symbols matters for repeatability.
            if (builder.Count > 1)
            {
                builder.Sort(ConsistentSymbolOrder.Instance);
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Given a list of viable lookup results (based on the name, arity, and containing symbol),
        /// attempt to select one.
        /// </summary>
        private ImmutableArray<Symbol> ProcessCrefMemberLookupResults(
            ImmutableArray<Symbol> symbols,
            int arity,
            MemberCrefSyntax memberSyntax,
            TypeArgumentListSyntax? typeArgumentListSyntax,
            BaseCrefParameterListSyntax? parameterListSyntax,
            out Symbol? ambiguityWinner,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!symbols.IsEmpty);

            if (parameterListSyntax == null)
            {
                return ProcessParameterlessCrefMemberLookupResults(symbols, arity, memberSyntax, typeArgumentListSyntax, out ambiguityWinner, diagnostics);
            }

            ArrayBuilder<Symbol> candidates = ArrayBuilder<Symbol>.GetInstance();
            GetCrefOverloadResolutionCandidates(symbols, arity, typeArgumentListSyntax, candidates);

            ImmutableArray<ParameterSymbol> parameterSymbols = BindCrefParameters(parameterListSyntax, diagnostics);
            ImmutableArray<Symbol> results = PerformCrefOverloadResolution(candidates, parameterSymbols, arity, memberSyntax, out ambiguityWinner, diagnostics);

            candidates.Free();

            // NOTE: This diagnostic is just a hint that might help fix a broken cref, so don't do
            // any work unless there are no viable candidates.
            if (results.Length == 0)
            {
                for (int i = 0; i < parameterSymbols.Length; i++)
                {
                    if (ContainsNestedTypeOfUnconstructedGenericType(parameterSymbols[i].Type))
                    {
                        // This warning is new in Roslyn, because our better-defined semantics for
                        // cref lookup disallow some things that were possible in dev12.
                        //
                        // Consider the following code:
                        //
                        //   public class C<T>
                        //   {
                        //       public class Inner { }
                        //   
                        //       public void M(Inner i) { }
                        //   
                        //       /// <see cref="M"/>
                        //       /// <see cref="C{T}.M"/>
                        //       /// <see cref="C{Q}.M"/>
                        //       /// <see cref="C{Q}.M(C{Q}.Inner)"/>
                        //       /// <see cref="C{Q}.M(Inner)"/> // WRN_UnqualifiedNestedTypeInCref
                        //       public void N() { }
                        //   }
                        //
                        // Dev12 binds all of the crefs as "M:C`1.M(C{`0}.Inner)".
                        // Roslyn accepts all but the last.  The issue is that the context for performing
                        // the lookup is not C<Q>, but C<T>.  Consequently, Inner binds to C<T>.Inner and
                        // then overload resolution fails because C<T>.Inner does not match C<Q>.Inner,
                        // the parameter type of C<Q>.M.  Since we could not agree that the old behavior
                        // was desirable (other than for backwards compatibility) and since mimicking it
                        // would have been expensive, we settled on introducing a new warning that at
                        // least hints to the user how then can work around the issue (i.e. by qualifying
                        // Inner as C{Q}.Inner).  Additional details are available in DevDiv #743425.
                        //
                        // CONSIDER: We could actually put the qualified form in the warning message,
                        // but that would probably just make it more frustrating (i.e. if the compiler
                        // knows exactly what I mean, why do I have to type it).
                        //
                        // NOTE: This is not a great location (whole parameter instead of problematic type),
                        // but it's better than nothing.
                        diagnostics.Add(ErrorCode.WRN_UnqualifiedNestedTypeInCref, parameterListSyntax.Parameters[i].Location);
                        break;
                    }
                }
            }

            return results;
        }

        private static bool ContainsNestedTypeOfUnconstructedGenericType(TypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Array:
                    return ContainsNestedTypeOfUnconstructedGenericType(((ArrayTypeSymbol)type).ElementType);
                case TypeKind.Pointer:
                    return ContainsNestedTypeOfUnconstructedGenericType(((PointerTypeSymbol)type).PointedAtType);
                case TypeKind.FunctionPointer:
                    MethodSymbol signature = ((FunctionPointerTypeSymbol)type).Signature;
                    if (ContainsNestedTypeOfUnconstructedGenericType(signature.ReturnType))
                    {
                        return true;
                    }

                    foreach (var param in signature.Parameters)
                    {
                        if (ContainsNestedTypeOfUnconstructedGenericType(param.Type))
                        {
                            return true;
                        }
                    }

                    return false;
                case TypeKind.Delegate:
                case TypeKind.Class:
                case TypeKind.Interface:
                case TypeKind.Struct:
                case TypeKind.Enum:
                case TypeKind.Error:
                    NamedTypeSymbol namedType = (NamedTypeSymbol)type;
                    if (IsNestedTypeOfUnconstructedGenericType(namedType))
                    {
                        return true;
                    }

                    foreach (TypeWithAnnotations typeArgument in namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
                    {
                        if (ContainsNestedTypeOfUnconstructedGenericType(typeArgument.Type))
                        {
                            return true;
                        }
                    }

                    return false;
                case TypeKind.Dynamic:
                case TypeKind.TypeParameter:
                    return false;
                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }

        private static bool IsNestedTypeOfUnconstructedGenericType(NamedTypeSymbol type)
        {
            NamedTypeSymbol containing = type.ContainingType;
            while ((object)containing != null)
            {
                if (containing.Arity > 0 && containing.IsDefinition)
                {
                    return true;
                }
                containing = containing.ContainingType;
            }

            return false;
        }

        /// <summary>
        /// At this point, we have a list of viable symbols and no parameter list with which to perform
        /// overload resolution.  We'll just return the first symbol, giving a diagnostic if there are
        /// others.
        /// Caveat: If there are multiple candidates and only one is from source, then the source symbol
        /// wins and no diagnostic is reported.
        /// </summary>
        private ImmutableArray<Symbol> ProcessParameterlessCrefMemberLookupResults(
            ImmutableArray<Symbol> symbols,
            int arity,
            MemberCrefSyntax memberSyntax,
            TypeArgumentListSyntax? typeArgumentListSyntax,
            out Symbol? ambiguityWinner,
            BindingDiagnosticBag diagnostics)
        {
            // If the syntax indicates arity zero, then we match methods of any arity.
            // However, if there are both generic and non-generic methods, then the
            // generic methods should be ignored.
            if (symbols.Length > 1 && arity == 0)
            {
                bool hasNonGenericMethod = false;
                bool hasGenericMethod = false;
                foreach (Symbol s in symbols)
                {
                    if (s.Kind != SymbolKind.Method)
                    {
                        continue;
                    }

                    if (((MethodSymbol)s).Arity == 0)
                    {
                        hasNonGenericMethod = true;
                    }
                    else
                    {
                        hasGenericMethod = true;
                    }

                    if (hasGenericMethod && hasNonGenericMethod)
                    {
                        break; //Nothing else to be learned.
                    }
                }

                if (hasNonGenericMethod && hasGenericMethod)
                {
                    symbols = symbols.WhereAsArray(s =>
                        s.Kind != SymbolKind.Method || ((MethodSymbol)s).Arity == 0);
                }
            }

            Debug.Assert(!symbols.IsEmpty);

            Symbol symbol = symbols[0];

            // If there's ambiguity, prefer source symbols.
            // Logic is similar to ResultSymbol, but separate because the error handling is totally different.
            if (symbols.Length > 1)
            {
                // Size is known, but IndexOfSymbolFromCurrentCompilation expects a builder.
                ArrayBuilder<Symbol> unwrappedSymbols = ArrayBuilder<Symbol>.GetInstance(symbols.Length);

                foreach (Symbol wrapped in symbols)
                {
                    unwrappedSymbols.Add(UnwrapAliasNoDiagnostics(wrapped));
                }

                BestSymbolInfo secondBest;
                BestSymbolInfo best = GetBestSymbolInfo(unwrappedSymbols, out secondBest);

                Debug.Assert(!best.IsNone);
                Debug.Assert(!secondBest.IsNone);

                unwrappedSymbols.Free();

                int symbolIndex = 0;

                if (best.IsFromCompilation)
                {
                    symbolIndex = best.Index;
                    symbol = symbols[symbolIndex]; // NOTE: symbols, not unwrappedSymbols.
                }

                if (symbol.Kind == SymbolKind.TypeParameter)
                {
                    CrefSyntax crefSyntax = GetRootCrefSyntax(memberSyntax);
                    diagnostics.Add(ErrorCode.WRN_BadXMLRefTypeVar, crefSyntax.Location, crefSyntax.ToString());
                }
                else if (secondBest.IsFromCompilation == best.IsFromCompilation)
                {
                    CrefSyntax crefSyntax = GetRootCrefSyntax(memberSyntax);
                    int otherIndex = symbolIndex == 0 ? 1 : 0;
                    diagnostics.Add(ErrorCode.WRN_AmbiguousXMLReference, crefSyntax.Location, crefSyntax.ToString(),
                        new FormattedSymbol(symbol, SymbolDisplayFormat.CSharpErrorMessageFormat),
                        new FormattedSymbol(symbols[otherIndex], SymbolDisplayFormat.CSharpErrorMessageFormat));

                    ambiguityWinner = ConstructWithCrefTypeParameters(arity, typeArgumentListSyntax, symbol);
                    return symbols.SelectAsArray(sym => ConstructWithCrefTypeParameters(arity, typeArgumentListSyntax, sym));
                }
            }
            else if (symbol.Kind == SymbolKind.TypeParameter)
            {
                CrefSyntax crefSyntax = GetRootCrefSyntax(memberSyntax);
                diagnostics.Add(ErrorCode.WRN_BadXMLRefTypeVar, crefSyntax.Location, crefSyntax.ToString());
            }

            ambiguityWinner = null;
            return ImmutableArray.Create<Symbol>(ConstructWithCrefTypeParameters(arity, typeArgumentListSyntax, symbol));
        }

        /// <summary>
        /// Replace any named type in the symbol list with its instance constructors.
        /// Construct all candidates with the implicitly-declared CrefTypeParameterSymbols.
        /// </summary>
        private void GetCrefOverloadResolutionCandidates(ImmutableArray<Symbol> symbols, int arity, TypeArgumentListSyntax? typeArgumentListSyntax, ArrayBuilder<Symbol> candidates)
        {
            foreach (Symbol candidate in symbols)
            {
                Symbol constructedCandidate = ConstructWithCrefTypeParameters(arity, typeArgumentListSyntax, candidate);
                NamedTypeSymbol? constructedCandidateType = constructedCandidate as NamedTypeSymbol;
                if ((object?)constructedCandidateType == null)
                {
                    // Construct before overload resolution so the signatures will match.
                    candidates.Add(constructedCandidate);
                }
                else
                {
                    candidates.AddRange(constructedCandidateType.InstanceConstructors);
                }
            }
        }

        /// <summary>
        /// Given a list of method and/or property candidates, choose the first one (if any) with a signature
        /// that matches the parameter list in the cref.  Return null if there isn't one.
        /// </summary>
        /// <remarks>
        /// Produces a diagnostic for ambiguous matches, but not for unresolved members - WRN_BadXMLRef is
        /// handled in BindMemberCref.
        /// </remarks>
        private static ImmutableArray<Symbol> PerformCrefOverloadResolution(ArrayBuilder<Symbol> candidates, ImmutableArray<ParameterSymbol> parameterSymbols, int arity, MemberCrefSyntax memberSyntax, out Symbol? ambiguityWinner, BindingDiagnosticBag diagnostics)
        {
            ArrayBuilder<Symbol>? viable = null;

            foreach (Symbol candidate in candidates)
            {
                // BREAK: In dev11, any candidate with the type "dynamic" anywhere in its parameter list would be skipped
                // (see XmlDocCommentBinder::bindXmlReference).  Apparently, this was because "the params that the xml doc 
                // comments produce never will."  This does not appear to have made sense in dev11 (skipping dropping the
                // candidate doesn't cause anything to blow up and may cause resolution to start succeeding) and it almost
                // certainly does not in roslyn (the signature comparer ignores the object-dynamic distinction anyway).

                Symbol signatureMember;
                switch (candidate.Kind)
                {
                    case SymbolKind.Method:
                        {
                            MethodSymbol candidateMethod = (MethodSymbol)candidate;
                            MethodKind candidateMethodKind = candidateMethod.MethodKind;
                            bool candidateMethodIsVararg = candidateMethod.IsVararg;

                            // If the arity from the cref is zero, then we accept methods of any arity.
                            int signatureMemberArity = candidateMethodKind == MethodKind.Constructor
                                ? 0
                                : (arity == 0 ? candidateMethod.Arity : arity);

                            // CONSIDER: we might want to reuse this method symbol (as long as the MethodKind and Vararg-ness match).
                            signatureMember = new SignatureOnlyMethodSymbol(
                                methodKind: candidateMethodKind,
                                typeParameters: IndexedTypeParameterSymbol.TakeSymbols(signatureMemberArity),
                                parameters: parameterSymbols,
                                // This specific comparer only looks for varargs.
                                callingConvention: candidateMethodIsVararg ? Microsoft.Cci.CallingConvention.ExtraArguments : Microsoft.Cci.CallingConvention.HasThis,
                                // These are ignored by this specific MemberSignatureComparer.
                                containingType: null,
                                name: null,
                                refKind: RefKind.None,
                                isInitOnly: false,
                                isStatic: false,
                                returnType: default,
                                refCustomModifiers: ImmutableArray<CustomModifier>.Empty,
                                explicitInterfaceImplementations: ImmutableArray<MethodSymbol>.Empty);
                            break;
                        }

                    case SymbolKind.Property:
                        {
                            // CONSIDER: we might want to reuse this property symbol.
                            signatureMember = new SignatureOnlyPropertySymbol(
                                parameters: parameterSymbols,
                                // These are ignored by this specific MemberSignatureComparer.
                                containingType: null,
                                name: null,
                                refKind: RefKind.None,
                                type: default,
                                refCustomModifiers: ImmutableArray<CustomModifier>.Empty,
                                isStatic: false,
                                explicitInterfaceImplementations: ImmutableArray<PropertySymbol>.Empty);
                            break;
                        }

                    case SymbolKind.NamedType:
                        // Because we replaced them with constructors when we built the candidate list.
                        throw ExceptionUtilities.UnexpectedValue(candidate.Kind);

                    default:
                        continue;
                }

                if (MemberSignatureComparer.CrefComparer.Equals(signatureMember, candidate))
                {
                    Debug.Assert(candidate.GetMemberArity() != 0 || candidate.Name == WellKnownMemberNames.InstanceConstructorName || arity == 0,
                        "Can only have a 0-arity, non-constructor candidate if the desired arity is 0.");

                    if (viable == null)
                    {
                        viable = ArrayBuilder<Symbol>.GetInstance();
                        viable.Add(candidate);
                    }
                    else
                    {
                        bool oldArityIsZero = viable[0].GetMemberArity() == 0;
                        bool newArityIsZero = candidate.GetMemberArity() == 0;

                        // If the cref specified arity 0 and the current candidate has arity 0 but the previous
                        // match did not, then the current candidate is the unambiguous winner (unless there's
                        // another match with arity 0 in a subsequent iteration).
                        if (!oldArityIsZero || newArityIsZero)
                        {
                            if (!oldArityIsZero && newArityIsZero)
                            {
                                viable.Clear();
                            }

                            viable.Add(candidate);
                        }
                    }
                }
            }

            if (viable == null)
            {
                ambiguityWinner = null;
                return ImmutableArray<Symbol>.Empty;
            }

            if (viable.Count > 1)
            {
                ambiguityWinner = viable[0];
                CrefSyntax crefSyntax = GetRootCrefSyntax(memberSyntax);
                diagnostics.Add(ErrorCode.WRN_AmbiguousXMLReference, crefSyntax.Location, crefSyntax.ToString(), ambiguityWinner, viable[1]);
            }
            else
            {
                ambiguityWinner = null;
            }

            return viable.ToImmutableAndFree();
        }

        /// <summary>
        /// If the member is generic, construct it with the CrefTypeParameterSymbols that should be in scope.
        /// </summary>
        private Symbol ConstructWithCrefTypeParameters(int arity, TypeArgumentListSyntax? typeArgumentListSyntax, Symbol symbol)
        {
            if (arity > 0)
            {
                Debug.Assert(typeArgumentListSyntax is object);
                SeparatedSyntaxList<TypeSyntax> typeArgumentSyntaxes = typeArgumentListSyntax.Arguments;
                var typeArgumentsWithAnnotations = ArrayBuilder<TypeWithAnnotations>.GetInstance(arity);

                var unusedDiagnostics =
#if DEBUG
                    BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                Debug.Assert(unusedDiagnostics.DiagnosticBag is object);
#else
                    BindingDiagnosticBag.Discarded;
#endif
                for (int i = 0; i < arity; i++)
                {
                    TypeSyntax typeArgumentSyntax = typeArgumentSyntaxes[i];

                    var typeArgument = BindType(typeArgumentSyntax, unusedDiagnostics);
                    typeArgumentsWithAnnotations.Add(typeArgument);

                    // Should be in a WithCrefTypeParametersBinder.
                    Debug.Assert(typeArgumentSyntax.ContainsDiagnostics || !typeArgumentSyntax.SyntaxTree.ReportDocumentationCommentDiagnostics() ||
                        (!unusedDiagnostics.HasAnyErrors() && typeArgument.Type is CrefTypeParameterSymbol));

#if DEBUG
                    unusedDiagnostics.DiagnosticBag.Clear();
#endif
                }
#if DEBUG
                unusedDiagnostics.Free();
#endif

                if (symbol.Kind == SymbolKind.Method)
                {
                    symbol = ((MethodSymbol)symbol).Construct(typeArgumentsWithAnnotations.ToImmutableAndFree());
                }
                else
                {
                    Debug.Assert(symbol is NamedTypeSymbol);
                    symbol = ((NamedTypeSymbol)symbol).Construct(typeArgumentsWithAnnotations.ToImmutableAndFree());
                }
            }

            return symbol;
        }

        private ImmutableArray<ParameterSymbol> BindCrefParameters(BaseCrefParameterListSyntax parameterListSyntax, BindingDiagnosticBag diagnostics)
        {
            ArrayBuilder<ParameterSymbol> parameterBuilder = ArrayBuilder<ParameterSymbol>.GetInstance(parameterListSyntax.Parameters.Count);

            foreach (CrefParameterSyntax parameter in parameterListSyntax.Parameters)
            {
                RefKind refKind = parameter.RefKindKeyword.Kind().GetRefKind();
                if (refKind == RefKind.Ref && parameter.ReadOnlyKeyword.IsKind(SyntaxKind.ReadOnlyKeyword))
                {
                    CheckFeatureAvailability(parameter.ReadOnlyKeyword, MessageID.IDS_FeatureRefReadonlyParameters, diagnostics, forceWarning: true);
                    refKind = RefKind.RefReadOnlyParameter;
                }

                Debug.Assert(parameterListSyntax.Parent is object);
                TypeSymbol type = BindCrefParameterOrReturnType(parameter.Type, (MemberCrefSyntax)parameterListSyntax.Parent, diagnostics);

                parameterBuilder.Add(new SignatureOnlyParameterSymbol(TypeWithAnnotations.Create(type), ImmutableArray<CustomModifier>.Empty, isParamsArray: false, isParamsCollection: false, refKind: refKind));
            }

            return parameterBuilder.ToImmutableAndFree();
        }

        /// <remarks>
        /// Keep in sync with CSharpSemanticModel.GetSpeculativelyBoundExpressionWithoutNullability.
        /// </remarks>
        private TypeSymbol BindCrefParameterOrReturnType(TypeSyntax typeSyntax, MemberCrefSyntax memberCrefSyntax, BindingDiagnosticBag diagnostics)
        {
            // After much deliberation, we eventually decided to suppress lookup of inherited members within
            // crefs, in order to match dev11's behavior (Changeset #829014).  Unfortunately, it turns out
            // that dev11 does not suppress these members when performing lookup within parameter and return
            // types, within crefs (DevDiv #586815, #598371).
            Debug.Assert(InCrefButNotParameterOrReturnType);
            Binder parameterOrReturnTypeBinder = this.WithAdditionalFlags(BinderFlags.CrefParameterOrReturnType);

            // It would be nice to pull this binder out of the factory so we wouldn't have to worry about them getting out
            // of sync, but this code is also used for included crefs, which don't have BinderFactories.
            // As a compromise, we'll assert that the binding locations match in scenarios where we can go through the factory.
            Debug.Assert(!this.Compilation.ContainsSyntaxTree(typeSyntax.SyntaxTree) ||
                this.Compilation.GetBinderFactory(typeSyntax.SyntaxTree).GetBinder(typeSyntax).Flags ==
                (parameterOrReturnTypeBinder.Flags & ~BinderFlags.SemanticModel));

            var localDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, // Examined, but not reported.
                                                                    withDependencies: diagnostics.AccumulatesDependencies);
            Debug.Assert(localDiagnostics.DiagnosticBag is object);

            TypeSymbol type = parameterOrReturnTypeBinder.BindType(typeSyntax, localDiagnostics).Type;

            if (localDiagnostics.HasAnyErrors())
            {
                if (HasNonObsoleteError(localDiagnostics.DiagnosticBag))
                {
                    Debug.Assert(typeSyntax.Parent is object);
                    CrefSyntax crefSyntax = GetRootCrefSyntax(memberCrefSyntax);
                    if (typeSyntax.Parent.Kind() == SyntaxKind.ConversionOperatorMemberCref)
                    {
                        diagnostics.Add(ErrorCode.WRN_BadXMLRefReturnType, typeSyntax.Location);
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.WRN_BadXMLRefParamType, typeSyntax.Location, typeSyntax.ToString(), crefSyntax.ToString());
                    }
                }
            }
            else
            {
                Debug.Assert(type.TypeKind != TypeKind.Error || typeSyntax.ContainsDiagnostics || !typeSyntax.SyntaxTree.ReportDocumentationCommentDiagnostics(), "Why wasn't there a diagnostic?");
            }

            diagnostics.AddDependencies(localDiagnostics);
            localDiagnostics.Free();

            return type;
        }

        private static bool HasNonObsoleteError(DiagnosticBag unusedDiagnostics)
        {
            foreach (Diagnostic diag in unusedDiagnostics.AsEnumerable())
            {
                // CONSIDER: If this check is too slow, we could add a helper to DiagnosticBag
                // that checks for unrealized diagnostics without expanding them.
                switch ((ErrorCode)diag.Code)
                {
                    case ErrorCode.ERR_DeprecatedSymbolStr:
                    case ErrorCode.ERR_DeprecatedCollectionInitAddStr:
                        break;
                    default:
                        if (diag.Severity == DiagnosticSeverity.Error)
                        {
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        private static CrefSyntax GetRootCrefSyntax(MemberCrefSyntax syntax)
        {
            SyntaxNode? parentSyntax = syntax.Parent; // Could be null when speculating.
            return parentSyntax == null || parentSyntax.IsKind(SyntaxKind.XmlCrefAttribute)
                ? syntax
                : (CrefSyntax)parentSyntax;
        }
    }
}
