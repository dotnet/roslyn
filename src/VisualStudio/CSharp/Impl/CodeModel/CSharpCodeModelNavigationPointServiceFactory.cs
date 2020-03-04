﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    [ExportLanguageServiceFactory(typeof(ICodeModelNavigationPointService), LanguageNames.CSharp), Shared]
    internal partial class CSharpCodeModelNavigationPointServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpCodeModelNavigationPointServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            // This interface is implemented by the ICodeModelService as well, so just grab the other one and return it
            return provider.GetService<ICodeModelService>();
        }
    }
}
