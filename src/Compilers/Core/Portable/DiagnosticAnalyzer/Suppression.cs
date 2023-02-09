// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Programmatic suppression of a <see cref="Diagnostic"/> by a <see cref="DiagnosticSuppressor"/>.
    /// </summary>
    public readonly struct Suppression : IEquatable<Suppression>
    {
        private Suppression(SuppressionDescriptor descriptor, Diagnostic suppressedDiagnostic)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            SuppressedDiagnostic = suppressedDiagnostic ?? throw new ArgumentNullException(nameof(suppressedDiagnostic));
            Debug.Assert(suppressedDiagnostic.ProgrammaticSuppressionInfo == null);

            if (descriptor.SuppressedDiagnosticId != suppressedDiagnostic.Id)
            {
                // Suppressed diagnostic ID '{0}' does not match suppressable ID '{1}' for the given suppression descriptor.
                var message = string.Format(CodeAnalysisResources.InvalidDiagnosticSuppressionReported, suppressedDiagnostic.Id, descriptor.SuppressedDiagnosticId);
                throw new ArgumentException(message);
            }
        }

        /// <summary>
        /// Creates a suppression of a <see cref="Diagnostic"/> with the given <see cref="SuppressionDescriptor"/>.
        /// </summary>
        /// <param name="descriptor">
        /// Descriptor for the suppression, which must be from <see cref="DiagnosticSuppressor.SupportedSuppressions"/>
        /// for the <see cref="DiagnosticSuppressor"/> creating this suppression.
        /// </param>
        /// <param name="suppressedDiagnostic">
        /// <see cref="Diagnostic"/> to be suppressed, which must be from <see cref="SuppressionAnalysisContext.ReportedDiagnostics"/>
        /// for the suppression context in which this suppression is being created.</param>
        public static Suppression Create(SuppressionDescriptor descriptor, Diagnostic suppressedDiagnostic)
            => new Suppression(descriptor, suppressedDiagnostic);

        /// <summary>
        /// Descriptor for this suppression.
        /// </summary>
        public SuppressionDescriptor Descriptor { get; }

        /// <summary>
        /// Diagnostic suppressed by this suppression.
        /// </summary>
        public Diagnostic SuppressedDiagnostic { get; }

        public static bool operator ==(Suppression left, Suppression right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Suppression left, Suppression right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return obj is Suppression suppression
                && Equals(suppression);
        }

        public bool Equals(Suppression other)
        {
            return EqualityComparer<SuppressionDescriptor>.Default.Equals(Descriptor, other.Descriptor)
                && EqualityComparer<Diagnostic>.Default.Equals(SuppressedDiagnostic, other.SuppressedDiagnostic);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                EqualityComparer<SuppressionDescriptor>.Default.GetHashCode(Descriptor),
                EqualityComparer<Diagnostic>.Default.GetHashCode(SuppressedDiagnostic));
        }
    }
}
