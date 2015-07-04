// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal partial class DiagnosticsClassificationTaggerProvider
    {
        private class TagSource : AbstractAggregatedDiagnosticsTagSource<ClassificationTag>
        {
            private static ClassificationTag s_tag;

            public TagSource(
                ITextBuffer subjectBuffer,
                IForegroundNotificationService notificationService,
                DiagnosticService service,
                ClassificationTypeMap typeMap,
                IAsynchronousOperationListener asyncListener) :
                base(subjectBuffer, notificationService, service, asyncListener)
            {
                CacheClassificationTag(typeMap);
            }

            private void CacheClassificationTag(ClassificationTypeMap typeMap)
            {
                // cache tag since it can't be changed
                s_tag = s_tag ?? new ClassificationTag(typeMap.GetClassificationType(ClassificationTypeDefinitions.UnnecessaryCode));
            }

            protected override int MinimumLength
            {
                get
                {
                    return 0;
                }
            }

            protected override bool ShouldInclude(DiagnosticData diagnostic)
            {
                return diagnostic.CustomTags.Contains(tag => tag == WellKnownDiagnosticTags.Unnecessary);
            }

            protected override TagSpan<ClassificationTag> CreateTagSpan(SnapshotSpan span, DiagnosticData diagnostic)
            {
                Contract.Requires(ShouldInclude(diagnostic));

                return new TagSpan<ClassificationTag>(span, s_tag);
            }

            public override ITagSpanIntervalTree<ClassificationTag> GetAccurateTagIntervalTreeForBuffer(ITextBuffer buffer, CancellationToken cancellationToken)
            {
                // when contrast changed, tagger will refresh itself if host support high contrast mode. (VS does)
                if (HighContrastChecker.IsHighContrast)
                {
                    // if we are under high contrast mode, don't return anything.
                    // this basically will make us not fade out in high contrast mode (ex, unused usings)
                    return null;
                }

                return base.GetAccurateTagIntervalTreeForBuffer(buffer, cancellationToken);
            }
        }
    }
}
