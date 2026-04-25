// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Similar to design time code generation tests, but goes a character at a time.
// Don't add many of these since they are slow - instead add features to existing
// tests here, and use these as smoke tests, not for detailed regression testing.
public class ComponentTypingTest : RazorIntegrationTestBase
{
    internal override bool DesignTime => true;

    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    [Fact]
    public void DoSomeTyping()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public int Value { get; set; }
        [Parameter] public Action<int> ValueChanged { get; set; }
        [Parameter] public string AnotherValue { get; set; }
    }

    public class ModelState
    {
        public Action<string> Bind(Func<string, string> func) => throw null;
    }
}
"));
        var text = @"
<div>
  <MyComponent bind-Value=""myValue"" AnotherValue=""hi""/>
  <input type=""text"" bind=""@this.ModelState.Bind(x => x)"" />
  <button ref=""_button"" onsubmit=""@FormSubmitted"">Click me</button>
</div>
<MyComponent
    IntProperty=""123""
    BoolProperty=""true""
    StringProperty=""My string""
    ObjectProperty=""new SomeType()""/>
@functions {
    Test.ModelState ModelState { get; set; }
}";

        for (var i = 0; i <= text.Length; i++)
        {
            CompileToCSharp(text.Substring(0, i));
        }
    }

    [Fact] // Regression test for #1068
    public void Regression_1068()
    {
        CompileToCSharp(@"
<input type=""text"" bind="" />
@functions {
    Test.ModelState ModelState { get; set; }
}
",
            // /dir/subdir/Test/TestComponent.cshtml(3,10): error CS0234: The type or namespace name 'ModelState' does not exist in the namespace 'Test' (are you missing an assembly reference?)
            //     Test.ModelState ModelState { get; set; }
            Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "ModelState").WithArguments("ModelState", "Test").WithLocation(3, 10));
    }

    [Fact]
    public void MalformedAttributeContent()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public int Value { get; set; }
        [Parameter] public Action<int> ValueChanged { get; set; }
        [Parameter] public string AnotherValue { get; set; }
    }

    public class ModelState
    {
        public Action<string> Bind(Func<string, string> func) => throw null;
    }
}
"));
        CompileToCSharp(@"
  <MyComponent Value=10 Something=@for

  <button disabled=@form.IsSubmitting type=""submit"" class=""btn btn-primary mt-3 mr-3 has-spinner @(form.IsSubmitting ? ""active"" :"""")"" onclick=@(async () => await SaveAsync(false))>
@functions {
    Test.ModelState ModelState { get; set; }
}");
    }
}
