' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GenericConstraintTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub ConstraintWithContainingType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I(Of T)
End Interface
Class C(Of T As I(Of C(Of T)))
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub ConstraintWithSameType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I(Of T As I(Of T))
End Interface
Class C(Of T As C(Of T, U), U)
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        ' Constraint checking should handle cases where
        ' the type/method is the OriginalDefinition.
        <Fact()>
        Public Sub OriginalDefinition()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class A(Of T As Structure)
    Function F() As Object
        Return New A(Of T)()
    End Function
End Class
Class B
    Sub M(Of T As Class)()
        M(Of T)()
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub BaseWithSameType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface IA(Of T)
End Interface
Interface IB(Of T As IA(Of T))
    Inherits IA(Of IB(Of T))
End Interface
Class A(Of T)
End Class
Class B(Of T As A(Of T))
    Inherits A(Of B(Of T))
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        ' Constraints on base types and interfaces are checked lazily to
        ' avoid cycles. Verify that constraints are checked in those cases.
        ' (A simplified version of BasesInterfacesParametersAndReturnTypes
        ' from C#, without methods. Add methods to this test if we need
        ' to verify method parameters and return types as well.)
        <Fact()>
        Public Sub BasesAndInterfaces()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I(Of T As Structure)
End Interface
Class A(Of T As Structure)
    Friend Interface I
    End Interface
    Friend Class C
    End Class
End Class
Class B
    Friend Interface I(Of U As Structure)
    End Interface
    Friend Class C(Of U As Structure)
    End Class
End Class
' Simple type: A(Of T), etc.
Class C1(Of T)
    Inherits A(Of T)
    Implements I(Of T)
End Class
' Outer type: A(Of T).C, etc.
Class C2(Of T)
    Inherits A(Of T).C
    Implements A(Of T).I
End Class
' Inner type: B.C(Of T), etc.
Class C3(Of T)
    Inherits B.C(Of T)
    Implements B.I(Of T)
End Class
' Array: T()
Class C4(Of T)
    Inherits A(Of B.C(Of T)())
    Implements I(Of A(Of T)())
End Class
' Generic type parameter: A(Of I(Of T)), etc.
Class C5(Of T)
    Inherits A(Of I(Of T))
    Implements I(Of A(Of T))
End Class
' Multiple interfaces, multiple type parameters.
Structure S1(Of T, U)
    Implements I(Of Integer), B.I(Of U)
    Interface I
        Inherits I(Of U)
    End Interface
End Structure
    </file>
</compilation>)
            ' TODO: We're highlighting the derived type if a type argument to
            ' one of the base type or interfaces does not satisfy constraints.
            ' Dev10 highlights the type argument. The Roslyn error reporting is
            ' confusing if the type argument is used in multiple places. Log bug.
            compilation.AssertTheseDiagnostics(<errors>
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C1(Of T)
      ~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C1(Of T)
      ~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C2(Of T)
      ~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C2(Of T)
      ~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'U'.
Class C3(Of T)
      ~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'U'.
Class C3(Of T)
      ~~
BC32105: Type argument 'A(Of T)()' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C4(Of T)
      ~~
BC32105: Type argument 'B.C(Of T)()' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C4(Of T)
      ~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C4(Of T)
      ~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'U'.
Class C4(Of T)
      ~~
BC32105: Type argument 'A(Of T)' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C5(Of T)
      ~~
BC32105: Type argument 'I(Of T)' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C5(Of T)
      ~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C5(Of T)
      ~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C5(Of T)
      ~~
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'U'.
Structure S1(Of T, U)
          ~~
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Interface I
              ~
     </errors>)
        End Sub

        ' Constraints in method signatures are not checked
        ' at the time types in the signature are bound.
        ' Ensure the constraints are checked.
        <Fact()>
        Public Sub MethodSignatureConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Inherits System.Attribute
    Public Sub New(o As Object)
    End Sub
End Class
Class B(Of T As Class)
End Class
Class C
    <A(GetType(B(Of Integer)))>
    Shared Function F(<A(GetType(B(Of Single)))> o As B(Of Double)) As <A(GetType(B(Of Short)))> B(Of Byte)
        Return Nothing
    End Function
End Class
]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T'.
    <A(GetType(B(Of Integer)))>
                    ~~~~~~~
BC32106: Type argument 'Single' does not satisfy the 'Class' constraint for type parameter 'T'.
    Shared Function F(<A(GetType(B(Of Single)))> o As B(Of Double)) As <A(GetType(B(Of Short)))> B(Of Byte)
                                      ~~~~~~
BC32106: Type argument 'Double' does not satisfy the 'Class' constraint for type parameter 'T'.
    Shared Function F(<A(GetType(B(Of Single)))> o As B(Of Double)) As <A(GetType(B(Of Short)))> B(Of Byte)
                                                 ~
BC32106: Type argument 'Short' does not satisfy the 'Class' constraint for type parameter 'T'.
    Shared Function F(<A(GetType(B(Of Single)))> o As B(Of Double)) As <A(GetType(B(Of Short)))> B(Of Byte)
                                                                                       ~~~~~
BC32106: Type argument 'Byte' does not satisfy the 'Class' constraint for type parameter 'T'.
    Shared Function F(<A(GetType(B(Of Single)))> o As B(Of Double)) As <A(GetType(B(Of Short)))> B(Of Byte)
                                                                                                 ~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub DefaultArguments()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A(Of T As Structure)
    Const F As Integer = 1
    Shared Sub M(Optional arg As Integer = A(Of Object).F)
    End Sub
End Class
Class B
    Shared Function F(Of T As Structure)(Optional arg As Integer = F(Of String)())
        Return 0
    End Function
End Class
]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32105: Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Shared Sub M(Optional arg As Integer = A(Of Object).F)
                                                ~~~~~~
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Shared Function F(Of T As Structure)(Optional arg As Integer = F(Of String)())
                                                                   ~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub AttributeReferencingAttributedType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Inherits System.Attribute
    Public Sub New(o As Object)
    End Sub
End Class
<A(GetType(C(Of Object)))>
Class C(Of T As C(Of T))
End Class
]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'C(Of Object)'.
<A(GetType(C(Of Object)))>
                ~~~~~~
]]></errors>)
        End Sub

        ' Constraint checking for partial classes should handle
        ' cases where Inherits and Implements may be on one,
        ' more than one, or separate partial declarations.
        <Fact()>
        Public Sub PartialClasses()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I(Of T As Structure)
End Interface
Class A(Of T As Structure)
End Class
Partial Class C1 ' Part 1
    Inherits A(Of Object)
End Class
Partial Class C1 ' Part 2
    Implements I(Of String)
End Class
Partial Class C2 ' Part 1
End Class
Partial Class C2 ' Part 2
    Inherits A(Of String)
    Implements I(Of Object)
End Class
Partial Class C3 ' Part 1
    Inherits A(Of C1) ' Part 1
    Implements I(Of C2) ' Part 1
End Class
Partial Class C3 ' Part 2
    Inherits A(Of C1) ' Part 2
    Implements I(Of C2) ' Part 2
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC32105: Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'T'.
Partial Class C1 ' Part 1
              ~~
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'T'.
Partial Class C1 ' Part 2
              ~~
BC32105: Type argument 'Object' does not satisfy the 'Structure' constraint for type parameter 'T'.
Partial Class C2 ' Part 2
              ~~
BC32105: Type argument 'String' does not satisfy the 'Structure' constraint for type parameter 'T'.
Partial Class C2 ' Part 2
              ~~
BC32105: Type argument 'C1' does not satisfy the 'Structure' constraint for type parameter 'T'.
Partial Class C3 ' Part 1
              ~~
BC32105: Type argument 'C2' does not satisfy the 'Structure' constraint for type parameter 'T'.
Partial Class C3 ' Part 1
              ~~
     </errors>)
        End Sub

        <Fact()>
        Public Sub InaccessibleConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Friend Class A
End Class
Public Class B(Of T As A, U As C)
    Private Class C
    End Class
End Class
    </file>
</compilation>)
            compilation.AssertTheseDeclarationDiagnostics(
<expected><![CDATA[
BC30909: 'B' cannot expose type 'A' outside the project through class 'B'.
Public Class B(Of T As A, U As C)
                       ~
BC30508: 'B' cannot expose type 'B(Of T, U).C' in namespace '<Default>' through class 'B'.
Public Class B(Of T As A, U As C)
                               ~
    ]]>
</expected>)
        End Sub

        ' Report BC32082ERR_MustInheritForNewConstraint2 for abstract type that does
        ' not satisfy the 'New' constraint only if the type has a public parameterless
        ' constructor. Otherwise report BC32083ERR_NoSuitableNewForNewConstraint2.
        <Fact()>
        Public Sub NewConstraintAndMustInherit()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
MustInherit Class A
    Public Sub New()
    End Sub
End Class
MustInherit Class B
End Class
MustInherit Class C
    Protected Sub New()
    End Sub
End Class
Class D(Of T As New)
    Shared Sub M(Of U As New)()
        Dim o
        o = New D(Of A)()
        M(Of A)()
        o = New D(Of B)()
        M(Of B)()
        o = New D(Of C)()
        M(Of C)()
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC32082: Type argument 'A' is declared 'MustInherit' and does not satisfy the 'New' constraint for type parameter 'T'.
        o = New D(Of A)()
                     ~
BC32082: Type argument 'A' is declared 'MustInherit' and does not satisfy the 'New' constraint for type parameter 'U'.
        M(Of A)()
        ~~~~~~~
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        o = New D(Of B)()
                     ~
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'U'.
        M(Of B)()
        ~~~~~~~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        o = New D(Of C)()
                     ~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'U'.
        M(Of C)()
        ~~~~~~~
     </errors>)
        End Sub

        <Fact()>
        Public Sub Aliases()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports A1 = N1.A
Imports N2 = N1
Namespace N1
    Class A
    End Class
End Namespace
Namespace N3
    Class B(Of T1 As N1.A, T2 As A1, T3 As N2.A)
    End Class
    Class C(Of T As N1, U As N2)
    End Class
End Namespace
    </file>
</compilation>)
            compilation.AssertTheseDeclarationDiagnostics(
<expected>
BC30182: Type expected.
    Class C(Of T As N1, U As N2)
                    ~~
BC30182: Type expected.
    Class C(Of T As N1, U As N2)
                             ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub EffectiveBaseType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Structure S
End Structure
Class A(Of T)
    Overridable Sub F(Of U As T)(o As U)
    End Sub
    Sub M(o As T)
    End Sub
End Class
Class B1
    Inherits A(Of Integer)
    Public Overrides Sub F(Of U As Integer)(o As U)
        Dim i As Integer = o
        M(o)
    End Sub
End Class
Class B2
    Inherits A(Of S)
    Public Overrides Sub F(Of U As S)(o As U)
        Dim s As S = o
        M(o)
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        <WorkItem(544122, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544122")>
        <Fact()>
        Public Sub [TryCast]()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Class A
End Class
Class B1(Of T As A)
    Shared Function F1(Of U)(o As U) As T
        Return TryCast(o, T)
    End Function
    Shared Function F2(Of U As A)(o As U) As T
        Return TryCast(o, T)
    End Function
    Shared Function F3(Of U As Class)(o As U) As T
        Return TryCast(o, T)
    End Function
End Class
Class B2(Of T As Class)
    Shared Function F1(Of U)(o As U) As T
        Return TryCast(o, T)
    End Function
    Shared Function F2(Of U As A)(o As U) As T
        Return TryCast(o, T)
    End Function
    Shared Function F3(Of U As Class)(o As U) As T
        Return TryCast(o, T)
    End Function
    Shared Function F4(Of U As Structure)(o As U) As T
        Return TryCast(o, T)
    End Function
End Class
    </file>
</compilation>)
            Dim expectedIL = <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "U"
  IL_0006:  isinst     "T"
  IL_000b:  unbox.any  "T"
  IL_0010:  ret
}
]]>.Value
            compilationVerifier.VerifyIL("B1(Of T).F1(Of U)(U)", expectedIL.Replace("F1", "F1"))
            compilationVerifier.VerifyIL("B1(Of T).F2(Of U)(U)", expectedIL.Replace("F1", "F2"))
            compilationVerifier.VerifyIL("B1(Of T).F3(Of U)(U)", expectedIL.Replace("F1", "F3"))
            compilationVerifier.VerifyIL("B2(Of T).F1(Of U)(U)", expectedIL.Replace("F1", "F1"))
            compilationVerifier.VerifyIL("B2(Of T).F2(Of U)(U)", expectedIL.Replace("F1", "F2"))
            compilationVerifier.VerifyIL("B2(Of T).F3(Of U)(U)", expectedIL.Replace("F1", "F3"))
            compilationVerifier.VerifyIL("B2(Of T).F4(Of U)(U)", expectedIL.Replace("F1", "F4"))
        End Sub

        <Fact()>
        Public Sub [DirectCast]()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Class A
End Class
Class B1
    Shared Function F1(Of T, U As T)(o As U) As T
        Return DirectCast(o, T)
    End Function
    Shared Function F2(Of T As A, U As T)(o As U) As T
        Return DirectCast(o, T)
    End Function
    Shared Function F3(Of T As Class, U As T)(o As U) As T
        Return DirectCast(o, T)
    End Function
    Shared Function F3_1(Of T As Class, U As {T, A})(o As U) As T
        Return DirectCast(o, T)
    End Function
    Shared Function F4(Of T, U As {Class, T})(o As U) As T
        Return DirectCast(o, T)
    End Function
    Shared Function F5(Of T, U As {Structure, T})(o As U) As T
        Return DirectCast(o, T)
    End Function
End Class
Class B2
    Shared Function F1(Of T As U, U)(o As U) As T
        Return DirectCast(o, T)
    End Function
    Shared Function F2(Of T As U, U As A)(o As U) As T
        Return DirectCast(o, T)
    End Function
    Shared Function F3(Of T As U, U As Class)(o As U) As T
        Return DirectCast(o, T)
    End Function
    Shared Function F4(Of T As {Class, U}, U)(o As U) As T
        Return DirectCast(o, T)
    End Function
    Shared Function F5(Of T As {Structure, U}, U)(o As U) As T
        Return DirectCast(o, T)
    End Function
End Class
    </file>
</compilation>)
        End Sub

        <Fact()>
        Public Sub NewT()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Structure S
End Structure
Class C
End Class
Module M
    Function F1(Of T As New)()
        Return New T()
    End Function
    Function F2(Of T As {Class, New})()
        Return New T()
    End Function
    Function F3(Of T As Structure)()
        Return New T()
    End Function
    Sub M(o As Object)
        System.Console.WriteLine("{0}", o)
    End Sub
    Sub Main()
        M(F1(Of C)())
        M(F1(Of S)())
        M(F2(Of C)())
        M(F3(Of S)())
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
C
S
C
S]]>)
            compilationVerifier.VerifyIL("M.F1(Of T)()",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  call       "Function System.Activator.CreateInstance(Of T)() As T"
  IL_0005:  box        "T"
  IL_000a:  ret
}]]>)
            compilationVerifier.VerifyIL("M.F2(Of T)()",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  call       "Function System.Activator.CreateInstance(Of T)() As T"
  IL_0005:  box        "T"
  IL_000a:  ret
}]]>)
            compilationVerifier.VerifyIL("M.F3(Of T)()",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  call       "Function System.Activator.CreateInstance(Of T)() As T"
  IL_0005:  box        "T"
  IL_000a:  ret
}]]>)
        End Sub

        ' Should bind type parameter constructor arguments
        ' even though no arguments are expected.
        <Fact()>
        Public Sub NewTWithBadArguments()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Structure S(Of T As New, U)
    Shared Sub M()
        Dim o
        o = New T(F())
        o = New U(G())
    End Sub
End Structure
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC30451: 'F' is not declared. It may be inaccessible due to its protection level.
        o = New T(F())
                  ~
BC32085: Arguments cannot be passed to a 'New' used on a type parameter.
        o = New T(F())
                  ~~~
BC32046: 'New' cannot be used on a type parameter that does not have a 'New' constraint.
        o = New U(G())
                ~
BC30451: 'G' is not declared. It may be inaccessible due to its protection level.
        o = New U(G())
                  ~
     </errors>)
        End Sub

        ' Invoke methods and properties on constrained generic types.
        <Fact()>
        Public Sub Members()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Option Strict On
Imports System
Interface I
    Property P As Object
    Sub M()
End Interface
MustInherit Class A
    Public MustOverride Property P As Object
    Public MustOverride Sub M()
End Class
Class B
    Inherits A
    Implements I
    Public Overrides Property P As Object Implements I.P
        Get
            Console.WriteLine("B.get_P")
            Return Nothing
        End Get
        Set(value As Object)
            Console.WriteLine("B.set_P")
        End Set
    End Property
    Public Overrides Sub M() Implements I.M
        Console.WriteLine("B.M")
    End Sub
End Class
Structure S
    Implements I
    Public Property P As Object Implements I.P
        Get
            Console.WriteLine("S.get_P")
            Return Nothing
        End Get
        Set(value As Object)
            Console.WriteLine("S.set_P")
        End Set
    End Property
    Public Sub M() Implements I.M
        Console.WriteLine("S.M")
    End Sub
End Structure
Class C(Of T1 As I, T2 As A)
    Friend Shared Sub M(Of U1 As I, U2 As A)(_t1 As T1, _t2 As T2, _u1 As U1, _u2 As U2)
        _t1.P = _t1.P
        _t1.M()
        _t2.P = _t2.P
        _t2.M()
        _u1.P = _u1.P
        _u1.M()
        _u2.P = _u2.P
        _u2.M()
    End Sub
End Class
Module M
    Sub Main()
        Dim b = New B()
        Dim s = New S()
        C(Of I, A).M(s, b, s, b)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
