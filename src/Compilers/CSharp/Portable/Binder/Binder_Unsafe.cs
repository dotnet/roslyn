// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        /// <returns>True if a diagnostic was reported, or would have been reported if not for
        /// the suppress flag.</returns>
        private bool ReportUnsafeIfNotAllowed(CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            return ReportUnsafeIfNotAllowed(node, null, diagnostics);
        }

        /// <returns>True if a diagnostic was reported, or would have been reported if not for
        /// the suppress flag.</returns>
        private bool ReportUnsafeIfNotAllowed(CSharpSyntaxNode node, TypeSymbol sizeOfTypeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert((node.Kind() == SyntaxKind.SizeOfExpression) == ((object)sizeOfTypeOpt != null), "Should have a type for (only) sizeof expressions.");
            return ReportUnsafeIfNotAllowed(node.Location, sizeOfTypeOpt, diagnostics);
        }

        /// <returns>True if a diagnostic was reported, or would have been reported if not for
        /// the suppress flag.</returns>
        internal bool ReportUnsafeIfNotAllowed(Location location, DiagnosticBag diagnostics)
        {
            return ReportUnsafeIfNotAllowed(location, sizeOfTypeOpt: null, diagnostics: diagnostics);
        }

        /// <returns>True if a diagnostic was reported, or would have been reported if not for
        /// the suppress flag.</returns>
        private bool ReportUnsafeIfNotAllowed(Location location, TypeSymbol sizeOfTypeOpt, DiagnosticBag diagnostics)
        {
            var diagnosticInfo = GetUnsafeDiagnosticInfo(sizeOfTypeOpt);
            if (diagnosticInfo == null || this.Flags.Includes(BinderFlags.SuppressUnsafeDiagnostics))
            {
                return false;
            }

            diagnostics.Add(new CSDiagnostic(diagnosticInfo, location));
            return true;
        }

        private CSDiagnosticInfo GetUnsafeDiagnosticInfo(TypeSymbol sizeOfTypeOpt)
        {
            if (this.IsIndirectlyInIterator)
            {
                // Spec 8.2: "An iterator block always defines a safe context, even when its declaration
                // is nested in an unsafe context."
                return new CSDiagnosticInfo(ErrorCode.ERR_IllegalInnerUnsafe);
            }
            else if (!this.InUnsafeRegion)
            {
                return ((object)sizeOfTypeOpt == null)
                    ? new CSDiagnosticInfo(ErrorCode.ERR_UnsafeNeeded)
                    : new CSDiagnosticInfo(ErrorCode.ERR_SizeofUnsafe, sizeOfTypeOpt);
            }
            else
            {
                return null;
            }
        }
    }
}
