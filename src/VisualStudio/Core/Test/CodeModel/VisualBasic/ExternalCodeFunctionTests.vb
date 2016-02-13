' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp.VisualBasic
    Public Class ExternalCodeFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
Class C
    Sub $$Foo(string s)
    End Sub
End Class
</Code>

            Await TestFullName(code, "C.Foo")
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
Class C
    Sub $$Foo(string s)
    End Sub
End Class
</Code>

            Await TestName(code, "Foo")
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True

    End Class
End Namespace