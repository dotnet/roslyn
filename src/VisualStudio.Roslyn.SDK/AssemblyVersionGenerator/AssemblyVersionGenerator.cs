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
