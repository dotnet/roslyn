' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class OperatorDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of OperatorStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New OperatorDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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
