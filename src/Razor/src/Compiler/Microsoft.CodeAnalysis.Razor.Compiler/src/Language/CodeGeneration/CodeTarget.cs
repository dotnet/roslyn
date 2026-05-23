// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public abstract class CodeTarget(RazorCodeDocument codeDocument, ImmutableArray<ICodeTargetExtension> targetExtensions)
{
    public RazorCodeDocument CodeDocument => codeDocument;
    public RazorCodeGenerationOptions Options => codeDocument.CodeGenerationOptions;
    public ImmutableArray<ICodeTargetExtension> Extensions => targetExtensions;

    public static CodeTarget CreateDefault(
        RazorCodeDocument codeDocument,
        Action<CodeTargetBuilder>? configure = null)
    {
        ArgHelper.ThrowIfNull(codeDocument);

        var builder = new DefaultCodeTargetBuilder(codeDocument);
        configure?.Invoke(builder);
        return builder.Build();
    }

    public abstract IntermediateNodeWriter CreateNodeWriter();

    public TExtension? GetExtension<TExtension>()
        where TExtension : class, ICodeTargetExtension
    {
        foreach (var extension in Extensions)
        {
            if (extension is TExtension match)
            {
                return match;
            }
        }

        return null;
    }

    public bool HasExtension<TExtension>()
        where TExtension : class, ICodeTargetExtension
    {
        foreach (var extension in Extensions)
        {
            if (extension is TExtension)
            {
                return true;
            }
        }

        return false;
    }
}
