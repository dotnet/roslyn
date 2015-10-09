// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class AttributeNamedParameterCompletionProvider : CompletionListProvider
    {
        private const string EqualsString = "=";
        private const string SpaceEqualsString = " =";
        private const string ColonString = ":";

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsInNonUserCode(position, cancellationToken))
            {
                return;
            }

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (!token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken))
            {
                return;
            }

            var attributeArgumentList = token.Parent as AttributeArgumentListSyntax;
            var attributeSyntax = token.Parent.Parent as AttributeSyntax;
            if (attributeSyntax == null || attributeArgumentList == null)
            {
                return;
            }

            if (IsAfterNameColonArgument(token) || IsAfterNameEqualsArgument(token))
            {
                context.MakeExclusive(true);
            }

            // We actually want to collect two sets of named parameters to present the user.  The
            // normal named parameters that come from the attribute constructors.  These will be
            // presented like "foo:".  And also the named parameters that come from the writable
            // fields/properties in the attribute.  These will be presented like "bar =".  

            var existingNamedParameters = GetExistingNamedParameters(attributeArgumentList, position);

            var workspace = document.Project.Solution.Workspace;
            var semanticModel = await document.GetSemanticModelForNodeAsync(attributeSyntax, cancellationToken).ConfigureAwait(false);
            var nameColonItems = await GetNameColonItemsAsync(workspace, semanticModel, position, token, attributeSyntax, existingNamedParameters, cancellationToken).ConfigureAwait(false);
            var nameEqualsItems = await GetNameEqualsItemsAsync(workspace, semanticModel, position, token, attributeSyntax, existingNamedParameters, cancellationToken).ConfigureAwait(false);

            context.AddItems(nameEqualsItems);

            // If we're after a name= parameter, then we only want to show name= parameters.
            // Otherwise, show name: parameters too.
            if (!IsAfterNameEqualsArgument(token))
            {
                context.AddItems(nameColonItems);
            }
        }

        private bool IsAfterNameColonArgument(SyntaxToken token)
        {
            var argumentList = token.Parent as AttributeArgumentListSyntax;
            if (token.Kind() == SyntaxKind.CommaToken && argumentList != null)
            {
                foreach (var item in argumentList.Arguments.GetWithSeparators())
                {
                    if (item.IsToken && item.AsToken() == token)
                    {
                        return false;
                    }

                    if (item.IsNode)
                    {
                        var node = item.AsNode() as AttributeArgumentSyntax;
                        if (node.NameColon != null)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsAfterNameEqualsArgument(SyntaxToken token)
        {
            var argumentList = token.Parent as AttributeArgumentListSyntax;
            if (token.Kind() == SyntaxKind.CommaToken && argumentList != null)
            {
                foreach (var item in argumentList.Arguments.GetWithSeparators())
                {
                    if (item.IsToken && item.AsToken() == token)
                    {
                        return false;
                    }

                    if (item.IsNode)
                    {
                        var node = item.AsNode() as AttributeArgumentSyntax;
                        if (node.NameEquals != null)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private async Task<IEnumerable<CompletionItem>> GetNameEqualsItemsAsync(Workspace workspace, SemanticModel semanticModel,
            int position, SyntaxToken token, AttributeSyntax attributeSyntax, ISet<string> existingNamedParameters,
            CancellationToken cancellationToken)
        {
            var attributeNamedParameters = GetAttributeNamedParameters(semanticModel, position, attributeSyntax, cancellationToken);
            var unspecifiedNamedParameters = attributeNamedParameters.Where(p => !existingNamedParameters.Contains(p.Name));

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return from p in attributeNamedParameters
                   where !existingNamedParameters.Contains(p.Name)
                   select new CompletionItem(
                       this,
                       p.Name.ToIdentifierToken().ToString() + SpaceEqualsString,
                       CompletionUtilities.GetTextChangeSpan(text, position),
                       CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, token.SpanStart, p),
                       p.GetGlyph(),
                       sortText: p.Name,
                       rules: ItemRules.Instance);
        }

        private async Task<IEnumerable<CompletionItem>> GetNameColonItemsAsync(
            Workspace workspace, SemanticModel semanticModel, int position, SyntaxToken token, AttributeSyntax attributeSyntax, ISet<string> existingNamedParameters,
            CancellationToken cancellationToken)
        {
            var parameterLists = GetParameterLists(semanticModel, position, attributeSyntax, cancellationToken);
            parameterLists = parameterLists.Where(pl => IsValid(pl, existingNamedParameters));

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return from pl in parameterLists
                   from p in pl
                   where !existingNamedParameters.Contains(p.Name)
                   select new CompletionItem(
                       this,
                       p.Name.ToIdentifierToken().ToString() + ColonString,
                       CompletionUtilities.GetTextChangeSpan(text, position),
                       CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, token.SpanStart, p),
                       p.GetGlyph(),
                       sortText: p.Name,
                       rules: ItemRules.Instance);
        }

        private bool IsValid(ImmutableArray<IParameterSymbol> parameterList, ISet<string> existingNamedParameters)
        {
            return existingNamedParameters.Except(parameterList.Select(p => p.Name)).IsEmpty();
        }

        private ISet<string> GetExistingNamedParameters(AttributeArgumentListSyntax argumentList, int position)
        {
            var existingArguments1 =
                argumentList.Arguments.Where(a => a.Span.End <= position)
                                      .Where(a => a.NameColon != null)
                                      .Select(a => a.NameColon.Name.Identifier.ValueText);
            var existingArguments2 =
                argumentList.Arguments.Where(a => a.Span.End <= position)
                                      .Where(a => a.NameEquals != null)
                                      .Select(a => a.NameEquals.Name.Identifier.ValueText);

            return existingArguments1.Concat(existingArguments2).ToSet();
        }

        private IEnumerable<ImmutableArray<IParameterSymbol>> GetParameterLists(
            SemanticModel semanticModel,
            int position,
            AttributeSyntax attribute,
            CancellationToken cancellationToken)
        {
            var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            var attributeType = semanticModel.GetTypeInfo(attribute, cancellationToken).Type as INamedTypeSymbol;
            if (within != null && attributeType != null)
            {
                return attributeType.InstanceConstructors.Where(c => c.IsAccessibleWithin(within))
                                                         .Select(c => c.Parameters);
            }

            return SpecializedCollections.EmptyEnumerable<ImmutableArray<IParameterSymbol>>();
        }

        private IEnumerable<ISymbol> GetAttributeNamedParameters(
            SemanticModel semanticModel,
            int position,
            AttributeSyntax attribute,
            CancellationToken cancellationToken)
        {
            var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            var attributeType = semanticModel.GetTypeInfo(attribute, cancellationToken).Type as INamedTypeSymbol;
            return attributeType.GetAttributeNamedParameters(semanticModel.Compilation, within);
        }
    }
}
