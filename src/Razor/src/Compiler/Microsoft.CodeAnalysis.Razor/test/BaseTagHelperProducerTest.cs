// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public abstract class TagHelperDescriptorProviderTestBase
{
    protected TagHelperDescriptorProviderTestBase(string? additionalCode = null)
    {
        CSharpParseOptions = new CSharpParseOptions(LanguageVersion.CSharp7_3);

        var testTagHelpers = CSharpCompilation.Create(
            assemblyName: AssemblyName,
            syntaxTrees:
            [
                Parse(TagHelperDescriptorFactoryTagHelpers.Code),
                .. additionalCode != null ? [Parse(additionalCode)] : Array.Empty<SyntaxTree>(),
            ],
            references: ReferenceUtil.AspNetLatestAll,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        BaseCompilation = TestCompilation.Create(
            syntaxTrees: [],
            references: [testTagHelpers.VerifyDiagnostics().EmitToImageReference()]);

        var projectEngine = RazorProjectEngine.CreateEmpty(builder =>
        {
            builder.Features.Add(new TagHelperDiscoveryService());

            ConfigureEngine(builder);
        });

        Engine = projectEngine.Engine;
    }

    protected RazorEngine Engine { get; }

    protected Compilation BaseCompilation { get; }

    protected CSharpParseOptions CSharpParseOptions { get; }

    protected static string AssemblyName { get; } = "Microsoft.CodeAnalysis.Razor.Test";

    protected virtual void ConfigureEngine(RazorProjectEngineBuilder builder)
    {
    }

    private protected TagHelperCollection GetTagHelpers(Compilation compilation, TagHelperDiscoveryOptions options)
        => GetDiscoveryService().GetTagHelpers(compilation, options);

    private protected TagHelperCollection GetTagHelpers(Compilation compilation)
        => GetDiscoveryService().GetTagHelpers(compilation);

    private protected bool TryGetDiscoverer(
        Compilation compilation, TagHelperDiscoveryOptions options, [NotNullWhen(true)] out TagHelperDiscoverer? discoverer)
        => GetDiscoveryService().TryGetDiscoverer(compilation, options, out discoverer);

    private protected bool TryGetDiscoverer(
        Compilation compilation, [NotNullWhen(true)] out TagHelperDiscoverer? discoverer)
        => GetDiscoveryService().TryGetDiscoverer(compilation, out discoverer);

    private protected ITagHelperDiscoveryService GetDiscoveryService()
    {
        Assert.True(Engine.TryGetFeature(out ITagHelperDiscoveryService? discoveryService));
        return discoveryService;
    }

    protected CSharpSyntaxTree Parse(string text)
    {
        return (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(text, CSharpParseOptions);
    }

    protected static bool IsBuiltInComponent(TagHelperDescriptor tagHelper)
        => tagHelper.DisplayName.StartsWith("Microsoft.AspNetCore.Components.", StringComparison.Ordinal);

    protected static TagHelperDescriptor[] AssertAndExcludeFullyQualifiedNameMatchComponents(
        TagHelperDescriptor[] components,
        int expectedCount)
    {
        var fullyQualifiedNameMatchComponents = components.Where(c => c.IsFullyQualifiedNameMatch).ToArray();
        Assert.Equal(expectedCount, fullyQualifiedNameMatchComponents.Length);

        var shortNameMatchComponents = components.Where(c => !c.IsFullyQualifiedNameMatch).ToArray();

        // For every fully qualified name component, we want to make sure we have a corresponding short name component.
        foreach (var fullNameComponent in fullyQualifiedNameMatchComponents)
        {
            Assert.Contains(shortNameMatchComponents, component =>
            {
                return component.Name == fullNameComponent.Name &&
                    component.Kind == fullNameComponent.Kind &&
                    component.BoundAttributes.SequenceEqual(fullNameComponent.BoundAttributes);
            });
        }

        return shortNameMatchComponents;
    }

    protected static TagHelperCollection AssertAndExcludeFullyQualifiedNameMatchComponents(
        TagHelperCollection collection,
        int expectedCount)
    {
        var fullyQualifiedNameMatchComponents = collection.Where(c => c.IsFullyQualifiedNameMatch);
        Assert.Equal(expectedCount, fullyQualifiedNameMatchComponents.Count);

        var shortNameMatchComponents = collection.Where(c => !c.IsFullyQualifiedNameMatch);

        // For every fully qualified name component, we want to make sure we have a corresponding short name component.
        foreach (var fullNameComponent in fullyQualifiedNameMatchComponents)
        {
            Assert.Contains(shortNameMatchComponents, component =>
            {
                return component.Name == fullNameComponent.Name &&
                    component.Kind == fullNameComponent.Kind &&
                    component.BoundAttributes.SequenceEqual(fullNameComponent.BoundAttributes);
            });
        }

        return shortNameMatchComponents;
    }
}
