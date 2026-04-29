// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal sealed class DefaultCodeTarget(
    RazorCodeDocument codeDocument,
    ImmutableArray<ICodeTargetExtension> extensions)
    : CodeTarget(codeDocument, extensions)
{
    public override IntermediateNodeWriter CreateNodeWriter()
        => Options.DesignTime
            ? DesignTimeNodeWriter.Instance
            : RuntimeNodeWriter.Instance;
}
