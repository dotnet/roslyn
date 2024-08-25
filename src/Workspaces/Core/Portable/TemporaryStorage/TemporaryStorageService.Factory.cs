// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal partial class TemporaryStorageService
{
    [ExportWorkspaceServiceFactory(typeof(ITemporaryStorageServiceInternal), ServiceLayer.Default), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal partial class Factory(
        [Import(AllowDefault = true)] IWorkspaceThreadingService? workspaceThreadingService) : IWorkspaceServiceFactory
    {
        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // Only use the memory mapped file version of the temporary storage service on Windows.
            // It is only required for OOP communication (Windows only) and can cause issues on Linux containers
            // due to a small amount of space allocated by default to store memory mapped files.
            if (PlatformInformation.IsWindows || PlatformInformation.IsRunningOnMono)
            {
                var textFactory = workspaceServices.GetRequiredService<ITextFactoryService>();
                return new TemporaryStorageService(workspaceThreadingService, textFactory);
            }
            else
            {
                return TrivialTemporaryStorageService.Instance;
            }
        }
    }
}
