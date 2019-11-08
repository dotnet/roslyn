// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public bool Equals(Diagnostic x, Diagnostic y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x is { Location: y.Location, Id: y.Id };
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
