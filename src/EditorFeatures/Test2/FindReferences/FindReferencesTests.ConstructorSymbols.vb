' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisConstructorInitializerSameFile1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    private int v;
    public Program() : $$[|this|](4)
    {
    }

    public {|Definition:Program|}(int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisConstructorInitializerSameFile2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    private int v;
    public Program() : [|this|](4)
    {
    }

    public $${|Definition:Program|}(int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisConstructorInitializerDifferentFile1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class Program
{
    private int v;
    public Program() : $$[|this|](4)
    {
    }
}
        </Document>
        <Document>
partial class Program
{
    public {|Definition:Program|}(int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisConstructorInitializerDifferentFile2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class Program
{
    private int v;
    public Program() : [|this|](4)
    {
    }
}
        </Document>
        <Document>
partial class Program
{
    public $${|Definition:Program|}(int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerSameFile1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program : BaseType
{
    private int v;
    public Program() : $$[|base|](4)
    {
    }
}

class BaseType
{
    public {|Definition:BaseType|}(int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerSameFile2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program : BaseType
{
    private int v;
    public Program() : [|base|](4)
    {
    }
}

class BaseType
{
    public $${|Definition:BaseType|}(int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerDifferentFile1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program : BaseType
{
    private int v;
    public Program() : [|base|](4)
    {
    }
}
        </Document>
        <Document>
class BaseType
{
    public $${|Definition:BaseType|}(int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerDifferentFile2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program : BaseType
{
    private int v;
    public Program() : $$[|base|](4)
    {
    }
}
        </Document>
        <Document>
class BaseType
{
    public {|Definition:BaseType|}(int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerDifferentFile3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
partial class Program : BaseType
{
}
        </Document>
        <Document>
partial class Program
{
    private int v;
    public Program() : $$[|base|](4)
    {
    }
}
        </Document>
        <Document>
class BaseType
{
    public {|Definition:BaseType|}(int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(541658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541658")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttribute1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

[[|AttClass|]()]
class Program
{
}

internal class {|Definition:$$AttClassAttribute|} : Attribute
{
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(541658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541658")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttribute2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

[[|AttClassAttribute|]()]
class Program
{
}

internal class {|Definition:$$AttClassAttribute|} : Attribute
{
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(541658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541658")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttribute3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[[|AttClass|]()]
class Program
{
}

internal class {|Definition:$$AttClassAttribute|} : Attribute
{
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(541658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541658")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttribute4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[[|AttClassAttribute|]()]
class Program
{
}

internal class {|Definition:$$AttClassAttribute|} : Attribute
{
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisAtEndOfFile(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Program
{
    private int v;
    public Program() : $$[|this|](4)
    {
    }

    public {|Definition:Program|}(int v)
    {
        this</Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructor_ThroughAlias1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N
        {
            class D
            {
                public {|Definition:$$D|}() { }
            }
        }
        </Document>
        <Document>
        using M = N.D;
        class C
        {
            M d = new [|M|]();
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N
        {
            class GooAttribute : System.Attribute
            {
                public {|Definition:$$GooAttribute|}() { }
            }
        }
        </Document>
        <Document>
        using M = N.GooAttribute;

        [[|M|]()]
        [[|M|]]
        class C
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N
        {
            class GooAttribute : System.Attribute
            {
                public {|Definition:$$GooAttribute|}() { }
            }
        }
        </Document>
        <Document>
        using MAttribute = N.GooAttribute;

        [[|M|]()]
        [[|M|]]
        [[|MAttribute|]()]
        [[|MAttribute|]]
        class C
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.Something;
 
namespace X
{
    class Something { public {|Definition:$$Something|}() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new [|AliasedSomething|]();
        X.Something y = new X.[|Something|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.[|Something|];
 
namespace X
{
    class {|Definition:Something|} { public {|Definition:Something|}() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        [|AliasedSomething|] x = new [|$$AliasedSomething|]();
        X.[|Something|] y = new X.[|Something|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.Something;
 
namespace X
{
    class Something { public {|Definition:Something|}() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new [|AliasedSomething|]();
        X.Something y = new X.[|$$Something|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias6(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.[|Something|];
 
namespace X
{
    class {|Definition:$$Something|} { public {|Definition:Something|}() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        [|AliasedSomething|] x = new [|AliasedSomething|]();
        X.[|Something|] y = new X.[|Something|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias7(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.[|Something|];
 
namespace X
{
    class {|Definition:Something|} { public {|Definition:Something|}() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        [|$$AliasedSomething|] x = new [|AliasedSomething|]();
        X.[|Something|] y = new X.[|Something|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias8(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.[|Something|];
 
namespace X
{
    class {|Definition:Something|} { public {|Definition:Something|}() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        [|AliasedSomething|] x = new [|AliasedSomething|]();
        X.[|$$Something|] y = new X.[|Something|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias9(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using $$AliasedSomething = X.[|Something|];
 
namespace X
{
    class {|Definition:Something|} { public {|Definition:Something|}() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        [|AliasedSomething|] x = new [|AliasedSomething|]();
        X.[|Something|] y = new X.[|Something|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(542979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542979")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias10(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using GooAttribute = System.[|ObsoleteAttribute|];
 
[$$[|Goo|]]
[[|Goo|]()]
[[|GooAttribute|]]
[[|GooAttribute|]()]
class C { }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

#If False Then
        <WorkItem(10441)>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias11() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using GooAttribute = System.$$[|ObsoleteAttribute|];
 
[Goo]
[Goo()]
[GooAttribute]
[GooAttribute()]
class C { }
        </Document>
    </Project>
</Workspace>
        Await TestAsync(input)
        End Sub
#End If

        <WorkItem(542386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542386")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestClassCalledNew1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim c As New [|[New]|]()
    End Sub
End Module
 
Class {|Definition:$$[New]|}
 
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(531200, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531200")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpCascadeFromTypeToConstructorsAndDestructors(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
namespace Tester
{
    class {|Definition:$$NotProgram|}
    {
        public {|Definition:NotProgram|}(int i) { }
        public {|Definition:NotProgram|}() { }
        ~{|Definition:NotProgram|}() { }
    }

    class Program
    {
        static void Main()
        {
            [|NotProgram|] np = new [|NotProgram|](23);
        }
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(531200, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531200")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVBCascadeFromTypeToConstructorsAndDestructors(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Namespace Tester
    Class {|Definition:$$NotProgram|}
        Public Sub {|Definition:New|}(i As Integer)
        End Sub

        Public Sub {|Definition:New|}()
        End Sub
    End Class

    Class Program
        Public shared Sub Main()
            Dim np as [|NotProgram|] = New [|NotProgram|](23)
        End Sub
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(652809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652809")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpStaticCtorVsInstanceCtorReferences(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class A
{
    public A() { }
    static {|Definition:$$A|}() // Invoke FAR on A here
    { }
}
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem(25655, "https://github.com/dotnet/roslyn/issues/25655")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNoCompilationProjectReferencingCSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="NoCompilation" CommonReferences="false">
        <ProjectReference>CSharpProject</ProjectReference>
        <Document>
            // a no-compilation document
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpProject" CommonReferences="true">
        <Document>
public class A
{
    public {|Definition:$$A|}()
    {
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
