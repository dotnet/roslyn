' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class CodeImportTests
        Inherits AbstractCodeImportTests

#Region "FullName tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestFullName1() As Task
            Dim code =
<Code>
using $$Foo;
</Code>

            Dim ex = Await Assert.ThrowsAsync(Of COMException)(
                Async Function()
                    Await TestName(code, "Foo")
                End Function)

            Assert.Equal(E_FAIL, ex.ErrorCode)
        End Function

#End Region

#Region "Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestName1() As Task
            Dim code =
<Code>
using $$Foo;
</Code>

            Dim ex = Await Assert.ThrowsAsync(Of COMException)(
                Async Function()
                    Await TestName(code, "Foo")
                End Function)

            Assert.Equal(E_FAIL, ex.ErrorCode)
        End Function

#End Region

#Region "Namespace tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestNamespace1() As Task
            Dim code =
<Code>
using $$Foo;
</Code>

            Await TestNamespace(code, "Foo")
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestNamespace2() As Task
            Dim code =
<Code>
namespace Bar
{
    using $$Foo;
}
</Code>

            Await TestNamespace(code, "Foo")
        End Function

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestTypeDescriptor_GetProperties() As Task
            Dim code =
<Code>
using $$System;
</Code>

            Dim expectedPropertyNames =
                {"DTE", "Collection", "Name", "FullName", "ProjectItem", "Kind", "IsCodeType",
                 "InfoLocation", "Children", "Language", "StartPoint", "EndPoint", "ExtenderNames",
                 "ExtenderCATID", "Namespace", "Alias", "Parent"}

            Await TestPropertyDescriptors(code, expectedPropertyNames)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property
    End Class
End Namespace
