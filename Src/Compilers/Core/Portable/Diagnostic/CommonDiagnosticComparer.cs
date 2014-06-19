// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // For the purposes of lambda error reporting we wish to compare 
    // diagnostics for equality only considering their code and location,
    // but not other factors such as the values supplied for the 
    // parameters of the diagnostic.

    internal sealed class CommonDiagnosticComparer : IEqualityComparer<Diagnostic>
    {
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
