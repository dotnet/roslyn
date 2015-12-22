' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEnumMember
Imports Microsoft.CodeAnalysis.Diagnostics
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateEnumMember
    Public Class GenerateEnumMemberTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateEnumMemberCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoEmpty() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Red|]) \n End Sub \n End Module \n Enum Color \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Red) \n End Sub \n End Module \n Enum Color \n Red \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoEnumWithSingleMember() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Green|]) \n End Sub \n End Module \n Enum Color \n Red \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Green) \n End Sub \n End Module \n Enum Color \n Red \n Green \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterEnumWithValue() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Green|]) \n End Sub \n End Module \n Enum Color \n Red = 1 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Green) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoLinearIncreasingSequence() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n Blue = 3 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoGeometricSequence() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Purple|]) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n Blue = 4 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Purple) \n End Sub \n End Module \n Enum Color \n Red = 1 \n Green = 2 \n Blue = 4 \n Purple = 8 \n End Enum"))
        End Function

        <WorkItem(540540)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterEnumWithIntegerMaxValue() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color \n Red \n Green = Integer.MaxValue \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color \n Red  \n Green = Integer.MaxValue \n Blue = Integer.MinValue \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestUnsigned16BitEnums() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Green|] \n End Sub \n End Module \n Enum Color As UShort \n Red = 65535 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Green \n End Sub \n End Module \n Enum Color As UShort \n Red = 65535 \n Green = 0 \n End Enum"))
        End Function

        <WorkItem(540546)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateEnumMemberOfTypeLong() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color As Long \n Red = Long.MinValue \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color As Long \n Red = Long.MinValue \n Blue = -9223372036854775807 \n End Enum"))
        End Function

        <WorkItem(540636)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterEnumWithLongMaxValueInHex() As Task
            Await TestAsync(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &H7FFFFFFFFFFFFFFF \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H7FFFFFFFFFFFFFFF \n Blue = &H8000000000000000 \n End Enum"))
        End Function

        <WorkItem(540638)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterEnumWithLongMinValueInHex() As Task
            Await TestAsync(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &H8000000000000000 \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H8000000000000000 \n Blue = &H8000000000000001 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterNegativeLongInHex() As Task
            Await TestAsync(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &HFFFFFFFFFFFFFFFF \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &HFFFFFFFFFFFFFFFF \n Blue = &H0 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterPositiveLongInHex() As Task
            Await TestAsync(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Orange|] \n End Sub \n End Class \n Enum Color As Long \n Red = &HFFFFFFFFFFFFFFFF \n Blue = &H0 \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Orange \n End Sub \n End Class \n Enum Color As Long \n Red = &HFFFFFFFFFFFFFFFF \n Blue = &H0 \n Orange = &H1 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterPositiveLongExprInHex() As Task
            Await TestAsync(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &H414 / 2 \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H414 / 2 \n Blue = 523 \n End Enum"))
        End Function

        <WorkItem(540632)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterEnumWithULongMaxValue() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As ULong \n Red = ULong.MaxValue \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As ULong \n Red = ULong.MaxValue \n Blue = 0 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestNegativeRangeIn64BitSignedEnums() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Green|] \n End Sub \n End Module \n Enum Color As Long \n Red = -10 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Green \n End Sub \n End Module \n Enum Color As Long \n Red = -10 \n Green = -9 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestUnaryMinusOnUInteger1() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As UInteger \n Red = -(0 - UInteger.MaxValue) \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As UInteger \n Red = -(0 - UInteger.MaxValue) \n Blue = 0 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestDoubleUnaryMinusOnUInteger() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As UInteger \n Red = --(UInteger.MaxValue - 1) \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As UInteger \n Red = --(UInteger.MaxValue - 1) \n Blue = UInteger.MaxValue \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestDoubleUnaryMinusOnUInteger1() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Green|] \n End Sub \n End Class \n Enum Color As UInteger \n Red = --(UInteger.MaxValue - 1) \n Blue = 4294967295 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Green \n End Sub \n End Class \n Enum Color As UInteger \n Red = --(UInteger.MaxValue - 1) \n Blue = 4294967295 \n Green = 0 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateWithImplicitValues() As Task
            ' Red is implicitly assigned to 0, Green is implicitly Red + 1, So Blue must be 2.
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Module \n Enum Color \n Red \n Green \n Yellow = -1 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Module \n Enum Color \n Red \n Green \n Yellow = -1 \n Blue = 2 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateWithImplicitValues2() As Task
            Await TestAsync(
NewLines("Class B \n Sub Main(args As String()) \n [|Color.Grey|] \n End Sub \n End Class \n Enum Color \n Red \n Green = 10 \n Blue \n End Enum"),
NewLines("Class B \n Sub Main(args As String()) \n Color.Grey \n End Sub \n End Class \n Enum Color \n Red \n Green = 10 \n Blue \n Grey \n End Enum"))
        End Function

        <WorkItem(540549)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestNoExtraneousStatementTerminatorBeforeCommentedMember() As Task
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

            Await TestAsync(code,
                    expected,
                    compareTokens:=False)
        End Function

        <WorkItem(540552)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterEnumWithMinValue() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color \n Red = Integer.MinValue \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color \n Red = Integer.MinValue \n Blue = -2147483647 \n End Enum"))
        End Function

        <WorkItem(540553)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterEnumWithMinValuePlusConstant() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color \n Red = Integer.MinValue + 100 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color \n Red = Integer.MinValue + 100 \n Blue = -2147483547 \n End Enum"))
        End Function

        <WorkItem(540556)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterEnumWithByteMaxValue() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n End Module \n Enum Color As Byte\n Red = 255 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n End Module \n Enum Color As Byte\n Red = 255 \n Blue = 0 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoNegativeSByteInOctal() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O1777777777777777777777 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O1777777777777777777777 \n Blue = &O0 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoPositiveSByteInOctal1() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O0 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O0 \n Blue = &O1 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoPositiveSByteInOctal2() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O176 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O176 \n Blue = &O177 \n End Enum"))
        End Function

        <WorkItem(540631)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateAfterEnumWithSByteMaxValueInOctal() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O177 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O177 \n Blue = &O1777777777777777777600 \n End Enum"))
        End Function

        <WorkItem(528207)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestAbsenceOfFixWhenImportingEnums() As Task
            Await TestMissingAsync(
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo([|Blue|]) \n End Sub \n End Module \n Enum Color As Byte \n Red \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestPresenceOfFixWhenImportingEnumsYetFullyQualifyingThem() As Task
            Await TestAsync(
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo([|Color.Green|]) \n End Sub \n End Module \n Enum Color As Long \n Red = -10 \n End Enum"),
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo(Color.Green) \n End Sub \n End Module \n Enum Color As Long \n Red = -10 \n Green = -9 \n End Enum"))
        End Function

        <WorkItem(540585)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoBitshiftEnum() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Color.Blue|]) \n End Sub \n Enum Color \n Red = 1 << 0 \n Green = 1 << 1 \n Orange = 1 << 2 \n End Enum \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Foo(Color.Blue) \n End Sub \n Enum Color \n Red = 1 << 0 \n Green = 1 << 1 \n Orange = 1 << 2 \n Blue = 1 << 3 \n End Enum \n End Module"))
        End Function

        <WorkItem(540566)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestKeywordName() As Task
            Await TestAsync(
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo([|Color.Enum|]) \n End Sub \n End Module \n Enum Color As Byte \n Red \n End Enum"),
NewLines("Imports Color \n Module Program \n Sub Main(args As String()) \n Foo(Color.Enum) \n End Sub \n End Module \n Enum Color As Byte \n Red \n [Enum] \n End Enum"))
        End Function

        <WorkItem(540547)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestStandaloneReference() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Module \n Enum Color As Integer \n Red = Integer.MinValue \n Green = 1 \n End Enum"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Module \n Enum Color As Integer \n Red = Integer.MinValue \n Green = 1 \n Blue = 2 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestCircularEnumsForErrorTolerance() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|Circular.C|] \n End Sub \n End Module \n Enum Circular \n A = B \n B \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Circular.C \n End Sub \n End Module \n Enum Circular \n A = B \n B \n C \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestEnumWithIncorrectValueForErrorTolerance() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Green|] \n End Sub \n End Module \n Enum Color As Byte \n Red = -2 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Green \n End Sub \n End Module \n Enum Color As Byte \n Red = -2 \n Green \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestHexValues() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|RenderType.LastViewedPage|] \n End Sub \n End Module \n <FlagsAttribute()> \n Enum RenderType \n None = &H0 \n DataUri = &H1 \n GZip = &H2 \n ContentPage = &H4 \n ViewPage = &H8 \n HomePage = &H10 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n RenderType.LastViewedPage \n End Sub \n End Module \n <FlagsAttribute()> \n Enum RenderType \n None = &H0 \n DataUri = &H1 \n GZip = &H2 \n ContentPage = &H4 \n ViewPage = &H8 \n HomePage = &H10 \n LastViewedPage = &H20 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoShadowedEnum() As Task
            Await TestAsync(
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n [|BaseColors.Blue|] \n End Sub \n Shadows Enum BaseColors \n Orange = 3 \n End Enum \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"),
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n BaseColors.Blue \n End Sub \n Shadows Enum BaseColors \n Orange = 3 \n Blue = 4 \n End Enum \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoDerivedEnumMissingShadowsKeyword() As Task
            Await TestAsync(
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n [|BaseColors.Blue|] \n End Sub \n Enum BaseColors \n Orange = 3 \n End Enum \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"),
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n BaseColors.Blue \n End Sub \n Enum BaseColors \n Orange = 3 \n Blue = 4 \n End Enum \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerateIntoBaseEnum() As Task
            Await TestAsync(
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n [|BaseColors.Blue|] \n End Sub \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n End Enum \n End Class"),
NewLines("Class B \n Inherits A \n Sub Main(args As String()) \n BaseColors.Blue \n End Sub \n End Class \n Public Class A \n Public Enum BaseColors \n Red = 1 \n Green = 2 \n Blue = 3 \n End Enum \n End Class"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestErrorToleranceWithStrictSemantics() As Task
            Await TestAsync(
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = 1.5 \n Green = 2.3 \n Orange = 3.3 \n End Enum"),
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = 1.5 \n Green = 2.3 \n Orange = 3.3 \n Blue = 4 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGeometricSequenceWithTypeConversions() As Task
            Await TestAsync(
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = CLng(1.5) \n Green = CLng(2.3) \n Orange = CLng(3.9) \n End Enum"),
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = CLng(1.5) \n Green = CLng(2.3) \n Orange = CLng(3.9) \n Blue = 8 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestLinearSequenceWithTypeConversions() As Task
            Await TestAsync(
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = CLng(1.5) \n Green = CLng(2.3) \n Orange = CLng(4.9) \n End Enum"),
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = CLng(1.5) \n Green = CLng(2.3) \n Orange = CLng(4.9) \n Blue = 6 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestGenerationWhenMembersShareValues() As Task
            Await TestAsync(
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n [|Color.Grey|] \n End Sub \n End Class \n Enum Color \n Red \n Green \n Blue \n Max = Blue \n End Enum"),
NewLines("Option Strict On \n Class B \n Sub Main(args As String()) \n Color.Grey \n End Sub \n End Class \n Enum Color \n Red \n Green \n Blue \n Max = Blue \n Grey = 3 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestInvokeFromAddAssignmentStatement() As Task
            Await TestAsync(
NewLines("Class B \n Sub Main(args As String()) \n Dim a As Integer = 1 \n a += [|Color.Grey|] \n End Sub \n End Class \n Enum Color \n Red \n Green = 10 \n Blue \n End Enum"),
NewLines("Class B \n Sub Main(args As String()) \n Dim a As Integer = 1 \n a += Color.Grey \n End Sub \n End Class \n Enum Color \n Red \n Green = 10 \n Blue \n Grey \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestMissingOnEnumsFromMetaData() As Task
            Await TestMissingAsync(
NewLines("Imports Microsoft.VisualBasic \n Module Program \n Sub Main(args As String()) \n Dim a = [|FirstDayOfWeek.EigthDay|] \n End Sub \n End Module"))
        End Function

        <WorkItem(540638)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestMaxHex() As Task
            Await TestAsync(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n  \n Enum Color As Long \n Red = &H8000000000000000 \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H8000000000000000 \n Blue = &H8000000000000001 \n End Enum"))
        End Function

        <WorkItem(540636)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestMinHex() As Task
            Await TestAsync(
NewLines("Class Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As Long \n Red = &H7FFFFFFFFFFFFFFF \n End Enum"),
NewLines("Class Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As Long \n Red = &H7FFFFFFFFFFFFFFF \n Blue = &H8000000000000000 \n End Enum"))
        End Function

        <WorkItem(540631)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestOctalBounds1() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As SByte \n Red = &O177 \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As SByte \n Red = &O177 \n Blue = &O1777777777777777777600 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestULongMax() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Class \n Enum Color As ULong \n Red = ULong.MaxValue \n End Enum"),
NewLines("Class A \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Class \n Enum Color As ULong \n Red = ULong.MaxValue \n Blue = 0 \n End Enum"))
        End Function

        <WorkItem(540604)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestWrapAround1() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|Color.Blue|] \n End Sub \n End Module \n Enum Color As Integer \n Green = Integer.MaxValue \n Orange = -2147483648 \n End Enum"),
NewLines("Module Program \n Sub Main(args As String()) \n Color.Blue \n End Sub \n End Module \n Enum Color As Integer \n Green = Integer.MaxValue \n Orange = -2147483648 \n Blue = -2147483647 \n End Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestMissingOnHiddenEnum() As Task
            Await TestMissingAsync(
NewLines("#ExternalSource (""Default.aspx"", 1) \n Imports System \n Enum E \n #End ExternalSource \n End Enum \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Console.Write([|E.x|]) \n End Sub \n End Class \n #End ExternalSource"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestMissingOnPartiallyHiddenEnum() As Task
            Await TestMissingAsync(
NewLines("#ExternalSource (""Default.aspx"", 1) \n Imports System \n Enum E \n A \n B \n C \n #End ExternalSource \n End Enum \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Console.Write([|E.x|]) \n End Sub \n End Class \n #End ExternalSource"))
        End Function

        <WorkItem(544656)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestShortHexidecimalLiterals() As Task
            Await TestAsync(
NewLines("Module M \n Dim y = [|E.Y|] ' Generate Y \n End Module \n Enum E As Short \n X = &H4000S \n End Enum"),
NewLines("Module M \n Dim y = E.Y ' Generate Y \n End Module \n Enum E As Short \n X = &H4000S \n Y = &H4001S \n End Enum"))
        End Function

        <WorkItem(545937)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)>
        Public Async Function TestUShortEnums() As Task
            Await TestAsync(
NewLines("Module M \n Dim y = [|E.Y|] ' Generate Y \n End Module \n  \n Enum E As UShort \n X = &H4000US \n End Enum"),
NewLines("Module M \n Dim y = E.Y ' Generate Y \n End Module \n  \n Enum E As UShort \n X = &H4000US \n Y = &H8000US \n End Enum"))
        End Function
    End Class
End Namespace
