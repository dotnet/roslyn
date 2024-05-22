// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests;

public class MefCompositionTests : AbstractIntegrationTest
{
    [IdeFact]
    public async Task AssertNoCompositionFailures()
    {
        // Read the .err file that contains errors; this isn't a great way to do it but we have no
        // better option at this point.
        var shell = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsSettingsManager, IVsSettingsManager>(HangMitigatingCancellationToken);
        Marshal.ThrowExceptionForHR(shell.GetApplicationDataFolder((uint)__VsApplicationDataFolder.ApplicationDataFolder_LocalSettings, out var applicationDataFolder));

        var compositionErrorFileLines = File.ReadAllLines(Path.Combine(applicationDataFolder, @"ComponentModelCache\Microsoft.VisualStudio.Default.err"));
        var compositionErrors = new List<string>();

        for (var i = 0; i < compositionErrorFileLines.Length; i++)
        {
            var line = compositionErrorFileLines[i];

            if (line.EndsWith("expected exactly 1 export matching constraints:") &&
                StartsWithApplicableSymbol(line))
            {
                var entireError = string.Join(Environment.NewLine, compositionErrorFileLines.Skip(i).TakeWhile(s => !string.IsNullOrWhiteSpace(s)));
                compositionErrors.Add(entireError);
            }
        }

        // Ideally I'd write Assert.Empty(compositionErrors), but since the string gets truncated we'll do this instead
        AssertEx.EqualOrDiff("", string.Join(Environment.NewLine, compositionErrors));
    }

    private static bool StartsWithApplicableSymbol(string s)
    {
        // ExternalAccess missing exports may mean the langauge isn't installed, or you aren't running
        // on fully matched bits; rather than flag them let's keep the noise down.
        if (s.StartsWith("Microsoft.CodeAnalysis.ExternalAccess"))
            return false;

        // Not actually our code
        if (s.StartsWith("Microsoft.CodeAnalysis.Editor.TypeScript"))
            return false;

        return s.StartsWith("Microsoft.CodeAnalysis") ||
               s.StartsWith("Microsoft.VisualStudio.LanguageServices") ||
               s.StartsWith("Roslyn");
    }
}
