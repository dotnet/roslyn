' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestThisConstructorInitializerSameFile_FileScopedNamespace(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
namespace FileScopedNS;

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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541658")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541658")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541658")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541658")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542218")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542979")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11049")>
        Public Async Function TestImplicitBaseConstructorReference_CSharp1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
internal abstract class Abstract
{
    protected {|Definition:$$Abstract|}()
    {
    }
}

internal abstract class Derived : Abstract
{
    protected [|Derived|]()
    {
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11049")>
        Public Async Function TestImplicitBaseConstructorReference_CSharp2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
internal abstract class Abstract
{
    protected {|Definition:$$Abstract|}()
    {
    }
}

internal abstract class Derived
{
    protected Derived()
    {
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11049")>
        Public Async Function TestImplicitBaseConstructorReference_CSharp3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
internal abstract class Abstract
{
    protected {|Definition:$$Abstract|}()
    {
    }
}

internal abstract class Derived : Abstract
{
    protected [|Derived|](int i)
    {
    }

    protected Derived() : this(0)
    {
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11049")>
        Public Async Function TestImplicitBaseConstructorReference_CSharp4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
internal abstract class Abstract
{
    protected {|Definition:$$Abstract|}(int i = 0)
    {
    }
}

internal abstract class Derived : Abstract
{
    protected [|Derived|](int i)
    {
    }

    protected Derived() : this(0)
    {
    }

    protected Derived(params int[] i) : this(0)
    {
    }
}

        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11049")>
        Public Async Function TestImplicitBaseConstructorReference_CSharp5(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
internal abstract class Abstract
{
    protected {|Definition:$$Abstract|}(params int[] i)
    {
    }
}

internal abstract class Derived : Abstract
{
    protected [|Derived|](int i)
    {
    }

    protected Derived() : this(0)
    {
    }

    protected Derived(params int[] i) : this(0)
    {
    }
}

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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542386")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531200")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531200")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652809")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/25655")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructor_ImplicitObjectCreation_Local(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    public {|Definition:$$D|}() { }
}
        </Document>
        <Document>
class C
{
    void M()
    {
        D d = [|new|]();
        D d2 = [|new|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructor_ImplicitAndExplicitObjectCreation_Local(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    public {|Definition:$$D|}() { }
}
        </Document>
        <Document>
class C
{
    void M()
    {
        D d = [|new|]();
        D d2 = new [|D|]();
        D d3 = [|new|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructor_ImplicitObjectCreation_Local_WithArguments(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    public {|Definition:$$D|}(int i, int j) { }
}
        </Document>
        <Document>
class C
{
    void M()
    {
        D d = [|new|](1, 2);
        D d2 = [|new|](3, 4);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructor_ImplicitObjectCreation_Field(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    public {|Definition:$$D|}() { }
}
        </Document>
        <Document>
class C
{
    D d = [|new|]();
    D d2 = [|new|]();
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/47987")>
        Public Async Function DoNotCountInstantiationTwiceWhenTargetTypedNewExists(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Bar
{
    public {|Definition:$$Bar|}() { }
}
        </Document>
        <Document>
public class Foo
{
    private readonly Bar bar1 = [|new|]();
    private readonly Bar bar2;
    public Foo(Bar bar)
    {
        this.bar2 = new [|Bar|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestConstructorReferenceInGlobalSuppression(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:D.[|#ctor|]")]

class D
{
    public {|Definition:$$D|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44288")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestStaticConstructorReferenceInGlobalSuppression(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "RuleId", Scope = "member", Target = "~M:D.[|#cctor|]")]

static class D
{
    static {|Definition:$$D|}() { }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorNotEnoughArguments(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x) { }
    void M()
    {
        D d = new();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorExactArgumentCount(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x) { }
    void M()
    {
        D d = [|new|](1);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorTooManyArguments(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x) { }
    void M()
    {
        D d = new(1, 2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorWithOptionalParam0(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x, int? y = null) { }
    void M()
    {
        D d = new();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorWithOptionalParam1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x, int? y = null) { }
    void M()
    {
        D d = [|new|](1);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorWithOptionalParam2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x, int? y = null) { }
    void M()
    {
        D d = [|new|](1, 2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorWithOptionalParam3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x, int? y = null) { }
    void M()
    {
        D d = new(1, 2, 3);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorWithParams0(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x, params int[] y) { }
    void M()
    {
        D d = new();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorWithParams1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x, params int[] y) { }
    void M()
    {
        D d = [|new|](1);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorWithParams2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x, params int[] y) { }
    void M()
    {
        D d = [|new|](1, 2);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorWithParams3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x, params int[] y) { }
    void M()
    {
        D d = [|new|](1, 2, 3);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestImplicitConstructorWithParams4(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class D
{
    {|Definition:$$D|}(int x, params int[] y) { }
    void M()
    {
        D d = [|new|](1, 2, 3, 4);
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/40848")>
        Public Async Function TestDottedConstructorUsage(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports Test

Module Module1
    Sub Main()
        Dim a = New Test.{|TypeOrNamespaceUsageInfo.ObjectCreation:[|$$TestClass|]|}()
        Dim b = New {|TypeOrNamespaceUsageInfo.ObjectCreation:[|TestClass|]|}()
    End Sub
End Module

Namespace Test
  Public Class TestClass
    Public Sub {|Definition:New|}()
    End Sub
  End Class
End Namespace]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11049")>
        Public Async Function TestImplicitBaseConstructorReference_VisualBasic1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
friend mustinherit class Abstract
    protected sub {|Definition:$$New|}()
    end sub
end class

friend class Derived
    inherits Abstract

    public sub [|New|]()
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11049")>
        Public Async Function TestImplicitBaseConstructorReference_VisualBasic2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
friend mustinherit class Abstract
    protected sub {|Definition:$$New|}()
    end sub
end class

friend class Derived
    public sub New()
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11049")>
        Public Async Function TestImplicitBaseConstructorReference_VisualBasic3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
friend mustinherit class Abstract
    protected sub {|Definition:$$New|}()
    end sub
end class

friend class Derived
    inherits Abstract

    public sub [|New|](i as integer)
    end sub

    public sub New()
        me.New(0)
    end sub
end class
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/60949")>
        Public Async Function TestImplicitObjectCreation(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class D
{
    void M()
    {
        C c1 = new {|TypeOrNamespaceUsageInfo.ObjectCreation:[|C|]|}();
        C c2 = {|TypeOrNamespaceUsageInfo.ObjectCreation:[|new|]|}();
    }
}

class C
{
    public {|Definition:$$C|}()
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/73704")>
        Public Async Function TestPrimaryConstructor1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class Program
        {
            public {|Definition:$$Program|}(int i)
            {
            }
        }

        class Derived() : [|Program|](0)
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/73704")>
        Public Async Function TestPrimaryConstructor2(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class Program
        {
            public {|Definition:$$Program|}(int i)
            {
            }
        }

        class Derived() : global::[|Program|](0)
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/73704")>
        Public Async Function TestPrimaryConstructor3(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N
        {
            class Program
            {
                public {|Definition:$$Program|}(int i)
                {
                }
            }
        }

        class Derived() : N.[|Program|](0)
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function PartialConstructor(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
        <Document>
using System;
partial class Program
{
    public partial {|Definition:Program|}();
    public partial {|Definition:P$$rogram|}() { }

    static void Main(string[] args)
    {
        var p = new [|Program|]();
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81767")>
        Public Async Function CollectionExpression_Constructor1(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNet9="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> mc = [|[]|];

public class MyCollection<T> : IEnumerable<T>
{
    public {|Definition:$$MyCollection|}() { }

    public IEnumerator<T> GetEnumerator() => null;
    public void Add(T item) { }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81767")>
        Public Async Function CollectionExpression_Constructor2(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNet9="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> mc1 = [];
MyCollection<int> mc2 = [|[with(1)]|];

public class MyCollection<T> : IEnumerable<T>
{
    public {|Definition:$$MyCollection|}(int capacity) { }

    public IEnumerator<T> GetEnumerator() => null;
    public void Add(T item) { }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81767")>
        Public Async Function CollectionExpression_Constructor3(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNet9="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> mc1 = [];
MyCollection<int> mc2 = [|[with(1)]|];

public class MyCollection<T> : IEnumerable<T>
{
    public MyCollection() { }
    public {|Definition:$$MyCollection|}(int capacity) { }

    public IEnumerator<T> GetEnumerator() => null;
    public void Add(T item) { }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81767")>
        Public Async Function CollectionExpression_Constructor4(host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferencesNet9="true" LanguageVersion="preview">
        <Document><![CDATA[
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

MyCollection<int> mc1 = [|[]|];
MyCollection<int> mc2 = [with(1)];

public class MyCollection<T> : IEnumerable<T>
{
    public {|Definition:$$MyCollection|}() { }
    public MyCollection(int capacity) { }

    public IEnumerator<T> GetEnumerator() => null;
    public void Add(T item) { }
}
        ]]></Document>
    </Project>
</Workspace>
            Await TestAPI(input, host)
        End Function
    End Class
End Namespace
