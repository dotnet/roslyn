' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    ' This class tests binding of various expressions; i.e., the code in Binder_Expressions.vb
    '
    ' Tests should be added here for every construct that can be bound
    ' correctly, with a test that compiles, verifies, and runs code for that construct. 
    ' Tests should also be added here for every diagnostic that can be generated.
    Public Class Binder_Expressions_Tests
        Inherits BasicTestBase

        ' Test that BC30157 is generated for a member access off With when there is no containing With.
        <Fact>
        Public Sub MemberAccessNoContainingWith()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MemberAccessNoContainingWith">
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        Dim x as Integer
        x = .goo
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        x = .goo
            ~~~~
</expected>)
        End Sub

        ' Test field access off a local variable of structure type.
        <Fact>
        Public Sub FieldAccessInLocalStruct()
            CompileAndVerify(
<compilation name="FieldAccessInLocalStruct">
    <file name="a.vb">
Imports System        

Module M1
    Structure S1
        Public Field1 As Integer
    End Structure
    Sub Main()
        Dim x as S1
        x.Field1 = 123
        Console.WriteLine(x.Field1)
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="123")
        End Sub

        <WorkItem(679765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/679765")>
        <ConditionalFact(GetType(NoIOperationValidation))>
        Public Sub Bug679765()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        <%= SemanticResourceUtil.T_68086 %>
    </file>
</compilation>, references:={MsvbRef})
        End Sub

        <WorkItem(707924, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/707924")>
        <Fact()>
        Public Sub Bug707924a()
            Dim source = SemanticResourceUtil.T_1247520
            Dim result = VisualBasicSyntaxTree.ParseText(source).ToString()
            Assert.Equal(source, result)
        End Sub

        ' Test access to a local variable and assignment of them..
        <Fact>
        Public Sub LocalVariable1()
            CompileAndVerify(
<compilation name="LocalVariable1">
    <file name="a.vb">
Imports System        

Module M1
    Sub Main()
        Dim x as Integer
        Dim y as Long
        x = 143
        y = x
        Console.WriteLine(y)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="143")
        End Sub

        ' Test access to a local variable, parameter, type parameter, namespace with arity.
        <Fact>
        Public Sub LocalVariableWrongArity()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LocalVariable1">
    <file name="a.vb">
Imports System        

Module M1
    Sub Main()
        Dim x, y as Integer
        x = 143
        y = x(Of Integer)
        x = System.Collections(Of Decimal)
    End Sub

    Sub goo(y as string)
        dim z as string
        z = y(of Boolean)
    End Sub
End Module

Class Q(Of T, U)
    Sub a()
        dim x as integer = U(Of T)
    End Sub
End Class

    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC32045: 'x' has no type parameters and so cannot have type arguments.
        y = x(Of Integer)
             ~~~~~~~~~~~~
BC32045: 'System.Collections' has no type parameters and so cannot have type arguments.
        x = System.Collections(Of Decimal)
                              ~~~~~~~~~~~~
BC32045: 'y As String' has no type parameters and so cannot have type arguments.
        z = y(of Boolean)
             ~~~~~~~~~~~~
BC32045: 'U' has no type parameters and so cannot have type arguments.
        dim x as integer = U(Of T)
                            ~~~~~~
</expected>)
        End Sub

        ' Test access to a local variable and assignment of them..
        <Fact>
        Public Sub ArrayAssignment1()
            CompileAndVerify(
<compilation name="ArrayAssignment1">
    <file name="a.vb">
Imports System        

Module M1
    Sub Main()
        dim z(10) as string
        dim i as integer
        i = 2
        z(i) = "hello"
        Console.WriteLine(z(i))
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="hello")
        End Sub

        ' Test access to a local variable and assignment of them..
        <Fact>
        Public Sub ArrayAssignmentError1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ArrayAssignmentError1">
    <file name="a.vb">
Imports System        

Module M1
    Sub Main()
        dim z(10) as string
        z(1,1) = "world"
        z() = "world"
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30106: Number of indices exceeds the number of dimensions of the indexed array.
        z(1,1) = "world"
         ~~~~~
BC30105: Number of indices is less than the number of dimensions of the indexed array.
        z() = "world"
         ~~  
</expected>)
        End Sub

        ' Test array upper bound is correct
        <WorkItem(4225, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub CheckArrayUpperBound()
            Dim compilation = CompileAndVerify(
<compilation name="ArrayAssignmentError1">
    <file name="a.vb">
Imports System        
Module M
  Sub Main()
    dim a as integer() = New Integer(1) {}
    Console.WriteLine(a.GetLength(0))

    Console.WriteLine(New Integer(-1) {}.GetLength(0))
  End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
2
0
]]>)
        End Sub

        ' Test access to a local variable and assignment of them..
        <Fact()>
        Public Sub ArrayAssignmentError2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ArrayAssignmentErrors2">
    <file name="a.vb">
Option strict on     
Imports System        

Module M1
    Sub Main()
        dim z(10) as string
        dim i as uinteger
        ' Should report an implicit conversion error, uinteger can't be converted to integer
        z(i) = "world"
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30512: Option Strict On disallows implicit conversions from 'UInteger' to 'Integer'.
        z(i) = "world"
          ~
</expected>)
        End Sub

        ' Test access to a parameter (both simple and byref)
        <Fact>
        Public Sub Parameter1()
            CompileAndVerify(
<compilation name="Parameter1">
    <file name="a.vb">
Imports System        

Module M1
    Sub Goo(xParam as Integer, ByRef yParam As Long)
        Console.WriteLine("xParam = {0}", xParam)
        Console.WriteLine("yParam = {0}", yParam)
        xParam = 17
        yParam = 189
        Console.WriteLine("xParam = {0}", xParam)
        Console.WriteLine("yParam = {0}", yParam)
    End Sub

    Sub Main()
        Dim x as Integer
        Dim y as Long
        x = 143
        y = 16442
        Console.WriteLine("x = {0}", x)
        Console.WriteLine("y = {0}", y)
        Goo(x,y)
        Console.WriteLine("x = {0}", x)
        Console.WriteLine("y = {0}", y)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
x = 143
y = 16442
xParam = 143
yParam = 16442
xParam = 17
yParam = 189
x = 143
y = 189
]]>)
        End Sub

        ' Test object creation expression
        <Fact>
        Public Sub SimpleObjectCreation1()
            CompileAndVerify(
<compilation name="SimpleObjectCreation">
    <file name="a.vb">
Imports System   

Class C1
    Public Sub New()
    End Sub

    Sub Goo()
        Console.WriteLine("Called C1.Goo")
    End Sub
End Class     

Module M1
    Sub Main()
        dim c as C1
        c = new C1()
        c.Goo()
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="Called C1.Goo")
        End Sub

        ' Test object creation expression
        <Fact>
        Public Sub MeReference()
            CompileAndVerify(
<compilation name="MeReference">
    <file name="a.vb">
Imports System   

Class C1
    private _i as integer

    Public Sub New(i as integer)
       Me._i = i
       Console.WriteLine(Me._i)
    End Sub
End Class     

Module M1
    Sub Main()
        dim c as C1
        c = new C1(1)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="1")
        End Sub

        ' Test access to simple identifier that isn't found anywhere.
        <Fact>
        Public Sub SimpleNameNotFound()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SimpleNameNotFound">
    <file name="a.vb">
Imports System        

Module M1
    Sub Main()
        Dim x as Integer
        x = goo
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'goo' is not declared. It may be inaccessible due to its protection level.
        x = goo
            ~~~
</expected>)
        End Sub

        <WorkItem(538871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538871")>
        <Fact>
        Public Sub QualifiedNameBeforeDotNotFound()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="QualifiedNameBeforeDotNotFound">
    <file name="a.vb">
Imports System

Module MainModule

    Class A
    End Class
    Sub Main()
        Rdim123.Rdim456()
        A.B.Rdim456()
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'Rdim123' is not declared. It may be inaccessible due to its protection level.
        Rdim123.Rdim456()
        ~~~~~~~
BC30456: 'B' is not a member of 'MainModule.A'.
        A.B.Rdim456()
        ~~~
</expected>)
        End Sub

        ' Test access to qualified identifier not found, in various scopes
        <Fact>
        Public Sub QualifiedNameNotFound()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="QualifiedNameNotFound">
    <file name="a.vb">
Imports System        

Namespace N
End Namespace

Class C
    Public y as Integer
End Class

Module M1
    Sub Main()
        Dim x as Integer
        Dim cInstance as C
        cInstance = Nothing
        x = N.goo
        x = C.goo
        x = cInstance.goo
        x = M1.goo
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30456: 'goo' is not a member of 'N'.
        x = N.goo
            ~~~~~
