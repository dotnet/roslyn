// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Helpers.RemoveUnnecessaryImports;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;

internal partial class CSharpRemoveUnnecessaryImportsService
{
    private class Rewriter : CSharpSyntaxRewriter
    {
        private readonly ISet<UsingDirectiveSyntax> _unnecessaryUsingsDoNotAccessDirectly;
        private readonly CancellationToken _cancellationToken;

        public Rewriter(
            ISet<UsingDirectiveSyntax> unnecessaryUsings,
            CancellationToken cancellationToken)
            : base(visitIntoStructuredTrivia: true)
        {
            _unnecessaryUsingsDoNotAccessDirectly = unnecessaryUsings;
            _cancellationToken = cancellationToken;
        }

        public override SyntaxNode DefaultVisit(SyntaxNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return base.DefaultVisit(node);
        }

        private static void ProcessUsings(
            SyntaxList<UsingDirectiveSyntax> usings,
            ISet<UsingDirectiveSyntax> usingsToRemove,
            out SyntaxList<UsingDirectiveSyntax> finalUsings,
            out SyntaxTriviaList finalTrivia)
        {
            var currentUsings = new List<UsingDirectiveSyntax>(usings);
            var firstUsingNotBeingRemoved = true;
            var passedLeadngTrivia = false;

            finalTrivia = default;
            for (var i = 0; i < usings.Count; i++)
            {
                if (usingsToRemove.Contains(usings[i]))
                {
                    var currentUsing = currentUsings[i];
                    currentUsings[i] = null;

                    var leadingTrivia = currentUsing.GetLeadingTrivia();
                    // We always preserve trivia on the first using in a file scoped namespace
                    if (ShouldPreserveTrivia(leadingTrivia) ||
                        (i == 0 && currentUsing.IsParentKind(SyntaxKind.FileScopedNamespaceDeclaration)))
                    {
                        // This using had trivia we want to preserve.  If we're the last
                        // directive, then copy this trivia out so that our caller can place
                        // it on the next token.  If there is any directive following us,
                        // then place it on that.
                        if (i < usings.Count - 1)
                        {
                            var nextIndex = i + 1;
                            var nextUsing = currentUsings[nextIndex];

                            if (ShouldPreserveTrivia(nextUsing.GetLeadingTrivia()))
                            {
                                // If we need to preserve the next trivia too then, prepend
                                // the two together.
                                currentUsings[nextIndex] = nextUsing.WithPrependedLeadingTrivia(leadingTrivia);
                            }
                            else
                            {
                                // Otherwise, replace the next trivia with this trivia that we
                                // want to preserve.
                                currentUsings[nextIndex] = nextUsing.WithLeadingTrivia(leadingTrivia);
                            }

                            passedLeadngTrivia = true;
                        }
                        else
                        {
                            finalTrivia = leadingTrivia;
                        }
                    }
                }
                else if (firstUsingNotBeingRemoved)
                {
                    // 1) We only apply this logic for not first using, that is saved:
                    // ===================
                    // namespace N;
                    //
                    // using System; <- if we save this using, we don't need to cut leading lines
                    // ===================
                    // 2) If leading trivia was saved from the previous using, that was removed,
                    // we don't bother cutting blank lines as well:
                    // ===================
                    // namespace N;
                    //
                    // using System; <- need to delete this using
                    // using System.Collections.Generic; <- this using is saved, no need to eat the line,
                    // otherwise https://github.com/dotnet/roslyn/issues/58972 will happen
                    if (i > 0 && !passedLeadngTrivia)
                    {
                        var currentUsing = currentUsings[i];
                        var currentUsingLeadingTrivia = currentUsing.GetLeadingTrivia();
                        currentUsings[i] = currentUsing.WithLeadingTrivia(currentUsingLeadingTrivia.WithoutLeadingBlankLines());
                    }

                    firstUsingNotBeingRemoved = false;
                }
            }

            finalUsings = [.. currentUsings.WhereNotNull()];
        }

        private static bool ShouldPreserveTrivia(SyntaxTriviaList trivia)
            => trivia.Any(t => !t.IsWhitespaceOrEndOfLine());

        private ISet<UsingDirectiveSyntax> GetUsingsToRemove(
            SyntaxList<UsingDirectiveSyntax> oldUsings,
            SyntaxList<UsingDirectiveSyntax> newUsings)
        {
            Debug.Assert(oldUsings.Count == newUsings.Count);

            var result = new HashSet<UsingDirectiveSyntax>();
            for (var i = 0; i < oldUsings.Count; i++)
            {
                if (_unnecessaryUsingsDoNotAccessDirectly.Contains(oldUsings[i]))
                {
                    result.Add(newUsings[i]);
                }
            }

            return result;
        }

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var compilationUnit = (CompilationUnitSyntax)base.VisitCompilationUnit(node);

