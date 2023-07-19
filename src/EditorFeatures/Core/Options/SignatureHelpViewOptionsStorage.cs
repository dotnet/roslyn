// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Options;

internal sealed class SignatureHelpViewOptionsStorage
{
    public static readonly PerLanguageOption2<bool> ShowSignatureHelp = new(
        "dotnet_show_signature_help", defaultValue: true);
}
