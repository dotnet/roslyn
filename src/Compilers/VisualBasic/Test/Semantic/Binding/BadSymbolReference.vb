' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Binding

    Public Class BadSymbolReference : Inherits BasicTestBase

        <Fact>
        Public Sub MissingTypes1()

            Dim cl2 = TestReferences.SymbolsTests.MissingTypes.CL2
            Dim cl3 = TestReferences.SymbolsTests.MissingTypes.CL3

            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="MissingTypes1_1">
        <file name="a.vb">
Option Strict Off

Module Module1

    Sub Main()
        Dim x1 As CL3_C1

        x1 = Nothing
    End Sub

End Module
        </file>
    </compilation>, {cl2, cl3})

            Dim a_vb =
        <file name="a.vb">
Option Strict Off

Module Module1

    Private f1 As CL3_C1

    Sub Main()
        Dim x1 As New CL3_C1
        x1 = Nothing
    End Sub

    Sub Test1
        Dim x2 As CL3_C3
        x2 = Nothing
    End Sub

    Sub Test2
        Dim x3 As System.Action(Of CL3_C3)
        x3 = Nothing
    End Sub

    Sub Test3
        CL3_C1.Test1()
    End Sub

    Sub Test4
        Global.CL3_C1.Test1()
    End Sub

    Sub Test5
        C1(Of CL3_C1).Test1()
    End Sub

    Sub Test6
        Global.C1(Of CL3_C1).Test1()
    End Sub

    Sub Test7
        Dim x1 As Object
        x1 = New CL3_C1
    End Sub

    Sub Test8
        Dim x4 As CL3_C3()
        x4=Nothing
    End Sub

    Sub Test9
        Dim x4 As Object
        x4 = New CL3_C3() {}
    End Sub

    Sub Test10
        Dim x5 As C1(Of CL3_C1)
        x5 = Nothing
    End Sub

    Sub Test11
        Dim x5 As Object
        x5 = New C1(Of CL3_C1)()
    End Sub

    Sub Test
        Dim v As New CL3_C4
    End Sub

    Sub Test12
        Dim w As New CL3_C5
    End Sub

    Sub Test13
        Dim y As New CL3_C2()
        Dim z As Object = y.x
    End Sub

    Sub Test15
        Dim y As New CL3_C2()
        Dim z As Object
        z = y.u
    End Sub

    Sub Test16
        Dim y As New CL3_C2()
        Dim z As Object
        z = y.y
    End Sub

    Sub Test17
        Dim y As New CL3_C2()
        Dim z As Object
        z = y.z
    End Sub

    Sub Test18
        Dim y As New CL3_C2()
        Dim z As Object
        z = y.v
    End Sub

    Sub Test19
        Dim z As Object
        z = f1
    End Sub

    Class C2
        Inherits CL3_C1
    End Class

    Class C3
        Inherits System.Collections.Generic.List(Of CL3_C1)
    End Class

    Class C4
        Inherits CL3_S1
    End Class

    Interface I2
        Inherits CL3_I1, I1(Of CL3_I1)
    End Interface

    Class C5
        Implements CL3_I1, I1(Of CL3_I1)
    End Class

    Sub Test20
        Dim x6 As CL3_S1?
        x6 = Nothing
    End Sub

    Sub Test21
        CL3_C2.Test1()
    End Sub

    Sub Test22
        CL3_C2.Test1(1)
    End Sub

    Sub Test23
        CL3_C2.Test3()
    End Sub

    Sub Test24
        CL3_C2.Test4()
    End Sub

    Sub Test24_1
        CL3_C2.Test4(nothing)
    End Sub

    Sub Test25
        Dim y As New CL3_C2()
        y.Test1()
    End Sub

    Sub Test26
        Dim y As New CL3_C2()
        y.Test1(1)
    End Sub

    Sub Test27
        Dim y As New CL3_C2()
        y.Test2(2)
    End Sub

    Sub Test28
        Dim y As New CL3_C2()
        Dim d1 As CL3_D1 = AddressOf y.Test2
    End Sub

    Sub Test29
        Dim y As New CL3_C2()
        y.v(Nothing)
    End Sub

    Sub Test30
        Dim y As New CL3_C2()
        y.w(Nothing)
    End Sub

    Sub Test31
        Dim u As CL3_D1 = Sub(uuu) System.Console.WriteLine()
    End Sub

    Sub Test32
        Dim zz As Object = CL3_C2.Test5(Nothing)
    End Sub
End Module

Class C1(Of T)

    Shared Sub Test1()
    End Sub
