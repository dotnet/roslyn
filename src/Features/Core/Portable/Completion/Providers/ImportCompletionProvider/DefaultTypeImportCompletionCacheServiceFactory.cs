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
    [ExportWorkspaceServiceFactory(typeof(IImportCompletionCacheService<TypeImportCompletionCacheEntry, TypeImportCompletionCacheEntry>), ServiceLayer.Default), Shared]
    internal sealed class DefaultTypeImportCompletionCacheServiceFactory
        : AbstractImportCompletionCacheServiceFactory<TypeImportCompletionCacheEntry, TypeImportCompletionCacheEntry>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultTypeImportCompletionCacheServiceFactory(IAsynchronousOperationListenerProvider listenerProvider)
            : base(listenerProvider, AbstractTypeImportCompletionService.BatchUpdateCacheAsync, CancellationToken.None)
        {
        }
    }
}
