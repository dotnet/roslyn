// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(IWorkspaceConfigurationService)), Shared]
    internal sealed class VisualStudioWorkspaceConfigurationService : IWorkspaceConfigurationService
    {
        private readonly IGlobalOptionService _globalOptions;
        private WorkspaceConfigurationOptions? _lazyOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceConfigurationService(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public WorkspaceConfigurationOptions Options
            => _lazyOptions ??= _globalOptions.GetWorkspaceConfigurationOptions();
    }
}
