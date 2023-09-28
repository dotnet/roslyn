// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public class HandlerProviderTests
{
    [Theory]
    [CombinatorialData]
    public void GetMethodHandler(bool supportsGetRegisteredServices)
    {
        var handlerProvider = GetHandlerProvider(supportsGetRegisteredServices);

        var methodHandlers = handlerProvider.GetMethodHandlers(TestMethodHandler.Name, TestMethodHandler.RequestType, TestMethodHandler.ResponseType);
        Assert.Same(TestMethodHandler.Instance, methodHandlers.Single().Value);
    }

    [Theory]
    [CombinatorialData]
    public void GetMethodHandler_Parameterless(bool supportsGetRegisteredServices)
    {
        var handlerProvider = GetHandlerProvider(supportsGetRegisteredServices);

        var methodHandlers = handlerProvider.GetMethodHandlers(TestParameterlessMethodHandler.Name, requestType: null, TestParameterlessMethodHandler.ResponseType);
        Assert.Same(TestParameterlessMethodHandler.Instance, methodHandlers.Single().Value);
    }

    [Theory]
    [CombinatorialData]
    public void GetMethodHandler_Notification(bool supportsGetRegisteredServices)
    {
        var handlerProvider = GetHandlerProvider(supportsGetRegisteredServices);

        var methodHandlers = handlerProvider.GetMethodHandlers(TestNotificationHandler.Name, TestNotificationHandler.RequestType, responseType: null);
        Assert.Same(TestNotificationHandler.Instance, methodHandlers.Single().Value);
    }

    [Theory]
    [CombinatorialData]
    public void GetMethodHandler_ParameterlessNotification(bool supportsGetRegisteredServices)
    {
        var handlerProvider = GetHandlerProvider(supportsGetRegisteredServices);

        var methodHandlers = handlerProvider.GetMethodHandlers(TestParameterlessNotificationHandler.Name, requestType: null, responseType: null);
        Assert.Same(TestParameterlessNotificationHandler.Instance, methodHandlers.Single().Value);
    }

    [Fact]
    public void GetMethodHandler_WrongMethod_Throws()
    {
        var handlerProvider = GetHandlerProvider(supportsGetRegisteredServices: false);

        Assert.Throws<InvalidOperationException>(() => handlerProvider.GetMethodHandlers("UndefinedMethod", TestMethodHandler.RequestType, TestMethodHandler.ResponseType));
    }

    [Fact]
    public void GetMethodHandler_WrongResponseType_Throws()
    {
        var handlerProvider = GetHandlerProvider(supportsGetRegisteredServices: false);

        Assert.Throws<InvalidOperationException>(() => handlerProvider.GetMethodHandlers(TestMethodHandler.Name, TestMethodHandler.RequestType, responseType: typeof(long)));
    }

    [Theory]
    [CombinatorialData]
    public void GetRegisteredMethods(bool supportsGetRegisteredServices)
    {
        var handlerProvider = GetHandlerProvider(supportsGetRegisteredServices);

        var registeredMethods = handlerProvider.GetRegisteredMethods().OrderBy(m => m.MethodName);

        Assert.Collection(registeredMethods,
            r => Assert.Equal(TestMethodHandler.Name, r.MethodName),
            r => Assert.Equal(TestNotificationHandler.Name, r.MethodName),
            r => Assert.Equal(TestParameterlessMethodHandler.Name, r.MethodName),
            r => Assert.Equal(TestParameterlessNotificationHandler.Name, r.MethodName));
    }

    [Fact]
    public void GetMethodHandler_LanguageHandlers()
    {
        var handlerProvider = new TestHandlerWithLanguageProvider(providers: new[]
        {
            (TestXamlLanguageHandler.Metadata, TestXamlLanguageHandler.Instance, TestXamlLanguageHandler.Language),
            (TestDefaultLanguageHandler.Metadata, TestDefaultLanguageHandler.Instance, TestDefaultLanguageHandler.Language),
        });

        var methodHandlers = handlerProvider.GetMethodHandlers(TestDefaultLanguageHandler.Name, TestDefaultLanguageHandler.RequestType, TestDefaultLanguageHandler.ResponseType).OrderBy(h => h.Metadata);

        Assert.Collection(methodHandlers,
            m => Assert.Equal(TestDefaultLanguageHandler.Language, m.Metadata),
            m => Assert.Equal(TestXamlLanguageHandler.Language, m.Metadata));

        Assert.Collection(methodHandlers,
            m => Assert.Equal(TestDefaultLanguageHandler.Instance, m.Value),
            m => Assert.Equal(TestXamlLanguageHandler.Instance, m.Value));
    }

    private static HandlerProvider GetHandlerProvider(bool supportsGetRegisteredServices)
        => new(GetLspServices(supportsGetRegisteredServices));

    private static TestLspServices GetLspServices(bool supportsGetRegisteredServices)
    {
        var services = new List<(Type, object)>
        {
            (typeof(IMethodHandler), TestMethodHandler.Instance),
            (typeof(IMethodHandler), TestNotificationHandler.Instance),
            (typeof(IMethodHandler), TestParameterlessMethodHandler.Instance),
            (typeof(IMethodHandler), TestParameterlessNotificationHandler.Instance),
        };

        return new TestLspServices(services, supportsGetRegisteredServices);
    }
}
