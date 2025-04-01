// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public sealed class HandlerProviderTests
{
    [Theory, CombinatorialData]
    public void GetMethodHandler(bool supportsMethodHandlerProvider)
    {
        var handlerProvider = GetHandlerProvider(supportsMethodHandlerProvider);

        var methodHandler = handlerProvider.GetMethodHandler(TestMethodHandler.Name, TestMethodHandler.RequestType, TestMethodHandler.ResponseType, LanguageServerConstants.DefaultLanguageName);
        Assert.Same(TestMethodHandler.Instance, methodHandler);
    }

    [Theory, CombinatorialData]
    public void GetMethodHandler_Parameterless(bool supportsMethodHandlerProvider)
    {
        var handlerProvider = GetHandlerProvider(supportsMethodHandlerProvider);

        var methodHandler = handlerProvider.GetMethodHandler(TestParameterlessMethodHandler.Name, requestTypeRef: null, TestParameterlessMethodHandler.ResponseTypeRef, LanguageServerConstants.DefaultLanguageName);
        Assert.Same(TestParameterlessMethodHandler.Instance, methodHandler);
    }

    [Theory, CombinatorialData]
    public void GetMethodHandler_Notification(bool supportsMethodHandlerProvider)
    {
        var handlerProvider = GetHandlerProvider(supportsMethodHandlerProvider);

        var methodHandler = handlerProvider.GetMethodHandler(TestNotificationHandler.Name, TestNotificationHandler.RequestTypeRef, responseTypeRef: null, LanguageServerConstants.DefaultLanguageName);
        Assert.Same(TestNotificationHandler.Instance, methodHandler);
    }

    [Theory, CombinatorialData]
    public void GetMethodHandler_ParameterlessNotification(bool supportsMethodHandlerProvider)
    {
        var handlerProvider = GetHandlerProvider(supportsMethodHandlerProvider);

        var methodHandler = handlerProvider.GetMethodHandler(TestParameterlessNotificationHandler.Name, requestTypeRef: null, responseTypeRef: null, LanguageServerConstants.DefaultLanguageName);
        Assert.Same(TestParameterlessNotificationHandler.Instance, methodHandler);
    }

    [Fact]
    public void GetMethodHandler_WrongMethod_Throws()
    {
        var handlerProvider = GetHandlerProvider(supportsMethodHandlerProvider: false);

        Assert.Throws<InvalidOperationException>(() => handlerProvider.GetMethodHandler("UndefinedMethod", TestMethodHandler.RequestType, TestMethodHandler.ResponseType, LanguageServerConstants.DefaultLanguageName));
    }

    [Fact]
    public void GetMethodHandler_WrongResponseType_Throws()
    {
        var handlerProvider = GetHandlerProvider(supportsMethodHandlerProvider: false);

        Assert.Throws<InvalidOperationException>(() => handlerProvider.GetMethodHandler(TestMethodHandler.Name, TestMethodHandler.RequestType, responseTypeRef: TypeRef.Of<long>(), LanguageServerConstants.DefaultLanguageName));
    }

    [Theory, CombinatorialData]
    public void GetRegisteredMethods(bool supportsMethodHandlerProvider)
    {
        var handlerProvider = GetHandlerProvider(supportsMethodHandlerProvider);

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
        var handlerProvider = new TestHandlerProvider(providers: [
            (TestXamlLanguageHandler.Metadata, TestXamlLanguageHandler.Instance),
            (TestDefaultLanguageHandler.Metadata, TestDefaultLanguageHandler.Instance),
        ]);

        var defaultMethodHandler = handlerProvider.GetMethodHandler(TestDefaultLanguageHandler.Name, TestDefaultLanguageHandler.RequestType, TestDefaultLanguageHandler.ResponseType, LanguageServerConstants.DefaultLanguageName);
        Assert.Equal(TestDefaultLanguageHandler.Instance, defaultMethodHandler);

        var xamlMethodHandler = handlerProvider.GetMethodHandler(TestDefaultLanguageHandler.Name, TestDefaultLanguageHandler.RequestType, TestDefaultLanguageHandler.ResponseType, TestXamlLanguageHandler.Language);
        Assert.Equal(TestXamlLanguageHandler.Instance, xamlMethodHandler);
    }

    private static HandlerProvider GetHandlerProvider(bool supportsMethodHandlerProvider)
        => new(GetLspServices(supportsMethodHandlerProvider), TypeRef.DefaultResolver.Instance);

    private static ILspServices GetLspServices(bool supportsMethodHandlerProvider)
    {
        var services = new List<(Type, object)>
        {
            (typeof(IMethodHandler), TestMethodHandler.Instance),
            (typeof(IMethodHandler), TestNotificationHandler.Instance),
            (typeof(IMethodHandler), TestParameterlessMethodHandler.Instance),
            (typeof(IMethodHandler), TestParameterlessNotificationHandler.Instance),
        };

        return TestLspServices.Create(services, supportsMethodHandlerProvider);
    }
}
