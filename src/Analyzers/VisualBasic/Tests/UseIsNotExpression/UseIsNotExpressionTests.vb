' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.UseIsNotExpression.VisualBasicUseIsNotExpressionDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.UseIsNotExpression.VisualBasicUseIsNotExpressionCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseIsNotExpression
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNotExpression)>
    Partial Public Class UseIsNotExpressionTests
        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46706")>
        Public Async Function TestIsExpression() As Task
            Await New VerifyVB.Test With {
                .TestCode = "
class C
    sub M(o as object)
        if not o [|is|] nothing
        end if
    end sub
end class",
                .FixedCode = "
class C
    sub M(o as object)
        if o IsNot nothing
        end if
    end sub
end class"
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestTypeOfIsExpression() As Task
            Await New VerifyVB.Test With {
                .TestCode = "
class C
    sub M(o as object)
        if not typeof o [|is|] string
        end if
    end sub
end class",
                .FixedCode = "
class C
    sub M(o as object)
        if typeof o IsNot string
        end if
    end sub
end class"
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestVB12() As Task
            Await New VerifyVB.Test With {
                .TestCode = "
class C
    sub M(o as object)
        if not o is nothing
        end if
    end sub
end class",
                .LanguageVersion = LanguageVersion.VisualBasic12
            }.RunAsync()
        End Function
    End Class
End Namespace
