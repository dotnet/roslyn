// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSyncClassViewCommandHandler(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
            : base(threadingContext, serviceProvider)
        {
        }
    }
}
