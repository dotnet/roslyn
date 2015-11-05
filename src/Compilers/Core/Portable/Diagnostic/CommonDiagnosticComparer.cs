// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class CommonDiagnosticComparer : IEqualityComparer<Diagnostic>
    {
        internal static readonly CommonDiagnosticComparer CompareIdAndLocationOnly = new CommonDiagnosticComparer(compareStringRepresentation: false);
        internal static readonly CommonDiagnosticComparer CompareAll = new CommonDiagnosticComparer(compareStringRepresentation: true);

        private readonly bool _compareStringRepresentation;

        private CommonDiagnosticComparer(bool compareStringRepresentation)
        {
            _compareStringRepresentation = compareStringRepresentation;
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

            if (x.Location != y.Location || x.Id != y.Id)
            {
                return false;
            }

            if (!_compareStringRepresentation)
            {
                return true;
            }

            // Compare string representations rather than requiring
            // argument types to implement Equals.
            var xs = ToString(x);
            var ys = ToString(y);
            return string.Equals(xs, ys, StringComparison.Ordinal);
        }

        public int GetHashCode(Diagnostic obj)
        {
            if (object.ReferenceEquals(obj, null))
            {
                return 0;
            }

            return Hash.Combine(obj.Location, obj.Id.GetHashCode());
        }

        private static string ToString(Diagnostic diagnostic)
        {
            return DiagnosticFormatter.Instance.Format(diagnostic, CultureInfo.InvariantCulture);
        }
    }
}
