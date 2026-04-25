// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.WebTools.Languages.Shared.VS.Test.LanguageServer.MiddleLayerProviders;

public class GenericInterceptionMiddleLayer_SystemTextJsonTest : ToolingTestBase
{
    public GenericInterceptionMiddleLayer_SystemTextJsonTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [Fact]
    public void Ctor_NullInterceptorManager_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GenericInterceptionMiddleLayer<JsonElement>(null!, "test"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Ctor_EmptyLanguageName_Throws(string? languageName)
    {
        var fakeInterceptorManager = Mock.Of<InterceptorManager>(MockBehavior.Strict);
        Assert.Throws<ArgumentException>(() => new GenericInterceptionMiddleLayer<JsonElement>(fakeInterceptorManager, languageName!));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanHandle_DelegatesToInterceptionManager(bool value)
    {
        var fakeInterceptorManager = Mock.Of<InterceptorManager>(MockBehavior.Strict);
        Mock.Get(fakeInterceptorManager).Setup(x => x.HasInterceptor("testMessage", "testLanguage"))
                                        .Returns(value);
        var sut = new GenericInterceptionMiddleLayer<JsonElement>(fakeInterceptorManager, "testLanguage");

        var result = sut.CanHandle("testMessage");

        Assert.Equal(value, result);
    }

    [Fact]
    public async Task HandleNotificationAsync_IfInterceptorReturnsDefault_DoesNotSendNotification()
    {
        var fakeInterceptorManager = Mock.Of<InterceptorManager>(MockBehavior.Strict);
        Mock.Get(fakeInterceptorManager)
            .Setup(x => x.HasInterceptor("testMethod", "testLanguage"))
            .Returns(true);
        Mock.Get(fakeInterceptorManager)
            .Setup(x => x.ProcessGenericInterceptorsAsync<JsonElement>(
                "testMethod",
                It.IsAny<JsonElement>(),
                "testLanguage",
                CancellationToken.None))
            .ReturnsAsync(value: default);
        var token = JsonDocument.Parse("{}").RootElement;
        var sut = new GenericInterceptionMiddleLayer<JsonElement>(fakeInterceptorManager, "testLanguage");
        var sentNotification = false;

        await sut.HandleNotificationAsync("testMethod", token, (_) =>
        {
            sentNotification = true;
            return Task.CompletedTask;
        });

        Assert.False(sentNotification);
    }

    [Fact]
    public async Task HandleNotificationAsync_IfInterceptorReturnsToken_SendsNotificationWithToken()
    {
        var token = JsonDocument.Parse("{}").RootElement;
        var expected = JsonDocument.Parse("\"expected\"").RootElement;
        JsonElement? actual = null;
        var fakeInterceptorManager = Mock.Of<InterceptorManager>(MockBehavior.Strict);
        Mock.Get(fakeInterceptorManager)
            .Setup(x => x.HasInterceptor("testMethod", "testLanguage"))
            .Returns(true);
        Mock.Get(fakeInterceptorManager)
            .Setup(x => x.ProcessGenericInterceptorsAsync<JsonElement>(
                "testMethod",
                It.IsAny<JsonElement>(),
                "testLanguage",
                CancellationToken.None))
            .ReturnsAsync(expected);
        var sut = new GenericInterceptionMiddleLayer<JsonElement>(fakeInterceptorManager, "testLanguage");

        await sut.HandleNotificationAsync("testMethod", token, (t) =>
        {
            actual = t;
            return Task.CompletedTask;
        });

        Assert.Equal(expected, actual);
    }
}
