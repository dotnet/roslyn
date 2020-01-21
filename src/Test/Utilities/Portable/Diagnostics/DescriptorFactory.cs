// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Factory for creating different kinds of <see cref="DiagnosticDescriptor"/>s for use in tests.
    /// </summary>
    public static class DescriptorFactory
    {
        /// <summary>
        /// Creates a <see cref="DiagnosticDescriptor"/> with specified <see cref="DiagnosticDescriptor.Id"/>.
        /// </summary>
        /// <remarks>
        /// Returned <see cref="DiagnosticDescriptor"/> has
        /// - empty <see cref="DiagnosticDescriptor.Title"/> and <see cref="DiagnosticDescriptor.Category"/>
        /// - <see cref="DiagnosticDescriptor.MessageFormat"/> set to <paramref name="id"/>
        /// - <see cref="DiagnosticDescriptor.DefaultSeverity"/> set to <see cref="DiagnosticSeverity.Hidden"/>
        /// - <see cref="WellKnownDiagnosticTags.NotConfigurable"/> custom tag added in <see cref="DiagnosticDescriptor.CustomTags"/>.
        /// </remarks>
        /// <param name="id">The value for <see cref="DiagnosticDescriptor.Id"/>.</param>
        /// <returns>A <see cref="DiagnosticDescriptor"/> with specified <see cref="DiagnosticDescriptor.Id"/>.</returns>
        public static DiagnosticDescriptor CreateSimpleDescriptor(string id)
        {
            return new DiagnosticDescriptor(id, title: "", messageFormat: id, category: "",
                defaultSeverity: DiagnosticSeverity.Hidden, isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.NotConfigurable);
        }
    }
}
