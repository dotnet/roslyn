// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public abstract class CodeTargetBuilder(RazorCodeDocument codeDocument)
{
    private ImmutableArray<ICodeTargetExtension>.Builder? _targetExtensions;

    public RazorCodeDocument CodeDocument => codeDocument;
    public RazorCodeGenerationOptions Options => codeDocument.CodeGenerationOptions;

    public ImmutableArray<ICodeTargetExtension>.Builder TargetExtensions
        => _targetExtensions ??= ImmutableArray.CreateBuilder<ICodeTargetExtension>();

    public abstract CodeTarget Build();
}
