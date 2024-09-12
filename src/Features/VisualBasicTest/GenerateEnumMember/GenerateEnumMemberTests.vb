' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEnumMember
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateEnumMember
    <Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
    Public Class GenerateEnumMemberTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New GenerateEnumMemberCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestGenerateIntoEmpty() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Red|])
    End Sub
End Module
Enum Color
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Red)
    End Sub
End Module
Enum Color
    Red
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoEnumWithSingleMember() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Green|])
    End Sub
End Module
Enum Color
    Red
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Green)
    End Sub
End Module
Enum Color
    Red
    Green
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateAfterEnumWithValue() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Green|])
    End Sub
End Module
Enum Color
    Red = 1
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Green)
    End Sub
End Module
Enum Color
    Red = 1
    Green = 2
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateBinaryLiteral() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Green|])
    End Sub
End Module
Enum Color
    Red = &B01
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Green)
    End Sub
End Module
Enum Color
    Red = &B01
    Green = &B10
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoLinearIncreasingSequence() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Blue|])
    End Sub
End Module
Enum Color
    Red = 1
    Green = 2
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Blue)
    End Sub
End Module
Enum Color
    Red = 1
    Green = 2
    Blue = 3
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoGeometricSequence() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Purple|])
    End Sub
End Module
Enum Color
    Red = 1
    Green = 2
    Blue = 4
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Purple)
    End Sub
End Module
Enum Color
    Red = 1
    Green = 2
    Blue = 4
    Purple = 8
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540540")>
        Public Async Function TestGenerateAfterEnumWithIntegerMaxValue() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Blue|])
    End Sub
End Module
Enum Color
    Red
    Green = Integer.MaxValue
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Blue)
    End Sub
End Module
Enum Color
    Red
    Green = Integer.MaxValue
    Blue = Integer.MinValue
End Enum")
        End Function

        <Fact>
        Public Async Function TestUnsigned16BitEnums() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|Color.Green|]
    End Sub
End Module
Enum Color As UShort
    Red = 65535
End Enum",
"Module Program
    Sub Main(args As String())
        Color.Green
    End Sub
End Module
Enum Color As UShort
    Red = 65535
    Green = 0
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540546")>
        Public Async Function TestGenerateEnumMemberOfTypeLong() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Blue|])
    End Sub
End Module
Enum Color As Long
    Red = Long.MinValue
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Blue)
    End Sub
End Module
Enum Color As Long
    Red = Long.MinValue
    Blue = -9223372036854775807
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540636")>
        Public Async Function TestGenerateAfterEnumWithLongMaxValueInHex() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As Long
    Red = &H7FFFFFFFFFFFFFFF
End Enum",
"Class Program
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As Long
    Red = &H7FFFFFFFFFFFFFFF
    Blue = &H8000000000000000
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540638")>
        Public Async Function TestGenerateAfterEnumWithLongMinValueInHex() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As Long
    Red = &H8000000000000000
End Enum",
"Class Program
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As Long
    Red = &H8000000000000000
    Blue = &H8000000000000001
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateAfterNegativeLongInHex() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As Long
    Red = &HFFFFFFFFFFFFFFFF
End Enum",
"Class Program
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As Long
    Red = &HFFFFFFFFFFFFFFFF
    Blue = &H0
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateAfterPositiveLongInHex() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Color.Orange|]
    End Sub
End Class
Enum Color As Long
    Red = &HFFFFFFFFFFFFFFFF
    Blue = &H0
End Enum",
"Class Program
    Sub Main(args As String())
        Color.Orange
    End Sub
End Class
Enum Color As Long
    Red = &HFFFFFFFFFFFFFFFF
    Blue = &H0
    Orange = &H1
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateAfterPositiveLongExprInHex() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As Long
    Red = &H414 / 2
End Enum",
"Class Program
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As Long
    Red = &H414 / 2
    Blue = 523
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540632")>
        Public Async Function TestGenerateAfterEnumWithULongMaxValue() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As ULong
    Red = ULong.MaxValue
