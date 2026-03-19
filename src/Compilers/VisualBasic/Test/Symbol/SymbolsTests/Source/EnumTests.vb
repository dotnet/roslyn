' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class EnumTests
        Inherits BasicTestBase

        ' The value of first enumerator, and the value of each successive enumerator 
        <Fact>
        Public Sub ValueOfFirst()
            Dim text =
<compilation name="C">
    <file name="a.vb">
        Enum Suits
            ValueA
            ValueB
            ValueC
            ValueD
        End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "Suits", 0, 1, 2, 3)
        End Sub

        ' The value can be explicated initialized 
        <Fact>
        Public Sub ExplicateInit()
            Dim text =
<compilation name="C">
    <file name="a.vb">
                 Public Enum Suits
                    ValueA = -1
                    ValueB = 2
                    ValueC = 3
                    ValueD = 4
                 End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "Suits", -1, 2, 3, 4)
        End Sub

        ' The value can be explicated and implicit initialized 
        <Fact>
        Public Sub MixedInit()
            Dim text =
<compilation name="C">
    <file name="a.vb">
                 Public Enum Suits
                    ValueA
                    ValueB = 10
                    ValueC
                    ValueD
                 End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "Suits", 0, 10, 11, 12)
        End Sub

        ' Using shared field of an enum member does not cause evaluation cycle 
        <Fact>
        Public Sub MixedInitShared()
            Dim text =
<compilation name="C">
    <file name="a.vb">
                 Public Enum Suits
                    ValueA
                    ValueB = 10
                    ValueC = Suits.ValueC.ValueB + 1
                    ValueD
                 End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "Suits", 0, 10, 11, 12)
        End Sub

        ' Enumerator initializers must be of integral or enumeration type 
        <WorkItem(539945, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539945")>
        <Fact>
        Public Sub OutOfUnderlyingRange()
            Dim text =
<compilation name="C">
    <file name="a.vb">
                Option Strict Off
                 Public Enum Suits As Byte
                    ValueA = "3"         	        ' Can't implicitly convert 
                    ValueB = 2.2         	        ' Can implicitly convert 
                    ValueC = 257         	        ' Out of underlying range 
                 End Enum
    </file>
</compilation>

            ' There are diagnostics for these values (see EnumErrorsInValues test),
            ' but as long as the value is constant (including the needed conversion), the constant value is used
            ' (see conversion of 2.2 vs. conversion of "3").
            Dim fields = VerifyEnumsValue(text, "Suits", SpecialType.System_Byte, Nothing, CByte(2), Nothing)

            fields.First.DeclaringCompilation.AssertTheseDiagnostics(
<expected>
BC30060: Conversion from 'String' to 'Byte' cannot occur in a constant expression.
                    ValueA = "3"         	        ' Can't implicitly convert 
                             ~~~
BC30439: Constant expression not representable in type 'Byte'.
                    ValueC = 257         	        ' Out of underlying range 
                             ~~~
</expected>)

            text =
<compilation name="C">
    <file name="a.vb">
                Option Strict On
                 Public Enum Suits As Byte
            ValueA = "3"                    ' Can't implicitly convert 
            ValueB = 2.2                    ' Can't implicitly convert: [Option Strict On] disallows implicit conversion
            ValueC = 257                    ' Out of underlying range 
                 End Enum
    </file>
</compilation>

            ' There are diagnostics for these values (see EnumErrorsInValues test),
            ' but as long as the value is constant (including the needed conversion), the constant value is used
            ' (see conversion of 2.2 vs. conversion of "3").
            fields = VerifyEnumsValue(text, "Suits", SpecialType.System_Byte, Nothing, CByte(2), Nothing)

            fields.First.DeclaringCompilation.AssertTheseDiagnostics(
<expected>
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Byte'.
            ValueA = "3"                    ' Can't implicitly convert 
                     ~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Byte'.
            ValueB = 2.2                    ' Can't implicitly convert: [Option Strict On] disallows implicit conversion
                     ~~~
BC30439: Constant expression not representable in type 'Byte'.
            ValueC = 257                    ' Out of underlying range 
                     ~~~
</expected>)

            text =
<compilation name="C">
    <file name="a.vb">
                     Enum Suits As Short
                        a
                        b
                        c
                        d = -65536
                        e
                        f
                     End Enum
    </file>
</compilation>

            fields = VerifyEnumsValue(text, "Suits", SpecialType.System_Int16, CShort(0), CShort(1), CShort(2), Nothing, Nothing, Nothing)

            fields.First.DeclaringCompilation.AssertTheseDiagnostics(
<expected>
BC30439: Constant expression not representable in type 'Short'.
                        d = -65536
                            ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub EnumErrorsInValues()
            Dim text =
<compilation name="C">
    <file name="a.vb">
        Public Enum Suits As Byte
            ValueA = "3"         	        ' Can't implicitly convert 
            ValueB = 2.2         	        ' Can implicitly convert 
            ValueC = 257         	        ' Out of underlying range 
        End Enum
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors>
BC30060: Conversion from 'String' to 'Byte' cannot occur in a constant expression.
            ValueA = "3"         	        ' Can't implicitly convert 
                     ~~~
BC30439: Constant expression not representable in type 'Byte'.
            ValueC = 257         	        ' Out of underlying range 
                     ~~~

</errors>)

            comp = CompilationUtils.CreateCompilationWithMscorlib40(text, options:=TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(comp, <errors>
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Byte'.
            ValueA = "3"         	        ' Can't implicitly convert 
                     ~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Byte'.
            ValueB = 2.2         	        ' Can implicitly convert 
                     ~~~
BC30439: Constant expression not representable in type 'Byte'.
            ValueC = 257         	        ' Out of underlying range 
                     ~~~
</errors>)

        End Sub

        <Fact()>
        Public Sub ExplicitAssociated()
            Dim text =
<compilation name="C">
    <file name="a.vb">
                 Class C(Of T)
                    Const field As Integer = 100
                    Private Enum TestEnum
                        A
                        B = A		        ' another member
                        C = D		        ' another member
                        D = CByte(11)		        ' type can be implicitly converted to underlying type
                        F = 3 + 5		        ' expression
                        G = field		        ' const field
                        TestEnum		        ' its own type name
                        var		        ' contextual keyword
                        T		        ' Type parameter
                    End Enum
                    Private Enum EnumB
                        B = TestEnum.T
                    End Enum
                 End Class
    </file>
</compilation>

            VerifyEnumsValue(text, "C.TestEnum", 0, 0, 11, 11, 8, 100, 101, 102, 103)
            VerifyEnumsValue(text, "C.EnumB", 103)

            text =
<compilation name="C">
    <file name="a.vb">
         Class c1
            Public Shared StaticField As Integer = 10
            Public Shared ReadOnly ReadonlyField As Integer = 100
            Private Enum EnumTest
                A = StaticField
                B = ReadonlyField
            End Enum
         End Class
    </file>
</compilation>

            VerifyEnumsValue(text, "c1.EnumTest", SpecialType.System_Int32, Nothing, Nothing)
        End Sub

        ' No enum-body 
        <Fact>
        Public Sub NoEnumBody()
            Dim text =
<compilation name="C">
    <file name="a.vb">
                        Enum Figure
                        End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "Figure")
        End Sub

        ' No identifier
        <Fact>
        Public Sub BC30203ERR_ExpectedIdentifier_NoIDForEnum()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BadEmptyEnum1">
    <file name="a.vb">
        Enum 
            One
            Two
            Three
        End Enum
        </file>
</compilation>)
            CompilationUtils.AssertTheseParseDiagnostics(comp, <ERRORS>
BC30203: Identifier expected.
Enum 
     ~
</ERRORS>)
        End Sub

        <Fact>
        Public Sub EnumTypeCharMismatch()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="EnumTypeCharMismatch">
    <file name="a.vb">
