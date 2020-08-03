// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateLocal)]
        public void GenerateLocal()
        {
            SetUpEditor(
@"class Program
{
    static void Main(string[] args)
    {
        string s = $$xyz;
    }
}");
            VisualStudio.Editor.Verify.CodeAction("Generate local 'xyz'", applyFix: true);
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
