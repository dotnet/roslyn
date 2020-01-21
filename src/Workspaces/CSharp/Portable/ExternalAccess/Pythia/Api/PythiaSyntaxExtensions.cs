// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static partial class PythiaSyntaxExtensions
    {
        public static bool IsInNonUserCode(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            => CSharp.Extensions.SyntaxTreeExtensions.IsInNonUserCode(syntaxTree, position, cancellationToken);

        public static SyntaxToken GetPreviousTokenIfTouchingWord(this SyntaxToken token, int position)
            => CSharp.Extensions.SyntaxTokenExtensions.GetPreviousTokenIfTouchingWord(token, position);

        public static SyntaxToken FindTokenOnLeftOfPosition(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, bool includeSkipped = true, bool includeDirectives = false, bool includeDocumentationComments = false)
            => Shared.Extensions.SyntaxTreeExtensions.FindTokenOnLeftOfPosition(syntaxTree, position, cancellationToken, includeSkipped, includeDirectives, includeDocumentationComments);

        public static bool IsFoundUnder<TParent>(this SyntaxNode node, Func<TParent, SyntaxNode> childGetter) where TParent : SyntaxNode
            => Shared.Extensions.SyntaxNodeExtensions.IsFoundUnder(node, childGetter);

        public static SimpleNameSyntax GetRightmostName(this ExpressionSyntax node)
            => CSharp.Extensions.ExpressionSyntaxExtensions.GetRightmostName(node);

        public static bool IsInStaticContext(this SyntaxNode node)
            => CSharp.Extensions.SyntaxNodeExtensions.IsInStaticContext(node);
    }
}
