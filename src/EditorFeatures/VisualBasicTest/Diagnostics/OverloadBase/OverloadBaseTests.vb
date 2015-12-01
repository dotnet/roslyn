' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace NS
    Public Class OverloadBaseTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New OverloadBaseCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddOverload)>
        Public Async Function TestAddOverloadsToProperty() As Task
            Await TestAsync(
            NewLines("Class Application \n Shared Property Current As Application \n End Class \n Class App : Inherits Application \n [|Shared Property Current As App|] \n End Class"),
            NewLines("Class Application \n Shared Property Current As Application \n End Class \n Class App : Inherits Application \n Overloads Shared Property Current As App \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddOverload)>
        Public Async Function TestAddOverloadsToFunction() As Task
            Await TestAsync(
            NewLines("Class Application \n Shared Function Test() As Integer \n Return 1 \n End Function \n End Class \n Class App : Inherits Application \n [|Shared Function Test() As Integer \n Return 2 \n End Function|] \n End Class"),
            NewLines("Class Application \n Shared Function Test() As Integer \n Return 1 \n End Function \n End Class \n Class App : Inherits Application \n Overloads Shared Function Test() As Integer \n Return 2 \n End Function \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddOverload)>
        Public Async Function TestAddOverloadsToSub() As Task
            Await TestAsync(
            NewLines("Class Application \n Shared Sub Test() \n End Sub \n End Class \n Class App : Inherits Application \n [|Shared Sub Test() \n End Sub|] \n End Class"),
            NewLines("Class Application \n Shared Sub Test() \n End Sub \n End Class \n Class App : Inherits Application \n Overloads Shared Sub Test() \n End Sub \n End Class"))
        End Function
    End Class
End Namespace