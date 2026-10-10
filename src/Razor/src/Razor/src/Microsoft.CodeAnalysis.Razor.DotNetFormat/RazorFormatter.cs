// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

public static class RazorFormatter
{
    // dotnet format calls this formatter sequentially.
    private static readonly IFormattingLoggerFactory s_formattingLoggerFactory = new FormattingLoggerFactory();
    private static readonly FormattingLoggerFactoryAdapter s_loggerFactory = new(formattingLogger: null);
    private static readonly ProjectHostServicesProvider s_hostServicesProvider = new(hostServices: null);
    private static readonly ImmutableArray<IFormattingPass> s_documentFormattingPasses =
    [
        new RazorFormattingPass(),
        new CSharpFormattingPass(s_hostServicesProvider, s_loggerFactory)
    ];
    private static readonly FormattingEngine s_formattingEngine = new(s_loggerFactory);

    public static async Task<SourceText?> TryFormatAsync(
        Project project,
        DocumentId documentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(documentId);

        var document = project.GetAdditionalDocument(documentId)
            ?? throw new ArgumentException("The document must be an additional document in the supplied project.", nameof(documentId));

        if (!IsRazorDocument(document))
        {
            return null;
        }

        var snapshot = await ProjectBackedFormattingSnapshot.TryCreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            return null;
        }

        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var options = GetFormattingOptions(document, sourceText, cancellationToken);
        var formattingLogger = s_formattingLoggerFactory.CreateLogger(document.FilePath!, "DotNetFormat");
        var context = FormattingContext.Create(snapshot, snapshot.CodeDocument, options, formattingLogger);

        try
        {
            s_loggerFactory.SetFormattingLogger(formattingLogger);
            s_hostServicesProvider.SetHostServices(project.Solution.Workspace.Services.HostServices);
            var changes = await s_formattingEngine.FormatDocumentAsync(
                context,
                s_documentFormattingPasses,
                htmlChanges: [],
                range: null,
                options,
                cancellationToken).ConfigureAwait(false);

            return sourceText.WithChanges(changes);
        }
        finally
        {
            s_loggerFactory.SetFormattingLogger(null);
            s_hostServicesProvider.SetHostServices(null);
        }
    }

    private static bool IsRazorDocument(TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.FilePath is { } filePath &&
            Path.GetExtension(filePath) is var extension &&
            (extension.Equals(".razor", StringComparison.OrdinalIgnoreCase) ||
             extension.Equals(".cshtml", StringComparison.OrdinalIgnoreCase));
    }

    private static FormattingEngineOptions GetFormattingOptions(
        TextDocument document,
        SourceText sourceText,
        CancellationToken cancellationToken)
    {
        var csharpOptions = document.GetCSharpSyntaxFormattingOptions(cancellationToken);

        return new()
        {
            InsertSpaces = !csharpOptions.LineFormatting.UseTabs,
            TabSize = csharpOptions.LineFormatting.TabSize,
            NewLine = GetNewLine(sourceText),
            CSharpSyntaxFormattingOptions = csharpOptions,
        };
    }

    private static string GetNewLine(SourceText sourceText)
    {
        // dotnet format has no LSP formatting request from which to take a newline. Preserve the document's dominant
        // convention, using its first newline to break ties and the environment only for a single-line document.
        var crlfCount = 0;
        var lfCount = 0;
        string? firstNewLine = null;

        foreach (var line in sourceText.Lines)
        {
            var lineBreakLength = line.EndIncludingLineBreak - line.End;
            if (lineBreakLength == 2)
            {
                crlfCount++;
                firstNewLine ??= "\r\n";
            }
            else if (lineBreakLength == 1)
            {
                lfCount++;
                firstNewLine ??= "\n";
            }
        }

        return lfCount == crlfCount
            ? firstNewLine ?? Environment.NewLine
            : lfCount > crlfCount ? "\n" : "\r\n";
    }
}
