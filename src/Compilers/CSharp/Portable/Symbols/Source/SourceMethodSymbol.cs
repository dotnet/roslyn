﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
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

        internal void ReportAsyncParameterErrors(DiagnosticBag diagnostics, Location location)
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_BadAsyncArgType, getLocation(parameter, location));
                }
                else if (parameter.Type.IsUnsafe())
                {
                    diagnostics.Add(ErrorCode.ERR_UnsafeAsyncArgType, getLocation(parameter, location));
                }
                else if (parameter.Type.IsRestrictedType())
                {
                    diagnostics.Add(ErrorCode.ERR_BadSpecialByRefLocal, getLocation(parameter, location), parameter.Type);
                }
            }

            static Location getLocation(ParameterSymbol parameter, Location location)
                => parameter.Locations.FirstOrDefault() ?? location;
        }
    }
}
