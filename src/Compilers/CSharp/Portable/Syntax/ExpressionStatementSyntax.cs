﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ExpressionStatementSyntax
    {
        /// <summary>
        /// Returns true if the <see cref="Expression"/> property is allowed by the rules of the
        /// language to be an arbitrary expression, not just a statement expression.
        /// </summary>
        /// <remarks>
        /// True if, for example, this expression statement represents the last expression statement
        /// of the interactive top-level code.
        /// </remarks>
        public bool AllowsAnyExpression
        {
            get
            {
                var semicolon = SemicolonToken;
                return semicolon.IsMissing && !semicolon.ContainsDiagnostics;
            }
        }
    }
}
