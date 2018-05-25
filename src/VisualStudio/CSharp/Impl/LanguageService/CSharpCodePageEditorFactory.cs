// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [Guid(Guids.CSharpCodePageEditorFactoryIdString)]
    internal class CSharpCodePageEditorFactory : AbstractCodePageEditorFactory
    {
        public CSharpCodePageEditorFactory(AbstractEditorFactory editorFactory)
            : base(editorFactory)
        {
        }
    }
}
