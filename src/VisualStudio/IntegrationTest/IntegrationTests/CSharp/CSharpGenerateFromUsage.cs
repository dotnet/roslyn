// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGenerateFromUsage : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGenerateFromUsage(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpGenerateFromUsage))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateLocal)]
        public async Task GenerateLocalAsync()
        {
            SetUpEditor(
@"class Program
{
    static void Main(string[] args)
    {
        string s = $$xyz;
    }
}");
            await VisualStudio.Editor.Verify.CodeActionAsync("Generate local 'xyz'", applyFix: true);
            VisualStudio.Editor.Verify.TextContains(
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
