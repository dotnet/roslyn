// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    [Export(typeof(ITemporaryStorageServiceInternal)), Shared]
    internal partial class TemporaryStorageServiceDispatcher : ITemporaryStorageServiceInternal
    {
        private readonly ITemporaryStorageServiceInternal _underlyingStorageService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TemporaryStorageServiceDispatcher(
            [Import(AllowDefault = true)] ITextFactoryService? textFactoryService,
            [Import(AllowDefault = true)] IWorkspaceThreadingService? workspaceThreadingService)
        {
            textFactoryService ??= TextFactoryService.Default;

            // MemoryMapped files which are used by the TemporaryStorageService are present in .NET Framework (including Mono)
            // and .NET Core Windows. For non-Windows .NET Core scenarios, we can return the TrivialTemporaryStorageService
            // until https://github.com/dotnet/runtime/issues/30878 is fixed.
#pragma warning disable CA1416 // Validate platform compatibility
            _underlyingStorageService = PlatformInformation.IsWindows || PlatformInformation.IsRunningOnMono
                ? new TemporaryStorageService(workspaceThreadingService, textFactoryService)
                : TrivialTemporaryStorageService.Instance;
#pragma warning restore CA1416 // Validate platform compatibility
        }

        public ITemporaryStreamStorageInternal CreateTemporaryStreamStorage()
            => _underlyingStorageService.CreateTemporaryStreamStorage();

        public ITemporaryTextStorageInternal CreateTemporaryTextStorage()
            => _underlyingStorageService.CreateTemporaryTextStorage();
    }
}
