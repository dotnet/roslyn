// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

using SyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

internal class TypeAccessibilityCodeActionProvider : ICSharpCodeActionProvider
{
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
        if (context.Request?.Context?.Diagnostics is null)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (codeActions.IsEmpty)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var results = context.SupportsCodeActionResolve
            ? ProcessCodeActionsVS(context, codeActions)
            : ProcessCodeActionsVSCode(context, codeActions);

        var orderedResults = results.Sort(static (x, y) => StringComparer.CurrentCulture.Compare(x.Title, y.Title));
        return Task.FromResult(orderedResults);
    }

    private static ImmutableArray<RazorVSInternalCodeAction> ProcessCodeActionsVSCode(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions)
    {
        var diagnostics = context.Request.Context.Diagnostics.Where(diagnostic =>
            diagnostic is { Severity: LspDiagnosticSeverity.Error, Code: { } code } &&
            code.TryGetSecond(out var str) &&
            s_supportedDiagnostics.Any(d => str.Equals(d, StringComparison.OrdinalIgnoreCase)));

        if (diagnostics is null || !diagnostics.Any())
        {
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
                continue;
            }

            var diagnosticSpan = context.SourceText.GetTextSpan(range);

            // Based on how we compute `Range.AsTextSpan` it's possible to have a span
            // which goes beyond the end of the source text. Something likely changed
            // between the capturing of the diagnostic (by the platform) and the retrieval of the
            // document snapshot / source text. In such a case, we skip processing of the diagnostic.
            if (diagnosticSpan.End > context.SourceText.Length)
            {
                continue;
            }

            foreach (var codeAction in codeActions)
            {
                var name = codeAction.Name;
                if (name is null || !name.Equals(LanguageServerConstants.CodeActions.CodeActionFromVSCode, StringComparison.Ordinal))
                {
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
                    continue;
                }

                var fqnCodeAction = CreateFQNCodeAction(context, diagnostic, codeAction, fqn);
                typeAccessibilityCodeActions.Add(fqnCodeAction);

                if (AddUsingsCodeActionResolver.TryCreateAddUsingResolutionParams(fqn, context.Request.TextDocument, additionalEdit: null, context.DelegatedDocumentUri, out var @namespace, out var resolutionParams))
                {
                    var addUsingCodeAction = RazorCodeActionFactory.CreateAddComponentUsing(@namespace, newTagName: null, resolutionParams);
                    typeAccessibilityCodeActions.Add(addUsingCodeAction);
                }
            }
        }

        return typeAccessibilityCodeActions.ToImmutable();
    }

    private static ImmutableArray<RazorVSInternalCodeAction> ProcessCodeActionsVS(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions)
    {
        using var typeAccessibilityCodeActions = new PooledArrayBuilder<RazorVSInternalCodeAction>();

        foreach (var codeAction in codeActions)
        {
            if (codeAction.Name is not null && codeAction.Name.Equals(RazorPredefinedCodeFixProviderNames.FullyQualify, StringComparison.Ordinal))
            {
                string action;

                if (!TryGetOwner(context, out var owner))
                {
                    // Failed to locate a valid owner for the light bulb
                    continue;
                }
                else if (IsSingleLineDirectiveNode(owner))
                {
                    // Don't support single line directives
                    continue;
                }
                else if (IsExplicitExpressionNode(owner))
                {
                    // Don't support explicit expressions
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

                typeAccessibilityCodeActions.Add(codeAction.WrapResolvableCodeAction(context, action));
            }
            // For add using suggestions, the code action title is of the form:
            // `using System.Net;`
            else if (codeAction.Name is not null && codeAction.Name.Equals(RazorPredefinedCodeFixProviderNames.AddImport, StringComparison.Ordinal) &&
                UsingDirectiveHelper.TryExtractNamespace(codeAction.Title, out var @namespace, out var prefix))
            {
                codeAction.Title = $"{prefix}@using {@namespace}";
                typeAccessibilityCodeActions.Add(codeAction.WrapResolvableCodeAction(context, LanguageServerConstants.CodeActions.Default));
            }
            // Not a type accessibility code action
            else
            {
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
}