BC30456: 'goo' is not a member of 'C'.
        x = C.goo
            ~~~~~
BC30456: 'goo' is not a member of 'C'.
        x = cInstance.goo
            ~~~~~~~~~~~~~
BC30456: 'goo' is not a member of 'M1'.
        x = M1.goo
            ~~~~~~
</expected>)
        End Sub

        ' Test access qualified identifier off of type parameter
        <Fact>
        Public Sub TypeParamCantQualify()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="TypeParamCantQualify">
    <file name="a.vb">
Imports System        

Class C(Of T)
    Public Sub f()
        dim x as Integer
        x = T.goo
    End Sub
End Class

    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC32098: Type parameters cannot be used as qualifiers.
        x = T.goo
            ~~~~~
</expected>)
        End Sub

        ' Test access to simple identifier that can be found, but has an error.
        <Fact>
        Public Sub BadSimpleName()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BadSimpleName">
    <file name="a.vb">
Imports System

Class Goo(Of T)
    Shared Public x As Integer
End Class

Module Module1
    Sub Main()
        Dim y As Integer
        y = Goo.x
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC32042: Too few type arguments to 'Goo(Of T)'.
        y = Goo.x
            ~~~
</expected>)
        End Sub

        ' Test access to qualified identifier that can be found, but has an error.
        <Fact>
        Public Sub BadQualifiedName()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BadQualifiedName">
    <file name="a.vb">
Imports System
Namespace N
    Class Goo(Of T)
        Shared Public x As Integer
    End Class
End Namespace

Class C
    Class Goo(Of T)
        Shared Public x As Integer
    End Class
End Class

Module Module1
    Sub Main()
        Dim y As Integer
        Dim cInstance as C
        cInstance = Nothing
        y = N.Goo.x
        y = C.Goo.x
        y = cInstance.Goo.x
        y = cInstance.Goo(Of Integer).x
    End Sub
End Module
    </file>
</compilation>)

            ' Note that we produce different (but I think better) error messages than Dev10.
            AssertTheseDiagnostics(compilation,
<expected>
BC32042: Too few type arguments to 'Goo(Of T)'.
        y = N.Goo.x
            ~~~~~
BC32042: Too few type arguments to 'C.Goo(Of T)'.
        y = C.Goo.x
            ~~~~~
BC32042: Too few type arguments to 'C.Goo(Of T)'.
        y = cInstance.Goo.x
            ~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        y = cInstance.Goo(Of Integer).x
            ~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ' Test access to instance member in various ways to get various errors.
        <Fact>
        Public Sub AccessInstanceFromStatic()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="AccessInstanceFromStatic">
    <file name="a.vb">
Class K
    Public Sub y()
    End Sub
    Public x As Integer

    Class Z
        Public Sub yy()
        End Sub
        Public xx As Integer

        Public Shared Sub goo()
            Dim v As Integer
            Dim zInstance As Z
            zInstance = Nothing
            y()
            v = x

            yy()
            v = xx

            zInstance.yy()
            v = zInstance.xx

            Z.yy()
            v = Z.xx

        End Sub
    End Class
End Class
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
            y()
            ~
BC30469: Reference to a non-shared member requires an object reference.
            v = x
                ~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
            yy()
            ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
            v = xx
                ~~
BC30469: Reference to a non-shared member requires an object reference.
            Z.yy()
            ~~~~
BC30469: Reference to a non-shared member requires an object reference.
            v = Z.xx
                ~~~~
</expected>)
        End Sub

        ' Test access to static member in various ways to get various errors.
        <Fact>
        Public Sub AccessStaticViaInstance()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="AccessStaticViaInstance">
    <file name="a.vb">
Class K
    Public Shared Sub y()
    End Sub
    Public Shared x As Integer

    Class Z
        Public Shared Sub yy()
        End Sub
        Public Shared xx As Integer

        Public Sub goo()
            Dim v As Integer
            Dim zInstance As Z
            zInstance = Nothing
            y()
            v = x

            yy()
            v = xx

            zInstance.yy()
            v = zInstance.xx

            Z.yy()
            v = Z.xx

        End Sub
    End Class
End Class
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            zInstance.yy()
            ~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            v = zInstance.xx
                ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(531587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531587")>
        Public Sub CircularSharedMemberAccessThroughInstance()
            Dim source =
<compilation name="FieldsConst">
    <file name="a.vb">
Option Strict On
Option Infer On

Class C1
    Const i As Integer = j.MaxValue
    Const j As Integer = i.MaxValue

    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CreateCompilationWithMscorlib40AndVBRuntime(source)
            c1.AssertTheseDiagnostics(
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    Const i As Integer = j.MaxValue
                         ~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    Const j As Integer = i.MaxValue
                         ~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub ConstantFields1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="VBConstantFields1">
    <file name="a.vb">
Module Module1

    Sub Main()
        System.Console.WriteLine("Int64Field: {0}", ConstFields.Int64Field)
        System.Console.WriteLine("DateTimeField: {0}", ConstFields.DateTimeField.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
        System.Console.WriteLine("DoubleField: {0}", ConstFields.DoubleField)
        System.Console.WriteLine("SingleField: {0}", ConstFields.SingleField)
        System.Console.WriteLine("StringField: {0}", ConstFields.StringField)
        System.Console.WriteLine("StringNullField: [{0}]", ConstFields.StringNullField)
        System.Console.WriteLine("ObjectNullField: [{0}]", ConstFields.ObjectNullField)

        System.Console.WriteLine("ByteValue: {0}", ByteEnum.ByteValue)
        System.Console.WriteLine("SByteValue: {0}", SByteEnum.SByteValue)
        System.Console.WriteLine("UInt16Value: {0}", UInt16Enum.UInt16Value)
        System.Console.WriteLine("Int16Value: {0}", Int16Enum.Int16Value)
        System.Console.WriteLine("UInt32Value: {0}", UInt32Enum.UInt32Value)
        System.Console.WriteLine("Int32Value: {0}", Int32Enum.Int32Value)
        System.Console.WriteLine("UInt64Value: {0}", UInt64Enum.UInt64Value)
        System.Console.WriteLine("Int64Value: {0}", Int64Enum.Int64Value)
    End Sub

End Module
    </file>
</compilation>, TestOptions.ReleaseExe)

            compilation = compilation.AddReferences(TestReferences.SymbolsTests.Fields.ConstantFields)

            CompileAndVerify(compilation, <![CDATA[
Int64Field: 634315546432909307
DateTimeField: 1/25/2011 12:17:23 PM
DoubleField: -10
SingleField: 9
StringField: 11
StringNullField: []
ObjectNullField: []
ByteValue: ByteValue
SByteValue: SByteValue
UInt16Value: UInt16Value
Int16Value: Int16Value
UInt32Value: UInt32Value
Int32Value: Int32Value
UInt64Value: UInt64Value
Int64Value: Int64Value
]]>)
        End Sub

        ' Test member of built in type.
        <Fact>
        Public Sub MemberOfBuiltInType()
            CompileAndVerify(
<compilation name="MeReference">
    <file name="a.vb">
Imports System   

Module M1
    Sub Main()
        Dim x as Integer
        x = Integer.Parse("143")
        Console.WriteLine(x)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="143")
        End Sub

        ' Test member of nullable type.
        <Fact>
        Public Sub MemberOfNullableType()
            CompileAndVerify(
<compilation name="MeReference">
    <file name="a.vb">
Imports System   

Module M1
    Sub Main()
        Dim x as boolean
        x = Integer?.Equals("goo", "g" + "oo")
        Console.WriteLine(x)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="True")
        End Sub

        <Fact>
        Public Sub Bug4272()

            Dim compilationDef =
<compilation name="Bug4272">
    <file name="a.vb">
Option Strict On

Module M
  Function Goo(x As Integer) As Integer()
    Goo(1) = Nothing
  End Function
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
<expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
    Goo(1) = Nothing
    ~~~~~~
BC42105: Function 'Goo' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
  End Function
  ~~~~~~~~~~~~
</expected>)

            compilationDef =
<compilation name="Bug4272">
    <file name="a.vb">
Module Module1

    Sub Main()
        Goo()
    End Sub

    Private val As TestClass

    Function Goo() As TestClass
        If val Is Nothing Then
            System.Console.WriteLine("Nothing")
            val = New TestClass()
            val.Field = 2
        Else
            System.Console.WriteLine("val")
            Return val
        End If

        Dim x As TestClass = New TestClass()
        x.Field = 1

        Goo = x
        System.Console.WriteLine(Goo.Field)
        System.Console.WriteLine(Goo.GetField())
        System.Console.WriteLine(Goo().Field)
        System.Console.WriteLine(Goo(3))
    End Function

    Function Goo(x As Integer) As Integer
        Return x
    End Function
End Module


Class TestClass
    Public Field As Integer

    Public Function GetField() As Integer
        Return Field
    End Function

End Class
    </file>
</compilation>

            compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
Nothing
1
1
val
2
3
]]>)

            compilationDef =
