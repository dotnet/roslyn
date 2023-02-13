' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ConvertToAsync

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertToAsync
    Public Class ConvertToAsyncTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicConvertToAsyncFunctionCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToAsync)>
        Public Async Function CantAwaitAsyncSub1() As Task
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

Async Function gt() As Task
End Function
</ModuleDeclaration>
            Await TestAsync(initial, expected)
        End Function
    End Class
End Namespace
