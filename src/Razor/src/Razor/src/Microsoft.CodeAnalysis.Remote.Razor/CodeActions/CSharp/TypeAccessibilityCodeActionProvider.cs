// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

using SyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

[Export(typeof(ICSharpCodeActionProvider)), Shared]
[method: ImportingConstructor]
internal sealed class TypeAccessibilityCodeActionProvider(ILoggerFactory loggerFactory) : ICSharpCodeActionProvider
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<TypeAccessibilityCodeActionProvider>();

    private static readonly IEnumerable<string> s_supportedDiagnostics = new[]
    {
        // `The type or namespace name 'type/namespace' could not be found
        //  (are you missing a using directive or an assembly reference?)`
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0246
        "CS0246",

        // `The name 'identifier' does not exist in the current context`
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103
        "CS0103",

        // `The name 'identifier' does not exist in the current context`
        "IDE1007"
    };

    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions,
        CancellationToken cancellationToken)
    {
        LogCodeActionTrace($"TypeAccessibility.Entry: SupportsResolve={context.SupportsCodeActionResolve}, Diagnostics=[{FormatDiagnostics(context.Request?.Context?.Diagnostics)}], InputCount={codeActions.Length}, Input=[{FormatCodeActions(codeActions)}]");

        if (context.Request?.Context?.Diagnostics is null)
        {
            LogCodeActionTrace("TypeAccessibility.ExitNoDiagnostics");
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (codeActions.IsEmpty)
        {
            LogCodeActionTrace("TypeAccessibility.ExitNoCodeActions");
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var results = context.SupportsCodeActionResolve
            ? ProcessCodeActionsVS(context, codeActions, _logger)
            : ProcessCodeActionsVSCode(context, codeActions, _logger);

        var orderedResults = results.Sort(static (x, y) => StringComparer.CurrentCulture.Compare(x.Title, y.Title));
        LogCodeActionTrace($"TypeAccessibility.Exit: RawCount={results.Length}, OrderedCount={orderedResults.Length}, Results=[{FormatCodeActions(orderedResults)}]");
        return Task.FromResult(orderedResults);
    }

    private static ImmutableArray<RazorVSInternalCodeAction> ProcessCodeActionsVSCode(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions,
        ILogger logger)
    {
        var diagnostics = context.Request.Context.Diagnostics.Where(diagnostic =>
            diagnostic is { Severity: LspDiagnosticSeverity.Error, Code: { } code } &&
            code.TryGetSecond(out var str) &&
            s_supportedDiagnostics.Any(d => str.Equals(d, StringComparison.OrdinalIgnoreCase)));

        if (diagnostics is null || !diagnostics.Any())
        {
            LogCodeActionTrace(logger, "TypeAccessibility.VSCode.NoSupportedDiagnostics");
            return [];
        }

        using var typeAccessibilityCodeActions = new PooledArrayBuilder<RazorVSInternalCodeAction>();

        foreach (var diagnostic in diagnostics)
        {
            // Corner case handling for diagnostics which (momentarily) linger after
            // @code block is cleared out
            var range = diagnostic.Range;
            var end = range.End;
            if (end.Line > context.SourceText.Lines.Count ||
                end.Character > context.SourceText.Lines[end.Line].End)
            {
                LogCodeActionTrace(logger, $"TypeAccessibility.VSCode.SkipDiagnosticOutOfBounds: Diagnostic={FormatDiagnostic(diagnostic)}");
                continue;
            }

            var diagnosticSpan = context.SourceText.GetTextSpan(range);

            // Based on how we compute `Range.AsTextSpan` it's possible to have a span
            // which goes beyond the end of the source text. Something likely changed
            // between the capturing of the diagnostic (by the platform) and the retrieval of the
            // document snapshot / source text. In such a case, we skip processing of the diagnostic.
            if (diagnosticSpan.End > context.SourceText.Length)
            {
                LogCodeActionTrace(logger, $"TypeAccessibility.VSCode.SkipDiagnosticSpanPastEnd: Diagnostic={FormatDiagnostic(diagnostic)}, Span='{diagnosticSpan}'");
                continue;
            }

            foreach (var codeAction in codeActions)
            {
                var name = codeAction.Name;
                if (name is null || !name.Equals(LanguageServerConstants.CodeActions.CodeActionFromVSCode, StringComparison.Ordinal))
                {
                    LogCodeActionTrace(logger, $"TypeAccessibility.VSCode.SkipName: Diagnostic={FormatDiagnostic(diagnostic)}, Action={FormatCodeAction(codeAction)}");
                    continue;
                }

                var associatedValue = context.SourceText.ToString(diagnosticSpan);

                var fqn = string.Empty;

                // When there's only one FQN suggestion, code action title is of the form:
                // `System.Net.Dns`
                if (!codeAction.Title.Any(c => char.IsWhiteSpace(c)) &&
                    codeAction.Title.EndsWith(associatedValue, StringComparison.OrdinalIgnoreCase))
                {
                    fqn = codeAction.Title;
                }
                // When there are multiple FQN suggestions, the code action title is of the form:
                // `Fully qualify 'Dns' -> System.Net.Dns`
                else
                {
                    var expectedCodeActionPrefix = $"Fully qualify '{associatedValue}' -> ";
                    if (codeAction.Title.StartsWith(expectedCodeActionPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        fqn = codeAction.Title[expectedCodeActionPrefix.Length..];
                    }
                }

                if (string.IsNullOrEmpty(fqn))
                {
                    LogCodeActionTrace(logger, $"TypeAccessibility.VSCode.SkipNoFqn: AssociatedValue='{associatedValue}', Diagnostic={FormatDiagnostic(diagnostic)}, Action={FormatCodeAction(codeAction)}");
                    continue;
                }

                var fqnCodeAction = CreateFQNCodeAction(context, diagnostic, codeAction, fqn);
                typeAccessibilityCodeActions.Add(fqnCodeAction);
                LogCodeActionTrace(logger, $"TypeAccessibility.VSCode.KeepFullyQualify: AssociatedValue='{associatedValue}', Fqn='{fqn}', Action={FormatCodeAction(fqnCodeAction)}");

                if (AddUsingsCodeActionResolver.TryCreateAddUsingResolutionParams(fqn, context.Request.TextDocument, additionalEdit: null, context.DelegatedDocumentUri, out var @namespace, out var resolutionParams))
                {
                    var addUsingCodeAction = RazorCodeActionFactory.CreateAddComponentUsing(@namespace, newTagName: null, resolutionParams);
                    typeAccessibilityCodeActions.Add(addUsingCodeAction);
                    LogCodeActionTrace(logger, $"TypeAccessibility.VSCode.KeepAddUsing: Namespace='{@namespace}', Action={FormatCodeAction(addUsingCodeAction)}");
                }
                else
                {
                    LogCodeActionTrace(logger, $"TypeAccessibility.VSCode.SkipAddUsingResolutionParams: Fqn='{fqn}', Diagnostic={FormatDiagnostic(diagnostic)}, Action={FormatCodeAction(codeAction)}");
                }
            }
        }

        return typeAccessibilityCodeActions.ToImmutable();
    }

    private static ImmutableArray<RazorVSInternalCodeAction> ProcessCodeActionsVS(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions,
        ILogger logger)
    {
        using var typeAccessibilityCodeActions = new PooledArrayBuilder<RazorVSInternalCodeAction>();

        foreach (var codeAction in codeActions)
        {
            LogCodeActionTrace(logger, $"TypeAccessibility.VS.Consider: Action={FormatCodeAction(codeAction)}");

            if (codeAction.Name is not null && codeAction.Name.Equals(PredefinedCodeFixProviderNames.FullyQualify, StringComparison.Ordinal))
            {
                string action;

                if (!TryGetOwner(context, out var owner))
                {
                    // Failed to locate a valid owner for the light bulb
                    LogCodeActionTrace(logger, $"TypeAccessibility.VS.DropFullyQualifyNoOwner: Action={FormatCodeAction(codeAction)}");
                    continue;
                }
                else if (IsSingleLineDirectiveNode(owner))
                {
                    // Don't support single line directives
                    LogCodeActionTrace(logger, $"TypeAccessibility.VS.DropFullyQualifySingleLineDirective: Owner='{owner.GetType().Name}', Action={FormatCodeAction(codeAction)}");
                    continue;
                }
                else if (IsExplicitExpressionNode(owner))
                {
                    // Don't support explicit expressions
                    LogCodeActionTrace(logger, $"TypeAccessibility.VS.DropFullyQualifyExplicitExpression: Owner='{owner.GetType().Name}', Action={FormatCodeAction(codeAction)}");
                    continue;
                }
                else if (IsImplicitExpressionNode(owner))
                {
                    action = LanguageServerConstants.CodeActions.UnformattedRemap;
                }
                else
                {
                    // All other scenarios we support default formatted code action behavior
                    action = LanguageServerConstants.CodeActions.Default;
                }

                var wrappedAction = codeAction.WrapResolvableCodeAction(context, action);
                LogCodeActionTrace(logger, $"TypeAccessibility.VS.KeepFullyQualify: Owner='{owner.GetType().Name}', RemapAction='{action}', WrappedAction={FormatCodeAction(wrappedAction)}");
                typeAccessibilityCodeActions.Add(wrappedAction);
            }
            // For add using suggestions, the code action title is of the form:
            // `using System.Net;`
            else if (codeAction.Name is not null && codeAction.Name.Equals(PredefinedCodeFixProviderNames.AddImport, StringComparison.Ordinal) &&
                UsingDirectiveHelper.TryExtractNamespace(codeAction.Title, out var @namespace, out var prefix))
            {
                codeAction.Title = $"{prefix}@using {@namespace}";
                var wrappedAction = codeAction.WrapResolvableCodeAction(context, LanguageServerConstants.CodeActions.Default);
                LogCodeActionTrace(logger, $"TypeAccessibility.VS.KeepAddImport: Namespace='{@namespace}', Prefix='{prefix}', WrappedAction={FormatCodeAction(wrappedAction)}");
                typeAccessibilityCodeActions.Add(wrappedAction);
            }
            else if (codeAction.Name is not null && codeAction.Name.Equals(PredefinedCodeFixProviderNames.AddImport, StringComparison.Ordinal))
            {
                LogCodeActionTrace(logger, $"TypeAccessibility.VS.DropAddImportNamespaceExtractionFailed: Action={FormatCodeAction(codeAction)}");
            }
            // Not a type accessibility code action
            else
            {
                LogCodeActionTrace(logger, $"TypeAccessibility.VS.DropUnsupportedName: Action={FormatCodeAction(codeAction)}");
                continue;
            }
        }

        return typeAccessibilityCodeActions.ToImmutable();

        static bool TryGetOwner(RazorCodeActionContext context, [NotNullWhen(true)] out SyntaxNode? owner)
        {
            if (!context.CodeDocument.TryGetSyntaxRoot(out var root))
            {
                owner = null;
                return false;
            }

            owner = root.FindInnermostNode(context.StartAbsoluteIndex);
            if (owner is null)
            {
                Debug.Fail("Owner should never be null.");
                return false;
            }

            return true;
        }

        static bool IsImplicitExpressionNode(SyntaxNode owner)
        {
            // E.g, (| is position)
            //
            // `@|foo` - true
            //
            return owner.AncestorsAndSelf().Any(n => n is CSharpImplicitExpressionSyntax);
        }

        static bool IsExplicitExpressionNode(SyntaxNode owner)
        {
            // E.g, (| is position)
            //
            // `@(|foo)` - true
            //
            return owner.AncestorsAndSelf().Any(n => n is CSharpExplicitExpressionBodySyntax);
        }

        static bool IsSingleLineDirectiveNode(SyntaxNode owner)
        {
            // E.g, (| is position)
            //
            // `@inject |SomeType SomeName` - true
            //
            return owner.AncestorsAndSelf().Any(
                static n => n is RazorDirectiveSyntax directive && directive.IsDirectiveKind(DirectiveKind.SingleLine));
        }
    }

    private static RazorVSInternalCodeAction CreateFQNCodeAction(
        RazorCodeActionContext context,
        LspDiagnostic fqnDiagnostic,
        RazorVSInternalCodeAction nonFQNCodeAction,
        string fullyQualifiedName)
    {
        var codeDocumentIdentifier = new OptionalVersionedTextDocumentIdentifier() { DocumentUri = context.Request.TextDocument.DocumentUri };

        var fqnTextEdit = LspFactory.CreateTextEdit(fqnDiagnostic.Range, fullyQualifiedName);

        var fqnWorkspaceEditDocumentChange = new TextDocumentEdit()
        {
            TextDocument = codeDocumentIdentifier,
            Edits = [fqnTextEdit],
        };

        var fqnWorkspaceEdit = new WorkspaceEdit()
        {
            DocumentChanges = new[] { fqnWorkspaceEditDocumentChange }
        };

        var codeAction = RazorCodeActionFactory.CreateFullyQualifyComponent(nonFQNCodeAction.Title, fqnWorkspaceEdit);
        return codeAction;
    }

    private void LogCodeActionTrace(string message)
        => LogCodeActionTrace(_logger, message);

    private static void LogCodeActionTrace(ILogger logger, string message)
    {
        var fullMessage = $"RazorCodeActionTrace {message}";
        logger.LogDebug(fullMessage);
        Trace.WriteLine(fullMessage);
    }

    private static string FormatCodeActions(ImmutableArray<RazorVSInternalCodeAction> codeActions)
        => codeActions.IsEmpty
            ? "<empty>"
            : string.Join("; ", codeActions.Select(static (action, index) => $"{index}: {FormatCodeAction(action)}"));

    private static string FormatCodeAction(RazorVSInternalCodeAction action)
        => $"Name='{action.Name ?? "<null>"}', Title='{action.Title}', Kind='{action.Kind?.ToString() ?? "<null>"}', Group='{action.Group ?? "<null>"}', Children={action.Children?.Length ?? 0}, Command='{action.Command?.CommandIdentifier ?? "<null>"}', Data={FormatData(action.Data)}";

    private static string FormatData(object? data)
    {
        if (data is null)
        {
            return "<null>";
        }

        if (data is not JsonElement jsonData)
        {
            return data.GetType().FullName ?? data.GetType().Name;
        }

        var customTags = jsonData.TryGetProperty("CustomTags", out var customTagsValue) && customTagsValue.ValueKind == JsonValueKind.Array
            ? $"CustomTags=[{string.Join(", ", customTagsValue.EnumerateArray().Select(static tag => tag.GetString()))}]"
            : "CustomTags=<missing>";

        var fixAllFlavors = jsonData.TryGetProperty("FixAllFlavors", out var fixAllFlavorsValue) && fixAllFlavorsValue.ValueKind == JsonValueKind.Array
            ? $"FixAllFlavors={fixAllFlavorsValue.GetArrayLength()}"
            : "FixAllFlavors=<missing>";

        return $"JsonElement{{ValueKind={jsonData.ValueKind}, {customTags}, {fixAllFlavors}}}";
    }

    private static string FormatDiagnostics(IEnumerable<LspDiagnostic>? diagnostics)
        => diagnostics is null
            ? "<null>"
            : string.Join("; ", diagnostics.Select(static (diagnostic, index) => $"{index}: {FormatDiagnostic(diagnostic)}"));

    private static string FormatDiagnostic(LspDiagnostic diagnostic)
        => $"Code='{diagnostic.Code}', Severity='{diagnostic.Severity}', Range='{diagnostic.Range.Start.Line}:{diagnostic.Range.Start.Character}-{diagnostic.Range.End.Line}:{diagnostic.Range.End.Character}', Message='{diagnostic.Message}'";
}
