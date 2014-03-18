// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
#if MEF
    using Microsoft.CodeAnalysis.CodeGeneration;

    [ExportLanguageServiceFactory(typeof(ICodeGenerationService), LanguageNames.CSharp)]
#endif
    internal partial class CSharpCodeGenerationServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(ILanguageServiceProvider provider)
        {
            return new CSharpCodeGenerationService(provider);
        }
    }
}