// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    public abstract class CSharpSquigglesCommon : AbstractEditorTest
    {
        public CSharpSquigglesCommon(VisualStudioInstanceFactory instanceFactory, string projectTemplate)
            : base(instanceFactory, nameof(CSharpSquigglesCommon), projectTemplate)
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        public virtual void VerifySyntaxErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"using System;
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

        private void sub()
        {
    }
}");
            VisualStudio.Editor.Verify.ErrorTags(
              "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\r'[286-287]",
              "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'}'[347-348]",
              "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'using System.Collections.Generic;\r\nusing System.Text;'[15-68]");
        }

        public virtual void VerifySemanticErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"using System;

class C  : Bar
{
}");
            VisualStudio.Editor.Verify.ErrorTags(
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[28-31]",
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'using System;'[0-13]");
        }
    }
}
