' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.CSharp.Syntax
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.GoToDefinition)>
    Public NotInheritable Class CSharpGoToDefinitionTests
        Inherits GoToDefinitionTestsBase
#Region "P2P Tests"

        <WpfFact>
        Public Async Function TestP2PClassReference() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
        using N;

        class CSharpClass
        {
            VB$$Class vb
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
        namespace N
            public class [|VBClass|]
            End Class
        End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

#Region "Normal CSharp Tests"

        <WpfFact>
        Public Async Function TestCSharpGoToDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { }
            class OtherClass { Some$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/23030")>
        Public Async Function TestCSharpLiteralGoToDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            int x = 1$$23;
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/23030")>
        Public Async Function TestCSharpStringLiteralGoToDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            string x = "wo$$ow";
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/3589")>
        Public Async Function TestCSharpGoToDefinitionOnAnonymousMember() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public class MyClass
{
    public string [|Prop1|] { get; set; }
}
class Program
{
    static void Main(string[] args)
    {
        var instance = new MyClass();

        var x = new
        {
            instance.$$Prop1
        };
    }
}        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToDefinitionSameClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { Some$$Class someObject; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToDefinitionNestedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Outer
            {
              class [|Inner|]
              {
              }

              In$$ner someObj;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionDifferentFiles() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class OtherClass { SomeClass obj; }
        </Document>
        <Document>
            class OtherClass2 { Some$$Class obj2; };
        </Document>
        <Document>
            class [|SomeClass|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionPartialClasses() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            partial class nothing { };
        </Document>
        <Document>
            partial class [|OtherClass|] { int a; }
        </Document>
        <Document>
            partial class [|OtherClass|] { int b; };
        </Document>
        <Document>
            class ConsumingClass { Other$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { int x; };
        </Document>
        <Document>
            class ConsumingClass
            {
                void goo()
                {
                    Some$$Class x;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/900438")>
        Public Async Function TestCSharpGotoDefinitionPartialMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            partial class Test
            {
                partial void M();
            }
        </Document>
        <Document>
            partial class Test
            {
                void Goo()
                {
                    var t = new Test();
                    t.M$$();
                }

                partial void [|M|]()
                {
                    throw new NotImplementedException();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionExtendedPartialMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            partial class Test
            {
                public partial void M();
            }
        </Document>
        <Document>
            partial class Test
            {
                void Goo()
                {
                    var t = new Test();
                    t.M$$();
                }

                public partial void [|M|]()
                {
                    throw new NotImplementedException();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionPartialProperty() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            partial class Test
            {
                public partial int Prop { get; set; }
            }
        </Document>
        <Document>
            partial class Test
            {
                void Goo()
                {
                    var t = new Test();
                    int i = t.Prop$$;
                }

                public partial void [|Prop|]
                {
                    get => throw new NotImplementedException();
                    set => throw new NotImplementedException();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnMethodCall1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void [|M|]() { }
                void M(int i) { }
                void M(int i, string s) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnMethodCall2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void M() { }
                void [|M|](int i, string s) { }
                void M(int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0, "text");
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnMethodCall3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void [|M|](int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnMethodCall4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void M(int i) { }
                void [|M|](string s, int i) { }

                void Call()
                {
                    $$M("text", 0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnConstructor1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|C|]
            {
                C() { }

                $$C c = new C();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(3376, "DevDiv_Projects/Roslyn")>
        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnConstructor2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                [|C|]() { }

                C c = new $$C();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionWithoutExplicitConstruct() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|C|]
            {
                void Method()
                {
                    C c = new $$C();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnLocalVariable1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void method()
                {
                    int [|x|] = 2, y, z = $$x * 2;
                    y = 10;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnLocalVariable2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void method()
                {
                    int x = 2, [|y|], z = x * 2;
                    $$y = 10;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnLocalField() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                int [|_X|] = 1, _Y;
                void method()
                {
                    _$$X = 8;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnAttributeClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            [FlagsAttribute]
            class [|C|]
            {
                $$C c;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTouchLeft() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|]
            {
                $$SomeClass c;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTouchRight() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|]
            {
                SomeClass$$ c;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionOnGenericTypeParameterInPresenceOfInheritedNestedTypeWithSameName() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            class B
            {
                public class T { }
            }
            class C<[|T|]> : B
            {
                $$T x;
            }]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538765")>
        Public Async Function TestCSharpGotoDefinitionThroughOddlyNamedType() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            class [|dynamic|] { }
            class C : dy$$namic { }
        ]]></Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToDefinitionOnConstructorInitializer1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private int v;
    public Program() : $$this(4)
    {
    }

    public [|Program|](int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToDefinitionOnExtensionMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
           class Program
           {
               static void Main(string[] args)
               {
                    "1".$$TestExt();
               }
           }

           public static class Ex
           {
              public static void TestExt<T>(this T ex) { }
              public static void [|TestExt|](this string ex) { }
           }]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542004")>
        Public Async Function TestCSharpTestLambdaParameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    delegate int D2(int i, int j);
    static void Main()
    {
        D2 d = (int [|i1|], int i2) => { return $$i1 + i2; };
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpTestLabel() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M()
    {
    [|Goo|]:
        int Goo;
        goto $$Goo;
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToDefinitionFromCref() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            /// <see cref="$$SomeClass"/>
            class [|SomeClass|]
            {
            }]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16529")>
        Public Async Function TestCSharpGoToOverriddenDefinition_FromDeconstructionDeclaration() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Two { public void Deconstruct(out int x1, out int x2) => throw null; }
class Four { public void [|Deconstruct|](out int x1, out int x2, out Two x3) => throw null; }
class C
{
    void M(Four four)
    {
        var (a, b, (c, d)) $$= four;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16529")>
        Public Async Function TestCSharpGoToOverriddenDefinition_FromDeconstructionAssignment() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Two { public void Deconstruct(out int x1, out int x2) => throw null; }
class Four { public void [|Deconstruct|](out int x1, out int x2, out Two x3) => throw null; }
class C
{
    void M(Four four)
    {
        int i;
        (i, i, (i, i)) $$= four;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16529")>
        Public Async Function TestCSharpGoToOverriddenDefinition_FromDeconstructionForeach() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Two { public void Deconstruct(out int x1, out int x2) => throw null; }
class Four { public void [|Deconstruct|](out int x1, out int x2, out Two x3) => throw null; }
class C
{
    void M(Four four)
    {
        foreach (var (a, b, (c, d)) $$in new[] { four }) { }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOverriddenDefinition_FromOverride() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Origin { public virtual void Method() { } }
            class Base : Origin { public override void [|Method|]() { } }
            class Derived : Base { }
            class Derived2 : Derived { public ove$$rride void Method() { }  }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOverriddenDefinition_FromOverride2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Origin { public virtual void [|Method|]() { } }
            class Base : Origin { public ove$$rride void Method() { } }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOverriddenProperty_FromOverride() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Origin { public virtual int [|Property|] { get; set; } }
            class Base : Origin { public ove$$rride int Property { get; set; } }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToUnmanaged_Keyword() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C&lt;T&gt; where T : un$$managed
            {
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToUnmanaged_Type() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            interface [|unmanaged|]
            {
            }
            class C&lt;T&gt; where T : un$$managed
            {
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/41870")>
        Public Async Function TestCSharpGoToImplementedInterfaceMemberFromImpl1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IFoo1 { void [|Bar|](); }

class Foo : IFoo1
{
    public void $$Bar()
    {

    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/41870")>
        Public Async Function TestCSharpGoToImplementedInterfaceMemberFromImpl2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IFoo1 { void [|Bar|](); }

class Foo : IFoo1
{
    void IFoo1.$$Bar()
    {

    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/41870")>
        Public Async Function TestCSharpGoToImplementedInterfaceMemberFromImpl3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
interface IFoo1 { void [|Bar|](); }
interface IFoo2 { void [|Bar|](); }

class Foo : IFoo1, IFoo2
{
    public void $$Bar()
    {

    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/51615")>
        Public Async Function TestCSharpGoToDefinitionInVarPatterns() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class [|C|] { }

class D
{
    C M() => new C();

    void M2()
    {
      if (M() is var$$ x)
      {
      }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function
#End Region

#Region "CSharp TupleTests"
        Private ReadOnly tuple2 As XCData =
        <![CDATA[
namespace System
{
    // struct with two values
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public override string ToString()
        {
            return '{' + Item1?.ToString() + "", "" + Item2?.ToString() + '}';
        }
    }
}
]]>

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldEqualTuples01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            var x = ([|Alice|]: 1, Bob: 2);

            var y = (Alice: 1, Bob: 2);

            var z1 = x.$$Alice;
            var z2 = y.Alice;
        }
    }

        <%= tuple2 %>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldEqualTuples02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <!-- intentionally not including tuple2, should still work -->
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            var x = (Alice: 1, Bob: 2);

            var y = ([|Alice|]: 1, Bob: 2);

            var z1 = x.Alice;
            var z2 = y.$$Alice;
        }
    }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldMatchToOuter01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            var x = ([|Program|]: 1, Main: 2);

            var z = x.$$Program;
        }
    }

        <%= tuple2 %>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldMatchToOuter02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            var x = ([|Pro$$gram|]: 1, Main: 2);

            var z = x.Program;
        }
    }

        <%= tuple2 %>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldMatchToOuter03() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            var x = (1,2,3,4,5,6,7,8,9,10, [|Program|]: 1, Main: 2);

            var z = x.$$Program;
        }
    }

        <%= tuple2 %>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldRedeclared01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            (int [|Alice|], int Bob) x = (Alice: 1, Bob: 2);

             var z1 = x.$$Alice;
        }
    }

        <%= tuple2 %>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldRedeclared02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            (string Alice, int Bob) x = ([|Al$$ice|]: null, Bob: 2);

             var z1 = x.Alice;
        }
    }

        <%= tuple2 %>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldItem01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            var x = ([|1|], Bob: 2);

             var z1 = x.$$Item1;
        }
    }

        <%= tuple2 %>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldItem02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            var x = ([|Alice|]: 1, Bob: 2);

             var z1 = x.$$Item1;
        }
    }

        <%= tuple2 %>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGotoDefinitionTupleFieldItem03() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
    class Program
    {
        static void Main(string[] args)
        {
            System.ValueTuple&lt;short, short&gt; x = (1, Bob: 2);

            var z1 = x.$$Item1;
        }
    }

        <%= tuple2 %>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/71680")>
        <InlineData("ValueTuple<int> valueTuple1;")>
        <InlineData("ValueTuple<int, int> valueTuple2;")>
        <InlineData("ValueTuple<int, int, int> valueTuple3;")>
        <InlineData("ValueTuple<int, int, int, int> valueTuple4;")>
        <InlineData("ValueTuple<int, int, int, int, int> valueTuple5;")>
        <InlineData("ValueTuple<int, int, int, int, int, int> valueTuple6;")>
        <InlineData("ValueTuple<int, int, int, int, int, int, int> valueTuple7;")>
        <InlineData("ValueTuple<int, int, int, int, int, int, int, int> valueTuple8;")>
        Public Async Function TestCSharpGotoDefinitionWithValueTuple(expression As String) As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj">
                        <Document FilePath="C.cs">
                            using System;

                            class C
                            {
                                void M()
                                {
                                    $$<%= expression %>
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Await TestAsync(workspace)
        End Function
#End Region

#Region "CSharp Venus Tests"

        <WpfFact>
        Public Async Function TestCSharpVenusGotoDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            #line 1 "CSForm1.aspx"
            public class [|_Default|]
            {
               _Defa$$ult a;
            #line default
            #line hidden
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        Public Async Function TestCSharpFilterGotoDefResultsFromHiddenCodeForUIPresenters() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            public class [|_Default|]
            {
            #line 1 "CSForm1.aspx"
               _Defa$$ult a;
            #line default
            #line hidden
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        Public Async Function TestCSharpDoNotFilterGotoDefResultsFromHiddenCodeForApis() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            public class [|_Default|]
            {
            #line 1 "CSForm1.aspx"
               _Defa$$ult a;
            #line default
            #line hidden
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#End Region

#Region "CSharp Script Tests"

        <WpfFact>
        Public Async Function TestCSharpScriptGoToDefinition() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { }
            class OtherClass { Some$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpScriptGoToDefinitionSameClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { Some$$Class someObject; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpScriptGoToDefinitionNestedClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class Outer
            {
              class [|Inner|]
              {
              }

              In$$ner someObj;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpScriptGotoDefinitionDifferentFiles() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class OtherClass { SomeClass obj; }
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class OtherClass2 { Some$$Class obj2; };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpScriptGotoDefinitionPartialClasses() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            partial class nothing { };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            partial class [|OtherClass|] { int a; }
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            partial class [|OtherClass|] { int b; };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class ConsumingClass { Other$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpScriptGotoDefinitionMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { int x; };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class ConsumingClass
            {
                void goo()
                {
                    Some$$Class x;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpScriptGotoDefinitionOnMethodCall1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void [|M|]() { }
                void M(int i) { }
                void M(int i, string s) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M();
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpScriptGotoDefinitionOnMethodCall2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void M() { }
                void [|M|](int i, string s) { }
                void M(int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0, "text");
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function
        <WpfFact>
        Public Async Function TestCSharpScriptGotoDefinitionOnMethodCall3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void [|M|](int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpScriptGotoDefinitionOnMethodCall4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void M(int i) { }
                void [|M|](string s, int i) { }

                void Call()
                {
                    $$M("text", 0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/989476")>
        Public Async Function TestCSharpDoNotFilterGeneratedSourceLocations() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Nongenerated.cs">
partial class [|C|]
{
    void M()
    {
        $$C c;
    }
}
        </Document>
        <Document FilePath="Generated.g.i.cs">
partial class [|C|]
{
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/989476")>
        Public Async Function TestCSharpUseGeneratedSourceLocationsIfNoNongeneratedLocationsAvailable() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Generated.g.i.cs">
class [|C|]
{
}
        </Document>
        <Document FilePath="Nongenerated.g.i.cs">
class D
{
    void M()
    {
        $$C c;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(workspace)
        End Function

#End Region

        <WpfFact>
        Public Async Function TestCallingConv() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
unsafe
{
    delegate* unmanaged[$$Cdecl]&lt;void&gt; f1;
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCallingConvWithAtSign() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
unsafe
{
    delegate* unmanaged[$$@Cdecl]&lt;void&gt; f1;
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        Public Async Function TestCSharpTestAliasAndTarget1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using [|AliasedSomething|] = X.Something;

namespace X
{
    class Something { public Something() { } }
}

class Program
{
    static void Main(string[] args)
    {
        $$AliasedSomething x = new AliasedSomething();
        X.Something y = new X.Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        Public Async Function TestCSharpTestAliasAndTarget2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using [|AliasedSomething|] = X.Something;

namespace X
{
    class Something { public Something() { } }
}

class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new $$AliasedSomething();
        X.Something y = new X.Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        Public Async Function TestCSharpTestAliasAndTarget3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.Something;

namespace X
{
    class [|Something|] { public Something() { } }
}

class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new AliasedSomething();
        X.$$Something y = new X.Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        Public Async Function TestCSharpTestAliasAndTarget4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.Something;

namespace X
{
    class Something { public [|Something|]() { } }
}

class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new AliasedSomething();
        X.Something y = new X.$$Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#Region "Show notification tests"

        <WpfFact>
        Public Async Function TestShowNotificationCS() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class SomeClass { }
            cl$$ass OtherClass
            {
                SomeClass obj;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546341")>
        Public Async Function TestGoToDefinitionOnGlobalKeyword() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                gl$$obal::System.String s;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function
#End Region

#Region "CSharp Query expressions Tests"

        Private Shared Function GetExpressionPatternDefinition(highlight As String, Optional index As Integer = 0) As String
            Dim definition As String =
"
using System;
namespace QueryPattern
{
    public class C
    {
        public C<T> Cast<T>() => throw new NotImplementedException();
    }

    public class C<T> : C
    {
        public C<T> Where(Func<T, bool> predicate) => throw new NotImplementedException();
        public C<U> Select<U>(Func<T, U> selector) => throw new NotImplementedException();
        public C<V> SelectMany<U, V>(Func<T, C<U>> selector, Func<T, U, V> resultSelector) => throw new NotImplementedException();
        public C<V> Join<U, K, V>(C<U> inner, Func<T, K> outerKeySelector, Func<U, K> innerKeySelector, Func<T, U, V> resultSelector) => throw new NotImplementedException();
        public C<V> GroupJoin<U, K, V>(C<U> inner, Func<T, K> outerKeySelector, Func<U, K> innerKeySelector, Func<T, C<U>, V> resultSelector) => throw new NotImplementedException();
        public O<T> OrderBy<K>(Func<T, K> keySelector) => throw new NotImplementedException();
        public O<T> OrderByDescending<K>(Func<T, K> keySelector) => throw new NotImplementedException();
        public C<G<K, T>> GroupBy<K>(Func<T, K> keySelector) => throw new NotImplementedException();
        public C<G<K, E>> GroupBy<K, E>(Func<T, K> keySelector, Func<T, E> elementSelector) => throw new NotImplementedException();
    }

    public class O<T> : C<T>
    {
        public O<T> ThenBy<K>(Func<T, K> keySelector) => throw new NotImplementedException();
        public O<T> ThenByDescending<K>(Func<T, K> keySelector) => throw new NotImplementedException();
    }

    public class G<K, T> : C<T>
    {
        public K Key { get; }
    }
}
"
            If highlight = "" Then
                Return definition
            End If

            Dim searchStartPosition As Integer = 0
            Dim searchFound As Integer
            For i As Integer = 0 To index
                searchFound = definition.IndexOf(highlight, searchStartPosition)
                If searchFound < 0 Then
                    Exit For
                End If
            Next

            If searchFound >= 0 Then
                definition = definition.Insert(searchFound + highlight.Length, "|]")
                definition = definition.Insert(searchFound, "[|")
                Return definition
            End If

            Throw New InvalidOperationException("Highlight not found")
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQuerySelect() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Select") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i in new C<int>()
                  $$select i;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryWhere() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Where") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i in new C<int>()
                  $$where true
                  select i;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQuerySelectMany1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("SelectMany") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  $$from i2 in new C<int>()
                  select i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQuerySelectMany2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("SelectMany") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  from i2 $$in new C<int>()
                  select i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryJoin1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Join") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  $$join i2 in new C<int>() on i2 equals i1
                  select i2;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryJoin2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Join") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  join i2 $$in new C<int>() on i1 equals i2
                  select i2;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryJoin3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Join") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  join i2 in new C<int>() $$on i1 equals i2
                  select i2;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryJoin4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Join") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  join i2 in new C<int>() on i1 $$equals i2
                  select i2;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryGroupJoin1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("GroupJoin") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  $$join i2 in new C<int>() on i1 equals i2 into g
                  select g;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryGroupJoin2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("GroupJoin") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  join i2 $$in new C<int>() on i1 equals i2 into g
                  select g;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryGroupJoin3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("GroupJoin") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  join i2 in new C<int>() $$on i1 equals i2 into g
                  select g;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryGroupJoin4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("GroupJoin") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  join i2 in new C<int>() on i1 $$equals i2 into g
                  select g;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryGroupBy1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("GroupBy") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  $$group i1 by i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryGroupBy2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("GroupBy") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  group i1 $$by i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryFromCast1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Cast") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = $$from int i1 in new C<int>()
                  select i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryFromCast2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Cast") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from int i1 $$in new C<int>()
                  select i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryJoinCast1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Cast") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  join int i2 $$in new C<int>() on i1 equals i2
                  select i2;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryJoinCast2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Join") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  $$join int i2 in new C<int>() on i1 equals i2
                  select i2;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQuerySelectManyCast1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Cast") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  from int i2 $$in new C<int>()
                  select i2;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQuerySelectManyCast2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("SelectMany") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  $$from int i2 in new C<int>()
                  select i2;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryOrderBySingleParameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("OrderBy") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  $$orderby i1
                  select i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryOrderBySingleParameterWithOrderClause() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("OrderByDescending") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  orderby i1 $$descending
                  select i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryOrderByTwoParameterWithoutOrderClause() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("ThenBy") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  orderby i1,$$ i2
                  select i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryOrderByTwoParameterWithOrderClause() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("ThenByDescending") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  orderby i1, i2 $$descending
                  select i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryDegeneratedSelect() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  where true
                  $$select i1;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, False)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/23049")>
        Public Async Function TestQueryLet() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="QueryPattern">
        <Document>
            <%= GetExpressionPatternDefinition("Select") %>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpProj">
        <ProjectReference>QueryPattern</ProjectReference>
        <Document>
            <![CDATA[
using QueryPattern;
class Test
{
    static void M()
    {
        var qry = from i1 in new C<int>()
                  $$let i2=1
                  select new { i1, i2 };
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function
#End Region

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInSwitchStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s:
                bre$$ak;
            default:
                return;
        }[||]
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnContinueInSwitchStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s:
                cont$$inue;
            default:
                return;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInDoStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        do
        {
            bre$$ak;
        }
        while (true)[||]
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnContinueInDoStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        [||]do
        {
            cont$$inue;
        }
        while (true);
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInForStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        for (int i = 0; ; )
        {
            bre$$ak;
        }[||]
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnContinueInForStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        [||]for (int i = 0; ; )
        {
            cont$$inue;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInForeachStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        foreach (int i in null)
        {
            bre$$ak;
        }[||]
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnContinueInForeachStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        [||]foreach (int i in null)
        {
            cont$$inue;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInForeachVariableStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        foreach (var (i, j) in null)
        {
            bre$$ak;
        }[||]
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnContinueInForeachVariableStatement() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        [||]foreach (var (i, j) in null)
        {
            cont$$inue;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnContinueInSwitchInForeach() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        [||]foreach (var (i, j) in null)
        {
            switch (1)
            {
                default:
                    cont$$inue;
            }
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnTopLevelContinue() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
cont$$inue;
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInParenthesizedLambda() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s:
                System.Action a = () => { bre$$ak; };
                break;
            default:
                return;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInSimpleLambda() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s:
                System.Action a = _ => { bre$$ak; };
                break;
            default:
                return;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInLocalFunction() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M(object o)
    {
        switch (o)
        {
            case string s:
                void local()
                {
                    System.Action a = _ => { bre$$ak; };
                }
                break;
            default:
                return;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        bre$$ak;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnBreakInAccessor() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    int Property
    {
        set { bre$$ak; }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnReturnInVoidMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void [||]M()
    {
        for (int i = 0; ; )
        {
            return$$;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnReturnInIntMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    [MyAttribute]
    int [||]M()
    {
        for (int i = 0; ; )
        {
            return$$ 1;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnReturnInVoidLambda() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    int M()
    {
        System.Action a = [||]() =>
        {
            for (int i = 0; ; )
            {
                return$$;
            }
        };
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnReturnedExpression() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    int M()
    {
        for (int [|i|] = 0; ; )
        {
            return $$i;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnReturnedConstantExpression() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    int M()
    {
        for (int i = 0; ; )
        {
            return $$1;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnYieldReturn_Return() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    IEnumerable [||]M()
    {
        yield return$$ 1;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnYieldReturn_Yield() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    IEnumerable [||]M()
    {
        yield$$ return 1;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnYieldReturn_Yield_Partial() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    partial IEnumerable M();

    partial IEnumerable [||]M()
    {
        yield$$ return 1;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnYieldReturn_Yield_Partial_ReverseOrder() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    partial IEnumerable [||]M()
    {
        yield$$ return 1;
    }

    partial IEnumerable M();
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnYieldBreak_Yield() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    IEnumerable [||]M()
    {
        yield$$ break;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestCSharpGoToOnYieldBreak_Break() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    IEnumerable [||]M()
    {
        yield break$$;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function ExtendedPropertyPattern_FirstPart() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    C [|CProperty|] { get; set; }
    int IntProperty { get; set; }

    void M()
    {
        _ = this is { CProper$$ty.IntProperty: 1 };
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function ExtendedPropertyPattern_SecondPart() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    C CProperty { get; set; }
    int [|IntProperty|] { get; set; }

    void M()
    {
        _ = this is { CProperty.IntProp$$erty: 1 };
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TopLevelStatements_EmptySpace() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

Console.WriteLine(1);

$$
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace, expectedResult:=False)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/37842")>
        Public Async Function TestCSharpGoToDefOnUsing1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    static void Goo()
    {
        $$using (IDisposable disposableObject = new DisposableObject())
        {
            //...
        }
    }
}

class DisposableObject : IDisposable
{
    public void [|Dispose|]() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/37842")>
        Public Async Function TestCSharpGoToDefOnUsing2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    static void Goo()
    {
        $$using (new DisposableObject())
        {
            //...
        }
    }
}

class DisposableObject : IDisposable
{
    public void [|Dispose|]() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/37842")>
        Public Async Function TestCSharpGoToDefOnUsing3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class Program
{
    static void Goo()
    {
        $$using IDisposable disposableObject = new DisposableObject();
    }
}

class DisposableObject : IDisposable
{
    public void [|Dispose|]() { }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/37842")>
        Public Async Function TestCSharpGoToDefOnUsing4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferencesNet6Name="true">
        <Document>
using System;
using System.Threading.Tasks;

class Program
{
    static void Goo()
    {
        await $$using (IAsyncDisposable disposableObject = new DisposableObject())
        {
            //...
        }
    }
}

class DisposableObject : IAsyncDisposable
{
    public ValueTask [|DisposeAsync|]() { }
}

namespace System
{
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/37842")>
        Public Async Function TestCSharpGoToDefOnUsing5() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferencesNet6Name="true">
        <Document>
using System;
using System.Threading.Tasks;

class Program
{
    static void Goo()
    {
        await $$using (new DisposableObject())
        {
            //...
        }
    }
}

class DisposableObject : IAsyncDisposable
{
    public ValueTask [|DisposeAsync|]() { }
}

namespace System
{
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/37842")>
        Public Async Function TestCSharpGoToDefOnUsing6() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferencesNet6Name="true">
        <Document>
using System;
using System.Threading.Tasks;

class Program
{
    static void Goo()
    {
        await $$using IAsyncDisposable disposableObject = new DisposableObject();
    }
}

class DisposableObject : IAsyncDisposable
{
    public ValueTask [|DisposeAsync|]() { }
}

namespace System
{
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69916")>
        Public Async Function TestCSharpGoToDefinition_GotoLabel01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document>
class Program
{
    public void Compare(string[] a, string[] b)
    {
        foreach (var x in a)
        {
            foreach (var y in b)
            {
                if (x.Length > y.Length)
                    $$goto end;
            }
        }
    [||]end:
        ;
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69916")>
        Public Async Function TestCSharpGoToDefinition_GotoSwitchDefaultLabel01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document>
class Program
{
    public void Method(int argument)
    {
        switch (argument)
        {
            case 1:
                $$goto default;
            case 2:
                goto case 1;
            [||]default:
                break;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69916")>
        Public Async Function TestCSharpGoToDefinition_GotoSwitchDefaultLabel02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document>
class Program
{
    public void Method(int argument)
    {
        switch (argument)
        {
            case 1:
                goto $$default;
            case 2:
                goto case 1;
            [||]default:
                break;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69916")>
        Public Async Function TestCSharpGoToDefinition_GotoSwitchCaseLabel01() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document>
class Program
{
    public void Method(int argument)
    {
        switch (argument)
        {
            [||]case 1:
                goto default;
            case 2:
                $$goto case 1;
            default:
                break;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69916")>
        Public Async Function TestCSharpGoToDefinition_GotoSwitchCaseLabel02() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document>
class Program
{
    public void Method(int argument)
    {
        switch (argument)
        {
            [||]case 1:
                goto default;
            case 2:
                goto $$case 1;
            default:
                break;
        }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestInterceptors_AttributeMissingVersion() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document FilePath="C.cs">
partial partial class Program
{
    public void Method(int argument)
    {
        Goo(0);
    }
}

        <%= s_interceptsLocationCode %>
        </Document>
        <Document FilePath="Generated.cs">
using System.Runtime.CompilerServices;

partial partial class Program
{
    [InterceptsLocationAttribute("")]
    public void $$[|Method|](int argument)
    {
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestInterceptors_UnsupportedVersion() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document FilePath="C.cs">
partial partial class Program
{
    public void Method(int argument)
    {
        Goo(0);
    }
}
        <%= s_interceptsLocationCode %>
        </Document>
        <Document FilePath="Generated.cs">
using System.Runtime.CompilerServices;

partial partial class Program
{
    [InterceptsLocationAttribute(-1, "")]
    public void $$[|Method|](int argument)
    {
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestInterceptors_EmptyData() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document FilePath="C.cs">
partial partial class Program
{
    public void Method(int argument)
    {
        Goo(0);
    }
}
        <%= s_interceptsLocationCode %>
        </Document>
        <Document FilePath="Generated.cs">
using System.Runtime.CompilerServices;

partial partial class Program
{
    [InterceptsLocationAttribute(1, "")]
    public void $$[|Method|](int argument)
    {
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestInterceptors_BogusData() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document FilePath="C.cs">
partial partial class Program
{
    public void Method(int argument)
    {
        Goo(0);
    }
}
        <%= s_interceptsLocationCode %>
        </Document>
        <Document FilePath="Generated.cs">
using System.Runtime.CompilerServices;

partial partial class Program
{
    [InterceptsLocationAttribute(1, "*")]
    public void $$[|Method|](int argument)
    {
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WpfFact>
        Public Async Function TestInterceptors_JustPadding() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#">
        <Document FilePath="C.cs">
partial partial class Program
{
    public void Method(int argument)
    {
        Goo(0);
    }
}
        <%= s_interceptsLocationCode %>
        </Document>
        <Document FilePath="Generated.cs">
using System.Runtime.CompilerServices;

partial partial class Program
{
    [InterceptsLocationAttribute(1, "=")]
    public void $$[|Method|](int argument)
    {
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

#Disable Warning RSEXPERIMENTAL002 ' Type is for evaluation purposes only and is subject to change or removal in future updates.

        Private Const s_interceptsLocationCode = "
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class InterceptsLocationAttribute : System.Attribute
    {
        public InterceptsLocationAttribute(int version, string data) { }
    }
}"

        Private Async Function TestInterceptor(code As String, getInvocations As Func(Of SyntaxNode, IEnumerable(Of InvocationExpressionSyntax))) As Task
            Dim firstFileContents = code & s_interceptsLocationCode

            Dim primordialWorkspace =
<Workspace>
    <Project Language="C#">
        <Document FilePath="C.cs"><%= firstFileContents %></Document>
    </Project>
</Workspace>

            Using testWorkspace = EditorTestWorkspace.Create(primordialWorkspace, composition:=GoToTestHelpers.Composition)
                Dim solution = testWorkspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim document = project.Documents.Single()

                Dim root = Await document.GetSyntaxRootAsync()
                Dim invocations = getInvocations(root)

                Dim semanticModel = Await document.GetSemanticModelAsync()
                Dim attributeText = ""

                For Each invocation In invocations
                    Dim location = semanticModel.GetInterceptableLocation(invocation)
                    attributeText += location.GetInterceptsLocationAttributeSyntax() & vbCrLf
                Next

                Dim finalWorkspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="C.cs"><%= firstFileContents %></Document>
        <Document FilePath="Generated.cs">
public partial class Program
{
    <%= attributeText %>public void $$Method()
    {
    }
}
        </Document>
    </Project>
</Workspace>

                Await TestAsync(finalWorkspace)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestInterceptors_SingleCaller() As Task
            Await TestInterceptor("
public partial class Program
{
    public void Method(int argument)
    {
        [|Goo|](0);
    }
}", Function(root) root.DescendantNodes().OfType(Of InvocationExpressionSyntax))
        End Function

        <WpfFact>
        Public Async Function TestInterceptors_SingleInterceptorForMultipleLocations() As Task
            Await TestInterceptor("
public partial class Program
{
    public void Method1()
    {
        {|PresenterLocation:Goo|}(0);
    }

    public void Method2()
    {
        this.{|PresenterLocation:Goo|}(1);
    }
}", Function(root) root.DescendantNodes().OfType(Of InvocationExpressionSyntax))
        End Function

#Enable Warning RSEXPERIMENTAL002 ' Type is for evaluation purposes only and is subject to change or removal in future updates.
    End Class
End Namespace
