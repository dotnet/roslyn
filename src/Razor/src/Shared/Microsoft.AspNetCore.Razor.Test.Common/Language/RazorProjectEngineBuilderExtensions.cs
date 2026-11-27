// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorProjectEngineBuilderExtensions
{
    /// <summary>
    /// Adds the provided <see cref="RazorProjectItem" />s as imports to all project items processed
    /// by the <see cref="RazorProjectEngine"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="imports">The collection of imports.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder AddDefaultImports(this RazorProjectEngineBuilder builder, params string[] imports)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Features.Add(new AdditionalImportsProjectFeature(imports));

        return builder;
    }

    public static RazorProjectEngineBuilder SetTagHelpers(this RazorProjectEngineBuilder builder, params TagHelperCollection tagHelpers)
    {
        var feature = (TestTagHelperFeature)builder.Features.OfType<ITagHelperFeature>().FirstOrDefault();
        if (feature == null)
        {
            feature = new TestTagHelperFeature();
            builder.Features.Add(feature);
        }

        feature.SetTagHelpers(tagHelpers);
        return builder;
    }

    public static RazorProjectEngineBuilder ConfigureDocumentClassifier(this RazorProjectEngineBuilder builder, string testFileName)
    {
        var feature = builder.Features.OfType<DefaultDocumentClassifierPassFeature>().FirstOrDefault();
        if (feature == null)
        {
            feature = new DefaultDocumentClassifierPassFeature();
            builder.Features.Add(feature);
        }

        feature.ConfigureNamespace.Clear();
        feature.ConfigureClass.Clear();
        feature.ConfigureMethod.Clear();

        feature.ConfigureNamespace.Add((RazorCodeDocument codeDocument, NamespaceDeclarationIntermediateNode node) =>
        {
            node.Name = "Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles";
        });

        feature.ConfigureClass.Add((RazorCodeDocument codeDocument, ClassDeclarationIntermediateNode node) =>
        {
            node.Name = testFileName.Replace('/', '_');
            node.Modifiers = ["public"];
        });

        feature.ConfigureMethod.Add((RazorCodeDocument codeDocument, MethodDeclarationIntermediateNode node) =>
        {
            node.Modifiers = ["public", "async"];
            node.Name = "ExecuteAsync";
            node.ReturnType = typeof(Task).FullName;
        });

        return builder;
    }

    internal static void SetImportFeature(this RazorProjectEngineBuilder builder, IImportProjectFeature feature)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(feature);

        // Remove any existing import features in favor of the new one we're given.

        var existingFeatures = builder.Features.OfType<IImportProjectFeature>().ToArray();
        foreach (var existingFeature in existingFeatures)
        {
            builder.Features.Remove(existingFeature);
        }

        builder.Features.Add(feature);
    }

    private sealed class AdditionalImportsProjectFeature(string[] imports) : RazorProjectEngineFeatureBase, IImportProjectFeature
    {
        private readonly ImmutableArray<RazorProjectItem> _imports = imports.SelectAsArray(
            static import => (RazorProjectItem)new DefaultImportProjectItem("Additional default imports", import));

        public void CollectImports(RazorProjectItem projectItem, ref PooledArrayBuilder<RazorProjectItem> imports)
        {
            imports.AddRange(_imports);
        }
    }
}
