' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Async

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Async
    Public Class ChangeToAsyncTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToAsync)>
        Public Async Function CantAwaitAsyncSub1() As Threading.Tasks.Task
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
            Await TestAsync(initial, expected)
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing,
                New VisualBasicConvertToAsyncFunctionCodeFixProvider())
        End Function
    End Class
End Namespace
