// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// This lets consumer to get to inner temporary storage that references use
    /// as its shadow copy storage
    /// </summary>
    internal interface ISupportTemporaryStorage
    {
        IReadOnlyList<ITemporaryStreamStorageInternal>? GetStorages();
    }
}
