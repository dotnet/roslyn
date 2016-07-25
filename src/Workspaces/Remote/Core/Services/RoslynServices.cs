// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Various Roslyn services provider.
    /// 
    /// TODO: change all these services to WorkspaceServices
    /// </summary>
    internal static class RoslynServices
    {
        // TODO: probably need to split this to private and public services
        public static readonly HostServices HostServices = MefHostServices.Create(
            MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

        public static readonly AssetService AssetService = new AssetService();
        public static readonly SolutionService SolutionService = new SolutionService();
        public static readonly CompilationService CompilationService = new CompilationService();
    }
}
