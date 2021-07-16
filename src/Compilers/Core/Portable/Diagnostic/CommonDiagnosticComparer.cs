// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class CommonDiagnosticComparer : IEqualityComparer<Diagnostic>
    {
        internal static readonly CommonDiagnosticComparer Instance = new CommonDiagnosticComparer();

        private CommonDiagnosticComparer()
        {
        }

        public bool Equals(Diagnostic? x, Diagnostic? y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Location == y.Location && x.Id == y.Id;
        }

        public int GetHashCode(Diagnostic obj)
        {
            if (object.ReferenceEquals(obj, null))
            {
                return 0;
            }

            return Hash.Combine(obj.Location, obj.Id.GetHashCode());
        }
    }
}
