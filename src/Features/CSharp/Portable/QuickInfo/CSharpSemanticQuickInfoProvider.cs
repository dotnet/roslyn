﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(QuickInfoProviderNames.Semantic, LanguageNames.CSharp), Shared]
    internal class CSharpSemanticQuickInfoProvider : CommonSemanticQuickInfoProvider
    {
        [ImportingConstructor]
        public CSharpSemanticQuickInfoProvider()
        {
        }

        /// <summary>
        /// If the token is the '=>' in a lambda, or the 'delegate' in an anonymous function,
        /// return the syntax for the lambda or anonymous function.
        /// </summary>
        protected override bool GetBindableNodeForTokenIndicatingLambda(SyntaxToken token, out SyntaxNode found)
        {
            if (token.IsKind(SyntaxKind.EqualsGreaterThanToken)
                && token.Parent.IsKind(SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression))
            {
                // () =>
                found = token.Parent;
                return true;
            }
            else if (token.IsKind(SyntaxKind.DelegateKeyword) && token.Parent.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                // delegate (...) { ... }
                found = token.Parent;
                return true;
            }

            found = null;
            return false;
        }

        protected override bool ShouldCheckPreviousToken(SyntaxToken token)
        {
            return !token.Parent.IsKind(SyntaxKind.XmlCrefAttribute);
        }

        protected override ImmutableArray<TaggedText> TryGetNullabilityAnalysis(Workspace workspace, SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        {
            // Anything less than C# 8 we just won't show anything, even if the compiler could theoretically give analysis
            var parseOptions = (CSharpParseOptions)token.SyntaxTree.Options;
            if (parseOptions.LanguageVersion < LanguageVersion.CSharp8)
            {
                return default;
            }

            var syntaxFacts = workspace.Services.GetLanguageServices(semanticModel.Language).GetRequiredService<ISyntaxFactsService>();
            var bindableParent = syntaxFacts.GetBindableParent(token);
            var symbolInfo = semanticModel.GetSymbolInfo(bindableParent, cancellationToken);

            if (symbolInfo.Symbol == null || string.IsNullOrEmpty(symbolInfo.Symbol.Name))
            {
                return default;
            }

            // Although GetTypeInfo can return nullability for uses of all sorts of things, it's not always useful for quick info.
            // For example, if you have a call to a method with a nullable return, the fact it can be null is already captured
            // in the return type shown -- there's no flow analysis information there.
            if (symbolInfo.Symbol.Kind != SymbolKind.Event &&
                symbolInfo.Symbol.Kind != SymbolKind.Field &&
                symbolInfo.Symbol.Kind != SymbolKind.Local &&
                symbolInfo.Symbol.Kind != SymbolKind.Parameter &&
                symbolInfo.Symbol.Kind != SymbolKind.Property &&
                symbolInfo.Symbol.Kind != SymbolKind.RangeVariable)
            {
                return default;
            }

            var typeInfo = semanticModel.GetTypeInfo(bindableParent, cancellationToken);

            string messageTemplate = null;

            if (typeInfo.Nullability.FlowState == NullableFlowState.NotNull)
            {
                messageTemplate = CSharpFeaturesResources._0_is_not_null_here;
            }
            else if (typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull)
            {
                messageTemplate = CSharpFeaturesResources._0_may_be_null_here;
            }

            if (messageTemplate != null)
            {
                return ImmutableArray.Create(new TaggedText(TextTags.Text, string.Format(messageTemplate, symbolInfo.Symbol.Name)));
            }
            else
            {
                return default;
            }
        }
    }
}
