// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// True if we are currently in an unsafe region (type, member, or block).
        /// </summary>
        /// <remarks>
        /// Does not imply that this compilation allows unsafe regions (could be in an error recovery scenario).
        /// To determine that, check this.Compilation.Options.AllowUnsafe.
        /// </remarks>
        internal bool InUnsafeRegion
        {
            get { return this.Flags.Includes(BinderFlags.UnsafeRegion); }
        }

        /// <returns>True if a diagnostic was reported</returns>
        internal bool ReportUnsafeIfNotAllowed(SyntaxNode node, BindingDiagnosticBag diagnostics, TypeSymbol sizeOfTypeOpt = null)
        {
            Debug.Assert((node.Kind() == SyntaxKind.SizeOfExpression) == ((object)sizeOfTypeOpt != null), "Should have a type for (only) sizeof expressions.");
            var diagnosticInfo = GetUnsafeDiagnosticInfo(sizeOfTypeOpt);
            if (diagnosticInfo == null)
            {
                return false;
            }

            diagnostics.Add(new CSDiagnostic(diagnosticInfo, node.Location));
            return true;
        }

        /// <returns>True if a diagnostic was reported</returns>
        internal bool ReportUnsafeIfNotAllowed(Location location, BindingDiagnosticBag diagnostics)
        {
            var diagnosticInfo = GetUnsafeDiagnosticInfo(sizeOfTypeOpt: null);
            if (diagnosticInfo == null)
            {
                return false;
            }

            diagnostics.Add(new CSDiagnostic(diagnosticInfo, location));
            return true;
        }

        private CSDiagnosticInfo GetUnsafeDiagnosticInfo(TypeSymbol sizeOfTypeOpt)
        {
            if (this.Flags.Includes(BinderFlags.SuppressUnsafeDiagnostics))
            {
                return null;
            }
            else if (!this.InUnsafeRegion)
            {
                return ((object)sizeOfTypeOpt == null)
                    ? new CSDiagnosticInfo(ErrorCode.ERR_UnsafeNeeded)
                    : new CSDiagnosticInfo(ErrorCode.ERR_SizeofUnsafe, sizeOfTypeOpt);
            }
            else if (this.IsIndirectlyInIterator && MessageID.IDS_FeatureRefUnsafeInIteratorAsync.GetFeatureAvailabilityDiagnosticInfo(Compilation) is { } unsafeInIteratorDiagnosticInfo)
            {
                // In C# 12 and below, iterators inherit the unsafe context from their containing scope (spec violation).
                // In C# 13+, iterators establish a safe context. When we're indirectly in an iterator (e.g., in a nested
                // local function) and the feature is not available (C# 12), we need to check if the unsafe context would
                // still exist in C# 13. If the containing member is a local function with an explicit unsafe modifier,
                // then the unsafe context would still exist in C# 13, so we allow it (return null). Otherwise, reporting
                // the language version error would be misleading because upgrading to C# 13 won't help - the iterator will
                // establish a safe context, and the code will need an explicit unsafe modifier. So we report ERR_UnsafeNeeded instead.
                if (this.ContainingMemberOrLambda is LocalFunctionSymbol localFunction && localFunction.IsUnsafe)
                {
                    // The local function has an explicit unsafe modifier, so it will still be in an unsafe region in C# 13.
                    // Allow the unsafe construct.
                    return null;
                }
                else
                {
                    // The unsafe context is inherited through the iterator, so upgrading to C# 13 won't help.
                    return ((object)sizeOfTypeOpt == null)
                        ? new CSDiagnosticInfo(ErrorCode.ERR_UnsafeNeeded)
                        : new CSDiagnosticInfo(ErrorCode.ERR_SizeofUnsafe, sizeOfTypeOpt);
                }
            }
            else
            {
                return null;
            }
        }
    }
}
