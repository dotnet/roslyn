// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.RoslynTools.Insertion;

internal static class Extensions
{
    public static Version ToFullVersion(this Version version)
        => new(version.Major, version.Minor, version.Build == -1 ? 0 : version.Build, version.Revision == -1 ? 0 : version.Revision);

    public static bool IsFileNotFound(this VssServiceException exception)
        => exception.Message.StartsWith("TF401174" /* The item could not be found */);
}