End Class

Interface I1(Of T)
End Interface

        </file>

            Dim source =
    <compilation name="MissingTypes1_1">
        <%= a_vb %>
    </compilation>

            Dim errors =
<errors>
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        CL3_C1.Test1()
        ~~~~~~~~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        Global.CL3_C1.Test1()
        ~~~~~~~~~~~~~~~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        Dim z As Object = y.x
                          ~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        Inherits CL3_C1
                 ~~~~~~
BC30258: Classes can inherit only from other classes.
        Inherits CL3_S1
                 ~~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_I1'. Add one to your project.
        Inherits CL3_I1, I1(Of CL3_I1)
                 ~~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_I1'. Add one to your project.
        Implements CL3_I1, I1(Of CL3_I1)
                   ~~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        CL3_C2.Test1()
        ~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
        CL3_C2.Test1(1)
        ~~~~~~~~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        CL3_C2.Test3()
        ~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Test4' accepts this number of arguments.
        CL3_C2.Test4()
               ~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        CL3_C2.Test4(nothing)
        ~~~~~~~~~~~~~~~~~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        y.Test1()
        ~~~~~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        Dim d1 As CL3_D1 = AddressOf y.Test2
                           ~~~~~~~~~~~~~~~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        y.w(Nothing)
        ~~~
BC30652: Reference required to assembly 'CL2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'CL2_C1'. Add one to your project.
        Dim u As CL3_D1 = Sub(uuu) System.Console.WriteLine()
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {cl3}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation2, errors)

            Dim cl3Source =
    <compilation name="cl3">
        <file name="a.vb"><%= TestResources.SymbolsTests.MissingTypes.CL3_VB %></file>
    </compilation>

            Dim cl3Compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(cl3Source, {cl2})

            CompilationUtils.AssertNoErrors(cl3Compilation)

            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {New VisualBasicCompilationReference(cl3Compilation)}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation3, errors)

            Dim cl3BadCompilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(cl3Source, {cl3})

            Dim compilation4 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {New VisualBasicCompilationReference(cl3BadCompilation1)}, options:=TestOptions.ReleaseExe)

            Dim errors2 =
<errors>
BC30002: Type 'CL2_C1' is not defined.
        CL3_C1.Test1()
        ~~~~~~~~~~~~
BC30002: Type 'CL2_C1' is not defined.
        Global.CL3_C1.Test1()
        ~~~~~~~~~~~~~~~~~~~
BC30002: Type 'CL2_C1' is not defined.
        Dim z As Object = y.x
                          ~~~
BC30002: Type 'CL2_C1' is not defined.
        Inherits CL3_C1
                 ~~~~~~
BC30258: Classes can inherit only from other classes.
        Inherits CL3_S1
                 ~~~~~~
BC30002: Type 'CL2_I1' is not defined.
        Inherits CL3_I1, I1(Of CL3_I1)
                 ~~~~~~
BC30002: Type 'CL2_I1' is not defined.
        Implements CL3_I1, I1(Of CL3_I1)
                   ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
        CL3_C2.Test1()
        ~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
        CL3_C2.Test1(1)
        ~~~~~~~~~~~~
BC30002: Type 'CL2_C1' is not defined.
        CL3_C2.Test3()
        ~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Test4' accepts this number of arguments.
        CL3_C2.Test4()
               ~~~~~
BC30002: Type 'CL2_C1' is not defined.
        CL3_C2.Test4(nothing)
        ~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'CL2_C1' is not defined.
        y.Test1()
        ~~~~~~~~~
BC30002: Type 'CL2_C1' is not defined.
        Dim d1 As CL3_D1 = AddressOf y.Test2
                           ~~~~~~~~~~~~~~~~~
BC30002: Type 'CL2_C1' is not defined.
        y.w(Nothing)
        ~~~
BC30002: Type 'CL2_C1' is not defined.
        Dim u As CL3_D1 = Sub(uuu) System.Console.WriteLine()
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation4, errors2)

            Dim cl3BadCompilation2 = CompilationUtils.CreateCompilationWithMscorlib40(cl3Source)

            Dim errors3 =
<errors>
BC30002: Type 'CL2_C1' is not defined.
    Inherits CL2_C1
             ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public Shared Function Test2() As CL2_C1
                                      ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public Function Test3() As CL2_C1
                               ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public Shared Function Test1() As CL2_C1
                                      ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public x As CL2_C1
                ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public Shared Function Test3() As CL2_C1
                                      ~~~~~~
