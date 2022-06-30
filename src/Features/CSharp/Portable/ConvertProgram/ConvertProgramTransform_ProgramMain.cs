﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram
{
    using static SyntaxFactory;

    internal static partial class ConvertProgramTransform
    {
        public static async Task<Document> ConvertToProgramMainAsync(Document document, AccessibilityModifiersRequired accessibilityModifiersRequired, CancellationToken cancellationToken)
        {
            // While the analyze and refactoring check ensure we're in a well formed state for their needs, the 'new
            // template' code just calls directly into this if the user prefers Program.Main.  So check and make sure
            // this is actually something we can convert before proceeding.
            var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root.IsTopLevelProgram())
            {
                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                var programType = compilation.GetBestTypeByMetadataName(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
                if (programType != null)
                {
                    if (programType.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName).FirstOrDefault() is IMethodSymbol mainMethod)
                    {
                        var classDeclaration = await GenerateProgramClassAsync(
                            document, programType, mainMethod, accessibilityModifiersRequired, cancellationToken).ConfigureAwait(false);

                        var newRoot = root.RemoveNodes(root.Members.OfType<GlobalStatementSyntax>().Skip(1), SyntaxGenerator.DefaultRemoveOptions);
                        Contract.ThrowIfNull(newRoot);

                        var firstGlobalStatement = newRoot.Members.OfType<GlobalStatementSyntax>().Single();
                        newRoot = newRoot.ReplaceNode(firstGlobalStatement, classDeclaration);

                        return document.WithSyntaxRoot(newRoot);
                    }
                }
            }

            return document;
        }

        private static async Task<SyntaxNode> GenerateProgramClassAsync(
            Document document,
            INamedTypeSymbol programType,
            IMethodSymbol mainMethod,
            AccessibilityModifiersRequired accessibilityModifiersRequired,
            CancellationToken cancellationToken)
        {
            // Respect user settings on if they want explicit or implicit accessibility modifiers.
            var useDeclaredAccessibity = accessibilityModifiersRequired is AccessibilityModifiersRequired.ForNonInterfaceMembers or AccessibilityModifiersRequired.Always;

            var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

            // See if we have an existing part in another file.  If so, we'll have to generate our declaration as partial.
            var hasExistingPart = programType.DeclaringSyntaxReferences.Any(static (d, cancellationToken) => d.GetSyntax(cancellationToken) is TypeDeclarationSyntax, cancellationToken);

            var method = (MethodDeclarationSyntax)generator.MethodDeclaration(
                mainMethod, WellKnownMemberNames.EntryPointMethodName,
                GenerateProgramMainStatements(root, out var leadingTrivia));
            method = method.WithReturnType(method.ReturnType.WithAdditionalAnnotations(Simplifier.AddImportsAnnotation));
            method = (MethodDeclarationSyntax)generator.WithAccessibility(
                method, useDeclaredAccessibity ? mainMethod.DeclaredAccessibility : Accessibility.NotApplicable);

            // Workaround for simplification not being ready when we generate a new file.  Substitute System.String[]
            // with string[].
            if (method.ParameterList.Parameters.Count == 1 && method.ParameterList.Parameters[0].Type is ArrayTypeSyntax arrayType)
                method = method.ReplaceNode(arrayType.ElementType, PredefinedType(Token(SyntaxKind.StringKeyword)));

            return FixupComments(generator.ClassDeclaration(
                WellKnownMemberNames.TopLevelStatementsEntryPointTypeName,
                accessibility: useDeclaredAccessibity ? programType.DeclaredAccessibility : Accessibility.NotApplicable,
                modifiers: hasExistingPart ? DeclarationModifiers.Partial : DeclarationModifiers.None,
                members: new[] { method }).WithLeadingTrivia(leadingTrivia));
        }

        private static ImmutableArray<StatementSyntax> GenerateProgramMainStatements(
            CompilationUnitSyntax root, out SyntaxTriviaList triviaToMove)
        {
            using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var statements);

            triviaToMove = default;
            var first = true;
            foreach (var globalStatement in root.Members.OfType<GlobalStatementSyntax>())
            {
                // Remove leading trivia from first statement.  We'll move it to the Program type. Any directly attached
                // comments though stay attached to the first statement.
                var statement = globalStatement.Statement.WithAdditionalAnnotations(Formatter.Annotation);
                if (first)
                {
                    first = false;

                    triviaToMove = statement.GetLeadingTrivia();
                    while (triviaToMove is [.., { RawKind: (int)SyntaxKind.SingleLineCommentTrivia }, { RawKind: (int)SyntaxKind.EndOfLineTrivia }])
                        triviaToMove = TriviaList(triviaToMove.Take(triviaToMove.Count - 2));

                    var commentsToPreserve = TriviaList(statement.GetLeadingTrivia().Skip(triviaToMove.Count));
                    statements.Add(FixupComments(statement.WithLeadingTrivia(commentsToPreserve)));
                }
                else
                {
                    statements.Add(statement);
                }
            }

            return statements.ToImmutable();
        }

        private static TSyntaxNode FixupComments<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode
        {
            // Remove comment explaining top level statements as it isn't relevant if the user switches back to full
            // Program.Main form.
            var leadingTrivia = node.GetLeadingTrivia();
            var comment = leadingTrivia.FirstOrNull(
                c => c.Kind() is SyntaxKind.SingleLineCommentTrivia && c.ToString().Contains("https://aka.ms/new-console-template"));
            if (comment == null)
                return node;

            var commentIndex = leadingTrivia.IndexOf(comment.Value);
            leadingTrivia = leadingTrivia.RemoveAt(commentIndex);

            while (commentIndex < leadingTrivia.Count && leadingTrivia[commentIndex].Kind() is SyntaxKind.EndOfLineTrivia)
                leadingTrivia = leadingTrivia.RemoveAt(commentIndex);

            return node.WithLeadingTrivia(leadingTrivia);
        }
    }
}
