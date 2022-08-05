// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Per language services provided by the host environment.
    /// </summary>
    internal sealed class HostProjectServices
    {
        private readonly HostLanguageServices _services;

        public SolutionServices SolutionServices => _services.WorkspaceServices.SolutionServices;

        // This ensures a single instance of this type associated with each HostLanguageServices.
        [Obsolete("Do not call directly.  Use HostLanguageServices.ProjectServices to acquire an instance")]
        internal HostProjectServices(HostLanguageServices services)
        {
            _services = services;
        }

        /// <inheritdoc cref="HostLanguageServices.Language"/>
        public string Language
            => _services.Language;

        /// <inheritdoc cref="HostLanguageServices.GetService"/>
        public TLanguageService? GetService<TLanguageService>() where TLanguageService : ILanguageService
            => _services.GetService<TLanguageService>();

        /// <inheritdoc cref="HostLanguageServices.GetRequiredService"/>
        public TLanguageService GetRequiredService<TLanguageService>() where TLanguageService : ILanguageService
            => _services.GetRequiredService<TLanguageService>();
    }
}
