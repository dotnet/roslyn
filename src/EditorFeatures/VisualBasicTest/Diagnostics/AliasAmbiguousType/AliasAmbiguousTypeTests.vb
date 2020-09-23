' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.AliasAmbiguousType

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.AliasAmbiguousType
    Public Class AliasAmbiguousTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicAliasAmbiguousTypeCodeFixProvider())
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        Private Shared Function GetAmbiguousDefinition(ByVal typeDefinion As String) As String
            Return $"
Namespace N1
    {typeDefinion}
End Namespace 
Namespace N2
    {typeDefinion}
End Namespace"
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)>
        Public Async Function TestAmbiguousClassObjectCreationGlobalImports() As Task
            Dim classDef = GetAmbiguousDefinition("
Public Class Ambiguous
End Class")
            Dim initialMarkup = "
Imports N1
Imports N2
" & classDef & "

Namespace N3
    Class C
        Private Sub M()
            Dim a = New [|Ambiguous|]()
        End Sub
    End Class
End Namespace"
            Dim expectedMarkupTemplate = "
Imports N1
Imports N2
{0}
" & classDef & "

Namespace N3
    Class C
        Private Sub M()
            Dim a = New Ambiguous()
        End Sub
    End Class
End Namespace"
            Await TestInRegularAndScriptAsync(initialMarkup, String.Format(expectedMarkupTemplate, "Imports Ambiguous = N1.Ambiguous"), index:=0)
            Await TestInRegularAndScriptAsync(initialMarkup, String.Format(expectedMarkupTemplate, "Imports Ambiguous = N2.Ambiguous"), index:=1)
            Await TestSmartTagTextAsync(initialMarkup, "Imports Ambiguous = N1.Ambiguous", index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)>
        Public Async Function TestAmbiguousAttribute() As Task
            Dim classDef = GetAmbiguousDefinition("
    Class AAttribute
        Inherits System.Attribute
    End Class
")
            Dim initialMarkup = "
Imports N1
Imports N2
" & classDef & "

<[|A|]()>
Class C
End Class"
            Dim expectedMarkupTemplate = "
Imports N1
Imports N2
Imports AAttribute = N1.AAttribute
" & classDef & "

<[|A|]()>
Class C
End Class"
            Await TestInRegularAndScriptAsync(initialMarkup, expectedMarkupTemplate)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)>
        Public Async Function TestAmbiguousBug4817() As Task
            Dim initialMarkup = "
Imports A
Imports B
Class A
    Shared Sub Goo()
    End Sub
End Class
Class B
    Inherits A
    Overloads Shared Sub Goo(x As Integer)
    End Sub
End Class
Module C
    Sub Main()
        [|Goo|]()
    End Sub
End Module
"
            Await TestMissingAsync(initialMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)>
        Public Async Function TestAmbiguousClassInModule() As Task
            Dim initialMarkup = "
Imports N1, N2
Namespace N1
    Module K
        Class Goo
        End Class
    End Module
End Namespace
Namespace N2
    Module L
        Class Goo
        End Class
    End Module
End Namespace
Class A
    Public d As [|Goo|]
End Class
"
            Dim expectedMarkup = "
Imports N1, N2
Imports Goo = N1.Goo

Namespace N1
    Module K
        Class Goo
        End Class
    End Module
End Namespace
Namespace N2
    Module L
        Class Goo
        End Class
    End Module
End Namespace
Class A
    Public d As Goo
End Class
"
            Await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAliasAmbiguousType)>
        Public Async Function TestAmbiguousInterfaceNameReferencedInSmallCaps() As Task
            Dim initialMarkup = "
Imports N1, N2
Namespace N1
    Interface I1
    End Interface
End Namespace
Namespace N2
    Interface I1
    End Interface
End Namespace
Public Class Cls2
    Implements [|i1|]
End Class
"
            Dim expectedMarkup = "
Imports N1, N2
Imports I1 = N1.I1

Namespace N1
    Interface I1
    End Interface
End Namespace
Namespace N2
    Interface I1
    End Interface
End Namespace
Public Class Cls2
    Implements i1
End Class
"
            Await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup)
        End Function
    End Class
End Namespace

