// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public abstract class CSharpSquigglesCommon : AbstractEditorTest
    {
        protected CSharpSquigglesCommon(string projectTemplate)
            : base(nameof(CSharpSquigglesCommon), projectTemplate)
        {
        }

        protected abstract bool SupportsGlobalUsings { get; }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact]
        public virtual async Task VerifySyntaxErrorSquiggles()
        {
            await TestServices.Editor.SetTextAsync(@"using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication1
{
    /// <summary/>
    public class Program
    {
        /// <summary/>
        public static void Main(string[] args)
        {
            Console.WriteLine(""Hello World"")
        }

        private static void Sub()
        {
    }
}", HangMitigatingCancellationToken);

            var usingsErrorTags = SupportsGlobalUsings ? "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'using System;\\r\\nusing System.Collections.Generic;\\r\\nusing System.Text;'[0-68]"
                : "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'using System.Collections.Generic;\\r\\nusing System.Text;'[15-68]";

            await TestServices.EditorVerifier.ErrorTagsAsync(
              new[]
              {
                  usingsErrorTags,
                  "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\\r'[286-287]",
                  "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'}'[354-355]",
              },
              HangMitigatingCancellationToken);
        }

        [IdeFact]
        public virtual async Task VerifySemanticErrorSquiggles()
        {
            await TestServices.Editor.SetTextAsync(@"using System;

class C  : Bar
{
}", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.ErrorTagsAsync(
                new[]
                {
                    "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'using System;'[0-13]",
                    "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[28-31]",
                },
                HangMitigatingCancellationToken);
        }
    }
}
