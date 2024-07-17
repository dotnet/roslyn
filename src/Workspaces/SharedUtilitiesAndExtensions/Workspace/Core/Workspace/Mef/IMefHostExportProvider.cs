// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Host.Mef;

internal interface IMefHostExportProvider
{
    IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>();
    IEnumerable<Lazy<TExtension>> GetExports<TExtension>();
}
