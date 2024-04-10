' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf.VisualBasicConvertTypeOfToNameOfDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf.VisualBasicConvertGetTypeToNameOfCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertGetTypeToNameOf
    Partial Public Class ConvertGetTypeToNameOfTests
        <Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)>
        Public Async Function BasicType() As Task
            Dim text = "
class Test
    sub Method()
        dim typeName = [|GetType(Test).Name|]
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim typeName = NameOf(Test)
    end sub
end class
"
            Await VerifyVB.VerifyCodeFixAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)>
        Public Async Function ClassLibraryType() As Task
            Dim text = "
class Test
    sub Method()
        dim typeName = [|GetType(System.String).Name|]
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim typeName = NameOf(System.String)
    end sub
end class
"
            Await VerifyVB.VerifyCodeFixAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)>
        Public Async Function ClassLibraryTypeWithImport() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim typeName = [|GetType(String).Name|]
    end sub
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim typeName = NameOf([String])
    end sub
end class
"
            Await VerifyVB.VerifyCodeFixAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)>
        Public Async Function NestedCall() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim typeName = Goo([|GetType(String).Name|])
    end sub

    function Goo(ByVal typeName As String) As Integer
    end function
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim typeName = Goo(NameOf([String]))
    end sub

    function Goo(ByVal typeName As String) As Integer
    end function
end class
"
            Await VerifyVB.VerifyCodeFixAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        Public Async Function NotOnVariableContainingType() As Task
            Dim text = "
imports System

class Test
    sub Method()
        dim typeVar = GetType(String)
        dim typeName = typeVar.Name 
    end sub
end class
"
            Await VerifyVB.VerifyCodeFixAsync(text, text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/54233")>
        Public Async Function OnVoid() As Task
            Dim text = "
imports System

class Test
    sub Method()
        dim typeVar = [|GetType(Void).Name|]
    end sub
end class
"
            Dim expected = "
imports System

class Test
    sub Method()
        dim typeVar = NameOf(Void)
    end sub
end class
"
            Await VerifyVB.VerifyCodeFixAsync(text, expected)
        End Function
    End Class
End Namespace
