// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal record struct SourceGeneratorIdentity(string AssemblyName, Version AssemblyVersion, string TypeName)
    {
        public SourceGeneratorIdentity(ISourceGenerator generator)
            : this(GetGeneratorAssemblyName(generator), generator.GetGeneratorType().Assembly.GetName().Version!, GetGeneratorTypeName(generator))
        {
        }

        public static string GetGeneratorAssemblyName(ISourceGenerator generator)
        {
            return generator.GetGeneratorType().Assembly.GetName().Name!;
        }

        public static string GetGeneratorTypeName(ISourceGenerator generator)
        {
            return generator.GetGeneratorType().FullName!;
        }
    }
}
