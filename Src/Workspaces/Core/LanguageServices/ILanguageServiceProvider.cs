// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface ILanguageServiceProvider
    {
        string Language { get; }
        ILanguageServiceProviderFactory Factory { get; }
        TLanguageService GetService<TLanguageService>() where TLanguageService : ILanguageService;
    }
}