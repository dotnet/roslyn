' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities.Formatting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting
    Public Class VisualBasicNewDocumentFormattingServiceTests
        Inherits AbstractNewDocumentFormattingServiceTests

        Protected Overrides ReadOnly Property Language As String = LanguageNames.VisualBasic

        Protected Overrides Function CreateTestWorkspace(testCode As String, parseOptions As ParseOptions) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(testCode, parseOptions)
        End Function

        <Fact>
        Public Async Function TestFileBanners() As Task
            Await TestAsync(
                testCode:="Imports System

Namespace Goo

End Namespace",
                expected:="' This is a banner.

Imports System

Namespace Goo

End Namespace",
                options:=New OptionsCollection(LanguageNames.VisualBasic) From {
                    {CodeStyleOptions2.FileHeaderTemplate, "This is a banner."}
                })
        End Function

        <Fact>
        Public Async Function TestOrganizeUsings() As Task
            Await TestAsync(
                testCode:="Imports Aaa
Imports System

Namespace Goo

End Namespace",
                expected:="Imports System
Imports Aaa

Namespace Goo

End Namespace")
        End Function
    End Class
End Namespace
