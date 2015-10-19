// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateFromMembers
{
    internal abstract class AbstractGenerateFromMembersService<TMemberDeclarationSyntax>
        where TMemberDeclarationSyntax : SyntaxNode
    {
        protected AbstractGenerateFromMembersService()
        {
        }

        protected abstract Task<IList<TMemberDeclarationSyntax>> GetSelectedMembersAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
        protected abstract IEnumerable<ISymbol> GetDeclaredSymbols(SemanticModel semanticModel, TMemberDeclarationSyntax memberDeclaration, CancellationToken cancellationToken);

        protected class SelectedMemberInfo
        {
            public INamedTypeSymbol ContainingType;
            public IList<TMemberDeclarationSyntax> SelectedDeclarations;
            public IList<ISymbol> SelectedMembers;
        }

        protected async Task<SelectedMemberInfo> GetSelectedMemberInfoAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var selectedDeclarations = await this.GetSelectedMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

            if (selectedDeclarations.Count > 0)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var selectedMembers = selectedDeclarations.SelectMany(
                    d => this.GetDeclaredSymbols(semanticModel, d, cancellationToken)).WhereNotNull().ToList();
                if (selectedMembers.Count > 0)
                {
                    var containingType = selectedMembers.First().ContainingType;
                    if (containingType != null)
                    {
                        return new SelectedMemberInfo { ContainingType = containingType, SelectedDeclarations = selectedDeclarations, SelectedMembers = selectedMembers };
                    }
                }
            }

            return null;
        }

        protected static bool IsWritableInstanceFieldOrProperty(ISymbol symbol)
        {
            // Can use non const fields and properties with setters in them.
            return
                IsInstanceFieldOrProperty(symbol) &&
                IsWritableFieldOrProperty(symbol);
        }

        private static bool IsWritableFieldOrProperty(ISymbol symbol)
        {
            return symbol.TypeSwitch(
                (IFieldSymbol field) => !field.IsConst,
                (IPropertySymbol property) => property.SetMethod != null);
        }

        protected static bool IsInstanceFieldOrProperty(ISymbol symbol)
        {
            return !symbol.IsStatic && (IsField(symbol) || IsProperty(symbol));
        }

        private static bool IsProperty(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Property;
        }

        private static bool IsField(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Field;
        }

        protected CodeRefactoring CreateCodeRefactoring(
            IList<TMemberDeclarationSyntax> selectedDeclarations,
            IEnumerable<CodeAction> actions)
        {
#if false
            var lastDeclaration = selectedDeclarations.Last();
            var endSpan = new TextSpan(lastDeclaration.Span.End - 1, 1);
            return new CodeRefactoring(actions, endSpan);
#endif
            return new CodeRefactoring(null, actions);
        }

        protected List<IParameterSymbol> DetermineParameters(
            IList<ISymbol> selectedMembers)
        {
            var parameters = new List<IParameterSymbol>();

            foreach (var symbol in selectedMembers)
            {
                var type = symbol is IFieldSymbol
                    ? ((IFieldSymbol)symbol).Type
                    : ((IPropertySymbol)symbol).Type;

                parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: null,
                    refKind: RefKind.None,
                    isParams: false,
                    type: type,
                    name: symbol.Name.ToCamelCase()));
            }

            return parameters;
        }

        protected IMethodSymbol GetDelegatedConstructor(
            INamedTypeSymbol containingType,
            List<IParameterSymbol> parameters)
        {
            var q =
                from c in containingType.InstanceConstructors
                orderby c.Parameters.Length descending
                where c.Parameters.Length > 0 && c.Parameters.Length < parameters.Count
                where c.Parameters.All(p => p.RefKind == RefKind.None) && !c.Parameters.Any(p => p.IsParams)
                let constructorTypes = c.Parameters.Select(p => p.Type)
                let symbolTypes = parameters.Take(c.Parameters.Length).Select(p => p.Type)
                where constructorTypes.SequenceEqual(symbolTypes)
                select c;

            return q.FirstOrDefault();
        }

        protected bool HasMatchingConstructor(
            INamedTypeSymbol containingType,
            List<IParameterSymbol> parameters)
        {
            return containingType.InstanceConstructors.Any(c => MatchesConstructor(c, parameters));
        }

        private bool MatchesConstructor(
            IMethodSymbol constructor,
            List<IParameterSymbol> parameters)
        {
            return parameters.Select(p => p.Type).SequenceEqual(constructor.Parameters.Select(p => p.Type));
        }

        protected static readonly SymbolDisplayFormat SimpleFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
    }
}
