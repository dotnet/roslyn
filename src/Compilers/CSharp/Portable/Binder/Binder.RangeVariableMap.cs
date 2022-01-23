// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// We represent the set of query variables in scope at a particular point by a RangeVariableMap.
        /// Each query variable in scope has a key in this map.  If the corresponding value is empty, then
        /// that query variable is represented directly by a lambda parameter.  If it is non-empty, then
        /// to get the value of that query variable one starts with the first parameter of the current
        /// lambda (the first parameter is always the transparent one), and dot through its members using
        /// the names in the value list, in reverse order.  So, for example, if the query variable "x" has
        /// a value in this map of ["Item2", "Item1", "Item1"], then the way to compute the value of that
        /// query variable is starting with the current lambda's first parameter P, compute "P.Item1.Item1.Item2".
        /// See also WithQueryLambdaParametersBinder.
        /// </summary>
        private class RangeVariableMap : Dictionary<RangeVariableSymbol, ImmutableArray<string>>
        {
            public RangeVariableMap() { }
        }
    }
}
