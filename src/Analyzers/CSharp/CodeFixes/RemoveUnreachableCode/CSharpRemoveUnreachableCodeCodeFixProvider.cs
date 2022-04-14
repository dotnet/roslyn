// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnreachableCode), Shared]
    internal class CSharpRemoveUnreachableCodeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpRemoveUnreachableCodeCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];

            // Only the first reported unreacha ble line will have a squiggle.  On that line, make the
            // code action normal priority as the user is likely bringing up the lightbulb to fix the
            // squiggle.  On all the other lines make the code action low priority as it's definitely
            // helpful, but shouldn't interfere with anything else the uesr is doing.
            var priority = IsSubsequentSection(diagnostic)
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;

            RegisterCodeFix(context, CSharpCodeFixesResources.Remove_unreachable_code, nameof(CSharpCodeFixesResources.Remove_unreachable_code), priority);

            return Task.CompletedTask;
        }

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !IsSubsequentSection(diagnostic);

        private static bool IsSubsequentSection(Diagnostic diagnostic)
            => diagnostic.Properties.ContainsKey(CSharpRemoveUnreachableCodeDiagnosticAnalyzer.IsSubsequentSection);

        protected override Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var firstUnreachableStatementLocation = diagnostic.AdditionalLocations.First();
                var firstUnreachableStatement = (StatementSyntax)firstUnreachableStatementLocation.FindNode(cancellationToken);

                RemoveStatement(editor, firstUnreachableStatement);

                var sections = RemoveUnreachableCodeHelpers.GetSubsequentUnreachableSections(firstUnreachableStatement);
                foreach (var section in sections)
                {
                    foreach (var statement in section)
                    {
                        RemoveStatement(editor, statement);
                    }
                }
            }

            return Task.CompletedTask;

            // Local function
            static void RemoveStatement(SyntaxEditor editor, SyntaxNode statement)
            {
                if (!statement.IsParentKind(SyntaxKind.Block)
                    && !statement.IsParentKind(SyntaxKind.SwitchSection))
                {
                    editor.ReplaceNode(statement, SyntaxFactory.Block());
                }
                else
                {
                    editor.RemoveNode(statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                }
            }
        }
    }
}
