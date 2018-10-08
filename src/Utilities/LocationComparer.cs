// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Compares Locations, ordering locations in source code first.
    /// </summary>
    internal sealed class LocationComparer : IComparer<Location>
    {
        /// <summary>
        /// Default instance.
        /// </summary>
        public static readonly LocationComparer Instance = new LocationComparer();

        public int Compare(Location x, Location y)
        {
            if (x.IsInSource && !y.IsInSource)
            {
                return -1;
            }
            else if (!x.IsInSource && y.IsInSource)
            {
                return 1;
            }
            else if (x.IsInSource && y.IsInSource)
            {
                return x.SourceSpan.CompareTo(y.SourceSpan);
            }
            else
            {
                return 0;
            }
        }
    }
}
