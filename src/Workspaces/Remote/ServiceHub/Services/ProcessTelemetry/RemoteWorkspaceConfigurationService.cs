// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceConfigurationService)), Shared]
    internal sealed class RemoteWorkspaceConfigurationService : IWorkspaceConfigurationService
    {
        private WorkspaceConfigurationOptions? _options;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteWorkspaceConfigurationService()
        {
        }

        /// <summary>
        /// Returns default values until the options are initialized.
        /// </summary>
        public WorkspaceConfigurationOptions Options
            => _options ?? WorkspaceConfigurationOptions.RemoteDefault;

        public void InitializeOptions(WorkspaceConfigurationOptions options)
        {
            // can only be set once:
            Contract.ThrowIfFalse(_options == null);

            _options = options;
        }
    }
}
