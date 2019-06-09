' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespace1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
                void Goo()
                {
                    [|N|].C x;
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespace2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
                void Goo()
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
                void Goo()
                {
                    [|N|].C x;
                }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespace3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly1" CommonReferences="true">
        <Document>
        namespace {|Definition:[|N|]|}
        {
            class C
            {
                void Goo()
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
                void Goo()
                {
                    [|$$N|].C x;
                }
            }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespace5(host As TestHost) As Task
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
        namespace [|N|].Inner
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceCaseSensitivity1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
                void Goo()
                {
                    [|N|].C x;
                    n.C x;
                }
            }
        }
        </Document>
    </Project>
</Workspace>

            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceCaseSensitivity2(host As TestHost) As Task
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
            sub Goo()
                dim c as [|n|].C = nothing
            end sub()
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceThroughAlias1(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceThroughAlias2(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceThroughAlias3(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WorkItem(541162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541162")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceCalledVar1(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WorkItem(541162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541162")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceCalledVar2(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceWithUnicodeCharacter(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceWithComment(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNamespaceWithVerbatimIdentifier(host As TestHost) As Task
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
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestGlobalNamespace(kind As TestKind, host As TestHost) As Task
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
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
