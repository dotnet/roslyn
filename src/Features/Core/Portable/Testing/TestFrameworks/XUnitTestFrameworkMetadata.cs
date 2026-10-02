// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Features.Testing;

[Export(typeof(ITestFrameworkMetadata)), Shared]
internal sealed class XUnitTestFrameworkMetadata : ITestFrameworkMetadata
{
    public ImmutableArray<string> TestAttributeMetadataNames { get; } =
        ["Xunit.FactAttribute", "Xunit.TheoryAttribute"];

    public bool SupportsDerivedTestAttributes => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public XUnitTestFrameworkMetadata()
    {
    }

    public bool MatchesAttributeSyntacticName(string attributeSyntacticName)
    {
        return attributeSyntacticName is "FactAttribute" or "TheoryAttribute" or "Fact" or "Theory";
    }
}