BC30002: Type 'CL2_I1' is not defined.
    Implements CL2_I1, CL2_I2
               ~~~~~~
BC30002: Type 'CL2_I2' is not defined.
    Implements CL2_I1, CL2_I2
                       ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
Public Delegate Sub CL3_D1(x As CL2_C1)
                                ~~~~~~
BC30002: Type 'CL2_I1' is not defined.
    Implements CL2_I1
               ~~~~~~
BC30002: Type 'CL2_I1' is not defined.
    Inherits CL2_I1
             ~~~~~~
</errors>

            CompilationUtils.AssertTheseDiagnostics(cl3BadCompilation2, errors3)

            Dim compilation5 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {New VisualBasicCompilationReference(cl3BadCompilation2)}, options:=TestOptions.ReleaseExe)

            Dim errors5 =
<errors>
BC30258: Classes can inherit only from other classes.
        Inherits CL3_S1
                 ~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
        CL3_C2.Test1(1)
        ~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Test4' accepts this number of arguments.
        CL3_C2.Test4()
               ~~~~~
BC30521: Overload resolution failed because no accessible 'Test4' is most specific for these arguments:
    'Public Shared Sub Test4(x As CL3_C1)': Not most specific.
    'Public Shared Sub Test4(x As CL3_C3)': Not most specific.
        CL3_C2.Test4(nothing)
               ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        y.Test1()
        ~~~~~~~
BC31143: Method 'Public Sub Test2(x As Integer)' does not have a signature compatible with delegate 'Delegate Sub CL3_D1(x As CL2_C1)'.
        Dim d1 As CL3_D1 = AddressOf y.Test2
                                     ~~~~~~~
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation5, errors5)

            Dim errors6 =
<errors>
BC30258: Classes can inherit only from other classes.
        Inherits CL3_S1
                 ~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
        CL3_C2.Test1(1)
        ~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Test4' accepts this number of arguments.
        CL3_C2.Test4()
               ~~~~~
BC30521: Overload resolution failed because no accessible 'Test4' is most specific for these arguments:
    'Public Shared Sub Test4(x As CL3_C1)': Not most specific.
    'Public Shared Sub Test4(x As CL3_C3)': Not most specific.
        CL3_C2.Test4(nothing)
               ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        y.Test1()
        ~~~~~~~
BC31143: Method 'Public Sub Test2(x As Integer)' does not have a signature compatible with delegate 'Delegate Sub CL3_D1(x As CL2_C1)'.
        Dim d1 As CL3_D1 = AddressOf y.Test2
                                     ~~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Inherits CL2_C1
             ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public Shared Function Test2() As CL2_C1
                                      ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public Function Test3() As CL2_C1
                               ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public Shared Function Test1() As CL2_C1
                                      ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public x As CL2_C1
                ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
    Public Shared Function Test3() As CL2_C1
                                      ~~~~~~
BC30002: Type 'CL2_I1' is not defined.
    Implements CL2_I1, CL2_I2
               ~~~~~~
BC30002: Type 'CL2_I2' is not defined.
    Implements CL2_I1, CL2_I2
                       ~~~~~~
BC30002: Type 'CL2_C1' is not defined.
Public Delegate Sub CL3_D1(x As CL2_C1)
                                ~~~~~~
BC30002: Type 'CL2_I1' is not defined.
    Implements CL2_I1
               ~~~~~~
BC30002: Type 'CL2_I1' is not defined.
    Inherits CL2_I1
             ~~~~~~
</errors>
            Dim cl4Source =
    <compilation name="cl4">
        <%= a_vb %>
        <file name="b.vb"><%= TestResources.SymbolsTests.MissingTypes.CL3_VB %></file>
    </compilation>

            Dim compilation6 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(cl4Source)

            CompilationUtils.AssertTheseDiagnostics(compilation6, errors6)

            CompilationUtils.AssertNoErrors(compilation1)
        End Sub

        <Fact>
        Public Sub MissingTypes2()

            Dim source =
    <compilation name="MissingTypes1_1">
        <file name="a.vb">
Option Strict Off

