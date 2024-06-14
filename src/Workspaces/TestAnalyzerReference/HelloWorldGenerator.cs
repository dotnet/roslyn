// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.TestSourceGenerator
{
    [Generator]
    public sealed class HelloWorldGenerator : ISourceGenerator
    {
        public const string GeneratedEnglishClassName = "HelloWorld";
        public const string GeneratedSpanishClassName = "HolaMundo";
        public const string GeneratedFolderName = "Folder";
        public const string GeneratedFolderClassName = "HelloFolder";

        public void Initialize(GeneratorInitializationContext context)
        {
        }

#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        public void Execute(GeneratorExecutionContext context)
        {
            var methodCalls = context.Compilation.SyntaxTrees
                .Select(t => t.GetRoot())
                .SelectMany(n => n.DescendantNodes().OfType<InvocationExpressionSyntax>())
                .Where(i => i.ArgumentList.Arguments.Count == 0);

            var interceptorMethods = new StringBuilder();

            var index = 0;
            foreach (var methodCall in methodCalls)
            {
                var semanticModel = context.Compilation.GetSemanticModel(methodCall.SyntaxTree);
                var interceptorLocation = semanticModel.GetInterceptableLocation(methodCall);

                if (interceptorLocation != null)
                {
                    interceptorMethods.AppendLine(interceptorLocation.GetInterceptsLocationAttributeSyntax());
                    interceptorMethods.AppendLine($@"public static void Intercept{index++}() {{ }}");
                    interceptorMethods.AppendLine();
                }
            }

            context.AddSource(GeneratedEnglishClassName, SourceText.From($$"""
                /// <summary><see cref="{{GeneratedEnglishClassName}}" /> is a simple class to fetch the classic message.</summary>
                internal class {{GeneratedEnglishClassName}}
                {
                    public static string GetMessage()
                    {
                        return "Hello, World!";
                    }

                    {{interceptorMethods}}
                }
                """, encoding: Encoding.UTF8));

            context.AddSource(GeneratedSpanishClassName, SourceText.From($$"""
                internal class {{GeneratedSpanishClassName}}
                {
                    public static string GetMessage()
                    {
                        return "Hola, Mundo!";
                    }
                }
                """, encoding: Encoding.UTF8));

            context.AddSource(GeneratedEnglishClassName + "WithTime", SourceText.From($$"""
                /// <summary><see cref="{{GeneratedEnglishClassName}}WithTime" /> is a simple class to fetch the classic message.</summary>
                internal class {{GeneratedEnglishClassName}}WithTime
                {
                    public static string GetMessage()
                    {
                        return "Hello, World @ {{DateTime.UtcNow.ToLocalTime().ToLongTimeString()}}";
                    }
                }
                """, encoding: Encoding.UTF8));

            context.AddSource($"{GeneratedFolderName}/{GeneratedFolderClassName}", $$"""
                class {{GeneratedFolderClassName}} { }
                """);
        }
    }
}
