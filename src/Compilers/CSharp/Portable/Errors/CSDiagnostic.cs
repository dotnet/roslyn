// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A diagnostic, along with the location where it occurred.
    /// </summary>
    internal sealed class CSDiagnostic : DiagnosticWithInfo
    {
        internal CSDiagnostic(DiagnosticInfo info, Location location, bool hasSourceSuppression = false)
            : base(info, location, hasSourceSuppression)
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
                return new CSDiagnostic(this.Info, location, this.HasSourceSuppression);
            }

            return this;
        }

        internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
        {
            if (this.Severity != severity)
            {
                return new CSDiagnostic(this.Info.GetInstanceWithSeverity(severity), this.Location, this.HasSourceSuppression);
            }

            return this;
        }

        internal override Diagnostic WithHasSourceSuppression(bool hasSourceSuppression)
        {
            if (this.HasSourceSuppression != hasSourceSuppression)
            {
                return new CSDiagnostic(this.Info, this.Location, hasSourceSuppression);
            }

            return this;
        }
    }
}
