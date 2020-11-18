// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Caravela.Compiler;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A diagnostic, along with the location where it occurred.
    /// </summary>
    internal sealed class CSDiagnostic : DiagnosticWithInfo
    {
        static DiagnosticInfo GetPreTransformationInfo(DiagnosticInfo info)
        {
            for (int i = 0; i < info.Arguments.Length; i++)
            {
                if (info.Arguments[i] is SyntaxNode node)
                    info.Arguments[i] = TreeTracker.GetPreTransformationSyntax(node);
            }

            return info;
        }

        internal CSDiagnostic(DiagnosticInfo info, Location location, bool isSuppressed = false)
            : base(GetPreTransformationInfo(info), location, isSuppressed)
        {
        }

        public override string ToString()
        {
            return CSharpDiagnosticFormatter.Instance.Format(this);
        }

        internal override Diagnostic WithLocation(Location location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (location != this.Location)
            {
                return new CSDiagnostic(this.Info, location, this.IsSuppressed);
            }

            return this;
        }

        internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
        {
            if (this.Severity != severity)
            {
                return new CSDiagnostic(this.Info.GetInstanceWithSeverity(severity), this.Location, this.IsSuppressed);
            }

            return this;
        }

        internal override Diagnostic WithIsSuppressed(bool isSuppressed)
        {
            if (this.IsSuppressed != isSuppressed)
            {
                return new CSDiagnostic(this.Info, this.Location, isSuppressed);
            }

            return this;
        }
    }
}