S.get_P
S.set_P
S.M
B.get_P
B.set_P
B.M
S.get_P
S.set_P
S.M
B.get_P
B.set_P
B.M]]>)
            compilationVerifier.VerifyIL("C(Of T1, T2).M(Of U1, U2)(T1, T2, U1, U2)",
            <![CDATA[
{
      // Code size      221 (0xdd)
  .maxstack  2
  .locals init (T1 V_0,
                U1 V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldloca.s   V_0
  IL_0004:  initobj    "T1"
  IL_000a:  ldloc.0
  IL_000b:  box        "T1"
  IL_0010:  brtrue.s   IL_001a
  IL_0012:  ldobj      "T1"
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  ldarga.s   V_0
  IL_001c:  constrained. "T1"
  IL_0022:  callvirt   "Function I.get_P() As Object"
  IL_0027:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002c:  constrained. "T1"
  IL_0032:  callvirt   "Sub I.set_P(Object)"
  IL_0037:  ldarga.s   V_0
  IL_0039:  constrained. "T1"
  IL_003f:  callvirt   "Sub I.M()"
  IL_0044:  ldarg.1
  IL_0045:  box        "T2"
  IL_004a:  ldarga.s   V_1
  IL_004c:  constrained. "T2"
  IL_0052:  callvirt   "Function A.get_P() As Object"
  IL_0057:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_005c:  callvirt   "Sub A.set_P(Object)"
  IL_0061:  ldarga.s   V_1
  IL_0063:  constrained. "T2"
  IL_0069:  callvirt   "Sub A.M()"
  IL_006e:  ldarga.s   V_2
  IL_0070:  ldloca.s   V_1
  IL_0072:  initobj    "U1"
  IL_0078:  ldloc.1
  IL_0079:  box        "U1"
  IL_007e:  brtrue.s   IL_0088
  IL_0080:  ldobj      "U1"
  IL_0085:  stloc.1
  IL_0086:  ldloca.s   V_1
  IL_0088:  ldarga.s   V_2
  IL_008a:  constrained. "U1"
  IL_0090:  callvirt   "Function I.get_P() As Object"
  IL_0095:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_009a:  constrained. "U1"
  IL_00a0:  callvirt   "Sub I.set_P(Object)"
  IL_00a5:  ldarga.s   V_2
  IL_00a7:  constrained. "U1"
  IL_00ad:  callvirt   "Sub I.M()"
  IL_00b2:  ldarg.3
  IL_00b3:  box        "U2"
  IL_00b8:  ldarga.s   V_3
  IL_00ba:  constrained. "U2"
  IL_00c0:  callvirt   "Function A.get_P() As Object"
  IL_00c5:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_00ca:  callvirt   "Sub A.set_P(Object)"
  IL_00cf:  ldarga.s   V_3
  IL_00d1:  constrained. "U2"
  IL_00d7:  callvirt   "Sub A.M()"
  IL_00dc:  ret
}
]]>)
        End Sub

        ' Access fields on constrained generic types.
        <WorkItem(543305, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543305")>
        <Fact()>
        Public Sub Fields()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Class A
    Friend F As Object
End Class
Class B(Of T As A)
    Friend Shared Sub Swap(Of U As T)(_t As T, _u As U)
        Dim v1 = _t.F
        Dim v2 = _u.F
        _t.F = v2
        _u.F = v1
    End Sub
End Class
Module M
    Sub Main()
        Dim a1 = New A()
        Dim a2 = New A()
        a1.F = 1
        a2.F = 2
        B(Of A).Swap(a1, a2)
        System.Console.WriteLine("{0}, {1}", a1.F, a2.F)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="2, 1")
            compilationVerifier.VerifyIL("B(Of T).Swap(Of U)(T, U)",
            <![CDATA[
{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (Object V_0, //v1
  Object V_1) //v2
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  ldfld      "A.F As Object"
  IL_000b:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0010:  stloc.0
  IL_0011:  ldarg.1
  IL_0012:  box        "U"
  IL_0017:  ldfld      "A.F As Object"
  IL_001c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0021:  stloc.1
  IL_0022:  ldarg.0
  IL_0023:  box        "T"
  IL_0028:  ldloc.1
  IL_0029:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002e:  stfld      "A.F As Object"
  IL_0033:  ldarg.1
  IL_0034:  box        "U"
  IL_0039:  ldloc.0
  IL_003a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_003f:  stfld      "A.F As Object"
  IL_0044:  ret
}]]>)
        End Sub

        ' Pass field on type T by ref and
        ' pass field of type T by ref.
        <Fact()>
        Public Sub FieldAddress()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Class A
    Public F As Object
End Class
Class B(Of T)
    Public F As T
End Class
Module M
    ' Pass field on type T by ref.
    Sub M1(Of T As A)(o As T)
        M(o.F)
    End Sub
    ' Pass field of type T by ref.
    Sub M2(Of T)(o As B(Of T))
        M(o.F)
    End Sub
    Sub M(Of T)(ByRef arg As T)
    End Sub
End Module
    </file>
</compilation>)
        End Sub

        ' Catch into a captured local to test
        ' catching into field of type T.
        <WorkItem(543382, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543382")>
        <Fact()>
        Public Sub FieldInCatch()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Module M
    Sub M(Of T As System.Exception)()
        Dim e As T
        Dim a = Sub()
                    Try
                    Catch e
                    End Try
                End Sub
        a()
    End Sub
End Module
    </file>
</compilation>)
        End Sub

        <Fact()>
        Public Sub DefaultProperty()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
    Default Property P(o As Object)
End Interface
Class A
    Default ReadOnly Property P(x As Integer, y As Integer)
        Get
            Return Nothing
        End Get
    End Property
End Class
Class B
    Inherits A
    Implements I
    Default Public Overloads Property P(o As Object) Implements I.P
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class
Class C
    Shared Sub M(Of T1 As I, T2 As A, T3 As {A, I}, T4 As T3, T5 As B)(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5)
        Dim o = Nothing
        Dim x = 1
        Dim y = 2
        _1(o) = _1(x, y)
        _2(o) = _2(x, y)
        _3(o) = _3(x, y)
        _4(o) = _4(x, y)
        _5(o) = _5(x, y)
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30057: Too many arguments to 'Default Property P(o As Object) As Object'.
        _1(o) = _1(x, y)
                      ~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Default Property P(x As Integer, y As Integer) As Object'.
        _2(o) = _2(x, y)
        ~~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Default Property P(x As Integer, y As Integer) As Object'.
        _3(o) = _3(x, y)
        ~~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Default Property P(x As Integer, y As Integer) As Object'.
        _4(o) = _4(x, y)
        ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub DefaultPropertyInheritedConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface IR
    Default ReadOnly Property P(o As Object)
End Interface
Interface IW
    Default WriteOnly Property P(o As Object)
End Interface
Class CR
    Default ReadOnly Property Q(o As Object)
        Get
            Return Nothing
        End Get
    End Property
End Class
Class CW
    Default WriteOnly Property Q(o As Object)
        Set(value)
        End Set
    End Property
End Class
Structure SR
    Default ReadOnly Property R(o As Object)
        Get
            Return Nothing
        End Get
    End Property
End Structure
Structure SW
    Default WriteOnly Property R(o As Object)
        Set(value)
        End Set
    End Property
End Structure
MustInherit Class A(Of T)
    Overridable Function F(Of U As T)(x As U, y As Object)
        Return Nothing
    End Function
    Overridable Function G(Of U As {T, IR})(x As U, y As Object)
        Return Nothing
    End Function
    Overridable Function H(Of U As {T, IW})(x As U, y As Object)
        Return Nothing
    End Function
End Class
Class B1
    Inherits A(Of IR)
    Public Overrides Function F(Of U As IR)(x As U, y As Object) As Object
        Return x(y) ' B1.F
    End Function
End Class
Class B2
    Inherits A(Of IW)
    Public Overrides Function F(Of U As IW)(x As U, y As Object) As Object
        Return x(y) ' B2.F
    End Function
End Class
Class B3
    Inherits A(Of CR)
    Public Overrides Function F(Of U As CR)(x As U, y As Object) As Object
        Return x(y) ' B3.F
    End Function
    Public Overrides Function G(Of U As {CR, IR})(x As U, y As Object) As Object
        Return x(y) ' B3.G
    End Function
    Public Overrides Function H(Of U As {CR, IW})(x As U, y As Object) As Object
        Return x(y) ' B3.H
    End Function
End Class
Class B4
    Inherits A(Of CW)
    Public Overrides Function F(Of U As CW)(x As U, y As Object) As Object
        Return x(y) ' B4.F
    End Function
    Public Overrides Function G(Of U As {CW, IR})(x As U, y As Object) As Object
        Return x(y) ' B4.G
    End Function
    Public Overrides Function H(Of U As {CW, IW})(x As U, y As Object) As Object
        Return x(y) ' B4.H
    End Function
End Class
Class B5
    Inherits A(Of SR)
    Public Overrides Function F(Of U As SR)(x As U, y As Object) As Object
        Return x(y) ' B5.F
    End Function
    Public Overrides Function G(Of U As {SR, IR})(x As U, y As Object) As Object
        Return x(y) ' B5.G
    End Function
    Public Overrides Function H(Of U As {SR, IW})(x As U, y As Object) As Object
        Return x(y) ' B5.H
    End Function
End Class
Class B6
    Inherits A(Of SW)
    Public Overrides Function F(Of U As SW)(x As U, y As Object) As Object
        Return x(y) ' B6.F
    End Function
    Public Overrides Function G(Of U As {SW, IR})(x As U, y As Object) As Object
        Return x(y) ' B6.G
    End Function
    Public Overrides Function H(Of U As {SW, IW})(x As U, y As Object) As Object
        Return x(y) ' B6.H
    End Function
End Class
Class B7
    Inherits A(Of System.Array)
    Public Overrides Function F(Of U As System.Array)(x As U, y As Object) As Object
        Return x(y) ' B7.F
    End Function
End Class
Class B8
    Inherits A(Of Object())
    Public Overrides Function F(Of U As Object())(x As U, y As Object)
        Return x(y) ' B8.F
    End Function
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30524: Property 'P' is 'WriteOnly'.
        Return x(y) ' B2.F
               ~~~~
BC30524: Property 'Q' is 'WriteOnly'.
        Return x(y) ' B4.F
               ~~~~
BC30524: Property 'Q' is 'WriteOnly'.
        Return x(y) ' B4.G
               ~~~~
BC30524: Property 'Q' is 'WriteOnly'.
        Return x(y) ' B4.H
               ~~~~
BC30547: 'U' cannot be indexed because it has no default property.
        Return x(y) ' B5.F
               ~
BC30524: Property 'P' is 'WriteOnly'.
        Return x(y) ' B5.H
               ~~~~
BC30547: 'U' cannot be indexed because it has no default property.
        Return x(y) ' B6.F
               ~
BC30524: Property 'P' is 'WriteOnly'.
        Return x(y) ' B6.H
               ~~~~
BC30547: 'U' cannot be indexed because it has no default property.
        Return x(y) ' B7.F
               ~
BC30547: 'U' cannot be indexed because it has no default property.
        Return x(y) ' B8.F
               ~
</expected>)
        End Sub

        <Fact()>
        Public Sub DefaultPropertyAmbiguous()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Interface IA
    Default Property A(o As Object)
End Interface
Interface IB
    Default Property B(o As Object)
End Interface
Interface IC
    Default Property C(x As Object, y As Object)
End Interface
Class A
    Default Public Property A(o As Object)
        Get
            Return Nothing
        End Get
        Set(value)
        End Set
    End Property
End Class
Class C
    Default Public Property C(x As Object, y As Object)
        Get
            Return Nothing
        End Get
        Set(value)
        End Set
    End Property
End Class
Module M
    Sub M(Of T1 As IA, T2 As {IA, IB}, T3 As {IB, IC}, T4 As {A, IA}, T5 As {C, IA}, T6 As {A, T1}, T7 As {A, T3}, T8 As {T1, IA}, T9 As T1, T10 As {T9, T2})(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6, _7 As T7, _8 As T8, _9 As T9, _10 As T10, o As Object)
        o = _1(o)
        o = _2(o)
        o = _3(o)
        o = _3(o, o)
        o = _4(o)
        o = _5(o)
        o = _5(o, o)
        o = _6(o)
        o = _7(o)
        o = _8(o)
        o = _9(o)
        o = _10(o)
    End Sub
End Module
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30686: Default property access is ambiguous between the inherited interface members 'Default Property A(o As Object) As Object' of interface 'IA' and 'Default Property B(o As Object) As Object' of interface 'IB'.
        o = _2(o)
            ~~
BC30686: Default property access is ambiguous between the inherited interface members 'Default Property B(o As Object) As Object' of interface 'IB' and 'Default Property C(x As Object, y As Object) As Object' of interface 'IC'.
        o = _3(o)
            ~~
BC30686: Default property access is ambiguous between the inherited interface members 'Default Property B(o As Object) As Object' of interface 'IB' and 'Default Property C(x As Object, y As Object) As Object' of interface 'IC'.
        o = _3(o, o)
            ~~
BC30455: Argument not specified for parameter 'y' of 'Public Default Property C(x As Object, y As Object) As Object'.
        o = _5(o)
            ~~
BC30686: Default property access is ambiguous between the inherited interface members 'Default Property A(o As Object) As Object' of interface 'IA' and 'Default Property B(o As Object) As Object' of interface 'IB'.
        o = _10(o)
            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MembersInheritedConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
    Sub M(o As Object)
    ReadOnly Property P
End Interface
Class C
    Public Sub M(o As Object)
    End Sub
    Public ReadOnly Property P
        Get
            Return Nothing
        End Get
    End Property
End Class
Structure S
    Public Sub M(o As Object)
    End Sub
    Public ReadOnly Property P
        Get
            Return Nothing
        End Get
    End Property
End Structure
MustInherit Class A(Of T)
    MustOverride Sub M1(Of U As T)(o As U)
    MustOverride Sub M2(Of U As {T, I})(o As U)
End Class
Class B1
    Inherits A(Of I)
    Public Overrides Sub M1(Of U As I)(o As U)
        o.M(o.P) ' B1.M1
    End Sub
    Public Overrides Sub M2(Of U As I)(o As U)
        o.M(o.P) ' B1.M2
    End Sub
End Class
Class B2
    Inherits A(Of C)
    Public Overrides Sub M1(Of U As C)(o As U)
        o.M(o.P) ' B2.M1
    End Sub
    Public Overrides Sub M2(Of U As {C, I})(o As U)
        o.M(o.P) ' B2.M2
    End Sub
End Class
Class B3
    Inherits A(Of S)
    Public Overrides Sub M1(Of U As S)(o As U)
        o.M(o.P) ' B3.M1
    End Sub
    Public Overrides Sub M2(Of U As {S, I})(o As U)
        o.M(o.P) ' B3.M2
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30456: 'M' is not a member of 'U'.
        o.M(o.P) ' B3.M1
        ~~~
BC30456: 'P' is not a member of 'U'.
        o.M(o.P) ' B3.M1
            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ThrowT()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Class C(Of T As System.Exception)
    Shared Sub ThrowT(e As T)
        Throw e
    End Sub
    Shared Sub ThrowU(Of U As {T, New})()
        Throw New U()
    End Sub
    Shared Sub RethrowT()
        Try
        Catch e As T
            Throw
        End Try
    End Sub
End Class
    </file>
</compilation>)
            compilationVerifier.VerifyIL("C(Of T).ThrowT(T)",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  throw
}]]>)
        End Sub

        <Fact()>
        Public Sub CatchT()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Class C(Of T As System.Exception)
    Shared Sub M(Of U As T)()
        Try
        Catch e As T
        End Try
        Try
        Catch e As U
        End Try
    End Sub
End Class
    </file>
</compilation>)
        End Sub

        <Fact()>
        Public Sub CatchTLifted()
            Dim compilationVerifier = CompileAndVerify(
<compilation>
    <file name="c.vb">
Module M
    Sub main()
        C(Of System.Exception).M(Of System.ArgumentException)()
    End Sub

    Class C(Of T As System.Exception)

        Shared Sub M(Of U As T)()
            Dim a As System.Action = Sub()
                                         Try
                                         Catch e As T
                                             Dim aa As System.Action = Sub() System.Console.WriteLine(e.ToString())
                                             aa()
                                         End Try

                                         Try
                                         Catch e As U
                                             Dim aa As System.Action = Sub() System.Console.WriteLine(e.ToString())
                                             aa()
                                         End Try
                                     End Sub

        End Sub

    End Class
End Module
    </file>
</compilation>)
        End Sub

        ' Member lookup should prefer class constraint over interfaces.
        ' And if there is no class constraint, member lookup should use
        ' System.Object or System.ValueType after lookup on interfaces.
        <Fact()>
        Public Sub MemberLookupOrder()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
    Sub M(arg As Object)
    ReadOnly Property P(arg As Object) As Object
    Sub GetHashCode(arg As Object)
End Interface
Class A
    Public Sub M()
    End Sub
    Public ReadOnly Property P(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
Class B
    Implements I
    Public Sub M()
    End Sub
    Public ReadOnly Property P(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Sub I_M(arg As Object) Implements I.M
    End Sub
    Public ReadOnly Property I_P(arg As Object) As Object Implements I.P
        Get
            Return Nothing
        End Get
    End Property
    Public Sub I_GetHashCode(arg As Object) Implements I.GetHashCode
    End Sub
    Sub M(Of T1, T2 As I, T3 As {New, I}, T4 As {Structure, I}, T5 As {Class, I}, T6 As {A, I}, T7 As B)(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6, _7 As T7)
        Dim o As Object
        _1.GetType()
        _1.GetHashCode()
        _2.GetType()
        _2.GetHashCode(Nothing)
        _2.M(Nothing)
        o = _2.P(Nothing)
        _3.GetType()
        _3.GetHashCode(Nothing)
        _3.M(Nothing)
        o = _3.P(Nothing)
        _4.GetType()
        _4.GetHashCode(Nothing)
        _4.M(Nothing)
        o = _4.P(Nothing)
        _5.GetType()
        _5.GetHashCode(Nothing)
        _5.M(Nothing)
        o = _5.P(Nothing)
        _6.GetType()
        _6.GetHashCode(Nothing)
        _6.M(Nothing)
        o = _6.P(Nothing)
        _7.GetType()
        _7.GetHashCode(Nothing)
        _7.M(Nothing)
        o = _7.P(Nothing)
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30057: Too many arguments to 'Public Overridable Overloads Function GetHashCode() As Integer'.
        _6.GetHashCode(Nothing)
                       ~~~~~~~
BC30057: Too many arguments to 'Public Sub M()'.
        _6.M(Nothing)
             ~~~~~~~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Property P(x As Object, y As Object) As Object'.
        o = _6.P(Nothing)
               ~
BC30057: Too many arguments to 'Public Overridable Overloads Function GetHashCode() As Integer'.
        _7.GetHashCode(Nothing)
                       ~~~~~~~
BC30516: Overload resolution failed because no accessible 'M' accepts this number of arguments.
        _7.M(Nothing)
           ~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Property P(x As Object, y As Object) As Object'.
        o = _7.P(Nothing)
               ~
</expected>)
        End Sub

        ' Lookup members on interfaces across all constraint types, and
        ' avoid reporting ambiguities if the same interface is repeated.
        <Fact()>
        Public Sub MemberLookupOnInterfaces()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface IA
    Sub M1()
End Interface
Interface IB
    Sub M1()
End Interface
Interface IC
    Sub M2()
End Interface
Class M
    Shared Sub M(Of T As {IB, IC}, U As {T, IA, IB})(arg As U)
        arg.M1()
        arg.M2()
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30521: Overload resolution failed because no accessible 'M1' is most specific for these arguments:
    'Sub IB.M1()': Not most specific.
    'Sub IA.M1()': Not most specific.
        arg.M1()
            ~~
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub MemberLookupOverloadsWithMultipleTypeConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class A
    Friend F1 As Object
    Friend Sub M1(o As Object)
    End Sub
    Friend Overloads Sub M3()
    End Sub
    Friend Overloads Sub M4(o As Object)
    End Sub
End Class
Class B
    Inherits A
    Friend F2 As Object
    Friend Sub M2(o As Object)
    End Sub
    Friend Overloads Sub M3(o As Object)
    End Sub
    Friend Overloads Sub M4()
    End Sub
End Class
MustInherit Class C(Of T)
    MustOverride Sub M(Of U As {A, T})(o As U)
End Class
Class D
    Inherits C(Of B)
    Public Overrides Sub M(Of U As {A, B})(o As U)
        o.M1(o.F1)
        o.M2(o.F2)
        o.M3()
        o.M4()
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub MemberLookupWithMultipleTypeConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class A
End Class
Class B
    Inherits A
End Class
Structure S
End Structure
Enum E
    A
End Enum
MustInherit Class C(Of T1, T2)
    Shared Sub M_Object(o As Object)
    End Sub
    Shared Sub M_A(o As A)
    End Sub
    Shared Sub M_B(o As B)
    End Sub
    Shared Sub M_S(o As S)
    End Sub
    Shared Sub M_A_Array(o As A())
    End Sub
    Shared Sub M_Integer(o As Integer)
    End Sub
    Shared Sub M_E(o As E)
    End Sub
    Shared Sub M_ValueType(o As System.ValueType)
    End Sub
    Shared Sub M_Enum(o As System.Enum)
    End Sub
    Shared Sub M_Array(o As System.Array)
    End Sub
    Overridable Sub M(Of U1 As {T1, T2}, U2 As {T2, T1})(x As U1, y As U2)
        M_Object(x)
    End Sub
End Class
Class C0
    Inherits C(Of A, B)
    Public Overrides Sub M(Of U1 As {A, B}, U2 As {B, A})(x As U1, y As U2)
        M_A(x)
        M_B(x)
        M_A(y)
        M_B(y)
    End Sub
End Class
Class C1
    Inherits C(Of Integer, System.ValueType)
    Public Overrides Sub M(Of U1 As {Integer, System.ValueType}, U2 As {System.ValueType, Integer})(x As U1, y As U2)
        M_Integer(x)
        M_ValueType(x)
        M_Integer(y)
        M_ValueType(y)
    End Sub
End Class
Class C2
    Inherits C(Of System.ValueType, S)
    Public Overrides Sub M(Of U1 As {System.ValueType, S}, U2 As {S, System.ValueType})(x As U1, y As U2)
        M_ValueType(x)
        M_S(x)
        M_ValueType(y)
        M_S(y)
    End Sub
End Class
Class C3
    Inherits C(Of System.Enum, E)
    Public Overrides Sub M(Of U1 As {System.Enum, E}, U2 As {E, System.Enum})(x As U1, y As U2)
        M_Enum(x)
        M_E(x)
        M_Enum(y)
        M_E(y)
    End Sub
End Class
Class C4
    Inherits C(Of System.Array, A())
    Public Overrides Sub M(Of U1 As {System.Array, A()}, U2 As {A(), System.Array})(x As U1, y As U2)
        M_Array(x)
        M_A_Array(x)
        M_Array(y)
        M_A_Array(y)
    End Sub
End Class
Class C5(Of T As Structure)
    Inherits C(Of T, T)
    Public Overrides Sub M(Of U1 As {T}, U2 As {T})(x As U1, y As U2)
        M_Object(y)
        M_ValueType(y)
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub MemberLookupClassAndInterfaceConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Interface IA
    Sub MA(o)
    Default ReadOnly Property PA(o)
End Interface
Interface IB
    Sub MB(o)
    Default ReadOnly Property PB(o)
End Interface
Interface IC
    Sub MC(o)
    Default ReadOnly Property PC(o)
End Interface
Interface ID
    Sub MD(Of T)(o)
    Default ReadOnly Property PD(o)
End Interface
' Public implementation.
Class A
    Implements IA
    Public Sub MA(o) Implements IA.MA
    End Sub
    Default Public ReadOnly Property PA(o) Implements IA.PA
        Get
            Return Nothing
        End Get
    End Property
End Class
' Private implementation.
Class B
    Implements IB
    Private Sub MB(o) Implements IB.MB
    End Sub
    Private ReadOnly Property PB(o) Implements IB.PB
        Get
            Return Nothing
        End Get
    End Property
End Class
' No explicit implementation.
Class C
    Public Sub MC(o)
    End Sub
    Default Public ReadOnly Property PC(o)
        Get
            Return Nothing
        End Get
    End Property
End Class
' Private implementation and public members with different arity or argument count.
Class D
    Implements ID
    Public Sub MD(Of T, U)(o)
    End Sub
    Default Public ReadOnly Property PD(x, y)
        Get
            Return Nothing
        End Get
    End Property
    Private Sub MD_ID(Of T)(o As Object) Implements ID.MD
    End Sub
    Private ReadOnly Property PD_ID(o As Object) As Object Implements ID.PD
        Get
            Return Nothing
        End Get
    End Property
End Class
Module M
    Sub MA(Of T1 As IA, T2 As A, T3 As {A, IA}, T4 As {T1, T2})(_1 As T1, _2 As T2, _3 As T3, _4 As T4, o As Object)
        _1.MA(_1(o))
        _2.MA(_2(o))
        _3.MA(_3(o))
        _4.MA(_4(o))
    End Sub
    Sub MB(Of T1 As IB, T2 As B, T3 As {B, IB}, T4 As {T1, T2})(_1 As T1, _2 As T2, _3 As T3, _4 As T4, o As Object)
        _1.MB(_1(o))
        _2.MB(_2(o))
        _3.MB(_3(o))
        _4.MB(_4(o))
    End Sub
    Sub MC(Of T1 As IC, T2 As C, T3 As {C, IC}, T4 As {T1, T2})(_1 As T1, _2 As T2, _3 As T3, _4 As T4, o As Object)
        _1.MC(_1(o))
        _2.MC(_2(o))
        _3.MC(_3(o))
        _4.MC(_4(o))
    End Sub
    Sub MD(Of T1 As ID, T2 As D, T3 As {D, ID}, T4 As {T1, T2})(_1 As T1, _2 As T2, _3 As T3, _4 As T4, o As Object)
        _1.MD(Of Object)(_1(o))
        _2.MD(Of Object)(_2(o))
        _3.MD(Of Object)(_3(o))
        _4.MD(Of Object)(_4(o))
    End Sub
End Module
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30390: 'B.Private Sub MB(o As Object)' is not accessible in this context because it is 'Private'.
        _2.MB(_2(o))
        ~~~~~
BC30547: 'T2' cannot be indexed because it has no default property.
        _2.MB(_2(o))
              ~~
BC32042: Too few type arguments to 'Public Sub MD(Of T, U)(o As Object)'.
        _2.MD(Of Object)(_2(o))
             ~~~~~~~~~~~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Default Property PD(x As Object, y As Object) As Object'.
        _2.MD(Of Object)(_2(o))
                         ~~
BC32042: Too few type arguments to 'Public Sub MD(Of T, U)(o As Object)'.
        _3.MD(Of Object)(_3(o))
             ~~~~~~~~~~~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Default Property PD(x As Object, y As Object) As Object'.
        _3.MD(Of Object)(_3(o))
                         ~~
BC32042: Too few type arguments to 'Public Sub MD(Of T, U)(o As Object)'.
        _4.MD(Of Object)(_4(o))
             ~~~~~~~~~~~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Default Property PD(x As Object, y As Object) As Object'.
        _4.MD(Of Object)(_4(o))
                         ~~
</expected>)
        End Sub

        ' Various other cases with class and interface constraints.
        <Fact()>
        Public Sub MemberLookupClassAndInterfaceConstraints_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Interface I
    Sub M()
