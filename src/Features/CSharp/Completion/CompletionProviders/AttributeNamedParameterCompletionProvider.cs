// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class AttributeNamedParameterCompletionProvider : AbstractCompletionProvider
    {
        private const string EqualsString = "=";
        private const string SpaceEqualsString = " =";
        private const string ColonString = ":";

        public override bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return CompletionUtilities.IsCommitCharacter(completionItem, ch, textTypedSoFar);
        }

        public override bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar)
        {
            return CompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override TextChange GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            var displayText = selectedItem.DisplayText;

            if (ch != null)
            {
                // If user types a space, do not complete the " =" (space and equals) at the end of a named parameter. The
                // typed space character will be passed through to the editor, and they can then type the '='.
                if (ch == ' ' && displayText.EndsWith(SpaceEqualsString, StringComparison.Ordinal))
                {
                    return new TextChange(selectedItem.FilterSpan, displayText.Remove(displayText.Length - SpaceEqualsString.Length));
                }

                // If the user types '=', do not complete the '=' at the end of the named parameter because the typed '=' 
                // will be passed through to the editor.
                if (ch == '=' && displayText.EndsWith(EqualsString, StringComparison.Ordinal))
                {
                    return new TextChange(selectedItem.FilterSpan, displayText.Remove(displayText.Length - EqualsString.Length));
                }

                // If the user types ':', do not complete the ':' at the end of the named parameter because the typed ':' 
                // will be passed through to the editor.
                if (ch == ':' && displayText.EndsWith(ColonString, StringComparison.Ordinal))
                {
                    return new TextChange(selectedItem.FilterSpan, displayText.Remove(displayText.Length - ColonString.Length));
                }
            }

            return new TextChange(selectedItem.FilterSpan, displayText);
        }

        protected override async Task<bool> IsExclusiveAsync(Document document, int caretPosition, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = syntaxTree.FindTokenOnLeftOfPosition(caretPosition, cancellationToken)
                                  .GetPreviousTokenIfTouchingWord(caretPosition);

            return IsAfterNameColonArgument(token) || IsAfterNameEqualsArgument(token);
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

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(
            Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsInNonUserCode(position, cancellationToken))
            {
                return null;
            }

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.Kind() != SyntaxKind.OpenParenToken && token.Kind() != SyntaxKind.CommaToken)
            {
                return null;
            }

            var attributeArgumentList = token.Parent as AttributeArgumentListSyntax;
            var attributeSyntax = token.Parent.Parent as AttributeSyntax;
            if (attributeSyntax == null || attributeArgumentList == null)
            {
                return null;
            }

            // We actually want to collect two sets of named parameters to present the user.  The
            // normal named parameters that come from the attribute constructors.  These will be
            // presented like "foo:".  And also the named parameters that come from the writable
            // fields/properties in the attribute.  These will be presented like "bar =".  

            var existingNamedParameters = GetExistingNamedParameters(attributeArgumentList, position);

            var workspace = document.Project.Solution.Workspace;
            var semanticModel = await document.GetCSharpSemanticModelForNodeAsync(attributeSyntax, cancellationToken).ConfigureAwait(false);
            var nameColonItems = await GetNameColonItemsAsync(workspace, semanticModel, position, token, attributeSyntax, existingNamedParameters, cancellationToken).ConfigureAwait(false);
            var nameEqualsItems = await GetNameEqualsItemsAsync(workspace, semanticModel, position, token, attributeSyntax, existingNamedParameters, cancellationToken).ConfigureAwait(false);

            // If we're after a name= parameter, then we only want to show name= parameters.
            if (IsAfterNameEqualsArgument(token))
            {
                return nameEqualsItems;
            }

            return nameColonItems.Concat(nameEqualsItems);
        }

        private async Task<IEnumerable<CompletionItem>> GetNameEqualsItemsAsync(Workspace workspace, SemanticModel semanticModel,
            int position, SyntaxToken token, AttributeSyntax attributeSyntax, ISet<string> existingNamedParameters,
            CancellationToken cancellationToken)
        {
            var attributeNamedParameters = GetAttributeNamedParameters(semanticModel, position, attributeSyntax, cancellationToken);
            var unspecifiedNamedParameters = attributeNamedParameters.Where(p => !existingNamedParameters.Contains(p.Name));

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return
            from p in attributeNamedParameters
            where !existingNamedParameters.Contains(p.Name)
            select new CSharpCompletionItem(
                workspace,
                this,
                p.Name.ToIdentifierToken().ToString() + SpaceEqualsString,
                CompletionUtilities.GetTextChangeSpan(text, position),
                CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, token.SpanStart, p),
                p.GetGlyph(),
                sortText: p.Name);
        }

        private async Task<IEnumerable<CompletionItem>> GetNameColonItemsAsync(
            Workspace workspace, SemanticModel semanticModel, int position, SyntaxToken token, AttributeSyntax attributeSyntax, ISet<string> existingNamedParameters,
            CancellationToken cancellationToken)
        {
            var parameterLists = GetParameterLists(semanticModel, position, attributeSyntax, cancellationToken);
            parameterLists = parameterLists.Where(pl => IsValid(pl, existingNamedParameters));

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return
            from pl in parameterLists
            from p in pl
            where !existingNamedParameters.Contains(p.Name)
            select new CSharpCompletionItem(
                workspace,
                this,
                p.Name.ToIdentifierToken().ToString() + ColonString,
                CompletionUtilities.GetTextChangeSpan(text, position),
                CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, token.SpanStart, p),
                p.GetGlyph(),
                sortText: p.Name);
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
