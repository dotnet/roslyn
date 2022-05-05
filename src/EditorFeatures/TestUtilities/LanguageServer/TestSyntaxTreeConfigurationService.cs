// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Roslyn.Test.Utilities
{
    [Export]
    [ExportWorkspaceService(typeof(ISyntaxTreeConfigurationService), ServiceLayer.Test)]
    [Shared]
    [PartNotDiscoverable]
    internal sealed class TestSyntaxTreeConfigurationService : ISyntaxTreeConfigurationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestSyntaxTreeConfigurationService()
        {
        }

        public bool DisableRecoverableTrees { get; set; }

        public bool DisableProjectCacheService { get; set; }

        public bool EnableOpeningSourceGeneratedFilesInWorkspace { get; set; }
    }
}
