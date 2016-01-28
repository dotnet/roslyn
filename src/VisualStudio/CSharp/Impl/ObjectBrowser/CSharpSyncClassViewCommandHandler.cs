// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ClassView;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.ClassView, ContentTypeNames.CSharpContentType)]
    internal class CSharpSyncClassViewCommandHandler : AbstractSyncClassViewCommandHandler
    {
        [ImportingConstructor]
        private CSharpSyncClassViewCommandHandler(SVsServiceProvider serviceProvider, IWaitIndicator waitIndicator)
            : base(serviceProvider, waitIndicator)
        {
        }
    }
}
