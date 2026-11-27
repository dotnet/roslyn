// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class ConfigureDirectivesFeature : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature
{
    private readonly Dictionary<RazorFileKind, ImmutableArray<DirectiveDescriptor>.Builder> _fileKindToDirectivesMap = [];

    public void AddDirective(DirectiveDescriptor directive, params ReadOnlySpan<RazorFileKind> fileKinds)
    {
        lock (_fileKindToDirectivesMap)
        {
            // To maintain backwards compatibility, FileKinds.Legacy is assumed when a file kind is not specified.
            if (fileKinds.IsEmpty)
            {
                fileKinds = [RazorFileKind.Legacy];
            }

            foreach (var fileKind in fileKinds)
            {
                var directives = _fileKindToDirectivesMap.GetOrAdd(fileKind, _ => ImmutableArray.CreateBuilder<DirectiveDescriptor>());
                directives.Add(directive);
            }
        }
    }

    public ImmutableArray<DirectiveDescriptor> GetDirectives(RazorFileKind? fileKind = null)
    {
        // To maintain backwards compatibility, FileKinds.Legacy is assumed when a file kind is not specified.
        var fileKindValue = fileKind ?? RazorFileKind.Legacy;

        lock (_fileKindToDirectivesMap)
        {
            return _fileKindToDirectivesMap.TryGetValue(fileKindValue, out var directives)
                ? directives.ToImmutable()
                : [];
        }
    }

    public int Order => 100;

    void IConfigureRazorParserOptionsFeature.Configure(RazorParserOptions.Builder builder)
    {
        builder.Directives = GetDirectives(builder.FileKind);
    }
}
