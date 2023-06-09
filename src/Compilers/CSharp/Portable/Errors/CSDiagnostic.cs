﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Metalama.Compiler;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A diagnostic, along with the location where it occurred.
    /// </summary>
    internal sealed class CSDiagnostic : DiagnosticWithInfo
    {
        // <Metalama>
        static DiagnosticInfo GetSourceDiagnosticInfo(DiagnosticInfo info)
        {
            for (int i = 0; i < info.Arguments.Length; i++)
            {
                if (info.Arguments[i] is SyntaxNode node)
                    info.Arguments[i] = TreeTracker.GetSourceSyntaxNode(node);
            }

            return info;
        }
        // </Metalama>

        internal CSDiagnostic(DiagnosticInfo info, Location location, bool isSuppressed = false)
            // <Metalama> - Call GetPreTransformationInfo
            : base(GetSourceDiagnosticInfo(info), location, isSuppressed)
            // </Metalama>
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
            var info = this.Info.GetInstanceWithSeverity(severity);
            if (info != this.Info)
            {
                return new CSDiagnostic(info, this.Location, this.IsSuppressed);
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
