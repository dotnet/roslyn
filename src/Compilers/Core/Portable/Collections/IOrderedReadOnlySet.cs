// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    internal interface IOrderedReadOnlySet<T> : IReadOnlySet<T>, IReadOnlyList<T>
    {
    }
}
