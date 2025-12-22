// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private void ReportDiagnosticsIfUnsafeMemberAccess(BindingDiagnosticBag diagnostics, Symbol symbol, SyntaxNode node)
        {
            var callerUnsafeMode = symbol.CallerUnsafeMode;
            if (callerUnsafeMode != CallerUnsafeMode.None)
            {
                ReportUnsafeIfNotAllowed(node, diagnostics, disallowedUnder: MemorySafetyRules.Updated,
                    customErrorCode: callerUnsafeMode switch
                    {
                        CallerUnsafeMode.Explicit or CallerUnsafeMode.Extern => ErrorCode.ERR_UnsafeMemberOperation,
                        CallerUnsafeMode.Implicit => ErrorCode.ERR_UnsafeMemberOperationCompat,
                        _ => throw ExceptionUtilities.UnexpectedValue(callerUnsafeMode),
                    },
                    customArgs: [symbol]);
            }
        }

        /// <param name="disallowedUnder">
        /// Memory safety rules which the current location is disallowed under.
        /// PROTOTYPE: Consider removing the default parameter value.
        /// </param>
        /// <returns>True if a diagnostic was reported</returns>
        internal bool ReportUnsafeIfNotAllowed(
            SyntaxNode node,
            BindingDiagnosticBag diagnostics,
            TypeSymbol? sizeOfTypeOpt = null,
            MemorySafetyRules disallowedUnder = MemorySafetyRules.Legacy,
            ErrorCode? customErrorCode = null,
            object[]? customArgs = null)
        {
            Debug.Assert((node.Kind() == SyntaxKind.SizeOfExpression) == ((object?)sizeOfTypeOpt != null), "Should have a type for (only) sizeof expressions.");
            var diagnosticInfo = GetUnsafeDiagnosticInfo(sizeOfTypeOpt, disallowedUnder, customErrorCode, customArgs);
            if (diagnosticInfo == null)
            {
                return false;
            }

            diagnostics.Add(new CSDiagnostic(diagnosticInfo, node.Location));
            return true;
        }

        /// <inheritdoc cref="ReportUnsafeIfNotAllowed(SyntaxNode, BindingDiagnosticBag, TypeSymbol?, MemorySafetyRules, ErrorCode?, object[])"/>
        internal bool ReportUnsafeIfNotAllowed(Location location, BindingDiagnosticBag diagnostics, MemorySafetyRules disallowedUnder = MemorySafetyRules.Legacy)
        {
            var diagnosticInfo = GetUnsafeDiagnosticInfo(sizeOfTypeOpt: null, disallowedUnder);
            if (diagnosticInfo == null)
            {
                return false;
            }

            diagnostics.Add(new CSDiagnostic(diagnosticInfo, location));
            return true;
        }

        private CSDiagnosticInfo? GetUnsafeDiagnosticInfo(
            TypeSymbol? sizeOfTypeOpt,
            MemorySafetyRules disallowedUnder = MemorySafetyRules.Legacy,
            ErrorCode? customErrorCode = null,
            object[]? customArgs = null)
        {
            Debug.Assert(sizeOfTypeOpt is null || disallowedUnder is MemorySafetyRules.Legacy);

            if (this.Flags.Includes(BinderFlags.SuppressUnsafeDiagnostics))
            {
                return null;
            }
            else if (!this.InUnsafeRegion)
            {
                if (disallowedUnder is MemorySafetyRules.Legacy)
                {
                    Debug.Assert(customErrorCode is null && customArgs is null);

                    if (this.Compilation.SourceModule.UseUpdatedMemorySafetyRules)
                    {
                        return MessageID.IDS_FeatureUnsafeEvolution.GetFeatureAvailabilityDiagnosticInfo(this.Compilation);
                    }

                    // PROTOTYPE: Update the error message to hint that one can enable updated memory safety rules.
                    return ((object?)sizeOfTypeOpt == null)
                        ? new CSDiagnosticInfo(ErrorCode.ERR_UnsafeNeeded)
                        : new CSDiagnosticInfo(ErrorCode.ERR_SizeofUnsafe, sizeOfTypeOpt);
                }

                Debug.Assert(disallowedUnder is MemorySafetyRules.Updated);

                if (this.Compilation.SourceModule.UseUpdatedMemorySafetyRules)
                {
                    return MessageID.IDS_FeatureUnsafeEvolution.GetFeatureAvailabilityDiagnosticInfo(this.Compilation)
                        ?? new CSDiagnosticInfo(customErrorCode ?? ErrorCode.ERR_UnsafeOperation, customArgs ?? []);
                }

                // This location is disallowed only under updated memory safety rules which are not enabled.
                // We report an error elsewhere, usually at the pointer type itself
                // (where we are called with `disallowedUnder: MemorySafetyRules.Legacy`).
                return null;
            }
            else if (this.IsIndirectlyInIterator && MessageID.IDS_FeatureRefUnsafeInIteratorAsync.GetFeatureAvailabilityDiagnosticInfo(Compilation) is { } unsafeInIteratorDiagnosticInfo)
            {
                if (disallowedUnder is MemorySafetyRules.Legacy)
                {
                    return unsafeInIteratorDiagnosticInfo;
                }

                // This location is disallowed only under updated memory safety rules.
                // We report the RefUnsafeInIteratorAsync langversion error elsewhere, usually at the pointer type itself
                // (where we are called with `disallowedUnder: MemorySafetyRules.Legacy`).
                Debug.Assert(disallowedUnder is MemorySafetyRules.Updated);
                return null;
            }
            else
            {
                return null;
            }
        }
    }

    internal enum MemorySafetyRules
    {
        Legacy,

        /// <summary>
        /// <see cref="CSharpCompilationOptions.UseUpdatedMemorySafetyRules"/>
        /// </summary>
        Updated,
    }
}
