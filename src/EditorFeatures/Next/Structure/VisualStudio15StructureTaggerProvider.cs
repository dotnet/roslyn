// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

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
        public VisualStudio15StructureTaggerProvider(
            IForegroundNotificationService notificationService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
                : base(notificationService, textEditorFactoryService, editorOptionsFactoryService, projectionBufferFactoryService, asyncListeners)
        {
        }

        protected override IBlockTag CreateTag(
            IBlockTag parentTag, ITextSnapshot snapshot, BlockSpan region)
        {
            return new RoslynBlockTag(
                this.TextEditorFactoryService,
                this.ProjectionBufferFactoryService,
                this.EditorOptionsFactoryService,
                parentTag, snapshot, region);
        }

        private class RoslynBlockTag : RoslynOutliningRegionTag, IBlockTag
        {
            public IBlockTag Parent { get; }
            public int Level { get; }
            public SnapshotSpan Span { get; }
            public SnapshotSpan StatementSpan { get; }

            public string Type => ConvertType(BlockSpan.Type);

            public bool IsCollapsible => true;

            public RoslynBlockTag(
                ITextEditorFactoryService textEditorFactoryService,
                IProjectionBufferFactoryService projectionBufferFactoryService,
                IEditorOptionsFactoryService editorOptionsFactoryService,
                IBlockTag parent,
                ITextSnapshot snapshot,
                BlockSpan outliningSpan) :
                base(textEditorFactoryService,
                    projectionBufferFactoryService,
                    editorOptionsFactoryService,
                    snapshot, outliningSpan)
            {
                Parent = parent;
                Level = parent == null ? 0 : parent.Level + 1;
                Span = outliningSpan.TextSpan.ToSnapshotSpan(snapshot);
                StatementSpan = outliningSpan.HintSpan.ToSnapshotSpan(snapshot);
            }

            public override bool Equals(object obj)
                => Equals(obj as RoslynBlockTag);

            /// <summary>
            /// This is only called if the spans for the tags were the same.  However, even if we 
            /// have the same span as the previous tag (taking into account span mapping) that 
            /// doesn't mean we can use the old block tag.  Specifically, the editor will look at
            /// other fields in the tags So we need to make sure that these values have not changed
            /// if we want to reuse the old block tag.  For example, perhaps the item's type changed
            /// (i.e. from class to struct).  It will have the same span, but might have a new 
            /// presentation as the 'Type' will be different.
            /// </summary>
            public bool Equals(RoslynBlockTag tag)
            {
                return tag != null &&
                       this.Level == tag.Level &&
                       this.Type == tag.Type &&
                       EqualityComparer<object>.Default.Equals(this.CollapsedForm, tag.CollapsedForm) &&
                       this.StatementSpan == tag.StatementSpan &&
                       this.Span == tag.Span;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(this.Level,
                       Hash.Combine(this.Type, 
                       Hash.Combine(this.CollapsedForm,
                       Hash.Combine(this.StatementSpan.GetHashCode(), this.Span.GetHashCode()))));
            }

            private string ConvertType(string type)
            {
                switch (type)
                {
                    // Basic types.
                    case BlockTypes.Structural: return PredefinedStructureTypes.Structural;
                    case BlockTypes.Nonstructural: return PredefinedStructureTypes.Nonstructural;

                    // Top level declarations.  Note that Enum is not currently supported
                    // and that we map Module down to Class.
                    case BlockTypes.Namespace: return PredefinedStructureTypes.Namespace;
                    case BlockTypes.Structure: return PredefinedStructureTypes.Struct;
                    case BlockTypes.Interface: return PredefinedStructureTypes.Interface;
                    case BlockTypes.Module:
                    case BlockTypes.Class: return PredefinedStructureTypes.Class;

                    // Member declarations
                    case BlockTypes.Accessor: return PredefinedStructureTypes.AccessorBlock;
                    case BlockTypes.Constructor: return PredefinedStructureTypes.Constructor;
                    case BlockTypes.Destructor: return PredefinedStructureTypes.Destructor;
                    case BlockTypes.Method: return PredefinedStructureTypes.Method;
                    case BlockTypes.Operator: return PredefinedStructureTypes.Operator;

                    // Map events/indexers/properties all to the 'property' type.
                    case BlockTypes.Event:
                    case BlockTypes.Indexer:
                    case BlockTypes.Property: return PredefinedStructureTypes.PropertyBlock;

                    // Statements
                    case BlockTypes.Case: return PredefinedStructureTypes.Case;
                    case BlockTypes.Conditional: return PredefinedStructureTypes.Conditional;
                    case BlockTypes.Lock: return PredefinedStructureTypes.Lock;
                    case BlockTypes.Loop: return PredefinedStructureTypes.Loop;
                    case BlockTypes.TryCatchFinally: return PredefinedStructureTypes.TryCatchFinally;
                    case BlockTypes.Standalone: return PredefinedStructureTypes.Standalone;

                    // Expressions
                    case BlockTypes.AnonymousMethod: return PredefinedStructureTypes.AnonymousMethodBlock;

                    // These types don't currently map to any editor types.  Just make them
                    // the 'Unknown' type for now.
                    case BlockTypes.Enum:
                    case BlockTypes.Other:
                    case BlockTypes.Xml:
                    default:
                        return PredefinedStructureTypes.Unknown;
                }
            }
        }
    }
}