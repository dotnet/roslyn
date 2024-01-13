// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Progression;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Progression
{
    [GraphProvider(Name = "CSharpRoslynProvider", ProjectCapability = "CSharp")]
    internal sealed class CSharpGraphProvider : AbstractGraphProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGraphProvider(
            IThreadingContext threadingContext,
            IGlyphService glyphService,
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, glyphService, serviceProvider, workspace, streamingPresenter, listenerProvider)
        {
        }
    }
}
