// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ImplementType;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Options;

internal sealed record class OmniSharpEditorConfigOptions
{
    public required OmniSharpLineFormattingOptions LineFormattingOptions { get; init; }
    public OmniSharpImplementTypeOptions ImplementTypeOptions { get; init; }
}
