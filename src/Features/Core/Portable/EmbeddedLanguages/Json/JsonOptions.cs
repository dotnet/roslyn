// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;

[Flags]
internal enum JsonOptions
{
    /// <summary>
    /// Parse using Newtonsoft JSON.net rules.  This is very relaxed and allows lots of features not allowed by standard
    /// IETF 8259.
    /// </summary> 
    Newtonsoft = 0,
    /// <summary>
    /// Strict IETF 8259 mode.  Anything not allowed by that spec is flagged as an error.
    /// </summary>
    Strict = 1,
    /// <summary>
    /// Same as <see cref="Strict"/> except that comments are allowed as well.
    /// </summary>
    Comments = 2 | Strict,
    /// <summary>
    /// Same as <see cref="Strict"/> except that trailing commas are allowed as well.
    /// </summary>
    TrailingCommas = 4 | Strict,
}
