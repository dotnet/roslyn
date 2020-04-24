// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EncapsulateField
{
    internal abstract partial class AbstractEncapsulateFieldService
    {
        internal class Result
        {
            public Solution Solution { get; }
            public string Name { get; }
            public Glyph Glyph { get; }

            public Result(Solution solutionWithProperty, string name, Glyph glyph)
            {
                Solution = solutionWithProperty;
                Name = name;
                Glyph = glyph;
            }

            public Result(Solution originalSolution)
                : this(originalSolution, string.Empty, Glyph.Error)
            {
            }
        }
    }
}