            var usingsToRemove = GetUsingsToRemove(node.Usings, compilationUnit.Usings);
            if (usingsToRemove.Count == 0)
            {
                return compilationUnit;
            }

            ProcessUsings(compilationUnit.Usings, usingsToRemove, out var finalUsings, out var finalTrivia);

            // If all the using directives were removed, and the group was followed by a blank line, remove a single
            // blank line as well.
            if (compilationUnit.Usings.Count > 0 && finalUsings.Count == 0)
            {
                var nextToken = compilationUnit.Usings.Last().GetLastToken().GetNextTokenOrEndOfFile();
                if (nextToken.HasLeadingTrivia && nextToken.LeadingTrivia[0].IsEndOfLine())
                {
                    compilationUnit = compilationUnit.ReplaceToken(
                        nextToken,
                        nextToken.WithLeadingTrivia(nextToken.LeadingTrivia.RemoveAt(0)));
                }
            }

            // If there was any left over trivia, then attach it to the next token that
            // follows the usings.
            if (finalTrivia.Count > 0)
            {
                var nextToken = compilationUnit.Usings.Last().GetLastToken().GetNextTokenOrEndOfFile();
                compilationUnit = compilationUnit.ReplaceToken(nextToken, nextToken.WithPrependedLeadingTrivia(finalTrivia));
            }

            var resultCompilationUnit = compilationUnit.WithUsings(finalUsings);
            if (finalUsings.Count == 0 &&
                resultCompilationUnit.Externs.Count == 0 &&
                resultCompilationUnit.Members.Count >= 1)
            {
                // We've removed all the usings and now the first thing in the namespace is a
                // type.  In this case, remove any newlines preceding the type.
                var firstToken = resultCompilationUnit.GetFirstToken();
                var newFirstToken = RemoveUnnecessaryImportsHelpers.StripNewLines(CSharpSyntaxFacts.Instance, firstToken);
                resultCompilationUnit = resultCompilationUnit.ReplaceToken(firstToken, newFirstToken);
            }

            return resultCompilationUnit;
        }

        public override SyntaxNode VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            => VisitBaseNamespaceDeclaration(node, (BaseNamespaceDeclarationSyntax)base.VisitFileScopedNamespaceDeclaration(node));

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            => VisitBaseNamespaceDeclaration(node, (BaseNamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node));

        private SyntaxNode VisitBaseNamespaceDeclaration(
            BaseNamespaceDeclarationSyntax node,
            BaseNamespaceDeclarationSyntax namespaceDeclaration)
        {
            var usingsToRemove = GetUsingsToRemove(node.Usings, namespaceDeclaration.Usings);
            if (usingsToRemove.Count == 0)
                return namespaceDeclaration;

            ProcessUsings(namespaceDeclaration.Usings, usingsToRemove, out var finalUsings, out var finalTrivia);

            // If all the using directives were removed, and the group was followed by a blank line, remove a single
            // blank line as well.
            if (namespaceDeclaration.Usings.Count > 0 && finalUsings.Count == 0)
            {
                var nextToken = namespaceDeclaration.Usings.Last().GetLastToken().GetNextTokenOrEndOfFile();
                if (nextToken.HasLeadingTrivia && nextToken.LeadingTrivia[0].IsEndOfLine())
                {
                    namespaceDeclaration = namespaceDeclaration.ReplaceToken(
                        nextToken,
                        nextToken.WithLeadingTrivia(nextToken.LeadingTrivia.RemoveAt(0)));
                }
            }

            // If there was any left over trivia, then attach it to the next token that
            // follows the usings.
            if (finalTrivia.Count > 0)
            {
                var nextToken = namespaceDeclaration.Usings.Last().GetLastToken().GetNextToken();
                namespaceDeclaration = namespaceDeclaration.ReplaceToken(nextToken, nextToken.WithPrependedLeadingTrivia(finalTrivia));
            }

            var resultNamespace = namespaceDeclaration.WithUsings(finalUsings);
            if (finalUsings.Count == 0 &&
                resultNamespace.Externs.Count == 0 &&
                resultNamespace.Members.Count >= 1)
            {
                // We've removed all the usings and now the first thing in the namespace is a
                // type.  In this case, remove any newlines preceding the type.
                var firstToken = resultNamespace.Members.First().GetFirstToken();
                var newFirstToken = RemoveUnnecessaryImportsHelpers.StripNewLines(CSharpSyntaxFacts.Instance, firstToken);
                resultNamespace = resultNamespace.ReplaceToken(firstToken, newFirstToken);
            }

            return resultNamespace;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // Avoid recursing into a class declaration
            return node;
        }

        public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            // Avoid recursing into a delegate declaration
            return node;
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            // Avoid recursing into an enum declaration
            return node;
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            // Avoid recursing into an interface declaration
            return node;
        }

        public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            // Avoid recursing into a record declaration
            return node;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            // Avoid recursing into a struct declaration
            return node;
        }
    }
}