End Enum",
"Class A
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As ULong
    Red = ULong.MaxValue
    Blue = 0
End Enum")
        End Function

        <Fact>
        Public Async Function TestNegativeRangeIn64BitSignedEnums() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|Color.Green|]
    End Sub
End Module
Enum Color As Long
    Red = -10
End Enum",
"Module Program
    Sub Main(args As String())
        Color.Green
    End Sub
End Module
Enum Color As Long
    Red = -10
    Green = -9
End Enum")
        End Function

        <Fact>
        Public Async Function TestUnaryMinusOnUInteger1() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As UInteger
    Red = -(0 - UInteger.MaxValue)
End Enum",
"Class A
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As UInteger
    Red = -(0 - UInteger.MaxValue)
    Blue = 0
End Enum")
        End Function

        <Fact>
        Public Async Function TestDoubleUnaryMinusOnUInteger() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As UInteger
    Red = --(UInteger.MaxValue - 1)
End Enum",
"Class A
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As UInteger
    Red = --(UInteger.MaxValue - 1)
    Blue = UInteger.MaxValue
End Enum")
        End Function

        <Fact>
        Public Async Function TestDoubleUnaryMinusOnUInteger1() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Green|]
    End Sub
End Class
Enum Color As UInteger
    Red = --(UInteger.MaxValue - 1)
    Blue = 4294967295
End Enum",
"Class A
    Sub Main(args As String())
        Color.Green
    End Sub
End Class
Enum Color As UInteger
    Red = --(UInteger.MaxValue - 1)
    Blue = 4294967295
    Green = 0
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateWithImplicitValues() As Task
            ' Red is implicitly assigned to 0, Green is implicitly Red + 1, So Blue must be 2.
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Module
Enum Color
    Red
    Green
    Yellow = -1
End Enum",
"Module Program
    Sub Main(args As String())
        Color.Blue
    End Sub
End Module
Enum Color
    Red
    Green
    Yellow = -1
    Blue = 2
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateWithImplicitValues2() As Task
            Await TestInRegularAndScriptAsync(
"Class B
    Sub Main(args As String())
        [|Color.Grey|]
    End Sub
End Class
Enum Color
    Red
    Green = 10
    Blue
End Enum",
"Class B
    Sub Main(args As String())
        Color.Grey
    End Sub
End Class
Enum Color
    Red
    Green = 10
    Blue
    Grey
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540549")>
        Public Async Function TestNoExtraneousStatementTerminatorBeforeCommentedMember() As Task
            Dim code = <Text>Module Program
    Sub Main(args As String())
        Goo([|Color.Blue|])
    End Sub
End Module

Enum Color
    Red
    'Blue
End Enum</Text>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <Text>Module Program
    Sub Main(args As String())
        Goo(Color.Blue)
    End Sub
End Module

Enum Color
    Red
    Blue
    'Blue
End Enum</Text>.Value.Replace(vbLf, vbCrLf)

            Await TestInRegularAndScriptAsync(code,
                    expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540552")>
        Public Async Function TestGenerateAfterEnumWithMinValue() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Blue|])
    End Sub
End Module
Enum Color
    Red = Integer.MinValue
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Blue)
    End Sub
End Module
Enum Color
    Red = Integer.MinValue
    Blue = -2147483647
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540553")>
        Public Async Function TestGenerateAfterEnumWithMinValuePlusConstant() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Blue|])
    End Sub
End Module
Enum Color
    Red = Integer.MinValue + 100
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Blue)
    End Sub
End Module
Enum Color
    Red = Integer.MinValue + 100
    Blue = -2147483547
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540556")>
        Public Async Function TestGenerateAfterEnumWithByteMaxValue() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Blue|])
    End Sub
End Module
Enum Color As Byte
    Red = 255
End Enum",
"Module Program
    Sub Main(args As String())
        Goo(Color.Blue)
    End Sub
End Module
Enum Color As Byte
    Red = 255
    Blue = 0
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoNegativeSByteInOctal() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As SByte
    Red = &O1777777777777777777777
