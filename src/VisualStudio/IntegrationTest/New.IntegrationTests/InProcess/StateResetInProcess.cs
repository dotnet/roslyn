// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal class StateResetInProcess : InProcComponent
    {
        public StateResetInProcess(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task ResetGlobalOptionsAsync(CancellationToken cancellationToken)
        {
            var globalOptions = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            ResetPerLanguageOption(globalOptions, NavigationBarViewOptions.ShowNavigationBar);

            static void ResetPerLanguageOption<T>(IGlobalOptionService globalOptions, PerLanguageOption<T> option)
            {
                globalOptions.SetGlobalOption(new OptionKey(option, LanguageNames.CSharp), option.DefaultValue);
                globalOptions.SetGlobalOption(new OptionKey(option, LanguageNames.VisualBasic), option.DefaultValue);
            }
        }

        public async Task ResetHostSettingsAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Close all Find References windows
            await foreach (var window in TestServices.Shell.EnumerateWindowsAsync(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Tool, cancellationToken).WithCancellation(cancellationToken))
            {
                ErrorHandler.ThrowOnFailure(window.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out var captionObj));
                if (Regex.IsMatch($"{captionObj}", "^(?:'.*' references|Find All References(?: \\d)?)$"))
                {
                    window.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                }
            }
        }
    }
}
