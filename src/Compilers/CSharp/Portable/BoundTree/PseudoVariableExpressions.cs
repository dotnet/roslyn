// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// BoundExpressions to be used for emit. The expressions are assumed
    /// to be lowered and will not be visited by <see cref="BoundTreeWalker"/>.
    /// </summary>
    internal abstract class PseudoVariableExpressions
    {
        internal abstract BoundExpression GetValue(BoundPseudoVariable variable, DiagnosticBag diagnostics);
        internal abstract BoundExpression GetAddress(BoundPseudoVariable variable);
    }
}
