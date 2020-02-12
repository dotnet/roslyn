﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class DirectiveSyntaxExtensions
    {
        private static readonly ConditionalWeakTable<SyntaxNode, DirectiveInfo> s_rootToDirectiveInfo =
            new ConditionalWeakTable<SyntaxNode, DirectiveInfo>();

        private static SyntaxNode GetAbsoluteRoot(this SyntaxNode node)
        {
            while (node != null && (node.Parent != null || node is StructuredTriviaSyntax))
            {
                if (node.Parent != null)
                {
                    node = node.Parent;
                }
                else
                {
                    node = node.ParentTrivia.Token.Parent;
                }
            }

            return node;
        }

        private static DirectiveInfo GetDirectiveInfo(SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = node.GetAbsoluteRoot();
            var info = s_rootToDirectiveInfo.GetValue(root, r =>
            {
                var directiveMap = new Dictionary<DirectiveTriviaSyntax, DirectiveTriviaSyntax>(
                    DirectiveSyntaxEqualityComparer.Instance);
                var conditionalMap = new Dictionary<DirectiveTriviaSyntax, IReadOnlyList<DirectiveTriviaSyntax>>(
                    DirectiveSyntaxEqualityComparer.Instance);

                var walker = new DirectiveWalker(directiveMap, conditionalMap, cancellationToken);
                walker.Visit(r);
                walker.Finish();

                return new DirectiveInfo(directiveMap, conditionalMap, inactiveRegionLines: null);
            });

            return info;
        }

        internal static DirectiveTriviaSyntax GetMatchingDirective(this DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            var directiveSyntaxMap = GetDirectiveInfo(directive, cancellationToken).DirectiveMap;
            directiveSyntaxMap.TryGetValue(directive, out var result);

            return result;
        }

        internal static IReadOnlyList<DirectiveTriviaSyntax> GetMatchingConditionalDirectives(this DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            var directiveConditionalMap = GetDirectiveInfo(directive, cancellationToken).ConditionalMap;
            directiveConditionalMap.TryGetValue(directive, out var result);

            return result;
        }
    }
}
