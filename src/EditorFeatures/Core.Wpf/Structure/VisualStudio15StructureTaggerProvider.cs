// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
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
namespace Microsoft.CodeAnalysis.Editor.Structure
{
    [Export(typeof(ITaggerProvider))]
    [Export(typeof(VisualStudio15StructureTaggerProvider))]
    [TagType(typeof(IBlockTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class VisualStudio15StructureTaggerProvider :
        AbstractStructureTaggerProvider<IBlockTag>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudio15StructureTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            ITextEditorFactoryService textEditorFactoryService,
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
