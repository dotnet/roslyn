// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    internal interface IOrderedReadOnlySet<T> : IReadOnlySet<T>, IReadOnlyList<T>
    {
    }
}
