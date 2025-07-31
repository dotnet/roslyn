// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Host;

internal static class Extensions
{
    private const string RazorCSharpLspClientName = "RazorCSharp";

    public static bool CanApplyChange([NotNullWhen(returnValue: true)] this TextDocument? document)
        => document?.State.CanApplyChange() ?? false;

    public static bool CanApplyChange([NotNullWhen(returnValue: true)] this TextDocumentState? document)
        => document?.DocumentServiceProvider.GetService<IDocumentOperationService>()?.CanApplyChange ?? false;

    public static bool SupportsDiagnostics([NotNullWhen(returnValue: true)] this TextDocument? document)
        => document?.State.SupportsDiagnostics() ?? false;

    public static bool SupportsDiagnostics([NotNullWhen(returnValue: true)] this TextDocumentState? document)
        => document?.DocumentServiceProvider.GetService<IDocumentOperationService>()?.SupportDiagnostics ?? false;

    public static bool IsRazorDocument(this TextDocument document)
        => IsRazorDocument(document.State);

    public static bool IsRazorDocument(this TextDocumentState documentState)
        => documentState.DocumentServiceProvider.GetService<DocumentPropertiesService>()?.DiagnosticsLspClientName == RazorCSharpLspClientName;

    public static bool IsRazorSourceGeneratedDocument(this Document document)
        => document is SourceGeneratedDocument { Identity.Generator.TypeName: "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator" };

    public static async Task<ImmutableArray<MappedSpanResult>?> TryGetMappedSpanResultAsync(this Document document, ImmutableArray<TextSpan> textSpans, CancellationToken cancellationToken)
    {
        if (document is SourceGeneratedDocument sourceGeneratedDocument &&
            document.Project.Solution.Services.GetService<ISourceGeneratedDocumentSpanMappingService>() is { } sourceGeneratedSpanMappingService)
        {
            var result = await sourceGeneratedSpanMappingService.MapSpansAsync(sourceGeneratedDocument, textSpans, cancellationToken).ConfigureAwait(false);
            if (result.IsDefaultOrEmpty)
            {
                return null;
            }

            Contract.ThrowIfFalse(textSpans.Length == result.Length,
                $"The number of input spans {textSpans.Length} should match the number of mapped spans returned {result.Length}");
            return result;
        }

        var spanMappingService = document.DocumentServiceProvider.GetService<ISpanMappingService>();
        if (spanMappingService == null)
        {
            return null;
        }

        var mappedSpanResult = await spanMappingService.MapSpansAsync(document, textSpans, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfFalse(textSpans.Length == mappedSpanResult.Length,
            $"The number of input spans {textSpans.Length} should match the number of mapped spans returned {mappedSpanResult.Length}");
        return mappedSpanResult;
    }
}
