// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class Diagnostic
    {
        private sealed class DiagnosticWithProgrammaticSuppression : Diagnostic
        {
            private readonly Diagnostic _originalUnsuppressedDiagnostic;
            private readonly ProgrammaticSuppressionInfo _programmaticSuppressionInfo;

            public DiagnosticWithProgrammaticSuppression(
                Diagnostic originalUnsuppressedDiagnostic,
                ProgrammaticSuppressionInfo programmaticSuppressionInfo)
            {
                Debug.Assert(!originalUnsuppressedDiagnostic.IsSuppressed);
                Debug.Assert(originalUnsuppressedDiagnostic.ProgrammaticSuppressionInfo == null);
                Debug.Assert(programmaticSuppressionInfo != null);

                _originalUnsuppressedDiagnostic = originalUnsuppressedDiagnostic;
                _programmaticSuppressionInfo = programmaticSuppressionInfo;
            }

            public override DiagnosticDescriptor Descriptor
            {
                get { return _originalUnsuppressedDiagnostic.Descriptor; }
            }

            public override string Id
            {
                get { return Descriptor.Id; }
            }

            public override string GetMessage(IFormatProvider formatProvider = null)
                => _originalUnsuppressedDiagnostic.GetMessage(formatProvider);

            internal override IReadOnlyList<object> Arguments
            {
                get { return _originalUnsuppressedDiagnostic.Arguments; }
            }

            public override DiagnosticSeverity Severity
            {
                get { return _originalUnsuppressedDiagnostic.Severity; }
            }

            public override bool IsSuppressed
            {
                get { return true; }
            }

            internal override ProgrammaticSuppressionInfo ProgrammaticSuppressionInfo
            {
                get { return _programmaticSuppressionInfo; }
            }

            public override int WarningLevel
            {
                get { return _originalUnsuppressedDiagnostic.WarningLevel; }
            }

            public override Location Location
            {
                get { return _originalUnsuppressedDiagnostic.Location; }
            }

            public override IReadOnlyList<Location> AdditionalLocations
            {
                get { return _originalUnsuppressedDiagnostic.AdditionalLocations; }
            }

            public override ImmutableDictionary<string, string> Properties
            {
                get { return _originalUnsuppressedDiagnostic.Properties; }
            }

            public override bool Equals(Diagnostic obj)
            {
                var other = obj as DiagnosticWithProgrammaticSuppression;
                if (other == null)
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return Equals(_originalUnsuppressedDiagnostic, other._originalUnsuppressedDiagnostic) &&
                    Equals(_programmaticSuppressionInfo, other._programmaticSuppressionInfo);
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as Diagnostic);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_originalUnsuppressedDiagnostic.GetHashCode(), _programmaticSuppressionInfo.GetHashCode());
            }

            internal override Diagnostic WithLocation(Location location)
            {
                if (location == null)
                {
                    throw new ArgumentNullException(nameof(location));
                }

                if (this.Location != location)
                {
                    return new DiagnosticWithProgrammaticSuppression(_originalUnsuppressedDiagnostic.WithLocation(location), _programmaticSuppressionInfo);
                }

                return this;
            }

            internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                if (this.Severity != severity)
                {
                    return new DiagnosticWithProgrammaticSuppression(_originalUnsuppressedDiagnostic.WithSeverity(severity), _programmaticSuppressionInfo);
                }

                return this;
            }

            internal override Diagnostic WithIsSuppressed(bool isSuppressed)
            {
                // We do not support toggling suppressed diagnostic to unsuppressed.
                if (!isSuppressed)
                {
                    throw new ArgumentException(nameof(isSuppressed));
                }

                return this;
            }
        }
    }
}
