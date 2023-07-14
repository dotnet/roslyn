// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertProgram
{
    using static ConvertProgramTransform;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertToTopLevelStatements), Shared]
    internal class ConvertToTopLevelStatementsCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertToTopLevelStatementsCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseTopLevelStatementsId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var options = await document.GetCSharpCodeFixOptionsProviderAsync(context.Options, cancellationToken).ConfigureAwait(false);
            var priority = options.PreferTopLevelStatements.Notification.Severity == ReportDiagnostic.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Default;

            RegisterCodeFix(context, CSharpAnalyzersResources.Convert_to_top_level_statements, nameof(ConvertToTopLevelStatementsCodeFixProvider), priority);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var methodDeclaration = (MethodDeclarationSyntax)diagnostics[0].AdditionalLocations[0].FindNode(cancellationToken);

            var newDocument = await ConvertToTopLevelStatementsAsync(document, methodDeclaration, fallbackOptions, cancellationToken).ConfigureAwait(false);
            var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(editor.OriginalRoot, newRoot);
        }
    }
}
