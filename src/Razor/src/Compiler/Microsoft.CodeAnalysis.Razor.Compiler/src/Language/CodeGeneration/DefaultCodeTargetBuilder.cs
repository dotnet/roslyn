// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal sealed class DefaultCodeTargetBuilder(RazorCodeDocument codeDocument) : CodeTargetBuilder(codeDocument)
{
    public override CodeTarget Build()
        => new DefaultCodeTarget(CodeDocument, TargetExtensions.ToImmutable());
}
