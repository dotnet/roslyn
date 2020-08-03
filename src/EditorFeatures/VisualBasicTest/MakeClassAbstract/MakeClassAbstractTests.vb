﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.MakeClassAbstract

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.MakeClassAbstract
    Public Class MakeClassAbstractTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicMakeClassAbstractCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestMethod_CodeFix() As Task
            Await TestInRegularAndScript1Async("
Public Class [|Foo|]
    Public MustOverride Sub M()
End Class",
"
Public MustOverride Class Foo
    Public MustOverride Sub M()
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestMethodEnclosingClassWithoutAccessibility_NoCodeFix() As Task
            Await TestMissingInRegularAndScriptAsync("
Class Foo
    Public MustOverride Sub [|M|]()
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestMethodEnclosingClassDocumentationComment() As Task
            Await TestMissingInRegularAndScriptAsync("
''' <summary>
''' Some class comment.
''' </summary>
Public Class Foo
    Public MustOverride Sub [|M|]()
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestProperty() As Task
            Await TestMissingInRegularAndScriptAsync("
Public Class Foo
    Public MustOverride Property [|P|] As Object
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestIndexer() As Task
            Await TestMissingInRegularAndScriptAsync("
Public Class Foo
    Default Public MustOverride Property [|Item|](ByVal o As Object) As Object
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestEvent() As Task
            Await TestMissingInRegularAndScriptAsync("
Public Class Foo
    Public MustOverride Custom Event [|E|] As EventHandler
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestMethodWithBody() As Task
            Await TestMissingInRegularAndScriptAsync("
Public Class Foo
    Public MustOverride Function [|M|]() As Integer
        Return 3
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestPropertyWithBody() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Class Foo
    Public MustOverride ReadOnly Property [|P|] As Integer
        Get
            Return 3
        End Get
    End Property
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestStructNestedInClass() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Class C
    Public Structure S
        Public MustOverride Sub [|Foo|]()
    End Structure
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function TestMethodEnclosingClassStatic() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Public Static Class Foo
    Public MustOverride Sub [|M|]()
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)>
        Public Async Function FixAll() As Task
            Await TestMissingInRegularAndScriptAsync("
Namespace NS
    Public Class C1
        Public MustOverride Sub {|FixAllInDocument:M|}()
        Public MustOverride Property P As Object
        Default Public MustOverride Property Item(ByVal o As Object) As Object
    End Class

    Public Class C2
        Public MustOverride Sub M()
    End Class

    Public Class C3
        Public Class InnerClass
            Public MustOverride Sub M()
        End Class
    End Class
End Namespace")
        End Function
    End Class
End Namespace
