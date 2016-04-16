// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly IDictionary<DirectiveTriviaSyntax, DirectiveTriviaSyntax> _directiveMap;
            private readonly IDictionary<DirectiveTriviaSyntax, IReadOnlyList<DirectiveTriviaSyntax>> _conditionalMap;
            private readonly CancellationToken _cancellationToken;

            private readonly Stack<DirectiveTriviaSyntax> _regionStack = new Stack<DirectiveTriviaSyntax>();
            private readonly Stack<DirectiveTriviaSyntax> _ifStack = new Stack<DirectiveTriviaSyntax>();

            public DirectiveWalker(
                IDictionary<DirectiveTriviaSyntax, DirectiveTriviaSyntax> directiveMap,
                IDictionary<DirectiveTriviaSyntax, IReadOnlyList<DirectiveTriviaSyntax>> conditionalMap,
                CancellationToken cancellationToken) :
                base(SyntaxWalkerDepth.Token)
            {
                _directiveMap = directiveMap;
                _conditionalMap = conditionalMap;
                _cancellationToken = cancellationToken;
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                _cancellationToken.ThrowIfCancellationRequested();

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
                    switch (directive.Kind())
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
                _ifStack.Push(directive);
            }

            private void HandleRegionDirective(DirectiveTriviaSyntax directive)
            {
                _regionStack.Push(directive);
            }

            private void HandleElifDirective(DirectiveTriviaSyntax directive)
            {
                _ifStack.Push(directive);
            }

            private void HandleElseDirective(DirectiveTriviaSyntax directive)
            {
                _ifStack.Push(directive);
            }

            private void HandleEndIfDirective(DirectiveTriviaSyntax directive)
            {
                if (_ifStack.IsEmpty())
                {
                    return;
                }

                FinishIf(directive);
            }

            private void FinishIf(DirectiveTriviaSyntax directiveOpt)
            {
                var condDirectives = new List<DirectiveTriviaSyntax>();
                if (directiveOpt != null)
                {
                    condDirectives.Add(directiveOpt);
                }

                while (!_ifStack.IsEmpty())
                {
                    var poppedDirective = _ifStack.Pop();
                    condDirectives.Add(poppedDirective);
                    if (poppedDirective.Kind() == SyntaxKind.IfDirectiveTrivia)
                    {
                        break;
                    }
                }

                condDirectives.Sort((n1, n2) => n1.SpanStart.CompareTo(n2.SpanStart));

                foreach (var cond in condDirectives)
                {
                    _conditionalMap.Add(cond, condDirectives);
                }

                // #If should be the first one in sorted order
                var ifDirective = condDirectives.First();
                Contract.Assert(
                    ifDirective.Kind() == SyntaxKind.IfDirectiveTrivia ||
                    ifDirective.Kind() == SyntaxKind.ElifDirectiveTrivia ||
                    ifDirective.Kind() == SyntaxKind.ElseDirectiveTrivia);

                if (directiveOpt != null)
                {
                    _directiveMap.Add(directiveOpt, ifDirective);
                    _directiveMap.Add(ifDirective, directiveOpt);
                }
            }

            private void HandleEndRegionDirective(DirectiveTriviaSyntax directive)
            {
                if (_regionStack.IsEmpty())
                {
                    return;
                }

                var previousDirective = _regionStack.Pop();

                _directiveMap.Add(directive, previousDirective);
                _directiveMap.Add(previousDirective, directive);
            }

            internal void Finish()
            {
                while (_regionStack.Count > 0)
                {
                    _directiveMap.Add(_regionStack.Pop(), null);
                }

                while (_ifStack.Count > 0)
                {
                    FinishIf(directiveOpt: null);
                }
            }
        }
    }
}