<compilation name="Bug4272">
    <file name="a.vb">
Option Strict On
Module M
  Function Goo(x As Integer) As Integer()
    Dim y As Integer() = Goo(1)
  End Function
End Module

Module M1
    Function Goo(x As Object) As Integer
        Return Goo(1)
    End Function
    Function Goo(x As Integer) As Integer
        Return 1
    End Function
End Module
    </file>
</compilation>

            compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
<expected>
BC42105: Function 'Goo' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
  End Function
  ~~~~~~~~~~~~    
</expected>)

        End Sub

        <WorkItem(538802, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538802")>
        <Fact>
        Public Sub MethodAccessibilityChecking()
            CompileAndVerify(
<compilation name="MeReference">
    <file name="a.vb">
Imports System

Public Class C1
    Private Shared Sub goo(x as String)
        Console.Writeline("Private")
    End Sub
    Public Shared Sub goo(x as Object)
        Console.Writeline("Public")
    End Sub
End class

Module Program
    Sub Main()
        'Below call should bind to public overload that takes object
        c1.goo("")
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="Public")
        End Sub

        <Fact>
        Public Sub Bug4249()

            Dim compilationDef =
<compilation name="Bug4249">
    <file name="a.vb">
Module Program
  Sub Main()
    Main().ToString
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
    Main().ToString
    ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Bug4250()

            Dim compilationDef =
<compilation name="Bug4250">
    <file name="a.vb">
Module Program
  Sub Main()
    System.Console.WriteLine(Goo.ToString)
    System.Console.WriteLine(Bar(Of Integer).ToString)
  End Sub
 
  Function Goo() as Integer
    return 123
  End Function

  Function Bar(Of T)() as Integer
    return 231
  End Function
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
123
231
]]>)

            compilationDef =
<compilation name="Bug4250">
    <file name="a.vb">
Module Program
  Sub Main()
    System.Console.WriteLine(Goo.ToString)
  End Sub
 
  Function Goo(x as Integer) as Integer
    return 321
  End Function
End Module
    </file>
</compilation>

            compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30455: Argument not specified for parameter 'x' of 'Public Function Goo(x As Integer) As Integer'.
    System.Console.WriteLine(Goo.ToString)
                             ~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Bug4277()

            Dim compilationDef =
<compilation name="Bug4277">
    <file name="a.vb">
Option Strict Off

Module M
  Sub Main()
  End Sub
 
End Module

Class B
  Sub T()
  End Sub
End Class
 
Class A(Of T)
  Inherits B
 Sub Goo()
   T()
 End Sub
End Class

Class C

    Sub S()
    End Sub

    Class A(Of S)
        Sub Goo()
            S()
        End Sub
    End Class

End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
<expected>
BC30108: 'S' is a type and cannot be used as an expression.
            S()
            ~
</expected>)
        End Sub

        <WorkItem(538438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538438")>
        <Fact>
        Public Sub TestRangeExpressionAllowableLowerBounds()

            Dim compilationDef =
<compilation name="TestRangeExpressionAllowableLowerBounds">
    <file name="a.vb">
    Friend Module ExpArrBounds0LowerBound
        Sub ExpArrBounds0LowerBound()
            Dim x0(0 To 2&amp;)
            Dim x1(0&amp; To 2&amp;)
            Dim x2(0ul To 2&amp;)
            Dim x3(0l To 2&amp;)
            Dim x4(0us To 2&amp;)
            Dim x5(0s To 2&amp;)
        End Sub
    End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoErrors(compilation)
        End Sub

        <WorkItem(537219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537219")>
        <Fact>
        Public Sub BC32059ERR_OnlyNullLowerBound()
            Dim compilationDef =
<compilation name="BC32059ERR_OnlyNullLowerBound">
    <file name="a.b">
    Friend Module ExpArrBounds003Errmod
        Sub ExpArrBounds003Err()
            ' COMPILEERROR: BC32059, "0!"
    	    Dim x1(0! To 5) as Single
            Dim x2(0.0 to 5) as Single
            Dim x3(0d to 5) as Single
        End Sub
    End Module  
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
                                               <errors>
 BC32059: Array lower bounds can be only '0'.
    	    Dim x1(0! To 5) as Single
                ~~
BC32059: Array lower bounds can be only '0'.
            Dim x2(0.0 to 5) as Single
                   ~~~
BC32059: Array lower bounds can be only '0'.
            Dim x3(0d to 5) as Single
                   ~~
                                               </errors>)

        End Sub

        <Fact>
        Public Sub LocalShadowsGenericMethod()
            Dim compilationDef =
<compilation name="LocalShadowsGenericMethod">
    <file name="a.vb">
Class Y
    Sub f()
        Dim goo As Integer

        goo(Of Integer)()
    End Sub

    Public Sub goo(Of T)()

    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
                                               <errors>
BC42024: Unused local variable: 'goo'.
        Dim goo As Integer
            ~~~
BC32045: 'goo' has no type parameters and so cannot have type arguments.
        goo(Of Integer)()
           ~~~~~~~~~~~~
                                               </errors>)

        End Sub

        <Fact>
        Public Sub AccessingMemberOffOfNothing()
            Dim compilationDef =
<compilation name="AccessingMemberOffOfNothing">
    <file name="a.vb">
Class Y
    Sub f()
        Dim x As System.Type = Nothing.GetType()
    End Sub
End Class
    </file>
</compilation>
            CompileAndVerify(compilationDef).
                VerifyIL("Y.f",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldnull    
  IL_0001:  callvirt   "Function Object.GetType() As System.Type"
  IL_0006:  pop
  IL_0007:  ret       
}
]]>)
        End Sub

        <Fact>
        Public Sub ColorColor()
            Dim compilationDef =
<compilation name="AccessingMemberOffOfNothing">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1
    Sub main()
        Dim o As New cls1
    End Sub
End Module


Class cls1
    Public Const s As Integer = 123

    Shared o As Integer = cls1.s
    Public ReadOnly Property cls1 As cls1
        Get
            Console.WriteLine("hi")
            Return Me
        End Get
    End Property

    Sub New()
        ' cls1 is a type here, no warnings needed, but colorizer should show it as a type
        cls1.s.ToString()
    End Sub

    Shared Sub moo()
        Console.WriteLine(cls1.s)
    End Sub
End Class

Class Color
    Public Shared Red As Color = Nothing
    Public Shared Property Green As Color

    Public Class TypeInColor
        Public shared c as Color
    End Class

    Public Class GenericTypeInColor
        Public shared c as Color
    End Class

    Public Class GenericTypeInColor(of T)
        Public shared c as Color
    End Class
End Class

Class Test
    ReadOnly Property Color() As Color
        Get
            Return Color.Red
        End Get
    End Property

    Shared Function DefaultColor() As Color
        Dim c as Color = Color.Green ' Binds to the instance property!
        c= Color.TypeInColor.c
        c= Color.GenericTypeInColor.c
        c= Color.GenericTypeInColor(of UShort).c

        return c
    End Function
End Class

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoErrors(compilation)

        End Sub

        <Fact>
        Public Sub FalseColorColor()
            Dim compilationDef =
<compilation name="AccessingMemberOffOfNothing">
    <file name="a.vb">
Imports Q = B

Class B
    Public Shared Zip As Integer

    Public ReadOnly Property Q As B
        Get
            Return Nothing
        End Get
    End Property

    Shared Sub f()
        Dim x As Integer = Q.Zip 'this should be an error
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
                                               <errors>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        Dim x As Integer = Q.Zip 'this should be an error
                           ~
                                               </errors>)

        End Sub

        <Fact>
        Public Sub ColorColor1()
            Dim compilationDef =
<compilation name="AccessingMemberOffOfNothing">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1
    Sub main()
        Test.DefaultColor()
    End Sub
End Module


Class Color
    Public Shared Red As Color = Nothing

    Public Shared Function G(Of T)(x As T) As Color
        Return Red
    End Function
End Class

