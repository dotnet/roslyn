// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpCodeDefinitionWindow : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpCodeDefinitionWindow()
            : base(nameof(CSharpCodeDefinitionWindow))
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
public class Test
{
    $$int field;
}
", HangMitigatingCancellationToken);

            // The structure line should be the same, and we'll check for the presence/absence of the decompilation marker
            Assert.Contains("public struct Int32", await TestServices.CodeDefinitionWindow.GetCurrentLineTextAsync(HangMitigatingCancellationToken));
            Assert.Equal(enableDecompilation, (await TestServices.CodeDefinitionWindow.GetTextAsync(HangMitigatingCancellationToken)).Contains("Decompiled with ICSharpCode.Decompiler"));
        }
    }
}
