// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.RoslynTools.Authentication.PopUps;

namespace Microsoft.RoslynTools.UnitTests.Authentication;

public class UxManagerTests
{
    [Fact]
    public void ParsesEditorCommandWithArguments()
    {
        var command = UxManager.GetParsedCommand("code --wait");

        Assert.Equal("code", command.FileName);
        Assert.Equal(" --wait", command.Arguments);
    }

    [Fact]
    public void ParsesQuotedEditorPathWithArguments()
    {
        var command = UxManager.GetParsedCommand(@"'C:\Users\user\AppData\Local\Programs\Microsoft VS Code\Code.exe' --wait");

        Assert.Equal(@"C:\Users\user\AppData\Local\Programs\Microsoft VS Code\Code.exe", command.FileName);
        Assert.Equal(" --wait", command.Arguments);
    }

    [Fact]
    public void ParsesQuotedEditorPathWithoutArguments()
    {
        var command = UxManager.GetParsedCommand(@"'C:\Users\user\AppData\Local\Programs\Microsoft VS Code\Code.exe'");

        Assert.Equal(@"C:\Users\user\AppData\Local\Programs\Microsoft VS Code\Code.exe", command.FileName);
        Assert.Equal(string.Empty, command.Arguments);
    }

    [Fact]
    public void ParsesDoubleQuotedEditorPathWithForwardSlashes()
    {
        var command = UxManager.GetParsedCommand("\"C:/Program Files/Microsoft VS Code/Code.exe\"");

        Assert.Equal("C:/Program Files/Microsoft VS Code/Code.exe", command.FileName);
        Assert.Equal(string.Empty, command.Arguments);
    }
}
