// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License; Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal partial class ErrorLogger
    {
        /// <summary>
        /// Represents an issue to be logged into the error log.
        /// This could be corresponding to a <see cref="Diagnostic"/> or a <see cref="DiagnosticInfo"/> reported by the <see cref="CommonCompiler"/>.
        /// </summary>
        private struct Issue
        {
            public readonly string Id;
            public readonly string Message;
            public readonly string Description;
            public readonly string Title;
            public readonly string Category;
            public readonly string HelpLink;
            public readonly bool IsEnabledByDefault;
            public readonly bool IsSuppressedInSource;
            public readonly DiagnosticSeverity DefaultSeverity;
            public readonly DiagnosticSeverity Severity;
            public readonly int WarningLevel;
            public readonly Location Location;
            public readonly IReadOnlyList<Location> AdditionalLocations;
            public readonly IReadOnlyList<string> CustomTags;
            public readonly ImmutableArray<KeyValuePair<string, string>> CustomProperties;

            public Issue(
                string id, string message, string description,
                string title, string category, string helpLink, bool isEnabledByDefault, bool isSuppressedInSource,
                DiagnosticSeverity defaultSeverity, DiagnosticSeverity severity, int warningLevel,
                Location location, IReadOnlyList<Location> additionalLocations,
                IReadOnlyList<string> customTags, ImmutableDictionary<string, string> customProperties)
            {
                Id = id;
                Message = message;
                Description = description;
                Title = title;
                Category = category;
                HelpLink = helpLink;
                IsEnabledByDefault = isEnabledByDefault;
                IsSuppressedInSource = isSuppressedInSource;
                DefaultSeverity = defaultSeverity;
                Severity = severity;
                WarningLevel = warningLevel;
                Location = location;
                AdditionalLocations = additionalLocations;
                CustomTags = customTags;
                CustomProperties = customProperties.OrderBy(kvp => kvp.Key).ToImmutableArray();
            }
        }
    }
}
