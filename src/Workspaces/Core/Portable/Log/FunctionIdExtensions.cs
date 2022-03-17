// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal static class FunctionIdExtensions
    {
        private static readonly Lazy<ImmutableDictionary<FunctionId, string>> s_functionIdsToString = new(
            () => Enum.GetValues(typeof(FunctionId)).Cast<FunctionId>().ToImmutableDictionary(f => f, f => f.ToString()));

        public static string Convert(this FunctionId functionId) => s_functionIdsToString.Value[functionId];
    }
}
