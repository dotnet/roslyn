// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TextDiffing;

internal static class TextDifferencingServiceExtensions
{
    public static IHierarchicalDifferenceCollection DiffSourceTexts(this ITextDifferencingService diffService, SourceText oldText, SourceText newText, ITextBufferFactoryService bufferFactoryService, StringDifferenceOptions options)
    {
        var oldTextSnapshot = oldText.FindCorrespondingEditorTextSnapshot();
        var newTextSnapshot = newText.FindCorrespondingEditorTextSnapshot();

        if (oldTextSnapshot != null || newTextSnapshot != null)
        {
            // If either source text has an associated snapshot, then we can utilize that 
            // snapshot to compute the diff.  This allows us to avoid allocating strings
            // for the diffing process, which can be expensive for large files.
            oldTextSnapshot ??= CreateTextSnapshot(oldText, bufferFactoryService, newTextSnapshot!.ContentType);
            newTextSnapshot ??= CreateTextSnapshot(newText, bufferFactoryService, oldTextSnapshot!.ContentType);

            return diffService.DiffSnapshotSpans(oldTextSnapshot.GetFullSpan(), newTextSnapshot.GetFullSpan(), options);

            static ITextSnapshot CreateTextSnapshot(SourceText text, ITextBufferFactoryService bufferFactoryService, IContentType contentType)
            {
                // Unable to find an existing snapshot for the given SourceText. Create a temporary one to aid in diff computation.
                var reader = new SourceTextReader(text);
                var buffer = bufferFactoryService.CreateTextBuffer(reader, contentType);

                return buffer.CurrentSnapshot;
            }
        }

        // Fallback to diffing by string
        return diffService.DiffStrings(oldText.ToString(), newText.ToString(), options);
    }

    private sealed class SourceTextReader(SourceText sourceText) : TextReader
    {
        private readonly SourceText _sourceText = sourceText;
        private int _position = 0;

        public override int Peek()
        {
            return _position == _sourceText.Length
                ? -1
                : _sourceText[_position];
        }

        public override int Read()
        {
            return _position == _sourceText.Length
                ? -1
                : _sourceText[_position++];
        }

        public override int Read(char[] buffer, int index, int count)
        {
            var charsToCopy = Math.Min(count, _sourceText.Length - _position);
            _sourceText.CopyTo(_position, buffer, index, charsToCopy);
            _position += charsToCopy;
            return charsToCopy;
        }
    }
}
