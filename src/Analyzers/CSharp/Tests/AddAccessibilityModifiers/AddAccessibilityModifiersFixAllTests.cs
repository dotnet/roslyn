// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddAccessibilityModifiers;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)]
public class AddAccessibilityModifiersFixAllTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public AddAccessibilityModifiersFixAllTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpAddAccessibilityModifiersDiagnosticAnalyzer(), new CSharpAddAccessibilityModifiersCodeFixProvider());

    [Fact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6611")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInContainingType_DoesNotCrashInDuplicateProgramInTopLevelStatements()
    {
        var input = """
            Console.WriteLine("Hello, World!");
            class {|FixAllInContainingType:Program|}
            {
            }
            """;

        var expected = """
            Console.WriteLine("Hello, World!");

            internal class Program
            {
            }
            """;

        await TestAsync(input, expected, TestParameters.Default.parseOptions);
    }
}
