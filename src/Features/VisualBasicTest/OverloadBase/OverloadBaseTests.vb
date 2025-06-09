' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase.OverloadBaseCodeFixProvider)

Namespace NS
    Public Class OverloadBaseTests
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddOverload)>
        Public Async Function TestAddOverloadsToProperty() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class Application
    Shared Property Current As Application
End Class
Class App : Inherits Application
    Shared Property {|BC40003:Current|} As App
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
            Await VerifyVB.VerifyCodeFixAsync(
"Class Application
    Shared Function Test() As Integer
        Return 1
    End Function
End Class
Class App : Inherits Application
    Shared Function {|BC40003:Test|}() As Integer
        Return 2
    End Function
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
            Await VerifyVB.VerifyCodeFixAsync(
"Class Application
    Shared Sub Test()
    End Sub
End Class
Class App : Inherits Application
    Shared Sub {|BC40003:Test|}()
    End Sub
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddOverload)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/21948")>
        Public Async Function TestAddOverloadsToSub_HandlingTrivia() As Task
            Await VerifyVB.VerifyCodeFixAsync("
Class Base
    Sub M()

    End Sub
End Class

Class Derived
    Inherits Base
    ' Trivia
    Sub {|BC40003:M|}()
    End Sub ' Trivia2
End Class
", "
Class Base
    Sub M()

    End Sub
End Class

Class Derived
    Inherits Base
    ' Trivia
    Overloads Sub M()
    End Sub ' Trivia2
End Class
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddShadows)>
        Public Async Function TestAddShadowsToProperty() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class Application
    Shared Sub Current()
    End Sub
End Class
Class App : Inherits Application
    Shared Property {|BC40004:Current|} As App
End Class",
"Class Application
    Shared Sub Current()
    End Sub
End Class
Class App : Inherits Application
    Shared Shadows Property Current As App
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddShadows)>
        Public Async Function TestAddShadowsToFunction() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class Application
    Shared Property Test As Integer
End Class
Class App : Inherits Application
    Shared Function {|BC40004:Test|}() As Integer
        Return 2
    End Function
End Class",
"Class Application
    Shared Property Test As Integer
End Class
Class App : Inherits Application
    Shared Shadows Function Test() As Integer
        Return 2
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddShadows)>
        Public Async Function TestAddShadowsToSub() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Class Application
    Shared Property Test As Integer
End Class
Class App : Inherits Application
    Shared Sub {|BC40004:Test|}()
    End Sub
End Class",
"Class Application
    Shared Property Test As Integer
End Class
Class App : Inherits Application
    Shared Shadows Sub Test()
    End Sub
End Class")
        End Function
    End Class
End Namespace
