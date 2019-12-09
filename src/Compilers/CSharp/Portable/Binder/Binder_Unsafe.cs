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

        /// <returns>True if a diagnostic was reported</returns>
        internal bool ReportUnsafeIfNotAllowed(SyntaxNode node, DiagnosticBag diagnostics, TypeSymbol sizeOfTypeOpt = null)
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
        internal bool ReportUnsafeIfNotAllowed(Location location, DiagnosticBag diagnostics)
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
            else if (this.IsIndirectlyInIterator)
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
