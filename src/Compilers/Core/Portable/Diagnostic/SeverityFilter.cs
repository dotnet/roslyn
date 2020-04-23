// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents a set of filtered diagnostic severities.
    /// Currently, we only support filtering out Hidden and Info severities during build.
    /// </summary>
    [Flags]
    internal enum SeverityFilter
    {
        None = 0x00,
        Hidden = 0x01,
        Info = 0x10
    }

    internal static class SeverityFilterExtensions
    {
        internal static bool Contains(this SeverityFilter severityFilter, ReportDiagnostic severity)
        {
            return severity switch
            {
                ReportDiagnostic.Hidden => (severityFilter & SeverityFilter.Hidden) != 0,
                ReportDiagnostic.Info => (severityFilter & SeverityFilter.Info) != 0,
                _ => false
            };
        }
    }
}
