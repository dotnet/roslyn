// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentDocumentClassifierPass : DocumentClassifierPassBase
{
    public const string ComponentDocumentKind = "component.1.0";

    /// <summary>
    /// The fallback value of the root namespace. Only used if the fallback root namespace
    /// was not passed in.
    /// </summary>
    public string FallbackRootNamespace { get; set; } = "__GeneratedComponent";

    /// <summary>
    /// Gets or sets whether to mangle class names.
    ///
    /// Set to true in the IDE so we can generated mangled class names. This is needed
    /// to avoid conflicts between generated design-time code and the code in the editor.
    ///
    /// A better workaround for this would be to create a singlefilegenerator that overrides
    /// the codegen process when a document is open, but this is more involved, so hacking
    /// it for now.
    /// </summary>
    public bool MangleClassNames { get; set; }

    protected override string DocumentKind => ComponentDocumentKind;

    // Ensure this runs before the MVC classifiers which have Order = 0
    public override int Order => -100;

    protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        return codeDocument.FileKind.IsComponent();
    }

    protected override CodeTarget CreateTarget(RazorCodeDocument codeDocument)
        => new ComponentCodeTarget(codeDocument, TargetExtensions);

    /// <inheritdoc />
    protected override void OnDocumentStructureCreated(
        RazorCodeDocument codeDocument,
        NamespaceDeclarationIntermediateNode @namespace,
        ClassDeclarationIntermediateNode @class,
        MethodDeclarationIntermediateNode method)
    {
        if (!codeDocument.TryGetNamespace(fallbackToRootNamespace: true, out var computedNamespace, out var computedNamespaceSpan))
        {
            computedNamespace = FallbackRootNamespace;
        }

        if (!TryComputeClassName(codeDocument, out var computedClass))
        {
            var checksum = ChecksumUtilities.BytesToString(codeDocument.Source.Text.GetChecksum());
            computedClass = $"AspNetCore_{checksum}";
        }

        var documentNode = codeDocument.GetRequiredDocumentNode();
        if (char.IsLower(computedClass, 0))
        {
            // We don't allow component names to start with a lowercase character.
            documentNode.AddDiagnostic(
                ComponentDiagnosticFactory.Create_ComponentNamesCannotStartWithLowerCase(computedClass, documentNode.Source));
        }

        if (MangleClassNames)
        {
            computedClass = ComponentHelpers.MangleClassName(computedClass);
        }

        @class.NullableContext = true;

        @namespace.Name = computedNamespace;
        @namespace.Source = computedNamespaceSpan;
        @class.Name = computedClass;
        @class.Modifiers = CommonModifiers.PublicPartial;

        if (codeDocument.FileKind.IsComponentImport())
        {
            // We don't want component imports to be considered as real component.
            // But we still want to generate code for it so we can get diagnostics.
            @class.BaseType = new BaseTypeWithModel("object");

            method.ReturnType = "void";
            method.Name = "Execute";
            method.Modifiers = CommonModifiers.Protected;
            method.Parameters = [];
        }
        else
        {
            @class.BaseType = new BaseTypeWithModel("global::" + ComponentsApi.ComponentBase.FullTypeName);

            // Constrained type parameters are only supported in Razor language versions v6.0
            var razorLanguageVersion = codeDocument.ParserOptions.LanguageVersion;
            var directiveType = razorLanguageVersion >= RazorLanguageVersion.Version_6_0
                ? ComponentConstrainedTypeParamDirective.Directive
                : ComponentTypeParamDirective.Directive;

            using var typeParameters = new PooledArrayBuilder<TypeParameter>();

            foreach (var typeParamReference in documentNode.FindDirectiveReferences(directiveType))
            {
                var typeParamNode = typeParamReference.Node;
                if (typeParamNode.HasDiagnostics)
                {
                    continue;
                }

                // The first token is the type parameter's name, the rest are its constraints, if any.
                var name = typeParamNode.Tokens.First();
                var constraints = typeParamNode.Tokens.Skip(1).FirstOrDefault();

                typeParameters.Add(new(name.Content, name.Source, constraints?.Content, constraints?.Source));
            }

            @class.TypeParameters = typeParameters.ToImmutableAndClear();

            method.ReturnType = "void";
            method.Name = ComponentsApi.ComponentBase.BuildRenderTree;
            method.Modifiers = CommonModifiers.ProtectedOverride;

            method.Parameters = [new(
                name: ComponentsApi.RenderTreeBuilder.BuilderParameter,
                type: $"global::{ComponentsApi.RenderTreeBuilder.FullTypeName}")];
        }
    }

    private static bool TryComputeClassName(RazorCodeDocument codeDocument, [NotNullWhen(true)] out string? className)
    {
        className = null;
        if (codeDocument.Source.FilePath == null || codeDocument.Source.RelativePath == null)
        {
            return false;
        }

        var relativePath = NormalizePath(codeDocument.Source.RelativePath);
        className = CSharpIdentifier.SanitizeIdentifier(Path.GetFileNameWithoutExtension(relativePath).AsSpanOrDefault());
        return true;
    }

    private static string NormalizePath(string path)
    {
        path = path.Replace('\\', '/');

        return path;
    }
}
