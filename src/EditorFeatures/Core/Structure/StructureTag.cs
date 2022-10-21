// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    internal sealed class StructureTag : IStructureTag
    {
        private readonly AbstractStructureTaggerProvider _tagProvider;

        public StructureTag(AbstractStructureTaggerProvider tagProvider, BlockSpan blockSpan, ITextSnapshot snapshot)
        {
            Snapshot = snapshot;
            OutliningSpan = blockSpan.TextSpan.ToSpan();
            Type = ConvertType(blockSpan.Type);
            IsCollapsible = blockSpan.IsCollapsible;
            IsDefaultCollapsed = blockSpan.IsDefaultCollapsed;
            IsImplementation = blockSpan.AutoCollapse;

            if (blockSpan.HintSpan.Start < blockSpan.TextSpan.Start)
            {
                // The HeaderSpan is what is used for drawing the guidelines and also what is shown if
                // you mouse over a guideline. We will use the text from the hint start to the collapsing
                // start; in the case this spans mutiple lines the editor will clip it for us and suffix an
                // ellipsis at the end.
                HeaderSpan = Span.FromBounds(blockSpan.HintSpan.Start, blockSpan.TextSpan.Start);
            }
            else
            {
                var hintLine = snapshot.GetLineFromPosition(blockSpan.HintSpan.Start);
                HeaderSpan = AbstractStructureTaggerProvider.TrimLeadingWhitespace(hintLine.Extent);
            }

            CollapsedText = blockSpan.BannerText;
            CollapsedHintFormSpan = blockSpan.HintSpan.ToSpan();
            _tagProvider = tagProvider;
        }

        /// <summary>
        /// The contents of the buffer to show if we mouse over the collapsed indicator.
        /// </summary>
        public readonly Span CollapsedHintFormSpan;

        public readonly string CollapsedText;

        public ITextSnapshot Snapshot { get; }
        public Span? OutliningSpan { get; }
        public Span? HeaderSpan { get; }
        public Span? GuideLineSpan => null;
        public int? GuideLineHorizontalAnchorPoint => null;
        public string Type { get; }
        public bool IsCollapsible { get; }
        public bool IsDefaultCollapsed { get; }
        public bool IsImplementation { get; }

        public object? GetCollapsedForm()
        {
            return CollapsedText;
        }

        public object? GetCollapsedHintForm()
        {
            return _tagProvider.GetCollapsedHintForm(this);
        }

        private static string ConvertType(string type)
        {
            return type switch
            {
                BlockTypes.Conditional => PredefinedStructureTagTypes.Conditional,
                BlockTypes.Comment => PredefinedStructureTagTypes.Comment,
                BlockTypes.Expression => PredefinedStructureTagTypes.Expression,
                BlockTypes.Imports => PredefinedStructureTagTypes.Imports,
                BlockTypes.Loop => PredefinedStructureTagTypes.Loop,
                BlockTypes.Member => PredefinedStructureTagTypes.Member,
                BlockTypes.Namespace => PredefinedStructureTagTypes.Namespace,
                BlockTypes.Nonstructural => PredefinedStructureTagTypes.Nonstructural,
                BlockTypes.PreprocessorRegion => PredefinedStructureTagTypes.PreprocessorRegion,
                BlockTypes.Statement => PredefinedStructureTagTypes.Statement,
                BlockTypes.Type => PredefinedStructureTagTypes.Type,
                _ => PredefinedStructureTagTypes.Structural
            };
        }
    }
}
