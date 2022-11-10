' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class PropertyDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of PropertyStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New PropertyDeclarationStructureProvider()
        End Function

        <Fact>
        Public Async Function TestReadOnlyProperty() As Task
            Const code = "
Class C1
    {|span:ReadOnly Property P1$$ As Integer
        Get
            Return 0
        End Get
    End Property|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "ReadOnly Property P1 As Integer ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestWriteOnlyProperty() As Task
            Const code = "
Class C1
    {|span:WriteOnly Property $$P1 As Integer
        Set(ByVal value As Integer)
        End Set
    End Property|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "WriteOnly Property P1 As Integer ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
