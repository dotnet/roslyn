// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentCodeTarget(
    RazorCodeDocument codeDocument,
    ImmutableArray<ICodeTargetExtension> targetExtensions)
    : CodeTarget(codeDocument, GetComponentTargetExtensions(targetExtensions))
{
    private RazorLanguageVersion Version => CodeDocument.ParserOptions.LanguageVersion;

    private static ImmutableArray<ICodeTargetExtension> GetComponentTargetExtensions(ImmutableArray<ICodeTargetExtension> targetExtensions)
    {
        // Components provide some built-in target extensions that don't apply to legacy documents.
        return [new ComponentTemplateTargetExtension(), .. targetExtensions];
    }

    public override IntermediateNodeWriter CreateNodeWriter()
        => Options.DesignTime
            ? new ComponentDesignTimeNodeWriter(Version)
            : new ComponentRuntimeNodeWriter(Version);
}
