// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes the kind of binding to be performed in one of the SemanticModel
    /// speculative binding methods.
    /// </summary>
    public enum SpeculativeBindingOption
    {
        /// <summary>
        /// Binds the given expression using the normal expression binding rules
        /// that would occur during normal binding of expressions.
        /// </summary>
        BindAsExpression = 0,

        /// <summary>
        /// Binds the given expression as a type or namespace only. If this option
        /// is selected, then the given expression must derive from TypeSyntax.
        /// </summary>
        BindAsTypeOrNamespace = 1
    }
}
