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
    public async Task EmptyDocument(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>$$</Document>
                </Project>
            </Workspace>
            """, testHost);

    [Theory, CombinatorialData]
    public async Task ReferenceToSameDocument(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>[||]$$
                    class C
                    {
                        C c;
                    }
                    </Document>
                </Project>
            </Workspace>
            """, testHost);

    [Theory, CombinatorialData]
    public async Task MultipleReferencesToSameDocument(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>[||]$$
                    class C
                    {
                        C c;
                        C c;
                    }
                    </Document>
                </Project>
            </Workspace>
            """, testHost);

    [Theory, CombinatorialData]
    public async Task ReferenceToDifferentDocument(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>$$
                    class C
                    {
                        D d;
                    }
                    </Document>
                    <Document>[||]
                    class D
                    {
                    }
                    </Document>
                </Project>
            </Workspace>
            """, testHost);

    [Theory, CombinatorialData]
    public async Task MultipleReferencesToDifferentDocument(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>$$
                    class C
                    {
                        D d;
                        D d;
                    }
                    </Document>
                    <Document>[||]
                    class D
                    {
                    }
                    </Document>
                </Project>
            </Workspace>
            """, testHost);

    [Theory, CombinatorialData]
    public async Task ReferenceWithinGeneric(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document><![CDATA[$$
                    class C
                    {
                        List<D> d;
                    }
                    ]]></Document>
                    <Document>[||]
                    class D
                    {
                    }
                    </Document>
                </Project>
            </Workspace>
            """, testHost);

    [Theory, CombinatorialData]
    public async Task QualifiedReferenceWithinGeneric(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>$$
                    class C
                    {
                        N.D d;
                    }
                    </Document>
                    <Document>[||]
                    namespace N;

                    class D
                    {
                    }
                    </Document>
                </Project>
            </Workspace>
            """, testHost);

    [Theory, CombinatorialData]
    public async Task QualifiedReferenceThroughStaticWithinGeneric(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>$$
                    class C
                    {
                        void M()
                        {
                            Console.WriteLine(N.D.I);
                        }
                    }
                    </Document>
                    <Document>[||]
                    namespace N;

                    class D
                    {
                        public static int I;
                    }
                    </Document>
                </Project>
            </Workspace>
            """, testHost);

    [Theory, CombinatorialData]
    public async Task ReferenceToPartialType(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>$$
                    class C
                    {
                        D d;
                    }
                    </Document>
                    <Document>[||]
                    partial class D
                    {
                    }
                    </Document>
                    <Document>[||]
                    partial class D
                    {
                    }
                    </Document>
                </Project>
            </Workspace>
            """, testHost);

    [Theory, CombinatorialData]
    public async Task NoReferenceToNamespace(TestHost testHost)
        => await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>$$
                    using N;

                    class C
                    {
                    }
                    </Document>
                    <Document>
                    namespace N;

                    partial class D
                    {
                    }
                    </Document>
                </Project>
            </Workspace>
            """, testHost);
}