Enum E As Integer
    X
    Y = E.X%
    Z = E.X$
End Enum
        </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(comp, <ERRORS>
BC30277: Type character '$' does not match declared data type 'Integer'.
    Z = E.X$
          ~~
</ERRORS>)
        End Sub

        <Fact>
        Public Sub EnumTypeCharMismatch1()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="EnumTypeCharMismatch">
    <file name="a.vb">
Imports System 
        
Module M1
    Enum E As Integer
        X = Int32%.MinValue
        Y = E%.X
        Z = E$.X
    End Enum
End Module
        </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(comp, <ERRORS>
BC30277: Type character '$' does not match declared data type 'Integer'.
        Z = E$.X
            ~~
</ERRORS>)
        End Sub

        ' Same identifier for enum members
        <Fact>
        Public Sub SameIDForEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Enum TestEnum
   One
   One
End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "TestEnum", 0, 1)
        End Sub

        ' Modifiers for enum
        <WorkItem(539944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539944")>
        <Fact>
        Public Sub BC30396ERR_BadEnumFlags1_ModifiersForEnum()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
             <compilation name="C">
                 <file name="a.vb">
Class Program
    Protected Enum Figure1
        One = 1
    End Enum    ' OK
    New Public Enum Figure2
        Zero = 0
    End Enum    ' new + protection modifier is Not OK 
    Private MustInherit Enum Figure3
        Zero
    End Enum    ' abstract not valid
    Private Private Enum Figure4
        One = 1
    End Enum    ' Duplicate modifier is not OK
    Private Public Enum Figure5
    End Enum    ' More than one protection modifiers is not OK
    Private NotInheritable Enum Figure0
        Zero
    End Enum    ' sealed not valid
    Private Shadows Enum Figure
        Zero
    End Enum    ' OK
End Class
        </file>
             </compilation>)
            Dim expectedErrors1 = <errors>
BC30035: Syntax error.
    New Public Enum Figure2
    ~~~
BC30188: Declaration expected.
        Zero = 0
        ~~~~
BC30184: 'End Enum' must be preceded by a matching 'Enum'.
    End Enum    ' new + protection modifier is Not OK 
    ~~~~~~~~
BC30396: 'MustInherit' is not valid on an Enum declaration.
    Private MustInherit Enum Figure3
            ~~~~~~~~~~~
