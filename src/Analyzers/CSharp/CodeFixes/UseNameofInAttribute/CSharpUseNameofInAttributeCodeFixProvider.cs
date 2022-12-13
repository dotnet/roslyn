// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseNameofInAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.UseNameofInAttribute
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseNameofInAttribute), Shared]
    internal sealed class CSharpUseNameofInAttributeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpUseNameofInAttributeCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseNameofInAttributeDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(
                context,
                CSharpAnalyzersResources.Use_nameof,
                nameof(CSharpAnalyzersResources.Use_nameof));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var expression = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                var name = diagnostic.Properties[CSharpUseNameofInAttributeDiagnosticAnalyzer.NameKey];
                Contract.ThrowIfNull(name);

                editor.ReplaceNode(
                    expression,
                    editor.Generator.NameOfExpression(editor.Generator.IdentifierName(name)).WithTriviaFrom(expression));
            }

            return Task.CompletedTask;
        }
    }
}
