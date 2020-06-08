﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(QuickInfoProviderNames.Semantic, LanguageNames.CSharp), Shared]
    internal class CSharpSemanticQuickInfoProvider : CommonSemanticQuickInfoProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSemanticQuickInfoProvider()
        {
        }

        /// <summary>
        /// If the token is the '=>' in a lambda, or the 'delegate' in an anonymous function,
        /// return the syntax for the lambda or anonymous function.
        /// </summary>
        protected override bool GetBindableNodeForTokenIndicatingLambda(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found)
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

        protected override bool GetBindableNodeForTokenIndicatingPossibleIndexerAccess(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found)
        {
            if (token.IsKind(SyntaxKind.CloseBracketToken, SyntaxKind.OpenBracketToken) &&
                token.Parent?.Parent.IsKind(SyntaxKind.ElementAccessExpression) == true)
            {
                // Suppression is due to issue https://github.com/dotnet/roslyn/issues/41107
                found = token.Parent.Parent!;
                return true;
            }

            found = null;
            return false;
        }

        protected override bool ShouldCheckPreviousToken(SyntaxToken token)
            => !token.Parent.IsKind(SyntaxKind.XmlCrefAttribute);

        protected override NullableFlowState GetNullabilityAnalysis(Workspace workspace, SemanticModel semanticModel, ISymbol symbol, SyntaxNode node, CancellationToken cancellationToken)
        {
            // Anything less than C# 8 we just won't show anything, even if the compiler could theoretically give analysis
            var parseOptions = (CSharpParseOptions)semanticModel.SyntaxTree!.Options;
            if (parseOptions.LanguageVersion < LanguageVersion.CSharp8)
            {
                return NullableFlowState.None;
            }

            // If the user doesn't have nullable enabled, don't show anything. For now we're not trying to be more precise if the user has just annotations or just
            // warnings. If the user has annotations off then things that are oblivious might become non-null (which is a lie) and if the user has warnings off then
            // that probably implies they're not actually trying to know if their code is correct. We can revisit this if we have specific user scenarios.
            var nullableContext = semanticModel.GetNullableContext(node.SpanStart);
            if (!nullableContext.WarningsEnabled() || !nullableContext.AnnotationsEnabled())
            {
                return NullableFlowState.None;
            }

            // Although GetTypeInfo can return nullability for uses of all sorts of things, it's not always useful for quick info.
            // For example, if you have a call to a method with a nullable return, the fact it can be null is already captured
            // in the return type shown -- there's no flow analysis information there.
            switch (symbol)
            {
                // Ignore constant values for nullability flow state
                case IFieldSymbol { HasConstantValue: true }: return default;
                case ILocalSymbol { HasConstantValue: true }: return default;

                // Symbols with useful quick info
                case IFieldSymbol _:
                case ILocalSymbol _:
                case IParameterSymbol _:
                case IPropertySymbol _:
                case IRangeVariableSymbol _:
                    break;

                default:
                    return default;
            }

            var typeInfo = semanticModel.GetTypeInfo(node, cancellationToken);

            // Nullability is a reference type only feature, value types can use
            // something like "int?"  to be nullable but that ends up encasing as
            // Nullable<int>, which isn't exactly the same. To avoid confusion and
            // extra noise, we won't show nullable flow state for value types
            if (typeInfo.Type?.IsValueType == true)
            {
                return default;
            }

            return typeInfo.Nullability.FlowState;
        }
    }
}
