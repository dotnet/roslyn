// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal interface IActiveStatementTrackingController
{
    void StartTracking(Solution solution, IActiveStatementSpanFactory spanProvider);

    void EndTracking();

    ActiveStatementSpanProvider GetSpanProvider(Solution solution);
}
