// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ChangeAccessibilityModifier;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeAccessibilityModifier
{
    public class ChangeAccessibilityModifierTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public ChangeAccessibilityModifierTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpChangeAccessibilityModifierCodeFixProvider());

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact]
        public async Task TestProperty()
        {
            const string initial = @"
abstract class C
{
    abstract string [|Prop|] { get; }
}
";
            const string expected = @"
abstract class C
{
    public abstract string Prop { get; }
}
";
            await TestInRegularAndScriptAsync(initial, expected);
        }
    }
}
