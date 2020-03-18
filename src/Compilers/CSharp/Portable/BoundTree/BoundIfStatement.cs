// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundIfStatement : IBoundConditional
    {
        BoundNode IBoundConditional.Condition => this.Condition;

        BoundNode IBoundConditional.Consequence => this.Consequence;

        BoundNode? IBoundConditional.AlternativeOpt => this.AlternativeOpt;
    }
}