Module Module1

    Private f1 As CL3_C1

    Sub Main()
        Dim x1 As New CL3_C1
        Dim x2 As CL3_C3
        Dim x3 As System.Action(Of CL3_C3)

        x1 = Nothing
        x2 = Nothing
        x3 = Nothing

        CL3_C1.Test1()
        Global.CL3_C1.Test1()
        C1(Of CL3_C1).Test1()
        Global.C1(Of CL3_C1).Test1()

        x1 = New CL3_C1
        Dim x4 As CL3_C3()
        x4 = New CL3_C3() {}

        Dim x5 As C1(Of CL3_C1)
        x5 = New C1(Of CL3_C1)()

        Dim v As New CL3_C4
        Dim w As New CL3_C5

        Dim y As New CL3_C2()

        Dim z As Object = y.x

        z = y.u
        z = y.y
        z = y.z
        z = y.v

        z = f1

    End Sub

    Class C2
        Inherits CL3_C1
    End Class

    Class C3
        Inherits System.Collections.Generic.List(Of CL3_C1)
    End Class

    Class C4
        Inherits CL3_S1
    End Class

    Interface I2
        Inherits CL3_I1, I1(Of CL3_I1)
    End Interface

    Class C5
        Implements CL3_I1, I1(Of CL3_I1)
    End Class

    Sub Test2()
        Dim y As New CL3_C2()
        Dim x6 As CL3_S1?
        x6 = Nothing

        CL3_C2.Test1()
        CL3_C2.Test1(1)

        CL3_C2.Test3()

        CL3_C2.Test4()

        y.Test1()
        y.Test1(1)
        y.Test2(2)

        Dim d1 As CL3_D1 = AddressOf y.Test2

        y.v(Nothing)

        y.w(Nothing)

        Dim u As CL3_D1 = Sub(uuu) System.Console.WriteLine()

        Dim zz As Object = CL3_C2.Test5(Nothing)

    End Sub
End Module

Class C1(Of T)

    Shared Sub Test1()
    End Sub
End Class

Interface I1(Of T)
End Interface

        </file>
    </compilation>

            Dim errors =
<errors>
BC30258: Classes can inherit only from other classes.
        Inherits CL3_S1
                 ~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
        CL3_C2.Test1(1)
        ~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Test4' accepts this number of arguments.
        CL3_C2.Test4()
               ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        y.Test1()
        ~~~~~~~
BC31143: Method 'Public Sub Test2(x As Integer)' does not have a signature compatible with delegate 'Delegate Sub CL3_D1(x As CL2_C1)'.
        Dim d1 As CL3_D1 = AddressOf y.Test2
                                     ~~~~~~~
</errors>

            Dim cl3Source =
    <compilation name="cl3">
        <file name="a.vb"><%= TestResources.SymbolsTests.MissingTypes.CL3_VB %></file>
    </compilation>

            Dim cl3BadCompilation = CompilationUtils.CreateCompilationWithMscorlib40(cl3Source)

            Dim compilation4 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {New VisualBasicCompilationReference(cl3BadCompilation)}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation4, errors)
        End Sub

        ' Constant fields should be bound in the declaration phase.
        <Fact()>
        Public Sub TestConstantEvalAtDeclarationPhase()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C
    Const F As String = F
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source)
            compilation.AssertTheseDeclarationDiagnostics(<expected>
BC30500: Constant 'F' cannot depend on its own value.
    Const F As String = F
          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub TestConstantEvalAcrossCompilations()
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class A
    Public Const A1 As String = Nothing
End Class
Public Class B
    Public Const B1 As String = A.A1
End Class
Public Class C
    Public Const C1 As String = B.B1
End Class
]]>
                    </file>
                </compilation>
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class D
    Public Const D1 As String = E.E1
End Class
Public Class E
    Public Const E1 As String = C.C1
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40(source1)
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(source2, {New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertNoErrors()
            compilation1.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub TestCyclicConstantEvalAcrossCompilations()
            Dim source1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class A
    Public Const A1 As String = B.B1
End Class
Public Class B
    Public Const B1 As String = A.A1
End Class
Public Class C
    Public Const C1 As String = B.B1
End Class
]]>
                    </file>
                </compilation>
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class D
    Public Const D1 As String = D1
End Class
]]>
                    </file>
                </compilation>
            Dim source3 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class E
    Public Const E1 As String = F.F1
End Class
Public Class F
    Public Const F1 As String = C.C1
