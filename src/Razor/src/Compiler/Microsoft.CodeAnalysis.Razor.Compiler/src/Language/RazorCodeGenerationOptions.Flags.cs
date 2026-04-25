// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RazorCodeGenerationOptions
{
    [Flags]
    private enum Flags
    {
        DesignTime = 1 << 0,
        IndentWithTabs = 1 << 1,
        SuppressChecksum = 1 << 2,
        SuppressMetadataAttributes = 1 << 3,
        SuppressMetadataSourceChecksumAttributes = 1 << 4,
        SuppressPrimaryMethodBody = 1 << 5,
        SuppressNullabilityEnforcement = 1 << 6,
        OmitMinimizedComponentAttributeValues = 1 << 7,
        SupportLocalizedComponentNames = 1 << 8,
        UseEnhancedLinePragma = 1 << 9,
        SuppressAddComponentParameter = 1 << 10,
        RemapLinePragmaPathsOnWindows = 1 << 11,

        DefaultFlags = UseEnhancedLinePragma,
        DefaultDesignTimeFlags = DesignTime | SuppressMetadataAttributes | UseEnhancedLinePragma | RemapLinePragmaPathsOnWindows
    }
}
