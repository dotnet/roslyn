// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public abstract class CSharpSquigglesCommon : AbstractEditorTest
{
    protected CSharpSquigglesCommon(string projectTemplate)
        : base(nameof(CSharpSquigglesCommon), projectTemplate)
    {
    }

    protected abstract bool SupportsGlobalUsings { get; }

    protected override string LanguageName => LanguageNames.CSharp;

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63042")]
    public async Task VerifySyntaxErrorSquiggles()
    {
        await TestServices.Editor.SetTextAsync("""
            using System;
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
                        Console.WriteLine("Hello World")
                    }

                    private static void Sub()
                    {
                }
            }
            """, HangMitigatingCancellationToken);

        var usingsErrorTags = SupportsGlobalUsings ? ("suggestion", TextSpan.FromBounds(0, 68), """
            using System;
            using System.Collections.Generic;
            using System.Text;
            """, "IDE0005: Using directive is unnecessary.")
            : ("suggestion", TextSpan.FromBounds(15, 68), """
            using System.Collections.Generic;
            using System.Text;
            """, "IDE0005: Using directive is unnecessary.");

        await TestServices.EditorVerifier.ErrorTagsAsync(
          [
              usingsErrorTags,
              ("syntax error", TextSpan.FromBounds(286, 287), "\r", "CS1002: ; expected"),
              ("syntax error", TextSpan.FromBounds(354, 355), "}", "CS1513: } expected"),
          ],
          HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/61367")]
    public async Task VerifySemanticErrorSquiggles()
    {
        await TestServices.Editor.SetTextAsync("""
            using System;

            class C  : Bar
            {
            }
            """, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.ErrorTagsAsync(
            [
                ("suggestion", TextSpan.FromBounds(0, 13), "using System;", "IDE0005: Using directive is unnecessary."),
                ("syntax error", TextSpan.FromBounds(28, 31), "Bar", "CS0246: The type or namespace name 'Bar' could not be found (are you missing a using directive or an assembly reference?)"),
            ],
            HangMitigatingCancellationToken);
    }
}
