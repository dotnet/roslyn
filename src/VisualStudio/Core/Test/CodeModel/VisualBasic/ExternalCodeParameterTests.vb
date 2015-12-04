' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class ExternalCodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "FullName tests"

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
Class C
    Sub Foo($$s As String)
    End Sub
End Class
</Code>

            TestFullName(code, "s")
        End Sub
#End Region

#Region "Name tests"

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_NoModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S1($$p1 As Integer)
   End Sub

End Class
</Code>

            TestName(code, "p1")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ByValModifier()
            Dim code =
<Code>
Public Class C1

   Public Sub S2(ByVal $$p2 As Integer)
   End Sub

End Class
</Code>

            TestName(code, "p2")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ByRefModifier()
            Dim code =
<Code>
Public Class C1

   Public Sub S3(ByRef $$p3 As Integer)
   End Sub

End Class
</Code>

            TestName(code, "p3")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_OptionalByValModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S4(Optional ByVal $$p4 As Integer = 0)
   End Sub

End Class
</Code>

            TestName(code, "p4")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_ByValParamArrayModifiers()
            Dim code =
<Code>
Public Class C1

   Public Sub S5(ByVal ParamArray $$p5() As Integer)
   End Sub

End Class
</Code>

            TestName(code, "p5")
        End Sub

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_TypeCharacter()
            Dim code =
<Code>
Public Class C1

   Public Sub S6($$p6%)
   End Sub

End Class
</Code>

            TestName(code, "p6")
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True

    End Class
End Namespace