// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.IntelliCode.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.IntelliCode;

[Export(typeof(IIntentSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class IntentSourceProvider() : IIntentSourceProvider
{
    public Task<ImmutableArray<IntentSource>> ComputeIntentsAsync(IntentRequestContext intentRequestContext, CancellationToken cancellationToken)
        => SpecializedTasks.EmptyImmutableArray<IntentSource>();
}
