' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class OperatorDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of OperatorStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New OperatorDeclarationStructureProvider()
        End Function

        <Fact>
        Public Async Function TestOperatorDeclaration() As Task
            Const code = "
Class Base
    {|span:Public Shared Widening Operator $$CType(b As Base) As Integer
    End Operator|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Public Shared Widening Operator CType(b As Base) As Integer ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestOperatorWithComments() As Task
            Const code = "
Class Base
    {|span1:'Hello
    'World|}
    {|span2:Public Shared Widening Operator $$CType(b As Base) As Integer
    End Operator|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Public Shared Widening Operator CType(b As Base) As Integer ...", autoCollapse:=True))
        End Function
    End Class
End Namespace
