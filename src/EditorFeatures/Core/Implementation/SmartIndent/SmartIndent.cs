﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
{
    internal partial class SmartIndent : ISmartIndent
    {
        private readonly ITextView _textView;

        public SmartIndent(ITextView textView)
            => _textView = textView ?? throw new ArgumentNullException(nameof(textView));

        public int? GetDesiredIndentation(ITextSnapshotLine line)
            => GetDesiredIndentation(line, CancellationToken.None);

        public void Dispose()
        {
        }

        private int? GetDesiredIndentation(ITextSnapshotLine line, CancellationToken cancellationToken)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            using (Logger.LogBlock(FunctionId.SmartIndentation_Start, cancellationToken))
            {
                var document = line.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                    return null;

                var newService = document.GetLanguageService<IIndentationService>();
                if (newService == null)
                    return null;

                var result = newService.GetIndentation(document, line.LineNumber, cancellationToken);
                return result.GetIndentation(_textView, line);
            }
        }
    }
}
