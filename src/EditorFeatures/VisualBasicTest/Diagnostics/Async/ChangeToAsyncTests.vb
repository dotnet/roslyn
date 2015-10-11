' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Async

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Async
    Public Class ChangeToAsyncTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToAsync)>
        Public Sub CantAwaitAsyncSub1()
            Dim initial =
    <ModuleDeclaration>
    Async Function rtrt() As Task
        [|Await gt()|]
    End Function
 
    Async Sub gt()
    End Sub
</ModuleDeclaration>
            Dim expected =
    <ModuleDeclaration>
    Async Function rtrt() As Task
        Await gt()
    End Function
 
    Async Function gt() As Task
    End Function
</ModuleDeclaration>
            Test(initial, expected)
        End Sub

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing,
                New VisualBasicConvertToAsyncFunctionCodeFixProvider())
        End Function
    End Class
End Namespace
