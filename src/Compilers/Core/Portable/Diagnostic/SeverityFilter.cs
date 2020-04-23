// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents a set of filtered diagnostic severities.
    /// </summary>
    internal readonly struct SeverityFilter
    {
        private const int HiddenBit = 0x01;
        private const int InfoBit = 0x10;

        private readonly int _flag;

        private SeverityFilter(bool includeHidden, bool includeInfo)
        {
            _flag = 0;

            if (includeHidden)
            {
                _flag = HiddenBit;
            }

            if (includeInfo)
            {
                _flag |= InfoBit;
            }
        }

        internal static SeverityFilter Hidden = new SeverityFilter(includeHidden: true, includeInfo: false);
        internal static SeverityFilter HiddenAndInfo = new SeverityFilter(includeHidden: true, includeInfo: true);

        internal bool IsEmpty => _flag == 0;
        internal bool Contains(ReportDiagnostic severity)
        {
            return severity switch
            {
                ReportDiagnostic.Hidden => (_flag & HiddenBit) != 0,
                ReportDiagnostic.Info => (_flag & InfoBit) != 0,
                _ => false
            };
        }
    }
}
