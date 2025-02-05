// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class TextDocumentExtensions
{
    public static TLanguageService? GetLanguageService<TLanguageService>(this TextDocument? document) where TLanguageService : class, ILanguageService
        => document?.Project?.GetLanguageService<TLanguageService>();

    public static TLanguageService GetRequiredLanguageService<TLanguageService>(this TextDocument document) where TLanguageService : class, ILanguageService
        => document.Project.GetRequiredLanguageService<TLanguageService>();

#if !CODE_STYLE
    public static bool IsGeneratedCode(this TextDocument textDocument, CancellationToken cancellationToken)
    {
        var generatedCodeRecognitionService = textDocument.GetLanguageService<IGeneratedCodeRecognitionService>();
        if (textDocument is Document document)
        {
            return generatedCodeRecognitionService?.IsGeneratedCode(document, cancellationToken) == true;
        }
        else if (textDocument is AdditionalDocument additionalDocument)
        {
            var additionalText = textDocument.Project.AnalyzerOptions.AdditionalFiles.FirstOrDefault(static (text, additionalDocument) => true, additionalDocument);
            if (additionalText is not null)
                return generatedCodeRecognitionService?.IsGeneratedCode(additionalText, additionalDocument) == true;
            else
                return GeneratedCodeUtilities.IsGeneratedCodeFile(additionalDocument.FilePath);
        }

        return GeneratedCodeUtilities.IsGeneratedCodeFile(textDocument.FilePath);
    }
#endif

    public static async Task<bool> IsGeneratedCodeAsync(this TextDocument textDocument, CancellationToken cancellationToken)
    {
        var generatedCodeRecognitionService = textDocument.GetLanguageService<IGeneratedCodeRecognitionService>();
        if (generatedCodeRecognitionService is null)
            return false;

        if (textDocument is Document document)
        {
            return await generatedCodeRecognitionService.IsGeneratedCodeAsync(document, cancellationToken).ConfigureAwait(false);
        }
        else if (textDocument is AdditionalDocument additionalDocument)
        {
            var additionalText = textDocument.Project.AnalyzerOptions.AdditionalFiles.FirstOrDefault(static (text, additionalDocument) => true, additionalDocument);
            if (additionalText is not null)
                return generatedCodeRecognitionService.IsGeneratedCode(additionalText, additionalDocument);
            else
                return GeneratedCodeUtilities.IsGeneratedCodeFile(additionalDocument.FilePath);
        }

        return GeneratedCodeUtilities.IsGeneratedCodeFile(textDocument.FilePath);
    }

#if CODE_STYLE
    public static ValueTask<SourceText> GetValueTextAsync(this TextDocument document, CancellationToken cancellationToken)
    {
        if (document.TryGetText(out var text))
            return ValueTaskFactory.FromResult(text);

        return new ValueTask<SourceText>(document.GetTextAsync(cancellationToken));
    }
#endif

    /// <summary>
    /// Creates a new instance of this text document updated to have the text specified.
    /// </summary>
    public static TextDocument WithText(this TextDocument textDocument, SourceText text)
    {
        switch (textDocument)
        {
            case Document document:
                return document.WithText(text);

            case AnalyzerConfigDocument analyzerConfigDocument:
                return analyzerConfigDocument.WithAnalyzerConfigDocumentText(text);

            case AdditionalDocument additionalDocument:
                return additionalDocument.WithAdditionalDocumentText(text);

            default:
                throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Creates a new instance of this additional document updated to have the text specified.
    /// </summary>
    public static TextDocument WithAdditionalDocumentText(this TextDocument textDocument, SourceText text)
    {
        Contract.ThrowIfFalse(textDocument is AdditionalDocument);
        return textDocument.Project.Solution.WithAdditionalDocumentText(textDocument.Id, text, PreservationMode.PreserveIdentity).GetTextDocument(textDocument.Id)!;
    }

    /// <summary>
    /// Creates a new instance of this analyzer config document updated to have the text specified.
    /// </summary>
    public static TextDocument WithAnalyzerConfigDocumentText(this TextDocument textDocument, SourceText text)
    {
        Contract.ThrowIfFalse(textDocument is AnalyzerConfigDocument);
        return textDocument.Project.Solution.WithAnalyzerConfigDocumentText(textDocument.Id, text, PreservationMode.PreserveIdentity).GetTextDocument(textDocument.Id)!;
    }
}
