﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides a description about a <see cref="Diagnostic"/> that is a trigger for some custom behavior for code analyis clients.
    /// </summary>
    public class TriggerDiagnosticDescriptor : DiagnosticDescriptor
    {
        /// <summary>
        /// Create a TriggerDiagnosticDescriptor, which provides description about a <see cref="Diagnostic"/> that is a trigger for some custom behavior for code analyis clients.
        /// </summary>
        /// <param name="id">A unique identifier for the diagnostic. For example, code analysis diagnostic ID "CA1001".</param>
        /// <param name="customTags">Optional custom tags for the diagnostic. See <see cref="WellKnownDiagnosticTags"/> for some well known tags.</param>
        public TriggerDiagnosticDescriptor(string id, params string[] customTags)
            : base(id, title: "", messageFormat: "", category: "", defaultSeverity: DiagnosticSeverity.Hidden, isEnabledByDefault: true,
                    customTags: customTags.Append(WellKnownDiagnosticTags.NotConfigurable).Append(WellKnownDiagnosticTags.Trigger))
        {
        }
    }
}
