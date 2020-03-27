﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    [Export(typeof(ITextViewModelProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(ChangeSignatureViewModelFactoryService.AddParameterTextViewRole)]
    internal class AddParameterTextViewModelProvider : ITextViewModelProvider
    {
        public IProjectionBufferFactoryService ProjectionBufferFactoryService { get; }

        [ImportingConstructor]
        public AddParameterTextViewModelProvider(IProjectionBufferFactoryService projectionBufferFactoryService)
        {
            ProjectionBufferFactoryService = projectionBufferFactoryService;
        }

        public ITextViewModel CreateTextViewModel(ITextDataModel dataModel, ITextViewRoleSet roles)
        {
            var projectionSnapshot = (IProjectionSnapshot)dataModel.DataBuffer.CurrentSnapshot;
            // There are five spans: 
            // 0. From the start of the document and to the inserted comma before the insertion.
            // 1. The insertion span for the type - we need to catch it for AddParameterTypeTextViewRole.
            // 2. the space span
            // 3. The parameter name span.
            // 4. The rest of the document.
            // In case of VB, we revert spans: <3> AS <1>
            int index;
            if (roles.Contains(ChangeSignatureViewModelFactoryService.AddParameterTypeTextViewRole))
            {
                index = 1;
            }
            else
            {
                index = 3;
            }

            var span = projectionSnapshot.GetSourceSpans()[index];
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
