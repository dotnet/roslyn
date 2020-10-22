// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    [Export(typeof(ITaggerProvider))]
    [Export(typeof(VisualStudioForMacStructureTaggerProvider))]
    [TagType(typeof(IBlockTag))]
    [ContentType("Roslyn Languages")]//ContentTypeNames.RoslynContentType)]
    internal partial class VisualStudioForMacStructureTaggerProvider :
        AbstractStructureTaggerProvider<IBlockTag>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioForMacStructureTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            ICocoaTextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IAsynchronousOperationListenerProvider listenerProvider)
                : base(threadingContext, notificationService, textEditorFactoryService, editorOptionsFactoryService, projectionBufferFactoryService, listenerProvider)
        {
        }

        protected override IBlockTag CreateTag(
            IBlockTag parentTag, ITextSnapshot snapshot, BlockSpan region)
        {
            return new RoslynBlockTag(
                ThreadingContext,
                this.TextEditorFactoryService,
                this.ProjectionBufferFactoryService,
                this.EditorOptionsFactoryService,
                parentTag, snapshot, region);
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
