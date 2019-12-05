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
            var elisionBuffer =
                ProjectionBufferFactoryService.CreateElisionBuffer(/*resolver=*/null,
                                            new NormalizedSnapshotSpanCollection(new SnapshotSpan(dataModel.DataBuffer.CurrentSnapshot, 85, 0)),
                                            ElisionBufferOptions.None);                                            //dataModel.DataBuffer.ContentType);

            return new ElisionBufferTextViewModel(dataModel, elisionBuffer);
        }
    }
}