End Class
]]>
                    </file>
                </compilation>
            Dim source4 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class G
    Public Const G1 As String = F.F1 + D.D1
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40(source1)
            Dim reference1 = New VisualBasicCompilationReference(compilation1)
            Dim compilation2 = CreateCompilationWithMscorlib40(source2)
            Dim reference2 = New VisualBasicCompilationReference(compilation2)
            Dim compilation3 = CreateCompilationWithMscorlib40AndReferences(source3, {reference1})
            Dim reference3 = New VisualBasicCompilationReference(compilation3)
            Dim compilation4 = CreateCompilationWithMscorlib40AndReferences(source4, {reference2, reference3})
            compilation4.AssertNoErrors()
            compilation3.AssertNoErrors()
            compilation2.AssertTheseDiagnostics(<expected>
BC30500: Constant 'D1' cannot depend on its own value.
    Public Const D1 As String = D1
                 ~~
</expected>)
            compilation1.AssertTheseDiagnostics(<expected>
BC30500: Constant 'A1' cannot depend on its own value.
    Public Const A1 As String = B.B1
                 ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MissingTypeInTypeArgumentsOfImplementedInterface()
            Dim lib1 = CreateCompilationWithMscorlib40(
                <compilation name="MissingTypeInTypeArgumentsOfImplementedInterface1">
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    public Interface I1(Of Out T1)
    End Interface

    public Interface I2
    End Interface

    Public Interface I6(Of In T1)
    End Interface

    Public Class C10(Of T As I1(Of I2))
    End Class
End Namespace
]]>
                    </file>
                </compilation>, options:=TestOptions.ReleaseDll)

            Dim lib1Ref = New VisualBasicCompilationReference(lib1)

            Dim lib2 = CreateCompilationWithMscorlib40AndReferences(
                <compilation name="MissingTypeInTypeArgumentsOfImplementedInterface2">
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    Public Interface I3
        Inherits I2
    End Interface

End Namespace
]]>
                    </file>
                </compilation>, {lib1Ref}, TestOptions.ReleaseDll)

            Dim lib2Ref = New VisualBasicCompilationReference(lib2)

            Dim lib3 = CreateCompilationWithMscorlib40AndReferences(
                <compilation name="MissingTypeInTypeArgumentsOfImplementedInterface3">
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    Public Class C4
        Implements I1(Of I3)
    End Class

    Public Interface I5
        Inherits I1(Of I3)
    End Interface

    Public Class C8(Of T As I6(Of I3))
    End Class
End Namespace
]]>
                    </file>
                </compilation>, {lib1Ref, lib2Ref}, TestOptions.ReleaseDll)

            Dim lib3Ref = New VisualBasicCompilationReference(lib3)

            Dim lib4Def =
                <compilation name="MissingTypeInTypeArgumentsOfImplementedInterface4">
                    <file name="c.vb"><![CDATA[
Option Strict On

Namespace ErrorTest

    Class Test
        Sub Test(y As C4)
            Dim x As I1(Of I2) = y
        End Sub
    End Class

    Public Class C6
        Implements I5
    End Class

    Public Class C7
        Inherits C4
    End Class

    Class Test3(Of T As C4)
        Sub Test(y3 As T)
            Dim x As I1(Of I2) = y3
        End Sub
    End Class

    Class Test4(Of T As I5)
        Sub Test(y4 As T)
            Dim x As I1(Of I2) = y4
        End Sub
    End Class
    
    Class Test5
        Sub Test(y5 As I5)
            Dim x As I1(Of I2) = y5
        End Sub
    End Class

    Public Class C9
        Inherits C8(Of I6(Of I2))
    End Class

    Public Class C11
        Inherits C10(Of C4)
    End Class

    Public Class C12
        Inherits C10(Of I5)
    End Class

    Class Test6
        Sub Test(x As C8(Of I6(Of I2)))
        End Sub
        Sub Test(x As C10(Of C4))
        End Sub
        Sub Test(x As C10(Of I5))
        End Sub
    End Class
End Namespace
]]>
                    </file>
                </compilation>

            Dim lib4 = CreateCompilationWithMscorlib40AndReferences(lib4Def, {lib1Ref, lib3Ref}, TestOptions.ReleaseDll)

            Dim expectedErrors =
<expected>
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
            Dim x As I1(Of I2) = y
                                 ~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
        Implements I5
                   ~~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
            Dim x As I1(Of I2) = y3
                                 ~~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
            Dim x As I1(Of I2) = y4
                                 ~~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
            Dim x As I1(Of I2) = y5
                                 ~~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
    Public Class C9
                 ~~
BC32044: Type argument 'I6(Of I2)' does not inherit from or implement the constraint type 'I6(Of I3)'.
    Public Class C9
                 ~~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
    Public Class C11
                 ~~~
BC32044: Type argument 'C4' does not inherit from or implement the constraint type 'I1(Of I2)'.
    Public Class C11
                 ~~~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
    Public Class C12
                 ~~~
