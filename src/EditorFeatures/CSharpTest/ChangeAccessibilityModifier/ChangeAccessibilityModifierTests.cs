// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpChangeAccessibilityModifierCodeFixProvider());

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Theory]
        [InlineData("public", 0)]
        [InlineData("protected", 1)]
        [InlineData("internal", 2)]
        [InlineData("internal protected", 3)]
        [InlineData("private protected", 4)]
        public async Task TestProperty(string accessibility, int index)
        {
            var initial = @"
abstract class C
{
    abstract string [|Prop|] { get; }
}
";
            var expected = $@"
abstract class C
{{
    {accessibility} abstract string Prop {{ get; }}
}}
";
            await TestInRegularAndScriptAsync(initial, expected, index: index);
        }

        [Theory]
        [InlineData("public", 0)]
        [InlineData("protected", 1)]
        [InlineData("internal", 2)]
        [InlineData("internal protected", 3)]
        [InlineData("private protected", 4)]
        public async Task TestMethod(string accessibility, int index)
        {
            var initial = @"
abstract class C
{
    abstract string [|M|]();
}
";
            var expected = $@"
abstract class C
{{
    {accessibility} abstract string M();
}}
";
            await TestInRegularAndScriptAsync(initial, expected, index: index);
        }

        [Fact]
        public async Task TestEvent()
        {
            var initial = @"
using System;

abstract class C
{
    abstract event Action [|E|];
}
";
            var expected = @"
using System;

abstract class C
{
    public abstract event Action E;
}
";
            await TestInRegularAndScriptAsync(initial, expected);
        }

        [Fact]
        public async Task TestEventMultiple()
        {
            var initial = @"
using System;

abstract class C
{
    abstract event Action [|E|], E2;
}
";
            var expected = @"
using System;

abstract class C
{
    public abstract event Action E;
    abstract event Action E2;
}
";
            await TestInRegularAndScriptAsync(initial, expected);
        }

        [Fact]
        public async Task TestPublicOverride()
        {
            var initial = @"
abstract class C
{
    override string [|ToString|]();
}
";
            var expected = @"
abstract class C
{
    public override string ToString();
}
";
            await TestInRegularAndScriptAsync(initial, expected);
        }

        [Fact]
        public async Task TestProtectedOverride()
        {
            var initial = @"
abstract class B
{
    protected abstract string Prop { get; }
}
class D : B
{
    override string [|Prop|] { get; }
}
";
            var expected = @"
abstract class B
{
    protected abstract string Prop { get; }
}
class D : B
{
    protected override string Prop { get; }
}
";
            await TestInRegularAndScriptAsync(initial, expected);
        }

        [Fact]
        public async Task TestProtectedOverrideWithAccessibility()
        {
            var initial = @"
abstract class B
{
    protected abstract string Prop { get; }
}
class D : B
{
    public override string [|Prop|] { get; }
}
";
            var expected = @"
abstract class B
{
    protected abstract string Prop { get; }
}
class D : B
{
    protected override string Prop { get; }
}
";
            await TestInRegularAndScriptAsync(initial, expected);
        }

        [Theory]
        [InlineData("public", 0)]
        [InlineData("protected", 1)]
        [InlineData("internal", 2)]
        [InlineData("internal protected", 3)]
        [InlineData("private protected", 4)]
        public async Task TestFixAll(string visibility, int index)
        {
            var initial = @"
using System;

abstract class B
{
    abstract string {|FixAllInDocument:Prop|} { get; }

    abstract int this[int index] { get; }

    abstract event Action Event;

    override string ToString() => """"; // Not touched because of equivalence key

    abstract event Action<int> Event1, Event2; // multi-var event declaration is intentially not supported by FixAll
}
";
            var expected = $@"
using System;

abstract class B
{{
    {visibility} abstract string Prop {{ get; }}

    {visibility} abstract int this[int index] {{ get; }}

    {visibility} abstract event Action Event;

    override string ToString() => """"; // not touched because of equivalence key

    {visibility} abstract event Action<int> Event1;
    abstract event Action<int> Event2; // multi-var event declaration is intentially not supported by FixAll
}}
";
            await TestInRegularAndScriptAsync(initial, expected, index: index);
        }

        [Fact]
        public async Task TestFixAllOverride()
        {
            var initial = @"
abstract class B
{
    public abstract void Foo();

    protected abstract void Boo();

    virtual void Bar(); // not touched because of equivalence key
}

class D : B
{
    override void Foo() {}

    public override void {|FixAllInDocument:Boo|}() {}

    override void Bar(); // inappropriate override not touched
}
";
            var expected = @"
abstract class B
{
    public abstract void Foo();

    protected abstract void Boo();

    virtual void Bar(); // not touched because of equivalence key
}

class D : B
{
    public override void Foo() {}

    protected override void Boo() {}

    override void Bar(); // inappropriate override not touched
}
";
            await TestInRegularAndScriptAsync(initial, expected);
        }
    }
}
