' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(667962, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667962")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(667962, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667962")>
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

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
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
    End Class
End Namespace
