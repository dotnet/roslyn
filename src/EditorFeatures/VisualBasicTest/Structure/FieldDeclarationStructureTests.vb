' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class FieldDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of FieldDeclarationSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New FieldDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestVariableMemberDeclarationWithComments() As Task
            Const code = "
Class C
    {|span:'Hello
    'World|}
    Dim $$x As Integer
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