Class Test
    ReadOnly Property color(x as integer) As Color
        Get
            Console.WriteLine("evaluated")
            Return Color.Red
        End Get
    End Property

    Shared Function DefaultColor() As Color
        dim a = color.G(1)          ' error, missing parameter to color property
        Return color.G(of Long)(1)          ' error, missing parameter to color property
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation, <expected>
BC30455: Argument not specified for parameter 'x' of 'Public ReadOnly Property color(x As Integer) As Color'.
        dim a = color.G(1)          ' error, missing parameter to color property
                ~~~~~
BC30455: Argument not specified for parameter 'x' of 'Public ReadOnly Property color(x As Integer) As Color'.
        Return color.G(of Long)(1)          ' error, missing parameter to color property
               ~~~~~
                                                            </expected>)

        End Sub

        <Fact>
        Public Sub ColorColor2()
            Dim compilationDef =
<compilation name="AccessingMemberOffOfNothing">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1
    Sub main()
        Test.DefaultColor()
    End Sub
End Module


Class Color
    Public Shared Red As Color = Nothing

    Public Shared Function G(Of T)(x As T) As Color
        Return Red
    End Function
End Class

Class Test
    ReadOnly Property color() As Color
        Get
            Console.WriteLine("evaluated")
            Return Color.Red
        End Get
    End Property

    Shared Function DefaultColor() As Color
        Return color.G(1)          ' Binds to the type
        Return color.G(of Long)(1)          ' Binds to the type
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoErrors(compilation)

        End Sub

        <Fact>
        Public Sub ColorColorOverloaded()
            Dim compilationDef =
<compilation name="ColorColorOverloaded">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1
    Public Sub Main()
        Dim o as Test = New Test
        o.DefaultColor()
    End Sub


    Class Color
        Public Shared Red As Color = New Color

        Public Shared Function G(x As Integer) As Color
            Return Red
        End Function

        Public Function G() As Color
            Return Red
        End Function
    End Class

    Class Test
        ReadOnly Property Color() As Color
            Get
                Console.Write("evaluated")
                Return Color.Red
            End Get
        End Property

        Function DefaultColor() As Color
            Dim c1 As Color
            c1 = Color.G(1)          ' Binds to the type
            c1 = Color.G()          ' Binds to the member

            Return c1
        End Function
    End Class
End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoErrors(compilation)

            CompileAndVerify(compilationDef, expectedOutput:="evaluated").
                VerifyIL("Module1.Test.DefaultColor",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       "Function Module1.Color.G(Integer) As Module1.Color"
  IL_0006:  pop
  IL_0007:  ldarg.0
  IL_0008:  call       "Function Module1.Test.get_Color() As Module1.Color"
  IL_000d:  callvirt   "Function Module1.Color.G() As Module1.Color"
  IL_0012:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ColorColorOverloadedOptional()
            Dim compilationDef =
<compilation name="ColorColorOverloaded">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1
    Public Sub Main()
        Dim o as Test = New Test
        o.DefaultColor()
    End Sub


    Class Color
        Public Shared Red As Color = New Color

        Public Shared Function G(x As Integer) As Color
            Return Red
        End Function

        Public Function G() As Color
            Return Red
        End Function
    End Class

    Class Test
        ReadOnly Property Color(optional x as integer = 1) As Color
            Get
                Console.Write("evaluated")
                Return Color.Red
            End Get
        End Property

        Function DefaultColor() As Color
            Dim c1 As Color
            c1 = Color.G(1)          ' Binds to the type
            c1 = Color.G()          ' Binds to the member

            Return c1
        End Function
    End Class
End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoErrors(compilation)

            CompileAndVerify(compilationDef, expectedOutput:="evaluated").
                VerifyIL("Module1.Test.DefaultColor",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  call       "Function Module1.Color.G(Integer) As Module1.Color"
  IL_0006:  pop
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  call       "Function Module1.Test.get_Color(Integer) As Module1.Color"
  IL_000e:  callvirt   "Function Module1.Color.G() As Module1.Color"
  IL_0013:  ret
}

]]>)
        End Sub

        <Fact>
        Public Sub ColorColorOverloadedErr()
            Dim compilationDef =
<compilation name="ColorColorOverloaded">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1
    Public Sub Main()
        Dim o as Test = New Test
        o.DefaultColor()
    End Sub


    Class Color
        Public Shared Red As Color = New Color

        Public Shared Function G(x As Integer) As Color
            Return Red
        End Function

        Public Function G() As Color
            Return Red
        End Function
    End Class

    Class Test
        ReadOnly Property Color(x as integer) As Color
            Get
                Console.Write("evaluated")
                Return Color.Red
            End Get
        End Property

        Function DefaultColor() As Color
            Dim c1 As Color
            c1 = Color.G(1)          ' missing parameter x
            c1 = Color.G()          ' missing parameter x

            Return c1
        End Function
    End Class
End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation,
                                               <errors>
BC30455: Argument not specified for parameter 'x' of 'Public ReadOnly Property Color(x As Integer) As Module1.Color'.
            c1 = Color.G(1)          ' missing parameter x
                 ~~~~~
BC30455: Argument not specified for parameter 'x' of 'Public ReadOnly Property Color(x As Integer) As Module1.Color'.
            c1 = Color.G()          ' missing parameter x
                 ~~~~~
                                               </errors>)
        End Sub

        <Fact>
        Public Sub ColorColorOverloadedErr2()
            Dim compilationDef =
<compilation name="ColorColorOverloaded">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1
    Public Sub Main()
        Dim o as Test = New Test
        o.DefaultColor()
    End Sub


    Class Color
        Public Shared Red As Color = New Color

        Public Shared Function G(x As Integer) As Color
            Return Red
        End Function

        Public Function G() As Color
            Return Red
        End Function
    End Class

    Class Test
        ReadOnly Property Color As Color
            Get
                Console.Write("evaluated")
                Return Color.Red
            End Get
        End Property

        Function DefaultColor() As Color
            Dim c1 As Color
            c1 = Color.G(1)          ' Binds to the type
            c1 = Color.G()          ' Binds to the member

            Return c1
        End Function
    End Class
End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub ColorColorOverloadedAddressOf()
            Dim compilationDef =
<compilation name="ColorColorOverloadedAddressOf">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1
    Public Sub Main()
        Dim o as Test = New Test
        o.DefaultColor()
    End Sub


    Class Color
        Public Shared Red As Color = New Color

        Public Shared Function G(x As Integer) As Color
            Return Red
        End Function

        Public Function G() As Color
            Return Red
        End Function
    End Class

    Class Test
        ReadOnly Property Color() As Color
            Get
                Console.Write("evaluated")
                Return Color.Red
            End Get
        End Property

        Function DefaultColor() As Color
            Dim c1 As Color

            Dim d1 As Func(Of Integer, Color) = AddressOf Color.G  ' Binds to the type
            c1 = d1(1)

            Dim d2 As Func(Of Color) = AddressOf Color.G   ' Binds to the member
            c1 = d2()

            Return c1
        End Function
    End Class

