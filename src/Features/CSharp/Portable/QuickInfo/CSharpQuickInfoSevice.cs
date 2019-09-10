// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo
{
    [ExportLanguageServiceFactory(typeof(QuickInfoService), LanguageNames.CSharp), Shared]
    internal class CSharpQuickInfoServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpQuickInfoServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpQuickInfoService(languageServices.WorkspaceServices.Workspace);
        }
    }

    internal class CSharpQuickInfoService : QuickInfoServiceWithProviders
    {
        internal CSharpQuickInfoService(Workspace workspace)
            : base(workspace, LanguageNames.CSharp)
        {
        }
    }
}

