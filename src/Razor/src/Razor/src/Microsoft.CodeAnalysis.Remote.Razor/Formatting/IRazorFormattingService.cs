// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

internal interface IRazorFormattingService
{
    Task<ImmutableArray<TextChange>> GetDocumentFormattingChangesAsync(
       RemoteDocumentContext documentContext,
       ImmutableArray<TextChange> htmlEdits,
       LinePositionSpan? span,
       RazorFormattingOptions options,
       CancellationToken cancellationToken);

    Task<ImmutableArray<TextChange>> GetHtmlOnTypeFormattingChangesAsync(
      RemoteDocumentContext documentContext,
      ImmutableArray<TextChange> htmlEdits,
      RazorFormattingOptions options,
      int hostDocumentIndex,
      char triggerCharacter,
      CancellationToken cancellationToken);

    Task<ImmutableArray<TextChange>> GetCSharpOnTypeFormattingChangesAsync(
      RemoteDocumentContext documentContext,
      RazorFormattingOptions options,
      int hostDocumentIndex,
      char triggerCharacter,
      bool declarationDocument,
      CancellationToken cancellationToken);

    Task<TextChange?> TryGetSingleCSharpEditAsync(
        RemoteDocumentContext documentContext,
        TextChange csharpEdit,
        bool declarationDocument,
        RazorFormattingOptions options,
        CancellationToken cancellationToken);

    Task<TextChange?> TryGetCSharpCodeActionEditAsync(
       RemoteDocumentSnapshot documentSnapshot,
       ImmutableArray<TextChange> csharpEdits,
       bool declarationDocument,
       RazorFormattingOptions options,
       CancellationToken cancellationToken);

    Task<TextChange?> TryGetCSharpSnippetFormattingEditAsync(
       RemoteDocumentContext documentContext,
       ImmutableArray<TextChange> csharpEdits,
       bool declarationDocument,
       RazorFormattingOptions options,
       CancellationToken cancellationToken);

    bool TryGetOnTypeFormattingTriggerKind(
        RazorCodeDocument codeDocument,
        int hostDocumentIndex,
        string triggerCharacter,
        out RazorLanguageKind triggerCharacterKind);
}
