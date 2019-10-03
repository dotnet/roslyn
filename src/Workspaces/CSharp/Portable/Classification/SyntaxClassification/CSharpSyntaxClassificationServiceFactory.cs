// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Classification.SyntaxClassification
{
    [ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxClassificationServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSyntaxClassificationServiceFactory()
        {
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpSyntaxClassificationService(languageServices);
        }
    }
}
