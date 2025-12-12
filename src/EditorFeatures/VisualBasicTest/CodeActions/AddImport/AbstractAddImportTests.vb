' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.AddImport
    Public MustInherit Class AbstractAddImportTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overloads Async Function TestAsync(initialMarkup As String,
                expectedMarkup As String,
                testHost As TestHost,
                Optional index As Integer = 0,
                Optional priority As CodeActionPriority? = Nothing,
                Optional placeSystemFirst As Boolean = True) As Task

            Await TestInRegularAndScriptAsync(
                initialMarkup, expectedMarkup, index,
                parameters:=New TestParameters(
                    options:=[Option](GenerationOptions.PlaceSystemNamespaceFirst, placeSystemFirst),
                    testHost:=testHost,
                    priority:=priority))
        End Function
    End Class
End Namespace
