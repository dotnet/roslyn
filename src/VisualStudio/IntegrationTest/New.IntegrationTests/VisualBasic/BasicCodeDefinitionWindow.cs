// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

public class BasicCodeDefinitionWindow : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicCodeDefinitionWindow()
        : base(nameof(BasicCodeDefinitionWindow))
    {
    }

    [IdeTheory]
    [CombinatorialData]
    public async Task CodeDefinitionWindowOpensMetadataAsSource(bool enableDecompilation)
    {
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources, enableDecompilation);

        await TestServices.CodeDefinitionWindow.ShowAsync(HangMitigatingCancellationToken);

        // Opening the code definition window sets focus to the code definition window, but we want to go back to editing
        // our regular file.
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);

        await SetUpEditorAsync(@"
Public Class Test
    Dim field As $$Integer
End Class
", HangMitigatingCancellationToken);

        // If we are enabling decompilation, we'll get C# code since we don't support decompiling into VB
        if (enableDecompilation)
            Assert.Contains("public struct Int32", await TestServices.CodeDefinitionWindow.GetCurrentLineTextAsync(HangMitigatingCancellationToken));
        else
            Assert.Contains("Public Structure Int32", await TestServices.CodeDefinitionWindow.GetCurrentLineTextAsync(HangMitigatingCancellationToken));
    }
}
