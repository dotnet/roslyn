' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class ExternalCodeParameterTests
        Inherits AbstractCodeParameterTests

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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
