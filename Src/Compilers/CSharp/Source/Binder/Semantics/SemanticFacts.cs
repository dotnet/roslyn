// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Symbol
    {
        /// <summary>
        /// Checks if 'symbol' is accessible from within named type 'within'.  If 'symbol' is accessed off
        /// of an expression then 'throughTypeOpt' is the type of that expression. This is needed to
        /// properly do protected access checks.
        /// </summary>
        public static bool IsSymbolAccessible(
            Symbol symbol,
            NamedTypeSymbol within,
            NamedTypeSymbol throughTypeOpt = null)
        {
            if ((object)symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }

            if ((object)within == null)
            {
                throw new ArgumentNullException("within");
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return AccessCheck.IsSymbolAccessible(
                symbol,
                within,
                ref useSiteDiagnostics,
                throughTypeOpt);
        }

        /// <summary>
        /// Checks if 'symbol' is accessible from within assembly 'within'.  
        /// </summary>
        public static bool IsSymbolAccessible(
            Symbol symbol,
            AssemblySymbol within)
        {
            if ((object)symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }

            if ((object)within == null)
            {
                throw new ArgumentNullException("within");
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return AccessCheck.IsSymbolAccessible(symbol, within, ref useSiteDiagnostics);
        }

        private static readonly HashSet<string> supportedOperators =
            new HashSet<string>(StringComparer.Ordinal)
            {
                WellKnownMemberNames.AdditionOperatorName,
                WellKnownMemberNames.BitwiseAndOperatorName,
                WellKnownMemberNames.BitwiseOrOperatorName,
                WellKnownMemberNames.DecrementOperatorName,
                WellKnownMemberNames.DivisionOperatorName,
                WellKnownMemberNames.EqualityOperatorName,
                WellKnownMemberNames.ExclusiveOrOperatorName,
                WellKnownMemberNames.ExplicitConversionName,
                WellKnownMemberNames.FalseOperatorName,
                WellKnownMemberNames.GreaterThanOperatorName,
                WellKnownMemberNames.GreaterThanOrEqualOperatorName,
                WellKnownMemberNames.ImplicitConversionName,
                WellKnownMemberNames.IncrementOperatorName,
                WellKnownMemberNames.InequalityOperatorName,
                WellKnownMemberNames.LeftShiftOperatorName,
                WellKnownMemberNames.LessThanOperatorName,
                WellKnownMemberNames.LessThanOrEqualOperatorName,
                WellKnownMemberNames.LogicalNotOperatorName,
                WellKnownMemberNames.ModulusOperatorName,
                WellKnownMemberNames.MultiplyOperatorName,
                WellKnownMemberNames.OnesComplementOperatorName,
                WellKnownMemberNames.RightShiftOperatorName,
                WellKnownMemberNames.SubtractionOperatorName,
                WellKnownMemberNames.TrueOperatorName,
                WellKnownMemberNames.UnaryNegationOperatorName,
                WellKnownMemberNames.UnaryPlusOperatorName
            };

        public static bool IsSupportedOperatorName(string name)
        {
            return supportedOperators.Contains(name);
        }
    }
}
