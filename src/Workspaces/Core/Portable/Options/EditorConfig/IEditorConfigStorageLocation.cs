// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

#if CODE_STYLE
namespace Microsoft.CodeAnalysis.Internal.Options
#else
namespace Microsoft.CodeAnalysis.Options
#endif
{
    internal interface IEditorConfigStorageLocation
    {
        bool TryGetOption(IReadOnlyDictionary<string, string> rawOptions, Type type, out object value);
    }
}
