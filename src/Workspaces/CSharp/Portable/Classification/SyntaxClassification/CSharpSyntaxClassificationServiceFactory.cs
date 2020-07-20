// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => new CSharpSyntaxClassificationService(languageServices);
    }
}
