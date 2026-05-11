// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.VisualStudio.Razor;

internal static class RazorConstants
{
    public const string LegacyContentType = "LegacyRazorCSharp";

    public const string LegacyCoreContentType = "LegacyRazorCoreCSharp";

    public const string RazorLSPContentTypeName = "Razor";

    public const string RazorLanguageServiceString = "4513FA64-5B72-4B58-9D4C-1D3C81996C2C";

    public const string RazorCohostingUIContext = "6d5b86dc-6b8a-483b-ae30-098a3c7d6774";

    public static readonly Guid RazorLanguageServiceGuid = new(RazorLanguageServiceString);

    public const string VSProjectItemsIdentifier = "CF_VSSTGPROJECTITEMS";

    public const int AboveManagedProjectSystemOrder = 50;
}
