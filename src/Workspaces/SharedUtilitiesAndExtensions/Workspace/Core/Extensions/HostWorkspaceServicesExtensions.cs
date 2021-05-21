// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    internal static class HostWorkspaceServicesExtensions
    {
        public static TLanguageService? GetLanguageService<TLanguageService>(this HostWorkspaceServices hostWorkspaceServices, string languageName) where TLanguageService : class, ILanguageService
            => hostWorkspaceServices?.GetExtendedLanguageServices(languageName).GetService<TLanguageService>();

        public static TLanguageService GetRequiredLanguageService<TLanguageService>(this HostWorkspaceServices hostWorkspaceServices, string languageName) where TLanguageService : class, ILanguageService
            => hostWorkspaceServices.GetExtendedLanguageServices(languageName).GetRequiredService<TLanguageService>();

#pragma warning disable RS0030 // Do not used banned API 'GetLanguageServices', use 'GetExtendedLanguageServices' instead - allow in this helper which computes the extended language services.
        /// <summary>
        /// Gets extended host language services, which includes language services from <see cref="HostWorkspaceServices.GetLanguageServices(string)"/>.
        /// </summary>
        public static HostLanguageServices GetExtendedLanguageServices(this HostWorkspaceServices hostWorkspaceServices, string languageName)
        {
            var languageServices = hostWorkspaceServices.GetLanguageServices(languageName);

#if CODE_STYLE
            languageServices = CodeStyleHostLanguageServices.GetRequiredMappedCodeStyleLanguageServices(languageServices);
#endif
            return languageServices;
        }
#pragma warning restore RS0030 // Do not used banned APIs
    }
}
