' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class ExternalCodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
Class C
    Sub Foo($$s As String)
    End Sub
End Class
</Code>

            Await TestFullName(code, "s")
        End Function
#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_NoModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S1($$p1 As Integer)
   End Sub

End Class
</Code>

            Await TestName(code, "p1")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ByValModifier() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S2(ByVal $$p2 As Integer)
   End Sub

End Class
</Code>

            Await TestName(code, "p2")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ByRefModifier() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S3(ByRef $$p3 As Integer)
   End Sub

End Class
</Code>

            Await TestName(code, "p3")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_OptionalByValModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S4(Optional ByVal $$p4 As Integer = 0)
   End Sub

End Class
</Code>

            Await TestName(code, "p4")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_ByValParamArrayModifiers() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S5(ByVal ParamArray $$p5() As Integer)
   End Sub

End Class
</Code>

            Await TestName(code, "p5")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName_TypeCharacter() As Task
            Dim code =
<Code>
Public Class C1

   Public Sub S6($$p6%)
   End Sub

End Class
</Code>

            Await TestName(code, "p6")
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True

    End Class
End Namespace