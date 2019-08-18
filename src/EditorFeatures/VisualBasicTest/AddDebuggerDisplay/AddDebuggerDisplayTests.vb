' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddDebuggerDisplay

    <Trait(Traits.Feature, Traits.Features.CodeActionsAddDebuggerDisplay)>
    Public NotInheritable Class AddDebuggerDisplayTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicAddDebuggerDisplayCodeRefactoringProvider
        End Function
    End Class
End Namespace
