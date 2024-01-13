' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.MakeTypePartial.VisualBasicMakeTypePartialCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.MakeTypePartial
    <Trait(Traits.Feature, Traits.Features.CodeActionsMakeTypePartial)>
    Public Class MakeTypePartialTests
        <Fact>
        Public Async Function OutsideNamespace() As Task
            Dim document = "
Partial Class Declaration
End Class

Class {|BC40046:Declaration|}
End Class

Class {|BC40046:Declaration|}
End Class"

            Dim fixedDocument = "
Partial Class Declaration
End Class

Partial Class Declaration
End Class

Partial Class Declaration
End Class"

            Dim test = New VerifyVB.Test With {
                .TestCode = document,
                .FixedCode = fixedDocument
            }

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function InsideOneNamespace() As Task
            Dim document = "
Namespace TestNamespace
    Partial Class Declaration
    End Class

    Class {|BC40046:Declaration|}
    End Class

    Class {|BC40046:Declaration|}
    End Class
End Namespace"

            Dim fixedDocument = "
Namespace TestNamespace
    Partial Class Declaration
    End Class

    Partial Class Declaration
    End Class

    Partial Class Declaration
    End Class
End Namespace"

            Dim test = New VerifyVB.Test With {
                .TestCode = document,
                .FixedCode = fixedDocument
            }

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function InsideSeveralEqualNamespaces() As Task
            Dim document = "
Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace

Namespace TestNamespace
    Class {|BC40046:Declaration|}
    End Class
End Namespace

Namespace TestNamespace
    Class {|BC40046:Declaration|}
    End Class
End Namespace"

            Dim fixedDocument = "
Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace

Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace

Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace"

            Dim test = New VerifyVB.Test With {
                .TestCode = document,
                .FixedCode = fixedDocument
            }

            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function WithOtherModifiers() As Task
            Dim document = "
Partial Public Class Declaration
End Class

Public Class {|BC40046:Declaration|}
End Class

Public Class {|BC40046:Declaration|}
End Class"

            Dim fixedDocument = "
Partial Public Class Declaration
End Class

Partial Public Class Declaration
End Class

Partial Public Class Declaration
End Class"

            Dim test = New VerifyVB.Test With {
                .TestCode = document,
                .FixedCode = fixedDocument
            }

            Await test.RunAsync()
        End Function
    End Class
End Namespace
