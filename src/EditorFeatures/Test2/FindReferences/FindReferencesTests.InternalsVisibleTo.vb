' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestInternalMethodSeenInDownstreamProjectWithIVT_SimpleString(kind As TestKind, host As TestHost) As Task
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestInternalMethodSeenInDownstreamProjectWithIVT_WithAttributeSuffix(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" Name="P1">
        <Document>
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleToAttribute("P2")]

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

        <WpfTheory, CombinatorialData>
        Public Async Function TestInternalMethodSeenInDownstreamProjectWithIVT_QualifiedAttributeName(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" Name="P1">
        <Document>
        [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("P2")]

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

        <WpfTheory, CombinatorialData>
        Public Async Function TestInternalMethodSeenInDownstreamProjectWithIVT_VerbatimString(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" Name="P1">
        <Document>
        using System.Runtime.CompilerServices;

        [assembly: InternalsVisibleTo(@"P2")]

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

        <WpfTheory, CombinatorialData>
        Public Async Function TestInternalMethodSeenInDownstreamProjectWithIVT_UsingAlias(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" Name="P1">
        <Document>
        using X = System.Runtime.CompilerServices.InternalsVisibleToAttribute;

        [assembly: X(@"P2")]

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

        <WpfTheory, CombinatorialData>
        Public Async Function TestInternalMethodSeenInDownstreamProjectWithIVT_NonLiteralString(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" Name="P1">
        <Document>
        [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("P" + "2")]

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
