// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class DirectiveSyntaxExtensions
    {
        private static readonly ConditionalWeakTable<SyntaxNode, DirectiveInfo> s_rootToDirectiveInfo = new();
        private static readonly ObjectPool<Stack<DirectiveTriviaSyntax>> s_stackPool = new(() => new());

        private static SyntaxNode GetAbsoluteRoot(this SyntaxNode node)
        {
            while (node.Parent != null || node is StructuredTriviaSyntax)
            {
                if (node.Parent != null)
                {
                    node = node.GetRequiredParent();
                }
                else
                {
                    node = node.ParentTrivia.Token.GetRequiredParent();
                }
            }

            return node;
        }

        private static DirectiveInfo GetDirectiveInfo(SyntaxNode node, CancellationToken cancellationToken)
            => s_rootToDirectiveInfo.GetValue(
                node.GetAbsoluteRoot(),
                root => GetDirectiveInfoForRoot(root, cancellationToken));

        private static DirectiveInfo GetDirectiveInfoForRoot(SyntaxNode root, CancellationToken cancellationToken)
        {
            var directiveMap = new Dictionary<DirectiveTriviaSyntax, DirectiveTriviaSyntax?>(
                DirectiveSyntaxEqualityComparer.Instance);
            var conditionalMap = new Dictionary<DirectiveTriviaSyntax, IReadOnlyList<DirectiveTriviaSyntax>>(
                DirectiveSyntaxEqualityComparer.Instance);

            using var pooledRegionStack = s_stackPool.GetPooledObject();
            using var pooledIfStack = s_stackPool.GetPooledObject();

            var regionStack = pooledRegionStack.Object;
            var ifStack = pooledIfStack.Object;

            foreach (var token in root.DescendantTokens(descendIntoChildren: static node => node.ContainsDirectives))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!token.ContainsDirectives)
                    continue;

                foreach (var directive in token.LeadingTrivia)
                {
                    switch (directive.Kind())
                    {
                        case SyntaxKind.RegionDirectiveTrivia:
                            HandleRegionDirective((DirectiveTriviaSyntax)directive.GetStructure()!);
                            break;
                        case SyntaxKind.IfDirectiveTrivia:
                            HandleIfDirective((DirectiveTriviaSyntax)directive.GetStructure()!);
                            break;
                        case SyntaxKind.EndRegionDirectiveTrivia:
                            HandleEndRegionDirective((DirectiveTriviaSyntax)directive.GetStructure()!);
                            break;
                        case SyntaxKind.EndIfDirectiveTrivia:
                            HandleEndIfDirective((DirectiveTriviaSyntax)directive.GetStructure()!);
                            break;
                        case SyntaxKind.ElifDirectiveTrivia:
                            HandleElifDirective((DirectiveTriviaSyntax)directive.GetStructure()!);
                            break;
                        case SyntaxKind.ElseDirectiveTrivia:
                            HandleElseDirective((DirectiveTriviaSyntax)directive.GetStructure()!);
                            break;
                    }
                }
            }

            while (regionStack.Count > 0)
                directiveMap.Add(regionStack.Pop(), null);

            while (ifStack.Count > 0)
                FinishIf(directive: null);

            return new DirectiveInfo(directiveMap, conditionalMap, inactiveRegionLines: null);

            void HandleIfDirective(DirectiveTriviaSyntax directive)
                => ifStack.Push(directive);

            void HandleRegionDirective(DirectiveTriviaSyntax directive)
                => regionStack.Push(directive);

            void HandleElifDirective(DirectiveTriviaSyntax directive)
                => ifStack.Push(directive);

            void HandleElseDirective(DirectiveTriviaSyntax directive)
                => ifStack.Push(directive);

            void HandleEndIfDirective(DirectiveTriviaSyntax directive)
            {
                if (ifStack.Count == 0)
                    return;

                FinishIf(directive);
            }

            void FinishIf(DirectiveTriviaSyntax? directive)
            {
                var condDirectives = new List<DirectiveTriviaSyntax>();
                if (directive != null)
                    condDirectives.Add(directive);

                while (ifStack.Count > 0)
                {
                    var poppedDirective = ifStack.Pop();
                    condDirectives.Add(poppedDirective);
                    if (poppedDirective.Kind() == SyntaxKind.IfDirectiveTrivia)
                        break;
                }

                condDirectives.Sort(static (n1, n2) => n1.SpanStart.CompareTo(n2.SpanStart));

                foreach (var cond in condDirectives)
                    conditionalMap.Add(cond, condDirectives);

                // #If should be the first one in sorted order
                var ifDirective = condDirectives.First();
                Debug.Assert(
                    ifDirective.Kind() is SyntaxKind.IfDirectiveTrivia or
                    SyntaxKind.ElifDirectiveTrivia or
                    SyntaxKind.ElseDirectiveTrivia);

                if (directive != null)
                {
                    directiveMap.Add(directive, ifDirective);
                    directiveMap.Add(ifDirective, directive);
                }
            }

            void HandleEndRegionDirective(DirectiveTriviaSyntax directive)
            {
                if (regionStack.Count == 0)
                    return;

                var previousDirective = regionStack.Pop();

                directiveMap.Add(directive, previousDirective);
                directiveMap.Add(previousDirective, directive);
            }
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
