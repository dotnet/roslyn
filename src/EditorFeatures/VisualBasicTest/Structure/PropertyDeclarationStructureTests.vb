' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class PropertyDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of PropertyStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New PropertyDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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