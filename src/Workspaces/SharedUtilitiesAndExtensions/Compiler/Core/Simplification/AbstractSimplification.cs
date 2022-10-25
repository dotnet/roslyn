// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Simplification;

internal abstract class AbstractSimplification : ISimplification
{
    public abstract SimplifierOptions DefaultOptions { get; }
    public abstract SimplifierOptions GetSimplifierOptions(AnalyzerConfigOptions options, SimplifierOptions? fallbackOptions);
}
