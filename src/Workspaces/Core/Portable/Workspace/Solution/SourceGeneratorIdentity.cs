// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal record struct SourceGeneratorIdentity(string AssemblyName, string TypeName)
    {
        public SourceGeneratorIdentity(ISourceGenerator generator)
            : this(GetGeneratorAssemblyName(generator), GetGeneratorTypeName(generator))
        {
        }

        public static string GetGeneratorAssemblyName(ISourceGenerator generator)
        {
            return generator.GetGeneratorType().Assembly.FullName!;
        }

        public static string GetGeneratorTypeName(ISourceGenerator generator)
        {
            return generator.GetGeneratorType().FullName!;
        }
    }
}
