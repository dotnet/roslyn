// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    [Export(typeof(ITextViewModelProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(VisualStudioChangeSignatureOptionsService.AddParameterTextViewRole)]
    internal class AddParameterTextViewModelProvider : ITextViewModelProvider
    {
        [Import]
        public IProjectionBufferFactoryService ProjectionBufferFactoryService { get; set; }

        public ITextViewModel CreateTextViewModel(ITextDataModel dataModel, ITextViewRoleSet roles)
        {
            var projectionSnapshot = (IProjectionSnapshot)dataModel.DataBuffer.CurrentSnapshot;
            // There are three spans: 
            // 1. From the start of the document and to the inserted comma before the insertion.
            // 2. The insertion span -  we need to catch it here.
            // 3. The rest of the document.
            var span = projectionSnapshot.GetSourceSpans()[1];
            var mappedSpans = projectionSnapshot.MapFromSourceSnapshot(span);
            var elisionBuffer =
                ProjectionBufferFactoryService.CreateElisionBuffer(
                    projectionEditResolver: null,
                    exposedSpans: new NormalizedSnapshotSpanCollection(
                        new[] { new SnapshotSpan(dataModel.DocumentBuffer.CurrentSnapshot, mappedSpans[0]) }),
                    options: ElisionBufferOptions.None);

            return new ElisionBufferTextViewModel(dataModel, elisionBuffer);
        }
    }
}
