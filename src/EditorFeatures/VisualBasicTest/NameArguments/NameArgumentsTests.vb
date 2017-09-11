' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.NameArguments

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseExplicitTupleName
    Public Class NameArgumentsTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicNameArgumentsDiagnosticAnalyzer(),
                    New VisualBasicNameArgumentsCodeFixProvider())
        End Function

        Private Shared ReadOnly s_parseOptions As VisualBasicParseOptions =
            VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)>
        Public Async Function TestLiteralInInvocation() As Task
            Await TestAsync(
"
Class C
    Sub M(a As Integer, b As Integer)
        M([||]1, 2)
    End Sub
End Class",
"
Class C
    Sub M(a As Integer, b As Integer)
        M(a:=1, 2)
    End Sub
End Class", s_parseOptions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)>
        Public Async Function TestLiteralInObjectCreation() As Task
            Await TestAsync(
"
Class C
    Sub New(a As Integer, b As Integer)
        Dim x = New C([||]1, 2)
    End Sub
End Class",
"
Class C
    Sub New(a As Integer, b As Integer)
        Dim x = New C(a:=1, 2)
    End Sub
End Class", s_parseOptions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)>
        Public Async Function TestLiteralInAttribute() As Task
            Await TestAsync(
"
<C([||]1, 2)>
Class C
    Inherits System.Attribute
    Sub New(a As Integer, b As Integer)
    End Sub
End Class",
"
<C(a:=1, 2)>
Class C
    Inherits System.Attribute
    Sub New(a As Integer, b As Integer)
    End Sub
End Class", s_parseOptions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)>
        Public Async Function TestLiteralInThisCreation() As Task
            Await TestAsync(
"
Class C
    Sub New(a As Integer, b As Integer)
        Me.New([||]1, 2)
    End Sub
End Class",
"
Class C
    Sub New(a As Integer, b As Integer)
        Me.New(a:=1, 2)
    End Sub
End Class", s_parseOptions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)>
        Public Async Function TestLiteralInBaseCreation() As Task
            Await TestAsync(
"
Class C
    Inherits Base
    Sub New()
        MyBase.New([||]1, 2)
    End Sub
End Class
Class Base
    Sub New(a As Integer, b As Integer)
    End Sub
End Class
",
"
Class C
    Inherits Base
    Sub New()
        MyBase.New(a:=1, 2)
    End Sub
End Class
Class Base
    Sub New(a As Integer, b As Integer)
    End Sub
End Class
", s_parseOptions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)>
        Public Async Function TestLiteralInArray() As Task
            Await TestActionCountAsync(
"
Class C
    Dim x As Integer(,)
    Sub M(y As Integer)
        M(x([||]1, 2))
    End Sub
End Class
", count:=0, parameters:=New TestParameters(s_parseOptions))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)>
        Public Async Function TestAllLiteralsInArguments() As Task
            Await TestAsync(
"
Class C
    Sub M(a As Integer, b As Integer)
        M({|FixAllInDocument:1|}, 2)
    End Sub
End Class",
"
Class C
    Sub M(a As Integer, b As Integer)
        M(a:=1, b:=2)
    End Sub
End Class", s_parseOptions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)>
        Public Async Function TestAllLiteralsInArgumentsWithTrivia() As Task
            Await TestAsync(
"
Class C
    Sub M(a As Integer, b As Integer)
        M( ' before
            {|FixAllInDocument:1|}, ' after
            2)
    End Sub
End Class",
"
Class C
    Sub M(a As Integer, b As Integer)
        M( ' before
            a:=1, ' after
            b:=2)
    End Sub
End Class", s_parseOptions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsNameArguments)>
        Public Async Function TestAllLiteralsInInvocationWithNesting() As Task
            Await TestAsync(
"
Class C
    Function M(a As Integer, b As Integer) As Integer
        M({|FixAllInDocument:1|}, M(1, 2))
    End Function
End Class",
"
Class C
    Function M(a As Integer, b As Integer) As Integer
        M(a:=1, M(a:=1, b:=2))
    End Function
End Class", s_parseOptions)
        End Function

    End Class
End Namespace
