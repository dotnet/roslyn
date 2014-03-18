// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    /// <summary>
    /// A factory that creates instances of specific language services.
    /// </summary>
    internal interface ILanguageServiceFactory
    {
        ILanguageService CreateLanguageService(ILanguageServiceProvider provider);
    }
}