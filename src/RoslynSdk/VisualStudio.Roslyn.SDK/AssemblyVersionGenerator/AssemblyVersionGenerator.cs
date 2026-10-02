// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace AssemblyVersionGenerator
{
    [Generator]
    public class AssemblyVersionGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("assemblyversion.g.cs", $@"

internal class AssemblyVersion
{{
    public const string Version = ""{context.Compilation.Assembly.Identity.Version}"";
}}");
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
