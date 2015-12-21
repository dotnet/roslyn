' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class RegionDirectiveOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of RegionDirectiveTriviaSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New MaSOutliners.RegionDirectiveOutliner()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function FileHeader() As Task
            Const code = "
{|span:$$#Region ""Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""
' C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
#End Region|}
"
            Await VerifyRegionsAsync(code,
                Region("span", "Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", autoCollapse:=True, isDefaultCollapsed:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function EmptyFileHeader() As Task
            Const code = "
{|span:$$#Region """"
' C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
#End Region|}
"
            Await VerifyRegionsAsync(code,
                Region("span", "#Region", autoCollapse:=True, isDefaultCollapsed:=False))
        End Function

    End Class
End Namespace
