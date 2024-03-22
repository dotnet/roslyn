// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp;

partial class BoundLoweredIsPatternExpression
{
    private partial void Validate()
    {
        // Ensure fall-through is unreachable
        Debug.Assert(this.Statements is [.., BoundGotoStatement or BoundSwitchDispatch]);
    }
}
