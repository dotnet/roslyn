// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
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

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(GeneratedEnglishClassName, SourceText.From($$"""
                /// <summary><see cref="{{GeneratedEnglishClassName}}" /> is a simple class to fetch the classic message.</summary>
                internal class {{GeneratedEnglishClassName}}
                {
                    public static string GetMessage()
                    {
                        return "Hello, World!";
                    }
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
