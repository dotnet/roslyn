﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IStructureTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class StructureTaggerProvider :
        AbstractStructureTaggerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StructureTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IAsynchronousOperationListenerProvider listenerProvider)
                : base(threadingContext, notificationService, editorOptionsFactoryService, projectionBufferFactoryService, listenerProvider)
        {
        }

        internal override object? GetCollapsedHintForm(StructureTag structureTag)
        {
            return CreateElisionBufferForTagTooltip(structureTag).CurrentSnapshot.GetText();
        }
    }
}
