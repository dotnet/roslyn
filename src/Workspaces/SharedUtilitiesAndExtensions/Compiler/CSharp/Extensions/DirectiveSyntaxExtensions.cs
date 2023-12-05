// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class DirectiveSyntaxExtensions
    {
        private static readonly ConditionalWeakTable<SyntaxNode, DirectiveInfo<DirectiveTriviaSyntax>> s_rootToDirectiveInfo = new();

        private static SyntaxNode GetAbsoluteRoot(this SyntaxNode node)
        {
            while (node.Parent != null || node is StructuredTriviaSyntax)
            {
                node = node.Parent ?? node.ParentTrivia.Token.GetRequiredParent();
            }

            return node;
        }

        private static DirectiveInfo<DirectiveTriviaSyntax> GetDirectiveInfo(SyntaxNode node, CancellationToken cancellationToken)
            => s_rootToDirectiveInfo.GetValue(
                node.GetAbsoluteRoot(),
                root => GetDirectiveInfoForRoot(root, cancellationToken));

        private static DirectiveInfo<DirectiveTriviaSyntax> GetDirectiveInfoForRoot(SyntaxNode root, CancellationToken cancellationToken)
            => CodeAnalysis.Shared.Extensions.SyntaxNodeExtensions.GetDirectiveInfoForRoot<DirectiveTriviaSyntax>(
                root, CSharpSyntaxKinds.Instance, cancellationToken);

        internal static DirectiveTriviaSyntax? GetMatchingDirective(this DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
        {
            if (directive == null)
                throw new ArgumentNullException(nameof(directive));

            var directiveSyntaxMap = GetDirectiveInfo(directive, cancellationToken).DirectiveMap;
            return directiveSyntaxMap.TryGetValue(directive, out var result)
                ? result
                : null;
        }

        public static ImmutableArray<DirectiveTriviaSyntax> GetMatchingConditionalDirectives(this DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
        {
            if (directive == null)
                throw new ArgumentNullException(nameof(directive));

            var directiveConditionalMap = GetDirectiveInfo(directive, cancellationToken).ConditionalMap;
            return directiveConditionalMap.TryGetValue(directive, out var result)
                ? result
                : ImmutableArray<DirectiveTriviaSyntax>.Empty;
        }
    }
}