End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoErrors(compilation)

            CompileAndVerify(compilationDef, expectedOutput:="evaluated").
                VerifyIL("Module1.Test.DefaultColor",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      "Function Module1.Color.G(Integer) As Module1.Color"
  IL_0007:  newobj     "Sub System.Func(Of Integer, Module1.Color)..ctor(Object, System.IntPtr)"
  IL_000c:  ldc.i4.1
  IL_000d:  callvirt   "Function System.Func(Of Integer, Module1.Color).Invoke(Integer) As Module1.Color"
  IL_0012:  pop
  IL_0013:  ldarg.0
  IL_0014:  call       "Function Module1.Test.get_Color() As Module1.Color"
  IL_0019:  ldftn      "Function Module1.Color.G() As Module1.Color"
  IL_001f:  newobj     "Sub System.Func(Of Module1.Color)..ctor(Object, System.IntPtr)"
  IL_0024:  callvirt   "Function System.Func(Of Module1.Color).Invoke() As Module1.Color"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ColorColorOverloadedAddressOfRelaxed()
            Dim compilationDef =
<compilation name="ColorColorOverloadedAddressOfRelaxed">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1
    Public Sub Main()
        Dim o As Test = New Test
        o.DefaultColor()
    End Sub


    Class Color
        Public Shared Red As Color = New Color

        Public Shared Function G(x As Integer) As Color
            Return Red
        End Function

        Public Function G() As Color
            Return Red
        End Function
    End Class

    Class Test
        ReadOnly Property Color() As Color
            Get
                Console.Write("evaluated")
                Return Color.Red
            End Get
        End Property

        Sub DefaultColor()
            Dim d1 As Action(Of Integer) = AddressOf Color.G  ' Binds to the type
            d1(1)

            Dim d2 As Action = AddressOf Color.G   ' Binds to the member
            d2()
        End Sub
    End Class

End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoErrors(compilation)

            CompileAndVerify(compilationDef, expectedOutput:="evaluated").
                VerifyIL("Module1.Test.DefaultColor",
            <![CDATA[
{
  // Code size       76 (0x4c)
  .maxstack  3
  IL_0000:  ldsfld     "Module1.Test._Closure$__.$IR3-2 As System.Action(Of Integer)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Module1.Test._Closure$__.$IR3-2 As System.Action(Of Integer)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Module1.Test._Closure$__.$I As Module1.Test._Closure$__"
  IL_0013:  ldftn      "Sub Module1.Test._Closure$__._Lambda$__R3-2(Integer)"
  IL_0019:  newobj     "Sub System.Action(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Module1.Test._Closure$__.$IR3-2 As System.Action(Of Integer)"
  IL_0024:  ldc.i4.1
  IL_0025:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_002a:  newobj     "Sub Module1.Test._Closure$__R3-0..ctor()"
  IL_002f:  dup
  IL_0030:  ldarg.0
  IL_0031:  call       "Function Module1.Test.get_Color() As Module1.Color"
  IL_0036:  stfld      "Module1.Test._Closure$__R3-0.$VB$NonLocal_2 As Module1.Color"
  IL_003b:  ldftn      "Sub Module1.Test._Closure$__R3-0._Lambda$__R3()"
  IL_0041:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0046:  callvirt   "Sub System.Action.Invoke()"
  IL_004b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ColorColorExtension()
            Dim compilationDef =
<compilation name="ColorColorOverloaded">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Runtime.CompilerServices

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

Module Module1
    Public Sub Main()
        Dim o As Test = New Test
        o.DefaultColor()
    End Sub

    Structure S1

    End Structure

    &lt;Extension()&gt;
    Public Function G(this As Color) As Color
        Return Color.Red
    End Function

    Class Color
        Public Shared Red As Color = New Color

        Public Shared Function G(x As Integer) As Color
            Return Red
        End Function
    End Class

    Class Test
        Shared ReadOnly Property Color() As Color
            Get
                Console.Write("evaluated")
                Return Color.Red
            End Get
        End Property

        Function DefaultColor() As Color
            Dim c1 As Color
            c1 = Color.G(1)          ' Binds to the type
            c1 = Color.G()          ' Binds to the member + extension

            Return c1
        End Function
    End Class
End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoErrors(compilation)

            CompileAndVerify(compilationDef, expectedOutput:="evaluated").
                VerifyIL("Module1.Test.DefaultColor",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       "Function Module1.Color.G(Integer) As Module1.Color"
  IL_0006:  pop
  IL_0007:  call       "Function Module1.Test.get_Color() As Module1.Color"
  IL_000c:  call       "Function Module1.G(Module1.Color) As Module1.Color"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ColorColorAlias()
            Dim compilationDef =
<compilation name="ColorColorAlias">
    <file name="a.vb">
Option Strict On

Imports System
Imports Bar = NS1.Bar

Module Module1
    Public Sub Main()
        Dim o as Goo = New Goo()
        Console.WriteLine(o.M())
    End Sub
End Module

Namespace NS1
    Public Class Bar
        Public Shared c as Integer = 48
    End Class
End Namespace

Class Goo
  ReadOnly Property Bar As Bar
    Get 
      Console.WriteLine("property called")
      Return Nothing
    End Get
  End Property
   
  Function M() As Integer
    return Bar.c
  End Function
End Class


    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertNoDiagnostics(compilation)

            CompileAndVerify(compilationDef, expectedOutput:="48").
                VerifyIL("Goo.M",
            <![CDATA[
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldsfld     "NS1.Bar.c As Integer"
  IL_0005:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub ColorColorWrongAlias()
            Dim compilationDef =
<compilation name="ColorColorWrongAlias">
    <file name="a.vb">
Option Strict On
Imports System
Imports Bar2 = NS1.Bar

Module Module1
    Public Sub Main()
        Dim o As Goo = New Goo()
        Console.WriteLine(Goo.M())
    End Sub
End Module

Namespace NS1

    Public Class Bar

        Public Shared c As Integer = 48
    End Class
End Namespace

Class Goo

    ReadOnly Property Bar2 As Bar2
        Get
            Console.WriteLine("property called")
            Return Nothing
        End Get
    End Property

    Public Shared Function M() As Integer
        Return Bar2.c
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            AssertTheseDiagnostics(compilation, <expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        Return Bar2.c
               ~~~~
                                                            </expected>)
        End Sub

        <Fact>
        Public Sub ModulesWhereTypesShouldBe()
            Dim text =
<compilation name="ModulesWhereTypesShouldBe">
    <file name="a.vb">

Module M

    Sub GG()
        Dim y As System.Collections.Generic.List(Of M)
        Dim z As Object = Nothing
        Dim q = TryCast(z, M)
        Dim p = CType(z, M)
        Dim r As M()
    End Sub

End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(text)
            AssertTheseDiagnostics(compilation,
                                               <errors>
BC42024: Unused local variable: 'y'.
        Dim y As System.Collections.Generic.List(Of M)
            ~
BC30371: Module 'M' cannot be used as a type.
        Dim y As System.Collections.Generic.List(Of M)
                                                    ~
BC30371: Module 'M' cannot be used as a type.
        Dim q = TryCast(z, M)
                           ~
BC30371: Module 'M' cannot be used as a type.
        Dim p = CType(z, M)
                         ~
BC42024: Unused local variable: 'r'.
        Dim r As M()
            ~
BC30371: Module 'M' cannot be used as a type.
        Dim r As M()
                 ~
</errors>)

        End Sub

        <Fact>
        Public Sub GetTypeOnNSAlias()
            Dim text =
<compilation name="GetTypeOnNSAlias">
    <file name="a.vb">
Imports NS=System.Collections
Module M
  Sub S()
    Dim x = GetType(NS)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(text)
            AssertTheseDiagnostics(compilation,
<errors>
BC30182: Type expected.
    Dim x = GetType(NS)
                    ~~
</errors>)
        End Sub

        <WorkItem(542383, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542383")>
        <Fact>
        Public Sub GetTypeOnModuleName()
            Dim text =
<compilation name="GetTypeForModuleName">
    <file name="a.vb">
Imports System

Namespace AttrUseCust011
    Friend Module AttrUseCust011mod

        Sub GG()
            Dim x = GetType(AttrUseCust011mod)
        End Sub

    End Module
End Namespace

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(text)
            AssertNoErrors(compilation)

        End Sub

        <Fact>
        Public Sub GetTypeOnAlias()
            Dim text =
<compilation name="GetTypeOnAlias">
    <file name="a.vb">
Imports System
Imports Con = System.Console

    Module M

        Sub GG()
            Dim x = GetType(Con)
        End Sub

    End Module

    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(text)
            AssertNoErrors(compilation)

        End Sub

        <Fact>
        Public Sub Bug9300_1()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NotYetImplementedInRoslyn">
        <file name="a.vb">
Imports System.Runtime.CompilerServices
Imports System.Collections
Imports System

Module M
    Sub Main()
        Goo(Sub(x) x.New())
    End Sub

    Sub Goo(x As Action(Of Object()))
        System.Console.WriteLine("Action(Of Object())")
        x(New Object() {})
    End Sub

    &lt;Extension()&gt;
    Sub [New](Of T)(ByVal x As T)
        System.Console.WriteLine("[New]")
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
BC30251: Type 'Object()' has no constructors.
        Goo(Sub(x) x.New())
                   ~~~~~
                 </errors>
            AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub Bug9300_2()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NotYetImplementedInRoslyn">
        <file name="a.vb">
Imports System.Runtime.CompilerServices
Imports System.Collections
Imports System

Module M
    Sub Main()
        Goo(Sub(x) x.New())
    End Sub

    Class TC1
    End Class

    Sub Goo(x As Action(Of TC1))
    End Sub

    &lt;Extension()&gt;
    Sub [New](Of T)(ByVal x As T)
        System.Console.WriteLine("[New]")
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Goo(Sub(x) x.New())
                   ~~~~~
                 </errors>
            AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub Bug9300_3()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NotYetImplementedInRoslyn">
        <file name="a.vb">
Imports System.Runtime.CompilerServices
Imports System.Collections
Imports System

Module M
    Sub Main()
        Goo(Sub(x) x.New())
    End Sub

    Sub Goo(x As Action(Of IEnumerable))
        System.Console.WriteLine("Action(Of IEnumerable)")
        x(New Object() {})
    End Sub

    Sub Goo(x As Action(Of Object()))
        System.Console.WriteLine("Action(Of Object())")
        x(New Object() {})
    End Sub

    &lt;Extension()&gt;
    Sub [New](Of T)(ByVal x As T)
        System.Console.WriteLine("[New]")
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
        </file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, <![CDATA[
Action(Of IEnumerable)
[New]
]]>)
        End Sub

        <Fact>
        Public Sub IllegalTypeExpressionsFromParserShouldNotBlowUpBinding()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="IllegalTypeExpressionsFromParserShouldNotBlowUpBinding">
        <file name="a.vb">
            Class Outer(Of T)
                Public Shared Sub Print()
                    System.Console.WriteLine(GetType(Outer(Of ).Inner(Of T))) ' BC32099: Comma or ')' expected.
                    System.Console.WriteLine(GetType(Outer(Of ).Inner(Of Integer))) ' BC32099: Comma or ')' expected.
                    System.Console.WriteLine(GetType(Outer(Of T).Inner(Of ))) ' BC30182: Type expected.
                    System.Console.WriteLine(GetType(Outer(Of Integer).Inner(Of ))) ' BC30182: Type expected.
                End Sub

                Class Inner(Of U)
                End Class
            End Class
        </file>
    </compilation>)

            AssertTheseDiagnostics(compilation1, <expected>
BC32099: Comma or ')' expected.
                    System.Console.WriteLine(GetType(Outer(Of ).Inner(Of T))) ' BC32099: Comma or ')' expected.
                                                                         ~
BC32099: Comma or ')' expected.
                    System.Console.WriteLine(GetType(Outer(Of ).Inner(Of Integer))) ' BC32099: Comma or ')' expected.
                                                                         ~~~~~~~
BC30182: Type expected.
                    System.Console.WriteLine(GetType(Outer(Of T).Inner(Of ))) ' BC30182: Type expected.
                                                                          ~
BC30182: Type expected.
                    System.Console.WriteLine(GetType(Outer(Of Integer).Inner(Of ))) ' BC30182: Type expected.
                                                                                ~                                                                 
                                                             </expected>)
        End Sub

        <Fact()>
        Public Sub Bug10335_1()
            Dim source1_with =
    <compilation name="Unavailable">
        <file name="a.vb">
            Public Interface IUnavailable
                ReadOnly Default Property Item(p as Integer) as Integer
            End Interface
        </file>
    </compilation>

            Dim c1 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source1_with)

            Dim baseBuffer = CompileAndVerify(c1).EmittedAssemblyData

            Dim source2 =
    <compilation>
        <file name="a.vb">

            Public Class Class1
                Implements IUnavailable
                Public Default ReadOnly Property Item(p as Integer) as Integer implements IUnavailable.Item
                    Get
                        Return p
                    End Get
                End Property
            End Class

            Public Class Class2

                Public ReadOnly Property AProperty() as Class1
                    Get
                        Return new Class1()
                    End Get
                End Property
            End Class
        </file>
    </compilation>

            Dim c2 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source2, {MetadataReference.CreateFromImage(baseBuffer)})

            Dim derivedBuffer = CompileAndVerify(c2).EmittedAssemblyData

            Dim source3 =
    <compilation>
        <file name="a.vb">
            Module M1
                Sub Main()
                    Dim x as new Class2()
                    dim y = x.AProperty(23)
                    Dim z as Object = x.AProperty()
                    x.AProperty()
                End Sub
            End Module
        </file>
    </compilation>

            Dim source1_without =
<compilation name="Unavailable">
    <file name="a.vb">
        Class Unused
        End Class
    </file>
</compilation>

            Dim c1_without = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source1_without)

            Dim image = CompileAndVerify(c1_without).EmittedAssemblyData

            Dim c3 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source3, {MetadataReference.CreateFromImage(derivedBuffer), MetadataReference.CreateFromImage(image)})

            AssertTheseDiagnostics(c3, <expected>
BC30545: Property access must assign to the property or use its value.
                    x.AProperty()
                    ~~~~~~~~~~~~~
                                  </expected>)
        End Sub

        <Fact>
        Public Sub ColorColorOverriddenProperty()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Bug12687">
    <file name="a.vb">
