' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticEndConstructCorrection
    Public Class AutomaticEndConstructCorrectorTests
        Inherits AbstractCorrectorTests

        Friend Overrides Function CreateCorrector(buffer As ITextBuffer, waitIndicator As TestWaitIndicator) As ICorrector
            Return New AutomaticEndConstructCorrector(buffer, waitIndicator)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestStructureToInterface()
            Dim code = <code>[|Structure|] A
End [|Structure|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestEnumToInterface()
            Dim code = <code>[|Enum|] A
End [|Enum|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestInterfaceToEnum()
            Dim code = <code>[|Interface|] A
End [|Interface|]</code>.Value

            Verify(code, "Enum")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestClassToInterface()
            Dim code = <code>[|Class|] A
End [|Class|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestModuleToInterface()
            Dim code = <code>[|Module|] A
End [|Module|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestNamespaceToInterface()
            Dim code = <code>[|Namespace|] A
End [|Namespace|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestSubToFunction()
            Dim code = <code>Class A
    [|Sub|] Test()
    End [|Sub|]
End Class</code>.Value

            Verify(code, "Function")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestFunctionToSub()
            Dim code = <code>Class A
    [|Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Verify(code, "Sub")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestModuleToInterface1()
            Dim code = <code>[|Module|] A : End [|Module|] : Module B : End Module</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestModuleToInterface2()
            Dim code = <code>Module A : End Module : [|Module|] B : End [|Module|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestModuleToInterface3()
            Dim code = <code>Module A : End Module:[|Module|] B : End [|Module|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestModuleToInterface4()
            Dim code = <code>[|Module|] A : End [|Module|]:Module B : End Module</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestErrorCaseMissingEndFunction()
            Dim code = <code>Class A
    [|Function|] Test() As Integer
    End [|Sub|]
End Class</code>.Value.Replace(vbLf, vbCrLf)

            VerifyBegin(code, "Interface", "Sub")
            VerifyEnd(code, "Interface", "Function")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestContinuousEditsOnFunctionToInterface()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Interface", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestContinuousEditsOnFunctionToInterfaceWithLeadingSpaces()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "     Interface", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestContinuousEditsOnFunctionToInterfaceWithTrailingSpaces()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Interface              ", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestContinuousEditsOnFunctionToInterfaceWithLeadingAndTrailingSpaces()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "             Interface              ", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestAddSharedModifierToFunction()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, " Shared ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestAddSharedModifierToFunction1()
            Dim code = <code>Class A
    [|Function$$|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Shared   ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestAddTrailingSpaceToFunction()
            Dim code = <code>Class A
    [|Function$$|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "           ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestAddLeadingSpaceToFunction()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "           ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestAddSharedModifierToFunction2()
            Dim code = <code>Class A
    [|Function$$|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Shared", Function(s) "Function", removeOriginalContent:=False, split:="Function")
        End Sub

        <WorkItem(539362)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestMultiLineLambdaSubToFunction()
            Dim code = <code>Class A
    Public Sub F()
        Dim nums() As Integer = {1, 2, 3, 4, 5}
        Array.ForEach(nums, [|Sub|](n)
                                Console.Write("Number: ")
                                Console.WriteLine(n)
                            End [|Sub|])
    End Sub
End Class</code>.Value

            Verify(code, "Function")
        End Sub

        <WorkItem(539362)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestMultiLineLambdaFunctionToSub()
            Dim code = <code>Class A
    Public Sub F()
        Dim nums() As Integer = {1, 2, 3, 4, 5}
        Dim numDelegate = [|Function|](n As Integer)

                          End [|Function|]
    End Sub
End Class</code>.Value

            Verify(code, "Sub")
        End Sub

        <WorkItem(539365)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub BugFix5290()
            Dim code = <code>Public Class Class1
    Sub M()
        [|Class|]
    End Sub
End [|Class|]</code>.Value

            VerifyBegin(code, "Structure", "Class")
            VerifyEnd(code, "Structure", "Class")
        End Sub

        <WorkItem(539357)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub BugFix5276()
            Dim code = <code>Class A
    [|Func$$tion|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "  ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WorkItem(539360)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub BugFix5283()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Shared Sub", Function(s) If(s.Trim() = "Shared Sub", "Sub", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        <WorkItem(539498)>
        Public Sub DontThrowDueToSingleLineDeletion()
            Dim code = <code>Class A
    [|$$Sub M() : End Sub|]
End Class</code>.Value

            VerifyContinuousEdits(code, "", Function() "", removeOriginalContent:=True)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestPropertySet()
            Dim code = <code>Class A
    Property Test
        [|Get|]
        End [|Get|]
        Set(value)
        End Set
    End Property
End Class</code>.Value

            Verify(code, "Set")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestPropertyGet()
            Dim code = <code>Class A
    Property Test
        Get
        End Get
        [|Set|](value)
        End [|Set|]
    End Property
End Class</code>.Value

            Verify(code, "Get")
        End Sub
    End Class
End Namespace
