﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Host
{
    // TODO(cyrusn): Make public.  Tracked through https://github.com/dotnet/roslyn/issues/62914
    internal sealed class HostSolutionServices
    {
        /// <remarks>
        /// Note: do not expose publicly.  <see cref="HostWorkspaceServices"/> exposes a <see
        /// cref="HostWorkspaceServices.Workspace"/> which we want to avoid doing from our immutable snapshots.
        /// </remarks>
        private readonly HostWorkspaceServices _services;

        // This ensures a single instance of this type associated with each HostWorkspaceServices.
        [Obsolete("Do not call directly.  Use HostWorkspaceServices.SolutionServices to acquire an instance")]
        internal HostSolutionServices(HostWorkspaceServices services)
        {
            _services = services;
        }

        /// <inheritdoc cref="HostWorkspaceServices.GetService"/>
        public TWorkspaceService? GetService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService
            => _services.GetService<TWorkspaceService>();

        /// <inheritdoc cref="HostWorkspaceServices.GetRequiredService"/>
        public TWorkspaceService GetRequiredService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService
            => _services.GetRequiredService<TWorkspaceService>();

        /// <inheritdoc cref="HostWorkspaceServices.SupportedLanguages"/>
        public IEnumerable<string> SupportedLanguages
            => _services.SupportedLanguages;

        /// <inheritdoc cref="HostWorkspaceServices.IsSupported"/>
        public bool IsSupported(string languageName)
            => _services.IsSupported(languageName);

        /// <summary>
        /// Gets the <see cref="HostProjectServices"/> for the language name.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown if the language isn't supported.</exception>
        public HostProjectServices GetProjectServices(string languageName)
            => _services.GetLanguageServices(languageName).ProjectServices;
    }
}
