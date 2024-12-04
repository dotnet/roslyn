// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

internal partial class RazorDynamicFileInfoProvider
{
    private sealed class TextChangesTextLoader(
        TextDocument? document,
        IEnumerable<TextChange> changes,
        byte[] checksum,
        SourceHashAlgorithm checksumAlgorithm,
        int? codePage,
        Uri razorUri) : TextLoader
    {
        private readonly Lazy<SourceText> _emptySourceText = new Lazy<SourceText>(() =>
        {
            var encoding = codePage is null ? null : Encoding.GetEncoding(codePage.Value);
            return SourceText.From("", checksumAlgorithm: checksumAlgorithm, encoding: encoding);
        });

        public override async Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        {
            if (document is null)
            {
                var text = _emptySourceText.Value.WithChanges(changes);
                return TextAndVersion.Create(text, VersionStamp.Default.GetNewerVersion());
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // Validate the checksum information so the edits are known to be correct
            if (IsSourceTextMatching(sourceText))
            {
                var version = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                var newText = sourceText.WithChanges(changes);
                return TextAndVersion.Create(newText, version.GetNewerVersion());
            }

            return await GetFullDocumentFromServerAsync(razorUri, cancellationToken).ConfigureAwait(false);
        }

        private bool IsSourceTextMatching(SourceText sourceText)
        {
            if (sourceText.ChecksumAlgorithm != checksumAlgorithm)
            {
                return false;
            }

            if (sourceText.Encoding?.CodePage != codePage)
            {
                return false;
            }

            if (!sourceText.GetChecksum().SequenceEqual(checksum))
            {
                return false;
            }

            return true;
        }

        private async Task<TextAndVersion> GetFullDocumentFromServerAsync(Uri razorUri, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(LanguageServerHost.Instance, "We don't have an LSP channel yet to send this request through.");
            var clientLanguageServerManager = LanguageServerHost.Instance.GetRequiredLspService<IClientLanguageServerManager>();

            var response = await clientLanguageServerManager.SendRequestAsync<RazorProvideDynamicFileParams, RazorProvideDynamicFileResponse>(
                ProvideRazorDynamicFileInfoMethodName,
                new RazorProvideDynamicFileParams
                {
                    RazorDocument = new()
                    {
                        Uri = razorUri,
                    },
                    FullText = true
                },
                cancellationToken);

            RoslynDebug.AssertNotNull(response.Edits);
            RoslynDebug.Assert(response.Edits.IsSingle());

            var textChanges = response.Edits.Select(e => new TextChange(e.Span.ToTextSpan(), e.NewText));
            var text = _emptySourceText.Value.WithChanges(textChanges);
            return TextAndVersion.Create(text, VersionStamp.Default.GetNewerVersion());
        }
    }

}
