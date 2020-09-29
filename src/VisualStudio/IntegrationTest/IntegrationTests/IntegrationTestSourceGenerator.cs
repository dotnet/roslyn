// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [Generator]
    internal sealed class IntegrationTestSourceGenerator : ISourceGenerator
    {
        public const string GeneratedClassName = nameof(IntegrationTestSourceGenerator) + "Output";

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(GeneratedClassName, SourceText.From(@"
internal class " + GeneratedClassName + @"
{
    public static string GetMessage()
    {
        return ""Hello, World!"";
    }
}
", encoding: Encoding.UTF8));
        }
    }
}
