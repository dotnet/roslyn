// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Used when a non-runtime specific loader is needed that is never intended to be used.
    /// </summary>
    internal sealed class ThrowingAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            throw new NotImplementedException();
        }
    }
}
