// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.PassInCapturedVariables), Shared]
internal sealed class PassInCapturedVariablesAsArgumentsCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    private const string CS8421 = nameof(CS8421); // A static local function can't contain a reference to <variable>.

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public PassInCapturedVariablesAsArgumentsCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [CS8421];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();

        return WrapFixAsync(
            context.Document,
            [diagnostic],
            (document, localFunction, captures) =>
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CSharpCodeFixesResources.Pass_in_captured_variables_as_arguments,
                        cancellationToken => MakeLocalFunctionStaticCodeFixHelper.MakeLocalFunctionStaticAsync(document, localFunction, captures, context.GetOptionsProvider(), cancellationToken),
                        nameof(CSharpCodeFixesResources.Pass_in_captured_variables_as_arguments)),
                    diagnostic);

                return Task.CompletedTask;
            },
            context.CancellationToken);
    }

    protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        => WrapFixAsync(
            document,
            diagnostics,
            (document, localFunction, captures) => MakeLocalFunctionStaticCodeFixHelper.MakeLocalFunctionStaticAsync(
                document, localFunction, captures, editor, fallbackOptions, cancellationToken),
            cancellationToken);

    // The purpose of this wrapper is to share some common logic between FixOne and FixAll. The main reason we chose
    // this approach over the typical "FixOne calls FixAll" approach is to avoid duplicate code.
    private static async Task WrapFixAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        Func<Document, LocalFunctionStatementSyntax, ImmutableArray<ISymbol>, Task> fixer,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Even when the language version doesn't support static local function, the compiler will still generate
        // this error. So we need to check to make sure we don't provide incorrect fix.
        if (!MakeLocalFunctionStaticHelper.IsStaticLocalFunctionSupported(root.SyntaxTree.Options.LanguageVersion()))
            return;

        // Find all unique local functions that contain the error.
        var localFunctions = diagnostics
            .Select(d => root.FindNode(d.Location.SourceSpan).AncestorsAndSelf().OfType<LocalFunctionStatementSyntax>().FirstOrDefault())
            .WhereNotNull()
            .Distinct()
            .ToImmutableArrayOrEmpty();

        if (localFunctions.Length == 0)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var localFunction in localFunctions)
        {
            if (MakeLocalFunctionStaticHelper.CanMakeLocalFunctionStaticByRefactoringCaptures(localFunction, semanticModel, out var captures))
                await fixer(document, localFunction, captures).ConfigureAwait(false);
        }
    }
}
