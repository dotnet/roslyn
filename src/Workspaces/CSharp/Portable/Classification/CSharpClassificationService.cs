// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    [ExportLanguageServiceFactory(typeof(ClassificationService), LanguageNames.CSharp), Shared]
    internal class CSharpClassificationServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpClassificationService(languageServices.WorkspaceServices.Workspace);
        }

        private class CSharpClassificationService : CommonClassificationService
        {
            public CSharpClassificationService(Workspace workspace)
                : base(workspace)
            {
            }

            protected override string Language => LanguageNames.CSharp;
        }
    }
}
