// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests;

public sealed class InfrastructureTests : AbstractEditorTest
{
    public InfrastructureTests()
        : base(nameof(InfrastructureTests), WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
    {
    }

    protected override string LanguageName => LanguageNames.CSharp;

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/73099")]
    public async Task CanCloseSaveDialog()
    {
        await SetUpEditorAsync(
            """

            namespace MyNamespace
            {
            $$
            }
            """,
            HangMitigatingCancellationToken);

        // Trigger a call to File.Close to ensure we can recover from it
        await TestServices.Input.SendAsync([(VirtualKeyCode.VK_F, VirtualKeyCode.MENU), VirtualKeyCode.VK_C], HangMitigatingCancellationToken);

        var modalWindow = IntegrationHelper.GetModalWindowFromParentWindow(await TestServices.Shell.GetMainWindowAsync(HangMitigatingCancellationToken));
        Assert.NotEqual(IntPtr.Zero, modalWindow);

        Assert.Equal("Microsoft Visual Studio", IntegrationHelper.GetTitleForWindow(modalWindow));

        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);

        modalWindow = IntegrationHelper.GetModalWindowFromParentWindow(await TestServices.Shell.GetMainWindowAsync(HangMitigatingCancellationToken));
        if (modalWindow != IntPtr.Zero)
        {
            switch (IntegrationHelper.GetTitleForWindow(modalWindow))
            {
                case "Default IME":
                    // "Default IME" was seen in local testing of this method
                    break;

                case "":
                    // Empty string was seen in CI for a case where no dialog was visible
                    break;

                case var title:
                    throw ExceptionUtilities.UnexpectedValue(title);
            }
        }
    }
}
