﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class to represent all source method-like symbols. This includes
    /// things like ordinary methods and constructors, and functions
    /// like lambdas and local functions.
    /// </summary>
    internal abstract class SourceMethodSymbol : MethodSymbol
    {
        /// <summary>
        /// If there are no constraints, returns an empty immutable array. Otherwise, returns an immutable
        /// array of clauses, indexed by the constrained type parameter in <see cref="MethodSymbol.TypeParameters"/>.
        /// If a type parameter does not have constraints, the corresponding entry in the array is null.
        /// </summary>
        public abstract ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses();

        protected static void ReportBadRefToken(TypeSyntax returnTypeSyntax, DiagnosticBag diagnostics)
        {
            if (!returnTypeSyntax.HasErrors)
            {
                var refKeyword = returnTypeSyntax.GetFirstToken();
                diagnostics.Add(ErrorCode.ERR_UnexpectedToken, refKeyword.GetLocation(), refKeyword.ToString());
            }
        }
    }
}
