' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeImportTests
        Inherits AbstractCodeImportTests

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub FullName1()
            Dim code =
<Code>
using $$Foo;
</Code>

            Dim ex = Assert.Throws(Of COMException)(
                Sub()
                    TestName(code, "Foo")
                End Sub)

            Assert.Equal(E_FAIL, ex.ErrorCode)
        End Sub

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Name1()
            Dim code =
<Code>
using $$Foo;
</Code>

            Dim ex = Assert.Throws(Of COMException)(
                Sub()
                    TestName(code, "Foo")
                End Sub)

            Assert.Equal(E_FAIL, ex.ErrorCode)
        End Sub

#End Region

#Region "Namespace tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Namespace1()
            Dim code =
<Code>
using $$Foo;
</Code>

            TestNamespace(code, "Foo")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Namespace2()
            Dim code =
<Code>
namespace Bar
{
    using $$Foo;
}
</Code>

            TestNamespace(code, "Foo")
        End Sub

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TypeDescriptor_GetProperties()
            Dim code =
<Code>
using $$System;
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Namespace", "Alias", "Parent"}

            TestPropertyDescriptors(code, expectedPropertyNames)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
