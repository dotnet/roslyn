// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using NewIndentationService = Microsoft.CodeAnalysis.Indentation.IIndentationService;
using OldIndentationService = Microsoft.CodeAnalysis.Editor.IIndentationService;
using OldSynchronousIndentationService = Microsoft.CodeAnalysis.Editor.ISynchronousIndentationService;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
{
    internal partial class SmartIndent : ISmartIndent
    {
        private readonly ITextView _textView;

        public SmartIndent(ITextView textView)
        {
            _textView = textView ?? throw new ArgumentNullException(nameof(textView));
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            return GetDesiredIndentation(line, CancellationToken.None);
        }

        public void Dispose()
        {
        }

        private int? GetDesiredIndentation(ITextSnapshotLine lineToBeIndented, CancellationToken cancellationToken)
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

                // First, try to go through the normal feature-layer indentation service.
                var newService = document.GetLanguageService<NewIndentationService>();
                if (newService != null)
                {
                    var result = newService.GetIndentation(document, lineToBeIndented.LineNumber, cancellationToken);
                    return result.GetIndentation(_textView, lineToBeIndented);
                }

                // If we don't have a feature-layer service, try to fall back to the legacy
                // editor-feature-layer interfaces.

#pragma warning disable CS0618 // Type or member is obsolete
                var oldSyncService = document.GetLanguageService<OldSynchronousIndentationService>();
                if (oldSyncService != null)
                {
                    var result = (Indentation.IndentationResult?)oldSyncService.GetDesiredIndentation(document, lineToBeIndented.LineNumber, cancellationToken);
                    return result?.GetIndentation(_textView, lineToBeIndented);
                }

                var oldAsyncService = document.GetLanguageService<OldIndentationService>();
                if (oldAsyncService != null)
                {
                    var result = (Indentation.IndentationResult?)oldAsyncService.GetDesiredIndentation(document, lineToBeIndented.LineNumber, cancellationToken).WaitAndGetResult(cancellationToken);
                    return result?.GetIndentation(_textView, lineToBeIndented);
                }
#pragma warning restore CS0618 // Type or member is obsolete

                return null;
            }
        }
    }
}