End Enum",
"Class A
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As SByte
    Red = &O1777777777777777777777
    Blue = &O0
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoPositiveSByteInOctal1() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As SByte
    Red = &O0
End Enum",
"Class A
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As SByte
    Red = &O0
    Blue = &O1
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoPositiveSByteInOctal2() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As SByte
    Red = &O176
End Enum",
"Class A
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As SByte
    Red = &O176
    Blue = &O177
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540631")>
        Public Async Function TestGenerateAfterEnumWithSByteMaxValueInOctal() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As SByte
    Red = &O177
End Enum",
"Class A
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As SByte
    Red = &O177
    Blue = &O1777777777777777777600
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528207")>
        Public Async Function TestAbsenceOfFixWhenImportingEnums() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports Color
Module Program
    Sub Main(args As String())
        Goo([|Blue|])
    End Sub
End Module
Enum Color As Byte
    Red
End Enum")
        End Function

        <Fact>
        Public Async Function TestPresenceOfFixWhenImportingEnumsYetFullyQualifyingThem() As Task
            Await TestInRegularAndScriptAsync(
"Imports Color
Module Program
    Sub Main(args As String())
        Goo([|Color.Green|])
    End Sub
End Module
Enum Color As Long
    Red = -10
End Enum",
"Imports Color
Module Program
    Sub Main(args As String())
        Goo(Color.Green)
    End Sub
End Module
Enum Color As Long
    Red = -10
    Green = -9
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540585")>
        Public Async Function TestGenerateIntoBitshiftEnum() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Blue|])
    End Sub
    Enum Color
        Red = 1 << 0
        Green = 1 << 1
        Orange = 1 << 2
    End Enum
End Module",
"Module Program
    Sub Main(args As String())
        Goo(Color.Blue)
    End Sub
    Enum Color
        Red = 1 << 0
        Green = 1 << 1
        Orange = 1 << 2
        Blue = 1 << 3
    End Enum
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540566")>
        Public Async Function TestKeywordName() As Task
            Await TestInRegularAndScriptAsync(
"Imports Color
Module Program
    Sub Main(args As String())
        Goo([|Color.Enum|])
    End Sub
End Module
Enum Color As Byte
    Red
End Enum",
"Imports Color
Module Program
    Sub Main(args As String())
        Goo(Color.Enum)
    End Sub
End Module
Enum Color As Byte
    Red
    [Enum]
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540547")>
        Public Async Function TestStandaloneReference() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Module
Enum Color As Integer
    Red = Integer.MinValue
    Green = 1
End Enum",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Color.Blue
    End Sub
End Module
Enum Color As Integer
    Red = Integer.MinValue
    Green = 1
    Blue = 2
End Enum")
        End Function

        <Fact>
        Public Async Function TestCircularEnumsForErrorTolerance() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|Circular.C|]
    End Sub
End Module
Enum Circular
    A = B
    B
End Enum",
"Module Program
    Sub Main(args As String())
        Circular.C
    End Sub
End Module
Enum Circular
    A = B
    B
    C
End Enum")
        End Function

        <Fact>
        Public Async Function TestEnumWithIncorrectValueForErrorTolerance() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|Color.Green|]
    End Sub
End Module
Enum Color As Byte
    Red = -2
End Enum",
"Module Program
    Sub Main(args As String())
        Color.Green
    End Sub
End Module
Enum Color As Byte
    Red = -2
    Green
End Enum")
        End Function

        <Fact>
        Public Async Function TestHexValues() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|RenderType.LastViewedPage|]
    End Sub
End Module
<FlagsAttribute()>
Enum RenderType
    None = &H0
    DataUri = &H1
    GZip = &H2
    ContentPage = &H4
    ViewPage = &H8
    HomePage = &H10
End Enum",
"Module Program
    Sub Main(args As String())
        RenderType.LastViewedPage
    End Sub
