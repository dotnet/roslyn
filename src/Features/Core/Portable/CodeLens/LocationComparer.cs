// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeLens
{
    internal sealed class LocationComparer : IEqualityComparer<Location>
    {
        public static LocationComparer Instance { get; } = new LocationComparer();

        public bool Equals(Location x, Location y)
        {
            if (x is { IsInSource: true } && y is { IsInSource: true })
            {
                return x.SourceSpan.Equals(y.SourceSpan) &&
                       x.SourceTree.FilePath.Equals(y.SourceTree.FilePath, StringComparison.OrdinalIgnoreCase);
            }

            return object.Equals(x, y);
        }

        public int GetHashCode(Location obj)
        {
            if (obj != null && obj.IsInSource)
            {
                return Hash.Combine(obj.SourceSpan.GetHashCode(),
                   StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SourceTree.FilePath));
            }
            return obj?.GetHashCode() ?? 0;
        }
    }
}
