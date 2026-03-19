' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        Public Async Function TestAlias1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using $$D = System.[|DateTime|];
        partial class C
        {
            [|D|] date;

            void Goo()
            {
            }
        }
        </Document>
        <Document>
        partial class C
        {
            // Should not be found here.
            D date;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAlias2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using $$D = [|C|];
        partial class {|Definition:C|}
        {
            [|D|] date;

            void Goo()
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAlias3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using $$D = [|N|];
        namespace {|Definition:[|N|]|} {
            partial class C
            {
                [|D|].C date;
                [|N|].C date;

                void Goo()
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_CSharpAttributeEndingWithAttributeThroughAlias(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using GooAttribute = System.[|ObsoleteAttribute|];

        [[|GooAttribute|]]
        class C{ }

        [[|Goo|]]
        class D{ }

        [[|GooAttribute|]()]
        class B{ }

        [[|$$Goo|]()] // Invoke FAR here on Goo
        class Program
        {    
            static void Main(string[] args)    
            {}
        }
        ]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667962")>
        Public Async Function TestMultipleAliasSymbols(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using XAttribute = NS.[|XAttribute|];
using $$YAttribute = NS.[|XAttribute|];
using YAttributeAttribute = NS.[|XAttribute|];

[[|Y|]]
[[|YAttribute|]]
[[|@YAttribute|]]
class Test
{
}

namespace NS
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class {|Definition:XAttribute|} : Attribute
    {
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667962")>
        Public Async Function TestMultipleAliasSymbols2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using XAttribute = NS.[|XAttribute|];
using $$YAttribute = NS.[|XAttribute|];

[[|Y|]]
[[|YAttribute|]]
[[|@YAttribute|]]
class Test
{
}

namespace NS
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class {|Definition:XAttribute|} : Attribute
    {
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamedType_VBAttributeEndingWithAttributeThroughAlias(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
        Imports GooAttribute = System.[|ObsoleteAttribute|];

        <[|GooAttribute|]>
        Class C
        End Class

        <[|Goo|]>
        Class D
        End Class

        <[|GooAttribute|]()>
        Class B
        End Class

        <[|$$Goo|]()> ' Invoke FAR here on Goo
        Class Program
            Public Shared Sub Main()    
            End Function
        End Class
        ]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAliasReferenceInGlobalSuppression(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using $$AliasToC = N.[|C|];

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("RuleCategory", "RuleId", Scope = "member", Target = "~M:N.[|C|].Goo")]

namespace N
{
    class {|Definition:C|}
    {
        void Goo()
        {
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAliasReferenceInGlobalSuppression_WithAttributeSuffix(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using $$AliasToC = N.[|C|];

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("RuleCategory", "RuleId", Scope = "member", Target = "~M:N.[|C|].Goo")]

namespace N
{
    class {|Definition:C|}
    {
        void Goo()
        {
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAliasReferenceInGlobalSuppression_WithUsing(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using $$AliasToC = N.[|C|];
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("RuleCategory", "RuleId", Scope = "member", Target = "~M:N.[|C|].Goo")]

namespace N
{
    class {|Definition:C|}
    {
        void Goo()
        {
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAliasReferenceInGlobalSuppression_WithUsing_WithAttributeSuffix(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using $$AliasToC = N.[|C|];
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessageAttribute("RuleCategory", "RuleId", Scope = "member", Target = "~M:N.[|C|].Goo")]

namespace N
{
    class {|Definition:C|}
    {
        void Goo()
        {
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/55894")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGlobalAlias1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        global using $$D = System.[|DateTime|];
        partial class C
        {
            [|D|] date;

            void Goo()
            {
            }
        }
        </Document>
        <Document>
        partial class C
        {
            [|D|] date;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/55894")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGlobalAlias2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        global using $$D = System.[|DateTime|];
        </Document>
        <Document>
        partial class C
        {
            [|D|] date;
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/55894")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGlobalAlias3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        global using $$DAttribute = System.[|CLSCompliantAttribute|];
        </Document>
        <Document>
        [[|DAttribute|]]
        partial class C
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/55894")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGlobalAlias4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        global using $$DAttribute = System.[|CLSCompliantAttribute|];
        </Document>
        <Document>
        [[|DAttribute|](false)]
        partial class C
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/55894")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGlobalAlias5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        global using $$DAttribute = System.[|CLSCompliantAttribute|];
        </Document>
        <Document>
        [[|D|]]
        partial class D
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/55894")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGlobalAlias6(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        global using $$DAttribute = System.[|CLSCompliantAttribute|];
        </Document>
        <Document>
        [[|D|](false)]
        partial class D
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/55894")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGlobalAlias7(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        global using $$D = System.[|DateTime|];

        </Document>
        <Document>
        namespace Outer
        {
            using D2 = [|D|];
            partial class C
            {
                [|D2|] date;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/67989")>
        Public Async Function TestAliasToPointer(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using unsafe $${|Definition:Alias|} = int*;

namespace N
{
    internal unsafe class C
    {
        const int Factor = 3;

        void M()
        {
            _ = Alias * Factor;
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/67989")>
        Public Async Function TestAliasToPointer2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using unsafe $${|Definition:Alias|} = int*;

namespace N
{
    internal unsafe class C
    {
        void M()
        {
            [|Alias|]* f = null;
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/74384")>
        Public Async Function TestAliasToPrimitive1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using $$TBitMap = [|ulong|];

        var size = sizeof([|TBitMap|]);
        var array = new [|TBitMap|][10];
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
