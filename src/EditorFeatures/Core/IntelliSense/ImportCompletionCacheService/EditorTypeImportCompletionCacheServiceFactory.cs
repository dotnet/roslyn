// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.IntelliSense
{
    // This is the implementation at Editor layer to provide a CancellationToken
    // for the workqueue used for background cache refresh.
    [ExportWorkspaceServiceFactory(typeof(IImportCompletionCacheService<TypeImportCompletionCacheEntry, TypeImportCompletionCacheEntry>), ServiceLayer.Editor), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class EditorTypeImportCompletionCacheServiceFactory(IAsynchronousOperationListenerProvider listenerProvider, IThreadingContext threadingContext)
                : AbstractImportCompletionCacheServiceFactory<TypeImportCompletionCacheEntry, TypeImportCompletionCacheEntry>(listenerProvider, AbstractTypeImportCompletionService.BatchUpdateCacheAsync, threadingContext.DisposalToken)
    {
    }
}
