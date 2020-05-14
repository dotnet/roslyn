// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    // Our implementation of an outlining region tag.  The collapsedHintForm
    // is dynamically created using an elision buffer over the actual text
    // we are collapsing.
    internal class RoslynOutliningRegionTag : IOutliningRegionTag
    {
        private readonly BlockTagState _state;

        public RoslynOutliningRegionTag(
            IThreadingContext threadingContext,
            ITextEditorFactoryService textEditorFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextSnapshot snapshot,
            BlockSpan blockSpan)
        {
            _state = new BlockTagState(
                threadingContext,
                textEditorFactoryService, projectionBufferFactoryService,
                editorOptionsFactoryService, snapshot, blockSpan);
        }

        public override bool Equals(object obj)
            => Equals(obj as RoslynOutliningRegionTag);

        public bool Equals(RoslynOutliningRegionTag tag)
            => tag != null && _state.Equals(tag._state);

        public override int GetHashCode()
            => _state.GetHashCode();

        public object CollapsedForm => _state.CollapsedForm;

        public object CollapsedHintForm => _state.CollapsedHintForm;

        public bool IsDefaultCollapsed => _state.IsDefaultCollapsed;

        public bool IsImplementation => _state.IsImplementation;
    }
}
