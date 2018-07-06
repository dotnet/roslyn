// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    public abstract class CSharpSquigglesCommon : AbstractIdeEditorTest
    {
        public CSharpSquigglesCommon(string projectTemplate)
            : base(nameof(CSharpSquigglesCommon), projectTemplate)
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        public virtual async Task VerifySyntaxErrorSquigglesAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"using System;
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

        private void Sub()
        {
    }
}");
            await VisualStudio.Editor.Verify.ErrorTagsAsync(
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'using System.Collections.Generic;\r\nusing System.Text;'[15-68]",
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\r'[286-287]",
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'}'[347-348]");
        }

        public virtual async Task VerifySemanticErrorSquigglesAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"using System;

class C  : Bar
{
}");
            await VisualStudio.Editor.Verify.ErrorTagsAsync(
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'using System;'[0-13]",
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[28-31]");
        }
    }
}
