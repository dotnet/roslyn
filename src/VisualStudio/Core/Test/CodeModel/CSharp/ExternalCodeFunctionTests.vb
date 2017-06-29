' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class ExternalCodeFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
class C
{
    void $$Foo(string s)
    {
    }
}
</Code>

            TestFullName(code, "C.Foo")
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
class C
{
    void $$Foo(string s)
    {
    }
}
</Code>

            TestName(code, "Foo")
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.CSharp
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True

    End Class
End Namespace
