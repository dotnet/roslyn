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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedLocalFunction
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedLocalFunction), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal class CSharpRemoveUnusedLocalFunctionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const string CS8321 = nameof(CS8321); // The local function 'X' is declared but never used

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpRemoveUnusedLocalFunctionCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS8321);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CSharpFeaturesResources.Remove_unused_function,
                    GetDocumentUpdater(context),
                    nameof(CSharpFeaturesResources.Remove_unused_function)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

            // Order diagnostics in reverse (from latest in file to earliest) so that we process
            // all inner local functions before processing outer local functions.  If we don't
            // do this, then SyntaxEditor will fail if it tries to remove an inner local function
            // after already removing the outer one.
            var localFunctions = diagnostics.OrderBy((d1, d2) => d2.Location.SourceSpan.Start - d1.Location.SourceSpan.Start)
                                            .Select(d => root.FindToken(d.Location.SourceSpan.Start))
                                            .Select(t => t.GetAncestor<LocalFunctionStatementSyntax>());

            foreach (var localFunction in localFunctions)
            {
                editor.RemoveNode(localFunction.IsParentKind(SyntaxKind.GlobalStatement) ? localFunction.Parent : localFunction);
            }

            return Task.CompletedTask;
        }
    }
}
