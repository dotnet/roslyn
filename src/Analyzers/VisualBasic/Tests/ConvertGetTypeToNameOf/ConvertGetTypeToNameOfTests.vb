' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertGetTypeToNameOf
    Partial Public Class ConvertGetTypeToNameOfTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicConvertTypeOfToNameOfDiagnosticAnalyzer(), New VisualBasicConvertGetTypeToNameOfCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)>
        Public Async Function BasicType() As Task
            Dim text = "
class Test
    sub Method()
        dim typeName = [||]GetType(Test).Name
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim typeName = [||]NameOf(Test)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)>
        Public Async Function ClassLibraryType() As Task
            Dim text = "
class Test
    sub Method()
        dim typeName = [||]GetType(System.String).Name
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim typeName = [||]NameOf(System.String)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)>
        Public Async Function ClassLibraryTypeWithImport() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim typeName = [||]GetType(String).Name
    end sub
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim typeName = [||]NameOf([String])
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)>
        Public Async Function NestedCall() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim typeName = Foo([||]GetType(String).Name)
    end sub

    sub Foo(ByVal typeName As String)
    end sub
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim typeName = Foo([||]NameOf([String]))
    end sub

    sub Foo(ByVal typeName As String)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function NotOnVariableContainingType() As Task
            Await TestMissingInRegularAndScriptAsync("
import System

class Test
    sub Method()
        dim typeVar = [||]GetType(String)
        dim typeName = typeVar.Name 
    end sub
end class
")
        End Function

    End Class
End Namespace
