﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Options passed to a <see cref="GeneratorDriver"/> during creation
    /// </summary>
    public readonly struct GeneratorDriverOptions
    {
        public readonly IncrementalGeneratorOutputKind DisabledOutputs;

        public GeneratorDriverOptions(IncrementalGeneratorOutputKind disabledOutputs)
        {
            DisabledOutputs = disabledOutputs;
        }
    }
}
