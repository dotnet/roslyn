// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class ComponentTagHelperProducer : TagHelperProducer
{
    private readonly BindTagHelperProducer? _bindTagHelperProducer;

    private ComponentTagHelperProducer(BindTagHelperProducer? bindTagHelperProducer)
    {
        _bindTagHelperProducer = bindTagHelperProducer;
    }

    public override TagHelperProducerKind Kind => TagHelperProducerKind.Component;

    public override bool SupportsTypes => true;

    public override bool IsCandidateType(INamedTypeSymbol type)
        => ComponentDetectionConventions.IsComponent(type, ComponentsApi.IComponent.MetadataName);

    public override void AddTagHelpersForType(
        INamedTypeSymbol type,
        ref TagHelperCollection.RefBuilder results,
        CancellationToken cancellationToken)
    {
        // Components have very simple matching rules.
        // 1. The type name (short) matches the tag name.
        // 2. The fully qualified name matches the tag name.

        // First, compute the relevant properties for this type so that we
        // don't need to compute them twice.
        var properties = GetProperties(type);

        var shortNameMatchingDescriptor = CreateShortNameMatchingDescriptor(type, properties);
        results.Add(shortNameMatchingDescriptor);

        // If the component is in the global namespace, skip adding this descriptor which will be the same as the short name one.
        TagHelperDescriptor? fullyQualifiedNameMatchingDescriptor = null;
        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            fullyQualifiedNameMatchingDescriptor = CreateFullyQualifiedNameMatchingDescriptor(type, properties);
            results.Add(fullyQualifiedNameMatchingDescriptor);
        }

        // Produce bind tag helpers for the component.
        if (_bindTagHelperProducer is { SupportsTypes: true })
        {
            _bindTagHelperProducer.AddTagHelpersForComponent(shortNameMatchingDescriptor, ref results);

            if (fullyQualifiedNameMatchingDescriptor is not null)
            {
                _bindTagHelperProducer.AddTagHelpersForComponent(fullyQualifiedNameMatchingDescriptor, ref results);
            }
        }

        foreach (var childContent in shortNameMatchingDescriptor.GetChildContentProperties())
        {
            // Synthesize a separate tag helper for each child content property that's declared.
            results.Add(CreateChildContentDescriptor(shortNameMatchingDescriptor, childContent));
            if (fullyQualifiedNameMatchingDescriptor is not null)
            {
                results.Add(CreateChildContentDescriptor(fullyQualifiedNameMatchingDescriptor, childContent));
            }
        }
    }

    private static TagHelperDescriptor CreateShortNameMatchingDescriptor(
        INamedTypeSymbol type,
        ImmutableArray<(IPropertySymbol property, PropertyKind kind)> properties)
        => CreateNameMatchingDescriptor(type, properties, fullyQualified: false);

    private static TagHelperDescriptor CreateFullyQualifiedNameMatchingDescriptor(
        INamedTypeSymbol type,
        ImmutableArray<(IPropertySymbol property, PropertyKind kind)> properties)
        => CreateNameMatchingDescriptor(type, properties, fullyQualified: true);

    private static TagHelperDescriptor CreateNameMatchingDescriptor(
        INamedTypeSymbol type,
        ImmutableArray<(IPropertySymbol property, PropertyKind kind)> properties,
        bool fullyQualified)
    {
        var typeName = TypeNameObject.From(type);
        var assemblyName = type.ContainingAssembly.Identity.Name;

        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            TagHelperKind.Component, typeName.FullName.AssumeNotNull(), assemblyName, out var builder);

        builder.RuntimeKind = RuntimeKind.IComponent;
        builder.SetTypeName(typeName);

        var metadata = new ComponentMetadata.Builder();

        builder.CaseSensitive = true;

        if (fullyQualified)
        {
            var fullName = type.ContainingNamespace.IsGlobalNamespace
                ? type.Name
                : $"{type.ContainingNamespace.GetFullName()}.{type.Name}";

            builder.TagMatchingRule(r =>
            {
                r.TagName = fullName;
            });

            builder.IsFullyQualifiedNameMatch = true;
        }
        else
        {
            builder.TagMatchingRule(r =>
            {
                r.TagName = type.Name;
            });
        }

        if (type.IsGenericType)
        {
            metadata.IsGeneric = true;

            using var cascadeGenericTypeAttributes = new PooledHashSet<string>(StringComparer.Ordinal);

            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.HasFullName(ComponentsApi.CascadingTypeParameterAttribute.MetadataName) &&
                    attribute.ConstructorArguments.FirstOrDefault() is { Value: string value })
                {
                    cascadeGenericTypeAttributes.Add(value);
                }
            }

            foreach (var typeArgument in type.TypeArguments)
            {
                if (typeArgument is ITypeParameterSymbol typeParameter)
                {
                    var cascade = cascadeGenericTypeAttributes.Contains(typeParameter.Name);
                    CreateTypeParameterProperty(builder, typeParameter, cascade);
                }
            }
        }

        if (HasRenderModeDirective(type))
        {
            metadata.HasRenderModeDirective = true;
        }

        var xml = type.GetDocumentationCommentXml();
        if (!string.IsNullOrEmpty(xml))
        {
            builder.SetDocumentation(xml);
        }

        foreach (var (property, kind) in properties)
        {
            if (kind == PropertyKind.Ignored)
            {
                continue;
            }

            CreateProperty(builder, type, property, kind);
        }

        if (builder.BoundAttributes.Any(static a => a.IsParameterizedChildContentProperty()) &&
            !builder.BoundAttributes.Any(static a => string.Equals(a.Name, ComponentHelpers.ChildContent.ParameterAttributeName, StringComparison.OrdinalIgnoreCase)))
        {
            // If we have any parameterized child content parameters, synthesize a 'Context' parameter to be
            // able to set the variable name (for all child content). If the developer defined a 'Context' parameter
            // already, then theirs wins.
            CreateContextParameter(builder, childContentName: null);
        }

        builder.SetMetadata(metadata.Build());

        return builder.Build();
    }

    private static void CreateProperty(TagHelperDescriptorBuilder builder, INamedTypeSymbol containingSymbol, IPropertySymbol property, PropertyKind kind)
    {
        builder.BindAttribute(pb =>
        {
            var builder = new PropertyMetadata.Builder();

            pb.Name = property.Name;
            pb.ContainingType = containingSymbol.GetFullName();
            pb.TypeName = property.Type.GetFullName();
            pb.PropertyName = property.Name;
            pb.IsEditorRequired = property.GetAttributes().Any(
                static a => a.HasFullName("Microsoft.AspNetCore.Components.EditorRequiredAttribute"));

            pb.CaseSensitive = false;

            builder.GloballyQualifiedTypeName = property.Type.GetGloballyQualifiedFullName();

            if (kind == PropertyKind.Enum)
            {
                pb.IsEnum = true;
            }
            else if (kind == PropertyKind.ChildContent)
            {
                builder.IsChildContent = true;
            }
            else if (kind == PropertyKind.EventCallback)
            {
                builder.IsEventCallback = true;
            }
            else if (kind == PropertyKind.Delegate)
            {
                builder.IsDelegateSignature = true;
                builder.IsDelegateWithAwaitableResult = IsAwaitable(property);
            }

            if (HasTypeParameter(property.Type))
            {
                builder.IsGenericTyped = true;
            }

            if (property.SetMethod.AssumeNotNull().IsInitOnly)
            {
                builder.IsInitOnlyProperty = true;
            }

            pb.SetMetadata(builder.Build());

            var xml = property.GetDocumentationCommentXml();
            if (!string.IsNullOrEmpty(xml))
            {
                pb.SetDocumentation(xml);
            }
        });

        static bool HasTypeParameter(ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol)
            {
                return true;
            }

            // We need to check for cases like:
            // [Parameter] public List<T> MyProperty { get; set; }
            // AND
            // [Parameter] public List<string> MyProperty { get; set; }
            //
            // We need to inspect the type arguments to tell the difference between a property that
            // uses the containing class' type parameter(s) and a vanilla usage of generic types like
            // List<> and Dictionary<,>
            //
            // Since we need to handle cases like RenderFragment<List<T>>, this check must be recursive.
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                foreach (var typeArgument in namedType.TypeArguments)
                {
                    if (HasTypeParameter(typeArgument))
                    {
                        return true;
                    }
                }

                // Another case to handle - if the type being inspected is a nested type
                // inside a generic containing class. The common usage for this would be a case
                // where a generic templated component defines a 'context' nested class.
                if (namedType.ContainingType != null && HasTypeParameter(namedType.ContainingType))
                {
                    return true;
                }
            }
            // Also check for cases like:
            // [Parameter] public T[] MyProperty { get; set; }
            else if (type is IArrayTypeSymbol array && HasTypeParameter(array.ElementType))
            {
                return true;
            }

            return false;
        }
    }

    private static bool IsAwaitable(IPropertySymbol prop)
    {
        var methodSymbol = ((INamedTypeSymbol)prop.Type).DelegateInvokeMethod.AssumeNotNull();
        if (methodSymbol.ReturnsVoid)
        {
            return false;
        }

        var members = methodSymbol.ReturnType.GetMembers();
        foreach (var candidate in members)
        {
            if (candidate is not IMethodSymbol method || !string.Equals(candidate.Name, "GetAwaiter", StringComparison.Ordinal))
            {
                continue;
            }

            if (!VerifyGetAwaiter(method))
            {
                continue;
            }

            return true;
        }

        return methodSymbol.IsAsync;

        static bool VerifyGetAwaiter(IMethodSymbol getAwaiter)
        {
            var returnType = getAwaiter.ReturnType;
            if (returnType == null)
            {
                return false;
            }

            var foundIsCompleted = false;
            var foundOnCompleted = false;
            var foundGetResult = false;

            foreach (var member in returnType.GetMembers())
            {
                if (!foundIsCompleted &&
                    member is IPropertySymbol property &&
                    IsProperty_IsCompleted(property))
                {
                    foundIsCompleted = true;
                }

                if (!(foundOnCompleted && foundGetResult) && member is IMethodSymbol method)
                {
                    if (IsMethod_OnCompleted(method))
                    {
                        foundOnCompleted = true;
                    }
                    else if (IsMethod_GetResult(method))
                    {
                        foundGetResult = true;
                    }
                }

                if (foundIsCompleted && foundOnCompleted && foundGetResult)
                {
                    return true;
                }
            }

            return false;

            static bool IsProperty_IsCompleted(IPropertySymbol property)
            {
                return property is
                {
                    Name: WellKnownMemberNames.IsCompleted,
                    Type.SpecialType: SpecialType.System_Boolean,
                    GetMethod: not null
                };
            }

            static bool IsMethod_OnCompleted(IMethodSymbol method)
            {
                return method is
                {
                    Name: WellKnownMemberNames.OnCompleted,
                    ReturnsVoid: true,
                    Parameters: [{ Type.TypeKind: TypeKind.Delegate }]
                };
            }

            static bool IsMethod_GetResult(IMethodSymbol method)
            {
                return method is
                {
                    Name: WellKnownMemberNames.GetResult,
                    Parameters: []
                };
            }
        }
    }

    private static void CreateTypeParameterProperty(TagHelperDescriptorBuilder builder, ITypeParameterSymbol typeParameter, bool cascade)
    {
        builder.BindAttribute(pb =>
        {
            pb.DisplayName = typeParameter.Name;
            pb.Name = typeParameter.Name;
            pb.TypeName = typeof(Type).FullName;
            pb.PropertyName = typeParameter.Name;

            var metadata = new TypeParameterMetadata.Builder
            {
                IsCascading = cascade
            };

            // Type constraints (like "Image" or "Foo") are stored independently of
            // things like constructor constraints and not null constraints in the
            // type parameter so we create a single string representation of all the constraints
            // here.
            using var constraints = new PooledList<string>();

            // CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints
            // cannot be combined or duplicated, and must be specified first in the constraints list.
            if (typeParameter.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }

            if (typeParameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (typeParameter.HasValueTypeConstraint)
            {
                // `HasValueTypeConstraint` is also true when `unmanaged` constraint is present.
                constraints.Add("struct");
            }

            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                constraints.Add(constraintType.GetGloballyQualifiedFullName());
            }

            // CS0401: The new() constraint must be the last constraint specified.
            if (typeParameter.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (TryGetWhereClauseText(typeParameter, constraints, out var whereClauseText))
            {
                metadata.Constraints = whereClauseText;
            }

            // Collect attributes that should be propagated to the type inference method.
            using var _ = StringBuilderPool.GetPooledObject(out var withAttributes);
            foreach (var attribute in typeParameter.GetAttributes())
            {
                if (attribute.HasFullName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"))
                {
                    Debug.Assert(attribute.AttributeClass != null);

                    if (withAttributes.Length > 0)
                    {
                        withAttributes.Append(", ");
                    }
                    else
                    {
                        withAttributes.Append('[');
                    }

                    withAttributes.Append(attribute.AttributeClass.GetGloballyQualifiedFullName());
                    withAttributes.Append('(');

                    var first = true;
                    foreach (var arg in attribute.ConstructorArguments)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            withAttributes.Append(", ");
                        }

                        if (arg.Kind == TypedConstantKind.Enum)
                        {
                            withAttributes.Append("unchecked((");
                            withAttributes.Append(arg.Type!.GetGloballyQualifiedFullName());
                            withAttributes.Append(')');
                            withAttributes.Append(SymbolDisplay.FormatPrimitive(arg.Value!, quoteStrings: true, useHexadecimalNumbers: true));
                            withAttributes.Append(')');
                        }
                        else
                        {
                            Debug.Assert(false, $"Need to add support for '{arg.Kind}' and make sure the output is 'global::' prefixed.");
                            withAttributes.Append(arg.ToCSharpString());
                        }
                    }

                    withAttributes.Append(')');
                }
            }

            if (withAttributes.Length > 0)
            {
                withAttributes.Append("] ");
                withAttributes.Append(typeParameter.Name);
                metadata.NameWithAttributes = withAttributes.ToString();
            }

            pb.SetMetadata(metadata.Build());

            pb.SetDocumentation(
                DocumentationDescriptor.From(
                    DocumentationId.ComponentTypeParameter,
                    typeParameter.Name,
                    builder.Name));
        });

        static bool TryGetWhereClauseText(ITypeParameterSymbol typeParameter, PooledList<string> constraints, [NotNullWhen(true)] out string? constraintsText)
        {
            if (constraints.Count == 0)
            {
                constraintsText = null;
                return false;
            }

            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            builder.Append("where ");
            builder.Append(typeParameter.Name);
            builder.Append(" : ");

            var addComma = false;

            foreach (var item in constraints)
            {
                if (addComma)
                {
                    builder.Append(", ");
                }
                else
                {
                    addComma = true;
                }

                builder.Append(item);
            }

            constraintsText = builder.ToString();
            return true;
        }
    }

    private static TagHelperDescriptor CreateChildContentDescriptor(TagHelperDescriptor component, BoundAttributeDescriptor attribute)
    {
        var typeName = component.TypeName + "." + attribute.Name;
        var assemblyName = component.AssemblyName;

        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            TagHelperKind.ChildContent, typeName, assemblyName,
            out var builder);

        builder.SetTypeName(typeName, component.TypeNamespace, component.TypeNameIdentifier);

        builder.CaseSensitive = true;

        var xml = attribute.Documentation;
        if (!string.IsNullOrEmpty(xml))
        {
            builder.SetDocumentation(xml);
        }

        // Child content matches the property name, but only as a direct child of the component.
        builder.TagMatchingRule(r =>
        {
            r.TagName = attribute.Name;
            r.ParentTag = component.TagMatchingRules[0].TagName;
        });

        if (attribute.IsParameterizedChildContentProperty())
        {
            // For child content attributes with a parameter, synthesize an attribute that allows you to name
            // the parameter.
            CreateContextParameter(builder, attribute.Name);
        }

        if (component.IsFullyQualifiedNameMatch)
        {
            builder.IsFullyQualifiedNameMatch = true;
        }

        var descriptor = builder.Build();

        return descriptor;
    }

    private static void CreateContextParameter(TagHelperDescriptorBuilder builder, string? childContentName)
    {
        builder.BindAttribute(b =>
        {
            b.Name = ComponentHelpers.ChildContent.ParameterAttributeName;
            b.TypeName = typeof(string).FullName;
            b.PropertyName = b.Name;
            b.SetMetadata(ChildContentParameterMetadata.Default);

            var documentation = childContentName == null
                ? DocumentationDescriptor.ChildContentParameterName_TopLevel
                : DocumentationDescriptor.From(DocumentationId.ChildContentParameterName, childContentName);

            b.SetDocumentation(documentation);
        });
    }

    // Does a walk up the inheritance chain to determine the set of parameters by using
    // a dictionary keyed on property name.
    //
    // We consider parameters to be defined by properties satisfying all of the following:
    // - are public
    // - are visible (not shadowed)
    // - have the [Parameter] attribute
    // - have a setter, even if private
    // - are not indexers
    private static ImmutableArray<(IPropertySymbol property, PropertyKind kind)> GetProperties(INamedTypeSymbol type)
    {
        using var names = new PooledHashSet<string>(StringComparer.Ordinal);
        using var results = new PooledArrayBuilder<(IPropertySymbol, PropertyKind)>();

        var currentType = type;
        do
        {
            if (currentType.HasFullName(ComponentsApi.ComponentBase.MetadataName))
            {
                // The ComponentBase base class doesn't have any [Parameter].
                // Bail out now to avoid walking through its many members, plus the members
                // of the System.Object base class.
                break;
            }

            foreach (var member in currentType.GetMembers())
            {
                if (member is not IPropertySymbol property)
                {
                    // Not a property
                    continue;
                }

                if (names.Contains(property.Name))
                {
                    // Not visible
                    continue;
                }

                var kind = PropertyKind.Default;
                if (property.DeclaredAccessibility != Accessibility.Public // Not public
                    || property.Parameters.Length != 0 // Indexer
                    || property.SetMethod == null // No setter
                    || property.SetMethod.DeclaredAccessibility != Accessibility.Public // No public setter
                    || property.IsStatic)
                {
                    // For non-override properties, skip the expensive GetAttributes() call
                    // since the property will be ignored regardless. GetAttributes() triggers
                    // attribute binding and nullable analysis in the compiler, which is a
                    // significant cost during tag helper discovery. Override properties still
                    // need the check because [Parameter] determines whether to shadow or pass
                    // through to the base class.
                    if (!property.IsOverride)
                    {
                        names.Add(property.Name);
                        results.Add((property, PropertyKind.Ignored));
                        continue;
                    }

                    kind = PropertyKind.Ignored;
                }

                if (!property.GetAttributes().Any(static a => a.HasFullName(ComponentsApi.ParameterAttribute.MetadataName)))
                {
                    if (property.IsOverride)
                    {
                        // This property does not contain [Parameter] attribute but it was overridden. Don't ignore it for now.
                        // We can ignore it if the base class does not contains a [Parameter] as well.
                        continue;
                    }

                    // Does not have [Parameter]
                    kind = PropertyKind.Ignored;
                }

                if (kind == PropertyKind.Default)
                {
                    kind = property switch
                    {
                        var p when IsEnum(p) => PropertyKind.Enum,
                        var p when IsRenderFragment(p) => PropertyKind.ChildContent,
                        var p when IsEventCallback(p) => PropertyKind.EventCallback,
                        var p when IsDelegate(p) => PropertyKind.Delegate,
                        _ => PropertyKind.Default
                    };
                }

                names.Add(property.Name);
                results.Add((property, kind));
            }

            currentType = currentType.BaseType;
        }
        while (currentType != null);

        return results.ToImmutableAndClear();

        static bool IsEnum(IPropertySymbol property)
        {
            return property.Type.TypeKind == TypeKind.Enum;
        }

        static bool IsRenderFragment(IPropertySymbol property)
        {
            return property.Type.HasFullName(ComponentsApi.RenderFragment.MetadataName) ||
                  (property.Type is INamedTypeSymbol { IsGenericType: true } namedType &&
                   namedType.ConstructedFrom.HasFullName(ComponentsApi.RenderFragmentOfT.DisplayName));
        }

        static bool IsEventCallback(IPropertySymbol property)
        {
            return property.Type.HasFullName(ComponentsApi.EventCallback.MetadataName) ||
                  (property.Type is INamedTypeSymbol { IsGenericType: true } namedType &&
                   namedType.ConstructedFrom.HasFullName(ComponentsApi.EventCallbackOfT.DisplayName));
        }

        static bool IsDelegate(IPropertySymbol property)
        {
            return property.Type.TypeKind == TypeKind.Delegate;
        }
    }

    private static bool HasRenderModeDirective(INamedTypeSymbol type)
    {
        var attributes = type.GetAttributes();
        foreach (var attribute in attributes)
        {
            var attributeClass = attribute.AttributeClass;
            while (attributeClass is not null)
            {
                if (attributeClass.HasFullName(ComponentsApi.RenderModeAttribute.FullTypeName))
                {
                    return true;
                }

                attributeClass = attributeClass.BaseType;
            }
        }
        return false;
    }

    private enum PropertyKind
    {
        Ignored,
        Default,
        Enum,
        ChildContent,
        Delegate,
        EventCallback,
    }
}
