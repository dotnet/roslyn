' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.MakeDeclarationpartial

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.MakeDeclarationPartial
    <Trait(Traits.Feature, Traits.Features.CodeActionsMakeDeclarationPartial)>
    Public Class MakeDeclarationPartialTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicMakeDeclarationPartialCodeFixProvider())
        End Function

        <Fact>
        Public Async Function OutsideNamespace() As Task
            Await TestInRegularAndScriptAsync(
<Workspace>
    <Project Language="Visual Basic">
        <Document>
Partial Class Declaration
End Class

Class Declaration
End Class
        </Document>
        <Document>
Class [|Declaration|]
End Class
        </Document>
    </Project>
</Workspace>.ToString(),
<Workspace>
    <Project Language="Visual Basic">
        <Document>
Partial Class Declaration
End Class

Class Declaration
End Class
        </Document>
        <Document>
Partial Class Declaration
End Class
        </Document>
    </Project>
</Workspace>.ToString())
        End Function

        <Fact>
        Public Async Function InsideOneNamespace() As Task
            Await TestInRegularAndScriptAsync(
<Workspace>
    <Project Language="Visual Basic">
        <Document>
Namespace TestNamespace
    Partial Class Declaration
    End Class

    Class Declaration
    End Class
End Namespace
        </Document>
        <Document>
Namespace TestNamespace
    Class [|Declaration|]
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>.ToString(),
<Workspace>
    <Project Language="Visual Basic">
        <Document>
Namespace TestNamespace
    Partial Class Declaration
    End Class

    Class Declaration
    End Class
End Namespace
        </Document>
        <Document>
Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>.ToString())
        End Function

        <Fact>
        Public Async Function InsideTwoEqualNamespaces() As Task
            Await TestInRegularAndScriptAsync(
<Workspace>
    <Project Language="Visual Basic">
        <Document>
Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace

Namespace TestNamespace
    Class Declaration
    End Class
End Namespace
        </Document>
        <Document>
Namespace TestNamespace
    Class [|Declaration|]
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>.ToString(),
<Workspace>
    <Project Language="Visual Basic">
        <Document>
Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace

Namespace TestNamespace
    Class Declaration
    End Class
End Namespace
        </Document>
        <Document>
Namespace TestNamespace
    Partial Class Declaration
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>.ToString())
        End Function

        <Fact>
        Public Async Function WithOtherModifiers() As Task
            Await TestInRegularAndScriptAsync(
<Workspace>
    <Project Language="Visual Basic">
        <Document>
Partial Public Class Declaration
End Class

Public Class Declaration
End Class
        </Document>
        <Document>
Public Class [|Declaration|]
End Class
        </Document>
    </Project>
</Workspace>.ToString(),
<Workspace>
    <Project Language="Visual Basic">
        <Document>
Partial Public Class Declaration
End Class

Public Class Declaration
End Class
        </Document>
        <Document>
Partial Public Class Declaration
End Class
        </Document>
    </Project>
</Workspace>.ToString())
        End Function
    End Class
End Namespace
