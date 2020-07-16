' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.AddExplicitCast
    Partial Public Class AddExplicitCastTests

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function TestFixAllInDocumentBC30512() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Option Strict On
Public Class Program1
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Property D As Derived
        Public Property B As Base

        Public Sub New()
        End Sub

        Public Sub New(ByRef d As Derived)
            Me.D = d
        End Sub

        Public Sub testing(ByVal d As Derived)
        End Sub

        Private Sub testing(ByVal b As Base)
        End Sub
    End Class

    Class Test2
        Inherits Test

        Public Sub New(ByVal b As Base)
            MyBase.New(b)
        End Sub
    End Class

    Class Test3
        Public Sub New(ByVal b As Derived)
        End Sub

        Public Sub New(ByVal i As Integer, ByVal b As Base)
            Me.New(b)
        End Sub
    End Class

    Private Function ReturnBase() As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnBase(ByVal d As Derived) As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnDerived(ByVal b As Base) As Derived
        Return b
    End Function

    Private Function ReturnDerived2(ByVal b As Base) As Derived
        Return ReturnBase()
    End Function

    Public Sub New()
        Dim b As Base = New Derived
        Dim d As Derived = {|FixAllInDocument:b|}
        d = New Base()
        Dim d2 As Derived = ReturnBase()
        d2 = ReturnBase(b)

        Dim t As Test = New Test()
        t.D = b
        t.D = b
        d = t.B
    End Sub

    Private Sub PassDerived(ByVal d As Derived)
    End Sub

    Private Sub PassDerived(ByVal i As Integer, ByVal d As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Dim d As Derived = b

        PassDerived(b)
        PassDerived(ReturnBase())
        PassDerived(1, b)
        PassDerived(1, ReturnBase())

        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add(b)
        Dim t As Test = New Test()
        t.testing(b)
        Dim foo2 As Func(Of Derived, Base) = Function(p_d) p_d
        Dim d2 As Derived = foo2(b)
        d2 = foo2(d)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Option Strict On
Public Class Program2
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Class Test
        Public Shared Narrowing Operator CType(t As Test) As Derived2
            Return New Derived2
        End Operator
    End Class

    Private Function returnDerived2_1() As Derived2
        Return New Derived1()
    End Function

    Private Function returnDerived2_2() As Derived2
        Return New Test()
    End Function

    Private Sub Foo1(ByVal b As Derived2)
    End Sub

    Private Sub Foo2(ByVal b As Base2)
    End Sub

    Private Sub Foo3(ByVal b1 As Derived2)
    End Sub

    Private Sub Foo3(ByVal i As Integer)
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Dim derived2 As Derived2 = b1
        derived2 = b3
        Dim base2 As Base2 = b1
        derived2 = d1
        d2 = New Test()
        Foo1(b1)
        Foo1(d1)
        Foo2(b1)
        Foo3(b1)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub returnD2(ByVal b As Base)
        Dim d As Derived = b
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Option Strict On
Public Class Program1
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Property D As Derived
        Public Property B As Base

        Public Sub New()
        End Sub

        Public Sub New(ByRef d As Derived)
            Me.D = d
        End Sub

        Public Sub testing(ByVal d As Derived)
        End Sub

        Private Sub testing(ByVal b As Base)
        End Sub
    End Class

    Class Test2
        Inherits Test

        Public Sub New(ByVal b As Base)
            MyBase.New(CType(b, Derived))
        End Sub
    End Class

    Class Test3
        Public Sub New(ByVal b As Derived)
        End Sub

        Public Sub New(ByVal i As Integer, ByVal b As Base)
            Me.New(CType(b, Derived))
        End Sub
    End Class

    Private Function ReturnBase() As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnBase(ByVal d As Derived) As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnDerived(ByVal b As Base) As Derived
        Return CType(b, Derived)
    End Function

    Private Function ReturnDerived2(ByVal b As Base) As Derived
        Return CType(ReturnBase(), Derived)
    End Function

    Public Sub New()
        Dim b As Base = New Derived
        Dim d As Derived = CType(b, Derived)
        d = New Base()
        Dim d2 As Derived = CType(ReturnBase(), Derived)
        d2 = ReturnBase(CType(b, Derived))

        Dim t As Test = New Test()
        t.D = CType(b, Derived)
        t.D = CType(b, Derived)
        d = CType(t.B, Derived)
    End Sub

    Private Sub PassDerived(ByVal d As Derived)
    End Sub

    Private Sub PassDerived(ByVal i As Integer, ByVal d As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Dim d As Derived = CType(b, Derived)

        PassDerived(CType(b, Derived))
        PassDerived(CType(ReturnBase(), Derived))
        PassDerived(1, CType(b, Derived))
        PassDerived(1, CType(ReturnBase(), Derived))

        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add(CType(b, Derived))
        Dim t As Test = New Test()
        t.testing(CType(b, Derived))
        Dim foo2 As Func(Of Derived, Base) = Function(p_d) p_d
        Dim d2 As Derived = foo2(CType(b, Derived))
        d2 = CType(foo2(d), Derived)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Option Strict On
Public Class Program2
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Class Test
        Public Shared Narrowing Operator CType(t As Test) As Derived2
            Return New Derived2
        End Operator
    End Class

    Private Function returnDerived2_1() As Derived2
        Return New Derived1()
    End Function

    Private Function returnDerived2_2() As Derived2
        Return New Test()
    End Function

    Private Sub Foo1(ByVal b As Derived2)
    End Sub

    Private Sub Foo2(ByVal b As Base2)
    End Sub

    Private Sub Foo3(ByVal b1 As Derived2)
    End Sub

    Private Sub Foo3(ByVal i As Integer)
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Dim derived2 As Derived2 = b1
        derived2 = b3
        Dim base2 As Base2 = b1
        derived2 = d1
        d2 = New Test()
        Foo1(b1)
        Foo1(d1)
        Foo2(b1)
        Foo3(b1)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub returnD2(ByVal b As Base)
        Dim d As Derived = b
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function TestFixAllInProjectBC30512() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Option Strict On
Public Class Program1
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Property D As Derived
        Public Property B As Base

        Public Sub New()
        End Sub

        Public Sub New(ByRef d As Derived)
            Me.D = d
        End Sub

        Public Sub testing(ByVal d As Derived)
        End Sub

        Private Sub testing(ByVal b As Base)
        End Sub
    End Class

    Class Test2
        Inherits Test

        Public Sub New(ByVal b As Base)
            MyBase.New(b)
        End Sub
    End Class

    Class Test3
        Public Sub New(ByVal b As Derived)
        End Sub

        Public Sub New(ByVal i As Integer, ByVal b As Base)
            Me.New(b)
        End Sub
    End Class

    Private Function ReturnBase() As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnBase(ByVal d As Derived) As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnDerived(ByVal b As Base) As Derived
        Return b
    End Function

    Private Function ReturnDerived2(ByVal b As Base) As Derived
        Return ReturnBase()
    End Function

    Public Sub New()
        Dim b As Base = New Derived
        Dim d As Derived = {|FixAllInProject:b|}
        d = New Base()
        Dim d2 As Derived = ReturnBase()
        d2 = ReturnBase(b)

        Dim t As Test = New Test()
        t.D = b
        t.D = b
        d = t.B
    End Sub

    Private Sub PassDerived(ByVal d As Derived)
    End Sub

    Private Sub PassDerived(ByVal i As Integer, ByVal d As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Dim d As Derived = b

        PassDerived(b)
        PassDerived(ReturnBase())
        PassDerived(1, b)
        PassDerived(1, ReturnBase())

        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add(b)
        Dim t As Test = New Test()
        t.testing(b)
        Dim foo2 As Func(Of Derived, Base) = Function(p_d) p_d
        Dim d2 As Derived = foo2(b)
        d2 = foo2(d)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Option Strict On
Public Class Program2
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Class Test
        Public Shared Narrowing Operator CType(t As Test) As Derived2
            Return New Derived2
        End Operator
    End Class

    Private Function returnDerived2_1() As Derived2
        Return New Derived1()
    End Function

    Private Function returnDerived2_2() As Derived2
        Return New Test()
    End Function

    Private Sub Foo1(ByVal b As Derived2)
    End Sub

    Private Sub Foo2(ByVal b As Base2)
    End Sub

    Private Sub Foo3(ByVal b1 As Derived2)
    End Sub

    Private Sub Foo3(ByVal i As Integer)
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Dim derived2 As Derived2 = b1
        derived2 = b3
        Dim base2 As Base2 = b1
        derived2 = d1
        d2 = New Test()

        Foo1(b1)
        Foo1(d1)
        Foo2(b1)
        Foo3(b1)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub returnD2(ByVal b As Base)
        Dim d As Derived = b
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Option Strict On
Public Class Program1
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Property D As Derived
        Public Property B As Base

        Public Sub New()
        End Sub

        Public Sub New(ByRef d As Derived)
            Me.D = d
        End Sub

        Public Sub testing(ByVal d As Derived)
        End Sub

        Private Sub testing(ByVal b As Base)
        End Sub
    End Class

    Class Test2
        Inherits Test

        Public Sub New(ByVal b As Base)
            MyBase.New(CType(b, Derived))
        End Sub
    End Class

    Class Test3
        Public Sub New(ByVal b As Derived)
        End Sub

        Public Sub New(ByVal i As Integer, ByVal b As Base)
            Me.New(CType(b, Derived))
        End Sub
    End Class

    Private Function ReturnBase() As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnBase(ByVal d As Derived) As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnDerived(ByVal b As Base) As Derived
        Return CType(b, Derived)
    End Function

    Private Function ReturnDerived2(ByVal b As Base) As Derived
        Return CType(ReturnBase(), Derived)
    End Function

    Public Sub New()
        Dim b As Base = New Derived
        Dim d As Derived = CType(b, Derived)
        d = New Base()
        Dim d2 As Derived = CType(ReturnBase(), Derived)
        d2 = ReturnBase(CType(b, Derived))

        Dim t As Test = New Test()
        t.D = CType(b, Derived)
        t.D = CType(b, Derived)
        d = CType(t.B, Derived)
    End Sub

    Private Sub PassDerived(ByVal d As Derived)
    End Sub

    Private Sub PassDerived(ByVal i As Integer, ByVal d As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Dim d As Derived = CType(b, Derived)

        PassDerived(CType(b, Derived))
        PassDerived(CType(ReturnBase(), Derived))
        PassDerived(1, CType(b, Derived))
        PassDerived(1, CType(ReturnBase(), Derived))

        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add(CType(b, Derived))
        Dim t As Test = New Test()
        t.testing(CType(b, Derived))
        Dim foo2 As Func(Of Derived, Base) = Function(p_d) p_d
        Dim d2 As Derived = foo2(CType(b, Derived))
        d2 = CType(foo2(d), Derived)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Option Strict On
Public Class Program2
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Class Test
        Public Shared Narrowing Operator CType(t As Test) As Derived2
            Return New Derived2
        End Operator
    End Class

    Private Function returnDerived2_1() As Derived2
        Return New Derived1()
    End Function

    Private Function returnDerived2_2() As Derived2
        Return CType(New Test(), Derived2)
    End Function

    Private Sub Foo1(ByVal b As Derived2)
    End Sub

    Private Sub Foo2(ByVal b As Base2)
    End Sub

    Private Sub Foo3(ByVal b1 As Derived2)
    End Sub

    Private Sub Foo3(ByVal i As Integer)
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Dim derived2 As Derived2 = CType(b1, Derived2)
        derived2 = CType(b3, Derived2)
        Dim base2 As Base2 = CType(b1, Base2)
        derived2 = CType(d1, Derived2)
        d2 = CType(New Test(), Derived2)

        Foo1(CType(b1, Derived2))
        Foo1(CType(d1, Derived2))
        Foo2(CType(b1, Base2))
        Foo3(b1)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub returnD2(ByVal b As Base)
        Dim d As Derived = b
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function TestFixAllInSolutionBC30512() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Option Strict On
Public Class Program1
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Property D As Derived
        Public Property B As Base

        Public Sub New()
        End Sub

        Public Sub New(ByRef d As Derived)
            Me.D = d
        End Sub

        Public Sub testing(ByVal d As Derived)
        End Sub

        Private Sub testing(ByVal b As Base)
        End Sub
    End Class

    Class Test2
        Inherits Test

        Public Sub New(ByVal b As Base)
            MyBase.New(b)
        End Sub
    End Class

    Class Test3
        Public Sub New(ByVal b As Derived)
        End Sub

        Public Sub New(ByVal i As Integer, ByVal b As Base)
            Me.New(b)
        End Sub
    End Class

    Private Function ReturnBase() As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnBase(ByVal d As Derived) As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnDerived(ByVal b As Base) As Derived
        Return b
    End Function

    Private Function ReturnDerived2(ByVal b As Base) As Derived
        Return ReturnBase()
    End Function

    Public Sub New()
        Dim b As Base = New Derived
        Dim d As Derived = {|FixAllInSolution:b|}
        d = New Base()
        Dim d2 As Derived = ReturnBase()
        d2 = ReturnBase(b)

        Dim t As Test = New Test()
        t.D = b
        t.D = b
        d = t.B
    End Sub

    Private Sub PassDerived(ByVal d As Derived)
    End Sub

    Private Sub PassDerived(ByVal i As Integer, ByVal d As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Dim d As Derived = b

        PassDerived(b)
        PassDerived(ReturnBase())
        PassDerived(1, b)
        PassDerived(1, ReturnBase())

        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add(b)
        Dim t As Test = New Test()
        t.testing(b)
        Dim foo2 As Func(Of Derived, Base) = Function(p_d) p_d
        Dim d2 As Derived = foo2(b)
        d2 = foo2(d)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Option Strict On
Public Class Program2
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Class Test
        Public Shared Narrowing Operator CType(t As Test) As Derived2
            Return New Derived2
        End Operator
    End Class

    Private Function returnDerived2_1() As Derived2
        Return New Derived1()
    End Function

    Private Function returnDerived2_2() As Derived2
        Return New Test()
    End Function

    Private Sub Foo1(ByVal b As Derived2)
    End Sub

    Private Sub Foo2(ByVal b As Base2)
    End Sub

    Private Sub Foo3(ByVal b1 As Derived2)
    End Sub

    Private Sub Foo3(ByVal i As Integer)
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Dim derived2 As Derived2 = b1
        derived2 = b3
        Dim base2 As Base2 = b1
        derived2 = d1
        d2 = New Test()

        Foo1(b1)
        Foo1(d1)
        Foo2(b1)
        Foo3(b1)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub returnD2(ByVal b As Base)
        Dim d As Derived = b
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Option Strict On
Public Class Program1
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Class Test
        Public Property D As Derived
        Public Property B As Base

        Public Sub New()
        End Sub

        Public Sub New(ByRef d As Derived)
            Me.D = d
        End Sub

        Public Sub testing(ByVal d As Derived)
        End Sub

        Private Sub testing(ByVal b As Base)
        End Sub
    End Class

    Class Test2
        Inherits Test

        Public Sub New(ByVal b As Base)
            MyBase.New(CType(b, Derived))
        End Sub
    End Class

    Class Test3
        Public Sub New(ByVal b As Derived)
        End Sub

        Public Sub New(ByVal i As Integer, ByVal b As Base)
            Me.New(CType(b, Derived))
        End Sub
    End Class

    Private Function ReturnBase() As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnBase(ByVal d As Derived) As Base
        Dim b As Base = New Base()
        Return b
    End Function

    Private Function ReturnDerived(ByVal b As Base) As Derived
        Return CType(b, Derived)
    End Function

    Private Function ReturnDerived2(ByVal b As Base) As Derived
        Return CType(ReturnBase(), Derived)
    End Function

    Public Sub New()
        Dim b As Base = New Derived
        Dim d As Derived = CType(b, Derived)
        d = New Base()
        Dim d2 As Derived = CType(ReturnBase(), Derived)
        d2 = ReturnBase(CType(b, Derived))

        Dim t As Test = New Test()
        t.D = CType(b, Derived)
        t.D = CType(b, Derived)
        d = CType(t.B, Derived)
    End Sub

    Private Sub PassDerived(ByVal d As Derived)
    End Sub

    Private Sub PassDerived(ByVal i As Integer, ByVal d As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Dim d As Derived = CType(b, Derived)

        PassDerived(CType(b, Derived))
        PassDerived(CType(ReturnBase(), Derived))
        PassDerived(1, CType(b, Derived))
        PassDerived(1, CType(ReturnBase(), Derived))

        Dim list As List(Of Derived) = New List(Of Derived)()
        list.Add(CType(b, Derived))
        Dim t As Test = New Test()
        t.testing(CType(b, Derived))
        Dim foo2 As Func(Of Derived, Base) = Function(p_d) p_d
        Dim d2 As Derived = foo2(CType(b, Derived))
        d2 = CType(foo2(d), Derived)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Option Strict On
Public Class Program2
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Class Test
        Public Shared Narrowing Operator CType(t As Test) As Derived2
            Return New Derived2
        End Operator
    End Class

    Private Function returnDerived2_1() As Derived2
        Return New Derived1()
    End Function

    Private Function returnDerived2_2() As Derived2
        Return CType(New Test(), Derived2)
    End Function

    Private Sub Foo1(ByVal b As Derived2)
    End Sub

    Private Sub Foo2(ByVal b As Base2)
    End Sub

    Private Sub Foo3(ByVal b1 As Derived2)
    End Sub

    Private Sub Foo3(ByVal i As Integer)
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Dim derived2 As Derived2 = CType(b1, Derived2)
        derived2 = CType(b3, Derived2)
        Dim base2 As Base2 = CType(b1, Base2)
        derived2 = CType(d1, Derived2)
        d2 = CType(New Test(), Derived2)

        Foo1(CType(b1, Derived2))
        Foo1(CType(d1, Derived2))
        Foo2(CType(b1, Base2))
        Foo3(b1)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Private Sub returnD2(ByVal b As Base)
        Dim d As Derived = CType(b, Derived)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function TestFixAllInDocumentBC30519() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Option Strict On
Public Class Program1
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Foo4(ByVal i As Integer, ByVal j As String, ByVal d As Derived1)
    End Sub

    Private Sub Foo4(ByVal j As String, ByVal i As Integer, ByVal d As Derived1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived2, ByVal Optional x As Integer = 1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived1, ParamArray d2list As Derived2())
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Foo4(1, "", b1)
        {|FixAllInDocument:Foo4(i:=1, j:="", b1)|}
        Foo5("", 1, b1)
        Foo5(d:=b1, i:=1, j:="", x:=1)
        Foo5(1, "", x:=1, d:=b1)
        Foo5(1, "", d:=b1, b2, b3, d1)
        Foo5("", 1, d:=b1, b2, b3, d1)
        Dim d2list = New Derived2() {}
        Foo5(j:="", i:=1, d:=b2, d2list)
        Dim d1list = New Derived1() {}
        Foo5(j:="", i:=1, d:=b2, d1list)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Option Strict On
Public Class Program2
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(b, 1)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(b, 1)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Option Strict On
Public Class Program1
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Foo4(ByVal i As Integer, ByVal j As String, ByVal d As Derived1)
    End Sub

    Private Sub Foo4(ByVal j As String, ByVal i As Integer, ByVal d As Derived1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived2, ByVal Optional x As Integer = 1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived1, ParamArray d2list As Derived2())
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Foo4(1, "", b1)
        Foo4(i:=1, j:="", CType(b1, Derived1))
        Foo5("", 1, b1)
        Foo5(d:=CType(b1, Derived2), i:=1, j:="", x:=1)
        Foo5(CStr(1), "", x:=1, d:=b1)
        Foo5(1, "", d:=b1, b2, b3, d1)
        Foo5("", 1, d:=b1, b2, b3, d1)
        Dim d2list = New Derived2() {}
        Foo5(j:="", i:=1, d:=CType(b2, Derived1), d2list)
        Dim d1list = New Derived1() {}
        Foo5(j:="", i:=1, d:=CType(b2, Derived1), d1list)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Option Strict On
Public Class Program2
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(b, 1)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(b, 1)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function TestFixAllInProjectBC30519() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Option Strict On
Public Class Program1
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Foo4(ByVal i As Integer, ByVal j As String, ByVal d As Derived1)
    End Sub

    Private Sub Foo4(ByVal j As String, ByVal i As Integer, ByVal d As Derived1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived2, ByVal Optional x As Integer = 1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived1, ParamArray d2list As Derived2())
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Foo4(1, "", b1)
        {|FixAllInProject:Foo4(i:=1, j:="", b1)|}
        Foo5("", 1, b1)
        Foo5(d:=b1, i:=1, j:="", x:=1)
        Foo5(1, "", x:=1, d:=b1)
        Foo5(1, "", d:=b1, b2, b3, d1)
        Foo5("", 1, d:=b1, b2, b3, d1)
        Dim d2list = New Derived2() {}
        Foo5(j:="", i:=1, d:=b2, d2list)
        Dim d1list = New Derived1() {}
        Foo5(j:="", i:=1, d:=b2, d1list)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Option Strict On
Public Class Program2
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(b, 1)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(b, 1)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Option Strict On
Public Class Program1
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Foo4(ByVal i As Integer, ByVal j As String, ByVal d As Derived1)
    End Sub

    Private Sub Foo4(ByVal j As String, ByVal i As Integer, ByVal d As Derived1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived2, ByVal Optional x As Integer = 1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived1, ParamArray d2list As Derived2())
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Foo4(1, "", b1)
        Foo4(i:=1, j:="", CType(b1, Derived1))
        Foo5("", 1, b1)
        Foo5(d:=CType(b1, Derived2), i:=1, j:="", x:=1)
        Foo5(CStr(1), "", x:=1, d:=b1)
        Foo5(1, "", d:=b1, b2, b3, d1)
        Foo5("", 1, d:=b1, b2, b3, d1)
        Dim d2list = New Derived2() {}
        Foo5(j:="", i:=1, d:=CType(b2, Derived1), d2list)
        Dim d1list = New Derived1() {}
        Foo5(j:="", i:=1, d:=CType(b2, Derived1), d1list)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Option Strict On
Public Class Program2
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(CType(b, Derived), 1)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(b, 1)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)>
        Public Async Function TestFixAllInSolutionBC30519() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Option Strict On
Public Class Program1
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Foo4(ByVal i As Integer, ByVal j As String, ByVal d As Derived1)
    End Sub

    Private Sub Foo4(ByVal j As String, ByVal i As Integer, ByVal d As Derived1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived2, ByVal Optional x As Integer = 1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived1, ParamArray d2list As Derived2())
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Foo4(1, "", b1)
        {|FixAllInSolution:Foo4(i:=1, j:="", b1)|}
        Foo5("", 1, b1)
        Foo5(d:=b1, i:=1, j:="", x:=1)
        Foo5(1, "", x:=1, d:=b1)
        Foo5(1, "", d:=b1, b2, b3, d1)
        Foo5("", 1, d:=b1, b2, b3, d1)
        Dim d2list = New Derived2() {}
        Foo5(j:="", i:=1, d:=b2, d2list)
        Dim d1list = New Derived1() {}
        Foo5(j:="", i:=1, d:=b2, d1list)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Option Strict On
Public Class Program2
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(b, 1)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(b, 1)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Option Strict On
Public Class Program1
    Interface Base1
    End Interface

    Interface Base2
        Inherits Base1
    End Interface

    Interface Base3
    End Interface

    Class Derived1
        Implements Base2, Base3
    End Class

    Class Derived2
        Inherits Derived1
    End Class

    Private Sub Foo4(ByVal i As Integer, ByVal j As String, ByVal d As Derived1)
    End Sub

    Private Sub Foo4(ByVal j As String, ByVal i As Integer, ByVal d As Derived1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived2, ByVal Optional x As Integer = 1)
    End Sub

    Private Sub Foo5(ByVal j As String, ByVal i As Integer, ByVal d As Derived1, ParamArray d2list As Derived2())
    End Sub

    Private Sub M2(ByVal b1 As Base1, ByVal b2 As Base2, ByVal b3 As Base3, ByVal d1 As Derived1, ByVal d2 As Derived2)
        Foo4(1, "", b1)
        Foo4(i:=1, j:="", CType(b1, Derived1))
        Foo5("", 1, b1)
        Foo5(d:=CType(b1, Derived2), i:=1, j:="", x:=1)
        Foo5(CStr(1), "", x:=1, d:=b1)
        Foo5(1, "", d:=b1, b2, b3, d1)
        Foo5("", 1, d:=b1, b2, b3, d1)
        Dim d2list = New Derived2() {}
        Foo5(j:="", i:=1, d:=CType(b2, Derived1), d2list)
        Dim d1list = New Derived1() {}
        Foo5(j:="", i:=1, d:=CType(b2, Derived1), d1list)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Option Strict On
Public Class Program2
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(CType(b, Derived), 1)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Option Strict On
Public Class Program3
    Class Base
    End Class

    Class Derived
        Inherits Base
    End Class

    Public Sub Foo(ByRef d As Derived, i As Integer)
    End Sub

    Public Sub Foo(ByRef d As Derived, d2 As Derived)
    End Sub

    Public Sub M()
        Dim b As Base = New Derived
        Foo(CType(b, Derived), 1)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function
    End Class
End Namespace
