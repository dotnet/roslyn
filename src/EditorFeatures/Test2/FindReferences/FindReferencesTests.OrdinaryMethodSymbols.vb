' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethod1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            private void {|Definition:Foo|}() { }

            void Bar()
            {
                [|Fo$$o|]();
                [|Foo|]();
                B.Foo();
                new C().[|Foo|]();
                new C().foo();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodCaseSensitivity() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        class C
            private sub {|Definition:Foo|}()
            end sub

            sub Bar()
                [|Fo$$o|]()
                [|Foo|]()
                B.Foo()
                Console.WriteLine(new C().[|Foo|]())
                Console.WriteLine(new C().[|foo|]())
            end sub
        end class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverride1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public virtual void {|Definition:Fo$$o|}() { }
            void Bar() { [|Foo|](); }
        }
        class D : C
        {
            public override void {|Definition:Foo|}() { }
            void Quux() { [|Foo|](); }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverride2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public virtual void {|Definition:Foo|}() { }
            void Bar() { [|Foo|](); }
        }
        class D : C
        {
            public override void {|Definition:Fo$$o|}() { }
            void Quux() { [|Foo|](); }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverride3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public virtual void Foo() { }
            void Bar() { Foo(); }
        }
        class D : C
        {
            public override void Foo() { }
            void Quux() { Foo(); }
        }
        class E : D
        {
            public new void {|Definition:Fo$$o|}() { }
            void Z() { [|Foo|](); }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverride_InMetadata() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            // Will walk up to Object.ToString
            public override string {|Definition:To$$String|}() { }
        }
        class O
        {
            public override string {|Definition:ToString|}() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodOverrideCrossLanguage() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
        public class C
        {
            public virtual void {|Definition:Fo$$o|}() { }
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
        class D : Inherits C
            public overrides sub {|Definition:Foo|}()
            end sub
            private sub Bar()
                [|Foo|]()
            end sub
        sub class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceInheritance_FromReference() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            void {|Definition:Foo|}();
        }

        class C1 : I1
        {
            public void {|Definition:Foo|}()
            {
            }
        }

        interface I2 : I1
        {
            void {|Definition:Foo|}();
            void Bar();
        }

        class C2 : I2
        {
            public void Bar()
            {
                [|Foo$$|]();
            }

            public void {|Definition:Foo|}();
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceInheritance_FromDefinition() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface I1
        {
            void {|Definition:Fo$$o|}();
        }

        class C1 : I1
        {
            public void {|Definition:Foo|}()
            {
            }
        }

        interface I2 : I1
        {
            void {|Definition:Foo|}();
            void Bar();
        }

        class C2 : I2
        {
            public void Bar()
            {
                [|Foo|]();
            }

            public void {|Definition:Foo|}();
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceImplementation1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IFoo
        {
            void {|Definition:Foo|}();
        }
        class C
        {
            public void {|Definition:Fo$$o|}() { }
        }
        class D : C, IFoo
        {
            void Quux()
            {
                IFoo f;
                f.[|Foo|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(529616)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceImplementationVB() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Interface IFoo
            Sub {|Definition:TestSub|}()
        End Interface

        Class Foo
            Implements IFoo
            Public Sub {|Definition:MethodWithADifferentName|}() Implements IFoo.[|$$TestSub|]
            End Function
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceImplementation2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IFoo
        {
            void {|Definition:F$$oo|}();
        }
        class C
        {
            public void {|Definition:Foo|}() { }
            void Zap()
            {
                this.[|Foo|]();
                [|Foo|]();
            }
        }
        class D : C, IFoo
        {
            void Quux()
            {
                IFoo f;
                f.[|Foo|]();
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceImplementationSingleFileOnly() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            void Zap()
            {
                IFoo foo;
                foo.[|Fo$$o|]();
            }
        }
        class D : IFoo
        {
            void Quux()
            {
                IFoo f;
                f.[|Foo|]();
            }
        }
        </Document>
        <Document>
        interface IFoo
        {
            void {|Definition:Foo|}();
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input, searchSingleFileOnly:=True)
        End Function

        <WorkItem(522786)>
        <WpfFact(Skip:="Bug 522786"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceDispose1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C : System.IDisposable
        {
            public void {|Definition:Disp$$ose|}() { }
            void Zap()
            {
                [|using|] (new C())
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(522786)>
        <WpfFact(Skip:="Bug 522786"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceDispose2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C : System.IDisposable
        {
            public void {|Definition:Disp$$ose|}() { }
            void Zap()
            {
                [|using|] (new D())
                {
                }
            }
        }
        class D : System.IDisposable
        {
            public void {|Definition:Dispose|}() { }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodIEnumerable1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System.Collections;
        class C : IEnumerable
        {
            public IEnumerator {|Definition:GetEnumera$$tor|}() { }
            void Zap()
            {
                [|foreach|] (var v in this)
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
        Public Async Function TestOrdinaryMethodIEnumerable2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System.Collections;
        class C
        {
            public struct Enumerator : IEnumerator
            {
                public object Current { get { } }
                public bool {|Definition:MoveNe$$xt|}() { }
            }
            public Enumerator GetEnumerator() { }
            void Zap()
            {
                [|foreach|] (var v in this)
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
        Public Async Function TestOrdinaryMethodIEnumerable3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System.Collections;
        class C
        {
            public struct Enumerator : IEnumerator
            {
                public object {|Definition:Cu$$rrent|} { get { } }
                public bool MoveNext() { }
            }
            public Enumerator GetEnumerator() { }
            void Zap()
            {
                [|foreach|] (var v in this)
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
        Public Async Function TestOrdinaryMethodIEnumerable4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Collections.Generic;
        class C : IEnumerable<int>
        {
            public IEnumerator<int> {|Definition:GetEnumera$$tor|}() { }
            void Zap()
            {
                [|foreach|] (var v in this)
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodIEnumerable5() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Collections.Generic;
        class C
        {
            public struct Enumerator<T> : IEnumerator<T>
            {
                public T Current { get { } }
                public bool {|Definition:MoveNe$$xt|}() { }
            }
            public Enumerator<int> GetEnumerator() { }
            void Zap()
            {
                [|foreach|] (var v in this)
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodIEnumerable6() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Collections.Generic;
        class C
        {
            public struct Enumerator<T> : IEnumerator<T>
            {
                public object {|Definition:Cu$$rrent|} { get { } }
                public bool MoveNext() { }
            }
            public Enumerator<T> GetEnumerator() { }
            void Zap()
            {
                [|foreach|] (var v in this)
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(634818)>
        <WpfFact(Skip:="636943"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodLinqWhere1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        using System.Collections.Generic;
        class C
        {
            public IEnumerable<int> {|Definition:Whe$$re|}(Func<int,bool> pred) { };
            public IEnumerable<int> Select(Func<int,int> func) { };
            void Zap()
            {
                var q = from v in this
                        [|where|] v > 21
                        select v;
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(636943)>
        <WpfFact(Skip:="636943"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodLinqWhere2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        using System.Collections.Generic;
        class C
        {
            public IEnumerable<int> {|Definition:Where|}(Func<int,bool> pred) { };
            public IEnumerable<int> Select(Func<int,int> func) { };
            void Zap()
            {
                var q = from v in this
                        [|w$$here|] v > 21
                        select v;
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(636943)>
        <WpfFact(Skip:="636943"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodLinqSelect1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        using System.Collections.Generic;
        class C
        {
            public IEnumerable<int> Where(Func<int,bool> pred) { };
            public IEnumerable<int> {|Definition:Sel$$ect|}(Func<int,int> func) { };
            void Zap()
            {
                var q = from v in this
                        where v > 21
                        [|select|] v + 1;
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(636943)>
        <WpfFact(Skip:="636943"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodLinqSelect2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        using System.Collections.Generic;
        class C
        {
            public IEnumerable<int> Where(Func<int,bool> pred) { };
            public IEnumerable<int> {|Definition:Select|}(Func<int,int> func) { };
            void Zap()
            {
                var q = from v in this
                        where v > 21
                        [|sel$$ect|] v + 1;
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(528936)>
        <WpfFact(Skip:="Bug 528936"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodMonitorEnter() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Threading;
        using System.Collections.Generic;
        class C
        {
            void Zap()
            {
                Monitor.[|En$$ter|](null);
                [|lock|] (new C())
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(528936)>
        <WpfFact(Skip:="Bug 528936"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodMonitorExit() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System.Threading;
        using System.Collections.Generic;
        class C
        {
            void Zap()
            {
                Monitor.[|Ex$$it|](null);
                [|lock|] (new C())
                {
                }
            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpInaccessibleInstanceAbstractMethod() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        abstract class C
        {
           public abstract void {|Definition:$$M|}(int i);
        }
        class D
        {
            void Foo()
            {
               C.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_VBInaccessibleInstanceAbstractMethod() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        MustInherit Class C
              public MustOverride Sub {|Definition:$$M|} (ByVal i as Integer)
        End Class
        Class D
              Sub Foo()
                   C.[|M|](1);
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538794)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpInaccessibleInstancePrivateStaticMethod() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           private static void {|Definition:$$M|}(int i) { }
        }
        class D
        {
            void Foo()
            {
               C.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_VBInaccessibleInstancePrivateStaticMethod() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              Private shared Sub {|Definition:$$M|} (ByVal i as Integer)
              End Sub
        End Class
        Class D
              Sub Foo()
                   C.[|M|](1)
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538794)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpInaccessibleInstanceProtectedMethod() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           protected void {|Definition:$$M|}(int i) { }
        }
        class D
        {
            void Foo()
            {
               C.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_VBInaccessibleInstanceProtectedMethod() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
        Class C
              Protected Sub {|Definition:$$M|} (ByVal i as Integer)
              End Sub
        End Class
        Class D
              Sub Foo()
                   C.[|M|](1)
              End Sub
        End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(2544, "https://github.com/dotnet/roslyn/issues/2544")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInaccessibleMemberOverrideVB() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class C
                Private Sub M(d As D)
                    d.[|$$M|](1)
                End Sub
            End Class
            Class D
                Private Sub {|Definition:M|}(i As Integer)
                End Sub
                Private Sub M(d As Double)
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace)
        End Function

        <WorkItem(2544, "https://github.com/dotnet/roslyn/issues/2544")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInaccessibleMemberOverrideCS() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                private void M(D d)
                {
                    d.[|$$M|](1);
                }
            }

            class D
            {
                private void {|Definition:M|}(int i) { }
                private void M(double d) { }
            }
        </Document>
    </Project>
</Workspace>


            Await TestAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpAccessibleInstanceProtectedMethod() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           protected void {|Definition:$$M|}(int i) { }
        }
        class D : C
        {
            void Foo()
            {
               D.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestField_CSharpAccessibleStaticProtectedMethod() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
           protected static void {|Definition:$$M|}(int i) { }
        }
        class D : C
        {
            void Foo()
            {
               C.[|M|](1);
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538726)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceMethodsDontCascadeThroughOtherInterfaceMethods1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IControl
        {
            void {|Definition:Pa$$int|}();
        }
        interface ISurface : IControl
        {
            void Paint();
        }
        class SampleClass : IControl
        {
            public void {|Definition:Paint|}()
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538726)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceMethodsDontCascadeThroughOtherInterfaceMethods2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IControl
        {
            void {|Definition:Paint|}();
        }
        interface ISurface : IControl
        {
            void Paint();
        }
        class SampleClass : IControl
        {
            public void {|Definition:Pa$$int|}()
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538726)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodInterfaceMethodsDontCascadeThroughOtherInterfaceMethods3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        interface IControl
        {
            void Definition();
        }
        interface ISurface : IControl
        {
            void {|Definition:Pa$$int|}();
        }
        class SampleClass : IControl
        {
            public void Paint()
            {
            }
        }
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(538898)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodMatchEntireInvocation() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Sub Main
        Dim x As I
        x.Foo(1)
    End Sub
End Module
 
Interface I
    Sub Foo(x as Integer)
    Sub {|Definition:F$$oo|}(x as Date)
End Interface
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539033)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
    interface I<T>
    {
        void {|Definition:$$F|}();
    }

    class Base<U> : I<U>
    {
        void I<U>.{|Definition:F|}() { }
    }

    class Derived<U, V> : Base<U>, I<V>
    {
        public void {|Definition:F|}()
        {
            [|F|]();
        }
    }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539033)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
    interface I<T>
    {
        void {|Definition:F|}();
    }

    class Base<U> : I<U>
    {
        void I<U>.{|Definition:$$F|}() { }
    }

    class Derived<U, V> : Base<U>, I<V>
    {
        public void {|Definition:F|}()
        {
            [|F|]();
        }
    }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539033)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
    interface I<T>
    {
        void {|Definition:F|}();
    }

    class Base<U> : I<U>
    {
        void I<U>.{|Definition:F|}() { }
    }

    class Derived<U, V> : Base<U>, I<V>
    {
        public void {|Definition:$$F|}()
        {
            [|F|]();
        }
    }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539033)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethodFromGenericInterface4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
    interface I<T>
    {
        void {|Definition:F|}();
    }

    class Base<U> : I<U>
    {
        void I<U>.{|Definition:F|}() { }
    }

    class Derived<U, V> : Base<U>, I<V>
    {
        public void {|Definition:F|}()
        {
            [|$$F|]();
        }
    }
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539046)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_DoNotFindInNonImplementingClass1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I
{
  void {|Definition:$$Foo|}();
}

class C : I
{
  public void {|Definition:Foo|}()
  {
  }
}

class D : C
{
  public void Foo()
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539046)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_DoNotFindInNonImplementingClass2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I
{
  void {|Definition:Foo|}();
}

class C : I
{
  public void {|Definition:$$Foo|}()
  {
  }
}

class D : C
{
  public void Foo()
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539046)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_DoNotFindInNonImplementingClass3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
interface I
{
  void Foo();
}

class C : I
{
  public void Foo()
  {
  }
}

class D : C
{
  public void {|Definition:$$Foo|}()
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Foo|}<T>(IList<T> list);
}

class C : I
{
  public void {|Definition:Foo|}<U>(IList<U> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:Foo|}<T>(IList<T> list);
}

class C : I
{
  public void {|Definition:$$Foo|}<U>(IList<U> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Foo|}<T>(IList<T> list);
}

class C<T> : I
{
  public void Foo<U>(IList<T> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Foo|}<T>(IList<T> list);
}

class C<T> : I
{
  public void Foo(IList<T> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_GenericMethod5() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Foo|}<T>(IList<T> list);
}

class C : I
{
  public void Foo<T>(IList<int> list)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_RefOut1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Foo|}(ref int i);
}

class C : I
{
  public void {|Definition:Foo|}(ref System.Int32 j)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_RefOut2_Success() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Foo|}(ref int i);
}

class C : I
{
  public void Foo(out System.Int32 j)
  {
  }

  void I.{|Definition:Foo|}(ref System.Int32 j) 
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCascadeOrdinaryMethod_RefOut2_Error() As Task
            ' In non-compiling code, finding an almost-matching definition
            ' seems reasonable.
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
using System.Collections.Generic;
interface I
{
  void {|Definition:$$Foo|}(ref int i);
}

class C : I
{
  public void {|Definition:Foo|}(out System.Int32 j)
  {
  }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethod_DelegateConstructor1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Program
{
    delegate double DoubleFunc(double x);
    DoubleFunc f = new DoubleFunc([|$$Square|]);
    static float Square(float x)
    {
        return x * x;
    }
    static double {|Definition:Square|}(double x)
    {
        return x * x;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethod_DelegateConstructor2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Program
{
    delegate double DoubleFunc(double x);
    DoubleFunc f = new DoubleFunc(Square);
    static float {|Definition:$$Square|}(float x)
    {
        return x * x;
    }
    static double Square(double x)
    {
        return x * x;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethod_DelegateConstructor3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class Program
{
    delegate double DoubleFunc(double x);
    DoubleFunc f = new DoubleFunc([|Square|]);
    static float Square(float x)
    {
        return x * x;
    }
    static double {|Definition:$$Square|}(double x)
    {
        return x * x;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539646)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateMethod1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
 using System;
class Program
{
    delegate R {|Definition:Func|}<T, R>(T t);
    static void Main(string[] args)
    {
        [|Func|]<int, int> f = (arg) =>
        {
            int s = 3;
            return s;
        };
        f.[|$$BeginInvoke|](2, null, null);
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539646)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateMethod2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
 using System;
class Program
{
    delegate R {|Definition:$$Func|}<T, R>(T t);
    static void Main(string[] args)
    {
        [|Func|]<int, int> f = (arg) =>
        {
            int s = 3;
            return s;
        };
        f.BeginInvoke(2, null, null);
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539646)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateMethod3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
 using System;
class Program
{
    delegate R {|Definition:Func|}<T, R>(T t);
    static void Main(string[] args)
    {
        [|$$Func|]<int, int> f = (arg) =>
        {
            int s = 3;
            return s;
        };
        f.BeginInvoke(2, null, null);
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539824)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodGroup1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class C
{
    public delegate int Func(int i);
 
    public Func Foo()
    {
        return [|$$Foo|];
    }
    private int {|Definition:Foo|}(int i)
    {
        return i;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(539824)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodGroup2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <![CDATA[
class C
{
    public delegate int Func(int i);
 
    public Func Foo()
    {
        return [|Foo|];
    }
    private int {|Definition:$$Foo|}(int i)
    {
        return i;
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540349)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNonImplementedInterfaceMethod1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I
    Sub {|Definition:$$Foo|}()
End Interface

Class A
    Implements I
    Public Sub {|Definition:Foo|}() Implements I.[|Foo|]
    End Sub
End Class

Class B
    Inherits A
    Implements I
    Public Sub Foo()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540349)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNonImplementedInterfaceMethod2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I
    Sub {|Definition:Foo|}()
End Interface

Class A
    Implements I
    Public Sub {|Definition:$$Foo|}() Implements I.[|Foo|]
    End Sub
End Class

Class B
    Inherits A
    Implements I
    Public Sub Foo()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540349)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestNonImplementedInterfaceMethod3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I
    Sub Foo()
End Interface

Class A
    Implements I
    Public Sub Foo() Implements I.Foo
    End Sub
End Class

Class B
    Inherits A
    Implements I
    Public Sub {|Definition:$$Foo|}()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540359)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestShadowedMethod1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I1
    Function {|Definition:$$Foo|}() As Integer
End Interface

Interface I2
    Inherits I1
    Shadows Function Foo() As Integer
End Interface

Class C
    Implements I1
    Public Function {|Definition:Foo|}() As Integer Implements I1.[|Foo|]
        Return 1
    End Function
End Class

Class M
    Inherits C
    Implements I2
    Public Overloads Function Foo() As Integer Implements I2.Foo
        Return 1
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540359)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestShadowedMethod2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I1
    Function {|Definition:Foo|}() As Integer
End Interface

Interface I2
    Inherits I1
    Shadows Function Foo() As Integer
End Interface

Class C
    Implements I1
    Public Function {|Definition:$$Foo|}() As Integer Implements I1.[|Foo|]
        Return 1
    End Function
End Class

Class M
    Inherits C
    Implements I2
    Public Overloads Function Foo() As Integer Implements I2.Foo
        Return 1
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540359)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestShadowedMethod3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I1
    Function Foo() As Integer
End Interface

Interface I2
    Inherits I1
    Shadows Function {|Definition:$$Foo|}() As Integer
End Interface

Class C
    Implements I1
    Public Function Foo() As Integer Implements I1.Foo
        Return 1
    End Function
End Class

Class M
    Inherits C
    Implements I2
    Public Overloads Function {|Definition:Foo|}() As Integer Implements I2.[|Foo|]
        Return 1
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540359)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestShadowedMethod4() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Interface I1
    Function Foo() As Integer
End Interface

Interface I2
    Inherits I1
    Shadows Function {|Definition:Foo|}() As Integer
End Interface

Class C
    Implements I1
    Public Function Foo() As Integer Implements I1.Foo
        Return 1
    End Function
End Class

Class M
    Inherits C
    Implements I2
    Public Overloads Function {|Definition:$$Foo|}() As Integer Implements I2.[|Foo|]
        Return 1
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540946)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAddressOfOverloads1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System

Class C
    Shared Sub Main()
        Dim a As Action(Of Integer) = AddressOf [|$$Foo|]
    End Sub

    Sub Foo()
    End Sub

    Shared Sub {|Definition:Foo|}(x As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540946)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAddressOfOverloads2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System

Class C
    Shared Sub Main()
        Dim a As Action(Of Integer) = AddressOf [|Foo|]
    End Sub

    Sub Foo()
    End Sub

    Shared Sub {|Definition:$$Foo|}(x As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(540946)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestAddressOfOverloads3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Imports System

Class C
    Shared Sub Main()
        Dim a As Action(Of Integer) = AddressOf Foo
    End Sub

    Sub {|Definition:$$Foo|}()
    End Sub

    Shared Sub Foo(x As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(542034)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionValue1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Public Class MyClass1
    Public Shared Sub Main()
    End Sub
    Shared Function {|Definition:$$Function1|}(ByRef arg)
        [|Function1|] = arg * 2
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(542034)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFunctionValue2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Public Class MyClass1
    Public Shared Sub Main()
    End Sub
    Shared Function {|Definition:Function1|}(ByRef arg)
        [|$$Function1|] = arg * 2
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(543002)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestForEachGetEnumerator1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class B
{
    public int Current { get; set; }
    public bool MoveNext()
    {
        return false;
    }
}

class C
{
    static void Main()
    {
        [|foreach|] (var x in new C()) { }
    }

    public B {|Definition:$$GetEnumerator|}()
    {
        return null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(543002)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestForEachMoveNext1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class B
{
    public int Current { get; set; }
    public bool {|Definition:$$MoveNext|}()
    {
        return false;
    }
}

class C
{
    static void Main()
    {
        [|foreach|] (var x in new C()) { }
    }

    public B GetEnumerator()
    {
        return null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(543002)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestForEachCurrent1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class B
{
    public int {|Definition:$$Current|} { get; set; }
    public bool MoveNext()
    {
        return false;
    }
}

class C
{
    static void Main()
    {
        [|foreach|] (var x in new C()) { }
    }

    public B GetEnumerator()
    {
        return null;
    }
}
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(544439)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodPartial1_CSharp() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Class1
{
    partial void {|Definition:$$foo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U;
    partial void {|Definition:foo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U
    {
    }
}]]></Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(544439)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodPartial2_CSharp() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Class1
{
    partial void {|Definition:$$foo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U;
    partial void {|Definition:foo|}<T, U, V>(T x, U y, V z) where T : class where U : Exception, T where V : U
    {
    }
}]]></Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(544437)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodPartial1_VB() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Public Module Module1
    Partial Private Sub {|Definition:$$FOo|}(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
    End Sub
    Private Sub {|Definition:foo|}(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
        Console.WriteLine("foo")
    End Sub
    Sub Main()
    End Sub
End Module
]]></Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(544437)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOrdinaryMethodPartial2_VB() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Public Module Module1
    Partial Private Sub {|Definition:FOo|}(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
    End Sub
    Private Sub {|Definition:$$foo|}(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
        Console.WriteLine("foo")
    End Sub
    Sub Main()
    End Sub
End Module
]]></Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceMethod() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
public Main()
{
    var c1 = new Class1();
    var c2 = new Class2();
    var c3 = new Class3();

    PrintMyName(c1);
    PrintMyName(c2);
    PrintMyName(c3);
}
public void PrintMyName(IClass c)
{
    Console.WriteLine(c.$$[|GetMyName|]());
}
public class Class1 : IClass
{
    public string {|Definition:GetMyName|}()
    {
        return "Class1";
    }
}
public class Class2 : IClass
{
    public string {|Definition:GetMyName|}()
    {
        return "Class2";
    }
}
public class Class3 : Class2
{
    public new string GetMyName()
    {
        return "Class3";
    }
}
public interface IClass
{
    string {|Definition:GetMyName|}();
}
</Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefMethod() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    ///  <see cref="Program.[|Main|]"/> to start the program.
    static void {|Definition:Ma$$in|}(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefMethod2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    ///  <see cref="Program.[|Ma$$in|]"/> to start the program.
    static void {|Definition:Main|}(string[] args)
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefMethodAcrossMultipleFiles() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Program
{
    ///  <see cref="Program.[|Main|]"/> to start the program.
    static void {|Definition:Ma$$in|}(string[] args)
    {
    }
}
]]>
        </Document>
        <Document><![CDATA[
partial class Program
{
    ///  <see cref="Program.[|Main|]"/>
    void foo() {}
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefMethodAcrossMultipleFiles2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
partial class Program
{
    ///  <see cref="Program.[|Main|]"/> to start the program.
    static void {|Definition:Main|}(string[] args)
    {
    }
}
]]>
        </Document>
        <Document><![CDATA[
partial class Program
{
    ///  <see cref="Program.[|Ma$$in|]"/>
    void foo() {}
    {
    }
}
]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(531010)>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrossAssemblyReferencesFromMetadata() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <MetadataReferenceFromSource Language="Visual Basic" CommonReferences="true">
            <Document FilePath="ReferencedDocument">
                                    Public Interface I
                                        Sub Foo()
                                    End Interface

                                    Friend Class F : Implements I
                                        Public Sub Foo() Implements I.Foo
                                        End Sub
                                    End Class
                                </Document>
        </MetadataReferenceFromSource>
        <Document>
Public Class C
    Sub Bar(i As I)
        i.$$[|Foo|]()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(623148)>
        Public Async Function TestFarWithInternalVisibleTo() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="ProjectA" CommonReferences="true">
        <Document>
            <![CDATA[
            <Assembly: Global.System.Runtime.CompilerServices.InternalsVisibleTo("ProjectB")> 

            Friend Class A
                Public Sub {|Definition:$$Foo|}()
                End Sub
            End Class]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="ProjectB" CommonReferences="true">
        <ProjectReference>ProjectA</ProjectReference>
        <Document>
            <![CDATA[
            Public Class B
                Public Sub Bar(a as A)
                    a.[|Foo|]()
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(657262)>
        Public Async Function TestMethodInsideMetadataToSourcePrimitiveTypeInCSharpSource() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="mscorlib" CommonReferences="true">
        <Document>
namespace System
{
    struct Int32
    {
        public override string {|Definition:$$ToString|}() { }
    }
}
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(657262)>
        Public Async Function TestMethodInsideMetadataToSourcePrimitiveTypeInVisualBasicSource() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="mscorlib" CommonReferences="true">
        <Document>
Namespace System
    Structure Int32
        Public Overrides Function {|Definition:$$ToString|}() As String
        End Function
    End Structure
End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_Basic() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        int x;
        public void {|Definition:Fo$$o|}(int x) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
class Class2
{
    int x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|Foo|](x);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_GenericType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        Tuple<int> x;
        public void {|Definition:Fo$$o|}(Tuple<int> x) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
using System;

class Class2
{
    Tuple<int> x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|Foo|](x);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_FARFromReferencingProject() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        Tuple<int> x;
        public void {|Definition:Foo|}(Tuple<int> x) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
using System;

class Class2
{
    Tuple<int> x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|$$Foo|](x);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_MultipleForwardedTypes() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        Tuple<int> x;
        public void {|Definition:$$Foo|}(Tuple<int> x, float y) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
using System;

class Class2
{
    Tuple<int> x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|Foo|](x, 0.0);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRetargetingMethod_NestedType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="PortableClassLibrary" CommonReferencesPortable="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;

namespace PortableClassLibrary
{
    public class Class1
    {
        public void {|Definition:$$Foo|}(System.Environment.SpecialFolder x) { }
    }
}]]>
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="MainLibrary" CommonReferences="true" CommonReferenceFacadeSystemRuntime="true">
        <ProjectReference>PortableClassLibrary</ProjectReference>
        <Document><![CDATA[
using System;

class Class2
{
    System.Environment.SpecialFolder x;
    public void TestMethod1(PortableClassLibrary.Class1 c)
    {
        c.[|Foo|](x);
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRefKindRef_FromDefinition() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Lib" CommonReferences="true">
        <Document><![CDATA[
using System;

public class C
{
    public static void {|Definition:$$M|}(ref int x) { }
}
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="Test" CommonReferences="true">
        <ProjectReference>Lib</ProjectReference>
        <Document><![CDATA[
Imports System

Class Test
    Sub M()
        Dim x As Integer = 0
        C.[|M|](x)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRefKindRef_FromReference() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Lib" CommonReferences="true">
        <Document><![CDATA[
using System;

public class C
{
    public static void {|Definition:M|}(ref int x) { }
}
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="Test" CommonReferences="true">
        <ProjectReference>Lib</ProjectReference>
        <Document><![CDATA[
Imports System

Class Test
    Sub M()
        Dim x As Integer = 0
        C.[|$$M|](x)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRefKindOut_FromDefinition() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Lib" CommonReferences="true">
        <Document><![CDATA[
using System;

public class C
{
    public static void {|Definition:$$M|}(out int x) { }
}
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="Test" CommonReferences="true">
        <ProjectReference>Lib</ProjectReference>
        <Document><![CDATA[
Imports System

Class Test
    Sub M()
        Dim x As Integer = 0
        C.[|M|](x)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function

        <WorkItem(599, "https://github.com/dotnet/roslyn/issues/599")>
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestRefKindOut_FromReference() As Task
            Dim input =
<Workspace>
    <Project Language="C#" AssemblyName="Lib" CommonReferences="true">
        <Document><![CDATA[
using System;

public class C
{
    public static void {|Definition:M|}(out int x) { }
}
]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="Test" CommonReferences="true">
        <ProjectReference>Lib</ProjectReference>
        <Document><![CDATA[
Imports System

Class Test
    Sub M()
        Dim x As Integer = 0
        C.[|$$M|](x)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Await TestAsync(input)
        End Function
    End Class
End Namespace
