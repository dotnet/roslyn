// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class ExtractToCodeBehindCodeActionProvider(ILoggerFactory loggerFactory) : IRazorCodeActionProvider
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<ExtractToCodeBehindCodeActionProvider>();

    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        if (!context.SupportsFileCreation)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!context.CodeDocument.FileKind.IsComponent())
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!context.CodeDocument.TryGetSyntaxRoot(out var root))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var owner = root.FindInnermostNode(context.StartAbsoluteIndex);
        if (owner is null)
        {
            _logger.LogWarning($"Owner should never be null.");
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var directiveNode = owner.Parent switch
        {
            // When the caret is '@code$$ {' or '@code$${' then tree is:
            // RazorDirective -> RazorDirectiveBody -> CSharpCodeBlock -> (MetaCode or TextLiteral)
            CSharpCodeBlockSyntax { Parent.Parent: RazorDirectiveSyntax d } => d,
            // When the caret is '@$$code' or '@c$$ode' or '@co$$de' or '@cod$$e' then tree is:
            // RazorDirective -> RazorDirectiveBody -> MetaCode
            RazorDirectiveBodySyntax { Parent: RazorDirectiveSyntax d } => d,
            // When the caret is '$$@code' then tree is:
            // RazorDirective
            RazorDirectiveSyntax d => d,
            _ => null
        };
        if (directiveNode is null)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Make sure we've found a @code or @functions
        if (!directiveNode.IsDirective(ComponentCodeDirective.Directive) &&
            !directiveNode.IsDirective(FunctionsDirective.Directive))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // No code action if malformed
        if (directiveNode.GetDiagnostics().Any(d => d.Severity == RazorDiagnosticSeverity.Error))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var csharpCodeBlockNode = directiveNode.DirectiveBody.CSharpCode;
        if (csharpCodeBlockNode is null)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        // Do not provide code action if the cursor is inside the code block
        if (context.StartAbsoluteIndex > csharpCodeBlockNode.SpanStart)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (HasUnsupportedChildren(csharpCodeBlockNode))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!TryGetNamespace(context.CodeDocument, out var @namespace))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var actionParams = new ExtractToCodeBehindCodeActionParams()
        {
            ExtractStart = csharpCodeBlockNode.Span.Start,
            ExtractEnd = csharpCodeBlockNode.Span.End,
            RemoveStart = directiveNode.Span.Start,
            RemoveEnd = directiveNode.Span.End,
            Namespace = @namespace
        };

        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = context.Request.TextDocument,
            Action = LanguageServerConstants.CodeActions.ExtractToCodeBehind,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = actionParams,
        };

        var codeAction = RazorCodeActionFactory.CreateExtractToCodeBehind(resolutionParams);
        return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([codeAction]);
    }

    private static bool TryGetNamespace(RazorCodeDocument codeDocument, [NotNullWhen(returnValue: true)] out string? @namespace)
        // If the compiler can't provide a computed namespace it will fallback to "__GeneratedComponent" or
        // similar for the NamespaceNode. This would end up with extracting to a wrong namespace
        // and causing compiler errors. Avoid offering this refactoring if we can't accurately get a
        // good namespace to extract to
        => codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out @namespace);

    private static bool HasUnsupportedChildren(RazorSyntaxNode node)
        => node.DescendantNodes().Any(n => n is MarkupBlockSyntax or CSharpTransitionSyntax or RazorCommentBlockSyntax);
}
