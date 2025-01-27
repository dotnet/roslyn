// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices.Interactive;

internal static class LogMessage
{
    public const string Window = "InteractiveWindow";
    public const string Create = nameof(Create);
    public const string Close = nameof(Close);
    public const string LanguageBufferCount = nameof(LanguageBufferCount);
}
