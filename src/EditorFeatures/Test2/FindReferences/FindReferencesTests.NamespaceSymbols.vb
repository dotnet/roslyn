' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541162")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541162")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestNamespace_TypeOrNamespaceUsageInfo(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using {|TypeOrNamespaceUsageInfo.Import:[|N1|]|};
        using {|TypeOrNamespaceUsageInfo.Qualified,Import:[|N1|]|}.N2;

        namespace {|Definition:{|TypeOrNamespaceUsageInfo.NamespaceDeclaration:[|$$N1|]|}|}
        {
            public class Class1
            {
                public static int Field;
            }
        }
        
        namespace {|TypeOrNamespaceUsageInfo.Qualified,NamespaceDeclaration:[|N1|]|}.N2
        {
            public class Class2
            {
                public static int M() =>  {|TypeOrNamespaceUsageInfo.Qualified:[|N1|]|}.Class1.Field;
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamespaceReferenceInGlobalSuppression(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~N:[|N|]")]
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~T:[|N|].C")]
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:[|N|].C.M")]

        namespace {|Definition:[|$$N|]|}
        {
            class C
            {
                void M()
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamespaceReferenceInGlobalSuppression_OuterNamespace(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~N:[|N1|]")]
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~N:[|N1|].N2")]
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~T:[|N1|].N2.C")]
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:[|N1|].N2.C.M")]

        namespace {|Definition:[|$$N1|]|}.N2
        {
            class C
            {
                void M()
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNamespaceReferenceInGlobalSuppression_InnerNamespace(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~N:N1.[|N2|]")]
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~T:N1.[|N2|].C")]
        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:N1.[|N2|].C.M")]

        namespace N1.{|Definition:[|$$N2|]|}
        {
            class C
            {
                void M()
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfFact, CombinatorialData>
        Public Async Function TestNamespaceUsedInSourceGeneratedDocument() As Task
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
        <DocumentFromSourceGenerator>
        namespace [|N|]
        {
            class D
            {
            }
        }
        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPI(input, TestHost.InProcess) ' TODO: support out of proc in tests: https://github.com/dotnet/roslyn/issues/50494
        End Function
    End Class
End Namespace
