// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram
{
    internal static class ConvertProgramHelpers
    {
        public static async Task<Document> ConvertToProgramMainAsync(Document document, CancellationToken cancellationToken)
        {
            var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var members = root.Members;
            if (members.Any(m => m is GlobalStatementSyntax))
            {
                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                var programType = compilation.GetBestTypeByMetadataName(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
                if (programType != null)
                {
                    if (programType.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName).FirstOrDefault() is IMethodSymbol mainMethod)
                    {
                        var classDeclaration = await GenerateProgramClassAsync(
                            document, programType, mainMethod, cancellationToken).ConfigureAwait(false);

                        var newRoot = root.RemoveNodes(members.OfType<GlobalStatementSyntax>().Skip(1), SyntaxGenerator.DefaultRemoveOptions);
                        Contract.ThrowIfNull(newRoot);

                        var firstGlobalStatement = newRoot.Members.OfType<GlobalStatementSyntax>().Single();
                        newRoot = newRoot.ReplaceNode(
                            firstGlobalStatement,
                            FixupComments(classDeclaration.WithLeadingTrivia(firstGlobalStatement.GetLeadingTrivia())));

                        return document.WithSyntaxRoot(newRoot);
                    }
                }
            }

            return document;
        }

        private static async Task<ClassDeclarationSyntax> GenerateProgramClassAsync(
            Document document,
            INamedTypeSymbol programType,
            IMethodSymbol mainMethod,
            CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var option = options.GetOption(CodeStyleOptions2.RequireAccessibilityModifiers);
            var accessibilityModifiersRequired = option.Value is AccessibilityModifiersRequired.ForNonInterfaceMembers or AccessibilityModifiersRequired.Always;

            var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

            // See if we have an existing part in another file.  If so, we'll have to generate our declaration as partial.
            var hasExistingPart = programType.DeclaringSyntaxReferences.Any(d => d.GetSyntax(cancellationToken) is TypeDeclarationSyntax);

            var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return (ClassDeclarationSyntax)generator.ClassDeclaration(
                WellKnownMemberNames.TopLevelStatementsEntryPointTypeName,
                accessibility: accessibilityModifiersRequired ? programType.DeclaredAccessibility : Accessibility.NotApplicable,
                modifiers: hasExistingPart ? DeclarationModifiers.Partial : DeclarationModifiers.None,
                members: new[]
                {
                    (MemberDeclarationSyntax)generator.WithAccessibility(
                        generator.MethodDeclaration(mainMethod, "Main", GetStatements(root)),
                        accessibilityModifiersRequired ? mainMethod.DeclaredAccessibility : Accessibility.NotApplicable)
                });
        }

        private static ImmutableArray<StatementSyntax> GetStatements(CompilationUnitSyntax root)
        {
            using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var statements);

            var first = true;
            foreach (var globalStatement in root.Members.OfType<GlobalStatementSyntax>())
            {
                // Remove leading trivia from first statement.  We'll move it to the Program type.
                var statement = globalStatement.Statement.WithAdditionalAnnotations(Formatter.Annotation);
                if (first)
                {
                    first = false;
                    statements.Add(statement.WithoutLeadingTrivia());
                }
                else
                {
                    statements.Add(statement);
                }
            }

            return statements.ToImmutable();
        }

        private static ClassDeclarationSyntax FixupComments(ClassDeclarationSyntax declaration)
        {
            // Remove comment explaining top level statements as it isn't relevant if the user switches back to full
            // Program.Main form.
            var leadingTrivia = declaration.GetLeadingTrivia();
            var comment = leadingTrivia.FirstOrNull(
                c => c.Kind() is SyntaxKind.SingleLineCommentTrivia && c.ToString().Contains("https://aka.ms/new-console-template"));
            if (comment == null)
                return declaration;

            var commentIndex = leadingTrivia.IndexOf(comment.Value);
            leadingTrivia = leadingTrivia.RemoveAt(commentIndex);

            if (commentIndex < leadingTrivia.Count && leadingTrivia[commentIndex].Kind() is SyntaxKind.EndOfLineTrivia)
                leadingTrivia = leadingTrivia.RemoveAt(commentIndex);

            return declaration.WithLeadingTrivia(leadingTrivia);
        }
    }
}
