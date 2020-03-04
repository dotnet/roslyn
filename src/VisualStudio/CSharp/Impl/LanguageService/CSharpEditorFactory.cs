// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
