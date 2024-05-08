// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class BuildHostLogger(TextWriter output)
{
    public void LogInformation(string message)
        => output.WriteLine(message);

    public void LogCritical(string message)
        => output.WriteLine(message);
}
