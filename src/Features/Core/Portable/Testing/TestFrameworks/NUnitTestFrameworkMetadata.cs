// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Features.Testing;

[Export(typeof(ITestFrameworkMetadata)), Shared]
internal class NUnitTestFrameworkMetadata : ITestFrameworkMetadata
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NUnitTestFrameworkMetadata()
    {
    }

    public bool MatchesAttributeSymbolName(string attributeSymbolName)
    {
        return attributeSymbolName is "NUnit.Framework.TestAttribute" or "NUnit.Framework.TheoryAttribute" or "NUnit.Framework.TestCaseAttribute" or "NUnit.Framework.TestCaseSourceAttribute";
    }
}
