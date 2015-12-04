' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class DelegateDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of DelegateStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New DelegateDeclarationOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestDelegateWithComments() As Task
            Const code = "
{|span:'Hello
'World|}
Delegate Sub $$Foo()
"
            Await VerifyRegionsAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
