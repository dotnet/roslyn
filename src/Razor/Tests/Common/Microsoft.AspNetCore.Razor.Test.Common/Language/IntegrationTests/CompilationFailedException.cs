// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class CompilationFailedException : XunitException
{
    public CompilationFailedException(Compilation compilation, Diagnostic[] diagnostics)
        : base(userMessage: "Compilation failed")
    {
        Compilation = compilation;
        Diagnostics = diagnostics;
    }

    public Compilation Compilation { get; }

    public Diagnostic[] Diagnostics { get; }

    public override string Message
    {
        get
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            builder.AppendLine("Compilation failed: ");

            var syntaxTreesWithErrors = new HashSet<SyntaxTree>();
            foreach (var diagnostic in Diagnostics)
            {
                builder.AppendLine(diagnostic.ToString());

                if (diagnostic.Location.IsInSource)
                {
                    syntaxTreesWithErrors.Add(diagnostic.Location.SourceTree);
                }
            }

            if (syntaxTreesWithErrors.Any())
            {
                builder.AppendLine();
                builder.AppendLine();

                foreach (var syntaxTree in syntaxTreesWithErrors)
                {
                    builder.AppendLine($"File {syntaxTree.FilePath ?? "unknown"}:");
                    builder.AppendLine(syntaxTree.GetText().ToString());
                }
            }

            return builder.ToString();
        }
    }
}
