// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides a description about a <see cref="Diagnostic"/>
    /// </summary>
    public class DiagnosticDescriptor
    {
        /// <summary>
        /// An unique identifier for the diagnostic.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// A short localizable description of the diagnostic.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// A localizable format message string, which can be passed as the first argument to <see cref="M:System.String.Format"/> when creating the diagnostic message with this descriptor.
        /// </summary>
        /// <returns></returns>
        public string MessageFormat { get; private set; }

        /// <summary>
        /// The category of the diagnostic (like Design, Naming etc.)
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// The default severity of the diagnostic.
        /// </summary>
        /// <returns></returns>
        public DiagnosticSeverity DefaultSeverity { get; private set; }

        /// <summary>
        /// Create a DiagnosticDescriptor, which provides description about a <see cref="Diagnostic"/>.
        /// </summary>
        /// <param name="id">A unique identifier for the diagnostic. For example, code analysis diagnostic ID "CA1001".</param>
        /// <param name="description">A short localizable description of the diagnostic. For example, for CA1001: "Types that own disposable fields should be disposable".</param>
        /// <param name="messageFormat">A localizable format message string, which can be passed as the first argument to <see cref="M:System.String.Format"/> when creating the diagnostic message with this descriptor.
        /// For example, for CA1001: "Implement IDisposable on '{0}' because it creates members of the following IDisposable types: '{1}'."</param>
        /// <param name="category">The category of the diagnostic (like Design, Naming etc.). For example, for CA1001: "Microsoft.Design".</param>
        /// <param name="defaultSeverity">Default severity of the diagnostic.</param>
        public DiagnosticDescriptor(string id, string description, string messageFormat, string category, DiagnosticSeverity defaultSeverity)
        {
            this.Id = id;
            this.Description = description;
            this.Category = category;
            this.MessageFormat = messageFormat;
            this.DefaultSeverity = defaultSeverity;
        }
    }
}
