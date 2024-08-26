// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities.RelatedDocuments;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.RelatedDocuments;

public sealed class CSharpRelatedDocumentsTests : AbstractRelatedDocumentsTests
{
    [Theory, CombinatorialData]
    public async Task TestEmptyDocument(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>$$</Document>
                </Project>
            </Workspace>
            """, testHost);
}
