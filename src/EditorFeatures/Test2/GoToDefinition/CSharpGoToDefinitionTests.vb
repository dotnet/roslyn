' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
    <[UseExportProvider]>
    Public Class CSharpGoToDefinitionTests
        Inherits GoToDefinitionTestsBase
#Region "P2P Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestP2PClassReference()
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

            Test(workspace)
        End Sub

#End Region

#Region "Normal CSharp Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToDefinition()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { }
            class OtherClass { Some$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        <WorkItem(23030, "https://github.com/dotnet/roslyn/issues/23030")>
        Public Sub TestCSharpLiteralGoToDefinition()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            int x = 1$$23;
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        <WorkItem(23030, "https://github.com/dotnet/roslyn/issues/23030")>
        Public Sub TestCSharpStringLiteralGoToDefinition()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            string x = "wo$$ow";
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(3589, "https://github.com/dotnet/roslyn/issues/3589")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToDefinitionOnAnonymousMember()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToDefinitionSameClass()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { Some$$Class someObject; }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToDefinitionNestedClass()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionDifferentFiles()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionPartialClasses()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionMethod()
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

            Test(workspace)
        End Sub

        <WorkItem(900438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/900438")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionPartialMethod()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionExtendedPartialMethod()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnMethodCall1()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnMethodCall2()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnMethodCall3()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnMethodCall4()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnConstructor1()
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

            Test(workspace)
        End Sub

        <WorkItem(3376, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnConstructor2()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionWithoutExplicitConstruct()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnLocalVariable1()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnLocalVariable2()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnLocalField()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnAttributeClass()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTouchLeft()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTouchRight()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionOnGenericTypeParameterInPresenceOfInheritedNestedTypeWithSameName()
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

            Test(workspace)
        End Sub

        <WorkItem(538765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538765")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionThroughOddlyNamedType()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            class [|dynamic|] { }
            class C : dy$$namic { }
        ]]></Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToDefinitionOnConstructorInitializer1()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToDefinitionOnExtensionMethod()
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

            Test(workspace)
        End Sub

        <WorkItem(542004, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542004")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpTestLambdaParameter()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpTestLabel()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToDefinitionFromCref()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        <WorkItem(16529, "https://github.com/dotnet/roslyn/issues/16529")>
        Public Sub TestCSharpGoToOverriddenDefinition_FromDeconstructionDeclaration()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        <WorkItem(16529, "https://github.com/dotnet/roslyn/issues/16529")>
        Public Sub TestCSharpGoToOverriddenDefinition_FromDeconstructionAssignment()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        <WorkItem(16529, "https://github.com/dotnet/roslyn/issues/16529")>
        Public Sub TestCSharpGoToOverriddenDefinition_FromDeconstructionForeach()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOverriddenDefinition_FromOverride()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOverriddenDefinition_FromOverride2()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Origin { public virtual void [|Method|]() { } }
            class Base : Origin { public ove$$rride void Method() { } }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOverriddenProperty_FromOverride()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Origin { public virtual int [|Property|] { get; set; } }
            class Base : Origin { public ove$$rride int Property { get; set; } }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToUnmanaged_Keyword()
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

            Test(workspace, expectedResult:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToUnmanaged_Type()
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

            Test(workspace)
        End Sub

        <WorkItem(41870, "https://github.com/dotnet/roslyn/issues/41870")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToImplementedInterfaceMemberFromImpl1()
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

            Test(workspace)
        End Sub

        <WorkItem(41870, "https://github.com/dotnet/roslyn/issues/41870")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToImplementedInterfaceMemberFromImpl2()
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

            Test(workspace)
        End Sub

        <WorkItem(41870, "https://github.com/dotnet/roslyn/issues/41870")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToImplementedInterfaceMemberFromImpl3()
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

            Test(workspace)
        End Sub

        <WorkItem(51615, "https://github.com/dotnet/roslyn/issues/51615")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToDefinitionInVarPatterns()
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

            Test(workspace)
        End Sub
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldEqualTuples01()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldEqualTuples02()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldMatchToOuter01()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldMatchToOuter02()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldMatchToOuter03()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldRedeclared01()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldRedeclared02()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldItem01()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldItem02()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGotoDefinitionTupleFieldItem03()
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

            Test(workspace, expectedResult:=False)
        End Sub
#End Region

#Region "CSharp Venus Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpVenusGotoDefinition()
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

            Test(workspace)
        End Sub

        <WorkItem(545324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpFilterGotoDefResultsFromHiddenCodeForUIPresenters()
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

            Test(workspace)
        End Sub

        <WorkItem(545324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpDoNotFilterGotoDefResultsFromHiddenCodeForApis()
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

            Test(workspace)
        End Sub

#End Region

#Region "CSharp Script Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGoToDefinition()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGoToDefinitionSameClass()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { Some$$Class someObject; }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGoToDefinitionNestedClass()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGotoDefinitionDifferentFiles()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGotoDefinitionPartialClasses()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGotoDefinitionMethod()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGotoDefinitionOnMethodCall1()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGotoDefinitionOnMethodCall2()
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

            Test(workspace)
        End Sub
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGotoDefinitionOnMethodCall3()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpScriptGotoDefinitionOnMethodCall4()
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

            Test(workspace)
        End Sub

        <WorkItem(989476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/989476")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpDoNotFilterGeneratedSourceLocations()
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
            Test(workspace)
        End Sub

        <WorkItem(989476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/989476")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpUseGeneratedSourceLocationsIfNoNongeneratedLocationsAvailable()
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
            Test(workspace)
        End Sub

#End Region

        <WorkItem(542220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpTestAliasAndTarget1()
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

            Test(workspace)
        End Sub

        <WorkItem(542220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpTestAliasAndTarget2()
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

            Test(workspace)
        End Sub

        <WorkItem(542220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpTestAliasAndTarget3()
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

            Test(workspace)
        End Sub

        <WorkItem(542220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542220")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpTestAliasAndTarget4()
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

            Test(workspace)
        End Sub

#Region "Show notification tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestShowNotificationCS()
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

            Test(workspace, expectedResult:=False)
        End Sub

        <WorkItem(546341, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546341")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestGoToDefinitionOnGlobalKeyword()
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

            Test(workspace, expectedResult:=False)
        End Sub
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

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQuerySelect()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryWhere()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQuerySelectMany1()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQuerySelectMany2()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryJoin1()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryJoin2()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryJoin3()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryJoin4()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryGroupJoin1()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryGroupJoin2()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryGroupJoin3()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryGroupJoin4()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryGroupBy1()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryGroupBy2()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryFromCast1()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryFromCast2()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryJoinCast1()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryJoinCast2()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQuerySelectManyCast1()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQuerySelectManyCast2()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryOrderBySingleParameter()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryOrderBySingleParameterWithOrderClause()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryOrderByTwoParameterWithoutOrderClause()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryOrderByTwoParameterWithOrderClause()
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

            Test(workspace)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryDegeneratedSelect()
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

            Test(workspace, False)
        End Sub

        <WorkItem(23049, "https://github.com/dotnet/roslyn/pull/23049")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestQueryLet()
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

            Test(workspace)
        End Sub
#End Region

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInSwitchStatement()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnContinueInSwitchStatement()
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

            Test(workspace, expectedResult:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInDoStatement()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnContinueInDoStatement()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInForStatement()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnContinueInForStatement()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInForeachStatement()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnContinueInForeachStatement()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInForeachVariableStatement()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnContinueInForeachVariableStatement()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnContinueInSwitchInForeach()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnTopLevelContinue()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
cont$$inue;
        </Document>
    </Project>
</Workspace>

            Test(workspace, expectedResult:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInParenthesizedLambda()
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

            Test(workspace, expectedResult:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInSimpleLambda()
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

            Test(workspace, expectedResult:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInLocalFunction()
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

            Test(workspace, expectedResult:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInMethod()
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

            Test(workspace, expectedResult:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnBreakInAccessor()
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

            Test(workspace, expectedResult:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnReturnInVoidMethod()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnReturnInIntMethod()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnReturnInVoidLambda()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnReturnedExpression()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnReturnedConstantExpression()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnYieldReturn_Return()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnYieldReturn_Yield()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnYieldReturn_Yield_Partial()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnYieldReturn_Yield_Partial_ReverseOrder()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnYieldBreak_Yield()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCSharpGoToOnYieldBreak_Break()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub ExtendedPropertyPattern_FirstPart()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub ExtendedPropertyPattern_SecondPart()
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

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TopLevelStatements_EmptySpace()
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

            Test(workspace, expectedResult:=False)
        End Sub
    End Class
End Namespace
