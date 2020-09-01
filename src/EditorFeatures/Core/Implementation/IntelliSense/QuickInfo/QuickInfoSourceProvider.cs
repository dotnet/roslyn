// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("RoslynQuickInfoProvider")]
    internal partial class QuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public QuickInfoSourceProvider(
            IThreadingContext threadingContext,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
        {
            _threadingContext = threadingContext;
            _streamingPresenter = streamingPresenter;
        }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            if (textBuffer.IsInCloudEnvironmentClientContext())
            {
                return null;
            }

            return new QuickInfoSource(textBuffer, _threadingContext, _streamingPresenter);
        }
    }
}
