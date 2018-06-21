// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExcludeFromCodeCoverage]
    [Guid(Guids.CSharpEditorFactoryIdString)]
    internal class CSharpEditorFactory : AbstractEditorFactory
    {
        public CSharpEditorFactory(IComponentModel componentModel)
            : base(componentModel)
        {
        }

        protected override string ContentTypeName => ContentTypeNames.CSharpContentType;
        protected override string LanguageName => LanguageNames.CSharp;
    }
}
