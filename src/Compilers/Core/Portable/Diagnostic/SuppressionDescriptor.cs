// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides a description about a programmatic suppression of a <see cref="Diagnostic"/> by a <see cref="DiagnosticSuppressor"/>.
    /// </summary>
    public sealed class SuppressionDescriptor : IEquatable<SuppressionDescriptor>
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
        /// A localizable description about the suppression.
        /// </summary>
        public LocalizableString Description { get; }

        /// <summary>
        /// Create a SuppressionDescriptor, which provides description about a programmatic suppression of a <see cref="Diagnostic"/>.
        /// NOTE: For localizable <paramref name="description"/>,
        /// use constructor overload .
        /// </summary>
        /// <param name="id">A unique identifier for the suppression. For example, suppression ID "SP1001".</param>
        /// <param name="suppressedDiagnosticId">Identifier of the suppressed diagnostic, i.e. <see cref="Diagnostic.Id"/>. For example, compiler warning Id "CS0649".</param>
        /// <param name="description">Description of the suppression. For example: "Suppress CS0649 on fields marked with YYY attribute as they are implicitly assigned.".</param>
        public SuppressionDescriptor(
            string id,
            string suppressedDiagnosticId,
            string description)
            : this(id, suppressedDiagnosticId,
                   LocalizableString.Create(description ?? throw new ArgumentNullException(nameof(description))))
        {
        }

        /// <summary>
        /// Create a SuppressionDescriptor, which provides description about a programmatic suppression of a <see cref="Diagnostic"/>.
        /// </summary>
        /// <param name="id">A unique identifier for the suppression. For example, suppression ID "SP1001".</param>
        /// <param name="suppressedDiagnosticId">Identifier of the suppressed diagnostic, i.e. <see cref="Diagnostic.Id"/>. For example, compiler warning Id "CS0649".</param>
        /// <param name="description">Description of the suppression. For example: "Suppress CS0649 on fields marked with YYY attribute as they are implicitly assigned.".</param>
        public SuppressionDescriptor(
            string id,
            string suppressedDiagnosticId,
            LocalizableString description)
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
            this.Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public bool Equals(SuppressionDescriptor other)
        {
            return
                other != null &&
                this.Id == other.Id &&
                this.SuppressedDiagnosticId == other.SuppressedDiagnosticId &&
                this.Description.Equals(other.Description);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SuppressionDescriptor);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Id.GetHashCode(),
                   Hash.Combine(this.SuppressedDiagnosticId.GetHashCode(), this.Description.GetHashCode()));
        }

        /// <summary>
        /// Flag indicating if the suppression is disabled for the given <see cref="CompilationOptions"/>.
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
