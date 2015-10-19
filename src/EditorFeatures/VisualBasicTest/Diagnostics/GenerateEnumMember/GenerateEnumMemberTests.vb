' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEnumMember
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateEnumMember
    Public Class GenerateEnumMemberTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateEnumMemberCodeFixProvider())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoEmpty()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Red|]) \n End Sub \n End Module \n Enum Color \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Red) \n End Sub \n End Module \n Enum Color \n Red \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoEnumWithSingleMember()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Green|]) \n End Sub \n End Module \n Enum Color \n Red \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Green) \n End Sub \n End Module \n Enum Color \n Red \n Green \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterEnumWithValue()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Green|]) \n End Sub \n End Module \n Enum Color \n Red = 1 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Green) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoLinearIncreasingSequence()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n Blue = 3 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoGeometricSequence()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Purple|]) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n Blue = 4 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Purple) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n Blue = 4 \n Purple = 8 \n End Enum"))
        End Sub

        <WorkItem(540540)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterEnumWithIntegerMaxValue()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color \n Red \n Green = Integer.MaxValue \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color \n Red  \n Green = Integer.MaxValue \n Blue = Integer.MinValue \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestUnsigned16BitEnums()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Green|] \n End Sub \n End Module \n Enum Color As UShort \n Red = 65535 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Green \n End Sub \n End Module \n Enum Color As UShort \n Red = 65535 \n Green = 0 \n End Enum"))
        End Sub

        <WorkItem(540546)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateEnumMemberOfTypeLong()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color As Long \n Red = Long.MinValue \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color As Long \n Red = Long.MinValue \n Blue = -9223372036854775807 \n End Enum"))
        End Sub

        <WorkItem(540636)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterEnumWithLongMaxValueInHex()
            Test(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &H7FFFFFFFFFFFFFFF \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H7FFFFFFFFFFFFFFF \n Blue = &H8000000000000000 \n End Enum"))
        End Sub

        <WorkItem(540638)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterEnumWithLongMinValueInHex()
            Test(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &H8000000000000000 \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H8000000000000000 \n Blue = &H8000000000000001 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterNegativeLongInHex()
            Test(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &HFFFFFFFFFFFFFFFF \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &HFFFFFFFFFFFFFFFF \n Blue = &H0 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterPositiveLongInHex()
            Test(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Orange|] \n End Sub \n End Class \n Enum Color As Long \n Red = &HFFFFFFFFFFFFFFFF \n Blue = &H0 \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Orange \n End Sub \n End Class \n Enum Color As Long \n Red = &HFFFFFFFFFFFFFFFF \n Blue = &H0 \n Orange = &H1 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterPositiveLongExprInHex()
            Test(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &H414 / 2 \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H414 / 2 \n Blue = 523 \n End Enum"))
        End Sub

        <WorkItem(540632)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterEnumWithULongMaxValue()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As ULong \n Red = ULong.MaxValue \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As ULong \n Red = ULong.MaxValue \n Blue = 0 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestNegativeRangeIn64BitSignedEnums()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Green|] \n End Sub \n End Module \n Enum Color As Long \n Red = -10 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Green \n End Sub \n End Module \n Enum Color As Long \n Red = -10 \n Green = -9 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestUnaryMinusOnUInteger1()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As UInteger \n Red = -(0 - UInteger.MaxValue) \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As UInteger \n Red = -(0 - UInteger.MaxValue) \n Blue = 0 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestDoubleUnaryMinusOnUInteger()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As UInteger \n Red = --(UInteger.MaxValue - 1) \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As UInteger \n Red = --(UInteger.MaxValue - 1) \n Blue = UInteger.MaxValue \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestDoubleUnaryMinusOnUInteger1()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Green|] \n End Sub \n End Class \n Enum Color As UInteger \n Red = --(UInteger.MaxValue - 1) \n Blue = 4294967295 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Green \n End Sub \n End Class \n Enum Color As UInteger \n Red = --(UInteger.MaxValue - 1) \n Blue = 4294967295 \n Green = 0 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateWithImplicitValues()
            ' Red is implicitly assigned to 0, Green is implicitly Red + 1, So Blue must be 2.
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Module \n Enum Color \n Red \n Green \n Yellow = -1 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Module \n Enum Color \n Red \n Green \n Yellow = -1 \n Blue = 2 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateWithImplicitValues2()
            Test(
NewLines("Class B \n Sub Main(args As String()) \n [|Color.Grey|] \n End Sub \n End Class \n Enum Color \n Red \n Green = 10 \n Blue \n End Enum"),
NewLines("Class B \n Sub Main(args As String()) \n Color.Grey \n End Sub \n End Class \n Enum Color \n Red \n Green = 10 \n Blue \n Grey \n End Enum"))
        End Sub

        <WorkItem(540549)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestNoExtraneousStatementTerminatorBeforeCommentedMember()
            Dim code = <Text>Module Program
    Sub Main(args As String())
        Foo([|Color.Blue|])
    End Sub
End Module

Enum Color
    Red
    'Blue
End Enum</Text>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <Text>Module Program
    Sub Main(args As String())
        Foo(Color.Blue)
    End Sub
End Module

Enum Color
    Red
    Blue
    'Blue
End Enum</Text>.Value.Replace(vbLf, vbCrLf)

            Test(code,
                    expected,
                    compareTokens:=False)
        End Sub

        <WorkItem(540552)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterEnumWithMinValue()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color \n Red = Integer.MinValue \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color \n Red = Integer.MinValue \n Blue = -2147483647 \n End Enum"))
        End Sub

        <WorkItem(540553)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterEnumWithMinValuePlusConstant()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color \n Red = Integer.MinValue + 100 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color \n Red = Integer.MinValue + 100 \n Blue = -2147483547 \n End Enum"))
        End Sub

        <WorkItem(540556)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterEnumWithByteMaxValue()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color As Byte\n Red = 255 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color As Byte\n Red = 255 \n Blue = 0 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoNegativeSByteInOctal()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O1777777777777777777777 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O1777777777777777777777 \n Blue = &O0 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoPositiveSByteInOctal1()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O0 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O0 \n Blue = &O1 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoPositiveSByteInOctal2()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O176 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O176 \n Blue = &O177 \n End Enum"))
        End Sub

        <WorkItem(540631)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateAfterEnumWithSByteMaxValueInOctal()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O177 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O177 \n Blue = &O1777777777777777777600 \n End Enum"))
        End Sub

        <WorkItem(528207)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestAbsenceOfFixWhenImportingEnums()
            TestMissing(
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo([|Blue|]) \n End Sub \n End Module \n Enum Color As Byte \n Red \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestPresenceOfFixWhenImportingEnumsYetFullyQualifyingThem()
            Test(
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo([|Color.Green|]) \n End Sub \n End Module \n Enum Color As Long \n Red = -10 \n End Enum"),
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo(Color.Green) \n End Sub \n End Module \n Enum Color As Long \n Red = -10 \n Green = -9 \n End Enum"))
        End Sub

        <WorkItem(540585)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoBitshiftEnum()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n Enum Color \n Red = 1 << 0 \n Green = 1 << 1 \n Orange = 1 << 2 \n End Enum \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n Enum Color \n Red = 1 << 0 \n Green = 1 << 1 \n Orange = 1 << 2 \n Blue = 1 << 3 \n End Enum \n End Module"))
        End Sub

        <WorkItem(540566)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestKeywordName()
            Test(
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo([|Color.Enum|]) \n End Sub \n End Module \n Enum Color As Byte \n Red \n End Enum"),
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo(Color.Enum) \n End Sub \n End Module \n Enum Color As Byte \n Red \n [Enum] \n End Enum"))
        End Sub

        <WorkItem(540547)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestStandaloneReference()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Module \n Enum Color As Integer \n Red = Integer.MinValue \n Green = 1 \n End Enum"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Module \n Enum Color As Integer \n Red = Integer.MinValue \n Green = 1 \n Blue = 2 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestCircularEnumsForErrorTolerance()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|Circular.C|] \n End Sub \n End Module \n Enum Circular \n A = B \n B \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Circular.C \n End Sub \n End Module \n Enum Circular \n A = B \n B \n C \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestEnumWithIncorrectValueForErrorTolerance()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Green|] \n End Sub \n End Module \n Enum Color As Byte \n Red = -2 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Green \n End Sub \n End Module \n Enum Color As Byte \n Red = -2 \n Green \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestHexValues()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|RenderType.LastViewedPage|] \n End Sub \n End Module \n <FlagsAttribute()> \n Enum RenderType \n None = &H0 \n DataUri = &H1 \n GZip = &H2 \n ContentPage = &H4 \n ViewPage = &H8 \n HomePage = &H10 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n RenderType.LastViewedPage \n End Sub \n End Module \n <FlagsAttribute()> \n Enum RenderType \n None = &H0 \n DataUri = &H1 \n GZip = &H2 \n ContentPage = &H4 \n ViewPage = &H8 \n HomePage = &H10 \n LastViewedPage = &H20 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoShadowedEnum()
            Test(
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n [|BaseColors.Blue|] \n End Sub \n Shadows Enum BaseColors \n Orange = 3 \n End Enum \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"),
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n BaseColors.Blue \n End Sub \n Shadows Enum BaseColors \n Orange = 3 \n Blue = 4 \n End Enum \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoDerivedEnumMissingShadowsKeyword()
            Test(
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n [|BaseColors.Blue|] \n End Sub \n Enum BaseColors \n Orange = 3 \n End Enum \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"),
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n BaseColors.Blue \n End Sub \n Enum BaseColors \n Orange = 3 \n Blue = 4 \n End Enum \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerateIntoBaseEnum()
            Test(
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n [|BaseColors.Blue|] \n End Sub \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"),
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n BaseColors.Blue \n End Sub \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n Blue = 3 \n End Enum \n End Class"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestErrorToleranceWithStrictSemantics()
            Test(
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = 1.5 \n Green = 2.3 \n Orange = 3.3 \n End Enum"),
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = 1.5 \n Green = 2.3 \n Orange = 3.3 \n Blue = 4 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGeometricSequenceWithTypeConversions()
            Test(
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = CLng(1.5) \n Green = CLng(2.3) \n Orange = CLng(3.9) \n End Enum"),
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = CLng(1.5) \n Green = CLng(2.3) \n Orange = CLng(3.9) \n Blue = 8 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestLinearSequenceWithTypeConversions()
            Test(
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = CLng(1.5) \n Green = CLng(2.3) \n Orange = CLng(4.9) \n End Enum"),
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = CLng(1.5) \n Green = CLng(2.3) \n Orange = CLng(4.9) \n Blue = 6 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestGenerationWhenMembersShareValues()
            Test(
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n [|Color.Grey|] \n End Sub \n End Class \n Enum Color \n Red \n Green \n Blue \n Max = Blue \n End Enum"),
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n Color.Grey \n End Sub \n End Class \n Enum Color \n Red \n Green \n Blue \n Max = Blue \n Grey = 3 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestInvokeFromAddAssignmentStatement()
            Test(
NewLines("Class B \n Sub Main(args As String()) \n Dim a As Integer = 1 \n a += [|Color.Grey|] \n End Sub \n End Class \n Enum Color \n Red \n Green = 10 \n Blue \n End Enum"),
NewLines("Class B \n Sub Main(args As String()) \n Dim a As Integer = 1 \n a += Color.Grey \n End Sub \n End Class \n Enum Color \n Red \n Green = 10 \n Blue \n Grey \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestMissingOnEnumsFromMetaData()
            TestMissing(
NewLines("Imports Microsoft.VisualBasic \n Module Program \n Sub Main(args As String()) \n Dim a = [|FirstDayOfWeek.EigthDay|] \n End Sub \n End Module"))
        End Sub

        <WorkItem(540638)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestMaxHex()
            Test(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n  \n Enum Color As Long \n Red = &H8000000000000000 \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H8000000000000000 \n Blue = &H8000000000000001 \n End Enum"))
        End Sub

        <WorkItem(540636)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestMinHex()
            Test(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &H7FFFFFFFFFFFFFFF \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H7FFFFFFFFFFFFFFF \n Blue = &H8000000000000000 \n End Enum"))
        End Sub

        <WorkItem(540631)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestOctalBounds1()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O177 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O177 \n Blue = &O1777777777777777777600 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestULongMax()
            Test(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As ULong \n Red = ULong.MaxValue \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As ULong \n Red = ULong.MaxValue \n Blue = 0 \n End Enum"))
        End Sub

        <WorkItem(540604)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestWrapAround1()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Module \n Enum Color As Integer \n Green = Integer.MaxValue \n Orange = -2147483648 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Module \n Enum Color As Integer \n Green = Integer.MaxValue \n Orange = -2147483648 \n Blue = -2147483647 \n End Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestMissingOnHiddenEnum()
            TestMissing(
NewLines("#ExternalSource (""Default.aspx"", 1) \n Imports System \n Enum E \n #End ExternalSource \n End Enum \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Console.Write([|E.x|]) \n End Sub \n End Class \n #End ExternalSource"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestMissingOnPartiallyHiddenEnum()
            TestMissing(
NewLines("#ExternalSource (""Default.aspx"", 1) \n Imports System \n Enum E \n A \n B \n C \n #End ExternalSource \n End Enum \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Console.Write([|E.x|]) \n End Sub \n End Class \n #End ExternalSource"))
        End Sub

        <WorkItem(544656)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestShortHexidecimalLiterals()
            Test(
NewLines("Module M \n Dim y = [|E.Y|] ' Generate Y \n End Module \n Enum E As Short \n X = &H4000S \n End Enum"),
NewLines("Module M \n Dim y = E.Y ' Generate Y \n End Module \n Enum E As Short \n X = &H4000S \n Y = &H4001S \n End Enum"))
        End Sub

        <WorkItem(545937)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Sub TestUShortEnums()
            Test(
NewLines("Module M \n Dim y = [|E.Y|] ' Generate Y \n End Module \n  \n Enum E As UShort \n X = &H4000US \n End Enum"),
NewLines("Module M \n Dim y = E.Y ' Generate Y \n End Module \n  \n Enum E As UShort \n X = &H4000US \n Y = &H8000US \n End Enum"))
        End Sub
    End Class
End Namespace
