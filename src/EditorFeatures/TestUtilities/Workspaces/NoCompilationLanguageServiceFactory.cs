// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [ExportLanguageServiceFactory(typeof(INoCompilationLanguageService), NoCompilationConstants.LanguageName), Shared]
    internal class NoCompilationLanguageServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public NoCompilationLanguageServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new NoCompilationLanguageService();
        }

        private class NoCompilationLanguageService : INoCompilationLanguageService
        {
        }
    }

    internal interface INoCompilationLanguageService : ILanguageService
    {
    }
}
