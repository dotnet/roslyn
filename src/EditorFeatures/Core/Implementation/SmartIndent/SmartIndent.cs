// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
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
            => _textView = textView ?? throw new ArgumentNullException(nameof(textView));

        public void Dispose()
        {
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
            => GetDesiredIndentationSynchronously(line, CancellationToken.None);

        private int? GetDesiredIndentationSynchronously(ITextSnapshotLine line, CancellationToken cancellationToken)
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

                var options = IndentationOptions.FromDocumentAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                var syntacticDocument = SyntacticDocument.CreateSynchronously(document, cancellationToken);
                var result = newService.GetIndentation(syntacticDocument, line.LineNumber, options.AutoFormattingOptions.IndentStyle, options, cancellationToken);
                return result.GetIndentation(_textView, line);
            }
        }
    }
}