End Interface
' Protected implementation.
Class A
    Implements I
    Protected Sub M() Implements I.M
    End Sub
End Class
' Private implementation and private
' member with interface member name.
Class B
    Implements I
    Private Sub M_I() Implements I.M
    End Sub
    Private Sub M()
    End Sub
End Class
' Public implementation and public shared
' member with interface member name.
Class C
    Implements I
    Public Sub M_I() Implements I.M
    End Sub
    Public Shared Sub M()
    End Sub
End Class
Module M
    Sub M(Of T1 As A, T2 As {A, I}, T3 As {B, I}, T4 As {C, I})(_1 As T1, _2 As T2, _3 As T3, _4 As T4)
        _1.M()
        _2.M()
        _3.M()
        _4.M()
    End Sub
End Module
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30390: 'A.Protected Sub M()' is not accessible in this context because it is 'Protected'.
        _1.M()
        ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _4.M()
        ~~~~
</expected>)
        End Sub

        ' Class and interface constraints with member names
        ' that are ambiguous across interfaces.
        <Fact()>
        Public Sub MemberLookupClassAndDuplicateInterfaceConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Interface IA
    Sub M(o)
    Default ReadOnly Property P(o)
End Interface
Interface IB
    Sub M(o)
    Default ReadOnly Property Q(o)
End Interface
' Public implementation of both.
Class Both
    Implements IA
    Implements IB
    Public Sub M(o) Implements IA.M, IB.M
    End Sub
    Default Public ReadOnly Property P(o) Implements IA.P, IB.Q
        Get
            Return Nothing
        End Get
    End Property
End Class
' Public and private implementations.
Class One
    Implements IA
    Implements IB
    Public Sub M(o) Implements IA.M
    End Sub
    Default Public ReadOnly Property P(o) Implements IA.P
        Get
            Return Nothing
        End Get
    End Property
    Private Sub M_IB(o As Object) Implements IB.M
    End Sub
    Private ReadOnly Property P_IB(o As Object) As Object Implements IB.Q
        Get
            Return Nothing
        End Get
    End Property
End Class
' Private implementation of both.
Class Neither
    Implements IA
    Implements IB
    Private Sub M_IA(o As Object) Implements IA.M
    End Sub
    Private ReadOnly Property P_IA(o As Object) As Object Implements IA.P
        Get
            Return Nothing
        End Get
    End Property
    Private Sub M_IB(o As Object) Implements IB.M
    End Sub
    Private ReadOnly Property P_IB(o As Object) As Object Implements IB.Q
        Get
            Return Nothing
        End Get
    End Property
End Class
Module M
    Sub MBoth(Of T1 As {IA, IB}, T2 As {Both, IA, IB})(_1 As T1, _2 As T2, o As Object)
        _1.M(_1(o)) ' Both
        _2.M(_2(o)) ' Both
    End Sub
    Sub MOne(Of T1 As {IA, IB}, T2 As {One, IA, IB})(_1 As T1, _2 As T2, o As Object)
        _1.M(_1(o)) ' One
        _2.M(_2(o)) ' One
    End Sub
    Sub MNeither(Of T1 As {IA, IB}, T2 As {Neither, IA, IB})(_1 As T1, _2 As T2, o As Object)
        _1.M(_1(o)) ' Neither
        _2.M(_2(o)) ' Neither
    End Sub
End Module
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC30686: Default property access is ambiguous between the inherited interface members 'ReadOnly Default Property P(o As Object) As Object' of interface 'IA' and 'ReadOnly Default Property Q(o As Object) As Object' of interface 'IB'.
        _1.M(_1(o)) ' Both
             ~~
BC30686: Default property access is ambiguous between the inherited interface members 'ReadOnly Default Property P(o As Object) As Object' of interface 'IA' and 'ReadOnly Default Property Q(o As Object) As Object' of interface 'IB'.
        _1.M(_1(o)) ' One
             ~~
BC30686: Default property access is ambiguous between the inherited interface members 'ReadOnly Default Property P(o As Object) As Object' of interface 'IA' and 'ReadOnly Default Property Q(o As Object) As Object' of interface 'IB'.
        _1.M(_1(o)) ' Neither
             ~~
BC30686: Default property access is ambiguous between the inherited interface members 'ReadOnly Default Property P(o As Object) As Object' of interface 'IA' and 'ReadOnly Default Property Q(o As Object) As Object' of interface 'IB'.
        _2.M(_2(o)) ' Neither
             ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ExtensionMethodLookupWithMultipleTypeConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Class A
End Class
Class B
    Inherits A
End Class
Structure S
End Structure
Enum E
    A
End Enum
MustInherit Class C(Of T1, T2)
    Overridable Sub M(Of U1 As {T1, T2}, U2 As {T2, T1})(x As U1, y As U2)
        x.M_Object()
    End Sub
End Class
Class C0
    Inherits C(Of A, B)
    Public Overrides Sub M(Of U1 As {A, B}, U2 As {B, A})(x As U1, y As U2)
        x.M_A()
        x.M_B()
        y.M_A()
        y.M_B()
    End Sub
End Class
Class C1
    Inherits C(Of Integer, System.ValueType)
    Public Overrides Sub M(Of U1 As {Integer, System.ValueType}, U2 As {System.ValueType, Integer})(x As U1, y As U2)
        x.M_Integer()
        x.M_ValueType()
        y.M_Integer()
        y.M_ValueType()
    End Sub
End Class
Class C2
    Inherits C(Of System.ValueType, S)
    Public Overrides Sub M(Of U1 As {System.ValueType, S}, U2 As {S, System.ValueType})(x As U1, y As U2)
        x.M_ValueType()
        x.M_S()
        y.M_ValueType()
        y.M_S()
    End Sub
End Class
Class C3
    Inherits C(Of System.Enum, E)
    Public Overrides Sub M(Of U1 As {System.Enum, E}, U2 As {E, System.Enum})(x As U1, y As U2)
        x.M_Enum()
        x.M_E()
        y.M_Enum()
        y.M_E()
    End Sub
End Class
Class C4
    Inherits C(Of System.Array, A())
    Public Overrides Sub M(Of U1 As {System.Array, A()}, U2 As {A(), System.Array})(x As U1, y As U2)
        x.M_Array()
        x.M_A_Array()
        y.M_Array()
        y.M_A_Array()
    End Sub
End Class
Class C5(Of T As Structure)
    Inherits C(Of T, T)
    Public Overrides Sub M(Of U1 As {T}, U2 As {T})(x As U1, y As U2)
        y.M_Object()
        y.M_ValueType()
    End Sub
End Class
Module M
    <Extension()>
    Sub M_Object(o As Object)
    End Sub
    <Extension()>
    Sub M_A(o As A)
    End Sub
    <Extension()>
    Sub M_B(o As B)
    End Sub
    <Extension()>
    Sub M_S(o As S)
    End Sub
    <Extension()>
    Sub M_A_Array(o As A())
    End Sub
    <Extension()>
    Sub M_Integer(o As Integer)
    End Sub
    <Extension()>
    Sub M_E(o As E)
    End Sub
    <Extension()>
    Sub M_ValueType(o As System.ValueType)
    End Sub
    <Extension()>
    Sub M_Enum(o As System.Enum)
    End Sub
    <Extension()>
    Sub M_Array(o As System.Array)
    End Sub
End Module
]]></file>
</compilation>, {Net40.References.SystemCore})
            ' Note: Dev10 reports several errors, although it seems those are incorrect.
            ' BC30456: 'M_Object' is not a member of 'U1'.
            '         x.M_Object()
            '         ~~~~~~~~~~~~
            ' BC30456: 'M_A_Array' is not a member of 'U1'.
            '         x.M_A_Array()
            '         ~~~~~~~~~~~~~
            ' BC30456: 'M_A_Array' is not a member of 'U2'.
            '         y.M_A_Array()
            '         ~~~~~~~~~~~~~
            compilation.AssertNoErrors()
        End Sub

        <WorkItem(542978, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542978")>
        <Fact()>
        Public Sub ExtensionMethodWithConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Interface I
End Interface
Class A
End Class
Class C
    Shared Sub M(Of T1 As Class, T2 As Structure, T3 As New, T4 As I, T5 As A, T6 As {I, Structure}, T7 As {Class, New})(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6, _7 As T7)
        _1.M1()
        _1.M2()
        _1.M3()
        _1.M4()
        _1.M5()
        _1.M6()
        _1.M7()
        _2.M1()
        _2.M2()
        _2.M3()
        _2.M4()
        _2.M5()
        _2.M6()
        _2.M7()
        _3.M1()
        _3.M2()
        _3.M3()
        _3.M4()
        _3.M5()
        _3.M6()
        _3.M7()
        _4.M1()
        _4.M2()
        _4.M3()
        _4.M4()
        _4.M5()
        _4.M6()
        _4.M7()
        _5.M1()
        _5.M2()
        _5.M3()
        _5.M4()
        _5.M5()
        _5.M6()
        _5.M7()
        _6.M1()
        _6.M2()
        _6.M3()
        _6.M4()
        _6.M5()
        _6.M6()
        _6.M7()
        _7.M1()
        _7.M2()
        _7.M3()
        _7.M4()
        _7.M5()
        _7.M6()
        _7.M7()
    End Sub
End Class
Module E
    <Extension()>
    Sub M1(Of T As Class)(o As T)
    End Sub
    <Extension()>
    Sub M2(Of T As Structure)(o As T)
    End Sub
    <Extension()>
    Sub M3(Of T As New)(o As T)
    End Sub
    <Extension()>
    Sub M4(Of T As I)(o As T)
    End Sub
    <Extension()>
    Sub M5(Of T As A)(o As T)
    End Sub
    <Extension()>
    Sub M6(Of T As {I, Structure})(o As T)
    End Sub
    <Extension()>
    Sub M7(Of T As {Class, New})(o As T)
    End Sub
End Module
]]></file>
</compilation>, {Net40.References.SystemCore})
            compilation.AssertTheseDiagnostics(<expected>
BC30456: 'M2' is not a member of 'T1'.
        _1.M2()
        ~~~~~
BC30456: 'M3' is not a member of 'T1'.
        _1.M3()
        ~~~~~
BC30456: 'M4' is not a member of 'T1'.
        _1.M4()
        ~~~~~
BC30456: 'M5' is not a member of 'T1'.
        _1.M5()
        ~~~~~
BC30456: 'M6' is not a member of 'T1'.
        _1.M6()
        ~~~~~
BC30456: 'M7' is not a member of 'T1'.
        _1.M7()
        ~~~~~
BC30456: 'M1' is not a member of 'T2'.
        _2.M1()
        ~~~~~
BC30456: 'M4' is not a member of 'T2'.
        _2.M4()
        ~~~~~
BC30456: 'M5' is not a member of 'T2'.
        _2.M5()
        ~~~~~
BC30456: 'M6' is not a member of 'T2'.
        _2.M6()
        ~~~~~
BC30456: 'M7' is not a member of 'T2'.
        _2.M7()
        ~~~~~
BC30456: 'M1' is not a member of 'T3'.
        _3.M1()
        ~~~~~
BC30456: 'M2' is not a member of 'T3'.
        _3.M2()
        ~~~~~
BC30456: 'M4' is not a member of 'T3'.
        _3.M4()
        ~~~~~
BC30456: 'M5' is not a member of 'T3'.
        _3.M5()
        ~~~~~
BC30456: 'M6' is not a member of 'T3'.
        _3.M6()
        ~~~~~
BC30456: 'M7' is not a member of 'T3'.
        _3.M7()
        ~~~~~
BC30456: 'M1' is not a member of 'T4'.
        _4.M1()
        ~~~~~
BC30456: 'M2' is not a member of 'T4'.
        _4.M2()
        ~~~~~
BC30456: 'M3' is not a member of 'T4'.
        _4.M3()
        ~~~~~
BC30456: 'M5' is not a member of 'T4'.
        _4.M5()
        ~~~~~
BC30456: 'M6' is not a member of 'T4'.
        _4.M6()
        ~~~~~
BC30456: 'M7' is not a member of 'T4'.
        _4.M7()
        ~~~~~
BC30456: 'M2' is not a member of 'T5'.
        _5.M2()
        ~~~~~
BC30456: 'M3' is not a member of 'T5'.
        _5.M3()
        ~~~~~
BC30456: 'M4' is not a member of 'T5'.
        _5.M4()
        ~~~~~
BC30456: 'M6' is not a member of 'T5'.
        _5.M6()
        ~~~~~
BC30456: 'M7' is not a member of 'T5'.
        _5.M7()
        ~~~~~
BC30456: 'M1' is not a member of 'T6'.
        _6.M1()
        ~~~~~
BC30456: 'M5' is not a member of 'T6'.
        _6.M5()
        ~~~~~
BC30456: 'M7' is not a member of 'T6'.
        _6.M7()
        ~~~~~
BC30456: 'M2' is not a member of 'T7'.
        _7.M2()
        ~~~~~
BC30456: 'M4' is not a member of 'T7'.
        _7.M4()
        ~~~~~
BC30456: 'M5' is not a member of 'T7'.
        _7.M5()
        ~~~~~
BC30456: 'M6' is not a member of 'T7'.
        _7.M6()
        ~~~~~
</expected>)
        End Sub

        ' Reduced extension method with zero
        ' type parameters removed.
        <Fact()>
        Public Sub ExtensionMethodWithNoTypeParametersRemoved()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Interface I(Of T, U)
End Interface
Class A
End Class
Class C
    Sub M(x As I(Of Object, String), y As A)
        y.M(x)
    End Sub
End Class
Module E
    <Extension()>
    Sub M(Of T, U As T)(x As A, y As I(Of T, U))
    End Sub
End Module
]]></file>
</compilation>, {Net40.References.SystemCore})
            compilation.AssertNoErrors()
        End Sub

        ' Reduced extension method with second type
        ' parameter removed.
        <Fact()>
        Public Sub ExtensionMethodWithSecondTypeParameterRemoved()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Class C
    Shared Sub M(Of T As Class)(x As T, y As Object)
        x.M1(y)
        x.M2(y)
    End Sub
End Class
Module E
    <Extension()>
    Sub M1(Of T, U As Class)(x As U, y As T)
    End Sub
    <Extension()>
    Sub M2(Of T, U As Structure)(x As U, y As T)
    End Sub
End Module
]]></file>
</compilation>, {Net40.References.SystemCore})
            compilation.AssertTheseDiagnostics(<expected>
BC30456: 'M2' is not a member of 'T'.
        x.M2(y)
        ~~~~
</expected>)
        End Sub

        ' Reduced extension method with multiple
        ' type parameters removed.
        <Fact()>
        Public Sub ExtensionMethodWithMultipleTypeParametersRemoved()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Interface I(Of T, U)
End Interface
Class A
End Class
Class B
    Inherits A
End Class
Class C
    Sub M(x As I(Of A, B), y As I(Of B, A), z As String)
        x.M(z)
        y.M(z)
    End Sub
End Class
Module E
    <Extension()>
    Sub M(Of T1, T2, T3 As T1)(x As I(Of T1, T3), y As T2)
    End Sub
