// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit
{
    internal interface IIdeSettingsAttribute
    {
        VisualStudioVersion MinVersion { get; }

        VisualStudioVersion MaxVersion { get; }

        /// <summary>
        /// Gets the root suffix to use for Visual Studio instances.
        /// </summary>
        /// <value>
        /// <list type="bullet">
        /// <item><description><see langword="null"/> to use the default experimental instance <c>Exp</c></description></item>
        /// <item><description><c>""</c> to use the default (non-experimental) Visual Studio instance</description></item>
        /// <item><description>Another value to use a custom experimental instance</description></item>
        /// </list>
        /// </value>
        string? RootSuffix { get; }

        /// <summary>
        /// Gets the maximum number of retry attempts for a test.
        /// </summary>
        /// <value>
        /// <list type="bullet">
        /// <item><description><c>0</c> to inherit the value from an attribute applied to a containing type or member, or use the default value when no other value is specified (equivalent to <c>1</c>; tests will not be automatically retried on failure)</description></item>
        /// <item><description><c>1</c> to not retry the test on failure</description></item>
        /// <item><description>An explicit value greater than <c>1</c> to retry the test up to a total of this many attempts on failure</description></item>
        /// </list>
        /// </value>
        int MaxAttempts { get; }

        /// <summary>
        /// Gets the environment variables to set before launching the Visual Studio test process. Each variable has the
        /// form <c>key=value</c>.
        /// </summary>
        string[] EnvironmentVariables { get; }
    }
}
