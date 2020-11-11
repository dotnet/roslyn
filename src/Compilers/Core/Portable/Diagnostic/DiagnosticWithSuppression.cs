// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class Diagnostic
    {
        private sealed class DiagnosticWithSuppression : Diagnostic
        {
            private readonly Diagnostic _originalUnsuppressedDiagnostic;
            private readonly SuppressionInfo _suppressionInfo;

            public DiagnosticWithSuppression(
                Diagnostic originalUnsuppressedDiagnostic,
                SuppressionInfo suppressionInfo)
            {
                RoslynDebug.Assert(!originalUnsuppressedDiagnostic.IsSuppressed);
                RoslynDebug.Assert(originalUnsuppressedDiagnostic.SuppressionInfo == null);
                RoslynDebug.Assert(suppressionInfo != null);

                _originalUnsuppressedDiagnostic = originalUnsuppressedDiagnostic;
                _suppressionInfo = suppressionInfo;
            }

            public override DiagnosticDescriptor Descriptor
            {
                get { return _originalUnsuppressedDiagnostic.Descriptor; }
            }

            public override string Id
            {
                get { return Descriptor.Id; }
            }

            public override string GetMessage(IFormatProvider? formatProvider = null)
                => _originalUnsuppressedDiagnostic.GetMessage(formatProvider);

            internal override IReadOnlyList<object?> Arguments
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

            internal override SuppressionInfo SuppressionInfo
            {
                get { return _suppressionInfo; }
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

            public override ImmutableDictionary<string, string?> Properties
            {
                get { return _originalUnsuppressedDiagnostic.Properties; }
            }

            public override bool Equals(Diagnostic? obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                var other = obj as DiagnosticWithSuppression;
                if (other == null)
                {
                    return false;
                }

                return Equals(_originalUnsuppressedDiagnostic, other._originalUnsuppressedDiagnostic) &&
                    Equals(_suppressionInfo, other._suppressionInfo);
            }

            public override bool Equals(object? obj)
            {
                return this.Equals(obj as Diagnostic);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_originalUnsuppressedDiagnostic.GetHashCode(), _suppressionInfo.GetHashCode());
            }

            internal override Diagnostic WithLocation(Location location)
            {
                if (location == null)
                {
                    throw new ArgumentNullException(nameof(location));
                }

                if (this.Location != location)
                {
                    return new DiagnosticWithSuppression(_originalUnsuppressedDiagnostic.WithLocation(location), _suppressionInfo);
                }

                return this;
            }

            internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                if (this.Severity != severity)
                {
                    return new DiagnosticWithSuppression(_originalUnsuppressedDiagnostic.WithSeverity(severity), _suppressionInfo);
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
