' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestThisConstructorInitializerSameFile1()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestThisConstructorInitializerSameFile2()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestThisConstructorInitializerDifferentFile1()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestThisConstructorInitializerDifferentFile2()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestBaseConstructorInitializerSameFile1()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestBaseConstructorInitializerSameFile2()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestBaseConstructorInitializerDifferentFile1()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestBaseConstructorInitializerDifferentFile2()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestBaseConstructorInitializerDifferentFile3()
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
            Test(input)
        End Sub

        <WorkItem(541658)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttribute1()
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
            Test(input)
        End Sub

        <WorkItem(541658)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttribute2()
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
            Test(input)
        End Sub

        <WorkItem(541658)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttribute3()
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
            Test(input)
        End Sub

        <WorkItem(541658)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttribute4()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestThisAtEndOfFile()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestConstructor_ThroughAlias1()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias1()
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
            Test(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias2()
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
            Test(input)
        End Sub

        <WorkItem(542218)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias3()
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
            Test(input)
        End Sub

        <WorkItem(542218)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias4()
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
            Test(input)
        End Sub

        <WorkItem(542218)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias5()
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
            Test(input)
        End Sub

        <WorkItem(542218)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias6()
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
            Test(input)
        End Sub

        <WorkItem(542218)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias7()
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
            Test(input)
        End Sub

        <WorkItem(542218)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias8()
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
            Test(input)
        End Sub

        <WorkItem(542218)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias9()
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
            Test(input)
        End Sub

        <WorkItem(542979)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias10()
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
            Test(input)
        End Sub

#If False Then
        <WorkItem(10441)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestAttributeConstructor_ThroughAlias11()
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
            Test(input)
        End Sub
#End If

        <WorkItem(542386)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestClassCalledNew1()
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
            Test(input)
        End Sub

        <WorkItem(531200)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCSharpCascadeFromTypeToConstructorsAndDestructors()
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
            Test(input)
        End Sub

        <WorkItem(531200)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestVBCascadeFromTypeToConstructorsAndDestructors()
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
            Test(input)
        End Sub

        <WorkItem(652809)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Sub TestCSharpStaticCtorVsInstanceCtorReferences()
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
            Test(input)
        End Sub
    End Class
End Namespace
