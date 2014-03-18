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
        /// The kind of the diagnostic (like compiler, unnecessary etc.)
        /// </summary>
        public string Kind { get; private set; }

        /// <summary>
        /// Name of the diagnostic.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The parameterized message of the diagnostic.
        /// </summary>
        /// <returns></returns>
        public string MessageTemplate { get; private set; }

        /// <summary>
        /// The category of the diagnostic (like Design, Naming etc.)
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// The severity of the diagnostic.
        /// </summary>
        /// <returns></returns>
        public DiagnosticSeverity Severity { get; private set; }

        /// <summary>
        /// Create a DiagnosticDescriptor
        /// </summary>
        public DiagnosticDescriptor(string id, string kind, string name, string messageTemplate, string category, DiagnosticSeverity severity)
        {
            this.Id = id;
            this.Kind = kind;
            this.Name = name;
            this.Category = category;
            this.MessageTemplate = messageTemplate;
            this.Severity = severity;
        }
    }
}