End Module
]]></file>
</compilation>, {Net40.References.SystemCore})
            compilation.AssertTheseDiagnostics(<expected>
BC30456: 'M' is not a member of 'I(Of B, A)'.
        y.M(z)
        ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ConversionsWithMultipleTypeConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict On
Class A
End Class
Class B
    Inherits A
End Class
MustInherit Class C(Of T, U)
    MustOverride Function F(Of V As {T, U})(o As V) As U
    MustOverride Function G(Of V As {T, U})(o As V) As T
End Class
Class C0
    Inherits C(Of A, B)
    Public Overrides Function F(Of V As {A, B})(o As V) As B
        Return o
    End Function
    Public Overrides Function G(Of V As {A, B})(o As V) As A
        Return o
    End Function
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        ' Lookup should handle InternalLookupOptions.MethodsOnly
        ' correctly to support lookup for query expressions.
        <Fact()>
        Public Sub QueryExpressionLookup()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Runtime.CompilerServices
Interface IA
End Interface
Interface IB
    Property [Select] As IB
    Property Where As IB
End Interface
Interface IC
    Function [Select](f As Func(Of Object, Object)) As IC
    Function Where(f As Func(Of Object, Boolean)) As IC
End Interface
Class A
End Class
Class B
    Public Property [Select] As B
    Public Property Where As B
End Class
Class C
    Public Function [Select](f As Func(Of Object, Object)) As C
        Return Me
    End Function
    Public Function Where(f As Func(Of Object, Boolean)) As C
        Return Me
    End Function
End Class
Module M
    Sub M(Of T1 As IA, T2 As A, T3 As {IA, A}, T4 As IB, T5 As B, T6 As {IB, B}, T7 As IC, T8 As C, T9 As {IC, C})(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6, _7 As T7, _8 As T8, _9 As T9)
        Dim result As Object
        result = From o In _1 Where o IsNot Nothing
        result = From o In _2 Where o IsNot Nothing
        result = From o In _3 Where o IsNot Nothing
        result = From o In _4 Where o IsNot Nothing
        result = From o In _5 Where o IsNot Nothing
        result = From o In _6 Where o IsNot Nothing
        result = From o In _7 Where o IsNot Nothing
        result = From o In _8 Where o IsNot Nothing
        result = From o In _9 Where o IsNot Nothing
    End Sub
    <Extension()>
    Public Function [Select](c As IA, f As Func(Of Object, Object)) As IA
        Return c
    End Function
    <Extension()>
    Public Function Where(c As IA, f As Func(Of Object, Boolean)) As IA
        Return c
    End Function
    <Extension()>
    Public Function [Select](c As B, f As Func(Of Object, Object)) As B
        Return c
    End Function
    <Extension()>
    Public Function Where(c As B, f As Func(Of Object, Boolean)) As B
        Return c
    End Function
End Module
]]>
    </file>
</compilation>, {Net40.References.SystemCore})
            compilation.AssertTheseDiagnostics(<expected>
BC36593: Expression of type 'T2' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        result = From o In _2 Where o IsNot Nothing
                           ~~
BC36593: Expression of type 'T4' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        result = From o In _4 Where o IsNot Nothing
                           ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub OverloadResolutionError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict On
Class C
    Sub M(Of T As Class, U)(o As T, f As System.Func(Of U, Object))
        M(1, Function(x) x)
    End Sub
End Class
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC32106: Type argument 'Integer' does not satisfy the 'Class' constraint for type parameter 'T'.
        M(1, Function(x) x)
        ~
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
        M(1, Function(x) x)
                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub DelegateConstraintErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices
Delegate Function D1() As Object
Delegate Sub D2()
Class A
    Friend Function F1(Of T As Structure)() As Object
        Return Nothing
    End Function
    Friend Sub M1(Of T, U As {T, Class})()
    End Sub
End Class
Class C(Of T)
    Shared Sub M(arg As A)
        Dim _1 As D1
        Dim _2 As D2
        _1 = AddressOf arg.F1(Of T)
        _2 = AddressOf arg.M1(Of T, T)
        _1 = AddressOf arg.F2(Of T)
        _2 = AddressOf arg.M2(Of T, T)
    End Sub
End Class
Module E
    <Extension()>
    Friend Function F2(Of T As Structure)(arg As A) As Object
        Return Nothing
    End Function
    <Extension()>
    Friend Sub M2(Of T, U As {T, Class})(arg As A)
    End Sub
End Module
]]></file>
</compilation>, {Net40.References.SystemCore})
            compilation.AssertTheseDiagnostics(<expected>
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
        _1 = AddressOf arg.F1(Of T)
                       ~~~~~~~~~~~~
BC32106: Type argument 'T' does not satisfy the 'Class' constraint for type parameter 'U'.
        _2 = AddressOf arg.M1(Of T, T)
                       ~~~~~~~~~~~~~~~
BC32105: Type argument 'T' does not satisfy the 'Structure' constraint for type parameter 'T'.
        _1 = AddressOf arg.F2(Of T)
                       ~~~~~~~~~~~~
BC32106: Type argument 'T' does not satisfy the 'Class' constraint for type parameter 'U'.
        _2 = AddressOf arg.M2(Of T, T)
                       ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ConstraintCombinations()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface IA
End Interface
Interface IB
End Interface
Class A
End Class
Class B
End Class
Class A1(Of T As New, U As {T, New})
End Class
Class A2(Of T As New, U As {T, Structure})
End Class
Class A3(Of T As New, U As {T, Class})
End Class
Class A4(Of T As New, U As {T, A})
End Class
Class A5(Of T As New, U As {T, IA})
End Class
Class B1(Of T As Structure, U As {T, New})
End Class
Class B2(Of T As Structure, U As {T, Structure})
End Class
Class B3(Of T As Structure, U As {T, Class})
End Class
Class B4(Of T As Structure, U As {T, A})
End Class
Class B5(Of T As Structure, U As {T, IA})
End Class
Class C1(Of T As Class, U As {New, T})
End Class
Class C2(Of T As Class, U As {Structure, T})
End Class
Class C3(Of T As Class, U As {Class, T})
End Class
Class C4(Of T As Class, U As {A, T})
End Class
Class C5(Of T As Class, U As {IA, T})
End Class
Class D1(Of T As B, U As {New, T})
End Class
Class D2(Of T As B, U As {Structure, T})
End Class
Class D3(Of T As B, U As {Class, T})
End Class
Class D4(Of T As B, U As {A, T})
End Class
Class D5(Of T As B, U As {IA, T})
End Class
Class E1(Of T As IB, U As {New, T})
End Class
Class E2(Of T As IB, U As {Structure, T})
End Class
Class E3(Of T As IB, U As {Class, T})
End Class
Class E4(Of T As IB, U As {A, T})
End Class
Class E5(Of T As IB, U As {IA, T})
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC32114: Type parameter with a 'Structure' constraint cannot be used as a constraint.
Class B1(Of T As Structure, U As {T, New})
                                  ~
BC32114: Type parameter with a 'Structure' constraint cannot be used as a constraint.
Class B2(Of T As Structure, U As {T, Structure})
                                  ~
BC32114: Type parameter with a 'Structure' constraint cannot be used as a constraint.
Class B3(Of T As Structure, U As {T, Class})
                                  ~
BC32114: Type parameter with a 'Structure' constraint cannot be used as a constraint.
Class B4(Of T As Structure, U As {T, A})
                                  ~
BC32114: Type parameter with a 'Structure' constraint cannot be used as a constraint.
Class B5(Of T As Structure, U As {T, IA})
                                  ~
BC32110: Constraint 'Structure' conflicts with the indirect constraint 'Class B' obtained from the type parameter constraint 'T'.
Class D2(Of T As B, U As {Structure, T})
                          ~~~~~~~~~
BC32110: Constraint 'Class A' conflicts with the indirect constraint 'Class B' obtained from the type parameter constraint 'T'.
Class D4(Of T As B, U As {A, T})
                          ~
</expected>)
        End Sub

        ' Avoid reporting direct constraints on overriding methods
        ' and private interface implementations.
        <Fact()>
        Public Sub SkipDirectConstraintConflictsForImplementsAndOverrides()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class A
End Class
Class B
    Inherits A
End Class
Interface I(Of T, U)
    Sub M(Of V As {T, U})()
End Interface
Class C(Of T, U)
    Overridable Sub M(Of V As {T, U})()
    End Sub
End Class
' Overrides C.M
Class D1
    Inherits C(Of A, B)
    Public Overrides Sub M(Of V As {A, B})() ' D1
    End Sub
End Class
' Overloads but does not override C.M
Class D2
    Inherits C(Of A, B)
    Public Overloads Sub M(Of V As {A, B})() ' D2
    End Sub
End Class
' Private implementation of I.M
Class D3
    Implements I(Of A, B)
    Private Sub M(Of V As {A, B})() Implements I(Of A, B).M ' D3
    End Sub
End Class
' Public implementation of I.M
Class D4
    Implements I(Of A, B)
    Friend Sub M(Of V As {A, B})() Implements I(Of A, B).M ' D4
    End Sub
End Class
' Overrides C.M and public implementation of I.M
Class D5
    Inherits C(Of B, A)
    Implements I(Of B, A)
    Public Overrides Sub M(Of V As {B, A})() Implements I(Of B, A).M ' D5
    End Sub
End Class
        </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC32047: Type parameter 'V' can only have one constraint that is a class.
    Public Overloads Sub M(Of V As {A, B})() ' D2
                                       ~
BC32047: Type parameter 'V' can only have one constraint that is a class.
    Friend Sub M(Of V As {A, B})() Implements I(Of A, B).M ' D4
                             ~
