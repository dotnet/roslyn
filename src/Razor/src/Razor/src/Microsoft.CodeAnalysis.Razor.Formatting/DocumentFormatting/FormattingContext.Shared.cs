// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

// This partial contains the host-neutral formatting state and is compiled into both Remote Razor and
// Microsoft.CodeAnalysis.Razor.DotNetFormat. Keep Remote-only analysis, such as FormattingVisitor and its
// formatting-span caches, in the other partial so the dotnet format adapter does not acquire that dependency closure.
internal sealed partial class FormattingContext
{
    private readonly RazorCSharpDocument? _csharpDocument;

    private FormattingContext(
        IDocumentSnapshot originalSnapshot,
        RazorCodeDocument codeDocument,
        bool? declarationDocument,
        IDocumentSnapshot currentSnapshot,
        FormattingEngineOptions options,
        IFormattingLogger? logger,
        bool includeCSharpLanguageFeatureEdits,
        int hostDocumentIndex,
        char triggerCharacter)
    {
        OriginalSnapshot = originalSnapshot;
        CodeDocument = codeDocument;
        CurrentSnapshot = currentSnapshot;
        Options = options;
        Logger = logger;
        IncludeCSharpLanguageFeatureEdits = includeCSharpLanguageFeatureEdits;
        HostDocumentIndex = hostDocumentIndex;
        TriggerCharacter = triggerCharacter;

        if (declarationDocument is { } declDoc)
        {
            _csharpDocument = codeDocument.GetRequiredCSharpDocument(declDoc);
        }
    }

    public static bool SkipValidateComponents { get; set; }

    public IDocumentSnapshot OriginalSnapshot { get; }
    public RazorCodeDocument CodeDocument { get; }
    public IDocumentSnapshot CurrentSnapshot { get; }
    public FormattingEngineOptions Options { get; }
    public IFormattingLogger? Logger { get; }
    public bool IncludeCSharpLanguageFeatureEdits { get; }
    public int HostDocumentIndex { get; }
    public char TriggerCharacter { get; }

    public SourceText SourceText => CodeDocument.Source.Text;

    public RazorCSharpDocument CSharpDocument => _csharpDocument.AssumeNotNull("Cannot get C# source text when declaration document is not specified.");

    public string NewLineString => Options.NewLine;

    /// <summary>
    /// Generates a string of indentation based on a specific indentation level. For instance, inside of a C# method represents 1 indentation level. A method within a class would have indentaiton level of 2 by default etc.
    /// </summary>
    /// <param name="indentationLevel">The indentation level to represent</param>
    /// <returns>A whitespace string representing the indentation level based on the configuration.</returns>
    public string GetIndentationLevelString(int indentationLevel)
    {
        if (indentationLevel == 0)
        {
            return "";
        }

        var indentation = GetIndentationOffsetForLevel(indentationLevel);
        var indentationString = FormattingUtilities.GetIndentationString(indentation, Options.InsertSpaces, Options.TabSize);
        return indentationString;
    }

    /// <summary>
    /// Given a level, returns the corresponding offset.
    /// </summary>
    /// <param name="level">A value representing the indentation level.</param>
    /// <returns></returns>
    public int GetIndentationOffsetForLevel(int level)
    {
        return level * Options.TabSize;
    }

    public async Task<FormattingContext> WithTextAsync(SourceText changedText, CancellationToken cancellationToken)
    {
        // Each pass supplies the complete current text. Updating the original immutable snapshot with that text lets
        // each host regenerate its Razor and C# outputs through its normal pipeline.
        var changedSnapshot = OriginalSnapshot.WithText(changedText);

        var codeDocument = await changedSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        DEBUG_ValidateComponents(CodeDocument, codeDocument);

        var newContext = new FormattingContext(
            OriginalSnapshot,
            codeDocument,
            _csharpDocument?.IsDeclarationDocument,
            currentSnapshot: changedSnapshot,
            Options,
            Logger,
            IncludeCSharpLanguageFeatureEdits,
            HostDocumentIndex,
            TriggerCharacter);

        return newContext;
    }

    public FormattingContext WithCSharpDocument(bool declarationDocument)
        => new(
            OriginalSnapshot,
            CodeDocument,
            declarationDocument,
            CurrentSnapshot,
            Options,
            Logger,
            IncludeCSharpLanguageFeatureEdits,
            HostDocumentIndex,
            TriggerCharacter);

    /// <summary>
    /// It can be difficult in the testing infrastructure to correct constructs input files that work consistently across
    /// context changes, so this method validates that the number of components isn't changing due to lost tag help info.
    /// Without this guarantee its hard to reason about test behaviour/failures.
    /// </summary>
    [Conditional("DEBUG")]
    private static void DEBUG_ValidateComponents(RazorCodeDocument oldCodeDocument, RazorCodeDocument newCodeDocument)
    {
        if (SkipValidateComponents)
        {
            return;
        }

        var oldTagHelperElements = oldCodeDocument.GetRequiredSyntaxRoot().DescendantNodesAndSelf().OfType<MarkupTagHelperElementSyntax>().Count();
        var newTagHelperElements = newCodeDocument.GetRequiredSyntaxRoot().DescendantNodesAndSelf().OfType<MarkupTagHelperElementSyntax>().Count();
        Debug.Assert(oldTagHelperElements == newTagHelperElements, $"Previous context had {oldTagHelperElements} components, new only has {newTagHelperElements}.");
    }

    public static FormattingContext Create(
        IDocumentSnapshot originalSnapshot,
        RazorCodeDocument codeDocument,
        FormattingEngineOptions options,
        IFormattingLogger? logger)
    {
        return new FormattingContext(
            originalSnapshot,
            codeDocument,
            declarationDocument: null,
            currentSnapshot: originalSnapshot,
            options,
            logger,
            includeCSharpLanguageFeatureEdits: false,
            hostDocumentIndex: 0,
            triggerCharacter: '\0');
    }
}
