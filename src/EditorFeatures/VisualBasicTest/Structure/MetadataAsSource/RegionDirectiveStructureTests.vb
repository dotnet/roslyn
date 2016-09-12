' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure.MetadataAsSource
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class RegionDirectiveStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of RegionDirectiveTriviaSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New MetadataRegionDirectiveStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function FileHeader() As Task
            Const code = "
{|span:$$#Region ""Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""
' C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
#End Region|}
"
            Await VerifyBlockSpansAsync(code,
                Region("span", "Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", autoCollapse:=True, isDefaultCollapsed:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function EmptyFileHeader() As Task
            Const code = "
{|span:$$#Region """"
' C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
#End Region|}
"
            Await VerifyBlockSpansAsync(code,
                Region("span", "#Region", autoCollapse:=True, isDefaultCollapsed:=False))
        End Function
    End Class
End Namespace