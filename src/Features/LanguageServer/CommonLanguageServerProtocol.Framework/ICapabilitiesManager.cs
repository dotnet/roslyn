// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommonLanguageServerProtocol.Framework;

public interface ICapabilitiesManager<RequestType, ResponseType>
{
    ResponseType GetServerCapabilities();
    void SetClientCapabilities(RequestType request);

    RequestType GetClientCapabilities();
}
