// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateFromMembers
{
    internal abstract partial class AbstractGenerateFromMembersCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected AbstractGenerateFromMembersCodeRefactoringProvider()
        {
        }

        /// <summary>
        /// Gets the enclosing named type for the specified position.  We can't use
        /// <see cref="SemanticModel.GetEnclosingSymbol"/> because that doesn't return
        /// the type you're current on if you're on the header of a class/interface.
        /// </summary>
        internal static INamedTypeSymbol GetEnclosingNamedType(
            SemanticModel semanticModel, SyntaxNode root, int start, CancellationToken cancellationToken)
        {
            var token = root.FindToken(start);
            if (token == ((ICompilationUnitSyntax)root).EndOfFileToken)
            {
                token = token.GetPreviousToken();
            }

            for (var node = token.Parent; node != null; node = node.Parent)
            {
                if (semanticModel.GetDeclaredSymbol(node) is INamedTypeSymbol declaration)
                {
                    return declaration;
                }
            }

            return null;
        }

        protected async Task<SelectedMemberInfo> GetSelectedMemberInfoAsync(
            Document document, TextSpan textSpan, bool allowPartialSelection, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectedDeclarations = syntaxFacts.GetSelectedFieldsAndProperties(root, textSpan, allowPartialSelection);

            if (selectedDeclarations.Length > 0)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var selectedMembers = selectedDeclarations.SelectMany(
                    d => semanticFacts.GetDeclaredSymbols(semanticModel, d, cancellationToken)).WhereNotNull().ToImmutableArray();
                if (selectedMembers.Length > 0)
                {
                    var containingType = selectedMembers.First().ContainingType;
                    if (containingType != null)
                    {
                        return new SelectedMemberInfo(containingType, selectedDeclarations, selectedMembers);
                    }
                }
            }

            return null;
        }

        protected static bool IsReadableInstanceFieldOrProperty(ISymbol symbol)
            => !symbol.IsStatic && IsReadableFieldOrProperty(symbol);

        protected static bool IsWritableInstanceFieldOrProperty(ISymbol symbol)
            => !symbol.IsStatic && IsWritableFieldOrProperty(symbol);

        private static bool IsReadableFieldOrProperty(ISymbol symbol)
        {
            switch (symbol)
            {
                case IFieldSymbol field: return IsViableField(field);
                case IPropertySymbol property: return IsViableProperty(property) && !property.IsWriteOnly;
                default: return false;
            }
        }

        private static bool IsWritableFieldOrProperty(ISymbol symbol)
        {
            switch (symbol)
            {
                // Can use non const fields and properties with setters in them.
                case IFieldSymbol field: return IsViableField(field) && !field.IsConst;
                case IPropertySymbol property: return IsViableProperty(property) && property.IsWritableInConstructor();
                default: return false;
            }
        }

        private static bool IsViableField(IFieldSymbol field)
            => field.AssociatedSymbol == null;

        private static bool IsViableProperty(IPropertySymbol property)
            => property.Parameters.IsEmpty;

        /// <summary>
        /// Returns an array of parameter symbols that correspond to selected member symbols.
        /// If a selected member symbol has an empty base identifier name, the parameter symbol will not be added.
        /// </summary>
        /// <param name="selectedMembers"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        protected ImmutableArray<IParameterSymbol> DetermineParameters(
            ImmutableArray<ISymbol> selectedMembers, ImmutableArray<NamingRule> rules)
        {
            var parameters = ArrayBuilder<IParameterSymbol>.GetInstance();

            foreach (var symbol in selectedMembers)
            {
                var type = symbol.GetMemberType();

                var identifierNameParts = IdentifierNameParts.CreateIdentifierNameParts(symbol, rules);
                if (identifierNameParts.BaseName == "")
                {
                    continue;
                }

                var parameterNamingRule = rules.Where(rule => rule.SymbolSpecification.AppliesTo(SymbolKind.Parameter, Accessibility.NotApplicable)).First();
                var parameterName = parameterNamingRule.NamingStyle.MakeCompliant(identifierNameParts.BaseName).First();

                parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default,
                    refKind: RefKind.None,
                    isParams: false,
                    type: type,
                    name: parameterName));
            }

            return parameters.ToImmutableAndFree();
        }

        private static readonly char[] s_underscore = { '_' };

        protected static readonly SymbolDisplayFormat SimpleFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
    }
}