BC32078: 'Friend Sub M(Of V)()' cannot implement 'I(Of A, B).Sub M(Of V)()' because they differ by type parameter constraints.
    Friend Sub M(Of V As {A, B})() Implements I(Of A, B).M ' D4
                                              ~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(528855, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528855")>
        <Fact()>
        Public Sub ModReqsInConstraintsAreNotSupported()
            Dim ilSource = <![CDATA[
.class public A
{
}
.class interface public abstract I
{
    .method public abstract virtual instance void M<(class A modreq(int32))T>() { }
}
.class interface public abstract IT<(class A modreq(int32))T>
{
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C1
    Implements I
    Sub M(Of T)() Implements I.M
    End Sub
End Class
Class C2
    Implements IT(Of A)
End Class
Class C(Of T)
    Implements IT(Of T)
    Sub M(Of U As T)()
    End Sub
End Class
Class C3
    Implements I
    Sub M(Of T As A)() Implements I.M
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim comp = CreateCompilationWithCustomILSource(vbSource, ilSource)
            comp.AssertTheseDiagnostics(<expected>
BC32078: 'Public Sub M(Of T)()' cannot implement 'I.Sub M(Of T As ?)()' because they differ by type parameter constraints.
    Sub M(Of T)() Implements I.M
                             ~~~
BC30649: '' is an unsupported type.
Class C2
      ~~
BC32044: Type argument 'A' does not inherit from or implement the constraint type '?'.
Class C2
      ~~
BC30649: '' is an unsupported type.
Class C(Of T)
      ~
BC32044: Type argument 'T' does not inherit from or implement the constraint type '?'.
Class C(Of T)
      ~
BC32078: 'Public Sub M(Of T As A)()' cannot implement 'I.Sub M(Of T As ?)()' because they differ by type parameter constraints.
    Sub M(Of T As A)() Implements I.M
                                  ~~~
                                        </expected>)
        End Sub

        ''' <summary>
        ''' Constraints with modopts are treated as unsupported types.
        ''' (The native compiler imports constraints with modopts but
        ''' generates invalid types when implementing or overriding
        ''' generic methods with such constraints.)
        ''' </summary>
        <WorkItem(528856, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528856")>
        <Fact()>
        Public Sub ModOptsInConstraintsAreIgnored()
            Dim ilSource = <![CDATA[
.class public A
{
    .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
    .method public virtual instance void M<(class A modopt(A) modopt(int32))T>() { ret }
}
.class interface public abstract I<(class A modopt(A))T>
{
    .method public abstract virtual instance void M<(!T modopt(int32))U>() { }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class B
    Inherits A
    Public Overrides Sub M(Of T As A)()
    End Sub
End Class
Class C
    Implements I(Of A)
    Private Sub M(Of U As A)() Implements I(Of A).M
    End Sub
End Class
Module M
    Sub Main()
        Call New A().M(Of A)()
        Call New B().M(Of A)()
        DirectCast(New C(), I(Of A)).M(Of A)()
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim comp = CreateCompilationWithCustomILSource(vbSource, ilSource, includeVbRuntime:=True)
            comp.AssertTheseDiagnostics(<expected>
BC32077: 'Public Overrides Sub M(Of T)()' cannot override 'Public Overrides Sub M(Of T)()' because they differ by type parameter constraints.
    Public Overrides Sub M(Of T As A)()
                         ~
BC30649: '' is an unsupported type.
Class C
      ~
BC32044: Type argument 'A' does not inherit from or implement the constraint type '?'.
Class C
      ~
BC32078: 'Private Sub M(Of U)()' cannot implement 'I(Of A).Sub M(Of U)()' because they differ by type parameter constraints.
    Private Sub M(Of U As A)() Implements I(Of A).M
                                          ~~~~~~~~~
BC30649: '' is an unsupported type.
    Private Sub M(Of U As A)() Implements I(Of A).M
                                               ~
BC32044: Type argument 'A' does not inherit from or implement the constraint type '?'.
    Private Sub M(Of U As A)() Implements I(Of A).M
                                               ~
BC30649: '' is an unsupported type.
        Call New A().M(Of A)()
             ~~~~~~~~~~~~~~~~~
BC30649: '' is an unsupported type.
        DirectCast(New C(), I(Of A)).M(Of A)()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30649: '' is an unsupported type.
        DirectCast(New C(), I(Of A)).M(Of A)()
                                 ~
BC32044: Type argument 'A' does not inherit from or implement the constraint type '?'.
        DirectCast(New C(), I(Of A)).M(Of A)()
                                 ~
</expected>)
        End Sub

        ''' <summary>
        ''' Constraints on the nested type must match
        ''' constraints from the containing types.
        ''' Note: Dev11 checks constraint flags only.
        ''' </summary>
        <WorkItem(528859, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528859")>
        <Fact()>
        Public Sub InconsistentConstraintsAreNotSupported()
            Dim ilSource = <![CDATA[
.class public A
{
  .method public specialname rtspecialname instance void .ctor() { ret }
}
.class interface abstract public I { }
.class interface abstract public IT<T>
{
  .class interface abstract nested public IU<U> { }
  .class interface abstract nested public ITU<T, U> { }
  .class interface abstract nested public ITU2<(!U)T, U> { }
  .class interface abstract nested public IT<T>
  {
    .class interface abstract nested public IAI<(A, I)T> { }
  }
  .class interface abstract nested public IAI<(A, I)T>
  {
    .class interface abstract nested public IT<T> { }
    .class interface abstract nested public IAI<(A, I)T> { }
  }
  .class interface abstract nested public IF<class .ctor T> { }
  .class interface abstract nested public IIn<-T> { }
}
.class interface abstract public ITU<T, (!T)U>
{
  .class interface abstract nested public ITU<T, (!T)U> { }
  .class interface abstract nested public ITU2<(!U)T, U> { }
}
.class abstract interface public IAI<(A, I)T>
{
  .class interface abstract nested public IT<T>
  {
    .class interface abstract nested public IAI<(A, I)T> { }
  }
  .class nested public CIA<(I, A)T>
  {
    .method public specialname rtspecialname instance void .ctor() { ret }
  }
}
.class public CF<class .ctor T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .class nested public CT<T>
  {
    .method public specialname rtspecialname instance void .ctor() { ret }
  }
  .class interface abstract nested public IF<.ctor class T> { }
}
.class interface abstract public IIn<-T>
{
  .class interface abstract nested public IT<T> { }
  .class interface abstract nested public IInU<-U> { }
  .class interface abstract nested public IOut<+T> { }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C
    Inherits A
    Implements I
End Class
Class CIT
    Implements IT(Of Object)
End Class
Class CIT_IU
    Implements IT(Of Object).IU
End Class
Class CIT_ITU
    Implements IT(Of Object).ITU(Of Integer)
End Class
Class CIT_ITU2
    Implements IT(Of Object).ITU2(Of Object) ' BC36739 (not reported by Dev11)
End Class
Class CIT_IT_IAI
    Implements IT(Of Object).IT.IAI ' BC36739 (not reported by Dev11)
End Class
Class CIT_IAI
    Implements IT(Of Object).IAI ' BC36739 (not reported by Dev11)
End Class
Class CIT_IAI_IT
    Implements IT(Of Object).IAI.IT
End Class
Class CIT_IAI_IAI
    Implements IT(Of Object).IAI.IAI ' BC36739 (not reported by Dev11)
End Class
Class CITU_ITU
    Implements ITU(Of Object, Object).ITU
End Class
Class CITU_ITU2
    Implements ITU(Of Object, Object).ITU2 ' BC36739 (not reported by Dev11)
End Class
Class CIT_IF
    Implements IT(Of Object).IF ' BC36739
End Class
Class CIT_IIn
    Implements IT(Of Object).IIn ' BC36739
End Class
Class CIAI
    Implements IAI(Of C)
End Class
Class CIAI_IT
    Implements IAI(Of C).IT ' BC36739 (not reported by Dev11)
End Class
Class CIAI_IT_IAI
    Implements IAI(Of C).IT.IAI
End Class
Class CIAI_CIA
    Inherits IAI(Of C).CIA
End Class
Class CCF
    Inherits CF(Of C)
End Class
Class CCF_CT
    Inherits CF(Of C).CT ' BC36739
End Class
Class CCF_IF
    Implements CF(Of C).IF
End Class
Class CIIn
    Implements IIn(Of Object)
End Class
Class CIIn_IT
    Implements IIn(Of Object).IT ' BC36739
End Class
Class CIIn_IInU
    Implements IIn(Of Object).IInU
End Class
Class CIIn_IOut
    Implements IIn(Of Object).IOut ' BC36739
End Class
]]>
                    </file>
                </compilation>
            Dim comp = CreateCompilationWithCustomILSource(vbSource, ilSource, includeVbRuntime:=True)
            comp.AssertTheseDiagnostics(<expected>
BC36739: Type 'IT(Of T).ITU2(Of U)' does not inherit the generic type parameters of its container.
    Implements IT(Of Object).ITU2(Of Object) ' BC36739 (not reported by Dev11)
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36739: Type 'IT(Of T).IT.IAI' does not inherit the generic type parameters of its container.
    Implements IT(Of Object).IT.IAI ' BC36739 (not reported by Dev11)
               ~~~~~~~~~~~~~~~~~~~~
BC36739: Type 'IT(Of T).IAI' does not inherit the generic type parameters of its container.
    Implements IT(Of Object).IAI ' BC36739 (not reported by Dev11)
               ~~~~~~~~~~~~~~~~~
BC36739: Type 'IT(Of T).IAI.IAI' does not inherit the generic type parameters of its container.
    Implements IT(Of Object).IAI.IAI ' BC36739 (not reported by Dev11)
               ~~~~~~~~~~~~~~~~~~~~~
BC36739: Type 'ITU(Of T, U).ITU2' does not inherit the generic type parameters of its container.
    Implements ITU(Of Object, Object).ITU2 ' BC36739 (not reported by Dev11)
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36739: Type 'IT(Of T).IF' does not inherit the generic type parameters of its container.
    Implements IT(Of Object).IF ' BC36739
               ~~~~~~~~~~~~~~~~
BC36739: Type 'IT(Of T).IIn' does not inherit the generic type parameters of its container.
    Implements IT(Of Object).IIn ' BC36739
               ~~~~~~~~~~~~~~~~~
BC36739: Type 'IAI(Of T).IT' does not inherit the generic type parameters of its container.
    Implements IAI(Of C).IT ' BC36739 (not reported by Dev11)
               ~~~~~~~~~~~~
BC36739: Type 'CF(Of T).CT' does not inherit the generic type parameters of its container.
    Inherits CF(Of C).CT ' BC36739
             ~~~~~~~~~~~
BC36739: Type 'IIn(Of T).IT' does not inherit the generic type parameters of its container.
    Implements IIn(Of Object).IT ' BC36739
               ~~~~~~~~~~~~~~~~~
BC36739: Type 'IIn(Of T).IOut' does not inherit the generic type parameters of its container.
    Implements IIn(Of Object).IOut ' BC36739
               ~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ''' <summary>
        ''' Behavior differs from C#. VB does not check constraints
        ''' along the inheritance hierarchy.
        ''' </summary>
        <WorkItem(528861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528861")>
        <Fact()>
        Public Sub ConstraintsAreCheckedAlongHierarchy()
            Dim ilSource = <![CDATA[
.class interface public abstract I
{
}
.class public abstract A
{
    .method public specialname rtspecialname instance void .ctor() { ret }
}
.class interface public abstract IA1_1<valuetype T> { }
.class interface public abstract IA1_2<(I)T> { }
.class interface public abstract IA1_3<T, (!T)U> { }
.class interface public abstract IB1<T, U, V>
    implements class IA1_1<!T>, class IA1_2<!U>, class IA1_3<!U, !V>
{
}
.class interface public abstract IA2_1<valuetype T> { }
.class interface public abstract IA2_2<(I)T> { }
.class interface public abstract IA2_3<T, (!T)U> { }
.class interface public abstract IB2<T, U, V>
    implements class IA2_1<!T>, class IA2_2<!U>, class IA2_3<!U, !V>
{
}
.class interface public abstract IA3_1<valuetype T> { }
.class interface public abstract IA3_2<(I)T> { }
.class interface public abstract IA3_3<T, (!T)U> { }
.class interface public abstract IB3_1<T>
    implements class IA3_1<!T>
{
}
.class public abstract B3<T, U, V>
    implements class IA3_1<!T>, class IA3_2<!U>, class IA3_3<!U, !V>
{
    .method public specialname rtspecialname instance void .ctor() { ret }
}
.class public abstract A4<valuetype T, (I)U, (!U)V>
{
    .method public specialname rtspecialname instance void .ctor() { ret }
}
.class public abstract B4<T, U, V>
    extends class A4<!T, !U, !V>
{
    .method public specialname rtspecialname instance void .ctor() { ret }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface IC1
    Inherits IB1(Of I, A, Object)
End Interface
Interface IC2(Of T, U, V)
    Inherits IB2(Of T, U, V)
End Interface
Interface IC3_1
    Inherits IB3_1(Of Object)
End Interface
Class C2
    Implements IC2(Of I, A, Object)
End Class
Class C3
    Inherits B3(Of I, A, Object)
    Implements IC3_1
End Class
Class C4
    Inherits B4(Of I, A, Object)
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            compilation.AssertTheseDiagnostics(<expected/>)
            ' Verify there are no diagnostics generated when emitting metadata.
            Using stream = New MemoryStream()
                Dim result = compilation.Emit(stream)
                Assert.True(result.Success)
                Assert.True(result.Diagnostics.IsEmpty)
            End Using
        End Sub

        ' Interface implementations of generic methods that have both 'Structure'
        ' and System.ValueType constraints must include both constraints
        ' explicitly. This is different from C# (see corresponding C# test).
        <Fact()>
        Public Sub InterfaceConstraintsAbsorbed()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Imports System
Interface I(Of T)
    Sub M(Of U As {Structure, T})()
End Interface
Class C1
    Implements I(Of ValueType)
    Private Sub C1_M(Of U As {Structure})() Implements I(Of ValueType).M
    End Sub
End Class
Class C2
    Implements I(Of ValueType)
    Private Sub C2_M(Of U As {ValueType})() Implements I(Of ValueType).M
    End Sub
End Class
Class C3
    Implements I(Of ValueType)
    Private Sub C3_M(Of U As {Structure, ValueType})() Implements I(Of ValueType).M
    End Sub
End Class
        </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC32078: 'Private Sub C1_M(Of U)()' cannot implement 'I(Of ValueType).Sub M(Of U)()' because they differ by type parameter constraints.
    Private Sub C1_M(Of U As {Structure})() Implements I(Of ValueType).M
                                                       ~~~~~~~~~~~~~~~~~
BC32078: 'Private Sub C2_M(Of U)()' cannot implement 'I(Of ValueType).Sub M(Of U)()' because they differ by type parameter constraints.
    Private Sub C2_M(Of U As {ValueType})() Implements I(Of ValueType).M
                                                       ~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ' Use-site errors should be reported when a type or
        ' method from PE with a circular constraint is used.
        <Fact()>
        Public Sub UseSiteErrorCircularConstraints()
            Dim ilSource = <![CDATA[
.class public A<(!T)T> { }
.class public B
{
    .method public static void M<(!!U)T, (!!T)U>() { ret }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C
    Shared Sub M(arg As A(Of Object))
    End Sub
    Shared Sub M()
        B.M(Of String, String)()
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            compilation.AssertTheseDiagnostics(
<expected>
BC32113: Type parameter 'T' cannot be constrained to itself: 
    'T' is constrained to 'T'.
    Shared Sub M(arg As A(Of Object))
                 ~~~
BC32113: Type parameter 'T' cannot be constrained to itself: 
    'T' is constrained to 'U'.
    'U' is constrained to 'T'.
        B.M(Of String, String)()
        ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ' Use-site errors should not be reported for a type or
        ' method from PE with a missing constraint type in
        ' addition to any conversion error satisfying constraints.
        <Fact()>
        Public Sub UseSiteErrorMissingConstraintType()
            Dim ilSource = <![CDATA[
.assembly extern other {}
.class public A<([other]C)T> { }
.class public B
{
    .method public static void M<([other]C)U>() { ret }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class D
    Sub M(o As A(Of Object))
    End Sub
    Sub M()
        B.M(Of String)()
    End Sub
End Class
]]>
                    </file>
                </compilation>
            ' Note: for method overload resolution, methods with use-site errors
            ' are ignored so there is no constraint error for B.M(Of String)().
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            compilation.AssertTheseDiagnostics(
<expected>
BC30652: Reference required to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C'. Add one to your project.
    Sub M(o As A(Of Object))
          ~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'C'.
    Sub M(o As A(Of Object))
          ~
BC30652: Reference required to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'C'. Add one to your project.
        B.M(Of String)()
        ~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub UseSiteErrorMissingConstraintTypeOverriddenMethod()
            Dim vbSource1 =
                <compilation name="d521fe98-188c-45cf-0788-249e00d004ea">
                    <file name="c.vb"><![CDATA[
Public Interface IA
End Interface
Public Class A
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40(vbSource1)
            compilation1.AssertTheseDiagnostics(<errors/>)
            Dim vbSource2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Interface IB
    Inherits IA
End Interface
Public Class B
    Inherits A
End Class
Public Interface IB(Of T)
End Interface
Public Class B(Of T)
End Class
Public Interface IB1
    Sub M1(Of T As IB, U As B)()
End Interface
Public Interface IB2
    Sub M2(Of T As B(Of IB), U As IB(Of B()))()
End Interface
Public Interface IB3(Of T, U)
    Sub M3(Of V As T, W As U)()
End Interface
Public Interface IB4
    Inherits IB3(Of B, IB)
End Interface
Public MustInherit Class B1
    Public MustOverride Sub M1(Of T As IB, U As B)()
End Class
Public MustInherit Class B2
    Public MustOverride Sub M2(Of T As B(Of IB), U As IB(Of B()))()
End Class
Public MustInherit Class B3(Of T, U)
    Public MustOverride Sub M3(Of V As T, W As U)()
End Class
Public MustInherit Class B4
    Inherits B3(Of B, IB)
End Class
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(vbSource2, {MetadataReference.CreateFromImage(compilation1.EmitToArray())})
            compilation2.AssertTheseDiagnostics(<errors/>)
            Dim vbSource3 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C1A
    Implements IB1
    Private Sub M1(Of T1A As IB, U1A As B)() Implements IB1.M1
    End Sub
End Class
Class C2A
    Implements IB2
    Private Sub M2(Of T2A As B(Of IB), U2A As IB(Of B()))() Implements IB2.M2
    End Sub
End Class
Class C4A
    Implements IB4
    Private Sub M3(Of T4A As B, U4A As IB)() Implements IB3(Of B, IB).M3
    End Sub
End Class
Class C1B
    Inherits B1
    Public Overrides Sub M1(Of T1B As IB, U1B As B)()
    End Sub
End Class
Class C2B
    Inherits B2
    Public Overrides Sub M2(Of T2B As B(Of IB), U2B As IB(Of B()))()
    End Sub
End Class
Class C4B
    Inherits B4
    Public Overrides Sub M3(Of T4B As B, U4B As IB)()
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation3 = CreateCompilationWithMscorlib40AndReferences(vbSource3, {MetadataReference.CreateFromImage(compilation2.EmitToArray())})
            compilation3.AssertTheseDiagnostics(<errors/>)
        End Sub

        ' If a type parameter from metadata has multiple errors
        ' including a missing constraint type (a use-site error on
        ' the constraint type), the missing constraint type should
        ' be reported as the use-site error for the type parameter.
        <Fact()>
        Public Sub UseSiteErrorMissingConstraintTypeAndCircularConstraint()
            Dim ilSource = <![CDATA[
.assembly extern other {}
.class public A1<([other]B1, !T)T> { }
.class public A2<([other]B2, [other]I)T> { }
.class public A3
{
  .method static public hidebysig void M<([other]B3, !!T)T>() { ret }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C
    Sub M(a As A1(Of Object))
    End Sub
    Sub M(a As A2(Of Object))
    End Sub
    Sub M()
        A3.M(Of Object)()
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            compilation.AssertTheseDiagnostics(
<expected>
BC30652: Reference required to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'B1'. Add one to your project.
    Sub M(a As A1(Of Object))
          ~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'B1'.
    Sub M(a As A1(Of Object))
          ~
BC32113: Type parameter 'T' cannot be constrained to itself: 
    'T' is constrained to 'T'.
    Sub M(a As A1(Of Object))
          ~
BC30652: Reference required to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'B2'. Add one to your project.
    Sub M(a As A2(Of Object))
          ~
BC30652: Reference required to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I'. Add one to your project.
    Sub M(a As A2(Of Object))
          ~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'B2'.
    Sub M(a As A2(Of Object))
          ~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'I'.
    Sub M(a As A2(Of Object))
          ~
BC30652: Reference required to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'B3'. Add one to your project.
        A3.M(Of Object)()
        ~~~~~~~~~~~~~~~~~
BC32113: Type parameter 'T' cannot be constrained to itself: 
    'T' is constrained to 'T'.
        A3.M(Of Object)()
        ~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ' Same as UseSiteErrorMissingConstraintTypeAndCircularConstraint but
        ' with use-site errors from retargeting symbols rather than PE symbols.
        <Fact()>
        Public Sub RetargetingUseSiteErrorMissingConstraintTypeAndCircularConstraint()
            Dim vbSource1 =
                <compilation name="2a9bcbd6-baa6-4ed3-ab61-f9f404735875">
                    <file name="c.vb"><![CDATA[
Public Class B1
End Class
Public Class B2
End Class
Public Class B3
End Class
Public Interface I
End Interface
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40(vbSource1)
            compilation1.AssertNoErrors()
            Dim vbSource2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class A1(Of T As {B1, T})
End Class
Public Class A2(Of T As {B2, I})
End Class
Public Class A3
    Public Shared Sub M(Of T As {B3, T})()
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(vbSource2, {New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertTheseDiagnostics(
<expected>
BC32113: Type parameter 'T' cannot be constrained to itself: 
    'T' is constrained to 'T'.
Public Class A1(Of T As {B1, T})
                             ~
BC32113: Type parameter 'T' cannot be constrained to itself: 
    'T' is constrained to 'T'.
    Public Shared Sub M(Of T As {B3, T})()
                                     ~
</expected>)
            Dim vbSource3 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C
    Sub M(a As A1(Of Object))
    End Sub
    Sub M(a As A2(Of Object))
    End Sub
    Sub M()
        A3.M(Of Object)()
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation3 = CreateCompilationWithMscorlib40AndReferences(vbSource3, {New VisualBasicCompilationReference(compilation2)})
            compilation3.AssertTheseDiagnostics(
<expected>
BC30652: Reference required to assembly '2a9bcbd6-baa6-4ed3-ab61-f9f404735875, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'B1'. Add one to your project.
    Sub M(a As A1(Of Object))
          ~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'B1'.
    Sub M(a As A1(Of Object))
          ~
BC30652: Reference required to assembly '2a9bcbd6-baa6-4ed3-ab61-f9f404735875, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'B2'. Add one to your project.
    Sub M(a As A2(Of Object))
          ~
BC30652: Reference required to assembly '2a9bcbd6-baa6-4ed3-ab61-f9f404735875, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'I'. Add one to your project.
    Sub M(a As A2(Of Object))
          ~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'B2'.
    Sub M(a As A2(Of Object))
          ~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'I'.
    Sub M(a As A2(Of Object))
          ~
BC30652: Reference required to assembly '2a9bcbd6-baa6-4ed3-ab61-f9f404735875, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'B3'. Add one to your project.
        A3.M(Of Object)()
        ~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ' Use-site errors should not be reported for
        ' redundant constraints from PE type parameters.
        <Fact()>
        Public Sub NoUseSiteErrorRedundantConstraints()
            Dim ilSource = <![CDATA[
.class public A { }
.class public B { }
.class public C<class (A) T>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
}
.class public D<(A, A) T, (A, B) U>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
}
.class public E
{
    .method public static void M<class (A)T, (A, B)U>() { ret }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class M
    Shared Sub M()
        Dim o
        o = New C(Of A)()
        o = New D(Of A, A)()
        E.M(Of A, B)()
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            compilation.AssertTheseDiagnostics(
<expected>
BC32044: Type argument 'A' does not inherit from or implement the constraint type 'B'.
        o = New D(Of A, A)()
                        ~
BC32044: Type argument 'B' does not inherit from or implement the constraint type 'A'.
        E.M(Of A, B)()
          ~~~~~~~~~~
</expected>)
        End Sub

        ' Redundant constraints should not result in duplicate errors
        ' for type arguments that do not satisfy the constraints.
        <Fact()>
        Public Sub NotSatisfyingRedundantConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class B
    Dim F = New C(Of A, B)()
End Class
Class C(Of T, U As {T, T, I, I})
End Class
        </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC32044: Type argument 'B' does not inherit from or implement the constraint type 'A'.
    Dim F = New C(Of A, B)()
                        ~
BC32044: Type argument 'B' does not inherit from or implement the constraint type 'I'.
    Dim F = New C(Of A, B)()
                        ~
BC32071: Constraint type 'T' already specified for this type parameter.
Class C(Of T, U As {T, T, I, I})
                       ~
BC32071: Constraint type 'I' already specified for this type parameter.
Class C(Of T, U As {T, T, I, I})
                             ~
</expected>)
        End Sub

        ' Indirect constraint conflict from metadata should
        ' result in an error when referenced in source, but unlike
        ' direct constraint conflicts, this is not a use-site error.
        <Fact()>
        Public Sub IndirectConstraintConflictFromMetadata()
            Dim ilSource = <![CDATA[
.class public A { }
.class public B { }
.class public C<(A)T, (!T, B)U> { }
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class D
    Sub M(o As C(Of A, B))
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            compilation.AssertTheseDiagnostics(
<expected>
BC32044: Type argument 'B' does not inherit from or implement the constraint type 'A'.
    Sub M(o As C(Of A, B))
          ~
</expected>)
        End Sub

        <WorkItem(543348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543348")>
        <Fact()>
        Public Sub DuplicateConstraintTypes()
            Dim sources = <compilation>
                              <file name="c.vb">
Interface I(Of T)
End Interface
Interface I1(Of T, U)
    Sub M(Of V As {T, U})()
End Interface
Interface I2(Of T, U)
    Sub M(Of V As {T, I(Of U), I(Of Object)})()
End Interface
Interface I3(Of T, U)
    Sub M(Of V As {I(Of T), I(Of U)})()
End Interface
Interface I4(Of T)
    Inherits I1(Of T, T)
End Interface
Interface I5(Of T)
    Inherits I2(Of I(Of Object), T)
End Interface
Interface I6(Of U)
    Inherits I3(Of I(Of U), I(Of U))
End Interface
    </file>
                          </compilation>
            Dim validator = Sub([module] As ModuleSymbol)
                                Dim [namespace] = [module].GlobalNamespace

                                Dim method = [namespace].GetMember(Of NamedTypeSymbol)("I1").GetMember(Of MethodSymbol)("M")
                                CheckConstraints(method.TypeParameters(0), TypeParameterConstraintKind.None, "T", "U")

                                method = [namespace].GetMember(Of NamedTypeSymbol)("I2").GetMember(Of MethodSymbol)("M")
                                CheckConstraints(method.TypeParameters(0), TypeParameterConstraintKind.None, "T", "I(Of U)", "I(Of Object)")

                                method = [namespace].GetMember(Of NamedTypeSymbol)("I3").GetMember(Of MethodSymbol)("M")
                                CheckConstraints(method.TypeParameters(0), TypeParameterConstraintKind.None, "I(Of T)", "I(Of U)")

                                method = [namespace].GetMember(Of NamedTypeSymbol)("I4").Interfaces(0).GetMember(Of MethodSymbol)("M")
                                CheckConstraints(method.TypeParameters(0), TypeParameterConstraintKind.None, "T")

                                method = [namespace].GetMember(Of NamedTypeSymbol)("I5").Interfaces(0).GetMember(Of MethodSymbol)("M")
                                CheckConstraints(method.TypeParameters(0), TypeParameterConstraintKind.None, "I(Of Object)", "I(Of T)")

                                method = [namespace].GetMember(Of NamedTypeSymbol)("I6").Interfaces(0).GetMember(Of MethodSymbol)("M")
                                CheckConstraints(method.TypeParameters(0), TypeParameterConstraintKind.None, "I(Of I(Of U))")
                            End Sub
            CompileAndVerify(sources, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <WorkItem(543348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543348")>
        <Fact()>
        Public Sub DuplicateConstraintTypesMetadata()
            Dim ilSource = <![CDATA[
.class public A { }
.class public B { }
.class public C<T, (!T, !T)U, (A, B, A)V> { }
.class public D<T>
{
    .method public static void M<(!T, !T)U, (B, A, A)V>() { ret }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            Dim [namespace] = compilation.GlobalNamespace

            Dim type = [namespace].GetMember(Of NamedTypeSymbol)("C")
            CheckConstraints(type.TypeParameters(0), TypeParameterConstraintKind.None)
            CheckConstraints(type.TypeParameters(1), TypeParameterConstraintKind.None, "T")
            CheckConstraints(type.TypeParameters(2), TypeParameterConstraintKind.None, "A", "B")

            Dim method = [namespace].GetMember(Of NamedTypeSymbol)("D").GetMember(Of MethodSymbol)("M")
            CheckConstraints(method.TypeParameters(0), TypeParameterConstraintKind.None, "T")
            CheckConstraints(method.TypeParameters(1), TypeParameterConstraintKind.None, "B", "A")
        End Sub

        ' Constraints that differ by custom modifiers
        ' only should be considered duplicates.
        <Fact()>
        Public Sub DuplicateConstraintDifferentModifiers()
            Dim ilSource = <![CDATA[
.class public A { }
.class public abstract B0<T, U>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public virtual abstract instance void M<(!T, !U)V>() { }
}
.class public abstract B1 extends class B0<class A [], class A modopt(int32) []>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance void M<(class A [], class A modopt(int32) [])V>() { ret }
}
.class public abstract B2 extends class B0<class A modopt(int32) [], class A []>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance void M<(class A modopt(int32) [], class A [])V>() { ret }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C1
    Inherits B1
    Public Overrides Sub M(Of V As A())()
        MyBase.M(Of V)()
    End Sub
End Class
Class C2
    Inherits B2
    Public Overrides Sub M(Of V As {A()})()
        MyBase.M(Of V)()
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            compilation.AssertNoErrors()
        End Sub

        ' Binder handles "C?" directly, creating an instance of Nullable(Of C) and
        ' checking constraints (see CreateNullableOf()). In cases where "C?"
        ' appears in a method signature, ensure we're not checking constraints
        ' twice as a result, since constraints in signatures are checked later.
        <WorkItem(543335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543335")>
        <Fact()>
        Public Sub NullableOfTStructureConstraint()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Class A
    Inherits Attribute
    Public Sub New(t As Type)
    End Sub
End Class
Class B
End Class
Class C
    <A(GetType(A?))>
    Shared Function F() As B?
        Return Nothing
    End Function
    Shared Sub M(o As C?)
    End Sub
End Class
   ]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC33101: Type 'A' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
    <A(GetType(A?))>
               ~
BC33101: Type 'B' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
    Shared Function F() As B?
                           ~~
BC33101: Type 'C' must be a value type or a type argument constrained to 'Structure' in order to be used with 'Nullable' or nullable modifier '?'.
    Shared Sub M(o As C?)
                 ~
     ]]></errors>)
        End Sub

        ' Redundant '.ctor' and System.ValueType constraints should be
        ' removed if 'valuetype' is specified. This is consistent with Dev10
        ' and also prevents downstream diagnostics from being generated.
        ' By contrast, redundant 'class' constraints should not be removed
        ' if explicit class constraint is specified.
        <WorkItem(543335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543335")>
        <Fact()>
        Public Sub ObjectAndValueTypeMetadataConstraints()
            Dim ilSource = <![CDATA[
.class public A { }
.class public O1<T> { }
.class public O2<(object)T> { }
.class public V1<valuetype T> { }
.class public V2<valuetype .ctor T> { }
.class public V3<valuetype ([mscorlib]System.ValueType) T> { }
.class public V4<valuetype .ctor ([mscorlib]System.ValueType) T> { }
.class public V5<([mscorlib]System.ValueType) T> { }
.class public R1<(A) T> { }
.class public R2<class (A) T> { }
        ]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            Dim [namespace] = compilation.GlobalNamespace
            CheckConstraints([namespace].GetMember(Of NamedTypeSymbol)("O1").TypeParameters(0), TypeParameterConstraintKind.None)
            CheckConstraints([namespace].GetMember(Of NamedTypeSymbol)("O2").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
            CheckConstraints([namespace].GetMember(Of NamedTypeSymbol)("V1").TypeParameters(0), TypeParameterConstraintKind.ValueType)
            CheckConstraints([namespace].GetMember(Of NamedTypeSymbol)("V2").TypeParameters(0), TypeParameterConstraintKind.ValueType)
            CheckConstraints([namespace].GetMember(Of NamedTypeSymbol)("V3").TypeParameters(0), TypeParameterConstraintKind.ValueType)
            CheckConstraints([namespace].GetMember(Of NamedTypeSymbol)("V4").TypeParameters(0), TypeParameterConstraintKind.ValueType)
            CheckConstraints([namespace].GetMember(Of NamedTypeSymbol)("V5").TypeParameters(0), TypeParameterConstraintKind.None, "ValueType")
            CheckConstraints([namespace].GetMember(Of NamedTypeSymbol)("R1").TypeParameters(0), TypeParameterConstraintKind.None, "A")
            CheckConstraints([namespace].GetMember(Of NamedTypeSymbol)("R2").TypeParameters(0), TypeParameterConstraintKind.ReferenceType, "A")
        End Sub

        <WorkItem(543335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543335")>
        <Fact()>
        Public Sub ObjectAndValueTypeMethodMetadataConstraints()
            Dim ilSource = <![CDATA[
.class public abstract A<T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<(!T)U>() { }
  .method public abstract virtual instance void M2<valuetype (!T)U>() { }
}
.class public B0 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance void M1<U>() { ret }
  .method public virtual instance void M2<valuetype U>() { ret }
}
.class public B1 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance void M1<(object)U>() { ret }
  .method public virtual instance void M2<valuetype (object)U>() { ret }
}
.class public B2 extends class A<class [mscorlib]System.ValueType>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance void M1<(class [mscorlib]System.ValueType)U>() { ret }
  .method public virtual instance void M2<valuetype (class [mscorlib]System.ValueType)U>() { ret }
}
        ]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            Dim [namespace] = compilation.GlobalNamespace
            Dim type = [namespace].GetMember(Of NamedTypeSymbol)("B0")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M1").TypeParameters(0), TypeParameterConstraintKind.None)
            CheckConstraints(type.GetMember(Of MethodSymbol)("M2").TypeParameters(0), TypeParameterConstraintKind.ValueType)
            type = [namespace].GetMember(Of NamedTypeSymbol)("B1")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M1").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M2").TypeParameters(0), TypeParameterConstraintKind.ValueType, "Object")
            type = [namespace].GetMember(Of NamedTypeSymbol)("B2")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M1").TypeParameters(0), TypeParameterConstraintKind.None, "ValueType")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M2").TypeParameters(0), TypeParameterConstraintKind.ValueType)
        End Sub

        ' Overriding methods with implicit and explicit
        ' System.Object and System.ValueType constraints.
        <Fact()>
        Public Sub OverridingObjectAndValueTypeMethodMetadataConstraints()
            Dim ilSource = <![CDATA[
.class interface public abstract IA
{
  .method public abstract virtual instance void M1<U>() { }
  .method public abstract virtual instance void M2<(object)U>() { }
}
.class interface public abstract IB
{
  .method public abstract virtual instance void M1<valuetype U>() { }
  .method public abstract virtual instance void M2<valuetype (object)U>() { }
}
.class interface public abstract IC
{
  .method public abstract virtual instance void M1<valuetype U>() { }
  .method public abstract virtual instance void M2<valuetype (class [mscorlib]System.ValueType)U>() { }
}
.class public abstract A<T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<(!T)U>() { }
  .method public abstract virtual instance void M2<(!T)U>() { }
}
.class public abstract A0 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<U>() { }
  .method public abstract virtual instance void M2<(object)U>() { }
}
.class public abstract B<T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<valuetype (!T)U>() { }
  .method public abstract virtual instance void M2<valuetype (!T)U>() { }
}
.class public abstract B0 extends class B<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<valuetype U>() { }
  .method public abstract virtual instance void M2<valuetype (object)U>() { }
}
.class public abstract C0 extends class B<class [mscorlib]System.ValueType>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<valuetype U>() { }
  .method public abstract virtual instance void M2<valuetype (class [mscorlib]System.ValueType)U>() { }
}
]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class A
    Implements IA
    Public Sub M1(Of U)() Implements IA.M1
    End Sub
    Private Sub M2(Of U As Object)() Implements IA.M2
    End Sub
End Class
Class B
    Implements IB
    Public Sub M1(Of U As Structure)() Implements IB.M1
    End Sub
    Public Sub M2(Of U As Structure)() Implements IB.M2 ' Dev10 error
    End Sub
End Class
Class C
    Implements IC
    Public Sub M1(Of U As Structure)() Implements IC.M1
    End Sub
    Public Sub M2(Of U As Structure)() Implements IC.M2
    End Sub
End Class
Class A1
    Inherits A0
    Public Overrides Sub M1(Of U)()
    End Sub
    Public Overrides Sub M2(Of U As Object)()
    End Sub
End Class
Class B1
    Inherits B0
    Public Overrides Sub M1(Of U As Structure)()
    End Sub
    Public Overrides Sub M2(Of U As Structure)() ' Dev10 error
    End Sub
End Class
Class B2
    Inherits B0
    Public Overrides Sub M1(Of U As Structure)()
    End Sub
    Public Overrides Sub M2(Of U As {Structure, Object})()
    End Sub
End Class
Class C1
    Inherits C0
    Public Overrides Sub M1(Of U As Structure)()
    End Sub
    Public Overrides Sub M2(Of U As Structure)()
    End Sub
End Class
Class C2
    Inherits C0
    Public Overrides Sub M1(Of U As Structure)()
    End Sub
    Public Overrides Sub M2(Of U As {Structure, System.ValueType})() ' Dev10 error
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32077: 'Public Overrides Sub M2(Of U)()' cannot override 'Public MustOverride Overrides Sub M2(Of U)()' because they differ by type parameter constraints.
    Public Overrides Sub M2(Of U As {Structure, System.ValueType})() ' Dev10 error
                         ~~
     ]]></errors>)
        End Sub

        ' Object constraints should not be dropped from TypeParameterSymbol.ConstraintTypes
        ' on import and type substitution. (The C# compiler should drop object constraints.)
        <Fact()>
        Public Sub ObjectConstraintTypes()
            Dim ilSource = <![CDATA[
.class interface public abstract I<T>
{
  .method public abstract virtual instance void M<(!T)U>() { }
}
.class interface public abstract I0 implements class I<object>
{
}
.class public abstract A<T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M<(!T)U>() { }
}
.class public abstract A1 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M<U>() { }
}
.class public abstract A2 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M<(object)U>() { }
}
        ]]>.Value
            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface I1
    Inherits I(Of Object)
End Interface
Class B0
    Inherits A(Of Object)
    Public Overrides Sub M(Of U As Object)()
    End Sub
End Class
Class B1
    Inherits A1
    Public Overrides Sub M(Of U)()
    End Sub
End Class
Class B2
    Inherits A2
    Public Overrides Sub M(Of U As Object)()
    End Sub
End Class
Class C0
    Implements I0
    Private Sub M(Of U As Object)() Implements I(Of Object).M
    End Sub
End Class
Class C1
    Implements I(Of Object)
    Private Sub M(Of U As Object)() Implements I(Of Object).M
    End Sub
End Class
Class D(Of T)
    Public Sub M(Of U As T)()
    End Sub
End Class
Class D0
    Inherits D(Of Object)
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithCustomILSource(vbSource, ilSource)
            Dim [namespace] = compilation.GlobalNamespace
            Dim type = [namespace].GetMember(Of NamedTypeSymbol)("I0")
            CheckConstraints(type.Interfaces(0).GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
            type = [namespace].GetMember(Of NamedTypeSymbol)("A1")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None)
            type = [namespace].GetMember(Of NamedTypeSymbol)("A2")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
            type = [namespace].GetMember(Of NamedTypeSymbol)("I1")
            CheckConstraints(type.Interfaces(0).GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
            type = [namespace].GetMember(Of NamedTypeSymbol)("B0")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
            type = [namespace].GetMember(Of NamedTypeSymbol)("B1")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None)
            type = [namespace].GetMember(Of NamedTypeSymbol)("B2")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
            type = [namespace].GetMember(Of NamedTypeSymbol)("C0")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
            type = [namespace].GetMember(Of NamedTypeSymbol)("C1")
            CheckConstraints(type.GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
            type = [namespace].GetMember(Of NamedTypeSymbol)("D0")
            CheckConstraints(type.BaseType.GetMember(Of MethodSymbol)("M").TypeParameters(0), TypeParameterConstraintKind.None, "Object")
        End Sub

        <WorkItem(543449, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543449")>
        <Fact()>
        Public Sub ExplicitImplementationTypeParameterInSignature()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface IA
End Interface
Class A(Of T As IA)
End Class
Interface IB
    Function F(Of T As IA)(arg As A(Of T)) As A(Of T)
End Interface
Class B
    Implements IB
    Private Function F(Of T As IA)(arg As A(Of T)) As A(Of T) Implements IB.F
        Return Nothing
    End Function
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        ' Invalid constraint types should still be included
        ' in TypeParameterSymbol.ConstraintTypes.
        <Fact()>
        Public Sub InvalidConstraintTypes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
NotInheritable Class A
End Class
Delegate Sub D()
Structure S
End Structure
Enum E
    A
End Enum
Class C
    Sub M0(Of T As Object)()
    End Sub
    Sub M1(Of T As System.ValueType)()
    End Sub
    Sub M2(Of T As System.Array)()
    End Sub
    Sub M3(Of T As System.Enum)()
    End Sub
    Sub M4(Of T As System.Delegate)()
    End Sub
    Sub M5(Of T As A)()
    End Sub
    Sub M6(Of T As D)()
    End Sub
    Sub M7(Of T As S)()
    End Sub
    Sub M8(Of T As E)()
    End Sub
    Sub M9(Of T As Object())()
    End Sub
    Sub M10(Of T As Unknown)()
    End Sub
    Sub M()
        M0(Of Object)()
        M1(Of System.ValueType)()
        M1(Of Object)()
        M2(Of System.Array)()
        M2(Of Object)()
        M3(Of System.Enum)()
        M3(Of Object)()
        M4(Of System.Delegate)()
        M4(Of Object)()
        M5(Of A)()
        M5(Of Object)()
        M6(Of D)()
        M6(Of Object)()
        M7(Of S)()
        M7(Of Object)()
        M8(Of E)()
        M8(Of Object)()
        M9(Of Object())()
        M9(Of Object)()
        M10(Of Unknown)()
        M10(Of Object)()
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC32061: 'Object' cannot be used as a type constraint.
    Sub M0(Of T As Object)()
                   ~~~~~~
BC32061: 'ValueType' cannot be used as a type constraint.
    Sub M1(Of T As System.ValueType)()
                   ~~~~~~~~~~~~~~~~
BC32061: 'Array' cannot be used as a type constraint.
    Sub M2(Of T As System.Array)()
                   ~~~~~~~~~~~~
BC32061: '[Enum]' cannot be used as a type constraint.
    Sub M3(Of T As System.Enum)()
                   ~~~~~~~~~~~
BC32061: '[Delegate]' cannot be used as a type constraint.
    Sub M4(Of T As System.Delegate)()
                   ~~~~~~~~~~~~~~~
BC32060: Type constraint cannot be a 'NotInheritable' class.
    Sub M5(Of T As A)()
                   ~
BC32048: Type constraint 'D' must be either a class, interface or type parameter.
    Sub M6(Of T As D)()
                   ~
BC32048: Type constraint 'S' must be either a class, interface or type parameter.
    Sub M7(Of T As S)()
                   ~
BC32048: Type constraint 'E' must be either a class, interface or type parameter.
    Sub M8(Of T As E)()
                   ~
BC32048: Type constraint 'Object()' must be either a class, interface or type parameter.
    Sub M9(Of T As Object())()
                   ~~~~~~~~
BC30002: Type 'Unknown' is not defined.
    Sub M10(Of T As Unknown)()
                    ~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'ValueType'.
        M1(Of Object)()
        ~~~~~~~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'Array'.
        M2(Of Object)()
        ~~~~~~~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type '[Enum]'.
        M3(Of Object)()
        ~~~~~~~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type '[Delegate]'.
        M4(Of Object)()
        ~~~~~~~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'A'.
        M5(Of Object)()
        ~~~~~~~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'D'.
        M6(Of Object)()
        ~~~~~~~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'S'.
        M7(Of Object)()
        ~~~~~~~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'E'.
        M8(Of Object)()
        ~~~~~~~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'Object()'.
        M9(Of Object)()
        ~~~~~~~~~~~~~
BC30002: Type 'Unknown' is not defined.
        M10(Of Unknown)()
               ~~~~~~~
BC32044: Type argument 'Object' does not inherit from or implement the constraint type 'Unknown'.
        M10(Of Object)()
        ~~~~~~~~~~~~~~
     </errors>)
        End Sub

        <WorkItem(543639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543639")>
        <Fact()>
        Public Sub TestDefaultPropertyThroughConstraint()
            Dim vbCompilation = CreateVisualBasicCompilation("TestDefaultPropertyThroughConstraint",
            <![CDATA[Imports System
Public Module Program
    Class C1
        Default Property P(x As Integer) As Integer
            Get
                Return x
            End Get
            Set(ByVal value As Integer)
            End Set
        End Property
    End Class
    Class C2(Of T As C1)
        Public Dim x As T = New C1
        Public Dim y As Integer = x(100)
    End Class
    Sub Main()
        Dim a = New C2(Of C1)
        Console.WriteLine(a.y)
        M(Of C1)()
    End Sub
    Sub M(Of T As C1)
        Dim x As T = New C1
        Console.WriteLine(x(101))
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))
            CompileAndVerify(vbCompilation, expectedOutput:=<![CDATA[100
101]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(543639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543639")>
        <Fact()>
        Public Sub TestDictionaryAccessThroughConstraint()
            Dim vbCompilation = CreateVisualBasicCompilation("TestDictionaryAccessThroughConstraint",
            <![CDATA[Imports System
Public Module Program
    Class C1
        Default Property P(x As String) As Integer
            Get
                Return x.Length
            End Get
            Set(ByVal value As Integer)
            End Set
        End Property
    End Class
    Class C2(Of T As C1)
        Public Dim x As T = New C1
        Public Dim y As Integer = x!Hello
    End Class
    Sub Main()
        Dim a = New C2(Of C1)
        Console.WriteLine(a.y)
        M(Of C1)()
    End Sub
    Sub M(Of T As C1)
        Dim x As T = New C1
        Console.WriteLine(x!Hi)
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))
            CompileAndVerify(vbCompilation, expectedOutput:=<![CDATA[5
2]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(543688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543688")>
        <Fact()>
        Public Sub ConflictingInheritedConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Structure S
End Structure
Class A
End Class
NotInheritable Class B
    Private Sub New()
    End Sub
End Class
MustInherit Class C1(Of T)
    Friend MustOverride Sub M1(Of U As {Class, T})()
    Friend MustOverride Sub M2(Of U As {T, Class})()
End Class
MustInherit Class C2(Of T)
    Friend MustOverride Sub M1(Of U As {Structure, T})()
    Friend MustOverride Sub M2(Of U As {T, Structure})()
End Class
MustInherit Class C3(Of T)
    Friend MustOverride Sub M1(Of U As {T, New})()
    Friend MustOverride Sub M2(Of U As {New, T})()
End Class
MustInherit Class C4(Of T)
    Friend MustOverride Sub M1(Of U As {T, A})()
    Friend MustOverride Sub M2(Of U As {A, T})()
End Class
Class D1
    Inherits C1(Of S)
    Friend Overrides Sub M1(Of U As {Class, S})()
    End Sub
    Friend Overrides Sub M2(Of U As {S, Class})()
    End Sub
End Class
Class D2
    Inherits C2(Of B)
    Friend Overrides Sub M1(Of U As {Structure, B})()
    End Sub
    Friend Overrides Sub M2(Of U As {B, Structure})()
    End Sub
End Class
Class D3C
    Inherits C3(Of B)
    Friend Overrides Sub M1(Of U As {New, B})()
    End Sub
    Friend Overrides Sub M2(Of U As {New, B})()
    End Sub
End Class
Class D3S
    Inherits C3(Of S)
    Friend Overrides Sub M1(Of U As {New, S})()
    End Sub
    Friend Overrides Sub M2(Of U As {New, S})()
    End Sub
End Class
Class D4
    Inherits C4(Of B)
    Friend Overrides Sub M1(Of U As {B, A})()
    End Sub
    Friend Overrides Sub M2(Of U As {A, B})()
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC32119: Constraint 'Structure S' conflicts with the constraint 'Class' already specified for type parameter 'U'.
    Friend Overrides Sub M1(Of U As {Class, S})()
                                            ~
BC32119: Constraint 'Class' conflicts with the constraint 'Structure S' already specified for type parameter 'U'.
    Friend Overrides Sub M2(Of U As {S, Class})()
                                        ~~~~~
BC32119: Constraint 'Class B' conflicts with the constraint 'Structure' already specified for type parameter 'U'.
    Friend Overrides Sub M1(Of U As {Structure, B})()
                                                ~
BC32119: Constraint 'Structure' conflicts with the constraint 'Class B' already specified for type parameter 'U'.
    Friend Overrides Sub M2(Of U As {B, Structure})()
                                        ~~~~~~~~~
BC32119: Constraint 'Class A' conflicts with the constraint 'Class B' already specified for type parameter 'U'.
    Friend Overrides Sub M1(Of U As {B, A})()
                                        ~
BC32119: Constraint 'Class B' conflicts with the constraint 'Class A' already specified for type parameter 'U'.
    Friend Overrides Sub M2(Of U As {A, B})()
                                        ~
     </errors>)
        End Sub

        <WorkItem(543707, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543707")>
        <Fact()>
        Public Sub ConstraintsWithNestedType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class A(Of T)
    Class B
    End Class
    Class C(Of U As B)
    End Class
    Shared Sub M(Of U As B)(o As U)
    End Sub
End Class
Class D
    Inherits A(Of Object).C(Of A(Of Object).B)
End Class
Class E
    Shared Sub M(Of T)(o As A(Of T).B)
        A(Of Object).M(Of A(Of Object).B)(Nothing)
        A(Of T).M(Of A(Of T).B)(Nothing)
        A(Of T).M(o)
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        ' Object constraint should be emitted
        ' for compatibility with Dev10.
        <WorkItem(543710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543710")>
        <Fact()>
        Public Sub EmittedObjectConstraint()
            Dim sources = <compilation>
                              <file name="c.vb">
Class C
End Class
Interface I(Of T, U As T)
End Interface
Interface I0(Of T)
    Inherits I(Of Object, T)
End Interface
Interface I1(Of T As C)
    Inherits I(Of C, T)
End Interface
MustInherit Class A(Of T)
    Public MustOverride Sub M(Of U As T)()
End Class
Class A0
    Inherits A(Of Object)
    Public Overrides Sub M(Of U As Object)()
    End Sub
End Class
Class A1
    Inherits A(Of C)
    Public Overrides Sub M(Of U As C)()
    End Sub
End Class
    </file>
                          </compilation>
            Dim validator = Sub([module] As ModuleSymbol)
                                Dim [namespace] = [module].GlobalNamespace
                                Dim type = [namespace].GetMember(Of NamedTypeSymbol)("I0")
                                CheckConstraints(type.TypeParameters(0), TypeParameterConstraintKind.None)
                                type = [namespace].GetMember(Of NamedTypeSymbol)("I1")
                                CheckConstraints(type.TypeParameters(0), TypeParameterConstraintKind.None, "C")
                                Dim method = [namespace].GetMember(Of NamedTypeSymbol)("A0").GetMember(Of MethodSymbol)("M")
                                CheckConstraints(method.TypeParameters(0), TypeParameterConstraintKind.None, "Object")
                                method = [namespace].GetMember(Of NamedTypeSymbol)("A1").GetMember(Of MethodSymbol)("M")
                                CheckConstraints(method.TypeParameters(0), TypeParameterConstraintKind.None, "C")
                            End Sub
            CompileAndVerify(sources, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        ' The native compiler reports constraint errors at
        ' the syntax location of invalid type argument.
        <WorkItem(529188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529188")>
        <WorkItem(99630, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=99630")>
        <Fact>
        Public Sub ConstraintErrorLocation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I(Of T As Structure)
End Interface
Class A(Of T As Structure)
End Class
Class B(Of U As I(Of U))
End Class
Class C(Of U)
    Inherits A(Of U)
    Implements I(Of A(Of U))
    Class D
    End Class
    Sub M(Of V As Structure)(o As A(Of I(Of V)))
    End Sub
    Function F() As C(Of A(Of Object())()).D
        Return Nothing
    End Function
End Class
    </file>
</compilation>)
            Dim expected As XElement

            Const bug99630IsFixed = False

            If bug99630IsFixed Then
                expected = <errors>
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class B(Of U As I(Of U))
                     ~
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Inherits A(Of U)
                  ~
BC32105: Type argument 'A(Of U)' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Implements I(Of A(Of U))
                    ~~~~~~~
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Implements I(Of A(Of U))
                         ~
BC32105: Type argument 'I(Of V)' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Sub M(Of V As Structure)(o As A(Of I(Of V)))
                                       ~~~~~~~
BC32105: Type argument 'Object()' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Function F() As C(Of A(Of Object())()).D
                              ~~~~~~~~
                           </errors>
            Else
                expected = <errors>
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class B(Of U As I(Of U))
                ~~~~~~~
BC32105: Type argument 'A(Of U)' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C(Of U)
      ~
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C(Of U)
      ~
BC32105: Type argument 'U' does not satisfy the 'Structure' constraint for type parameter 'T'.
Class C(Of U)
      ~
BC32105: Type argument 'I(Of V)' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Sub M(Of V As Structure)(o As A(Of I(Of V)))
                             ~
BC32105: Type argument 'Object()' does not satisfy the 'Structure' constraint for type parameter 'T'.
    Function F() As C(Of A(Of Object())()).D
                    ~~~~~~~~~~~~~~~~~~~~~~~~
                           </errors>
            End If

            compilation.AssertTheseDiagnostics(expected)
        End Sub

        <Fact()>
        Public Sub CheckConstraintsOnBaseTypeWithUseSiteError()
            Dim vbSource1 =
                <compilation name="6E12649E-ACDD-4A6D-84F4-D1E00B6CA3BB">
                    <file name="c.vb"><![CDATA[
Public Class A
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40(vbSource1)
            compilation1.AssertNoErrors()
            Dim vbSource2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class B(Of T As B(Of T))
    Inherits A
End Class
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(vbSource2, {New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertNoErrors()
            Dim vbSource3 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C
    Inherits B(Of C)
End Class
]]>
                    </file>
                </compilation>
            Dim compilation3 = CreateCompilationWithMscorlib40AndReferences(vbSource3, {New VisualBasicCompilationReference(compilation2)})
            compilation3.AssertTheseDiagnostics(
<expected>
BC30652: Reference required to assembly '6E12649E-ACDD-4A6D-84F4-D1E00B6CA3BB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'A'. Add one to your project.
    Inherits B(Of C)
             ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub InheritedConstraints()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class A
End Class
Class B
End Class
Class C1(Of T As A)
    Sub M(Of U As {T, B}, V As U, W As {U, B})()
    End Sub
End Class
Class C2(Of T As A, U As B)
    Sub M(Of V As {T, U}, W As V, X As {V, B})()
    End Sub
End Class
Class C3(Of T As {A, B})
    Sub M(Of U As T, V As U, W As {U, B})()
    End Sub
End Class
    </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(
    <expected>
BC32111: Indirect constraint 'Class A' obtained from the type parameter constraint 'T' conflicts with the constraint 'Class B'.
    Sub M(Of U As {T, B}, V As U, W As {U, B})()
                   ~
BC32111: Indirect constraint 'Class A' obtained from the type parameter constraint 'U' conflicts with the constraint 'Class B'.
    Sub M(Of U As {T, B}, V As U, W As {U, B})()
                                        ~
BC32109: Indirect constraint 'Class B' obtained from the type parameter constraint 'U' conflicts with the indirect constraint 'Class A' obtained from the type parameter constraint 'T'.
    Sub M(Of V As {T, U}, W As V, X As {V, B})()
                      ~
BC32111: Indirect constraint 'Class A' obtained from the type parameter constraint 'V' conflicts with the constraint 'Class B'.
    Sub M(Of V As {T, U}, W As V, X As {V, B})()
                                        ~
BC32047: Type parameter 'T' can only have one constraint that is a class.
Class C3(Of T As {A, B})
                     ~
BC32111: Indirect constraint 'Class A' obtained from the type parameter constraint 'U' conflicts with the constraint 'Class B'.
    Sub M(Of U As T, V As U, W As {U, B})()
                                   ~
</expected>)
        End Sub

        ' No conflict errors should be reported for undefined constraint types.
        <Fact()>
        Public Sub UndefinedInheritedConstraints()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class A2
End Class
' Both constraints undefined.
Class C1(Of T As {A1, B1})
    Sub M(Of U As T)()
    End Sub
End Class
' One constraint undefined.
Class C2(Of T As {A2, B2})
    Sub M(Of U As T)()
    End Sub
End Class
' One constraint undefined.
Class C3(Of T As {Class, B3})
    Sub M(Of U As T)()
    End Sub
End Class
' Both constraints undefined, on separate parameters.
Class C4(Of T As A4)
    Sub M(Of U As {T, B4})()
    End Sub
End Class
    </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(
    <expected>
BC30002: Type 'A1' is not defined.
Class C1(Of T As {A1, B1})
                  ~~
BC30002: Type 'B1' is not defined.
Class C1(Of T As {A1, B1})
                      ~~
BC30002: Type 'B2' is not defined.
Class C2(Of T As {A2, B2})
                      ~~
BC30002: Type 'B3' is not defined.
Class C3(Of T As {Class, B3})
                         ~~
BC30002: Type 'A4' is not defined.
Class C4(Of T As A4)
                 ~~
BC30002: Type 'B4' is not defined.
    Sub M(Of U As {T, B4})()
                      ~~
</expected>)
        End Sub

        <WorkItem(545255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545255")>
        <Fact()>
        Public Sub Bug13573()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class A(Of T)
    Class B(Of U, V)
        Inherits A(Of U)
        Class C
            Inherits B(Of U, X)
        End Class
    End Class
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC30002: Type 'X' is not defined.
            Inherits B(Of U, X)
                             ~
     </errors>)
        End Sub

        <WorkItem(545415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545415")>
        <Fact()>
        Public Sub Bug13812()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class UpdateResults(Of A As {IStoreable})
    Public ReadOnly Property Deleted As IEnumerable(Of UpdateResult(Of String))
        Get
            Return Nothing
        End Get
    End Property
End Class
Public Class UpdateResult(Of A As {IStoreable})
End Class
Public Interface IStoreable
End Interface
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC32044: Type argument 'String' does not inherit from or implement the constraint type 'IStoreable'.
    Public ReadOnly Property Deleted As IEnumerable(Of UpdateResult(Of String))
                             ~~~~~~~
                                          </expected>)
        End Sub

        <WorkItem(545806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545806")>
        <Fact()>
        Public Sub ClassOrBasesSatisfyConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
MustInherit Class A
    Public Sub New()
    End Sub
End Class
NotInheritable Class B
    Inherits A
    Private Sub New()
    End Sub
End Class
Module M
    Function F(Of T As {A, New})(o As B) As Object
        Return TryCast(o, T)
    End Function
End Module
   ]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC30311: Value of type 'B' cannot be converted to 'T'.
        Return TryCast(o, T)
                       ~
     </errors>)
        End Sub

        ''' <summary>
        ''' Invoke an extension method with an instance of a type parameter U
        ''' where U is a T and T is an array, and where the extension method
        ''' instance parameter type is an interface implemented by the array or
        ''' System.Array. Dev11 fails to resolve the extension method, although
        ''' this appears to be a bug in Dev11. Roslyn resolves the extension method.
        ''' </summary>
        <WorkItem(529820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529820")>
        <Fact()>
        Public Sub ExtensionMethodOnArrayInterface()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
MustInherit Class A(Of T)
    MustOverride Sub M(Of U As T)(o As U)
    Shared Sub M1(o As IEnumerable)
    End Sub
    Shared Sub M2(o As IEnumerable(Of Object))
    End Sub
    Shared Function F() As T
        Return Nothing
    End Function
End Class
Class B
    Inherits A(Of Object())
    Public Overloads Overrides Sub M(Of U As Object())(o As U)
        Dim t = F()
        M1(o)
        M2(o)
        M1(t)
        M2(t)
        o.E1() ' Dev11: BC30456: 'E1' is not a member of 'U'.
        o.E2() ' Dev11: BC30456: 'E2' is not a member of 'U'.
        t.E1()
        t.E2()
    End Sub
End Class
Module E
    <Extension()>
    Sub E1(o As IEnumerable)
    End Sub
    <Extension()>
    Sub E2(o As IEnumerable(Of Object))
    End Sub
End Module
   ]]></file>
</compilation>, references:=DefaultVbReferences)
            compilation.AssertNoErrors()
            Dim compilationVerifier = CompileAndVerify(compilation)
            compilationVerifier.VerifyIL("B.M(Of U)(U)",
            <![CDATA[
{
  // Code size       93 (0x5d)
  .maxstack  2
  IL_0000:  call       "Function A(Of Object()).F() As Object()"
  IL_0005:  ldarg.1
  IL_0006:  box        "U"
  IL_000b:  castclass  "System.Collections.IEnumerable"
  IL_0010:  call       "Sub A(Of Object()).M1(System.Collections.IEnumerable)"
  IL_0015:  ldarg.1
  IL_0016:  box        "U"
  IL_001b:  castclass  "System.Collections.Generic.IEnumerable(Of Object)"
  IL_0020:  call       "Sub A(Of Object()).M2(System.Collections.Generic.IEnumerable(Of Object))"
  IL_0025:  dup
  IL_0026:  call       "Sub A(Of Object()).M1(System.Collections.IEnumerable)"
  IL_002b:  dup
  IL_002c:  call       "Sub A(Of Object()).M2(System.Collections.Generic.IEnumerable(Of Object))"
  IL_0031:  ldarg.1
  IL_0032:  box        "U"
  IL_0037:  castclass  "System.Collections.IEnumerable"
  IL_003c:  call       "Sub E.E1(System.Collections.IEnumerable)"
  IL_0041:  ldarg.1
  IL_0042:  box        "U"
  IL_0047:  castclass  "System.Collections.Generic.IEnumerable(Of Object)"
  IL_004c:  call       "Sub E.E2(System.Collections.Generic.IEnumerable(Of Object))"
  IL_0051:  dup
  IL_0052:  call       "Sub E.E1(System.Collections.IEnumerable)"
  IL_0057:  call       "Sub E.E2(System.Collections.Generic.IEnumerable(Of Object))"
  IL_005c:  ret
}
]]>)
        End Sub

        ''' <summary>
        ''' Constraint failures on derived type when referencing members of base type.
        ''' </summary>
        <WorkItem(530022, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530022")>
        <Fact()>
        Public Sub MembersOfBaseTypeConstraintViolationOnDerived()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Friend Class B
    End Class
    Friend Interface I
    End Interface
    Friend Shared F As Object = Nothing
End Class
Class C(Of T As Structure)
    Inherits A
End Class
Class C1
End Class
Class C2
End Class
Class C3
End Class
Class C4
End Class
Class C5
End Class
Class C6
End Class
Class D
    Inherits C(Of C1).B
    Implements C(Of C2).I
End Class
Class E
    Shared Function M1(o As C(Of C3).B) As C(Of C4).B
        Return Nothing
    End Function
    Shared Sub M2()
        Dim o As Object
        o = New C(Of C5).B()
        o = C(Of C6).F
    End Sub
End Class
   ]]></file>
</compilation>)
            ' Should report the following errors as well. See Dev11.
            ' BC32105: Type argument 'C1' does not satisfy the 'Structure' constraint for type parameter 'T'.
            '     Inherits C(Of C1).B
            '                   ~~
            ' BC32105: Type argument 'C2' does not satisfy the 'Structure' constraint for type parameter 'T'.
            '     Implements C(Of C2).I
            '                     ~~
            ' BC32105: Type argument 'C3' does not satisfy the 'Structure' constraint for type parameter 'T'.
            '     Shared Function M1(o As C(Of C3).B) As C(Of C4).B
            '                                  ~~
            ' BC32105: Type argument 'C4' does not satisfy the 'Structure' constraint for type parameter 'T'.
            '     Shared Function M1(o As C(Of C3).B) As C(Of C4).B
            '                                                 ~~
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32105: Type argument 'C5' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = New C(Of C5).B()
                     ~~
BC32105: Type argument 'C6' does not satisfy the 'Structure' constraint for type parameter 'T'.
        o = C(Of C6).F
                 ~~
     ]]></errors>)
        End Sub

        <WorkItem(545327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545327")>
        <Fact()>
        Public Sub MissingObjectType()
            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="525144ec-61b9-49ff-b073-37982adba3e3">
    <file name="a.vb"><![CDATA[
Class A
End Class
Class B(Of T As A)
End Class
]]></file>
</compilation>, references:={})
            compilation.AssertTheseDiagnostics(<errors>
BC30002: Type 'System.Void' is not defined.
Class A
~~~~~~~~
BC31091: Import of type 'Object' from assembly or module '525144ec-61b9-49ff-b073-37982adba3e3.dll' failed.
Class A
      ~
BC30002: Type 'System.Void' is not defined.
Class B(Of T As A)
~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module '525144ec-61b9-49ff-b073-37982adba3e3.dll' failed.
Class B(Of T As A)
      ~
BC31091: Import of type 'Object' from assembly or module '525144ec-61b9-49ff-b073-37982adba3e3.dll' failed.
Class B(Of T As A)
                ~
</errors>)
        End Sub

        <Fact()>
        Public Sub MissingValueType()
            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="525144ec-61b9-49ff-b073-37982adba3e4">
    <file name="a.vb"><![CDATA[
Structure S
End Structure
MustInherit Class A(Of T)
    Friend MustOverride Sub M(Of U As {Structure, T})()
End Class
Class B
    Inherits A(Of S)
    Friend Overrides Sub M(Of U As {Structure, S})()
    End Sub
End Class
]]></file>
</compilation>, references:={})
            compilation.AssertTheseDiagnostics(<errors>
BC30002: Type 'System.Void' is not defined.
Structure S
~~~~~~~~~~~~
BC31091: Import of type 'ValueType' from assembly or module '525144ec-61b9-49ff-b073-37982adba3e4.dll' failed.
Structure S
          ~
BC30002: Type 'System.Void' is not defined.
MustInherit Class A(Of T)
~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module '525144ec-61b9-49ff-b073-37982adba3e4.dll' failed.
MustInherit Class A(Of T)
                  ~
BC30002: Type 'System.Void' is not defined.
    Friend MustOverride Sub M(Of U As {Structure, T})()
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module '525144ec-61b9-49ff-b073-37982adba3e4.dll' failed.
    Friend MustOverride Sub M(Of U As {Structure, T})()
                                                  ~
BC30002: Type 'System.Void' is not defined.
Class B
~~~~~~~~
BC30002: Type 'System.Void' is not defined.
    Friend Overrides Sub M(Of U As {Structure, S})()
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module '525144ec-61b9-49ff-b073-37982adba3e4.dll' failed.
    Friend Overrides Sub M(Of U As {Structure, S})()
                                               ~
</errors>)
        End Sub

        ''' <summary>
        ''' Cycle with field types with New constraint.
        ''' </summary>
        <WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")>
        <Fact()>
        Public Sub HasPublicParameterlessConstructorCycle01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Private F As C(Of B)
End Class
Class B
    Private F As C(Of A)
End Class
Class C(Of T As New)
End Class
   ]]></file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        ''' <summary>
        ''' Cycle with event types with New constraint.
        ''' </summary>
        <WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")>
        <Fact()>
        Public Sub HasPublicParameterlessConstructorCycle02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Event E As D(Of B)
End Class
Class B
    Private Sub New()
    End Sub
    Event E As D(Of A)
End Class
Class C
    Private Sub New()
    End Sub
    Custom Event E As D(Of C)
        AddHandler(value As D(Of C))
        End AddHandler
        RemoveHandler(value As D(Of C))
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
Delegate Sub D(Of T As New)()
   ]]></file>
</compilation>)
            ' Note, there are redundant errors for the handlers in addition
            ' to the errors from the containing custom event. See #16080.
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Event E As D(Of B)
          ~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Custom Event E As D(Of C)
                 ~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        AddHandler(value As D(Of C))
                   ~~~~~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        AddHandler(value As D(Of C))
                                 ~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        RemoveHandler(value As D(Of C))
                      ~~~~~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        RemoveHandler(value As D(Of C))
                                    ~
     ]]></errors>)
        End Sub

        ''' <summary>
        ''' Cycle with property types with New constraint.
        ''' </summary>
        <WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")>
        <Fact()>
        Public Sub HasPublicParameterlessConstructorCycle03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Property P As C(Of B)
End Class
Class B
    Property P As C(Of A)
End Class
Class C(Of T As New)
End Class
   ]]></file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        ''' <summary>
        ''' Cycle with property types with New constraint where the types
        ''' are parameter types and properties are explicit implementations.
        ''' </summary>
        <WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")>
        <Fact()>
        Public Sub HasPublicParameterlessConstructorCycle04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Interface IA(Of T As New)
    ReadOnly Property P As IA(Of T)
End Interface
Interface IB(Of T As New)
    Default ReadOnly Property P(o As IB(Of T)) As Object
End Interface
Class A
    Implements IA(Of A)
    Private Sub New()
    End Sub
    ReadOnly Property P As IA(Of A) Implements IA(Of A).P
        Get
            Return Nothing
        End Get
    End Property
End Class
Class B
    Implements IB(Of B)
    Private Sub New()
    End Sub
    Default Public ReadOnly Property P(o As IB(Of B)) As Object Implements IB(Of B).P
        Get
            Return Nothing
        End Get
    End Property
End Class
   ]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC32083: Type argument 'A' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
Class A
      ~
BC32083: Type argument 'A' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    ReadOnly Property P As IA(Of A) Implements IA(Of A).P
                      ~
BC32083: Type argument 'A' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    ReadOnly Property P As IA(Of A) Implements IA(Of A).P
                                                     ~
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
Class B
      ~
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Default Public ReadOnly Property P(o As IB(Of B)) As Object Implements IB(Of B).P
                                       ~
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Default Public ReadOnly Property P(o As IB(Of B)) As Object Implements IB(Of B).P
                                                  ~
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Default Public ReadOnly Property P(o As IB(Of B)) As Object Implements IB(Of B).P
                                                                                 ~
     ]]></errors>)
        End Sub

        ''' <summary>
        ''' Avoid redundant errors from accessors when the
        ''' same errors are reported from property signature.
        ''' </summary>
        <WorkItem(530423, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530423")>
        <WorkItem(101074, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=101074")>
        <Fact>
        Public Sub PropertySignatureDuplicateErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I(Of T As New)
End Interface
Class A
    Private Sub New()
    End Sub
End Class
Class B
    Private Sub New()
    End Sub
End Class
Class C
    Private Sub New()
    End Sub
End Class
Class D
    Property P1(o As I(Of A)) As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    Property P2 As I(Of B)
    Property P3 As I(Of C)
        Get
            Return Nothing
        End Get
        Set(value As I(Of C))
        End Set
    End Property
End Class
   ]]></file>
