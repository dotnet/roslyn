// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CallHierarchy;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostCallHierarchyEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task Prepare_ExplicitStatement()
        => VerifyCallHierarchyAsync("""
            @{
                Get$$Value();
            }

            @code
            {
                int {|target:GetValue|}() => 1;
            }
            """);

    [Fact]
    public Task IncomingCalls_CodeBlock()
        => VerifyCallHierarchyAsync("""
            @code
            {
                void {|target:M$$|}()
                {
                }

                void {|incoming_caller:Caller|}()
                {
                    {|incoming_caller_from:M|}();
                }
            }
            """);

    [Fact]
    public Task OutgoingCalls_CodeBlock()
        => VerifyCallHierarchyAsync("""
            @code
            {
                void {|target:Caller$$|}()
                {
                    {|outgoing_target_from:Target()|};
                }

                void {|outgoing_target:Target|}()
                {
                }
            }
            """);

    [Fact]
    public Task Prepare_ImplicitExpression()
        => VerifyCallHierarchyAsync("""
            <div>@Get$$Value()</div>

            @code
            {
                int {|target:GetValue|}() => 1;
            }
            """);

    [Fact]
    public Task Prepare_ComponentAttribute()
        => VerifyCallHierarchyAsync(
            """
            <InputText Value="@Get$$Value()" />

            @code
            {
                int {|target:GetValue|}() => 1;
            }
            """);

    [Fact]
    public Task IncomingCalls_ImplicitExpression()
        => VerifyCallHierarchyAsync("""
            <div>@{|incoming_markup_from:M|}()</div>

            @code
            {
                void {|target:M$$|}()
                {
                }
            }
            """);

    [Fact]
    public Task IncomingCalls_ExplicitStatement()
        => VerifyCallHierarchyAsync("""
            @{
                {|incoming_markup_from:M|}();
            }

            @code
            {
                void {|target:M$$|}()
                {
                }
            }
            """);

    [Fact]
    public Task IncomingCalls_ComponentAttribute()
        => VerifyCallHierarchyAsync(
            """
            <InputText Value="@{|incoming_markup_from:M|}()" />

            @code
            {
                void {|target:M$$|}()
                {
                }
            }
            """);

    [Fact]
    public Task OutgoingCalls_ImplicitExpression()
        => VerifyCallHierarchyAsync("""
            @code
            {
                void {|target:Caller$$|}()
                {
                    RenderFragment fragment = @<div>@{|outgoing_target_from:Target()|}</div>;
                }

                int {|outgoing_target:Target|}() => 1;
            }
            """);

    [Fact]
    public Task OutgoingCalls_ExplicitStatement()
        => VerifyCallHierarchyAsync("""
            @code
            {
                void {|target:Caller$$|}()
                {
                    RenderFragment fragment = @<div>@{ {|outgoing_target_from:Target()|}; }</div>;
                }

                void {|outgoing_target:Target|}()
                {
                }
            }
            """);

    [Fact]
    public Task OutgoingCalls_ComponentAttribute()
        => VerifyCallHierarchyAsync(
            """
            @code
            {
                void {|target:Caller$$|}()
                {
                    RenderFragment fragment = @<InputText Value="@{|outgoing_target_from:Target()|}" />;
                }

                int {|outgoing_target:Target|}() => 1;
            }
            """);

    [Fact]
    public Task IncomingAndOutgoingCalls()
        => VerifyCallHierarchyAsync(
            """
            <div>@{|incoming_markup_from:Middle|}()</div>

            @{
                {|incoming_markup_from:Middle|}();
            }

            <InputText Value="@{|incoming_markup_from:Middle|}()" />

            @code
            {
                int {|target:Middle$$|}()
                {
                    return {|outgoing_first_from:First()|} + {|outgoing_second_from:Second()|};
                }

                int {|outgoing_first:First|}() => 1;

                int {|outgoing_second:Second|}() => 2;

                void {|incoming_code:CodeCaller|}()
                {
                    {|incoming_code_from:Middle|}();
                }
            }
            """);

    private async Task VerifyCallHierarchyAsync(TestCode input, (string fileName, string contents)[]? additionalFiles = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, additionalFiles: additionalFiles);
        var sourceText = await document.GetTextAsync(DisposalToken);

        var endpoint = new CohostPrepareCallHierarchyEndpoint(IncompatibleProjectService, RemoteServiceInvoker);
        var request = new CallHierarchyPrepareParams
        {
            Position = sourceText.GetPosition(input.Position),
            TextDocument = new TextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() },
        };

        var preparedItems = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);
        Assert.NotNull(preparedItems);
        var preparedItem = Assert.Single(preparedItems);

        // Simulate round trip with server
        preparedItem = RazorCallHierarchyResolveData.WithData(preparedItem, JsonSerializer.SerializeToElement(preparedItem.Data, JsonHelpers.JsonSerializerOptions));

        var targetSpan = Assert.Single(input.GetNamedSpans("target"));
        VerifyRazorItem(document, sourceText, preparedItem, targetSpan);
        await VerifyIncomingCallsAsync(document, sourceText, input, preparedItem);
        await VerifyOutgoingCallsAsync(document, sourceText, input, preparedItem);
    }

    private static void VerifyRazorItem(TextDocument document, SourceText sourceText, CallHierarchyItem item, TextSpan expectedSelectionSpan)
    {
        Assert.Equal(document.CreateUri(), item.Uri.GetRequiredParsedUri());
        Assert.Equal(sourceText.GetRange(expectedSelectionSpan), item.SelectionRange);

        var data = RazorCallHierarchyResolveData.Unwrap(item);
        Assert.NotNull(data);
        Assert.Equal(document.CreateUri(), data.TextDocument.DocumentUri.GetRequiredParsedUri());
        Assert.NotNull(data.OriginalData);
    }

    private static void VerifyRanges(SourceText sourceText, ImmutableArray<TextSpan> expectedSpans, LspRange[] actualRanges)
    {
        Assert.Equal(expectedSpans.Length, actualRanges.Length);

        for (var i = 0; i < actualRanges.Length; i++)
        {
            Assert.Equal(expectedSpans[i], sourceText.GetTextSpan(actualRanges[i]));
        }
    }

    private async Task VerifyIncomingCallsAsync(TextDocument document, SourceText sourceText, TestCode input, CallHierarchyItem preparedItem)
    {
        var expectedIncomingNames = GetExpectedCallNames(input, "incoming");
        if (expectedIncomingNames.IsDefaultOrEmpty)
        {
            return;
        }

        var endpoint = new CohostCallHierarchyIncomingCallsEndpoint(IncompatibleProjectService, RemoteServiceInvoker);
        var request = new CallHierarchyIncomingCallsParams
        {
            Item = preparedItem,
        };

        var incomingCalls = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);
        Assert.NotNull(incomingCalls);
        Assert.True(
            expectedIncomingNames.Length == incomingCalls.Length,
            $"Expected {expectedIncomingNames.Length} incoming calls, got {incomingCalls.Length}: {string.Join(", ", Array.ConvertAll(incomingCalls, static call => call.From.Name))}");

        foreach (var expectedName in expectedIncomingNames)
        {
            var expectedSelectionSpan = Assert.Single(input.GetNamedSpans(expectedName));
            var incomingCall = Assert.Single(incomingCalls, call => sourceText.GetTextSpan(call.From.SelectionRange) == expectedSelectionSpan);

            VerifyRazorItem(document, sourceText, incomingCall.From, expectedSelectionSpan);
            VerifyRanges(sourceText, input.GetNamedSpans(expectedName + "_from"), incomingCall.FromRanges);
        }
    }

    private async Task VerifyOutgoingCallsAsync(TextDocument document, SourceText sourceText, TestCode input, CallHierarchyItem preparedItem)
    {
        var expectedOutgoingNames = GetExpectedCallNames(input, "outgoing");
        if (expectedOutgoingNames.IsDefaultOrEmpty)
        {
            return;
        }

        var endpoint = new CohostCallHierarchyOutgoingCallsEndpoint(IncompatibleProjectService, RemoteServiceInvoker);
        var request = new CallHierarchyOutgoingCallsParams
        {
            Item = preparedItem,
        };

        var outgoingCalls = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);
        Assert.NotNull(outgoingCalls);
        Assert.True(
            expectedOutgoingNames.Length == outgoingCalls.Length,
            $"Expected {expectedOutgoingNames.Length} outgoing calls, got {outgoingCalls.Length}: {string.Join(", ", Array.ConvertAll(outgoingCalls, static call => call.To.Name))}");

        foreach (var expectedName in expectedOutgoingNames)
        {
            var expectedSelectionSpan = Assert.Single(input.GetNamedSpans(expectedName));
            var outgoingCall = Assert.Single(outgoingCalls, call => sourceText.GetTextSpan(call.To.SelectionRange) == expectedSelectionSpan);

            VerifyRazorItem(document, sourceText, outgoingCall.To, expectedSelectionSpan);
            VerifyRanges(sourceText, input.GetNamedSpans(expectedName + "_from"), outgoingCall.FromRanges);
        }
    }

    private static ImmutableArray<string> GetExpectedCallNames(TestCode input, string prefix)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        var prefixWithSeparator = prefix + "_";

        foreach (var name in input.NamedSpans.Keys)
        {
            if (name.StartsWith(prefixWithSeparator, StringComparison.Ordinal) &&
                !name.EndsWith("_from", StringComparison.Ordinal))
            {
                builder.Add(name);
            }
        }

        return builder.ToImmutable();
    }
}