BC32044: Type argument 'I5' does not inherit from or implement the constraint type 'I1(Of I2)'.
    Public Class C12
                 ~~~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
        Sub Test(x As C8(Of I6(Of I2)))
                 ~
BC32044: Type argument 'I6(Of I2)' does not inherit from or implement the constraint type 'I6(Of I3)'.
        Sub Test(x As C8(Of I6(Of I2)))
                 ~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
        Sub Test(x As C10(Of C4))
                 ~
BC32044: Type argument 'C4' does not inherit from or implement the constraint type 'I1(Of I2)'.
        Sub Test(x As C10(Of C4))
                 ~
BC30652: Reference required to assembly 'MissingTypeInTypeArgumentsOfImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I3'. Add one to your project.
        Sub Test(x As C10(Of I5))
                 ~
BC32044: Type argument 'I5' does not inherit from or implement the constraint type 'I1(Of I2)'.
        Sub Test(x As C10(Of I5))
                 ~
</expected>

            AssertTheseDiagnostics(lib4, expectedErrors)

            lib4 = CreateCompilationWithMscorlib40AndReferences(lib4Def, {lib1Ref, lib2Ref, lib3Ref}, TestOptions.ReleaseDll)

            CompileAndVerify(lib4)

            lib4 = CreateCompilationWithMscorlib40AndReferences(lib4Def, {lib1.EmitToImageReference(), lib3.EmitToImageReference()}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(lib4, expectedErrors)
        End Sub

        <Fact()>
        Public Sub MissingImplementedInterface()
            Dim lib1 = CreateCompilationWithMscorlib40(
                <compilation name="MissingImplementedInterface1">
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    public Interface I1
        Sub M1()
    End Interface

    Public Class C9(Of T As I1)
    End Class
End Namespace
]]>
                    </file>
                </compilation>, options:=TestOptions.ReleaseDll)

            Dim lib1Ref = New VisualBasicCompilationReference(lib1)

            Dim lib2 = CreateCompilationWithMscorlib40AndReferences(
                <compilation name="MissingImplementedInterface2">
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    Public Interface I2
        Inherits I1
    End Interface

    Public Class C12
        Implements I2

        Private Sub M1() Implements I1.M1
        End Sub
    End Class

End Namespace
]]>
                    </file>
                </compilation>, {lib1Ref}, TestOptions.ReleaseDll)

            Dim lib2Ref = New VisualBasicCompilationReference(lib2)

            Dim lib3 = CreateCompilationWithMscorlib40AndReferences(
                <compilation name="MissingImplementedInterface3">
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    Public Class C4
        Implements I2

        Private Sub M1() Implements I1.M1
        End Sub
    End Class

    Public Interface I5
        Inherits I2
    End Interface

    Public Class C13
        Inherits C12
    End Class
End Namespace
]]>
                    </file>
                </compilation>, {lib1Ref, lib2Ref}, TestOptions.ReleaseDll)

            Dim lib3Ref = New VisualBasicCompilationReference(lib3)

            Dim lib4Def =
                <compilation name="MissingImplementedInterface4">
                    <file name="c.vb"><![CDATA[
Option Strict On

Namespace ErrorTest

    Class Test
        Sub Test(y As C4)
            Dim x As I1 = y
        End Sub
    End Class

    Public Class C6
        Implements I5

        Private Sub M1() Implements I1.M1
        End Sub
    End Class

    Public Class C7
        Inherits C4
    End Class

    Class Test2
        Sub Test2(x as I5, y As C4)
            x.M1()
            y.M1()
        End Sub
    End Class

    Class Test3(Of T As C4)
        Sub Test(y3 As T)
            Dim x As I1 = y3
            y3.M1()
        End Sub
    End Class

    Class Test4(Of T As I5)
        Sub Test(y4 As T)
            Dim x As I1 = y4
            y4.M1()
        End Sub
    End Class

    Public Class C8
        Implements I5 'C8

        Private Sub M1() Implements I5.M1
        End Sub
    End Class

    Public Class C10 
        Inherits C9(Of C4)
    End Class

    Public Class C11 
        Inherits C9(Of I5)
    End Class

    Class Test5
        Sub Test(x As C9(Of C4))
        End Sub
        Sub Test(x As C9(Of I5))
        End Sub

        Sub Test(c13 As C13)
            Dim x As I1 = c13
        End Sub
    End Class
End Namespace
]]>
                    </file>
                </compilation>

            Dim lib4 = CreateCompilationWithMscorlib40AndReferences(lib4Def, {lib1Ref, lib3Ref}, TestOptions.ReleaseDll)

            Dim expectedErrors =
