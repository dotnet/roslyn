// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
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
        public CSharpGraphProvider(
            IGlyphService glyphService,
            SVsServiceProvider serviceProvider,
            IProgressionPrimaryWorkspaceProvider workspaceProvider,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners) :
            base(glyphService, serviceProvider, workspaceProvider.PrimaryWorkspace, asyncListeners)
        {
        }
    }
}
