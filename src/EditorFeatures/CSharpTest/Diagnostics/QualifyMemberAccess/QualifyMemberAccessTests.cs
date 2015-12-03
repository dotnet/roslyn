// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.QualifyMemberAccess;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.QualifyMemberAccess;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.QualifyMemberAccess
{
    public class QualifyMemberAccessTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(new CSharpQualifyMemberAccessDiagnosticAnalyzer(), new CSharpQualifyMemberAccessCodeFixProvider());
        }

        private Task TestAsyncWithOption(string code, string expected, PerLanguageOption<bool> option)
        {
            return TestAsync(code, expected, options: Option(option, true));
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_LHS()
        {
            await TestAsyncWithOption(
@"class Class { int i; void M() { [|i|] = 1; } }",
@"class Class { int i; void M() { this.i = 1; } }",
SimplificationOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_RHS()
        {
            await TestAsyncWithOption(
@"class Class { int i; void M() { var x = [|i|]; } }",
@"class Class { int i; void M() { var x = this.i; } }",
SimplificationOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_MethodArgument()
        {
            await TestAsyncWithOption(
@"class Class { int i; void M(int ii) { M([|i|]); } }",
@"class Class { int i; void M(int ii) { M(this.i); } }",
SimplificationOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_ChainedAccess()
        {
            await TestAsyncWithOption(
@"class Class { int i; void M() { var s = [|i|].ToString(); } }",
@"class Class { int i; void M() { var s = this.i.ToString(); } }",
SimplificationOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyFieldAccess_ConditionalAccess()
        {
            await TestAsyncWithOption(
@"class Class { string s; void M() { var x = [|s|]?.ToString(); } }",
@"class Class { string s; void M() { var x = this.s?.ToString(); } }",
SimplificationOptions.QualifyFieldAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_LHS()
        {
            await TestAsyncWithOption(
@"class Class { int i { get; set; } void M() { [|i|] = 1; } }",
@"class Class { int i { get; set; } void M() { this.i = 1; } }",
SimplificationOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_RHS()
        {
            await TestAsyncWithOption(
@"class Class { int i { get; set; } void M() { var x = [|i|]; } }",
@"class Class { int i { get; set; } void M() { var x = this.i; } }",
SimplificationOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_MethodArgument()
        {
            await TestAsyncWithOption(
@"class Class { int i { get; set; } void M(int ii) { M([|i|]); } }",
@"class Class { int i { get; set; } void M(int ii) { M(this.i); } }",
SimplificationOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_ChainedAccess()
        {
            await TestAsyncWithOption(
@"class Class { int i { get; set; } void M() { var s = [|i|].ToString(); } }",
@"class Class { int i { get; set; } void M() { var s = this.i.ToString(); } }",
SimplificationOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyPropertyAccess_ConditionalAccess()
        {
            await TestAsyncWithOption(
@"class Class { string s { get; set; } void M() { var x = [|s|]?.ToString(); } }",
@"class Class { string s { get; set; } void M() { var x = this.s?.ToString(); } }",
SimplificationOptions.QualifyPropertyAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_VoidCallWithArguments()
        {
            await TestAsyncWithOption(
@"class Class { void M(int i) { [|M|](0); } }",
@"class Class { void M(int i) { this.M(0); } }",
SimplificationOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_AsReturn()
        {
            await TestAsyncWithOption(
@"class Class { int M() { return [|M|](); }",
@"class Class { int M() { return this.M(); }",
SimplificationOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_ChainedAccess()
        {
            await TestAsyncWithOption(
@"class Class { string M() { var s = [|M|]().ToString(); }",
@"class Class { string M() { var s = this.M().ToString(); }",
SimplificationOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_ConditionalAccess()
        {
            await TestAsyncWithOption(
@"class Class { string M() { return [|M|]()?.ToString(); }",
@"class Class { string M() { return this.M()?.ToString(); }",
SimplificationOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_EventSubscription1()
        {
            await TestAsyncWithOption(
@"using System; class C { event EventHandler e; void Handler(object sender, EventArgs args) { e += [|Handler|]; } }",
@"using System; class C { event EventHandler e; void Handler(object sender, EventArgs args) { e += this.Handler; } }",
SimplificationOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyMethodAccess_EventSubscription2()
        {
            await TestAsyncWithOption(
@"using System; class C { event EventHandler e; void Handler(object sender, EventArgs args) { e += new EventHandler([|Handler|]); } }",
@"using System; class C { event EventHandler e; void Handler(object sender, EventArgs args) { e += new EventHandler(this.Handler); } }",
SimplificationOptions.QualifyMethodAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_EventSubscription()
        {
            await TestAsyncWithOption(
@"using System; class C { event EventHandler e; void Handler(object sender, EventArgs args) { [|e|] += Handler; } }",
@"using System; class C { event EventHandler e; void Handler(object sender, EventArgs args) { this.e += Handler; } }",
SimplificationOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_InvokeEvent1()
        {
            await TestAsyncWithOption(
@"using System; class C { event EventHandler e; void OnSomeEvent() { [|e|](this, new EventArgs()); } }",
@"using System; class C { event EventHandler e; void OnSomeEvent() { this.e(this, new EventArgs()); } }",
SimplificationOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_InvokeEvent2()
        {
            await TestAsyncWithOption(
@"using System; class C { event EventHandler e; void OnSomeEvent() { [|e|].Invoke(this, new EventArgs()); } }",
@"using System; class C { event EventHandler e; void OnSomeEvent() { this.e.Invoke(this, new EventArgs()); } }",
SimplificationOptions.QualifyEventAccess);
        }

        [WorkItem(7065, "https://github.com/dotnet/roslyn/issues/7065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        public async Task QualifyEventAccess_InvokeEvent3()
        {
            await TestAsyncWithOption(
@"using System; class C { event EventHandler e; void OnSomeEvent() { [|e|]?.Invoke(this, new EventArgs()); } }",
@"using System; class C { event EventHandler e; void OnSomeEvent() { this.e?.Invoke(this, new EventArgs()); } }",
SimplificationOptions.QualifyEventAccess);
        }
    }
}
