﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertToInterpolatedString
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertToInterpolatedString
    Public Class ConvertRegularStringToInterpolatedStringTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New ConvertRegularStringToInterpolatedStringRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMissingOnRegularStringWithNoBraces() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        Dim v = [||]""string""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestOnRegularStringWithBraces() As Task
            Await TestInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        Dim v = [||]""string {""
    End Sub
End Class",
"
Public Class C
    Sub M()
        Dim v = $""string {{""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMissingOnInterpolatedString() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        Dim i = 0;
        Dim v = $[||]""string {i}""
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)>
        Public Async Function TestMissingOnRegularStringWithBracesAssignedToConst() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Class C
    Sub M()
        Const v = [||]""string {""
    End Sub
End Class")
        End Function
    End Class
End Namespace
