' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Testing
Imports Microsoft.CodeAnalysis.VisualBasic.MakeDeclarationPartial

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.MakeDeclarationPartial
    <Trait(Traits.Feature, Traits.Features.CodeActionsMakeDeclarationPartial)>
    Public Class MakeDeclarationPartialTests
        Private Class TestVerifier
            Inherits VisualBasicCodeFixVerifier(Of EmptyDiagnosticAnalyzer, VisualBasicMakeDeclarationPartialCodeFixProvider).Test
        End Class

        <Fact>
        Public Async Function OutsideNamespace() As Task
            Dim document1 = "
Partial Class Declaration
End Class

Class {|BC40046:Declaration|}
End Class"

            Dim document2 = "
Class {|BC40046:Declaration|}
End Class"

            Dim fixedDocument1 = "
Partial Class Declaration
End Class

Partial Class Declaration
End Class"

            Dim test = New TestVerifier()

            test.TestState.Sources.Add(document1)
            test.TestState.Sources.Add(document2)

            test.FixedState.Sources.Add(fixedDocument1)
            test.FixedState.Sources.Add(document2)

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function InsideOneNamespace() As Task
            Dim document1 = "
Namespace TestNamespace
    Partial Class Declaration
    End Class

    Class {|BC40046:Declaration|}
    End Class
End Namespace"

            Dim document2 = "
Namespace TestNamespace
    Class {|BC40046:Declaration|}
    End Class
End Namespace"

            Dim fixedDocument1 = "
Namespace TestNamespace
    Partial Class Declaration
    End Class

    Partial Class Declaration
    End Class
End Namespace"

            Dim test = New TestVerifier()

            test.TestState.Sources.Add(document1)
            test.TestState.Sources.Add(document2)

            test.FixedState.Sources.Add(fixedDocument1)
            test.FixedState.Sources.Add(document2)

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function InsideTwoEqualNamespaces() As Task
            Dim document1 = "
Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace

Namespace TestNamespace
    Class {|BC40046:Declaration|}
    End Class
End Namespace"

            Dim document2 = "
Namespace TestNamespace
    Class {|BC40046:Declaration|}
    End Class
End Namespace"

            Dim fixedDocument1 = "
Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace

Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace"

            Dim test = New TestVerifier()

            test.TestState.Sources.Add(document1)
            test.TestState.Sources.Add(document2)

            test.FixedState.Sources.Add(fixedDocument1)
            test.FixedState.Sources.Add(document2)

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function WithOtherModifiers() As Task
            Dim document1 = "
Partial Public Class Declaration
End Class

Public Class {|BC40046:Declaration|}
End Class"

            Dim document2 = "
Public Class {|BC40046:Declaration|}
End Class"

            Dim fixedDocument1 = "
Partial Public Class Declaration
End Class

Partial Public Class Declaration
End Class"

            Dim test = New TestVerifier()

            test.TestState.Sources.Add(document1)
            test.TestState.Sources.Add(document2)

            test.FixedState.Sources.Add(fixedDocument1)
            test.FixedState.Sources.Add(document2)

            Await test.RunAsync()
        End Function
    End Class
End Namespace
