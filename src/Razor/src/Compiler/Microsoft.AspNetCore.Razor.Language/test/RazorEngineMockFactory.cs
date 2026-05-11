// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class RazorEngineMockFactory
{
    public static T CreateFeature<T>(System.Action<Mock<T>>? configure = null)
        where T : class, IRazorEngineFeature
    {
        var mock = new Mock<T>(MockBehavior.Strict);
        mock.Setup(feature => feature.Initialize(It.IsAny<RazorEngine>()));
        configure?.Invoke(mock);
        return mock.Object;
    }

    public static T CreateProjectFeature<T>(System.Action<Mock<T>>? configure = null)
        where T : class, IRazorProjectEngineFeature
    {
        var mock = new Mock<T>(MockBehavior.Strict);
        mock.Setup(feature => feature.Initialize(It.IsAny<RazorProjectEngine>()));
        configure?.Invoke(mock);
        return mock.Object;
    }

    public static T CreatePhase<T>(System.Action<Mock<T>>? configure = null)
        where T : class, IRazorEnginePhase
    {
        var mock = new Mock<T>(MockBehavior.Strict);
        mock.Setup(phase => phase.Initialize(It.IsAny<RazorEngine>()));
        configure?.Invoke(mock);
        return mock.Object;
    }
}
