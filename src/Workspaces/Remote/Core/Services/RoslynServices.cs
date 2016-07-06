// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote
{
    // TODO: currently, service hub provide no other way to share services between user service hub services.
    //       only way to do so is using static type
    internal static class RoslynServices
    {
        public static readonly HostServices HostServices = MefHostServices.Create(
            MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

        public static readonly AssetService AssetService = new AssetService();
        public static readonly SolutionService SolutionService = new SolutionService();
        public static readonly CompilationService CompilationService = new CompilationService();
    }
}
