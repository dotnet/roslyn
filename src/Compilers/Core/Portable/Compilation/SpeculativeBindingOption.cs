// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
