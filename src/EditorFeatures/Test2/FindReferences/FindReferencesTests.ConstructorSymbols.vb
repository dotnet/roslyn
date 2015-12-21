' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisConstructorInitializerSameFile1() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisConstructorInitializerSameFile2() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisConstructorInitializerDifferentFile1() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisConstructorInitializerDifferentFile2() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerSameFile1() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerSameFile2() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerDifferentFile1() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerDifferentFile2() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestBaseConstructorInitializerDifferentFile3() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(541658)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttribute1() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(541658)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttribute2() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(541658)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttribute3() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(541658)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttribute4() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestThisAtEndOfFile() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructor_ThroughAlias1() As Task
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
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N
        {
            class FooAttribute : System.Attribute
            {
                public {|Definition:$$FooAttribute|}() { }
            }
        }
        </Document>
        <Document>
        using M = N.FooAttribute;

        [[|M|]()]
        [[|M|]]
        class C
        {
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N
        {
            class FooAttribute : System.Attribute
            {
                public {|Definition:$$FooAttribute|}() { }
            }
        }
        </Document>
        <Document>
        using MAttribute = N.FooAttribute;

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
            Await TestAsync(input)
        End Function

        <WorkItem(542218)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias3() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542218)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias4() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542218)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias5() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542218)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias6() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542218)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias7() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542218)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias8() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542218)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias9() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(542979)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias10() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using FooAttribute = System.[|ObsoleteAttribute|];
 
[$$[|Foo|]]
[[|Foo|]()]
[[|FooAttribute|]]
[[|FooAttribute|]()]
class C { }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

#If False Then
        <WorkItem(10441)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAttributeConstructor_ThroughAlias11() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using FooAttribute = System.$$[|ObsoleteAttribute|];
 
[Foo]
[Foo()]
[FooAttribute]
[FooAttribute()]
class C { }
        </Document>
    </Project>
</Workspace>
        Await TestAsync(input)
        End Sub
#End If

        <WorkItem(542386)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestClassCalledNew1() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(531200)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpCascadeFromTypeToConstructorsAndDestructors() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(531200)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestVBCascadeFromTypeToConstructorsAndDestructors() As Task
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
            Await TestAsync(input)
        End Function

        <WorkItem(652809)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCSharpStaticCtorVsInstanceCtorReferences() As Task
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
            Await TestAsync(input)
        End Function
    End Class
End Namespace
