// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Test.MessageInterception;

#pragma warning disable CS0618 // Type or member is obsolete

public class DefaultInterceptionManagerTest : ToolingTestBase
{
    public DefaultInterceptionManagerTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [Fact]
    public void Ctor_NullArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DefaultInterceptorManager(null!, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HasInterceptor_InvalidMessageName_Throws(string? input)
    {
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors());

        Assert.Throws<ArgumentException>(() => sut.HasInterceptor(input!, "testContentType"));
    }

    [Fact]
    public void HasInterceptor_HasNoInterceptors_ReturnsFalse()
    {
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors());

        Assert.False(sut.HasInterceptor("foo", "testContentType"));
    }

    [Fact]
    public void HasInterceptor_HasMatchingInterceptor_ReturnsTrue()
    {
        var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "expected", "testContentType")), GenerateLazyGenericInterceptors());

        Assert.True(sut.HasInterceptor("expected", "testContentType"));
    }

    [Fact]
    public void HasInterceptor_HasMatchingGenericInterceptor_ReturnsTrue()
    {
        var fakeInterceptor = Mock.Of<GenericMessageInterceptor>(MockBehavior.Strict);
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors((fakeInterceptor, "expected", "testContentType")));

        Assert.True(sut.HasInterceptor("expected", "testContentType"));
    }

    [Fact]
    public void HasInterceptor_DoesNotHaveMatchingInterceptor_ReturnsFalse()
    {
        var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
        var fakeGenericInterceptor = Mock.Of<GenericMessageInterceptor>(MockBehavior.Strict);
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "unexpected", "testContentType")), GenerateLazyGenericInterceptors((fakeGenericInterceptor, "unexpected", "testContentType")));

        Assert.False(sut.HasInterceptor("expected", "testContentType"));
    }

    [Fact]
    public void HasInterceptor_HasMismatchedContentType_ReturnsFalse()
    {
        var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
        var fakeGenericInterceptor = Mock.Of<GenericMessageInterceptor>(MockBehavior.Strict);
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "expected", "testContentType")), GenerateLazyGenericInterceptors((fakeGenericInterceptor, "expected", "testContentType")));

        Assert.False(sut.HasInterceptor("expected", "unknownContentType"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ProcessInterceptorsAsync_InvalidMethodName_Throws(string? input)
    {
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors());

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ProcessInterceptorsAsync(input!, JToken.Parse("{}"), "valid", DisposalToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ProcessGenericInterceptorsAsync_JToken_InvalidMethodName_Throws(string? input)
    {
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors());

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ProcessGenericInterceptorsAsync<JToken>(input!, JToken.Parse("{}"), "valid", DisposalToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ProcessGenericInterceptorsAsync_JsonElement_InvalidMethodName_Throws(string? input)
    {
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors());

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ProcessGenericInterceptorsAsync<JsonElement>(input!, JsonDocument.Parse("{}").RootElement, "valid", DisposalToken));
    }

    [Fact]
    public async Task ProcessInterceptorsAsync_InvalidMessage_Throws()
    {
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.ProcessInterceptorsAsync("valid", null!, "valid", DisposalToken));
    }

    [Fact]
    public async Task ProcessGenericInterceptorsAsync_JToken_InvalidMessage_Throws()
    {
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.ProcessGenericInterceptorsAsync<JToken>("valid", null!, "valid", DisposalToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ProcessInterceptorsAsync_InvalidSourceLanguageName_Throws(string? input)
    {
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors());

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ProcessInterceptorsAsync("valid", JToken.Parse("{}"), input!, DisposalToken));
    }

    [Fact]
    public async Task ProcessInterceptorsAsync_NoInterceptorMatches_NoChangesMadeToToken()
    {
        var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "unexpected", "testContentType")), GenerateLazyGenericInterceptors());
        var testToken = JToken.Parse("\"theToken\"");

        var result = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        Assert.Equal(testToken, result);
    }

    [Fact]
    public async Task ProcessGenericInterceptorsAsync_JsonElement_NoInterceptorMatches_NoChangesMadeToToken()
    {
        var fakeInterceptor = Mock.Of<GenericMessageInterceptor>(MockBehavior.Strict);
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors((fakeInterceptor, "unexpected", "testContentType")));
        var testToken = JsonDocument.Parse("\"theToken\"").RootElement;

        var result = await sut.ProcessGenericInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        Assert.Equal(testToken, result);
    }

    [Fact]
    public async Task ProcessInterceptorsAsync_InterceptorMatchesButDoesNotChangeDocumentUri_ChangesAppliedToToken()
    {
        var expected = JToken.Parse("\"new token\"");
        var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
        Mock.Get(fakeInterceptor)
            .Setup(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InterceptionResult(expected, false));
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "testMessage", "testContentType")), GenerateLazyGenericInterceptors());
        var testToken = JToken.Parse("\"theToken\"");

        var result = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ProcessGenericInterceptorsAsync_JsonElement_InterceptorMatchesButDoesNotChangeDocumentUri_ChangesAppliedToToken()
    {
        var expected = JsonDocument.Parse("\"new token\"").RootElement;
        var fakeInterceptor = Mock.Of<GenericMessageInterceptor>(MockBehavior.Strict);
        Mock.Get(fakeInterceptor)
            .Setup(x => x.ApplyChangesAsync<JsonElement>(It.IsAny<JsonElement>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenericInterceptionResult<JsonElement>(expected, false));
        var sut = new DefaultInterceptorManager(GenerateLazyInterceptors(), GenerateLazyGenericInterceptors((fakeInterceptor, "testMessage", "testContentType")));
        var testToken = JsonDocument.Parse("\"theToken\"").RootElement;

        var result = await sut.ProcessGenericInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ProcessInterceptorsAsync_InterceptorMatches_ChangedTokenPassedToSecondInterceptor()
    {
        var expected = JToken.Parse("\"new token\"");
        var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
        Mock.Get(fakeInterceptor)
            .Setup(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InterceptionResult(expected, false));
        var mockSecondInterceptor = new Mock<MessageInterceptor>(MockBehavior.Strict);
        mockSecondInterceptor.Setup(x => x.ApplyChangesAsync(new JArray(), "testContentType", DisposalToken))
            .ReturnsAsync(new InterceptionResult(expected, false));
        var sut = new DefaultInterceptorManager(
            GenerateLazyInterceptors(
                (fakeInterceptor, "testMessage", "testContentType"),
                (mockSecondInterceptor.Object, "testMessage", "testContentType")),
            GenerateLazyGenericInterceptors());
        var testToken = JToken.Parse("\"theToken\"");

        _ = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        mockSecondInterceptor.Verify(
            x => x.ApplyChangesAsync(It.Is<JToken>(t => t.Equals(expected)), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        mockSecondInterceptor.Verify(
            x => x.ApplyChangesAsync(It.Is<JToken>(t => t.Equals(testToken)), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessGenericInterceptorsAsync_JsonElement_InterceptorMatches_ChangedTokenPassedToSecondInterceptor()
    {
        var expected = JsonDocument.Parse("\"new token\"").RootElement;
        var fakeInterceptor = Mock.Of<GenericMessageInterceptor>(MockBehavior.Strict);
        Mock.Get(fakeInterceptor)
            .Setup(x => x.ApplyChangesAsync(It.IsAny<JsonElement>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenericInterceptionResult<JsonElement>(expected, false));
        var mockSecondInterceptor = new Mock<GenericMessageInterceptor>(MockBehavior.Strict);
        mockSecondInterceptor.Setup(x => x.ApplyChangesAsync(It.IsAny<JsonElement>(), "testContentType", DisposalToken))
            .ReturnsAsync(new GenericInterceptionResult<JsonElement>(expected, false));
        var sut = new DefaultInterceptorManager(
            GenerateLazyInterceptors(),
            GenerateLazyGenericInterceptors(
                (fakeInterceptor, "testMessage", "testContentType"),
                (mockSecondInterceptor.Object, "testMessage", "testContentType")));
        var testToken = JsonDocument.Parse("\"theToken\"").RootElement;

        _ = await sut.ProcessGenericInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        mockSecondInterceptor.Verify(
            x => x.ApplyChangesAsync(It.Is<JsonElement>(t => t.Equals(expected)), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        mockSecondInterceptor.Verify(
            x => x.ApplyChangesAsync(It.Is<JsonElement>(t => t.Equals(testToken)), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessInterceptorsAsync_InterceptorChangesDocumentUri_CausesAdditionalPass()
    {
        var expected = JToken.Parse("\"new token\"");
        var mockInterceptor = new Mock<MessageInterceptor>(MockBehavior.Strict);
        mockInterceptor
            .SetupSequence(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InterceptionResult(expected, true))
            .ReturnsAsync(new InterceptionResult(expected, false));
        var sut = new DefaultInterceptorManager(
            GenerateLazyInterceptors(
                (mockInterceptor.Object, "testMessage", "testContentType")),
            GenerateLazyGenericInterceptors());
        var testToken = JToken.Parse("\"theToken\"");

        _ = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        mockInterceptor.Verify(
            x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessGenericInterceptorsAsync_InterceptorChangesDocumentUri_CausesAdditionalPass()
    {
        var expected = JsonDocument.Parse("\"new token\"").RootElement;
        var mockInterceptor = new Mock<GenericMessageInterceptor>(MockBehavior.Strict);
        mockInterceptor
            .SetupSequence(x => x.ApplyChangesAsync(It.IsAny<JsonElement>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenericInterceptionResult<JsonElement>(expected, true))
            .ReturnsAsync(new GenericInterceptionResult<JsonElement>(expected, false));
        var sut = new DefaultInterceptorManager(
            GenerateLazyInterceptors(),
            GenerateLazyGenericInterceptors(
                (mockInterceptor.Object, "testMessage", "testContentType")));
        var testToken = JsonDocument.Parse("\"theToken\"").RootElement;

        _ = await sut.ProcessGenericInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        mockInterceptor.Verify(
            x => x.ApplyChangesAsync(It.IsAny<JsonElement>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessInterceptorsAsync_InterceptorReturnsNull_DoesNotCallAdditionalInterceptors()
    {
        var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
        Mock.Get(fakeInterceptor)
            .Setup(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InterceptionResult(null, false));
        var mockSecondInterceptor = new Mock<MessageInterceptor>(MockBehavior.Strict);
        var sut = new DefaultInterceptorManager(
            GenerateLazyInterceptors(
                (fakeInterceptor, "testMessage", "testContentType"),
                (mockSecondInterceptor.Object, "testMessage", "testContentType")),
            GenerateLazyGenericInterceptors());
        var testToken = JToken.Parse("\"theToken\"");

        _ = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        mockSecondInterceptor.Verify(
            x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessGenericInterceptorsAsync_InterceptorReturnsDefault_DoesNotCallAdditionalInterceptors()
    {
        var fakeInterceptor = Mock.Of<GenericMessageInterceptor>(MockBehavior.Strict);
        Mock.Get(fakeInterceptor)
            .Setup(x => x.ApplyChangesAsync(It.IsAny<JsonElement>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenericInterceptionResult<JsonElement>(default, false));
        var mockSecondInterceptor = new Mock<GenericMessageInterceptor>(MockBehavior.Strict);
        var sut = new DefaultInterceptorManager(
            GenerateLazyInterceptors(),
            GenerateLazyGenericInterceptors(
                (fakeInterceptor, "testMessage", "testContentType"),
                (mockSecondInterceptor.Object, "testMessage", "testContentType")));
        var testToken = JsonDocument.Parse("\"theToken\"").RootElement;

        _ = await sut.ProcessGenericInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        mockSecondInterceptor.Verify(
            x => x.ApplyChangesAsync(It.IsAny<JsonElement>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessInterceptorsAsync_InterceptorReturnsNull_ReturnsNull()
    {
        var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
        Mock.Get(fakeInterceptor)
            .Setup(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InterceptionResult(null, false));
        var sut = new DefaultInterceptorManager(
            GenerateLazyInterceptors(
                (fakeInterceptor, "testMessage", "testContentType")),
            GenerateLazyGenericInterceptors());
        var testToken = JToken.Parse("\"theToken\"");

        var result = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task ProcessGenericInterceptorsAsync_InterceptorReturnsDefault_ReturnsDefault()
    {
        var fakeInterceptor = Mock.Of<GenericMessageInterceptor>(MockBehavior.Strict);
        Mock.Get(fakeInterceptor)
            .Setup(x => x.ApplyChangesAsync(It.IsAny<JsonElement>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenericInterceptionResult<JsonElement>(default, false));
        var sut = new DefaultInterceptorManager(
            GenerateLazyInterceptors(),
            GenerateLazyGenericInterceptors(
                (fakeInterceptor, "testMessage", "testContentType")));
        var testToken = JsonDocument.Parse("\"theToken\"").RootElement;

        var result = await sut.ProcessGenericInterceptorsAsync("testMessage", testToken, "testContentType", DisposalToken);

        Assert.Equal(JsonValueKind.Undefined, result.ValueKind);
        Assert.True(result.Equals(default(JsonElement)));
    }

    private static IEnumerable<Lazy<MessageInterceptor, IInterceptMethodMetadata>> GenerateLazyInterceptors(params (MessageInterceptor, string, string)[] fakeInterceptors)
    {
        var result = new List<Lazy<MessageInterceptor, IInterceptMethodMetadata>>();

        foreach ((var i, var metadataString, var contentTypeName) in fakeInterceptors)
        {
            var metadata = Mock.Of<IInterceptMethodMetadata>(m =>
                m.InterceptMethods == new string[] { metadataString } &&
                m.ContentTypes == new string[] { contentTypeName },
                MockBehavior.Strict);
            result.Add(new Lazy<MessageInterceptor, IInterceptMethodMetadata>(() => i, metadata));
        }

        return result;
    }

    private static IEnumerable<Lazy<GenericMessageInterceptor, IInterceptMethodMetadata>> GenerateLazyGenericInterceptors(params (GenericMessageInterceptor, string, string)[] fakeInterceptors)
    {
        var result = new List<Lazy<GenericMessageInterceptor, IInterceptMethodMetadata>>();

        foreach ((var i, var metadataString, var contentTypeName) in fakeInterceptors)
        {
            var metadata = Mock.Of<IInterceptMethodMetadata>(m =>
                m.InterceptMethods == new string[] { metadataString } &&
                m.ContentTypes == new string[] { contentTypeName },
                MockBehavior.Strict);
            result.Add(new Lazy<GenericMessageInterceptor, IInterceptMethodMetadata>(() => i, metadata));
        }

        return result;
    }
}
