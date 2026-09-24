// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Test.Utilities;

public enum SequencePointDisplayMode
{
    /// <summary>
    /// Do not display sequence points.
    /// </summary>
    None,
    /// <summary>
    /// Display minimal sequence points as just `-` in baselines.
    /// </summary>
    Minimal,
    /// <summary>
    /// Display sequence points with full IL offsets and source line/column information.
    /// </summary>
    Enhanced
}