BC30178: Specifier is duplicated.
    Private Private Enum Figure4
            ~~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Enum Figure5
            ~~~~~~
BC30280: Enum 'Figure5' must contain at least one member.
    Private Public Enum Figure5
                        ~~~~~~~
BC30396: 'NotInheritable' is not valid on an Enum declaration.
    Private NotInheritable Enum Figure0
            ~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        ' Modifiers for enum member
        <Fact>
        Public Sub ModifiersForEnumMember()
            Dim text =
<compilation name="C">
    <file name="a.vb">                
         Enum ColorA
            Public Red
         End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "ColorA")

            text =
<compilation name="C">
    <file name="a.vb">
         Enum ColorA
            Private Sub goo()
             End Sub
         End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "ColorA")
        End Sub

        ' Flag Attribute and Enumerate a Enum
        <Fact>
        Public Sub FlagOnEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
         &lt;System.Flags&gt;
         Public Enum Suits
            ValueA = 1
            ValueB = 2
            ValueC = 4
            ValueD = 8
            Combi = ValueA Or ValueB
         End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "Suits", 1, 2, 4, 8, 3)
        End Sub

        ' Customer Attribute on Enum declaration
        <Fact>
        Public Sub AttributeOnEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
         Class Attr1
            Inherits System.Attribute
         End Class
         &lt;Attr1&gt; _
         Enum Figure
            One
            Two
            Three
         End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "Figure", 0, 1, 2)
        End Sub

        <Fact()>
        Public Sub ConvertOnEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
         Imports System
         Class c1
            Public Enum Suits
                ValueA = 1
                ValueB = 2
                ValueC = 4
                ValueD = 2
                ValueE = 2
            End Enum
            Shared Sub Main()
                Dim S As Suits = CType(2, Suits)
                Console.WriteLine(S = Suits.ValueB)
                Console.WriteLine(S = Suits.ValueE)
                Dim S1 As Suits = CType(-1, Suits)
                Console.WriteLine(S1.ToString())        ' 255
            End Sub
        End Class
    </file>
</compilation>

            VerifyEnumsValue(text, "c1.Suits", 1, 2, 4, 2, 2)

            Dim expectedOutput = <![CDATA[True
True
-1
]]>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(text, TestOptions.ReleaseExe)
            CompileAndVerify(comp, expectedOutput)
        End Sub

        ' Enum used in switch
        <Fact>
        Public Sub SwitchInEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
 Class c1
    Public Enum Suits
        ValueA = 2
        ValueB
        ValueC = 2
    End Enum
    Public Sub Main()
        Dim s As Suits
        Select Case s
            Case Suits.ValueA
                Exit Select
            Case Suits.ValueB
                Exit Select
            Case Suits.ValueC
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
 End Class
    </file>
</compilation>

            VerifyEnumsValue(text, "c1.Suits", 2, 3, 2)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseDeclarationDiagnostics(comp, <errors></errors>)
        End Sub

        ' The literal 0 implicitly converts to any enum type. 
        <Fact>
        Public Sub ZeroInEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Imports System
Class c1
    Private Enum Gender As Byte
        Male = 2
    End Enum

    Public Shared Sub Main(args As String())
        Dim s As Gender = 0
        Console.WriteLine(s)
        s = -0
        Console.WriteLine(s)
        s = 0.0
        Console.WriteLine(s)
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(text, expectedOutput:="0" & Environment.NewLine & "0" & Environment.NewLine & "0" & Environment.NewLine)
        End Sub

        ' Derived.
        <Fact>
        Public Sub BC30628ERR_StructCantInherit_DerivedFromEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Public Enum Suits
    ValueA = 2
    ValueB
    ValueC = 2
End Enum
Structure S1
    Inherits Suits
End Structure
Interface I1
    Inherits Suits
