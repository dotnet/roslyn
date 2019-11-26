// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    // TODO here or below, we need a split for different languages
    // We may do this eiter creating such files for each content type or making the split below.
    // Actually, ITextDataModel contains ContentType. So, we may do the split below.
    [Export(typeof(ITextViewModelProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(VisualStudioChangeSignatureOptionsService.AddParameterTextViewRole)]
    internal class AddParameterTextViewModelProvider : ITextViewModelProvider
    {
        [Import]
        public IProjectionBufferFactoryService ProjectionBufferFactoryService { get; set; }

        public ITextViewModel CreateTextViewModel(ITextDataModel dataModel, ITextViewRoleSet roles)
        {
            var namespaceSpan = GetNamespaceSpan(dataModel.DataBuffer.CurrentSnapshot);

            var elisionBuffer = ProjectionBufferFactoryService.CreateElisionBuffer(
                null,
                new NormalizedSnapshotSpanCollection(namespaceSpan),
                ElisionBufferOptions.None);

            return new ElisionBufferTextViewModel(dataModel, elisionBuffer);
        }

        private SnapshotSpan GetNamespaceSpan(ITextSnapshot snapshot)
        {
            var totalLineNumber = snapshot.LineCount;
            var start = snapshot.GetLineFromLineNumber(0).Start;
            for (int i = 0; i < totalLineNumber; i++)
            {
                var currentLine = snapshot.GetLineFromLineNumber(i);
                string text = currentLine.GetText().Trim();
                //if (text.StartsWith("public virtual SomeCollection<T> Include(string path) => null;", StringComparison.Ordinal))
                if (text.StartsWith("namespace", StringComparison.Ordinal))
                {
                    int offset = "namespace".Length;
                    return new SnapshotSpan(currentLine.Start + offset, text.Length - offset);
                    //return new SnapshotSpan(currentLine.Start + text.IndexOf(")") - 1, 0);
                }
            }

            throw new Exception("Unable to find namespace span.");
        }
    }
}
