// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Features.Testing;

internal interface ITestFrameworkMetadata
{
    /// <summary>
    /// Fully qualified metadata names for test attributes supported by the framework.
    /// </summary>
    ImmutableArray<string> TestAttributeMetadataNames { get; }

    /// <summary>
    /// Whether attributes derived from the framework's test attributes are supported by its test runner.
    /// </summary>
    bool SupportsDerivedTestAttributes { get; }

    /// <summary>
    /// Determines if the input attribute token name matches known test method attribute names.
    /// </summary>
    bool MatchesAttributeSyntacticName(string attributeSyntacticName);
}
