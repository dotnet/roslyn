// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class SyntaxTreeComparer : IEqualityComparer<SyntaxTree>
    {
        public static readonly SyntaxTreeComparer Instance = new SyntaxTreeComparer();

        public bool Equals(SyntaxTree x, SyntaxTree y)
        {
            if (x == null)
            {
                return y == null;
            }
            else if (y == null)
            {
                return false;
            }

            return string.Equals(x.FilePath, y.FilePath, StringComparison.OrdinalIgnoreCase) &&
                SourceTextComparer.Instance.Equals(x.GetText(), y.GetText());
        }

        public int GetHashCode(SyntaxTree obj)
        {
            return Hash.Combine(obj.FilePath.GetHashCode(), SourceTextComparer.Instance.GetHashCode(obj.GetText()));
        }
    }
}
