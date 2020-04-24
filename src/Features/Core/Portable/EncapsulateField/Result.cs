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
            public ImmutableArray<IFieldSymbol> FailedFields { get; }

            public Result(Solution solutionWithProperty, string name, Glyph glyph)
            {
                Solution = solutionWithProperty;
                Name = name;
                Glyph = glyph;
            }

            public Result(Solution solutionWithProperty, string name, Glyph glyph, List<IFieldSymbol> failedFieldSymbols)
                : this(solutionWithProperty, name, glyph)
            {
                FailedFields = failedFieldSymbols.ToImmutableArrayOrEmpty();
            }

            public Result(Solution originalSolution, params IFieldSymbol[] fields)
                : this(originalSolution, string.Empty, Glyph.Error)
            {
                FailedFields = fields.ToImmutableArrayOrEmpty();
            }

            public Result WithFailedFields(List<IFieldSymbol> failedFieldSymbols)
            {
                if (failedFieldSymbols.Count == 0)
                {
                    return this;
                }

                return new Result(Solution, Name, Glyph, failedFieldSymbols);
            }
        }
    }
}
