' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateOverrides
Imports Microsoft.CodeAnalysis.PickMembers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateOverrides
    Public Class GenerateOverridesTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(Workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New GenerateOverridesCodeRefactoringProvider(DirectCast(parameters.fixProviderData, IPickMembersService))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateOverrides)>
        Public Async Function Test1() As Task
            Await TestWithPickMembersDialogAsync(
"
class C
    [||]
end class",
"
class C
    Public Overrides Function Equals(obj As Object) As Boolean
        Return MyBase.Equals(obj)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return MyBase.GetHashCode()
    End Function

    Public Overrides Function ToString() As String
        Return MyBase.ToString()
    End Function
end class", {"Equals", "GetHashCode", "ToString"})
        End Function
    End Class
End Namespace
