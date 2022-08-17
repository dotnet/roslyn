// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.TestSourceGenerator
{
    [Generator]
    public sealed class HelloWorldGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(Constants.GeneratedEnglishClassName, SourceText.From(@"
/// <summary><see cref=""" + Constants.GeneratedEnglishClassName + @""" /> is a simple class to fetch the classic message.</summary>
internal class " + Constants.GeneratedEnglishClassName + @"
{
    public static string GetMessage()
    {
        return ""Hello, World!"";
    }
}
", encoding: Encoding.UTF8));

            context.AddSource(Constants.GeneratedSpanishClassName, SourceText.From(@"
internal class " + Constants.GeneratedSpanishClassName + @"
{
    public static string GetMessage()
    {
        return ""Hola, Mundo!"";
    }
}
", encoding: Encoding.UTF8));
        }
    }
}
