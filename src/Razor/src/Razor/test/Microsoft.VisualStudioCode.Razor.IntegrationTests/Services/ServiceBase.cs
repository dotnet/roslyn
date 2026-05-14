// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Base class for all integration test service classes.
/// </summary>
public abstract class ServiceBase(IntegrationTestServices testServices)
{
    /// <summary>
    /// Gets the integration test services container.
    /// </summary>
    protected IntegrationTestServices TestServices { get; } = testServices;
}
