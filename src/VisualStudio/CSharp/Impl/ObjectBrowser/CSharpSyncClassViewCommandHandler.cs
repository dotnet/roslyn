// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ClassView;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
{
    [Export(typeof(Commanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.ClassView)]
    internal class CSharpSyncClassViewCommandHandler : AbstractSyncClassViewCommandHandler
    {
        [ImportingConstructor]
        private CSharpSyncClassViewCommandHandler(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}
