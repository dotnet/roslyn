' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class PropertyDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of PropertyStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New PropertyDeclarationOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

            Await VerifyRegionsAsync(code,
                Region("span", "ReadOnly Property P1 As Integer ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestWriteOnlyProperty() As Task
            Const code = "
Class C1
    {|span:WriteOnly Property $$P1 As Integer
        Set(ByVal value As Integer)
        End Set
    End Property|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "WriteOnly Property P1 As Integer ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
