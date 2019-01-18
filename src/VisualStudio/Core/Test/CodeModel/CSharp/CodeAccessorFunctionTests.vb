' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeAccessorFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
    <Code>
class C
{
    public int P
    {
        get
        {
            return 0;
        }
        $$set
        {
        }
    }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
    <Code>
class C
{
    public int P
    {
        get
        {
            return 0;
        }
        private $$set
        {
        }
    }
}
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
class C
{
    int P { $$get { return 42; } }
}
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeFunction2)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace


