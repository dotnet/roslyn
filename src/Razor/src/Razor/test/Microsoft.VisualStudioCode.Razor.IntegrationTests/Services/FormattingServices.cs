// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Services for formatting operations in integration tests.
/// </summary>
public class FormattingServices(IntegrationTestServices testServices) : ServiceBase(testServices)
{
    /// <summary>
    /// Formats the entire document using the command palette.
    /// </summary>
    public async Task FormatDocumentAsync()
    {
        // Formatting could be async, so make sure we save after the edit, so WaitForEditorTextChangeAsync works correctly
        await TestServices.Editor.SaveAsync();
        TestServices.Logger.Log("Formatting document via command palette...");
        await TestServices.Editor.ExecuteCommandAsync("Format Document");
        TestServices.Logger.Log("Format Document command executed");
        await TestServices.Editor.WaitForEditorDirtyAsync();
        TestServices.Logger.Log("Editor is dirty after formatting");
    }
}
