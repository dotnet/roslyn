// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    internal static class HostWorkspaceServicesExtensions
    {
        /// <summary>
        /// Gets extended host language services, which includes language services from <paramref name="languageServices"/>.
        /// </summary>
        public static LanguageServices GetExtendedLanguageServices(this LanguageServices languageServices)
#if CODE_STYLE
            => CodeStyleHostLanguageServices.GetExtendedLanguageServices(languageServices);
#else
            => languageServices;
#endif
    }
}
