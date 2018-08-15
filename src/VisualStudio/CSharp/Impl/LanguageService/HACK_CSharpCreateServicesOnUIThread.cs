// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class HACK_CSharpCreateServicesOnUIThread : HACK_AbstractCreateServicesOnUiThread
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HACK_CSharpCreateServicesOnUIThread(IThreadingContext threadingContext, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(threadingContext, serviceProvider, LanguageNames.CSharp)
        {
        }
    }
}
