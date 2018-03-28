// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;

namespace Microsoft.CodeAnalysis.Editor.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(PredefinedQuickInfoProviderNames.Semantic, LanguageNames.CSharp)]
    internal class SemanticQuickInfoProvider : AbstractSemanticQuickInfoProvider
    {
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
    }
}
