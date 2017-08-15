// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
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
        public HACK_CSharpCreateServicesOnUIThread([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(serviceProvider, LanguageNames.CSharp)
        {
        }
    }
}
