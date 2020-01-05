// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        /// <summary>
        /// Special informational diagnostic for each programmatic <see cref="Diagnostics.Suppression"/> reported by a <see cref="Diagnostics.DiagnosticSuppressor"/>.
        /// </summary>
        private sealed class SuppressionDiagnostic : Diagnostic
        {
            private static readonly DiagnosticDescriptor s_suppressionDiagnosticDescriptor = new DiagnosticDescriptor(
                "SP0001",
                CodeAnalysisResources.SuppressionDiagnosticDescriptorTitle,
                CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                "ProgrammaticSuppression",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

            private readonly Diagnostic _originalDiagnostic;
            private readonly string _suppressionId;
            private readonly LocalizableString _suppressionJustification;

            public SuppressionDiagnostic(
                Diagnostic originalDiagnostic,
                string suppressionId,
                LocalizableString suppressionJustification)
            {
                Debug.Assert(originalDiagnostic != null);
                Debug.Assert(originalDiagnostic.ProgrammaticSuppressionInfo != null);
                Debug.Assert(!string.IsNullOrEmpty(suppressionId));
                Debug.Assert(suppressionJustification != null);

                _originalDiagnostic = originalDiagnostic;
                _suppressionId = suppressionId;
                _suppressionJustification = suppressionJustification;
            }

            public override DiagnosticDescriptor Descriptor => s_suppressionDiagnosticDescriptor;

            public override string Id => Descriptor.Id;

            public override string GetMessage(IFormatProvider formatProvider = null)
            {
                // Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
                var localizableMessageFormat = s_suppressionDiagnosticDescriptor.MessageFormat.ToString(formatProvider);
                return string.Format(formatProvider,
                    localizableMessageFormat,
                    _originalDiagnostic.Id,
                    _originalDiagnostic.GetMessage(formatProvider),
                    _suppressionId,
                    _suppressionJustification.ToString(formatProvider));
            }

            public override DiagnosticSeverity Severity => DiagnosticSeverity.Info;
            public override bool IsSuppressed => false;
            public override int WarningLevel => GetDefaultWarningLevel(DiagnosticSeverity.Info);
            public override Location Location => _originalDiagnostic.Location;
            public override IReadOnlyList<Location> AdditionalLocations => _originalDiagnostic.AdditionalLocations;
            public override ImmutableDictionary<string, string> Properties => ImmutableDictionary<string, string>.Empty;

            public override bool Equals(Diagnostic obj)
            {
                var other = obj as SuppressionDiagnostic;
                if (other == null)
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return Equals(_originalDiagnostic, other._originalDiagnostic) &&
                    Equals(_suppressionId, other._suppressionId) &&
                    Equals(_suppressionJustification, other._suppressionJustification);
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as Diagnostic);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_originalDiagnostic.GetHashCode(),
                    Hash.Combine(_suppressionId.GetHashCode(), _suppressionJustification.GetHashCode()));
            }

            internal override Diagnostic WithLocation(Location location)
            {
                throw new NotSupportedException();
            }

            internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                throw new NotSupportedException();
            }

            internal override Diagnostic WithIsSuppressed(bool isSuppressed)
            {
                throw new NotSupportedException();
            }
        }
    }
}
