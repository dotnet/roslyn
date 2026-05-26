// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[Shared]
[Export(typeof(IActiveStatementTrackingController))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VoidActiveStatementTrackingController() : IActiveStatementTrackingController
{
    private static readonly ActiveStatementSpanProvider s_emptyActiveStatementProvider = async (_, _, _) => [];

    public void StartTracking(Solution solution, IActiveStatementSpanFactory spanProvider)
    {
    }

    public ActiveStatementSpanProvider GetSpanProvider(Solution solution)
        => s_emptyActiveStatementProvider;

    public void EndTracking()
    {
    }
}