Class TypeSubstitution
    Shared Function Create() As Integer
      Return 1
    End Function
End Class
Class InstanceTypeSymbol
        ReadOnly Property TypeSubstitution(a as Integer) As TypeSubstitution
            Get
                Return Nothing
            End Get
        End Property
End Class
Class Frame
      Inherits InstanceTypeSymbol
        Overloads ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return Nothing
            End Get
        End Property
  Function Goo() As Integer
    Return TypeSubstitution.Create()
  End Function
End Class


    </file>
</compilation>)

            AssertNoDiagnostics(compilation)
        End Sub

        <Fact>
        Public Sub ColorColorPropertyWithParam()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Bug12687">
    <file name="a.vb">
Class TypeSubstitution
    Shared Function Create() As Integer
      Return 1
    End Function
End Class
Class InstanceTypeSymbol
      Overridable ReadOnly Property TypeSubstitution(a as Integer) As TypeSubstitution
          Get
              Return Nothing
          End Get
      End Property
End Class
Class Frame
      Inherits InstanceTypeSymbol
      Function Goo() As Integer
         Return TypeSubstitution.Create()
      End Function
End Class


    </file>
</compilation>)

            AssertTheseDiagnostics(compilation, <expected>
BC30455: Argument not specified for parameter 'a' of 'Public Overridable ReadOnly Property TypeSubstitution(a As Integer) As TypeSubstitution'.
         Return TypeSubstitution.Create()
                ~~~~~~~~~~~~~~~~
                                                            </expected>)
        End Sub

        <Fact>
        Public Sub ColorColorPropertyWithOverloading()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Bug12687">
    <file name="a.vb">
Class TypeSubstitution
    Shared Function Create() As Integer
      Return 1
    End Function
End Class
Class InstanceTypeSymbol
      ReadOnly Property TypeSubstitution(a as Integer) As String
          Get
              Return Nothing
          End Get
      End Property
      ReadOnly Property TypeSubstitution As TypeSubstitution
          Get
              Return Nothing
          End Get
      End Property
End Class
Class Frame
      Inherits InstanceTypeSymbol
      Function Goo() As Integer
         Return TypeSubstitution.Create()
      End Function
End Class
    </file>
</compilation>)

            AssertNoDiagnostics(compilation)

        End Sub

        ' Tests IsValidAssignmentTarget for PropertyAccess
        ' and IsLValueFieldAccess for FieldAccess.
        <Fact>
        Public Sub IsValidAssignmentTarget()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Structure S
    Public F As Object
    Public Property P As Object
    Sub M()
        Me.F = Nothing ' IsLValue = False, IsMeReference = True
        MyClass.F = Nothing ' IsLValue = False, IsMyClassReference = True
        Me.P = Nothing ' IsLValue = False, IsMeReference = True
        MyClass.P = Nothing ' IsLValue = False, IsMyClassReference = True
    End Sub
End Structure
Class C
    Private F1 As S
    Private ReadOnly F2 As S
    Sub M()
        F1.F = Nothing ' IsLValue = True
        F2.F = Nothing ' IsLValue = False
        F1.P = Nothing ' IsLValue = True
        F2.P = Nothing ' IsLValue = False
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        F2.F = Nothing ' IsLValue = False
        ~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        F2.P = Nothing ' IsLValue = False
        ~~~~
                                          </expected>)
        End Sub

        <Fact>
        Public Sub Bug12900()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MemberAccessNoContainingWith">
    <file name="a.vb">
Imports System        
Module Program
    Sub Main(args As String())
        Const local _? As Integer
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30438: Constants must have a value.
        Const local _? As Integer
              ~~~~~
BC30203: Identifier expected.
        Const local _? As Integer
                    ~
</expected>)
        End Sub

        <Fact>
        Public Sub Bug13080()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System        
Module Program
    Sub Main(args As String())
        Const '
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30203: Identifier expected.
        Const '
              ~
BC30438: Constants must have a value.
        Const '
              ~    
