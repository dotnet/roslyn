// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// A factory that creates instances of a specific <see cref="ILanguageService"/>.
    /// 
    /// Implement a <see cref="ILanguageServiceFactory"/> when you want to provide <see cref="ILanguageService"/> instances that use other services.
    /// </summary>
    public interface ILanguageServiceFactory
    {
        /// <summary>
        /// Creates a new <see cref="ILanguageService"/> instance.
        /// </summary>
        /// <param name="languageServices">The <see cref="HostLanguageServices"/> that can be used to access other services.</param>
        ILanguageService CreateLanguageService(HostLanguageServices languageServices);
    }
}
