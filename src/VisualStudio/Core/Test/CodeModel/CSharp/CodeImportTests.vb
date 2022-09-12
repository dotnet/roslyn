' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    <Trait(Traits.Feature, Traits.Features.CodeModel)>
    Public Class CodeImportTests
        Inherits AbstractCodeImportTests

#Region "FullName tests"

        <WpfFact
#Region "FullName tests"
>
        Public Sub TestFullName1()
            Dim code =
<Code>
using $$Goo;
</Code>

            Dim ex = Assert.Throws(Of COMException)(
                Sub()
                    TestName(code, "Goo")
                End Sub)

            Assert.Equal(E_FAIL, ex.ErrorCode)
        End Sub

#End Region

#Region "Name tests"

        <WpfFact
#Region "Name tests"
>
        Public Sub TestName1()
            Dim code =
<Code>
using $$Goo;
</Code>

            Dim ex = Assert.Throws(Of COMException)(
                Sub()
                    TestName(code, "Goo")
                End Sub)

            Assert.Equal(E_FAIL, ex.ErrorCode)
        End Sub

#End Region

#Region "Namespace tests"

        <WpfFact
#Region "Namespace tests"
>
        Public Sub TestNamespace1()
            Dim code =
<Code>
using $$Goo;
</Code>

            TestNamespace(code, "Goo")
        End Sub

        <WpfFact>
        Public Sub TestNamespace2()
            Dim code =
<Code>
namespace Bar
{
    using $$Goo;
}
</Code>

            TestNamespace(code, "Goo")
        End Sub

#End Region

        <WpfFact>
        Public Sub TestTypeDescriptor_GetProperties()
            Dim code =
<Code>
using $$System;
</Code>

            TestPropertyDescriptors(Of EnvDTE80.CodeImport)(code)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
