// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
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
            IProgressionPrimaryWorkspaceProvider workspaceProvider,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, glyphService, serviceProvider, workspaceProvider.PrimaryWorkspace, listenerProvider)
        {
        }
    }
}
