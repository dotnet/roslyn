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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class AttributeNamedParameterCompletionProvider : CommonCompletionProvider
    {
        private const string EqualsString = "=";
        private const string SpaceEqualsString = " =";
        private const string ColonString = ":";

        private static readonly CompletionItemRules _spaceItemFilterRule = CompletionItemRules.Default.WithFilterCharacterRule(
            CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ' '));

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
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
                if (!(token.Parent.Parent is AttributeSyntax attributeSyntax) || !(token.Parent is AttributeArgumentListSyntax attributeArgumentList))
                {
                    return;
                }

                if (IsAfterNameColonArgument(token) || IsAfterNameEqualsArgument(token))
                {
                    context.IsExclusive = true;
                }

                // We actually want to collect two sets of named parameters to present the user.  The
                // normal named parameters that come from the attribute constructors.  These will be
                // presented like "goo:".  And also the named parameters that come from the writable
                // fields/properties in the attribute.  These will be presented like "bar =".  

                var existingNamedParameters = GetExistingNamedParameters(attributeArgumentList, position);

                var workspace = document.Project.Solution.Workspace;
                var semanticModel = await document.GetSemanticModelForNodeAsync(attributeSyntax, cancellationToken).ConfigureAwait(false);
                var nameColonItems = await GetNameColonItemsAsync(context, semanticModel, token, attributeSyntax, existingNamedParameters).ConfigureAwait(false);
                var nameEqualsItems = await GetNameEqualsItemsAsync(context, semanticModel, token, attributeSyntax, existingNamedParameters).ConfigureAwait(false);

                context.AddItems(nameEqualsItems);

                // If we're after a name= parameter, then we only want to show name= parameters.
                // Otherwise, show name: parameters too.
                if (!IsAfterNameEqualsArgument(token))
                {
                    context.AddItems(nameColonItems);
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // nop
            }
        }

        private bool IsAfterNameColonArgument(SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.CommaToken && token.Parent is AttributeArgumentListSyntax argumentList)
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
            if (token.Kind() == SyntaxKind.CommaToken && token.Parent is AttributeArgumentListSyntax argumentList)
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

        private async Task<ImmutableArray<CompletionItem>> GetNameEqualsItemsAsync(
            CompletionContext context, SemanticModel semanticModel,
            SyntaxToken token, AttributeSyntax attributeSyntax, ISet<string> existingNamedParameters)
        {
            var attributeNamedParameters = GetAttributeNamedParameters(semanticModel, context.Position, attributeSyntax, context.CancellationToken);
            var unspecifiedNamedParameters = attributeNamedParameters.Where(p => !existingNamedParameters.Contains(p.Name));

            var text = await semanticModel.SyntaxTree.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            var q = from p in attributeNamedParameters
                    where !existingNamedParameters.Contains(p.Name)
                    select SymbolCompletionItem.CreateWithSymbolId(
                       displayText: p.Name.ToIdentifierToken().ToString(),
                       displayTextSuffix: SpaceEqualsString,
                       insertionText: null,
                       symbols: ImmutableArray.Create(p),
                       contextPosition: token.SpanStart,
                       sortText: p.Name,
                       rules: _spaceItemFilterRule);
            return q.ToImmutableArray();
        }

        private async Task<IEnumerable<CompletionItem>> GetNameColonItemsAsync(
            CompletionContext context, SemanticModel semanticModel, SyntaxToken token, AttributeSyntax attributeSyntax, ISet<string> existingNamedParameters)
        {
            var parameterLists = GetParameterLists(semanticModel, context.Position, attributeSyntax, context.CancellationToken);
            parameterLists = parameterLists.Where(pl => IsValid(pl, existingNamedParameters));

            var text = await semanticModel.SyntaxTree.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            return from pl in parameterLists
                   from p in pl
                   where !existingNamedParameters.Contains(p.Name)
                   select SymbolCompletionItem.CreateWithSymbolId(
                       displayText: p.Name.ToIdentifierToken().ToString(),
                       displayTextSuffix: ColonString,
                       insertionText: null,
                       symbols: ImmutableArray.Create(p),
                       contextPosition: token.SpanStart,
                       sortText: p.Name,
                       rules: CompletionItemRules.Default);
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

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
            if (within != null && semanticModel.GetTypeInfo(attribute, cancellationToken).Type is INamedTypeSymbol attributeType)
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

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetTextChange(selectedItem, ch));
        }

        private TextChange? GetTextChange(CompletionItem selectedItem, char? ch)
        {
            var displayText = selectedItem.DisplayText + selectedItem.DisplayTextSuffix;

            if (ch != null)
            {
                // If user types a space, do not complete the " =" (space and equals) at the end of a named parameter. The
                // typed space character will be passed through to the editor, and they can then type the '='.
                if (ch == ' ' && displayText.EndsWith(SpaceEqualsString, StringComparison.Ordinal))
                {
                    return new TextChange(selectedItem.Span, displayText.Remove(displayText.Length - SpaceEqualsString.Length));
                }

                // If the user types '=', do not complete the '=' at the end of the named parameter because the typed '=' 
                // will be passed through to the editor.
                if (ch == '=' && displayText.EndsWith(EqualsString, StringComparison.Ordinal))
                {
                    return new TextChange(selectedItem.Span, displayText.Remove(displayText.Length - EqualsString.Length));
                }

                // If the user types ':', do not complete the ':' at the end of the named parameter because the typed ':' 
                // will be passed through to the editor.
                if (ch == ':' && displayText.EndsWith(ColonString, StringComparison.Ordinal))
                {
                    return new TextChange(selectedItem.Span, displayText.Remove(displayText.Length - ColonString.Length));
                }
            }

            return new TextChange(selectedItem.Span, displayText);
        }
    }
}
