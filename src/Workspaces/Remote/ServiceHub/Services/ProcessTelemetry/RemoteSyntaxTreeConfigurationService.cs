// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(ISyntaxTreeConfigurationService)), Shared]
    internal sealed class RemoteSyntaxTreeConfigurationService : ISyntaxTreeConfigurationService
    {
        public bool DisableRecoverableTrees { get; private set; }
        public bool DisableProjectCacheService { get; private set; }
        public bool EnableOpeningSourceGeneratedFilesInWorkspace { get; private set; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteSyntaxTreeConfigurationService()
        {
        }

        internal void SetOptions(bool disableRecoverableTrees, bool disableProjectCacheService, bool enableOpeningSourceGeneratedFilesInWorkspace)
        {
            DisableRecoverableTrees = disableRecoverableTrees;
            DisableProjectCacheService = disableProjectCacheService;
            EnableOpeningSourceGeneratedFilesInWorkspace = enableOpeningSourceGeneratedFilesInWorkspace;
        }
    }
}