<expected>
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
            Dim x As I1 = y
                          ~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
        Implements I5
                   ~~
BC31035: Interface 'I1' is not implemented by this class.
        Private Sub M1() Implements I1.M1
                                    ~~
BC30456: 'M1' is not a member of 'I5'.
            x.M1()
            ~~~~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
            x.M1()
            ~~~~
BC30390: 'C4.Private Sub M1()' is not accessible in this context because it is 'Private'.
            y.M1()
            ~~~~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
            Dim x As I1 = y3
                          ~~
BC30390: 'C4.Private Sub M1()' is not accessible in this context because it is 'Private'.
            y3.M1()
            ~~~~~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
            Dim x As I1 = y4
                          ~~
BC30456: 'M1' is not a member of 'T'.
            y4.M1()
            ~~~~~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
            y4.M1()
            ~~~~~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
        Implements I5 'C8
                   ~~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
        Private Sub M1() Implements I5.M1
                                    ~~
BC30401: 'M1' cannot implement 'M1' because there is no matching sub on interface 'I5'.
        Private Sub M1() Implements I5.M1
                                    ~~~~~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
    Public Class C10 
                 ~~~
BC32044: Type argument 'C4' does not inherit from or implement the constraint type 'I1'.
    Public Class C10 
                 ~~~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
    Public Class C11 
                 ~~~
BC32044: Type argument 'I5' does not inherit from or implement the constraint type 'I1'.
    Public Class C11 
                 ~~~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
        Sub Test(x As C9(Of C4))
                 ~
BC32044: Type argument 'C4' does not inherit from or implement the constraint type 'I1'.
        Sub Test(x As C9(Of C4))
                 ~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I2'. Add one to your project.
        Sub Test(x As C9(Of I5))
                 ~
BC32044: Type argument 'I5' does not inherit from or implement the constraint type 'I1'.
        Sub Test(x As C9(Of I5))
                 ~
BC30652: Reference required to assembly 'MissingImplementedInterface2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C12'. Add one to your project.
            Dim x As I1 = c13
                          ~~~
</expected>

            AssertTheseDiagnostics(lib4, expectedErrors)

            lib4 = CreateCompilationWithMscorlib40AndReferences(lib4Def, {lib1Ref, lib2Ref, lib3Ref}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(lib4,
<expected>
BC30390: 'C4.Private Sub M1()' is not accessible in this context because it is 'Private'.
            y.M1()
            ~~~~
BC30390: 'C4.Private Sub M1()' is not accessible in this context because it is 'Private'.
            y3.M1()
            ~~~~~
</expected>)

            lib4 = CreateCompilationWithMscorlib40AndReferences(lib4Def, {lib1.EmitToImageReference(), lib3.EmitToImageReference()}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(lib4, expectedErrors)
        End Sub

        <Fact()>
        Public Sub MissingBaseClass()
            Dim lib1 = CreateCompilationWithMscorlib40(
                <compilation name="MissingBaseClass1">
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    public Class C1
        Sub M1()
        End Sub
    End Class

    Public Class C6(Of T As C1)
    End Class
End Namespace
]]>
                    </file>
                </compilation>, options:=TestOptions.ReleaseDll)

            Dim lib1Ref = New VisualBasicCompilationReference(lib1)

            Dim lib2 = CreateCompilationWithMscorlib40AndReferences(
                <compilation name="MissingBaseClass2">
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    Public Class C2
        Inherits C1
    End Class

End Namespace
]]>
                    </file>
                </compilation>, {lib1Ref}, TestOptions.ReleaseDll)

            Dim lib2Ref = New VisualBasicCompilationReference(lib2)

            Dim lib3 = CreateCompilationWithMscorlib40AndReferences(
                <compilation name="MissingBaseClass3">
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    Public Class C4
        Inherits C2
    End Class

End Namespace
]]>
                    </file>
                </compilation>, {lib1Ref, lib2Ref}, TestOptions.ReleaseDll)

            Dim lib3Ref = New VisualBasicCompilationReference(lib3)

            Dim lib4Def =
                <compilation name="MissingBaseClass4">
                    <file name="c.vb"><![CDATA[
Option Strict On

Namespace ErrorTest

    Class Test
        Sub Test(y As C4)
            Dim x As C1 = y
        End Sub
    End Class

    Public Class C5
        Inherits C4
    End Class

    Class Test2
        Sub Test2(y As C4)
            y.M1()
        End Sub
    End Class

    Class Test3(Of T As C4)
        Sub Test(y3 As T)
            Dim x As C1 = y3
            y3.M1()
        End Sub
    End Class

    Public Class C7
        Inherits C6(Of C4)
    End Class

    Class Test4
        Sub Test(x As C6(Of C4))
        End Sub
    End Class
End Namespace
]]>
                    </file>
                </compilation>

            Dim lib4 = CreateCompilationWithMscorlib40AndReferences(lib4Def, {lib1Ref, lib3Ref}, TestOptions.ReleaseDll)

            Dim expectedErrors =
