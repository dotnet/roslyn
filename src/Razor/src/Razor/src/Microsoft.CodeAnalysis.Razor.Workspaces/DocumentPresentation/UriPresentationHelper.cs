// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.CodeAnalysis.Razor.DocumentPresentation;

internal static class UriPresentationHelper
{
    public static Uri? GetComponentFileNameFromUriPresentationRequest(Uri[]? uris, ILogger logger)
    {
        if (uris is null || uris.Length == 0)
        {
            logger.LogDebug($"No URIs were included in the request?");
            return null;
        }

        var razorFileUri = uris.Where(
            x => Path.GetFileName(x.GetAbsoluteOrUNCPath()).EndsWith(".razor", PathUtilities.OSSpecificPathComparison)).FirstOrDefault();

        // We only want to handle requests for a single .razor file, but when there are files nested under a .razor
        // file (for example, Goo.razor.css, Goo.razor.cs etc.) then we'll get all of those files as well, when the user
        // thinks they're just dragging the parent one, so we have to be a little bit clever with the filter here
        if (razorFileUri == null)
        {
            logger.LogDebug($"No file in the drop was a razor file URI.");
            return null;
        }

        var fileName = Path.GetFileName(razorFileUri.GetAbsoluteOrUNCPath());
        if (uris.Any(uri => !Path.GetFileName(uri.GetAbsoluteOrUNCPath()).StartsWith(fileName, PathUtilities.OSSpecificPathComparison)))
        {
            logger.LogDebug($"One or more URIs were not a child file of the main .razor file.");
            return null;
        }

        return razorFileUri;
    }
}
