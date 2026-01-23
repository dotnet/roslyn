// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.Apex;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Apex;
#endif

[Export(typeof(IApexAsynchronousOperationListenerProviderAccessor))]
[Shared]
internal sealed class ApexAsynchronousOperationListenerProviderAccessor : IApexAsynchronousOperationListenerProviderAccessor
{
    private readonly AsynchronousOperationListenerProvider _implementation;
    private readonly Workspace? _workspace;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ApexAsynchronousOperationListenerProviderAccessor(
        AsynchronousOperationListenerProvider implementation,
        [Import(AllowDefault = true)] VisualStudioWorkspace? workspace)
    {
        _implementation = implementation;
        _workspace = workspace;
    }

    public Task WaitAllAsync(string[]? featureNames = null, Action? eventProcessingAction = null, TimeSpan? timeout = null)
        => _implementation.WaitAllAsync(_workspace, featureNames, eventProcessingAction, timeout);
}
