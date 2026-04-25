// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal static class WellKnownStartupOrder
{
    // Client capabilities is first because it has no dependencies, but a lot of other things need to check capabilities
    public const int ClientCapabilities = int.MinValue;

    // Client server manager similarly has no dependencies, and nothing else can make requests without it
    public const int ClientServerManager = int.MinValue;

    // Options are early in case something needs to know what is enabled
    public const int LanguageServerFeatureOptions = -1000;

    // Remote services initialize before "default", but depends on the above so not too early
    public const int RemoteServices = -500;

    public const int Default = 0;

    // Dynamic registration is ordered last, because endpoints do all sorts of weird things in their register methods
    public const int DynamicRegistration = int.MaxValue;

}
