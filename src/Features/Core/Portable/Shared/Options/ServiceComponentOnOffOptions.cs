// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    /// <summary>
    /// options to indicate whether a certain component in Roslyn is enabled or not
    /// </summary>
    internal static class ServiceComponentOnOffOptions
    {
        public static readonly Option2<bool> DiagnosticProvider = new Option2<bool>(nameof(ServiceComponentOnOffOptions), nameof(DiagnosticProvider), defaultValue: true);
    }
}
