' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase

Namespace NS
    Public Class OverloadBaseTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New OverloadBaseCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddOverload)>
        Public Async Function TestAddOverloadsToProperty() As Task
            Await TestAsync(
"Class Application
    Shared Property Current As Application
End Class
Class App : Inherits Application
    [|Shared Property Current As App|]
End Class",
"Class Application
    Shared Property Current As Application
End Class
Class App : Inherits Application
    Overloads Shared Property Current As App
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddOverload)>
        Public Async Function TestAddOverloadsToFunction() As Task
            Await TestAsync(
"Class Application
    Shared Function Test() As Integer
        Return 1
    End Function
End Class
Class App : Inherits Application
    [|Shared Function Test() As Integer
        Return 2
    End Function|]
End Class",
"Class Application
    Shared Function Test() As Integer
        Return 1
    End Function
End Class
Class App : Inherits Application
    Overloads Shared Function Test() As Integer
        Return 2
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddOverload)>
        Public Async Function TestAddOverloadsToSub() As Task
            Await TestAsync(
"Class Application
    Shared Sub Test()
    End Sub
End Class
Class App : Inherits Application
    [|Shared Sub Test()
    End Sub|]
End Class",
"Class Application
    Shared Sub Test()
    End Sub
End Class
Class App : Inherits Application
    Overloads Shared Sub Test()
    End Sub
End Class")
        End Function
    End Class
End Namespace