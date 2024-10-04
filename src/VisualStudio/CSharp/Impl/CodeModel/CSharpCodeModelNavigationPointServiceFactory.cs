// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    [ExportLanguageServiceFactory(typeof(ICodeModelNavigationPointService), LanguageNames.CSharp), Shared]
    internal partial class CSharpCodeModelNavigationPointServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
