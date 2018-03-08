// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Roslyn.Utilities;

using CodeAnalysisQuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem;
using IntellisenseQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class Controller :
        AbstractController<Session<Controller, Model, IQuickInfoPresenterSession>, Model, IQuickInfoPresenterSession, IAsyncQuickInfoSession>
    {
        private static readonly object s_quickInfoPropertyKey = new object();
        private QuickInfoService _service;

        public Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IDocumentProvider documentProvider)
            : base(textView, subjectBuffer, null, null, documentProvider, "QuickInfo")
        {
        }

        // For testing purposes
        internal Controller(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IDocumentProvider documentProvider,
            QuickInfoService service)
            : base(textView, subjectBuffer, null, null, documentProvider, "QuickInfo")
        {
            _service = service;
        }

        internal static Controller GetInstance(
            ITextView textView,
            ITextBuffer subjectBuffer)
        {
            return textView.GetOrCreatePerSubjectBufferProperty(subjectBuffer, s_quickInfoPropertyKey,
                (v, b) => new Controller(v, b, new DocumentProvider()));
        }

        internal override void OnModelUpdated(Model modelOpt)
        {
            // do nothing
        }

        public async Task<IntellisenseQuickInfoItem> GetQuickInfoItemAsync(
            SnapshotPoint triggerPoint,
            CancellationToken cancellationToken)
        {
            var service = GetService();
            if (service == null)
            {
                return null;
            }

            var snapshot = this.SubjectBuffer.CurrentSnapshot;

            try
            {
                using (Logger.LogBlock(FunctionId.QuickInfo_ModelComputation_ComputeModelInBackground, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var document = await DocumentProvider.GetDocumentAsync(snapshot, cancellationToken).ConfigureAwait(false);
                    if (document == null)
                    {
                        return null;
                    }

                    var item = await service.GetQuickInfoAsync(document, triggerPoint, cancellationToken).ConfigureAwait(false);
                    if (item != null)
                    {
                        return BuildIntellisenseQuickInfoItem(triggerPoint, snapshot, item);
                    }

                    return null;
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public QuickInfoService GetService()
        {
            if (_service == null)
            {
                var snapshot = this.SubjectBuffer.CurrentSnapshot;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    _service = QuickInfoService.GetService(document);
                }
            }

            return _service;
        }

        private IntellisenseQuickInfoItem BuildIntellisenseQuickInfoItem(SnapshotPoint triggerPoint,
            ITextSnapshot snapshot, CodeAnalysisQuickInfoItem quickInfoItem)
        {
            var line = triggerPoint.GetContainingLine();
            var lineSpan = snapshot.CreateTrackingSpan(
                line.Extent,
                SpanTrackingMode.EdgeInclusive);

            // Build the first line of QuickInfo item, the images and the Description section should be on the first line with Wrapped style
            var glyphs = quickInfoItem.Tags.GetGlyphs();
            var symbolGlyph = glyphs.FirstOrDefault(g => g != Glyph.CompletionWarning);
            var warningGlyph = glyphs.FirstOrDefault(g => g == Glyph.CompletionWarning);
            var firstLineElements = new List<Object>();
            if (symbolGlyph != Glyph.None)
            {
                firstLineElements.Add(new ImageElement(symbolGlyph.GetImageId()));
            }

            if (warningGlyph != Glyph.None)
            {
                firstLineElements.Add(new ImageElement(warningGlyph.GetImageId()));
            }

            firstLineElements.Add(BuildClassifiedTextElement(
                quickInfoItem.Sections.Where(s => s.Kind == QuickInfoSectionKinds.Description).FirstOrDefault()));

            var elements = new List<object>();
            elements.Add(new ContainerElement(ContainerElementStyle.Wrapped, firstLineElements));

            // Add the remaining sections as Stacked style
            elements.AddRange(
                quickInfoItem.Sections.Where(s => s.Kind != QuickInfoSectionKinds.Description)
                                      .Select(section => BuildClassifiedTextElement(section)));

            var content = new ContainerElement(
                                ContainerElementStyle.Stacked,
                                elements);

            return new IntellisenseQuickInfoItem(lineSpan, content);
        }

        private static ClassifiedTextElement BuildClassifiedTextElement(QuickInfoSection section)
        {
            return new ClassifiedTextElement(section.TaggedParts.Select(
                    part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));
        }
    }
}