</compilation>)

            Dim expected As XElement

            Const bug101074IsFixed = False

            If bug101074IsFixed Then
                expected = <errors>
BC32083: Type argument 'A' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Property P1(o As I(Of A)) As Object
                          ~            
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Property P2 As I(Of B)
                        ~ 
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Property P3 As I(Of C)
                        ~ 
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        Set(value As I(Of C))
                          ~  
                           </errors>
            Else
                expected = <errors>
BC32083: Type argument 'A' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Property P1(o As I(Of A)) As Object
                ~
BC32083: Type argument 'A' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Property P1(o As I(Of A)) As Object
                ~
BC32083: Type argument 'A' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Property P1(o As I(Of A)) As Object
                          ~
BC32083: Type argument 'B' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Property P2 As I(Of B)
             ~~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
    Property P3 As I(Of C)
             ~~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        Set(value As I(Of C))
            ~~~~~
BC32083: Type argument 'C' must have a public parameterless instance constructor to satisfy the 'New' constraint for type parameter 'T'.
        Set(value As I(Of C))
                          ~
                           </errors>
            End If

            compilation.AssertTheseDiagnostics(expected)
        End Sub

        <WorkItem(546780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546780")>
        <Fact()>
        Public Sub Bug16806()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A(Of T)
    Class B(Of U)
        Inherits A(Of Object)
        Class C
            Inherits B(Of )
        End Class
        Private F As Object = GetType(B(Of ))
    End Class
End Class
   ]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30182: Type expected.
            Inherits B(Of )
                          ~
     ]]></errors>)
        End Sub

        <WorkItem(531227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531227")>
        <Fact()>
        Public Sub ConstraintOverrideBaseTypeCycle()
            Dim compilation = CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="a.vb"><![CDATA[
Public Class Base(Of T As New)
    Public Overridable Property P As Integer
End Class

Public Class Derived
    Inherits Base(Of Derived)

    Public Overrides Property P As Integer
End Class
   ]]></file>
                </compilation>)
            Dim derivedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Derived")
            derivedType.GetMembers()
        End Sub

        <WorkItem(531227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531227")>
        <Fact()>
        Public Sub ConstraintExplicitImplementationInterfaceCycle()
            Dim compilation = CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="a.vb"><![CDATA[
Public Interface I(Of T As New)
    Property P As Integer
End Interface

Public Class Implementation
    Implements I(Of Implementation)

    Public Property P As Integer Implements I(Of Implementation).P
End Class
   ]]></file>
                </compilation>)
            Dim derivedType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Implementation")
            derivedType.GetMembers()
        End Sub

        <Fact()>
        Public Sub UseSiteErrorReportingCycleInBaseReference()
            Dim source1 =
                <compilation name="e521fe98-c881-45cf-8870-249e00ae400d">
                    <file name="c.vb"><![CDATA[
Public Class A
End Class
Public Interface IA
End Interface
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40(source1)
            compilation1.AssertNoErrors()
            Dim source2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Public Class B
    Inherits A
End Class
Public Class C(Of T As B)
    Inherits B
End Class
Public Interface IB
    Inherits IA
End Interface
Public Interface IC(Of T As IB)
    Inherits IB
End Interface
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(source2, {New VisualBasicCompilationReference(compilation1)})
            compilation2.AssertNoErrors()
            Dim source3 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class D
    Inherits C(Of D)
    Implements IC(Of D)
End Class
]]>
                    </file>
                </compilation>
            Dim compilation3 = CreateCompilationWithMscorlib40AndReferences(source3, {New VisualBasicCompilationReference(compilation2)})
            compilation3.AssertTheseDiagnostics(
<expected>
BC30652: Reference required to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'A'. Add one to your project.
Class D
      ~
BC30652: Reference required to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'IA'. Add one to your project.
Class D
      ~
BC30652: Reference required to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'A'. Add one to your project.
    Inherits C(Of D)
             ~~~~~~~
BC30652: Reference required to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'A'. Add one to your project.
    Implements IC(Of D)
               ~~~~~~~~
BC30652: Reference required to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'IA'. Add one to your project.
    Implements IC(Of D)
               ~~~~~~~~
BC30652: Reference required to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'A'. Add one to your project.
    Implements IC(Of D)
                     ~
</expected>)
        End Sub

        ''' <summary>
        ''' Dev11 fails to resolve extension method E(Object) for o.E()
        ''' when the type of o is a type parameter with certain
        ''' constraints. Roslyn handles such cases correctly.
        ''' </summary>
        <WorkItem(578752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578752")>
        <Fact()>
        Public Sub Bug578752()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices
Interface I(Of T)
End Interface
Class C(Of T)
End Class
Module M
    Function F0(Of T)(o0 As T) As Object
        Return o0.E()
    End Function
    Function F1(Of T As U, U)(o1 As T) As Object
        Return o1.E() ' Dev11: BC30456: 'E' is not a member of 'T'.
    End Function
    Function F2(Of T As I(Of Object))(o2 As T) As Object
        Return o2.E() ' Dev11: BC30456: 'E' is not a member of 'T'.
    End Function
    Function F3(Of T As C(Of Object))(o3 As T) As Object
        Return o3.E()
    End Function
    <Extension>
    Function E(o As Object) As Object
        Return Nothing
    End Function
End Module
   ]]></file>
</compilation>, references:={Net40.References.SystemCore})
            compilation.AssertNoErrors()
        End Sub

        <WorkItem(578762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578762")>
        <Fact()>
        Public Sub Bug578762()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Linq
Module M
    Sub F(Of T As {IEnumerable(Of String), ICollection(Of String)})(x As T)
        Dim c As IEnumerable(Of String)
        c = x.AsEnumerable()
        c = x.AsQueryable()
    End Sub
End Module
   ]]></file>
</compilation>, references:={Net40.References.SystemCore})
            compilation.AssertNoErrors()
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Linq
Module M
    Sub F(Of T As {IEnumerable(Of String), ICollection(Of String)})(x As T)
        Dim y As String = x(0).ToLower()
    End Sub
End Module
   ]]></file>
</compilation>, references:={Net40.References.SystemCore})
            compilation.AssertNoErrors()
        End Sub

        <WorkItem(578762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578762")>
        <Fact()>
        Public Sub Bug578762_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices
Interface IA(Of T)
End Interface
Interface IB(Of T)
    Inherits IA(Of T)
End Interface
Interface IC(Of T)
    Inherits IB(Of T)
End Interface
Module M
    <Extension()>
    Sub E1(Of T)(o As IA(Of T))
    End Sub
    <Extension()>
    Sub E2(Of T)(o As IB(Of T))
    End Sub
    <Extension()>
    Sub E3(Of T)(o As IC(Of T))
    End Sub
End Module
Class C
    Sub F(Of T As {IA(Of String), IC(Of String), IB(Of String)})(o As T)
        o.E1()
        o.E2()
        o.E3()
    End Sub
End Class
   ]]></file>
</compilation>, references:={Net40.References.SystemCore})
            compilation.AssertNoErrors()
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices
Class A(Of T)
End Class
Class B(Of T)
    Inherits A(Of T)
End Class
Class C(Of T)
    Inherits B(Of T)
End Class
Module M
    <Extension()>
    Sub E1(Of T)(o As A(Of T))
    End Sub
    <Extension()>
    Sub E2(Of T)(o As B(Of T))
    End Sub
    <Extension()>
    Sub E3(Of T)(o As C(Of T))
    End Sub
End Module
MustInherit Class D(Of T, U, V)
    MustOverride Sub F(Of X As {T, U, V})(o As X)
End Class
Class E
    Inherits D(Of A(Of String), C(Of String), B(Of String))
    Public Overrides Sub F(Of X As {A(Of String), C(Of String), B(Of String)})(o As X)
        o.E1()
        o.E2()
        o.E3()
    End Sub
End Class
   ]]></file>
</compilation>, references:={Net40.References.SystemCore})
            compilation.AssertNoErrors()
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices
Interface IA(Of T)
End Interface
Class A(Of T)
    Implements IA(Of T)
End Class
Class B(Of T)
    Inherits A(Of T)
    Implements IA(Of Object)
End Class
Module M
    <Extension()>
    Sub E0(o As IA(Of Object))
    End Sub
    <Extension()>
    Sub E1(Of T)(o As IA(Of T))
    End Sub
End Module
MustInherit Class C(Of T, U)
    MustOverride Sub F(Of X As {T, U})(o As X)
End Class
Class D1
    Inherits C(Of A(Of String), B(Of String))
    Overrides Sub F(Of X As {A(Of String), B(Of String)})(o1 As X)
        o1.E0()
        o1.E1()
    End Sub
End Class
Class D2
    Inherits C(Of B(Of Object), A(Of Object))
    Overrides Sub F(Of X As {B(Of Object), A(Of Object)})(o2 As X)
        o2.E0()
        o2.E1()
    End Sub
End Class
   ]]></file>
</compilation>, references:={Net40.References.SystemCore})
            compilation.AssertTheseDiagnostics(
<expected>
BC30456: 'E1' is not a member of 'X'.
        o1.E1()
        ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub DelegateMembers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Delegate Function D() As Object
MustInherit Class A(Of T)
    MustOverride Sub F(Of U As T)(o As U)
End Class
Class B
    Inherits A(Of D)
    Public Overrides Sub F(Of T As D)(o As T)
        Dim v As Object
        v = o.Method
        v = o.Target
        v = o.GetInvocationList()
    End Sub
End Class
   ]]></file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        Private Shared Sub CheckConstraints(typeParameter As TypeParameterSymbol, constraints As TypeParameterConstraintKind, ParamArray constraintTypeDescriptions As String())
            Assert.Equal((constraints And TypeParameterConstraintKind.Constructor) <> 0, typeParameter.HasConstructorConstraint)
            Assert.Equal((constraints And TypeParameterConstraintKind.ReferenceType) <> 0, typeParameter.HasReferenceTypeConstraint)
            Assert.Equal((constraints And TypeParameterConstraintKind.ValueType) <> 0, typeParameter.HasValueTypeConstraint)
            CompilationUtils.CheckSymbols(typeParameter.ConstraintTypes, constraintTypeDescriptions)
        End Sub

        <WorkItem(578123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578123")>
        <Fact()>
        Public Sub Bug578123()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class C(Of T As System.Console)
End Class

Module Module1
	Sub Main()
        Dim x as New System.Console()
	End Sub
End Module

Class C1 
    Inherits System.Console
End Class
   ]]></file>
</compilation>, TestOptions.ReleaseDll)
            compilation.AssertTheseDiagnostics(
<expected>
BC32060: Type constraint cannot be a 'NotInheritable' class.
Class C(Of T As System.Console)
                ~~~~~~~~~~~~~~
BC30517: Overload resolution failed because no 'New' is accessible.
        Dim x as New System.Console()
                            ~~~~~~~
BC30299: 'C1' cannot inherit from class 'Console' because 'Console' is declared 'NotInheritable'.
    Inherits System.Console
             ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AccessProtectedMemberOnInstance_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Protected F As Object = 1
    Protected Function G() As Object
        Return Nothing
    End Function
    Protected Property P As Object
End Class
Class B(Of T As A)
    Shared Sub M(a As T)
        Dim o As Object
        o = a.F
        o = a.G()
        o = a.P
    End Sub
End Class
   ]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(
<expected>
BC30389: 'A.F' is not accessible in this context because it is 'Protected'.
        o = a.F
            ~~~
BC30390: 'A.Protected Function G() As Object' is not accessible in this context because it is 'Protected'.
        o = a.G()
            ~~~
BC30389: 'A.P' is not accessible in this context because it is 'Protected'.
        o = a.P
            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AccessProtectedMemberOnInstance_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Protected F As Object = 1
    Protected Function G() As Object
        Return Nothing
    End Function
    Protected Property P As Object
End Class
Class B(Of T As B(Of T))
    Shared Sub M(a As T)
        Dim o As Object
        o = a.F
        o = a.G()
        o = a.P
    End Sub
End Class
   ]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(
<expected>
BC30456: 'F' is not a member of 'T'.
        o = a.F
            ~~~
BC30456: 'G' is not a member of 'T'.
        o = a.G()
            ~~~
BC30456: 'P' is not a member of 'T'.
        o = a.P
            ~~~
</expected>)
        End Sub

        <WorkItem(837422, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837422")>
        <Fact()>
        Public Sub RedundantValueTypeConstraint()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Interface I(Of T)
    Sub M(Of U As {T, Structure})
End Interface
MustInherit Class A(Of T)
    Friend MustOverride Sub M(Of U As {Structure, T})
End Class
Class B
    Inherits A(Of ValueType)
    Implements I(Of ValueType)
    Friend Overrides Sub M(Of T As {Structure, ValueType})
    End Sub
    Private Sub I_M(Of T As {ValueType, Structure}) Implements I(Of ValueType).M
    End Sub
End Class
   ]]></file>
</compilation>)
        End Sub

        <WorkItem(4097, "https://github.com/dotnet/roslyn/issues/4097")>
        <Fact>
        Public Sub ObsoleteTypeInConstraints()
            CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
<System.Obsolete>
class Class1(Of T As Class2)
End Class

<System.Obsolete>
class Class2
End Class

class Class3(Of T As Class2)
    <System.Obsolete>
    Sub M1(Of S As Class2)
    End Sub

    Sub M2(Of S As Class2)
    End Sub
End Class

class Class4
    <System.Obsolete>
    Private Partial Sub M3(Of S As Class2)
    End Sub
End Class

Partial class Class4
    Private Partial Sub M4(Of S As Class2)
    End Sub
End Class
   ]]></file>
</compilation>, options:=TestOptions.DebugDll).Diagnostics.AssertTheseDiagnostics(
<expected>
BC40008: 'Class2' is obsolete.
class Class3(Of T As Class2)
                     ~~~~~~
BC40008: 'Class2' is obsolete.
    Sub M2(Of S As Class2)
                   ~~~~~~
BC40008: 'Class2' is obsolete.
    Private Partial Sub M4(Of S As Class2)
                                   ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub EnumConstraint_FromCSharp()
            Dim reference = CreateCSharpCompilation("
public class Test<T> where T : System.Enum
{
}", parseOptions:=New CSharp.CSharpParseOptions(CSharp.LanguageVersion.CSharp7_3)).EmitToImageReference()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib45AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Enum E1
    A
End Enum
Public Class Test2
    Public Sub M()
        Dim a = new Test(Of E1)()             ' enum
        Dim b = new Test(Of Integer)()        ' value type
        Dim c = new Test(Of string)()         ' reference type
        Dim d = new Test(Of System.Enum)()    ' Enum type
    End Sub
End Class
    </file>
</compilation>, {reference})

            AssertTheseDiagnostics(compilation,
<expected>
BC32044: Type argument 'Integer' does not inherit from or implement the constraint type '[Enum]'.
        Dim b = new Test(Of Integer)()        ' value type
                            ~~~~~~~
BC32044: Type argument 'String' does not inherit from or implement the constraint type '[Enum]'.
        Dim c = new Test(Of string)()         ' reference type
                            ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DelegateConstraint_FromCSharp()
            Dim reference = CreateCSharpCompilation("
public class Test<T> where T : System.Delegate
{
}", parseOptions:=New CSharp.CSharpParseOptions(CSharp.LanguageVersion.CSharp7_3)).EmitToImageReference()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib45AndVBRuntime(
<compilation>
    <file name="a.vb">
Delegate Sub D1()

Public Class Test2
    Public Sub M()
        Dim a = new Test(Of D1)()                   ' delegate
        Dim b = new Test(Of Integer)()              ' value type
        Dim c = new Test(Of string)()               ' reference type
        Dim d = new Test(Of System.Delegate)()      ' Delegate type
    End Sub
End Class
    </file>
</compilation>, {reference})

            AssertTheseDiagnostics(compilation,
<expected>
BC32044: Type argument 'Integer' does not inherit from or implement the constraint type '[Delegate]'.
        Dim b = new Test(Of Integer)()              ' value type
                            ~~~~~~~
BC32044: Type argument 'String' does not inherit from or implement the constraint type '[Delegate]'.
        Dim c = new Test(Of string)()               ' reference type
                            ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub MulticastDelegateConstraint_FromCSharp()
            Dim reference = CreateCSharpCompilation("
public class Test<T> where T : System.MulticastDelegate
{
}", parseOptions:=New CSharp.CSharpParseOptions(CSharp.LanguageVersion.CSharp7_3)).EmitToImageReference()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib45AndVBRuntime(
<compilation>
    <file name="a.vb">
Delegate Sub D1()

Public Class Test2
    Public Sub M()
        Dim a = new Test(Of D1)()                           ' delegate
        Dim b = new Test(Of Integer)()                      ' value type
        Dim c = new Test(Of string)()                       ' reference type
        Dim d = new Test(Of System.MulticastDelegate)()     ' MulticastDelegate type
    End Sub
End Class
    </file>
</compilation>, {reference})

            AssertTheseDiagnostics(compilation,
<expected>
BC32044: Type argument 'Integer' does not inherit from or implement the constraint type 'MulticastDelegate'.
        Dim b = new Test(Of Integer)()                      ' value type
                            ~~~~~~~
BC32044: Type argument 'String' does not inherit from or implement the constraint type 'MulticastDelegate'.
        Dim c = new Test(Of string)()                       ' reference type
                            ~~~~~~
</expected>)
        End Sub

        <Fact, WorkItem(1279758, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1279758/")>
        Public Sub RecursiveConstraintsFromUnifiedAssemblies()
            Dim metadataComp = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class A(Of T1 As A(Of T1, T2), T2 As A(Of T1, T2).B(Of T1, T2))
    Public Class B(Of T3 As A(Of T3, T4), T4 As A(Of T3, T4).B(Of T3, T4))
    End Class
End Class
Public Class C
    Inherits A(Of C, C.D)

    Public Class D
        Inherits A(Of C, C.D).B(Of C, C.D)
    End Class
End Class]]>
    </file>
</compilation>, assemblyName:="assembly1")

            metadataComp.AssertTheseDiagnostics()

            Dim finalComp = CreateCompilationWithMscorlib461(
<compilation>
    <file name="b.vb"><![CDATA[
Class D
    Shared Sub Main()
        System.Console.WriteLine(GetType(C.D).FullName)
    End Sub
End Class]]>
    </file>
</compilation>, {metadataComp.EmitToImageReference()})
            finalComp.AssertTheseDiagnostics()

            Assert.Null(finalComp.GetTypeByMetadataName("C").GetUseSiteErrorInfo())
        End Sub

    End Class

End Namespace
