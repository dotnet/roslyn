﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Capabilities specific to the `workspace/didChangeConfiguration` notification.
/// </summary>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didChangeConfigurationClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
internal class DidChangeConfigurationClientCapabilities : DynamicRegistrationSetting
{
}
