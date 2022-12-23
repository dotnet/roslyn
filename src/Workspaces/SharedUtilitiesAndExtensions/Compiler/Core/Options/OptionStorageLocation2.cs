// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// The base type of all types that specify where options are stored.
    /// </summary>
    internal abstract class OptionStorageLocation2
#if !CODE_STYLE
        : OptionStorageLocation
#endif
    {
    }
}