End Interface
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseDeclarationDiagnostics(comp, <errors>
BC30628: Structures cannot have 'Inherits' statements.
    Inherits Suits
    ~~~~~~~~~~~~~~
BC30354: Interface can inherit only from another interface.
    Inherits Suits
             ~~~~~
</errors>)
        End Sub

        ' Enums can Not be declared in nested enum declaration
        <WorkItem(539943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539943")>
        <Fact>
        Public Sub BC30619ERR_InvInsideEndsEnum_NestedFromEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Public Enum Num
    Enum Figure  
        Zero
    End Enum
End Enum
    </file>
</compilation>

            VerifyEnumsValue(text, "Num")
            VerifyEnumsValue(text, "Figure", 0)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseDiagnostics(comp, <errors>
BC30185: 'Enum' must end with a matching 'End Enum'.
Public Enum Num
~~~~~~~~~~~~~~~
BC30280: Enum 'Num' must contain at least one member.
Public Enum Num
            ~~~
BC30619: Statement cannot appear within an Enum body. End of Enum assumed.
    Enum Figure  
    ~~~~~~~~~~~
BC30184: 'End Enum' must be preceded by a matching 'Enum'.
End Enum
~~~~~~~~
</errors>)
        End Sub

        ' Enums can be declared anywhere
        <Fact>
        Public Sub DeclEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Namespace ns
    Enum Gender
        Male
    End Enum
End Namespace
Structure B
    Private Enum Gender
        Male
    End Enum
End Structure
    </file>
</compilation>

            VerifyEnumsValue(text, "ns.Gender", 0)
            VerifyEnumsValue(text, "B.Gender", 0)
        End Sub

        ' Enums obey local scope rules
        <Fact>
        Public Sub DeclEnum_01()
            Dim text =
<compilation name="C">
    <file name="a.vb">
        Namespace ns
            Enum E1
                yes = 1
                no = yes - 1
            End Enum
            Public Class mine
                Public Enum E1
                    yes = 1
                    no = yes - 1
                End Enum
            End Class
        End Namespace
    </file>
</compilation>

            VerifyEnumsValue(text, "ns.E1", 1, 0)
            VerifyEnumsValue(text, "ns.mine.E1", 1, 0)
        End Sub

        <Fact()>
        Public Sub NullableOfEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Enum EnumB As Long
    Num = 1000
End Enum
Class c1
    Public Shared Sub Main()
        Dim a As EnumB = 0
        Dim c As System.Nullable(Of EnumB) = Nothing
        a = CType(c, EnumB)
    End Sub
End Class
    </file>
</compilation>
            VerifyEnumsValue(text, "EnumB", 1000L)
            CompileAndVerify(text)
        End Sub

        ' Operator on null and enum 
        <Fact>
        Public Sub OperatorOnNullableAndEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
class c1
    Private e As MyEnum = Nothing And MyEnum.One
End Class
enum MyEnum
    One
End Enum
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseDeclarationDiagnostics(comp, <errors></errors>)
        End Sub

        ' Operator on enum 
        <Fact>
        Public Sub OperatorOnEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Class c1
    Public Shared Sub Main(args As String())
        Dim e1 As Enum1 = e1 + 5L
        Dim e2 As Enum2 = e1 + e2
        e1 = Enum1.A1 + Enum1.B1
        Dim b1 As Boolean = e1 = 1
        Dim b7 As Boolean = e1 = e2
        e1 += 1		' OK
        e2 -= 1		' OK
        e1 = e1 Xor Enum1.A1		' OK
        e1 = e1 Xor Enum1.B1		' OK
    End Sub
End Class
Public Enum Enum1
    A1 = 1
    B1 = 2
End Enum
Public Enum Enum2 As Byte
    A2
    B2
End Enum
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseDeclarationDiagnostics(comp, <errors></errors>)
        End Sub

        ' Operator on enum member 
        <Fact>
        Public Sub OperatorOnEnumMember()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Imports System
Class c1
    Public Shared Sub Main(args As String())
        Dim s As E = E.one
        Dim b1 = E.three &gt; E.two
        Dim b2 = E.three &lt; E.two
            Dim b3 = E.three = E.two
            Dim b4 = E.three &lt;&gt; E.two
            Dim b5 = s &gt; E.two
            Dim b6 = s &lt; E.two
            Dim b7 = s = E.two
            Dim b8 = s &lt;&gt; E.two
            Console.WriteLine(b1)
            Console.WriteLine(b2)
            Console.WriteLine(b3)
            Console.WriteLine(b4)
            Console.WriteLine(b5)
            Console.WriteLine(b6)
            Console.WriteLine(b7)
            Console.WriteLine(b8)
        End Sub
    End Class
    Public Enum E
        one = 1
        two = 2
        three = 3
    End Enum
    </file>
</compilation>

            CompileAndVerify(text, expectedOutput:="True" & Environment.NewLine & "False" & Environment.NewLine & "False" & Environment.NewLine & "True" & Environment.NewLine & "False" & Environment.NewLine & "True" & Environment.NewLine & "False" & Environment.NewLine & "True" & Environment.NewLine)
        End Sub

        ' CLS-Compliant
        <Fact>
        Public Sub CLSCompliantOnEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
&lt;Assembly: System.CLSCompliant(True)&gt;
Public Class c1
    Public Enum COLORS As UInteger
        RED
        GREEN
        BLUE
    End Enum
End Class
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            VerifyEnumsValue(comp, "c1.COLORS", SpecialType.System_UInt32, 0UI, 1UI, 2UI)
        End Sub

        ' No Base type after 'As' 
        <WorkItem(528031, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528031")>
        <Fact>
        Public Sub BC30182ERR_UnrecognizedType_NoUnderlyingTypeForEnum()
            Dim text =
<compilation name="C">
    <file name="a.vb">
    Public Enum Figure  As 
        One
        Two
        Three
    End Enum
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseParseDiagnostics(comp, <errors>
BC30182: Type expected.
Public Enum Figure  As 
                       ~
</errors>)
            VerifyEnumsValue(comp, "Figure", SpecialType.System_Int32, 0, 1, 2)
        End Sub

        ' All integral type could be as BASE type
        <WorkItem(539945, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539945")>
        <Fact>
        Public Sub BC30650ERR_InvalidEnumBase_BaseType()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Public Enum Figure As System.Int64
    One
    Two
    Three
End Enum
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseDeclarationDiagnostics(comp, <errors></errors>)
            VerifyEnumsValue(comp, "Figure", SpecialType.System_Int64, CLng(0), CLng(1), CLng(2))

            text =
<compilation name="C">
    <file name="a.vb">
Class C
End Class
Enum Figure As C
    One
    Two
    Three
End Enum
    </file>
</compilation>

            comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseDeclarationDiagnostics(comp, <errors>
BC30650: Enums must be declared as an integral type.
Enum Figure As C
               ~
</errors>)
            VerifyEnumsValue(comp, "Figure", SpecialType.System_Int32, 0, 1, 2)
        End Sub

        ' 'partial' as Enum name
        <Fact>
        Public Sub partialAsEnumName()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Partial Class EnumPartial
    Friend Enum [partial]
        ONE
    End Enum
    Dim M As [partial]
End Class
    </file>
</compilation>

            VerifyEnumsValue(text, "EnumPartial.partial", 0)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            Dim classEnum = TryCast(comp.SourceModule.GlobalNamespace.GetMembers("EnumPartial").Single(), NamedTypeSymbol)
            Dim member = TryCast(classEnum.GetMembers("M").Single(), FieldSymbol)
            Assert.Equal(TypeKind.Enum, member.Type.TypeKind)
        End Sub

        ' Enum as an optional parameter 
        <Fact()>
        Public Sub EnumAsOptionalParameter()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Enum ABC
    a
    b
    c
End Enum
Class c1
    Public Function Goo(Optional o As ABC = ABC.a Or ABC.b) As Integer
        Return 0
    End Function
    Public Function Moo(Optional o As Object = ABC.a) As Integer
        Return 1
    End Function
End Class
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseDeclarationDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(540427, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540427")>
        <Fact>
        Public Sub EnumInitializerCircularReference()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Enum Enum1
    A = B + 1
    B
End Enum
    </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlib40(text).VerifyDiagnostics(Diagnostic(ERRID.ERR_CircularEvaluation1, "A").WithArguments("A"))
        End Sub

        <WorkItem(540526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540526")>
        <Fact>
        Public Sub EnumBadMember()
            Dim text =
<compilation>
    <file name="a.vb">
Enum E
    [A
End Enum
    </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlib40(text).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_MissingEndEnum, "Enum E"),
                    Diagnostic(ERRID.ERR_InvInsideEndsEnum, ""),
                    Diagnostic(ERRID.ERR_MissingEndBrack, "[A"),
                    Diagnostic(ERRID.ERR_InvalidEndEnum, "End Enum"),
                    Diagnostic(ERRID.ERR_BadEmptyEnum1, "E").WithArguments("E"))
        End Sub

        <WorkItem(540526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540526")>
        <Fact>
        Public Sub EnumBadMember2()
            Dim text =
<compilation>
    <file name="a.vb">
Enum E
    goo:
End Enum
    </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlib40(text).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_InvInsideEnum, "goo:"))
        End Sub

        <WorkItem(540557, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540557")>
        <Fact>
        Public Sub EnumInDifferentFile()
            Dim text =
<compilation name="C">
    <file name="a.vb">
Imports System
Imports Color
Module M1
    Dim passed As Boolean
    Dim m_clr As Color
    Property Clr As Color
        Get
            Return m_clr
        End Get
        Set(value As Color)
            m_clr = value
        End Set
    End Property
End Module

    </file>
    <file name="color.vb">
Public Enum Color    
    red    
    green    
    blue
End Enum
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(text)
            CompilationUtils.AssertNoErrors(comp)

            Dim globalNS = comp.SourceModule.GlobalNamespace
            Dim M1 = DirectCast(globalNS.GetMembers("M1").First(), TypeSymbol)
            Dim Clr = DirectCast(M1.GetMembers("Clr").First(), PropertySymbol)
            Dim Color = Clr.Type
            Assert.Equal("Color", Color.Name)

        End Sub

        <Fact>
        Public Sub EnumMemberInitMustBeConstant()
            Dim text =
<compilation name="C">
    <file name="a.vb">
        Imports System

        Class A
            Public Const X As Integer = 1
        End Class

        Class B
            Sub New(x As Action)
            End Sub

            Sub New(x As Integer)
            End Sub

            Public Const X As Integer = 2
        End Class

        Class C
            Sub New(x As Integer)
            End Sub

            Public Const X As Integer = 3
        End Class

        Class D
            Sub New(x As Func(Of Integer))
            End Sub

            Public Const X As Integer = 4
        End Class

        Module M
            Public Enum Bar As Integer
                ValueWorks1 = new C(23).X
                ValueWorks2 = new A().X
                ValueWorks3 = 23 + new A().X
                ValueWorks4 = if(nothing, 23)
                ValueWorks5 = if(23 = 42, 23, 42)
                ValueWorks6 = if(new A().X = 0, 23, 42)
                ValueWorks7 = if(new A(), nothing).X
                ValueWorks8 = if(23 = 42, 23, new A().X)
                ValueWorks9 = if(23 = 42, new A().X, 42)
                ValueWorks10 = New B(Sub() Exit Sub).X
                ValueWorks11 = New D(Function() 23).X

                ValueDoesntWork1 = goo()                       
            End Enum

        Public Function goo() As Integer
            Return 23
        End Function

        public sub main()
        end sub
    End Module
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(text)
            CompilationUtils.AssertTheseDiagnostics(comp, <errors>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueWorks1 = new C(23).X
                              ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueWorks2 = new A().X
                              ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueWorks3 = 23 + new A().X
                                   ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueWorks6 = if(new A().X = 0, 23, 42)
                                 ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueWorks7 = if(new A(), nothing).X
                              ~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueWorks8 = if(23 = 42, 23, new A().X)
                                              ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueWorks9 = if(23 = 42, new A().X, 42)
                                          ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueWorks10 = New B(Sub() Exit Sub).X
                               ~~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueWorks11 = New D(Function() 23).X
                               ~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
                ValueDoesntWork1 = goo()                       
                                   ~~~~~
</errors>)
        End Sub

        ''' bug 8151
        <Fact>
        Public Sub EnumMemberWithNonConstInitializationAndSelfDependency()
            Dim text =
<compilation name="C">
    <file name="a.vb">
        Imports System

        Class D
            Sub New(x As Func(Of Integer))
            End Sub

            Public Const X As Integer = 4
        End Class

        Module M
            Public Enum Bar As Integer
                ValueDoesntWork2
                ValueDoesntWork3 = New D(Function() ValueDoesntWork2).X
                ValueDoesntWork4 = New D(Function() ValueDoesntWork4).X
            End Enum

        public sub main()
        end sub
    End Module
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(text)
            CompilationUtils.AssertTheseDiagnostics(comp, <errors>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                ValueDoesntWork3 = New D(Function() ValueDoesntWork2).X
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30500: Constant 'ValueDoesntWork4' cannot depend on its own value.
                ValueDoesntWork4 = New D(Function() ValueDoesntWork4).X
                ~~~~~~~~~~~~~~~~
                                                     </errors>)
        End Sub

        ' The value can be used off an enum member 
        <WorkItem(541364, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541364")>
        <Fact>
        Public Sub EnumUseQualified()
            Dim text =
<compilation name="C">
    <file name="a.vb">
            Enum Y
                X
                Y = Y.X
            End Enum
    </file>
</compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            CompilationUtils.AssertTheseDiagnostics(comp, <errors>
                                                          </errors>)

            VerifyEnumsValue(text, "Y", 0, 0)
        End Sub

        <WorkItem(750553, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750553")>
        <Fact>
        Public Sub InvalidEnumUnderlyingType()
            Dim text =
<compilation>
    <file name="a.vb">
Class C(Of T As Structure)
    Enum E As T
        A
    End Enum
End Class
    </file>
</compilation>
            Dim errors =
<errors>
BC30650: Enums must be declared as an integral type.
    Enum E As T
              ~
</errors>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            comp.AssertTheseDiagnostics(errors)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim diagnostics = model.GetDeclarationDiagnostics()
            AssertTheseDiagnostics(diagnostics, errors)
            Dim decl = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of EnumBlockSyntax).Single()
            Dim symbol = model.GetDeclaredSymbol(decl)
            Dim type = symbol.EnumUnderlyingType
            Assert.Equal(type.SpecialType, SpecialType.System_Int32)
        End Sub

        <Fact, WorkItem(895284, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/895284")>
        Public Sub CircularDefinition_Explicit()
            ' Bug#895284 Roslyn gives extra error BC30060: 
            '      Conversion from 'E2' to 'Integer' cannot occur in a constant expression.
            ' Per field 
            Dim source =
<compilation>
    <file name="a.vb">
Enum E1
    M10 = 1
    M11 = CType(M10, Integer) + 1
    M12 = CType(M11, Integer) + 1
End Enum
Enum E2
    M20 = CType(M22, Integer) + 1
    M21 = CType(M20, Integer) + 1
    M22 = CType(M21, Integer) + 1
End Enum
    </file>
</compilation>
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(source)
            comp.AssertTheseDiagnostics(<errors>
BC30500: Constant 'M20' cannot depend on its own value.
    M20 = CType(M22, Integer) + 1
    ~~~
BC30060: Conversion from 'E2' to 'Integer' cannot occur in a constant expression.
    M21 = CType(M20, Integer) + 1
                ~~~
BC30060: Conversion from 'E2' to 'Integer' cannot occur in a constant expression.
    M22 = CType(M21, Integer) + 1
                ~~~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub CircularDefinitionManyMembers_Implicit()
            ' Enum E
            '     M0 = Mn + 1
            '     M1
            '     ...
            '     Mn
            ' End Enum
            Dim source = GenerateEnum(6000, Function(i, n) If(i = 0, String.Format("M{0} + 1", n - 1), ""))
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(source)
            comp.AssertTheseDiagnostics(<errors>
BC30500: Constant 'M0' cannot depend on its own value.
    M0 = M5999 + 1
    ~~
</errors>)
        End Sub

        <Fact,
         WorkItem(123937, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=123937"),
         WorkItem(886047, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/886047")>
        Public Sub CircularDefinitionManyMembers_Explicit()
            ' Enum E
            '     M0 = Mn + 1
            '     M1 = M0 + 1
            '     ...
            '     Mn = Mn-1 + 1
            ' End Enum
            ' Dev12 crashes at ~300 members.
            Const bug123937IsFixed = False
            Dim count As Integer = 2
            If bug123937IsFixed Then
                count = 6000
            End If

            Dim source = GenerateEnum(count, Function(i, n) String.Format("M{0} + 1", If(i = 0, n - 1, i - 1)))
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(source)

            ' Note, native compiler doesn't report BC30060, we should try to suppress it too.
            comp.AssertTheseDiagnostics(<errors>
BC30500: Constant 'M0' cannot depend on its own value.
    M0 = M1 + 1
    ~~
BC30060: Conversion from 'E' to 'Integer' cannot occur in a constant expression.
    M1 = M0 + 1
         ~~
</errors>)
        End Sub

        <Fact,
          WorkItem(123937, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=123937"),
         WorkItem(886047, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/886047")>
        Public Sub InvertedDefinitionManyMembers_Explicit()
            ' Enum E
            '     M0 = M1 - 1
            '     M1 = M2 - 1
            '     ...
            '     Mn = n
            ' End Enum
            ' Dev12 crashes at ~300 members.
            Const bug123937IsFixed = False
            Dim count As Integer = 20
            If bug123937IsFixed Then
                count = 6000
            End If

            Dim source = GenerateEnum(count, Function(i, n) If(i < n - 1, String.Format("M{0} - 1", i + 1), i.ToString()))
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(source)
            comp.AssertTheseDiagnostics(<errors/>)
        End Sub

        ''' <summary>
        ''' Generate:
        ''' <code>
        ''' Enum E
        '''     M0 = ...
        '''     M1 = ...
        '''     ...
        '''     Mn = ...
        ''' End Enum
        ''' </code>
        ''' </summary>
        Private Shared Function GenerateEnum(n As Integer, getMemberValue As Func(Of Integer, Integer, String)) As XElement
            Dim builder As New StringBuilder()
            builder.AppendLine("Enum E")
            For i = 0 To n - 1
                builder.Append(String.Format("    M{0}", i))
                Dim value = getMemberValue(i, n)
                If Not String.IsNullOrEmpty(value) Then
                    builder.Append(" = ")
                    builder.Append(value)
                End If
                builder.AppendLine()
            Next
            builder.AppendLine("End Enum")
            Return <compilation>
                       <file name="a.vb"><%= builder.ToString() %></file>
                   </compilation>
        End Function

        Private Shared Function VerifyEnumsValue(text As XElement, enumName As String, ParamArray expectedEnumValues As Object()) As List(Of Symbol)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            Return VerifyEnumsValue(comp, enumName, If(expectedEnumValues.Any() AndAlso expectedEnumValues.First().GetType() Is GetType(Long), SpecialType.System_Int64, SpecialType.System_Int32), expectedEnumValues)
        End Function

        Private Shared Function VerifyEnumsValue(text As XElement, enumName As String, underlyingType As SpecialType, ParamArray expectedEnumValues As Object()) As List(Of Symbol)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(text)
            Return VerifyEnumsValue(comp, enumName, underlyingType, expectedEnumValues)
        End Function

        Private Shared Function VerifyEnumsValue(comp As VisualBasicCompilation, enumName As String, underlyingType As SpecialType, ParamArray expectedEnumValues As Object()) As List(Of Symbol)
            Dim symEnum = TryCast(GetSymbolByFullName(comp, enumName), NamedTypeSymbol)
            Assert.NotNull(symEnum)

            Dim type = symEnum.EnumUnderlyingType
            Assert.NotNull(type)
            Assert.Equal(underlyingType, type.SpecialType)

            Dim fields = symEnum.GetMembers().OfType(Of FieldSymbol).Cast(Of Symbol)()
            Assert.Equal(expectedEnumValues.Length, fields.Count - 1)
            For count = 0 To fields.Count - 1
                Dim field = DirectCast(fields(count), FieldSymbol)
                Dim fieldDefinition = DirectCast(field.GetCciAdapter(), Cci.IFieldDefinition)
                If count = 0 Then
                    Assert.Equal(field.Name, "value__")
                    Assert.False(field.IsShared)
                    Assert.False(field.IsConst)
                    Assert.False(field.IsReadOnly)
                    Assert.Equal(field.DeclaredAccessibility, Accessibility.Public)
                    Assert.Equal(field.Type.SpecialType, underlyingType)
                    Assert.True(fieldDefinition.IsSpecialName)
                    Assert.True(fieldDefinition.IsRuntimeSpecial)
                Else
                    Assert.Equal(expectedEnumValues(count - 1), field.ConstantValue)
                    Assert.True(field.IsShared)
                    Assert.True(field.IsConst)
                    Assert.False(fieldDefinition.IsSpecialName)
                    Assert.False(fieldDefinition.IsRuntimeSpecial)
                End If
            Next

            Return fields.ToList()
        End Function

        Private Shared Function GetSymbolByFullName(compilation As VisualBasicCompilation, memberName As String) As Symbol
            Dim names As String() = memberName.Split("."c)
            Dim currentSymbol As Symbol = compilation.GlobalNamespace
            For Each name In names
                Assert.True(TypeOf currentSymbol Is NamespaceOrTypeSymbol, String.Format("{0} does not have members", currentSymbol.ToDisplayString()))
                Dim currentContainer = DirectCast(currentSymbol, NamespaceOrTypeSymbol)
                Dim members = currentContainer.GetMembers(name)
                Assert.True(members.Length > 0, String.Format("No members named {0} inside {1}", name, currentSymbol.ToDisplayString()))
                Assert.True(members.Length <= 1, String.Format("Multiple members named {0} inside {1}", name, currentSymbol.ToDisplayString()))
                currentSymbol = members.First()
            Next
            Return currentSymbol
        End Function

        <WorkItem(45625, "https://github.com/dotnet/roslyn/issues/45625")>
        <Fact>
        Public Sub UseSiteError_01()
            Dim sourceA =
"
public class A
End Class
"
            Dim comp = CreateCompilation(sourceA, assemblyName:="UseSiteError_sourceA")
            Dim refA = comp.EmitToImageReference()

            Dim sourceB =
"
public class B( Of T)
    public enum E
        F
    end enum
end class
public class C
    public const F as B(Of A).E = Nothing
end class
"
            comp = CreateCompilation(sourceB, references:={refA})
            Dim refB = comp.EmitToImageReference()

            Dim sourceC =
"
class Program
    Shared Sub Main()
        const x As Integer = CType(Not C.F, Integer)
        System.Console.WriteLine(x)
    End Sub
end class
"
            comp = CreateCompilation(sourceC, references:={refB})
            comp.AssertTheseDiagnostics(
<expected>
BC30652: Reference required to assembly 'UseSiteError_sourceA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'A'. Add one to your project.
        const x As Integer = CType(Not C.F, Integer)
                                       ~~~
</expected>)

            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim expr = tree.GetRoot().DescendantNodes().Single(Function(n) n.Kind() = SyntaxKind.NotExpression)
            Dim value = model.GetConstantValue(expr)
            Assert.True(value.HasValue)
            Assert.Equal(-1, value.Value)
        End Sub

        <WorkItem(45625, "https://github.com/dotnet/roslyn/issues/45625")>
        <Fact>
        Public Sub UseSiteError_02()
            Dim sourceA =
"
public class A
End Class
"
            Dim comp = CreateCompilation(sourceA, assemblyName:="UseSiteError_sourceA")
            Dim refA = comp.EmitToImageReference()

            Dim sourceB =
"
public class B( Of T)
    public enum E
        F
    end enum
end class
public class C
    public const F as B(Of A).E = Nothing
end class
"
            comp = CreateCompilation(sourceB, references:={refA})
            Dim refB = comp.EmitToImageReference()

            Dim sourceC =
"
Option Infer On 
class Program
    Shared Sub Main()
        Dim x = Not C.F
        System.Console.WriteLine(x)
    End Sub
end class
"
            comp = CreateCompilation(sourceC, references:={refB}, options:=TestOptions.ReleaseExe)
            comp.AssertTheseDiagnostics(
<expected>
BC30652: Reference required to assembly 'UseSiteError_sourceA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'A'. Add one to your project.
        Dim x = Not C.F
                    ~~~
</expected>)

            comp = CreateCompilation(sourceC, references:={refB}, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics(
<expected>
BC30652: Reference required to assembly 'UseSiteError_sourceA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'A'. Add one to your project.
        Dim x = Not C.F
                    ~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(50163, "https://github.com/dotnet/roslyn/issues/50163")>
        Public Sub LongDependencyChain()
            Dim text As New StringBuilder()

            text.AppendLine(
"
Enum Test
    Item0 = 1
")
            For i As Integer = 1 To 2000
                text.AppendLine()
                text.AppendFormat("    Item{1} = Item{0} + 1", i - 1, i)
            Next

            text.AppendLine(
"
End Enum
")

            Dim comp = CreateCompilation(text.ToString())
            Dim item2000 = comp.GetMember(Of FieldSymbol)("Test.Item2000")
            Assert.Equal(2001, item2000.ConstantValue)
        End Sub

        <Fact>
        <WorkItem(52624, "https://github.com/dotnet/roslyn/issues/52624")>
        Public Sub Issue52624()
            Dim source1 =
"
Public Enum SyntaxKind As UShort
    None = 0
    List = GreenNode.ListKind
End Enum
"
            Dim source2 =
"
Friend Class GreenNode
    Public Const ListKind = 1
End Class
"

            For i As Integer = 1 To 1000
                Dim comp = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll)
                comp.VerifyDiagnostics()

                Dim listKind = comp.GlobalNamespace.GetMember(Of FieldSymbol)("GreenNode.ListKind")
                Assert.Equal(1, listKind.ConstantValue)
                Assert.Equal("System.Int32", listKind.Type.ToTestDisplayString())

                Dim list = comp.GlobalNamespace.GetMember(Of FieldSymbol)("SyntaxKind.List")
                Assert.Equal(1US, list.ConstantValue)
                Assert.Equal("SyntaxKind", list.Type.ToTestDisplayString())
            Next
        End Sub
    End Class

End Namespace