End Module
<FlagsAttribute()>
Enum RenderType
    None = &H0
    DataUri = &H1
    GZip = &H2
    ContentPage = &H4
    ViewPage = &H8
    HomePage = &H10
    LastViewedPage = &H20
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoShadowedEnum() As Task
            Await TestInRegularAndScriptAsync(
"Class B
    Inherits A
    Sub Main(args As String())
        [|BaseColors.Blue|]
    End Sub
    Shadows Enum BaseColors
        Orange = 3
    End Enum
End Class
Public Class A
    Public Enum BaseColors
        Red = 1
        Green = 2
    End Enum
End Class",
"Class B
    Inherits A
    Sub Main(args As String())
        BaseColors.Blue
    End Sub
    Shadows Enum BaseColors
        Orange = 3
        Blue = 4
    End Enum
End Class
Public Class A
    Public Enum BaseColors
        Red = 1
        Green = 2
    End Enum
End Class")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoDerivedEnumMissingShadowsKeyword() As Task
            Await TestInRegularAndScriptAsync(
"Class B
    Inherits A
    Sub Main(args As String())
        [|BaseColors.Blue|]
    End Sub
    Enum BaseColors
        Orange = 3
    End Enum
End Class
Public Class A
    Public Enum BaseColors
        Red = 1
        Green = 2
    End Enum
End Class",
"Class B
    Inherits A
    Sub Main(args As String())
        BaseColors.Blue
    End Sub
    Enum BaseColors
        Orange = 3
        Blue = 4
    End Enum
End Class
Public Class A
    Public Enum BaseColors
        Red = 1
        Green = 2
    End Enum
End Class")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoBaseEnum() As Task
            Await TestInRegularAndScriptAsync(
"Class B
    Inherits A
    Sub Main(args As String())
        [|BaseColors.Blue|]
    End Sub
End Class
Public Class A
    Public Enum BaseColors
        Red = 1
        Green = 2
    End Enum
End Class",
"Class B
    Inherits A
    Sub Main(args As String())
        BaseColors.Blue
    End Sub
End Class
Public Class A
    Public Enum BaseColors
        Red = 1
        Green = 2
        Blue = 3
    End Enum
End Class")
        End Function

        <Fact>
        Public Async Function TestErrorToleranceWithStrictSemantics() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class B
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As Long
    Red = 1.5
    Green = 2.3
    Orange = 3.3
End Enum",
"Option Strict On
Class B
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As Long
    Red = 1.5
    Green = 2.3
    Orange = 3.3
    Blue = 4
End Enum")
        End Function

        <Fact>
        Public Async Function TestGeometricSequenceWithTypeConversions() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class B
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As Long
    Red = CLng(1.5)
    Green = CLng(2.3)
    Orange = CLng(3.9)
End Enum",
"Option Strict On
Class B
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As Long
    Red = CLng(1.5)
    Green = CLng(2.3)
    Orange = CLng(3.9)
    Blue = 8
End Enum")
        End Function

        <Fact>
        Public Async Function TestLinearSequenceWithTypeConversions() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class B
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As Long
    Red = CLng(1.5)
    Green = CLng(2.3)
    Orange = CLng(4.9)
End Enum",
"Option Strict On
Class B
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As Long
    Red = CLng(1.5)
    Green = CLng(2.3)
    Orange = CLng(4.9)
    Blue = 6
End Enum")
        End Function

        <Fact>
        Public Async Function TestGenerationWhenMembersShareValues() As Task
            Await TestInRegularAndScriptAsync(
"Option Strict On
Class B
    Sub Main(args As String())
        [|Color.Grey|]
    End Sub
End Class
Enum Color
    Red
    Green
    Blue
    Max = Blue
End Enum",
"Option Strict On
Class B
    Sub Main(args As String())
        Color.Grey
    End Sub
End Class
Enum Color
    Red
    Green
    Blue
    Max = Blue
    Grey = 3
End Enum")
        End Function

        <Fact>
        Public Async Function TestInvokeFromAddAssignmentStatement() As Task
            Await TestInRegularAndScriptAsync(
"Class B
    Sub Main(args As String())
        Dim a As Integer = 1
        a += [|Color.Grey|]
    End Sub
End Class
Enum Color
    Red
    Green = 10
    Blue
End Enum",
"Class B
    Sub Main(args As String())
        Dim a As Integer = 1
        a += Color.Grey
    End Sub
End Class
Enum Color
    Red
    Green = 10
    Blue
    Grey
End Enum")
        End Function

        <Fact>
        Public Async Function TestMissingOnEnumsFromMetaData() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports Microsoft.VisualBasic
Module Program
    Sub Main(args As String())
        Dim a = [|FirstDayOfWeek.EigthDay|]
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540638")>
        Public Async Function TestMaxHex() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class

Enum Color As Long
    Red = &H8000000000000000
End Enum",
"Class Program
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class

Enum Color As Long
    Red = &H8000000000000000
    Blue = &H8000000000000001
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540636")>
        Public Async Function TestMinHex() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As Long
    Red = &H7FFFFFFFFFFFFFFF
End Enum",
"Class Program
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As Long
    Red = &H7FFFFFFFFFFFFFFF
    Blue = &H8000000000000000
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540631")>
        Public Async Function TestOctalBounds1() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As SByte
    Red = &O177
End Enum",
"Class A
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As SByte
    Red = &O177
    Blue = &O1777777777777777777600
End Enum")
        End Function

        <Fact>
        Public Async Function TestULongMax() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Class
Enum Color As ULong
    Red = ULong.MaxValue
End Enum",
"Class A
    Sub Main(args As String())
        Color.Blue
    End Sub
End Class
Enum Color As ULong
    Red = ULong.MaxValue
    Blue = 0
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540604")>
        Public Async Function TestWrapAround1() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|Color.Blue|]
    End Sub
End Module
Enum Color As Integer
    Green = Integer.MaxValue
    Orange = -2147483648
End Enum",
"Module Program
    Sub Main(args As String())
        Color.Blue
    End Sub
End Module
Enum Color As Integer
    Green = Integer.MaxValue
    Orange = -2147483648
    Blue = -2147483647
End Enum")
        End Function

        <Fact>
        Public Async Function TestMissingOnHiddenEnum() As Task
            Await TestMissingInRegularAndScriptAsync(
"#ExternalSource (""Default.aspx"", 1) 
Imports System
Enum E
#End ExternalSource
End Enum
#ExternalSource (""Default.aspx"", 2) 
Class C
    Sub Goo()
        Console.Write([|E.x|])
    End Sub
End Class
#End ExternalSource")
        End Function

        <Fact>
        Public Async Function TestMissingOnPartiallyHiddenEnum() As Task
            Await TestMissingInRegularAndScriptAsync(
"#ExternalSource (""Default.aspx"", 1) 
Imports System
Enum E
    A
    B
    C
#End ExternalSource
End Enum
#ExternalSource (""Default.aspx"", 2) 
Class C
    Sub Goo()
        Console.Write([|E.x|])
    End Sub
End Class
#End ExternalSource")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544656")>
        Public Async Function TestShortHexidecimalLiterals() As Task
            Await TestInRegularAndScriptAsync(
"Module M
    Dim y = [|E.Y|] ' Generate Y 
End Module
Enum E As Short
    X = &H4000S
End Enum",
"Module M
    Dim y = E.Y ' Generate Y 
End Module
Enum E As Short
    X = &H4000S
    Y = &H4001S
End Enum")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545937")>
        Public Async Function TestUShortEnums() As Task
            Await TestInRegularAndScriptAsync(
"Module M
    Dim y = [|E.Y|] ' Generate Y 
End Module

Enum E As UShort
    X = &H4000US
End Enum",
"Module M
    Dim y = E.Y ' Generate Y 
End Module

Enum E As UShort
    X = &H4000US
    Y = &H8000US
End Enum")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49679")>
        Public Async Function TestWithLeftShift_Long() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Color.Blue|])
    End Sub
    Enum Color
        Green = 1L << 0
    End Enum
End Module",
"Module Program
    Sub Main(args As String())
        Goo(Color.Blue)
    End Sub
    Enum Color
        Green = 1L << 0
        Blue = 1L << 1
    End Enum
End Module")
        End Function
    End Class
End Namespace
