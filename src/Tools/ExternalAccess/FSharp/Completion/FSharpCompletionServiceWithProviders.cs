// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    internal abstract class FSharpCompletionServiceWithProviders : CompletionService
    {
        internal FSharpCompletionServiceWithProviders(Workspace workspace)
            : base(workspace.Services.SolutionServices)
        {
        }

        internal sealed override CompletionRules GetRules(CompletionOptions options)
            => GetRulesImpl();

        internal abstract CompletionRules GetRulesImpl();
    }
}
