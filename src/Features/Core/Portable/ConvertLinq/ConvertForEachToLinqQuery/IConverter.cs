// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery
{
    internal interface IConverter<TForEachStatement, TStatement>
    {
        ForEachInfo<TForEachStatement, TStatement> ForEachInfo { get; }

        void Convert(SyntaxEditor editor, bool convertToQuery, CancellationToken cancellationToken);
    }
}
