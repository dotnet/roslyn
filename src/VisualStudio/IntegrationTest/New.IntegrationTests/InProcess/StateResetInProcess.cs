// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices;

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
            globalOptions.SetGlobalOption(new OptionKey(NavigationBarViewOptions.ShowNavigationBar, LanguageNames.CSharp), true);
        }

        public Task ResetHostSettingsAsync(CancellationToken cancellationToken)
        {
            _ = cancellationToken;

            return Task.CompletedTask;
        }
    }
}
