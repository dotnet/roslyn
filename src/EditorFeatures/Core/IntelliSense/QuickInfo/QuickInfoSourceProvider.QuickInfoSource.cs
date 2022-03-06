// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

using IntellisenseQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class QuickInfoSourceProvider
    {
        private sealed class QuickInfoSource : IAsyncQuickInfoSource
        {
            private readonly ITextBuffer _subjectBuffer;
            private readonly IThreadingContext _threadingContext;
            private readonly IUIThreadOperationExecutor _operationExecutor;
            private readonly IAsynchronousOperationListener _asyncListener;
            private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;
            private readonly IGlobalOptionService _globalOptions;

            public QuickInfoSource(
                ITextBuffer subjectBuffer,
                IThreadingContext threadingContext,
                IUIThreadOperationExecutor operationExecutor,
                IAsynchronousOperationListener asyncListener,
                Lazy<IStreamingFindUsagesPresenter> streamingPresenter,
                IGlobalOptionService globalOptions)
            {
                _subjectBuffer = subjectBuffer;
                _threadingContext = threadingContext;
                _operationExecutor = operationExecutor;
                _asyncListener = asyncListener;
                _streamingPresenter = streamingPresenter;
                _globalOptions = globalOptions;
            }

            public async Task<IntellisenseQuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
            {
                var triggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
                if (!triggerPoint.HasValue)
                    return null;

                var snapshot = triggerPoint.Value.Snapshot;
                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                    return null;

                var service = QuickInfoService.GetService(document);
                if (service == null)
                    return null;

                try
                {
                    using (Logger.LogBlock(FunctionId.Get_QuickInfo_Async, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var options = _globalOptions.GetSymbolDescriptionOptions(document.Project.Language);
                        var item = await service.GetQuickInfoAsync(document, triggerPoint.Value, options, cancellationToken).ConfigureAwait(false);
                        if (item != null)
                        {
                            var textVersion = snapshot.Version;
                            var trackingSpan = textVersion.CreateTrackingSpan(item.Span.ToSpan(), SpanTrackingMode.EdgeInclusive);
                            var classificationOptions = _globalOptions.GetClassificationOptions(document.Project.Language);

                            return await IntellisenseQuickInfoBuilder.BuildItemAsync(
                                trackingSpan, item, document, classificationOptions,
                                _threadingContext, _operationExecutor,
                                _asyncListener, _streamingPresenter, cancellationToken).ConfigureAwait(false);
                        }

                        return null;
                    }
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
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
