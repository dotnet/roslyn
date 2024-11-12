// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using StructureTag = Microsoft.CodeAnalysis.Editor.Implementation.Structure.StructureTag;

namespace Microsoft.CodeAnalysis.Editor.Structure
{
    [Export(typeof(ITaggerProvider))]
    [Export(typeof(AbstractStructureTaggerProvider))]
    [TagType(typeof(IStructureTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class StructureTaggerProvider(
        TaggerHost taggerHost,
        EditorOptionsService editorOptionsService,
        IProjectionBufferFactoryService projectionBufferFactoryService,
        ITextEditorFactoryService textEditorFactoryService) : AbstractStructureTaggerProvider(taggerHost, editorOptionsService, projectionBufferFactoryService)
    {
        private readonly ITextEditorFactoryService _textEditorFactoryService = textEditorFactoryService;

        internal override object? GetCollapsedHintForm(StructureTag structureTag)
        {
            return new ViewHostingControl(CreateElisionBufferView, () => CreateElisionBufferForTagTooltip(structureTag));
        }

        private IWpfTextView CreateElisionBufferView(ITextBuffer finalBuffer)
            => CreateShrunkenTextView(ThreadingContext, _textEditorFactoryService, finalBuffer);

        private static IWpfTextView CreateShrunkenTextView(
            IThreadingContext threadingContext,
            ITextEditorFactoryService textEditorFactoryService,
            ITextBuffer finalBuffer)
        {
            var roles = textEditorFactoryService.CreateTextViewRoleSet(OutliningRegionTextViewRole);
            var view = textEditorFactoryService.CreateTextView(finalBuffer, roles);

            view.Background = Brushes.Transparent;

            view.SizeToFit(threadingContext);

            // Zoom out a bit to shrink the text.
            view.ZoomLevel *= 0.75;

            return view;
        }

        private const string OutliningRegionTextViewRole = nameof(OutliningRegionTextViewRole);
    }
}
