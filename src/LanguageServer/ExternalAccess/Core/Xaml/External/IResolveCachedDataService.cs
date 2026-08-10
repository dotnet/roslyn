// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

/// <summary>
/// Represents a service to convert between a large data object + document identifier and request resolve data.
/// </summary>
/// <remarks>
/// The data is held in a short-term cache and the service is provided to implementers of <see cref="XamlRequestHandlerFactoryBase{TRequest, TResponse}" />
/// </remarks>
internal interface IResolveCachedDataService
{
    object ToResolveData(object data, Uri uri);
    (object? data, Uri? uri) FromResolveData(object? resolveData);
}
