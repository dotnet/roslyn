// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp;

public class CSharpGenerateFromUsage : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpGenerateFromUsage()
        : base(nameof(CSharpGenerateFromUsage))
    {
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateLocal)]
    public async Task GenerateLocal()
    {
        await SetUpEditorAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    string s = $$xyz;
                }
            }
            """, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Generate local 'xyz'", applyFix: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    string xyz = null;
                    string s = xyz;
                }
            }
            """, cancellationToken: HangMitigatingCancellationToken);
    }
}
