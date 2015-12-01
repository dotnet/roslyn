' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestException1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
        using System;
        class {|Definition:EClass|} : Exception { }
        class Program
        {
            /// <exception cref="[|$$EClass|]"></exception>
            static void Main(string[] pargs)
            {

            }
        }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestException1_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
        Imports System
        Class {|Definition:EClass|} : Inherits Exception
        End Class
        Class Program
            ''' <exception cref="[|$$EClass|]"></exception>
            Shared Sub Main(args As String())
            End Function
        End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestException2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class EClass
            {
                internal class {|Definition:$$ENClass|} : Exception
                { }
            }
            class Program
            {
                /// <exception cref="EClass.[|ENClass|]"></exception>
                static void Main(string[] pargs)
                {

                }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestException2_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class EClass
                Class {|Definition:$$ENClass|} : Inherits Exception
                End Class
            End Class
            Class Program
                ''' <exception cref="EClass.[|ENClass|]"></exception>
                Shared Sub Main(args As String())
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestException3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBAssembly1</ProjectReference>
        <Document><![CDATA[
            using System;
            class Program
            {
                /// <exception cref="[|EClass|]"></exception>
                static void Main(string[] pargs)
                {
                    [|EClass|] p = new [|EClass|]();
                }
            }]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBAssembly1">
        <Document>
            Imports System
            Public Class {|Definition:$$EClass|}
                Inherits Exception
            End Class
            Module Program
                Sub Main(args As String())
        
                End Sub
            End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestException3_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>CSAssembly1</ProjectReference>
        <Document><![CDATA[
            Imports System
            Class Program
                ''' <exception cref="[|EClass|]"></exception>
                Shared Sub Main(args As String())
                    Dim p As [|EClass|] = new [|EClass|]();
                End Sub
            End Class]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSAssembly1">
        <Document><![CDATA[
            using System;
            public class {|Definition:$$EClass|} : Exception { }
            static class Program
            {
                public static void Main(string[] args) { }
            }
        ]]></Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestException3_Inverse() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBAssembly1</ProjectReference>
        <Document><![CDATA[
            using System;
            class Program
            {
                /// <exception cref="[|$$EClass|]"></exception>
                static void Main(string[] pargs)
                {
                    [|EClass|] p = new [|EClass|]();
                }
            }]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBAssembly1">
        <Document>
            Imports System
            Public Class {|Definition:EClass|}
                Inherits Exception
            End Class
            Module Program
                Sub Main(args As String())
        
                End Sub
            End Module
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestException3_Inverse_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>CSAssembly1</ProjectReference>
        <Document><![CDATA[
            Imports System
            Class Program
                ''' <exception cref="[|$$EClass|]"></exception>
                Shared Sub Main(args As String())
                    Dim p As [|EClass|] = new [|EClass|]();
                End Sub
            End Class]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSAssembly1">
        <Document><![CDATA[
            using System;
            public class {|Definition:EClass|} : Exception { }
            static class Program
            {
                public static void Main(string[] args) { }
            }
        ]]></Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefConstructor1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class A
            {
                public class B : Exception
                {
                    public {|Definition:B|}() { }
                }
            }

            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="args"></param>
                /// <exception cref="A.B.[|B|]"></exception>
                static void Main(string[] args)
                {
                    A.B x = new A.[|$$B|]();
                }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(769369)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefConstructor1_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class A
                Public Class B : Inherits Exception
                    Public Sub {|Definition:New|}()
                    End Sub
                End Class
            End Class

            Class Program
                ''' <summary>
                ''' </summary>
                ''' <param name="args"></param>
                ''' <exception cref="A.B.[|New|]()"></exception>
                Shared Sub Main(args As String())
                    Dim x As A.B = New A.[|$$B|]()
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefConstructor2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class A
            {
                public A() { }
                public {|Definition:A|}(int x) { }
            }
            class Program
            {
                /// <summary>
                /// <see cref="A.A()"/>
                /// <see cref="A.[|$$A|](int)"/>
                /// </summary>
                /// <param name="args"></param>
                static void Main(string[] args)
                {
                    A a = new A();
                }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact(Skip:="769477"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestCrefConstructor2_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class A
                Public Sub New()
                End Sub
                Public Sub {|Definition:New|}(x As Integer)
                End Sub
            End Class
            Class Program
                ''' <summary>
                ''' <see cref="A.New()"/>
                ''' <see cref="A.[|$$New|](Integer)"/>
                ''' </summary>
                ''' <param name="args"></param>
                Shared Sub Main(args As String())
                    Dim a As A = new A()
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParam1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class Program
            {
                static void Main(string[] args)
                {
    
                }
                /// <summary>
                /// 
                /// </summary>
                /// <param name="[|x|]"></param>
                static void Foo(int {|Definition:$$x|}) { [|x|] = 1; }  
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParam1_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class Program
                Shared Sub Main(args As String())
                End Sub
                ''' <summary>
                ''' 
                ''' </summary>
                ''' <param name="[|x|]"></param>
                Shared Sub Foo({|Definition:$$x|} As Integer) 
                    [|x|] = 1
                End Sub  
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParam2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class Program
            {
                static void Main(string[] args)
                {
    
                }
                /// <summary>
                /// 
                /// </summary>
                /// <param name="[|$$x|]"></param>
                static void Foo(int {|Definition:x|}) { [|x|] = 1; }  
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParam2_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class Program
                Shared Sub Main(args As String())
                End Sub
                ''' <summary>
                ''' 
                ''' </summary>
                ''' <param name="[|$$x|]"></param>
                Shared Sub Foo({|Definition:x|} As Integer) 
                    [|x|] = 1
                End Sub  
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact(Skip:="Bug 640274"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParam3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class Program
            {
                static void Main(string[] args)
                {

                }
                /// <summary>
                /// 
                /// </summary>
                /// <param name="[|@if|]"></param>
                static void Foo(int {|Definition:$$@if|}) { }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact(Skip:="Bug 640274"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParam3_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class Program
                Shared Sub Main(args As String())
                End Sub
                ''' <summary>
                ''' 
                ''' </summary>
                ''' <param name="[|@if|]"></param>
                Shared Sub Foo({|Definition:$$[if]|} As Integer)
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParamRef1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class Program
            {
                static void Main(string[] args)
                {

                }
                /// <summary>
                /// the <paramref name="[|$$x|]"/> parameter takes a number
                /// </summary>
                /// <param name="[|x|]"></param>
                /// <param name="y"></param>
                /// <param name="@if"></param>
                static void Foo(int {|Definition:x|}, int y, out int @if) { @if = 2; [|x|] = 1; }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestParamRef1_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class Program
                Shared Sub Main(args As String())
                End Sub
                ''' <summary>
                ''' the <paramref name="[|$$x|]"/> parameter takes a number
                ''' </summary>
                ''' <param name="[|x|]"></param>
                ''' <param name="y"></param>
                ''' <param name="[if]"></param>
                Shared Sub Foo({|Definition:x|} As Integer, y As Integer, ByRef [if] As Integer) 
                    [if] = 2 
                    [|x|] = 1
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPermission() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            class Program
            {
                static void Main(string[] args)
                {
                    System.[|Security|].PermissionSet x;
                }
                /// <permission cref="System.[|$$Security|].PermissionSet"></permission>  
                static void Foo(int x, int y, out int @if) { @if = 2; }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestPermission_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Class Program
                Shared Sub Main(args As String())
                    Dim x As System.[|Security|].PermissionSet
                End Sub
                ''' <permission cref="System.[|$$Security|].PermissionSet"></permission>  
                Shared Sub Foo(x As Integer, y As Integer, ByRef [if] As Integer) 
                    [if] = 2
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParameters1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            /// <typeparam name="T"></typeparam>
            class Generic<T>
            {   
                public Generic() { }
                public T Foo(T x) { return x; }    
                /// <summary>
                /// <typeparam name="[|$$U|]"></typeparam>
                /// </summary>
                /// <param name="x"></param>
                /// <returns></returns>
                public [|U|] Foo<{|Definition:U|}>([|U|] x) {return x; }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParameters1_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            ''' <typeparam name="T"></typeparam>
            Class Generic(Of T)
                Public Sub New()
                End Sub
                Public Function Foo(x As T) As T 
                    Return x
                End Function    
                ''' <summary>
                ''' <typeparam name="[|$$U|]"></typeparam>
                ''' </summary>
                ''' <param name="x"></param>
                ''' <returns></returns>
                Public Function Foo(Of {|Definition:U|})(x As [|U|]) As [|U|] 
                    Return x
                End Function
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParameters2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            /// <typeparam name="[|T|]"></typeparam>
            class Generic<{|Definition:T|}>
            {   
                public Generic() { }
                /// <summary>
                /// <typeparamref name="[|T|]"/>
                /// </summary>
                /// <param name="x"></param>
                /// <returns></returns>
                public [|$$T|] Foo([|T|] x) { return x; }    
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParameters2_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            ''' <typeparam name="[|T|]"></typeparam>
            Class Generic(Of {|Definition:T|})
                Public Sub New()
                End Sub
                ''' <summary>
                ''' <typeparamref name="[|T|]"/>
                ''' </summary>
                ''' <param name="x"></param>
                ''' <returns></returns>
                Public Function Foo(x As [|T|]) As [|$$T|] 
                    Return x
                End Function    
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParametersInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class Tester<{|Definition:T|}>
            {
                /// <summary>
                /// <see cref="Tester{T}.Foo(T)"/>
                /// </summary>
                /// <param name="x"></param>
                public static void Foo([|$$T|] x) { }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestTypeParametersInCref_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class Tester(Of {|Definition:T|})
                ''' <summary>
                ''' <see cref="Tester(Of T).Foo(T)"/>
                ''' </summary>
                ''' <param name="x"></param>
                Public Shared Sub Foo(x As [|$$T|]) 
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodParametersInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class {|Definition:Tester|}
            {
                /// <summary>
                /// <seealso cref="[|Tester|].Foo([|Tester|])"/>
                /// </summary>
                /// <param name="x"></param>
                public static void Foo(int x) { }
                public static void Foo([|$$Tester|] x) { }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMethodParametersInCref_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class {|Definition:Tester|}
                ''' <summary>
                ''' <seealso cref="[|Tester|].Foo([|Tester|])"/>
                ''' </summary>
                ''' <param name="x"></param>
                Public Shared Sub Foo(x As Integer)
                End Sub
                Public Shared Sub Foo(x As [|$$Tester|]) 
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFieldInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class Tester
            {
                private int {|Definition:@if|};
                /// <summary>
                /// <see cref="Tester.[|@if|]"/>
                /// </summary>
                /// <returns>int</returns>
                /// <value>int</value>
                public int X
                {
                    get
                    {
                        return [|@if|]; // Rename x to if
                    }
                    set
                    {
                        [|$$@if|] = value;
                    }
                }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestFieldInCref_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class Tester
                Private {|Definition:[if]|} As Integer 
                ''' <summary>
                ''' <see cref="Tester.[|[if]|]"/>
                ''' </summary>
                ''' <returns>int</returns>
                ''' <value>int</value>
                Public Property X As Integer
                    Get
                        Return [|[if]|] ' Rename x to if
                    End Get
                    Set
                        [|$$[if]|] = Value
                    End Set
                End Property
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestSpecialTypeSimpleNameInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class A
            {
                /// <summary>
                /// <see cref="[|int|]"/>
                /// </summary>
                private [|int|] x; 
                /// <summary>
                /// <see cref="[|int|]"/> 
                /// </summary>
                /// <returns>int</returns>
                /// <value>int</value>
                public static [|$$int|] Foo() { return 1; } // Invoke FAR on int here
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestSpecialTypeSimpleNameInCref_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class A
                ''' <summary>
                ''' <see cref="[|Integer|]"/>
                ''' </summary>
                Private x As [|Integer|]
                ''' <summary>
                ''' <see cref="[|Integer|]"/> 
                ''' </summary>
                Public Shared Function Foo() As [|$$Integer|] 
                    Return 1 ' Invoke FAR on Integer here
                End Function
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact(Skip:="Bug 640502"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceInCref1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            interface I
            {
                void {|Definition:Foo|}();
            }
            abstract class C
            {
                public abstract void Boo();
                public void {|Definition:Foo|}() { }
            }
            class A : C, I
            {
                /// <summary>
                /// <seealso cref="[|Foo|]()"/>
                /// </summary>
                public override void Boo() { [|$$Foo|](); }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact(Skip:="Bug 640502"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceInCref1_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Interface I
                Sub {|Definition:Foo|}()
            End Interface
            Abstract Class C
                Public MustOverride Sub Boo()
                Public Sub {|Definition:Foo|}() 
                End Sub
            End Class
            Class A : Inherits C : Implements I
                ''' <summary>
                ''' <seealso cref="[|Foo|]()"/>
                ''' </summary>
                Public Overrides Sub Boo() Implements I.[|Foo|]
                    [|$$Foo|]()
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceInCref2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            interface I
            {
                void {|Definition:Foo|}();
            }
            abstract class C
            {
                public abstract void {|Definition:Foo|}();
            }
            class A : C, I
            {
                /// <summary>
                /// <seealso cref="[|Foo|]()"/>
                /// </summary>
                public override void {|Definition:$$Foo|}() {  } 
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceInCref2_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Interface I
                Sub {|Definition:Foo|}()
            End Interface
            MustInherit Class C
                Public MustOverride Sub {|Definition:Foo|}()
            End Class
            Class A : Inherits C : Implements I
                ''' <summary>
                ''' <seealso cref="[|Foo|]()"/>
                ''' </summary>
                Public Overrides Sub {|Definition:$$Foo|}() Implements I.[|Foo|]
                End Sub 
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceInCref3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            interface I
            {
                void {|Definition:Foo|}();
            }
            abstract class C
            {
                public abstract void Foo();
            }
            class A : C, I
            {
                /// <summary>
                /// <seealso cref="Foo()"/>
                /// </summary>
                public override void Foo() {  } 
                /// <summary>
                /// <see cref="I.[|$$Foo|]()"/>
                /// </summary>
                void I.{|Definition:Foo|}() { }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInterfaceInCref3_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Interface I
                Sub {|Definition:Foo|}()
            End Interface
            MustInherit Class C
                Public MustOverride Sub Foo()
            End Class
            Class A : Inherits C : Implements I
                ''' <summary>
                ''' <seealso cref="Foo()"/>
                ''' </summary>
                Public Overrides Sub Foo()
                End Sub 
                ''' <summary>
                ''' <see cref="I.[|$$Foo|]()"/>
                ''' </summary>
                Public Sub {|Definition:FooImpl|}() Implements I.[|Foo|]
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInheritanceInCref1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class Base
            {   
                public {|Definition:Base|}(int x) { }
            }
            class Derived : Base
            {
    
                /// <summary>
                /// <see cref="Base.[|Base|](int)"/>
                /// </summary>
                /// <param name="x"></param>
                public Derived(int x) : [|$$base|](x)
                {

                }
                public void Base(int x) { }
            }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(769369)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInheritanceInCref1_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class Base
                Public Sub {|Definition:New|}(x As Integer)
                End Sub
            End Class
            Class Derived : Inherits Base
                ''' <summary>
                ''' <see cref="Base.[|New|](Integer)"/>
                ''' </summary>
                ''' <param name="x"></param>
                Public Sub New(x As Integer)
                    MyBase.[|$$New|](x)
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOverloadingInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            using System;
            class B
                {
                    /// <summary>
                    /// <see cref="[|$$Del|]"/>
                    /// </summary>
                    /// <param name="x"></param>
                    public void {|Definition:Del|}(int x) { }
                    public void Del(string x) { }
                }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOverloadingInCref_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
            Imports System
            Class B
                ''' <summary>
                ''' <see cref="[|$$Del|]"/>
                ''' </summary>
                ''' <param name="x"></param>
                Public Sub {|Definition:Del|}(x As Integer)
                End Sub
                Public Sub Del(x As String)
                End Sub
            End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInheritanceInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class P
{
    public int {|Definition:y|};
}
class Q : P
{
    /// <summary>
    /// <see cref="y"/> 
    /// </summary>
    void Sub()
    {
        int x = [|$$y|]; // Invoke FAR here
    }
}]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestInheritanceInCref_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class P
    Public {|Definition:y|} As Integer
End Class
Class Q : Inherits P
    ''' <summary>
    ''' <see cref="[|y|]"/> 
    ''' </summary>
    Sub Subroutine()
        Dim x As Integer = [|$$y|] // Invoke FAR here
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructorInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
                using System;
                class {|Definition:A|}
                {
                    /// <summary>
                    /// <see cref="[|$$A|]"/>
                    /// <see cref="[|A|](int)"/>
                    /// </summary>
                    public {|Definition:A|}() { }
                    public {|Definition:A|}(int x) { }
                }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WorkItem(769369)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestConstructorInCref_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
                Imports System
                Class A
                    ''' <summary>
                    ''' <see cref="[|$$New|]()"/>
                    ''' <see cref="[|New|](Integer)"/>
                    ''' </summary>
                    Public Sub {|Definition:New|}()
                    End Sub
                    Public Sub New(x As Integer)
                    End Sub
                End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
                using System;
                class A
                {
                    delegate void Del(int x);
                    class B
                    {
                        /// <summary>
                        /// <see cref="[|$$Del|]"/>
                        /// </summary>
                        /// <param name="x"></param>
                        public void {|Definition:Del|}(int x) { }       
                }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestDelegateInCref_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
                Imports System
                Class A
                    Delegate Sub Del(x As Integer)
                    Class B
                        ''' <summary>
                        ''' <see cref="[|$$Del|]"/>
                        ''' </summary>
                        ''' <param name="x"></param>
                        Public Sub {|Definition:Del|}(x As Integer)
                        End Sub
                    End Class
                End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestScopeInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
                using System;
                class A
                {
                    delegate void Del(int x);
                    /// <summary>
                    /// <see cref="A.[|$$Del|]"/>
                    /// </summary>
                    class B
                    {
                        internal class A
                        {
                            internal class {|Definition:Del|}
                            {

                            }
                        }
                    }
                }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestScopeInCref_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
                Imports System
                Class A
                    Delegate Sub Del(x As Integer)
                    ''' <summary>
                    ''' <see cref="A.[|$$Del|]"/>
                    ''' </summary>
                    Class B
                        Class A
                            Class {|Definition:Del|}
                            End Class
                        End Class
                    End Class
                End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOperatorInCref() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
                using System;
                class Test
                {
                    /// <summary>
                    /// <see cref="operator !"/>
                    /// <see cref="operator [|+|]"/>
                    /// </summary>
                    /// <param name="t"></param>
                    /// <returns></returns>
                    public static Test operator !(Test t)
                    {
                        return new Test();
                    }
                    public static int operator {|Definition:$$+|}(Test t1, Test t2)
                    {
                        return 1;
                    }
                }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestOperatorInCrefVisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
                Imports System
                Class Test
                    ''' <summary>
                    ''' <see cref="Operator Not(Test)"/>
                    ''' <see cref="Operator [|+|](Test, Test)"/>
                    ''' </summary>
                    ''' <param name="t"></param>
                    ''' <returns></returns>
                    Public Shared Operator Not(t As Test) As Test
                        Return New Test()
                    End Operator
                    Public Shared Operator {|Definition:$$+|}(t1 As Test, t2 As Test) As Integer
                        Return 1
                    End Operator
                End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexInCref1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
                using System;
                class Test
                {
                    /// <summary>
                    /// <see cref="[||]this[int]"/>
                    /// </summary>
                    /// <param name="i"></param>
                    /// <returns></returns>
                    public int {|Definition:$$this|}[int i]
                    {
                        get { return i; }
                    }
                    public float this[float i]
                    {
                        get { return i; }
                    }
                }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexInCref1_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
                Imports System
                Class Test
                    ''' <summary>
                    ''' <see cref="[|Item|](Integer)"/>
                    ''' </summary>
                    ''' <param name="i"></param>
                    ''' <returns></returns>
                    Public ReadOnly Property {|Definition:$$Item|}(i As Integer) As Integer
                        Get
                            Return i
                        End Get
                    End Property
                    Public ReadOnly Property Item(i As Single) As Single
                        Get
                            Return i
                        End Get
                    End Property
                End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexInCref2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
                using System;
                class Test
                {
                    /// <summary>
                    /// <see cref="[|$$|]this"/>
                    /// </summary>
                    /// <param name="i"></param>
                    /// <returns></returns>
                    public int {|Definition:this|}[int i]
                    {
                        get { return i; }
                    }
                    public float this[float i]
                    {
                        get { return i; }
                    }
                }]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexInCref2_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
                Imports System
                Class Test
                    ''' <summary>
                    ''' <see cref="[|$$Item|]"/>
                    ''' </summary>
                    ''' <param name="i"></param>
                    ''' <returns></returns>
                    Public ReadOnly Property {|Definition:Item|}(i As Integer) As Integer
                        Get
                            Return i
                        End Get
                    End Property
                    Public ReadOnly Property Item(i As Single) As Single
                        Get
                            Return i
                        End Get
                    End Property
                End Class]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexInCref3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBAssembly1</ProjectReference>
        <Document><![CDATA[
            using System;
            class Program
            {
                /// <see cref="GetDNSaliases.[||]this[int]"/>
                static void Main(string[] args) { }
                {
                }
            }
            ]]>
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBAssembly1">
        <Document><![CDATA[
            Imports System
            Public Class GetDNSaliases
                Default Public ReadOnly Property {|Definition:$$Item|}(ByVal nIndex As Integer) As String
                    Get
                        Return ""
                    End Get
                End Property
            End Class
            ]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function

        <WpfFact(Skip:="783135"), Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestIndexInCref3_VisualBasic() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>CSAssembly1</ProjectReference>
        <Document><![CDATA[
            Imports System
            Class Program
                ''' <see cref="GetDNSaliases.[|Item|](Integer)"/>
                Shared Sub Main(args As String())
                End Sub
            End Class
            ]]>
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSAssembly1">
        <Document><![CDATA[
            using System;
            public class GetDNSaliases
            {
                public string {|Definition:$$this|}[int nIndex]
                {
                    get { return ""; }
                }
            }
            ]]>
        </Document>
    </Project>
</Workspace>
            Await TestAsync(input)
        End Function
    End Class
End Namespace
