' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class ExternalCodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "FullName tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
class C
{
    void Goo(string $$s)
    {
    }
}
</Code>

            TestFullName(code, "s")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName2()
            Dim code =
<Code>
class C
{
    void Goo(ref string $$s)
    {
    }
}
</Code>

            TestFullName(code, "s")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName3()
            Dim code =
<Code>
class C
{
    void Goo(out string $$s)
    {
    }
}
</Code>

            TestFullName(code, "s")
        End Sub

#End Region

#Region "Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
class C
{
    void Goo(string $$s)
    {
    }
}
</Code>

            TestName(code, "s")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName2()
            Dim code =
<Code>
class C
{
    void Goo(ref string $$s)
    {
    }
}
</Code>

            TestName(code, "s")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName3()
            Dim code =
<Code>
class C
{
    void Goo(out string $$s)
    {
    }
}
</Code>

            TestName(code, "s")
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.CSharp
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True

    End Class
End Namespace
