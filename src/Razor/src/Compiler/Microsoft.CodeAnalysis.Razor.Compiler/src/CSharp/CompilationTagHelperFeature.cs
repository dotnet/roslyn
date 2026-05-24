// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Razor;

public sealed class CompilationTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
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
