// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp;

internal partial class BoundTryStatement : BoundStatement
{
    private partial void Validate()
    {
#if DEBUG
        if (EndIsReachable == AsyncTryFinallyEndReachable.Ignored && FinallyBlockOpt != null)
        {
            Debug.Assert(!AsyncExceptionHandlerRewriter.AwaitsInFinally(this),
                "There are awaits in this finally block, reachability cannot be ignored! Ensure that valid reachability " +
                "is set for this try statement, and ensure that we have async tests for the case where the end of the " +
                "try is not reachable and the construct is in a Task<T> or ValueTask<T>-returning method, and runtime " +
                "async is enabled for the test case to ensure that we not branching to invalid locations.");
        }
#endif
    }
}
