// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    // Compare id and location only.
    internal sealed class SimpleDiagnosticComparer : IEqualityComparer<Diagnostic>
    {
        internal static readonly SimpleDiagnosticComparer Instance = new SimpleDiagnosticComparer();

        public bool Equals(Diagnostic x, Diagnostic y)
        {
            return x.Id == y.Id && x.Location == y.Location;
        }

        public int GetHashCode(Diagnostic obj)
        {
            return Hash.Combine(obj.Id.GetHashCode(), obj.Location.GetHashCode());
        }
    }
}
