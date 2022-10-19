// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Enables legacy APIs to access global options from workspace.
    /// </summary>
    [ExportWorkspaceService(typeof(ILegacyGlobalOptionsWorkspaceService)), Shared]
    internal sealed class LegacyGlobalOptionsWorkspaceService : ILegacyGlobalOptionsWorkspaceService
    {
        public IGlobalOptionService GlobalOptions { get; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyGlobalOptionsWorkspaceService(IGlobalOptionService globalOptions)
        {
            GlobalOptions = globalOptions;
        }
    }
}
