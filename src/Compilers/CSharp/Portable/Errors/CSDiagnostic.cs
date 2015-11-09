// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A diagnostic, along with the location where it occurred.
    /// </summary>
    internal sealed class CSDiagnostic : DiagnosticWithInfo
    {
        internal CSDiagnostic(DiagnosticInfo info, Location location, bool isSuppressed = false)
            : base(info, location, isSuppressed)
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
