' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespace1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
                void Foo()
                {
                    [|N|].C x;
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespace2()
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
                void Foo()
                {
                    [|N|].C x;
                }
            }
        }
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly2" CommonReferences="true">
        <ProjectReference>CSharpAssembly1</ProjectReference>
        <Document>
            class D
            {
                void Foo()
                {
                    [|N|].C x;
                }
            }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespace3()
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
        <Document>
        namespace {|Definition:[|N|]|}
        {
            class C
            {
                void Foo()
                {
                    [|N|].C x;
                }
            }
        }
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly2" CommonReferences="true">
        <ProjectReference>CSharpAssembly1</ProjectReference>
        <Document>
            class D
            {
                void Foo()
                {
                    [|$$N|].C x;
                }
            }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespace5()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
            }
        }
        namespace {|Definition:[|N|]|}.Inner
        {
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceCaseSensitivity1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
                void Foo()
                {
                    [|N|].C x;
                    n.C x;
                }
            }
        }
        </Document>
    </Project>
</Workspace>

            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceCaseSensitivity2()
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            public class C
            {
            }
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
        class D
            sub Foo()
                dim c as [|n|].C = nothing
            end sub()
        end class
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceThroughAlias1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class D
            {
            }
        }
        </Document>
        <Document>
        using N1 = [|N|];
        class C
        {
            [|N1|].D d;
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceThroughAlias2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace Outer.{|Definition:[|$$N|]|}
        {
            class D
            {
            }
        }
        </Document>
        <Document>
        using N1 = Outer.[|N|];
        class C
        {
            [|N1|].D d;
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceThroughAlias3()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace Outer.{|Definition:[|$$N|]|}
        {
            namespace Inner
            {
            }

            class D
            {
            }
        }
        </Document>
        <Document>
        using N1 = Outer.[|N|].Inner;
        class C
        {
            N1.D d;
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(541162)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceCalledVar1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class Program
{
    static void Main()
    {
        var x = 1;
    }
}
 
namespace {|Definition:$$[|var|]|} { }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WorkItem(541162)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceCalledVar2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
 
class Program
{
    static void Main()
    {
        $$var x = 1;
    }
}
 
namespace var { }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceWithUnicodeCharacter()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
                [|N|].C x;
                [|\u004e|].C x;
                [|\U0000004e|].C x;
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceWithComment()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
                /*N*/[|N|].C x;
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestNamespaceWithVerbatimIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$@namespace|]|}
        {
            class C
            {
                [|@namespace|].C x;
            }
        }
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestGlobalNamespace()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Module M
                Sub Main
                    [|$$Global|].M.Main()
                    [|Global|].M.Main()
                End Sub
            End Module
        </Document>
    </Project>
</Workspace>
            Test(input)
        End Sub
    End Class
End Namespace
