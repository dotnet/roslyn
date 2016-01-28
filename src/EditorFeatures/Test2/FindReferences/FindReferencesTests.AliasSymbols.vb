' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAlias1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using $$D = System.[|DateTime|];
        partial class C
        {
            [|D|] date;

            void Foo()
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAlias2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using $$D = [|C|];
        partial class {|Definition:C|}
        {
            [|D|] date;

            void Foo()
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAlias3() As Task
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

                void Foo()
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_CSharpAttributeEndingWithAttributeThroughAlias() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using FooAttribute = System.[|ObsoleteAttribute|];

        [[|FooAttribute|]]
        class C{ }

        [[|Foo|]]
        class D{ }

        [[|FooAttribute|]()]
        class B{ }

        [[|$$Foo|]()] // Invoke FAR here on Foo
        class Program
        {    
            static void Main(string[] args)    
            {}
        }
        ]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(667962)>
        Public Async Function TestMultipleAliasSymbols() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(667962)>
        Public Async Function TestMultipleAliasSymbols2() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamedType_VBAttributeEndingWithAttributeThroughAlias() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
        Imports FooAttribute = System.[|ObsoleteAttribute|];

        <[|FooAttribute|]>
        Class C
        End Class

        <[|Foo|]>
        Class D
        End Class

        <[|FooAttribute|]()>
        Class B
        End Class

        <[|$$Foo|]()> ' Invoke FAR here on Foo
        Class Program
            Public Shared Sub Main()    
            End Function
        End Class
        ]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
    End Class
End Namespace