﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using System.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare
{
    [ExportLanguageServiceFactory(typeof(IIndentationService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspIndentationServiceFactory : ILanguageServiceFactory
    {

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return languageServices.GetOriginalLanguageService<IIndentationService>();
        }
    }
}
