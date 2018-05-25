// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectedDeclarations = syntaxFacts.GetSelectedMembers(root, textSpan);

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

        protected ImmutableArray<IParameterSymbol> DetermineParameters(
            ImmutableArray<ISymbol> selectedMembers)
        {
            var parameters = ArrayBuilder<IParameterSymbol>.GetInstance();

            foreach (var symbol in selectedMembers)
            {
                var type = symbol is IFieldSymbol
                    ? ((IFieldSymbol)symbol).Type
                    : ((IPropertySymbol)symbol).Type;

                parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default,
                    refKind: RefKind.None,
                    isParams: false,
                    type: type,
                    name: symbol.Name.ToCamelCase().TrimStart(s_underscore)));
            }

            return parameters.ToImmutableAndFree();
        }

        private static readonly char[] s_underscore = { '_' };

        protected IMethodSymbol GetDelegatedConstructor(
            INamedTypeSymbol containingType,
            ImmutableArray<IParameterSymbol> parameters)
        {
            var q =
                from c in containingType.InstanceConstructors
                orderby c.Parameters.Length descending
                where c.Parameters.Length > 0 && c.Parameters.Length < parameters.Length
                where c.Parameters.All(p => p.RefKind == RefKind.None) && !c.Parameters.Any(p => p.IsParams)
                let constructorTypes = c.Parameters.Select(p => p.Type)
                let symbolTypes = parameters.Take(c.Parameters.Length).Select(p => p.Type)
                where constructorTypes.SequenceEqual(symbolTypes)
                select c;

            return q.FirstOrDefault();
        }

        protected IMethodSymbol GetMatchingConstructor(INamedTypeSymbol containingType, ImmutableArray<IParameterSymbol> parameters)
            => containingType.InstanceConstructors.FirstOrDefault(c => MatchesConstructor(c, parameters));

        private bool MatchesConstructor(IMethodSymbol constructor, ImmutableArray<IParameterSymbol> parameters)
            => parameters.Select(p => p.Type).SequenceEqual(constructor.Parameters.Select(p => p.Type));

        protected static readonly SymbolDisplayFormat SimpleFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
    }
}
