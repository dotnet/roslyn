// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Features.Testing;

internal interface ITestFrameworkMetadata
{
    /// <summary>
    /// Determines if the input attribute token name matches known test method attribute names.
    /// </summary>
    bool MatchesAttributeSyntacticName(string attributeSyntacticName);
}
