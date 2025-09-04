// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddOrRemoveAccessibilityModifiers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddOrRemoveAccessibilityModifiers;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddOrRemoveAccessibilityModifiers)]
public sealed class AddOrRemoveAccessibilityModifiersFixAllTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpAddOrRemoveAccessibilityModifiersDiagnosticAnalyzer(), new CSharpAddOrRemoveAccessibilityModifiersCodeFixProvider());

    [Fact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6611")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInContainingType_DoesNotCrashInDuplicateProgramInTopLevelStatements()
        => TestAsync("""
            Console.WriteLine("Hello, World!");
            class {|FixAllInContainingType:Program|}
            {
            }
            """, """
            Console.WriteLine("Hello, World!");
            internal class Program
            {
            }
            """, new(TestParameters.Default.parseOptions));
}
