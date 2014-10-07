// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class DirectiveSyntaxExtensions
    {
        private class DirectiveWalker : CSharpSyntaxWalker
        {
            private readonly IDictionary<DirectiveTriviaSyntax, DirectiveTriviaSyntax> directiveMap;
            private readonly IDictionary<DirectiveTriviaSyntax, IEnumerable<DirectiveTriviaSyntax>> conditionalMap;
            private readonly CancellationToken cancellationToken;

            private readonly Stack<DirectiveTriviaSyntax> regionStack = new Stack<DirectiveTriviaSyntax>();
            private readonly Stack<DirectiveTriviaSyntax> ifStack = new Stack<DirectiveTriviaSyntax>();

            public DirectiveWalker(
                IDictionary<DirectiveTriviaSyntax, DirectiveTriviaSyntax> directiveMap,
                IDictionary<DirectiveTriviaSyntax, IEnumerable<DirectiveTriviaSyntax>> conditionalMap,
                CancellationToken cancellationToken) :
                base(SyntaxWalkerDepth.Token)
            {
                this.directiveMap = directiveMap;
                this.conditionalMap = conditionalMap;
                this.cancellationToken = cancellationToken;
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!node.ContainsDirectives)
                {
                    return;
                }

                base.DefaultVisit(node);
            }

            public override void VisitToken(SyntaxToken token)
            {
                if (!token.ContainsDirectives)
                {
                    return;
                }

                foreach (var directive in token.LeadingTrivia)
                {
                    switch (directive.CSharpKind())
                    {
                        case SyntaxKind.RegionDirectiveTrivia:
                            HandleRegionDirective((DirectiveTriviaSyntax)directive.GetStructure());
                            break;
                        case SyntaxKind.IfDirectiveTrivia:
                            HandleIfDirective((DirectiveTriviaSyntax)directive.GetStructure());
                            break;
                        case SyntaxKind.EndRegionDirectiveTrivia:
                            HandleEndRegionDirective((DirectiveTriviaSyntax)directive.GetStructure());
                            break;
                        case SyntaxKind.EndIfDirectiveTrivia:
                            HandleEndIfDirective((DirectiveTriviaSyntax)directive.GetStructure());
                            break;
                        case SyntaxKind.ElifDirectiveTrivia:
                            HandleElifDirective((DirectiveTriviaSyntax)directive.GetStructure());
                            break;
                        case SyntaxKind.ElseDirectiveTrivia:
                            HandleElseDirective((DirectiveTriviaSyntax)directive.GetStructure());
                            break;
                    }
                }
            }

            private void HandleIfDirective(DirectiveTriviaSyntax directive)
            {
                ifStack.Push(directive);
            }

            private void HandleRegionDirective(DirectiveTriviaSyntax directive)
            {
                regionStack.Push(directive);
            }

            private void HandleElifDirective(DirectiveTriviaSyntax directive)
            {
                ifStack.Push(directive);
            }

            private void HandleElseDirective(DirectiveTriviaSyntax directive)
            {
                ifStack.Push(directive);
            }

            private void HandleEndIfDirective(DirectiveTriviaSyntax directive)
            {
                if (ifStack.IsEmpty())
                {
                    return;
                }

                var condDirectives = new List<DirectiveTriviaSyntax>();
                condDirectives.Add(directive);

                while (!ifStack.IsEmpty())
                {
                    var poppedDirective = ifStack.Pop();
                    condDirectives.Add(poppedDirective);
                    if (poppedDirective.CSharpKind() == SyntaxKind.IfDirectiveTrivia)
                    {
                        break;
                    }
                }

                condDirectives.Sort((n1, n2) => n1.SpanStart.CompareTo(n2.SpanStart));

                foreach (var cond in condDirectives)
                {
                    conditionalMap.Add(cond, condDirectives);
                }

                // #If should be the first one in sorted order
                var ifDirective = condDirectives.First();
                Contract.Assert(
                    ifDirective.CSharpKind() == SyntaxKind.IfDirectiveTrivia ||
                    ifDirective.CSharpKind() == SyntaxKind.ElifDirectiveTrivia ||
                    ifDirective.CSharpKind() == SyntaxKind.ElseDirectiveTrivia);

                directiveMap.Add(directive, ifDirective);
                directiveMap.Add(ifDirective, directive);
            }

            private void HandleEndRegionDirective(DirectiveTriviaSyntax directive)
            {
                if (regionStack.IsEmpty())
                {
                    return;
                }

                var previousDirective = regionStack.Pop();

                directiveMap.Add(directive, previousDirective);
                directiveMap.Add(previousDirective, directive);
            }
        }
    }
}