</expected>)
        End Sub

        <WorkItem(546469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546469")>
        <Fact>
        Public Sub GetTypeAllowsArrayOfModules()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">    
Imports System
Imports System.Collections.Generic
Imports VoidAlias = System.Void

Namespace Bar
    Module Test

        Sub Main()
            ' Array of types
            Dim x As Type = GetType(Test()) 'ok
            x = GetType(Void()) ' error

            ' types direct
            x = GetType(Test) ' ok
            x = GetType(Void) ' ok

            ' nullable
            x = GetType(Test?) ' error
            x = GetType(Void?) ' error

            x = GetType(List(Of Test)) ' error
            x = GetType(List(Of Void)) ' error

            x = GetType(Bar.Test) ' ok
            x = GetType(System.Void) ' ok

            x = GetType(VoidAlias) ' ok
        End Sub
    End Module

End Namespace
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation, <expected>
BC31428: Arrays of type 'System.Void' are not allowed in this expression.
            x = GetType(Void()) ' error
                        ~~~~~~
BC30371: Module 'Test' cannot be used as a type.
            x = GetType(Test?) ' error
                        ~~~~
BC33101: Type 'Test' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
            x = GetType(Test?) ' error
                        ~~~~
BC31422: 'System.Void' can only be used in a GetType expression.
            x = GetType(Void?) ' error
                        ~~~~
BC30371: Module 'Test' cannot be used as a type.
            x = GetType(List(Of Test)) ' error
                                ~~~~
BC31422: 'System.Void' can only be used in a GetType expression.
            x = GetType(List(Of Void)) ' error
                                ~~~~
                                           </expected>)
        End Sub

        <WorkItem(530438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530438")>
        <WorkItem(546469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546469")>
        <Fact()>
        Public Sub GetTypeAllowsModuleAlias()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">    
Imports ModuleAlias = Bar.Test
Imports System

Namespace Bar
    Module Test

        Sub Main()
            Dim x As Type = GetType(ModuleAlias) ' ok
        End Sub
    End Module

End Namespace
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub RangeVariableColorColor()
            Dim source = _
<compilation>
    <file name="a.vb">
Imports System.Linq

Class Program
    Shared Sub Main()
        Dim q = From X As X In New X() { New X() }
                Where X.S()
                Where X.I()
                Select 42
        System.Console.Write(q.Single())
    End Sub
End Class

Class X
    Public Shared Function S() As Boolean
        Return True
    End Function

    Public Shared Function I() As Boolean
        Return True
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, options:=TestOptions.ReleaseExe)
            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:="42")
        End Sub

        <WorkItem(1108036, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108036")>
        <Fact()>
        Public Sub Bug1108036()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class Color
    Public Shared Sub Cat()
    End Sub
End Class

Class Program
    Shared Sub Main()
        Color.Cat()
    End Sub
 
    ReadOnly Property Color(Optional x As Integer = 0) As Color
        Get
            Return Nothing
        End Get
    End Property
 
    ReadOnly Property Color(Optional x As String = "") As Integer
        Get
            Return 0
        End Get
    End Property
End Class
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Color' is most specific for these arguments:
    'Public ReadOnly Property Color([x As Integer = 0]) As Color': Not most specific.
    'Public ReadOnly Property Color([x As String = ""]) As Integer': Not most specific.
        Color.Cat()
        ~~~~~
</expected>)
        End Sub

        <WorkItem(1108036, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108036")>
        <Fact()>
        Public Sub Bug1108036_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class Color
    Public Shared Sub Cat()
    End Sub
End Class

Class Program
    Shared Sub Main()
        Color.Cat()
    End Sub
 
    ReadOnly Property Color(Optional x As Integer = 0) As Integer
        Get
            Return 0
        End Get
    End Property
 
    ReadOnly Property Color(Optional x As String = "") As Color
        Get
            Return Nothing
        End Get
    End Property
End Class
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Color' is most specific for these arguments:
    'Public ReadOnly Property Color([x As Integer = 0]) As Integer': Not most specific.
    'Public ReadOnly Property Color([x As String = ""]) As Color': Not most specific.
        Color.Cat()
        ~~~~~
</expected>)
        End Sub

        <WorkItem(969006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969006")>
        <Fact()>
        Public Sub Bug969006_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Enum E
    A
End Enum
Class C
    Sub M()
        Const e As E = E.A
        Dim z = e
    End Sub
End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model1 = compilation.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Single()
            Assert.Equal("E.A", node1.ToString())
            Assert.Equal("E", node1.Expression.ToString())

            Dim symbolInfo = model1.GetSymbolInfo(node1.Expression)

            Assert.Equal("E", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, symbolInfo.Symbol.Kind)

            Dim model2 = compilation.GetSemanticModel(tree)
            Dim node2 = tree.GetRoot().DescendantNodes.OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "e").Single()

            Assert.Equal("= e", node2.Parent.ToString())

            symbolInfo = model2.GetSymbolInfo(node2)

            Assert.Equal("e As E", symbolInfo.Symbol.ToTestDisplayString())

            symbolInfo = model2.GetSymbolInfo(node1.Expression)

            Assert.Equal("E", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, symbolInfo.Symbol.Kind)

            AssertTheseDiagnostics(compilation)
        End Sub

        <WorkItem(969006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969006")>
        <Fact()>
        Public Sub Bug969006_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Enum E
    A
End Enum
Class C
    Sub M()
        Dim e As E = E.A
        Dim z = e
    End Sub
End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model1 = compilation.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Single()
            Assert.Equal("E.A", node1.ToString())
            Assert.Equal("E", node1.Expression.ToString())

            Dim symbolInfo = model1.GetSymbolInfo(node1.Expression)

            Assert.Equal("E", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, symbolInfo.Symbol.Kind)

            Dim model2 = compilation.GetSemanticModel(tree)
            Dim node2 = tree.GetRoot().DescendantNodes.OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "e").Single()

            Assert.Equal("= e", node2.Parent.ToString())

            symbolInfo = model2.GetSymbolInfo(node2)

            Assert.Equal("e As E", symbolInfo.Symbol.ToTestDisplayString())

            symbolInfo = model2.GetSymbolInfo(node1.Expression)

            Assert.Equal("E", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, symbolInfo.Symbol.Kind)

            AssertTheseDiagnostics(compilation)
        End Sub

        <WorkItem(969006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969006")>
        <Fact()>
        Public Sub Bug969006_3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Enum E
    A
End Enum
Class C
    Sub M()
        Const e = E.A
        Dim z = e
    End Sub
End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model1 = compilation.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Single()
            Assert.Equal("E.A", node1.ToString())
            Assert.Equal("E", node1.Expression.ToString())

            Dim symbolInfo = model1.GetSymbolInfo(node1.Expression)

            Assert.Equal("e As System.Object", symbolInfo.Symbol.ToTestDisplayString())

            Dim model2 = compilation.GetSemanticModel(tree)
            Dim node2 = tree.GetRoot().DescendantNodes.OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "e").Single()

            Assert.Equal("= e", node2.Parent.ToString())

            symbolInfo = model2.GetSymbolInfo(node2)

            Assert.Equal("e As System.Object", symbolInfo.Symbol.ToTestDisplayString())

            symbolInfo = model2.GetSymbolInfo(node1.Expression)

            Assert.Equal("e As System.Object", symbolInfo.Symbol.ToTestDisplayString())

            AssertTheseDiagnostics(compilation, <expected>
BC30500: Constant 'e' cannot depend on its own value.
        Const e = E.A
                  ~
BC42104: Variable 'e' is used before it has been assigned a value. A null reference exception could result at runtime.
        Const e = E.A
                  ~
                                                </expected>)
        End Sub

        <WorkItem(969006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969006")>
        <Fact()>
        Public Sub Bug969006_4()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Enum E
    A
End Enum
Class C
    Sub M()
        Dim e = E.A
        Dim z = e
    End Sub
End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model1 = compilation.GetSemanticModel(tree)
            Dim node1 = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Single()
            Assert.Equal("E.A", node1.ToString())
            Assert.Equal("E", node1.Expression.ToString())

            Dim symbolInfo = model1.GetSymbolInfo(node1.Expression)

            Assert.Equal("e As ?", symbolInfo.Symbol.ToTestDisplayString())

            Dim model2 = compilation.GetSemanticModel(tree)
            Dim node2 = tree.GetRoot().DescendantNodes.OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "e").Single()

            Assert.Equal("= e", node2.Parent.ToString())

            symbolInfo = model2.GetSymbolInfo(node2)

            Assert.Equal("e As ?", symbolInfo.Symbol.ToTestDisplayString())

            symbolInfo = model2.GetSymbolInfo(node1.Expression)

            Assert.Equal("e As ?", symbolInfo.Symbol.ToTestDisplayString())

            AssertTheseDiagnostics(compilation, <expected>
BC30980: Type of 'e' cannot be inferred from an expression containing 'e'.
        Dim e = E.A
                ~
BC42104: Variable 'e' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim e = E.A
                ~
                                                </expected>)
        End Sub

        <WorkItem(1108007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108007")>
        <Fact()>
        Public Sub Bug1108007_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class Color
    Public Shared Sub M(x As Integer)
        System.Console.WriteLine(x)
    End Sub

    Public Sub M(x As String)
    End Sub
End Class

Class Program
    Dim Color As Color

    Shared Sub Main()
        Dim x As Object = 42
        Color.M(x)
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:="42")
        End Sub

        <WorkItem(1108007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108007")>
        <Fact>
        Public Sub Bug1108007_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Class Color
    Public Shared Sub M(x As Integer)
        Console.WriteLine(x)
    End Sub

    Public Sub M(x As String)
    End Sub
End Class

Class Program
    Dim Color As Color

    Shared Sub Main()
        Try
            Dim x As Object = ""
            Color.M(x)
        Catch e As Exception
            Console.WriteLine(e.GetType())
        End Try
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:=<![CDATA[System.NullReferenceException]]>)
        End Sub

        <WorkItem(1108007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108007")>
        <Fact()>
        Public Sub Bug1108007_3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Class MyAttribute
    Inherits System.Attribute

    Public ReadOnly I As Integer

    Public Sub New(i As Integer)
        Me.I = i
    End Sub
End Class

Class Color
    Public Const I As Integer = 42
End Class

Class Program
    Dim Color As Color

    <MyAttribute(Color.I)>
    Shared Sub Main()
    End Sub
End Class
        ]]>
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <WorkItem(1108007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108007")>
        <Fact()>
        Public Sub Bug1108007_4()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Class Color
    Public Shared Sub M(x As Integer)
        Console.WriteLine(x)
    End Sub

    Public Sub M(x As String)
    End Sub

    Class Program
        Dim Color As Color

        Sub M()
            Try
                Dim x As Object = ""
                Color.M(x)
            Catch e As Exception
                Console.WriteLine(e.GetType())
            End Try
        End Sub

        Shared Sub Main()
            Dim p = New Program()
            p.M()
        End Sub
    End Class
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.Field, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:=<![CDATA[System.NullReferenceException]]>)
        End Sub

        <WorkItem(1108007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108007")>
        <Fact()>
        Public Sub Bug1108007_5()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Class Color
    Public Shared Function M(x As Integer) As Integer
        Return x
    End Function

    Public Function M(x As String) As Integer
        Return x.Length
    End Function
End Class

Class A
    Public Sub New(x As Integer)
        Console.WriteLine(x)
    End Sub
End Class

Class B
    Inherits A

    Dim Color As Color

    Public Sub New()
        MyBase.New(Color.M(DirectCast(42, Object)))
    End Sub

    Shared Sub Main()
        Dim b = New B()
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:="42")
        End Sub

        <WorkItem(1108007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108007")>
        <Fact()>
        Public Sub Bug1108007_6()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Class Color
    Public Shared Function M(x As Integer) As Integer
        Console.WriteLine(x)
        Return x
    End Function

    Public Function M(x As String) As Integer
        Return x.Length
    End Function
End Class

Class Program
    Dim Color As Color
    Dim I As Integer = Color.M(DirectCast(42, Object))

    Shared Sub Main()
        Try
            Dim p = New Program()
        Catch e As Exception
            Console.WriteLine(e.GetType())
        End Try
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.Field, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:=<![CDATA[System.NullReferenceException]]>)
        End Sub

        <WorkItem(1108007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108007")>
        <Fact()>
        Public Sub Bug1108007_7()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Class Color
    Public Shared Function M(x As Integer) As Integer
        Console.WriteLine(x)
        Return x
    End Function

    Public Function M(x As String) As Integer
        Return x.Length
    End Function
End Class

Class Program
    Dim Color As Color
    Shared Dim I As Integer = Color.M(DirectCast(42, Object))

    Shared Sub Main()
        Dim i = Program.I
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:="42")
        End Sub

        <WorkItem(1108007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108007")>
        <Fact>
        Public Sub Bug1108007_8()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Class Color
    Public Shared Sub M(x As Integer)
        Console.WriteLine(x)
    End Sub

    Public Sub M(x As String)
    End Sub
End Class

Class Outer
    Dim Color As Color

    Class Program
        Sub M()
            Try
                Dim x As Object = ""
                Color.M(x)
            Catch e As Exception
                Console.WriteLine(e.GetType())
            End Try
        End Sub

        Shared Sub Main()
            Dim p = New Program()
            p.M()
        End Sub
    End Class
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:=<![CDATA[System.NullReferenceException]]>)
        End Sub

        <WorkItem(1108007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108007")>
        <Fact()>
        Public Sub Bug1108007_9()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class Color
    Public Shared Sub M(x As Integer)
        System.Console.WriteLine(x)
    End Sub

    Public Sub M(x As String)
    End Sub
End Class

Class Outer
    Shared Dim Color As Color = New Color()

    Class Program
        Sub M()
            Dim x As Object = 42
            Color.M(x)
        End Sub

        Shared Sub Main()
            Dim p = New Program()
            p.M()
        End Sub
    End Class
End Class    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.Field, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:="42")
        End Sub

        <WorkItem(1114969, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114969")>
        <Fact()>
        Public Sub Bug1114969()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class Color
    Public Function M() As Integer
        Return 42
    End Function
End Class

Class Base
    Protected Dim Color As Color = New Color()
End Class
    
Class Derived
    Inherits Base

    Sub M()
        System.Console.WriteLine(Color.M())
    End Sub

    Shared Sub Main()
        Dim d = New Derived()
        d.M()
    End Sub
End Class    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Select(Function(e) e.Expression).Where(Function(e) e.ToString() = "Color").Single()

            Dim symbol = model.GetSymbolInfo(node).Symbol
            Assert.NotNull(symbol)
            Assert.Equal("Color", symbol.Name)
            Assert.Equal(SymbolKind.Field, symbol.Kind)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, expectedOutput:="42")
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/70007")>
        <Fact()>
        Public Sub CycleThroughAttribute_01()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Reflection

