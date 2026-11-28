// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

namespace Microsoft.CodeAnalysis.Razor;

public sealed class CompilationTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature, IComponentAliasTargetFeature
{
    private ITagHelperDiscoveryService? _discoveryService;
    private IMetadataReferenceFeature? _referenceFeature;

    public TagHelperCollection GetTagHelpers(CancellationToken cancellationToken = default)
    {
        var compilation = CSharpCompilation.Create("__TagHelpers", references: _referenceFeature?.References);
        if (!IsValidCompilation(compilation))
        {
            return [];
        }

        Assumed.NotNull(_discoveryService);

        return _discoveryService.GetTagHelpers(compilation, cancellationToken);
    }

    ComponentAliasTargetMap? IComponentAliasTargetFeature.GetAliasTargets(RazorCodeDocument document, CancellationToken cancellationToken)
    {
        var aliases = ComponentAliasTargetMap.GetAliasInfos(document);
        if (aliases.IsEmpty)
        {
            return ComponentAliasTargetMap.Empty;
        }

        // Compilation-backed project-engine callers need the same complete lookup as the generator.
        // Keep this input on the document, not on the shared engine or its descriptor caches.
        var compilation = CSharpCompilation.Create("__Aliases", references: _referenceFeature?.References);
        if (!IsValidCompilation(compilation))
        {
            // Hosts can supply descriptors without references. Missing compilation information must
            // preserve that fallback, rather than authoritatively rejecting every supplied alias.
            return null;
        }

        return ComponentAliasTargetMap.Create([aliases], compilation, cancellationToken);
    }

    protected override void OnInitialized()
    {
        _referenceFeature = Engine.GetFeatures<IMetadataReferenceFeature>().FirstOrDefault();
        _discoveryService = GetRequiredFeature<ITagHelperDiscoveryService>();
    }

    internal static bool IsValidCompilation(Compilation compilation)
    {
        var @string = compilation.GetSpecialType(SpecialType.System_String);

        // Do some minimal tests to verify the compilation is valid. If symbols for System.String
        // is missing or errored, the compilation may be missing references.
        return @string != null && @string.TypeKind != TypeKind.Error;
    }
}
