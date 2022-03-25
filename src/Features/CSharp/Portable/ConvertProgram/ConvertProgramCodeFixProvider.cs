// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertToProgramMain), Shared]
    internal class ConvertToProgramMainCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertToProgramMainCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseProgramMainId);

        internal override CodeFixCategory CodeFixCategory
            => CodeFixCategory.CodeStyle;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var option = options.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);
            var priority = option.Notification.Severity == ReportDiagnostic.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;

            var diagnostic = context.Diagnostics[0];
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, diagnostic, c), priority),
                context.Diagnostics);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = (CompilationUnitSyntax)editor.OriginalRoot;
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            var programType = compilation.GetBestTypeByMetadataName(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
            Contract.ThrowIfNull(programType); // checked in analyzer.
            var mainMethod = (IMethodSymbol)programType.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName).First();

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var option = options.GetOption(CodeStyleOptions2.RequireAccessibilityModifiers);
            var accessibilityModifiersRequired = option.Value is AccessibilityModifiersRequired.ForNonInterfaceMembers or AccessibilityModifiersRequired.Always;

            // See if we have an existing part in another file.  If so, we'll have to generate our declaration as partial.
            var hasExistingPart = programType.DeclaringSyntaxReferences.Any(d => d.GetSyntax(cancellationToken) is TypeDeclarationSyntax);

            var statements = GetStatements(root);

            var generator = editor.Generator;
            var methodDeclaration = (MemberDeclarationSyntax)generator.WithAccessibility(
                generator.MethodDeclaration(mainMethod, "Main", statements),
                accessibilityModifiersRequired ? Accessibility.Private : Accessibility.NotApplicable);

            var classDeclaration = (ClassDeclarationSyntax)generator.ClassDeclaration(
                WellKnownMemberNames.TopLevelStatementsEntryPointTypeName,
                accessibility: accessibilityModifiersRequired ? Accessibility.Internal : Accessibility.NotApplicable);
            classDeclaration = classDeclaration.AddMembers(methodDeclaration);

            var newRoot = root.WithMembers(
                SyntaxFactory.List(root.Members.Insert(0, classDeclaration).Where(m => m is not GlobalStatementSyntax)));

            editor.ReplaceNode(root, newRoot);
        }

        private static ImmutableArray<StatementSyntax> GetStatements(CompilationUnitSyntax root)
        {
            using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var statements);

            foreach (var globalStatement in root.Members.OfType<GlobalStatementSyntax>())
                statements.Add(FixupComments(globalStatement.Statement));

            return statements.ToImmutable();
        }

        private static StatementSyntax FixupComments(StatementSyntax statement)
        {
            // Remove comment explaining top level statements as it isn't relevant if the user switches back to full
            // Program.Main form.
            var leadingTrivia = statement.GetLeadingTrivia();
            var comment = leadingTrivia.FirstOrNull(
                c => c.Kind() is SyntaxKind.SingleLineCommentTrivia && c.ToString().Contains("https://aka.ms/new-console-template"));
            if (comment == null)
                return statement;

            var commentIndex = leadingTrivia.IndexOf(comment.Value);
            leadingTrivia = leadingTrivia.Remove(comment.Value);

            if (commentIndex < leadingTrivia.Count && leadingTrivia[commentIndex].Kind() is SyntaxKind.EndOfLineTrivia)
                leadingTrivia.RemoveAt(commentIndex);

            return statement.WithLeadingTrivia(leadingTrivia);
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            internal override CodeActionPriority Priority { get; }

            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, CodeActionPriority priority)
                : base(CSharpAnalyzersResources.Convert_to_Program_Main_style_program, createChangedDocument, nameof(ConvertToProgramMainCodeFixProvider))
            {
                this.Priority = priority;
            }
        }
    }
}