<Assembly: AssemblyVersion(MainVersion.CurrentVersion)>

Public Class MainVersion
    Public Const Hauptversion As String = "8"
    Public Const Nebenversion As String = "2"
    Public Const Build As String = "0"
    Public Const Revision As String = "1"

    Public Const CurrentVersion As String = Hauptversion & "." & Nebenversion & "." & Build & "." & Revision
End Class
    ]]></file>
</compilation>)

            CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/70007")>
        <Fact()>
        Public Sub CycleThroughAttribute_02()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
<Module: MyAttribute(MainVersion.CurrentVersion)>

Public Class MainVersion
    Public Const Hauptversion As String = "8"
    Public Const Nebenversion As String = "2"
    Public Const Build As String = "0"
    Public Const Revision As String = "1"

    Public Const CurrentVersion As String = Hauptversion & "." & Nebenversion & "." & Build & "." & Revision
End Class

class MyAttribute
	Inherits System.Attribute

	Sub New(x as String)
	End Sub
End Class
    ]]></file>
</compilation>)

            CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/70007")>
        <Fact()>
        Public Sub CycleThroughAttribute_03()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
<MyAttribute(MainVersion.CurrentVersion)>
Public Class MainVersion
    Public Const Hauptversion As String = "8"
    Public Const Nebenversion As String = "2"
    Public Const Build As String = "0"
    Public Const Revision As String = "1"

    Public Const CurrentVersion As String = Hauptversion & "." & Nebenversion & "." & Build & "." & Revision
End Class

class MyAttribute
	Inherits System.Attribute

	Sub New(x as String)
	End Sub
End Class
    ]]></file>
</compilation>)

            CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/70007")>
        <Fact()>
        Public Sub CycleThroughAttribute_04()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Reflection

<Assembly: AssemblyVersion(MainVersion.CurrentVersion)>

<Module: MyAttribute(MainVersion.CurrentVersion)>

<MyAttribute(MainVersion.CurrentVersion)>
Public Class MainVersion
    Public Const Hauptversion As String = "8"
    Public Const Nebenversion As String = "2"
    Public Const Build As String = "0"
    Public Const Revision As String = "1"

    Public Const CurrentVersion As String = Hauptversion & "." & Nebenversion & "." & Build & "." & Revision
End Class

class MyAttribute
	Inherits System.Attribute

	Sub New(x as String)
	End Sub
End Class
    ]]></file>
</compilation>)

            CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

    End Class
End Namespace
