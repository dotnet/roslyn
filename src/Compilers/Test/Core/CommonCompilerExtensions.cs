// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    internal static class CommonCompilerExtensions
    {
        internal static (int Result, string Output) Run(this CommonCompiler compiler, CancellationToken cancellationToken = default)
        {
            using var writer = new StringWriter();
            var result = compiler.Run(writer, cancellationToken);
            return (result, writer.ToString());
        }
    }
}
