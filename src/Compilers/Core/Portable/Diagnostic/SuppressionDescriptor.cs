// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides a description about a programmatic suppression of a <see cref="Diagnostic"/> by a <see cref="DiagnosticSuppressor"/>.
    /// </summary>
    public sealed class SuppressionDescriptor : IEquatable<SuppressionDescriptor?>
    {
        /// <summary>
        /// An unique identifier for the suppression.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Identifier of the suppressed diagnostic, i.e. <see cref="Diagnostic.Id"/>.
        /// </summary>
        public string SuppressedDiagnosticId { get; }

        /// <summary>
        /// A localizable justification about the suppression.
        /// </summary>
        public LocalizableString Justification { get; }

        /// <summary>
        /// Create a SuppressionDescriptor, which provides a justification about a programmatic suppression of a <see cref="Diagnostic"/>.
        /// NOTE: For localizable <paramref name="justification"/>,
        /// use constructor overload <see cref="SuppressionDescriptor(string, string, LocalizableString)"/>.
        /// </summary>
        /// <param name="id">A unique identifier for the suppression. For example, suppression ID "SP1001".</param>
        /// <param name="suppressedDiagnosticId">Identifier of the suppressed diagnostic, i.e. <see cref="Diagnostic.Id"/>. For example, compiler warning Id "CS0649".</param>
        /// <param name="justification">Justification for the suppression. For example: "Suppress CS0649 on fields marked with YYY attribute as they are implicitly assigned.".</param>
        public SuppressionDescriptor(
            string id,
            string suppressedDiagnosticId,
            string justification)
            : this(id, suppressedDiagnosticId, (LocalizableString)justification)
        {
        }

        /// <summary>
        /// Create a SuppressionDescriptor, which provides a localizable justification about a programmatic suppression of a <see cref="Diagnostic"/>.
        /// </summary>
        /// <param name="id">A unique identifier for the suppression. For example, suppression ID "SP1001".</param>
        /// <param name="suppressedDiagnosticId">Identifier of the suppressed diagnostic, i.e. <see cref="Diagnostic.Id"/>. For example, compiler warning Id "CS0649".</param>
        /// <param name="justification">Justification for the suppression. For example: "Suppress CS0649 on fields marked with YYY attribute as they are implicitly assigned.".</param>
        public SuppressionDescriptor(
            string id,
            string suppressedDiagnosticId,
            LocalizableString justification)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(CodeAnalysisResources.SuppressionIdCantBeNullOrWhitespace, nameof(id));
            }

            if (string.IsNullOrWhiteSpace(suppressedDiagnosticId))
            {
                throw new ArgumentException(CodeAnalysisResources.DiagnosticIdCantBeNullOrWhitespace, nameof(suppressedDiagnosticId));
            }

            this.Id = id;
            this.SuppressedDiagnosticId = suppressedDiagnosticId;
            this.Justification = justification ?? throw new ArgumentNullException(nameof(justification));
        }

        public bool Equals(SuppressionDescriptor? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                other != null &&
                this.Id == other.Id &&
                this.SuppressedDiagnosticId == other.SuppressedDiagnosticId &&
                this.Justification.Equals(other.Justification);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as SuppressionDescriptor);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Id.GetHashCode(),
                   Hash.Combine(this.SuppressedDiagnosticId.GetHashCode(), this.Justification.GetHashCode()));
        }

        /// <summary>
        /// Returns a flag indicating if the suppression is disabled for the given <see cref="CompilationOptions"/>.
        /// </summary>
        /// <param name="compilationOptions">Compilation options</param>
        internal bool IsDisabled(CompilationOptions compilationOptions)
        {
            if (compilationOptions == null)
            {
                throw new ArgumentNullException(nameof(compilationOptions));
            }

            return compilationOptions.SpecificDiagnosticOptions.TryGetValue(Id, out var reportDiagnostic) &&
                reportDiagnostic == ReportDiagnostic.Suppress;
        }
    }
}
