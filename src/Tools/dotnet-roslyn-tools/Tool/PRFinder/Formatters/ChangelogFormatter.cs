// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRFinder.Formatters;

internal class ChangelogFormatter : DefaultFormatter
{
    public override string FormatPRListItem(string comment, string prNumber, string prUrl)
    {
        prNumber = prNumber.StartsWith("#")
            ? prNumber
            : $"#{prNumber}";

        return $"  * {comment} (PR: [{prNumber}]({prUrl}))";
    }
}
