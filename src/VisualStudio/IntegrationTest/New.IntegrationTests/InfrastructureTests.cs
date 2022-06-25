// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests
{
    public class InfrastructureTests : AbstractEditorTest
    {
        public InfrastructureTests()
            : base(nameof(InfrastructureTests))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact]
        public async Task CanCloseSaveDialog()
        {
            await SetUpEditorAsync(
                @"
namespace MyNamespace
{
$$
}",
                HangMitigatingCancellationToken);

            // Trigger a call to File.Close to ensure we can recover from it
            await TestServices.Input.SendAsync(new KeyPress(VirtualKey.F, ShiftState.Alt), VirtualKey.C);

            var modalWindow = IntegrationHelper.GetModalWindowFromParentWindow(await TestServices.Shell.GetMainWindowAsync(HangMitigatingCancellationToken));
            Assert.NotEqual(IntPtr.Zero, modalWindow);

            Assert.Equal("Microsoft Visual Studio", IntegrationHelper.GetTitleForWindow(modalWindow));

            await TestServices.Input.SendWithoutActivateAsync(VirtualKey.Escape);

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
}
