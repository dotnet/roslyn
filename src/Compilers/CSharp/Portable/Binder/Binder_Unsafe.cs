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

        /// <param name="disallowedUnderAtLeast">
        /// Minimum memory safety rules which the current location is disallowed under.
        /// PROTOTYPE: Consider removing the default parameter value.
        /// </param>
        /// <returns>True if a diagnostic was reported</returns>
        internal bool ReportUnsafeIfNotAllowed(SyntaxNode node, BindingDiagnosticBag diagnostics, TypeSymbol? sizeOfTypeOpt = null, MemorySafetyRules disallowedUnderAtLeast = MemorySafetyRules.Legacy)
        {
            Debug.Assert((node.Kind() == SyntaxKind.SizeOfExpression) == ((object?)sizeOfTypeOpt != null), "Should have a type for (only) sizeof expressions.");
            var diagnosticInfo = GetUnsafeDiagnosticInfo(sizeOfTypeOpt, disallowedUnderAtLeast);
            if (diagnosticInfo == null)
            {
                return false;
            }

            diagnostics.Add(new CSDiagnostic(diagnosticInfo, node.Location));
            return true;
        }

        /// <inheritdoc cref="ReportUnsafeIfNotAllowed(SyntaxNode, BindingDiagnosticBag, TypeSymbol?, MemorySafetyRules)"/>
        internal bool ReportUnsafeIfNotAllowed(Location location, BindingDiagnosticBag diagnostics, MemorySafetyRules disallowedUnderAtLeast = MemorySafetyRules.Legacy)
        {
            var diagnosticInfo = GetUnsafeDiagnosticInfo(sizeOfTypeOpt: null, disallowedUnderAtLeast);
            if (diagnosticInfo == null)
            {
                return false;
            }

            diagnostics.Add(new CSDiagnostic(diagnosticInfo, location));
            return true;
        }

        private CSDiagnosticInfo? GetUnsafeDiagnosticInfo(TypeSymbol? sizeOfTypeOpt, MemorySafetyRules disallowedUnderAtLeast = MemorySafetyRules.Legacy)
        {
            if (this.Flags.Includes(BinderFlags.SuppressUnsafeDiagnostics))
            {
                return null;
            }
            else if (!this.InUnsafeRegion)
            {
                if (disallowedUnderAtLeast is MemorySafetyRules.Legacy)
                {
                    if (this.Compilation.Options.HasEvolvedMemorySafetyRules)
                    {
                        return MessageID.IDS_FeatureUnsafeEvolution.GetFeatureAvailabilityDiagnosticInfo(this.Compilation);
                    }

                    // PROTOTYPE: Update the error message to hint that one can enable evolved memory safety rules.
                    return ((object?)sizeOfTypeOpt == null)
                        ? new CSDiagnosticInfo(ErrorCode.ERR_UnsafeNeeded)
                        : new CSDiagnosticInfo(ErrorCode.ERR_SizeofUnsafe, sizeOfTypeOpt);
                }

                Debug.Assert(disallowedUnderAtLeast is MemorySafetyRules.Evolved);

                if (this.Compilation.Options.HasEvolvedMemorySafetyRules)
                {
                    // PROTOTYPE: Handle ERR_SizeofUnsafe.
                    return new CSDiagnosticInfo(ErrorCode.ERR_UnsafeOperation);
                }

                // This location is disallowed only under evolved memory safety rules which are not enabled.
                // We report an error elsewhere, usually at the pointer type itself
                // (where we are called with `disallowedUnderAtLeast: MemorySafetyRules.Legacy`).
                return null;
            }
            else if (this.IsIndirectlyInIterator && MessageID.IDS_FeatureRefUnsafeInIteratorAsync.GetFeatureAvailabilityDiagnosticInfo(Compilation) is { } unsafeInIteratorDiagnosticInfo)
            {
                if (disallowedUnderAtLeast is MemorySafetyRules.Legacy)
                {
                    return unsafeInIteratorDiagnosticInfo;
                }

                // This location is disallowed only under evolved memory safety rules.
                // We report the RefUnsafeInIteratorAsync langversion error elsewhere, usually at the pointer type itself
                // (where we are called with `disallowedUnderAtLeast: MemorySafetyRules.Legacy`).
                Debug.Assert(disallowedUnderAtLeast is MemorySafetyRules.Evolved);
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
        /// <see cref="CSharpCompilationOptions.HasEvolvedMemorySafetyRules"/>
        /// </summary>
        Evolved,
    }
}
