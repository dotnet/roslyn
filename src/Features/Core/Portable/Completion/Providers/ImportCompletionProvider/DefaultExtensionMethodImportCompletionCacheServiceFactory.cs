// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    /// <summary>
    /// We don't use PE cache from the service, so just pass in type `object` for PE entries.
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IImportCompletionCacheService<ExtensionMethodImportCompletionCacheEntry, object>), ServiceLayer.Default), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class DefaultExtensionMethodImportCompletionCacheServiceFactory(IAsynchronousOperationListenerProvider listenerProvider)
                : AbstractImportCompletionCacheServiceFactory<ExtensionMethodImportCompletionCacheEntry, object>(listenerProvider, ExtensionMethodImportCompletionHelper.BatchUpdateCacheAsync, CancellationToken.None)
    {
    }
}
