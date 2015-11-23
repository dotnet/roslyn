' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class OperatorDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of OperatorStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New OperatorDeclarationOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestOperatorDeclaration() As Task
            Const code = "
Class Base
    {|span:Public Shared Widening Operator $$CType(b As Base) As Integer
    End Operator|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "Public Shared Widening Operator CType(b As Base) As Integer ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestOperatorWithComments() As Task
            Const code = "
Class Base
    {|span1:'Hello
    'World|}
    {|span2:Public Shared Widening Operator $$CType(b As Base) As Integer
    End Operator|}
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span1", "' Hello ...", autoCollapse:=True),
                Region("span2", "Public Shared Widening Operator CType(b As Base) As Integer ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
