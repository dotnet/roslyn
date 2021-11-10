// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

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
    }
}
