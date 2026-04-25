// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test.Cohost.Formatting;

public class CascadingTypeParameterFormattingTest(ITestOutputHelper testOutput) : DocumentFormattingTestBase(testOutput)
{
    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5648")]
    public async Task GenericComponentWithCascadingTypeParameter()
    {
        await RunFormattingTestAsync(
            input: """
                    @page "/counter"

                    @if(true)
                        {
                                    // indented
                            }

                    <TestGeneric Items="_items">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                            {
                                <div></div>
                            }
                        </TestGeneric>

                    @if(true)
                        {
                                    // indented
                                }

                    @code
                        {
                        private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                    }
                    """,
            htmlFormatted: """
                    @page "/counter"

                    @if(true)
                        {
                                    // indented
                            }

                    <TestGeneric Items="_items">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                        <div></div>
                        }
                    </TestGeneric>

                    @if(true)
                        {
                                    // indented
                                }

                    @code
                        {
                        private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                    }
                    """,
            expected: """
                    @page "/counter"

                    @if (true)
                    {
                        // indented
                    }

                    <TestGeneric Items="_items">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                            <div></div>
                        }
                    </TestGeneric>

                    @if (true)
                    {
                        // indented
                    }

                    @code
                    {
                        private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                    }
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5648")]
    public async Task GenericComponentWithCascadingTypeParameter_Nested()
    {
        await RunFormattingTestAsync(
            input: """
                    @page "/counter"

                    <TestGeneric Items="_items">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                            {
                                <div></div>
                            }
                    <TestGeneric Items="_items">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                            {
                                <div></div>
                            }
                        </TestGeneric>
                        </TestGeneric>

                    @code
                        {
                        private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                    }
                    """,
            htmlFormatted: """
                    @page "/counter"

                    <TestGeneric Items="_items">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                        <div></div>
                        }
                        <TestGeneric Items="_items">
                            @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                            {
                            <div></div>
                            }
                        </TestGeneric>
                    </TestGeneric>

                    @code
                        {
                        private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                    }
                    """,
            expected: """
                    @page "/counter"

                    <TestGeneric Items="_items">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                            <div></div>
                        }
                        <TestGeneric Items="_items">
                            @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                            {
                                <div></div>
                            }
                        </TestGeneric>
                    </TestGeneric>

                    @code
                    {
                        private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                    }
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5648")]
    public async Task GenericComponentWithCascadingTypeParameter_MultipleParameters()
    {
        await RunFormattingTestAsync(
            input: """
                    @page "/counter"

                    <TestGenericTwo Items="_items" ItemsTwo="_items2">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                            {
                                <div></div>
                            }
                        </TestGenericTwo>

                    @code
                        {
                        private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                        private IEnumerable<long> _items2 = new long[] { 1, 2, 3, 4, 5 };
                    }
                    """,
            htmlFormatted: """
                    @page "/counter"

                    <TestGenericTwo Items="_items" ItemsTwo="_items2">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                        <div></div>
                        }
                    </TestGenericTwo>

                    @code
                        {
                        private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                        private IEnumerable<long> _items2 = new long[] { 1, 2, 3, 4, 5 };
                    }
                    """,
            expected: """
                    @page "/counter"

                    <TestGenericTwo Items="_items" ItemsTwo="_items2">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                            <div></div>
                        }
                    </TestGenericTwo>

                    @code
                    {
                        private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                        private IEnumerable<long> _items2 = new long[] { 1, 2, 3, 4, 5 };
                    }
                    """);
    }

    private Task RunFormattingTestAsync(
        TestCode input,
        string htmlFormatted,
        string expected)
    {
        return base.RunFormattingTestAsync(
            input,
            htmlFormatted,
            expected,
            additionalFiles: [
                (FilePath("TestGeneric.razor"),  """
                    @using System.Collections.Generic
                    @using Microsoft.AspNetCore.Components
                    @typeparam TItem
                    @attribute [CascadingTypeParameter(nameof(TItem))]

                    <h3>TestGeneric</h3>

                    @code
                    {
                        [Parameter] public IEnumerable<TItem> Items { get; set; }
                        [Parameter] public RenderFragment ChildContent { get; set; }
                    }
                    """),
                (FilePath("TestGenericTwo.razor"),  """
                    @using System.Collections.Generic
                    @using Microsoft.AspNetCore.Components
                    @typeparam TItem
                    @typeparam TItemTwo
                    @attribute [CascadingTypeParameter(nameof(TItem))]
                    @attribute [CascadingTypeParameter(nameof(TItemTwo))]
        
                    <h3>TestGeneric</h3>
        
                    @code
                    {
                        [Parameter] public IEnumerable<TItem> Items { get; set; }
                        [Parameter] public IEnumerable<TItemTwo> ItemsTwo { get; set; }
                        [Parameter] public RenderFragment ChildContent { get; set; }
                    }
                    """)]);
    }
}
