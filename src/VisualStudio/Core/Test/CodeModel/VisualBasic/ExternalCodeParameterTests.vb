' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class ExternalCodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "FullName tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
Class C
    Sub Goo($$s As String)
    End Sub
End Class
</Code>

            TestFullName(code, "s")
        End Sub
#End Region

#Region "Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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
