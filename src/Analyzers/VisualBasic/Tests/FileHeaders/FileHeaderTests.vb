' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.FileHeaders.VisualBasicFileHeaderDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.FileHeaders.VisualBasicFileHeaderCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.FileHeaders
    Public Class FileHeaderTests

        Private Const TestSettings As String = "
[*.vb]
file_header_template = Copyright (c) SomeCorp. All rights reserved.\nLicensed under the ??? license. See LICENSE file in the project root for full license information.
"

        Private Const TestSettingsWithEmptyLines As String = "
[*.vb]
file_header_template = \nCopyright (c) SomeCorp. All rights reserved.\n\nLicensed under the ??? license. See LICENSE file in the project root for full license information.\n
"

        ''' <summary>
        ''' Verifies that the analyzer will not report a diagnostic when the file header is not configured.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Theory>
        <InlineData("")>
        <InlineData("file_header_template =")>
        <InlineData("file_header_template = unset")>
        Public Async Function TestFileHeaderNotConfiguredAsync(fileHeaderTemplate As String) As Task
            Dim testCode = "Namespace N
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = testCode,
                .EditorConfig = $"
[*]
{fileHeaderTemplate}
"
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that the analyzer will report a diagnostic when the file is completely missing a header.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Theory>
        <InlineData(vbLf)>
        <InlineData(vbCrLf)>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1414432")>
        Public Async Function TestNoFileHeaderAsync(lineEnding As String) As Task
            Dim testCode = "[||]Namespace N
End Namespace
"
            Dim fixedCode = "' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Namespace N
End Namespace
"

            Dim test As VerifyVB.Test = New VerifyVB.Test With
            {
                .TestCode = testCode.ReplaceLineEndings(lineEnding),
                .FixedCode = fixedCode.ReplaceLineEndings(lineEnding),
                .EditorConfig = TestSettings
            }

            test.Options.Add(FormattingOptions2.NewLine, lineEnding)
            Await test.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that the analyzer will report a diagnostic when the file is completely missing a header.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Fact>
        Public Async Function TestNoFileHeaderWithUsingDirectiveAsync() As Task
            Dim testCode = "[||]Imports System

Namespace N
End Namespace
"
            Dim fixedCode = "' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Imports System

Namespace N
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that the analyzer will report a diagnostic when the file is completely missing a header.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Fact>
        Public Async Function TestNoFileHeaderWithBlankLineAndUsingDirectiveAsync() As Task
            Dim testCode = "[||]
Imports System

Namespace N
End Namespace
"
            Dim fixedCode = "' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Imports System

Namespace N
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that the analyzer will report a diagnostic when the file is completely missing a header.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Fact>
        Public Async Function TestNoFileHeaderWithWhitespaceLineAsync() As Task
            Dim testCode = "[||]    " & "
Imports System

Namespace N
End Namespace
"
            Dim fixedCode = "' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Imports System

Namespace N
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that the built-in variable <c>fileName</c> works as expected.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Fact>
        Public Async Function TestFileNameBuiltInVariableAsync() As Task
            Dim editorConfig = "
[*.vb]
file_header_template = {fileName} Copyright (c) SomeCorp. All rights reserved.\nLicensed under the ??? license. See LICENSE file in the project root for full license information.
"

            Dim testCode = "[||]Namespace N
End Namespace
"
            Dim fixedCode = "' Test0.vb Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Namespace N
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = editorConfig
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that a valid file header built using single line comments will not produce a diagnostic message.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Fact>
        Public Async Function TestValidFileHeaderWithSingleLineCommentsAsync() As Task
            Dim testCode = "' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Namespace Bar
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = testCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that a file header without text / only whitespace will produce the expected diagnostic message.
        ''' </summary>
        ''' <param name="comment">The comment text.</param>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Theory>
        <InlineData("[|'|]")>
        <InlineData("[|'|]    ")>
        Public Async Function TestInvalidFileHeaderWithoutTextAsync(comment As String) As Task
            Dim testCode = $"{comment}

Namespace Bar
End Namespace
"
            Dim fixedCode = "' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Namespace Bar
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Fact>
        Public Async Function TestInvalidFileHeaderWithWrongTextAsync() As Task
            Dim testCode = "[|'|] Copyright (c) OtherCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Namespace Bar
End Namespace
"
            Dim fixedCode = "' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Namespace Bar
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Theory>
        <InlineData("", "")>
        <InlineData(" ' Header", "")>
        <InlineData(" ' Header", " ' Header")>
        Public Async Function TestValidFileHeaderInRegionAsync(startLabel As String, endLabel As String) As Task
            Dim testCode = $"#Region ""Header""{startLabel}
' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.
#End Region{endLabel}

Namespace Bar
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = testCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Theory>
        <InlineData("", "")>
        <InlineData(" ' Header", "")>
        <InlineData(" ' Header", " ' Header")>
        Public Async Function TestInvalidFileHeaderWithWrongTextInRegionAsync(startLabel As String, endLabel As String) As Task
            Dim testCode = $"#Region ""Header""{startLabel}
[|'|] Copyright (c) OtherCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.
#End Region{endLabel}

Namespace Bar
End Namespace
"
            Dim fixedCode = $"' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

#Region ""Header""{startLabel}
' Copyright (c) OtherCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.
#End Region{endLabel}

Namespace Bar
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Theory>
        <InlineData("")>
        <InlineData("    ")>
        Public Async Function TestInvalidFileHeaderWithWrongTextAfterBlankLineAsync(firstLine As String) As Task
            Dim testCode = $"{firstLine}
[|'|] Copyright (c) OtherCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Namespace Bar
End Namespace
"
            Dim fixedCode = "' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Namespace Bar
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        ''' <summary>
        ''' Verifies that an invalid file header built using single line comments will produce the expected diagnostic message.
        ''' </summary>
        ''' <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        <Fact>
        Public Async Function TestInvalidFileHeaderWithWrongTextFollowedByCommentAsync() As Task
            Dim testCode = "[|'|] Copyright (c) OtherCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

'Imports System

Namespace Bar
End Namespace
"
            Dim fixedCode = "' Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

'Imports System

Namespace Bar
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = TestSettings
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestHeaderMissingRequiredNewLinesAsync() As Task
            Dim testCode = "[|'|] Copyright (c) SomeCorp. All rights reserved.
' Licensed under the ??? license. See LICENSE file in the project root for full license information.

Namespace Bar
End Namespace
"
            Dim fixedCode = "'
' Copyright (c) SomeCorp. All rights reserved.
'
' Licensed under the ??? license. See LICENSE file in the project root for full license information.
'

Namespace Bar
End Namespace
"

            Await New VerifyVB.Test With
            {
                .TestCode = testCode,
                .FixedCode = fixedCode,
                .EditorConfig = TestSettingsWithEmptyLines
            }.RunAsync()
        End Function

    End Class
End Namespace
