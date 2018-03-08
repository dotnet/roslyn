// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
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
            private readonly QuickInfoSourceProvider _quickInfoSourceProvider;
            private readonly ITextBuffer _subjectBuffer;            
            private IDocumentProvider _documentProvider = new DocumentProvider();

            public QuickInfoSource(QuickInfoSourceProvider quickInfoSourceProvider, ITextBuffer subjectBuffer)
            {
                _quickInfoSourceProvider = quickInfoSourceProvider;
                _subjectBuffer = subjectBuffer;
            }

            public async Task<IntellisenseQuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
            {
                var triggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
                if (triggerPoint.HasValue)
                {
                    var textView = session.TextView;

                    var snapshot = _subjectBuffer.CurrentSnapshot;
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
                        using (Internal.Log.Logger.LogBlock(FunctionId.QuickInfo_ModelComputation_ComputeModelInBackground, cancellationToken))
                        {
                            cancellationToken.ThrowIfCancellationRequested();                            

                            var item = await service.GetQuickInfoAsync(document, triggerPoint.Value, cancellationToken).ConfigureAwait(false);
                            if (item != null)
                            {
                                return IntellisenseQuickInfoBuilder.BuildItem(triggerPoint.Value, item);
                            }

                            return null;
                        }
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }

                return null;
            }
           
            public void Dispose()
            {
            }
        }
    }
}
