// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
[Trait(Traits.Feature, Traits.Features.NetCore)]
public class BasicSquigglesNetCore : BasicSquigglesCommon
{
    public BasicSquigglesNetCore()
        : base(WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(false);

        // The VisualBasicNetCoreClassLibrary template does not open a file automatically.
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, WellKnownProjectTemplates.VisualBasicNetCoreClassLibraryClassFileName, HangMitigatingCancellationToken);
    }
}
