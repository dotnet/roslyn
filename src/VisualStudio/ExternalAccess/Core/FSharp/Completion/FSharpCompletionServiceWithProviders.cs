// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.TestHooks;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Completion;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion;
#endif

internal abstract class FSharpCompletionServiceWithProviders : CompletionService
{
    // Pass in NullProvider since it's only used for testing project reference based CompletionProvider,
    // which F# does not need.
    internal FSharpCompletionServiceWithProviders(Workspace workspace)
        : base(workspace.Services.SolutionServices, AsynchronousOperationListenerProvider.NullProvider)
    {
    }

    internal sealed override CompletionRules GetRules(CompletionOptions options)
        => GetRulesImpl();

    internal abstract CompletionRules GetRulesImpl();
}
