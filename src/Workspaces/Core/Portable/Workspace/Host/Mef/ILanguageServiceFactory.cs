// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
