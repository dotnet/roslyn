// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
{
    internal partial class SmartIndent : ISmartIndent
    {
        private readonly ITextView _textView;

        public SmartIndent(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            _textView = textView;
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            return GetDesiredIndentationAsync(line).WaitAndGetResult(CancellationToken.None);
        }

        internal Task<int?> GetDesiredIndentationAsync(ITextSnapshotLine line)
        {
            return GetDesiredIndentationAsync(line, CancellationToken.None);
        }

        public void Dispose()
        {
        }

        private async Task<int?> GetDesiredIndentationAsync(ITextSnapshotLine lineToBeIndented, CancellationToken cancellationToken)
        {
            if (lineToBeIndented == null)
            {
                throw new ArgumentNullException(@"line");
            }

            using (Logger.LogBlock(FunctionId.SmartIndentation_Start, cancellationToken))
            {
                var document = lineToBeIndented.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return null;
                }

                var service = document.GetLanguageService<IIndentationService>();
                if (service == null)
                {
                    return null;
                }

                var result = await service.GetDesiredIndentationAsync(document, lineToBeIndented.LineNumber, cancellationToken).ConfigureAwait(false);
                if (result == null)
                {
                    return null;
                }

                return result.Value.GetIndentation(_textView, lineToBeIndented);
            }
        }
    }
}
