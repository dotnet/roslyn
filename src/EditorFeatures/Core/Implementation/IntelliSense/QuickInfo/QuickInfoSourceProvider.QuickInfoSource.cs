// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

using IntellisenseQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class QuickInfoSourceProvider
    {
        private class QuickInfoSource : IAsyncQuickInfoSource
        {
            private readonly ITextBuffer _subjectBuffer;
            private readonly IThreadingContext _threadingContext;
            private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

            public QuickInfoSource(
                ITextBuffer subjectBuffer,
                IThreadingContext threadingContext,
                Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
            {
                _subjectBuffer = subjectBuffer;
                _threadingContext = threadingContext;
                _streamingPresenter = streamingPresenter;
            }

            public async Task<IntellisenseQuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
            {
                var triggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
                if (!triggerPoint.HasValue)
                {
                    return null;
                }

                var snapshot = triggerPoint.Value.Snapshot;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return null;
                }

                var service = QuickInfoService.GetService(document);
                if (service == null)
                {
                    return null;
                }

                try
                {
                    using (Internal.Log.Logger.LogBlock(FunctionId.Get_QuickInfo_Async, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var item = await service.GetQuickInfoAsync(document, triggerPoint.Value, cancellationToken).ConfigureAwait(false);
                        if (item != null)
                        {
                            var textVersion = snapshot.Version;
                            var trackingSpan = textVersion.CreateTrackingSpan(item.Span.ToSpan(), SpanTrackingMode.EdgeInclusive);
                            return await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan, item, document, _threadingContext, _streamingPresenter, cancellationToken).ConfigureAwait(false);
                        }

                        return null;
                    }
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            public void Dispose()
            {
            }
        }
    }
}
