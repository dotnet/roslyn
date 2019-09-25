﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.LocalForwarders
{
    [ExportLanguageServiceFactory(typeof(BlockStructureService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspBlockStructureServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return languageServices.GetOriginalLanguageService<BlockStructureService>();
        }
    }
}