<expected>
BC30652: Reference required to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C2'. Add one to your project.
            Dim x As C1 = y
                          ~
BC30652: Reference required to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C2'. Add one to your project.
        Inherits C4
                 ~~
BC30456: 'M1' is not a member of 'C4'.
            y.M1()
            ~~~~
BC30652: Reference required to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C2'. Add one to your project.
            y.M1()
            ~~~~
BC30652: Reference required to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C2'. Add one to your project.
            Dim x As C1 = y3
                          ~~
BC30456: 'M1' is not a member of 'T'.
            y3.M1()
            ~~~~~
BC30652: Reference required to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C2'. Add one to your project.
            y3.M1()
            ~~~~~
BC30652: Reference required to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C2'. Add one to your project.
    Public Class C7
                 ~~
BC32044: Type argument 'C4' does not inherit from or implement the constraint type 'C1'.
    Public Class C7
                 ~~
BC30652: Reference required to assembly 'MissingBaseClass2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C2'. Add one to your project.
        Sub Test(x As C6(Of C4))
                 ~
BC32044: Type argument 'C4' does not inherit from or implement the constraint type 'C1'.
        Sub Test(x As C6(Of C4))
                 ~
</expected>

            AssertTheseDiagnostics(lib4, expectedErrors)

            lib4 = CreateCompilationWithMscorlib40AndReferences(lib4Def, {lib1Ref, lib2Ref, lib3Ref}, TestOptions.ReleaseDll)

            CompileAndVerify(lib4)

            lib4 = CreateCompilationWithMscorlib40AndReferences(lib4Def, {lib1.EmitToImageReference(), lib3.EmitToImageReference()}, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(lib4, expectedErrors)
        End Sub

        <Fact()>
        Public Sub OverloadResolutionUseSiteErrors()
            Dim missing = CreateCompilationWithMscorlib40(
                <compilation name="missing">
                    <file name="c.vb"><![CDATA[
Public Class Missing
End Class]]>
                    </file>
                </compilation>, options:=TestOptions.ReleaseDll)

            Dim missingRef = New VisualBasicCompilationReference(missing)

            Dim ilSource1 =
            <![CDATA[
.assembly extern missing
{
  .ver 0:0:0:0
}

.class public abstract auto ansi UseSiteErrors
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method UseSiteErrors::.ctor

  .method public newslot abstract strict virtual 
          instance int32&  M1(uint8 x) cil managed
  {
  } // end of method UseSiteErrors::M1

  .method public newslot abstract strict virtual 
          instance int32  M1(class [missing]Missing x) cil managed
  {
  } // end of method UseSiteErrors::M1

  .method public newslot abstract strict virtual 
          instance int32  M1(int32 x) cil managed
  {
  } // end of method UseSiteErrors::M1

} // end of class UseSiteErrors
]]>

            Dim compDef =
                <compilation>
                    <file name="c.vb"><![CDATA[
Namespace ErrorTest

    Public Class Test
        Sub M(x as UseSiteErrors)
            Dim y as Integer = x.M1(1)
        End Sub
    End Class

End Namespace
]]>
                    </file>
                </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compDef, ilSource1.Value, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC30652: Reference required to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Missing'. Add one to your project.
            Dim y as Integer = x.M1(1)
                               ~~~~~~~
</expected>)

            compilation = compilation.AddReferences(missingRef)
            CompileAndVerify(compilation)

            Dim ilSource2 =
            <![CDATA[
.class public abstract auto ansi UseSiteErrors
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method UseSiteErrors::.ctor

  .method public newslot abstract strict virtual 
          instance int32&  M1(uint8 x) cil managed
  {
  } // end of method UseSiteErrors::M1
} // end of class UseSiteErrors
]]>

            compilation = CompilationUtils.CreateCompilationWithCustomILSource(compDef, ilSource2.Value, TestOptions.ReleaseDll)
            ' ByRef return supported.
            AssertTheseDiagnostics(compilation)
        End Sub

    End Class

End Namespace

