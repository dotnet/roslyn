// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGenerateFromUsage : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGenerateFromUsage()
            : base(nameof(CSharpGenerateFromUsage))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateLocal)]
        public async Task GenerateLocalAsync()
        {
            await SetUpEditorAsync(
@"class Program
{
    static void Main(string[] args)
    {
        string s = $$xyz;
    }
}");
            await VisualStudio.Editor.Verify.CodeActionAsync("Generate local 'xyz'", applyFix: true);
            await VisualStudio.Editor.Verify.TextContainsAsync(
@"class Program
{
    static void Main(string[] args)
    {
        string xyz = null;
        string s = xyz;
    }
}");
        }
    }
}
