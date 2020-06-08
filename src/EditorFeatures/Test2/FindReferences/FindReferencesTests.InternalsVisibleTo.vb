' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPrivateMethodNotSeenInDownstreamProjectWithoutIVT(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" Name="P1">
        <Document>
        public class C
        {
            private static void $${|Definition:Goo|}()
            {
            }
        }
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>P1</ProjectReference>
        <Document>
        class X
        {
            void Bar()
            {
                C.Goo();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInternalMethodNotSeenInDownstreamProjectWithoutIVT(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" Name="P1">
        <Document>
        public class C
        {
            internal static void $${|Definition:Goo|}()
            {
            }
        }
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>P1</ProjectReference>
        <Document>
        class X
        {
            void Bar()
            {
                C.Goo();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPrivateMethodNotSeenInDownstreamProjectWithIVT(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" Name="P1">
        <Document>
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleTo("P2")]

        public class C
        {
            private static void $${|Definition:Goo|}()
            {
            }
        }
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="P2">
        <ProjectReference>P1</ProjectReference>
        <Document>
        class X
        {
            void Bar()
            {
                C.Goo();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInternalMethodSeenInDownstreamProjectWithIVT(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" Name="P1">
        <Document>
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleTo("P2")]

        public class C
        {
            internal static void $${|Definition:Goo|}()
            {
            }
        }
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="P2">
        <ProjectReference>P1</ProjectReference>
        <Document>
        class X
        {
            void Bar()
            {
                C.[|Goo|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
