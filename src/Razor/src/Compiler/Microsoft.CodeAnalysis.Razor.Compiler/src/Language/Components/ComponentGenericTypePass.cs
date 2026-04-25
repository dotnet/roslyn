// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language.Components;

// This pass:
// 1. Adds diagnostics for missing generic type arguments
// 2. Rewrites the type name of the component to substitute generic type arguments
// 3. Rewrites the type names of parameters/child content to substitute generic type arguments
internal sealed class ComponentGenericTypePass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Runs after components/eventhandlers/ref/bind/templates. We want to validate every component
    // and it's usage of ChildContent.
    public override int Order => 160;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var visitor = new Visitor();
        visitor.Visit(documentNode);
    }

    internal sealed class Visitor : IntermediateNodeWalker
    {
        // Incrementing ID for type inference method names
        private int _id;

        public override void VisitComponent(ComponentIntermediateNode node)
        {
            if (node.Component.IsGenericTypedComponent())
            {
                // Not generic, ignore.
                Process(node);
            }

            base.VisitDefault(node);
        }

        private void Process(ComponentIntermediateNode node)
        {
            // First collect all of the information we have about each type parameter
            //
            // Listing all type parameters that exist
            var bindings = new Dictionary<string, Binding>();
            var componentTypeParameters = node.Component.GetTypeParameters().ToList();
            var supplyCascadingTypeParameters = componentTypeParameters
                .Where(p => p.IsCascadingTypeParameterProperty())
                .Select(p => p.Name)
                .ToList();
            foreach (var attribute in componentTypeParameters)
            {
                bindings.Add(attribute.Name, new Binding(attribute));
            }

            // Listing all type arguments that have been specified.
            var hasTypeArgumentSpecified = false;
            foreach (var typeArgumentNode in node.TypeArguments)
            {
                hasTypeArgumentSpecified = true;

                var binding = bindings[typeArgumentNode.TypeParameterName];
                binding.Node = typeArgumentNode;
                binding.Content = typeArgumentNode.Value;

                // Offer this explicit type argument to descendants too
                if (supplyCascadingTypeParameters.Contains(typeArgumentNode.TypeParameterName))
                {
                    node.ProvidesCascadingGenericTypes ??= new();
                    node.ProvidesCascadingGenericTypes[typeArgumentNode.TypeParameterName] = new CascadingGenericTypeParameter
                    {
                        GenericTypeNames = new[] { typeArgumentNode.TypeParameterName },
                        ValueType = typeArgumentNode.TypeParameterName,
                        ValueExpression = $"default({binding.Content.Content})",
                    };
                }
            }

            if (hasTypeArgumentSpecified)
            {
                // OK this means that the developer has specified at least one type parameter.
                // Either they specified everything and its OK to rewrite, or its an error.
                if (ValidateTypeArguments(node, bindings))
                {
                    var mappings = bindings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Node);
                    RewriteTypeNames(new GenericTypeNameRewriter(mappings), node, hasTypeArgumentSpecified);
                }

                return;
            }

            // OK if we get here that means that no type arguments were specified, so we will try to infer
            // the type.
            //
            // The actual inference is done by the C# compiler, we just emit an a method that represents the
            // use of this component.

            // Since we're generating code in a different namespace, we need to 'global qualify' all of the types
            // to avoid clashes with our generated code.
            RewriteTypeNames(new GlobalQualifiedTypeNameRewriter(bindings.Keys), node, hasTypeArgumentSpecified: false, bindings);

            //
            // We need to verify that an argument was provided that 'covers' each type parameter.
            //
            // For example, consider a repeater where the generic type is the 'item' type, but the developer has
            // not set the items. We won't be able to do type inference on this and so it will just be nonsense.
            foreach (var attribute in node.Attributes)
            {
                if (attribute != null && TryFindGenericTypeNames(attribute.BoundAttribute, attribute.GloballyQualifiedTypeName, out var typeParameters))
                {
                    // Keep only type parameters defined by this component.
                    typeParameters = typeParameters.Where(bindings.ContainsKey).ToArray();

                    var attributeValueIsLambda = SyntaxFactory.ParseExpression(GetContent(attribute)) is LambdaExpressionSyntax;
                    var provideCascadingGenericTypes = new CascadingGenericTypeParameter
                    {
                        GenericTypeNames = typeParameters,
                        ValueType = attribute.GloballyQualifiedTypeName,
                        ValueSourceNode = attribute,
                    };

                    foreach (var typeName in typeParameters)
                    {
                        if (supplyCascadingTypeParameters.Contains(typeName))
                        {
                            // Advertise that this particular inferred generic type is available to descendants.
                            // There might be multiple sources for each generic type, so pick the one that has the
                            // fewest other generic types on it. For example if we could infer from either List<T>
                            // or Dictionary<T, U>, we prefer List<T>.
                            node.ProvidesCascadingGenericTypes ??= new();
                            if (!node.ProvidesCascadingGenericTypes.TryGetValue(typeName, out var existingValue)
                                || existingValue.GenericTypeNames.Count > typeParameters.Count)
                            {
                                node.ProvidesCascadingGenericTypes[typeName] = provideCascadingGenericTypes;
                            }
                        }

                        if (attributeValueIsLambda)
                        {
                            // For attributes whose values are lambdas, we don't know whether or not the value
                            // covers the generic type - it depends on the content of the lambda.
                            // For example, "() => 123" can cover Func<T>, but "() => null" cannot. So we'll
                            // accept cascaded generic types from ancestors if they are compatible with the lambda,
                            // hence we don't remove it from the list of uncovered generic types until after
                            // we try matching against ancestor cascades.
                            if (bindings.TryGetValue(typeName, out var binding))
                            {
                                binding.CoveredByLambda = true;
                            }
                        }
                        else
                        {
                            bindings.Remove(typeName);
                        }
                    }
                }
            }

            // For any remaining bindings, scan up the hierarchy of ancestor components and try to match them
            // with a cascaded generic parameter that can cover this one
            List<CascadingGenericTypeParameter>? receivesCascadingGenericTypes = null;
            foreach (var uncoveredBindingKey in bindings.Keys.ToList())
            {
                foreach (var ancestor in Ancestors)
                {
                    if (ancestor is not ComponentIntermediateNode candidateAncestor)
                    {
                        continue;
                    }

                    if (candidateAncestor.ProvidesCascadingGenericTypes != null
                        && candidateAncestor.ProvidesCascadingGenericTypes.TryGetValue(uncoveredBindingKey, out var genericTypeProvider))
                    {
                        // If the parameter value is an expression that includes multiple generic types, we only want
                        // to use it if we want *all* those generic types. That is, a parameter of type MyType<T0, T1>
                        // can supply types to a Child<T0, T1>, but not to a Child<T0>.
                        // This is purely to avoid blowing up the complexity of the implementation here and could be
                        // overcome in the future if we want. We'd need to figure out which extra types are unwanted,
                        // and rewrite them to some unique name, and add that to the generic parameters list of the
                        // inference methods.
                        if (genericTypeProvider.GenericTypeNames.All(GenericTypeIsUsed))
                        {
                            bindings.Remove(uncoveredBindingKey);
                            receivesCascadingGenericTypes ??= new();
                            receivesCascadingGenericTypes.Add(genericTypeProvider);

                            // It's sufficient to identify the closest provider for each type parameter
                            break;
                        }

                        bool GenericTypeIsUsed(string typeName) => componentTypeParameters
                            .Select(t => t.Name)
                            .Contains(typeName, StringComparer.Ordinal);
                    }
                }
            }

            // There are two remaining sources of possible generic type info which we consider
            // lower-priority than cascades from ancestors. Since these two sources *may* actually
            // resolve generic type ambiguities in some cases, we treat them as covering.
            //
            // [1] Attributes given as lambda expressions. These are lower priority than ancestor
            //     cascades because in most cases, lambdas don't provide type info
            foreach (var entryToRemove in bindings.Where(e => e.Value.CoveredByLambda).ToList())
            {
                // Treat this binding as covered, because it's possible that the lambda does provide
                // enough info for type inference to succeed.
                bindings.Remove(entryToRemove.Key);
            }

            // [2] Child content parameters, which are nearly always defined as untyped lambdas
            //     (at least, that's what the Razor compiler produces), but can technically be
            //     hardcoded as a RenderFragment<Something> and hence actually give type info.
            foreach (var attribute in node.ChildContents)
            {
                if (TryFindGenericTypeNames(attribute.BoundAttribute, globallyQualifiedTypeName: null, out var typeParameters))
                {
                    foreach (var typeName in typeParameters)
                    {
                        bindings.Remove(typeName);
                    }
                }
            }

            // If any bindings remain then this means we would never be able to infer the arguments of this
            // component usage because the user hasn't set properties that include all of the types.
            if (bindings.Count > 0)
            {
                // However we still want to generate 'type inference' code because we want the errors to be as
                // helpful as possible. So let's substitute 'object' for all of those type parameters, and add
                // an error.
                var mappings = bindings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Node);
                RewriteTypeNames(new GenericTypeNameRewriter(mappings), node, bindings: bindings);

                node.AddDiagnostic(ComponentDiagnosticFactory.Create_GenericComponentTypeInferenceUnderspecified(node.Source, node, node.Component.GetTypeParameters()));
            }

            // Next we need to generate a type inference 'method' node. This represents a method that we will codegen that
            // contains all of the operations on the render tree building. Calling a method to operate on the builder
            // will allow the C# compiler to perform type inference.
            var documentNode = (DocumentIntermediateNode)Ancestors[^1];
            CreateTypeInferenceMethod(documentNode, node, receivesCascadingGenericTypes);
        }

        private bool TryFindGenericTypeNames(BoundAttributeDescriptor? boundAttribute, string? globallyQualifiedTypeName, [NotNullWhen(true)] out IReadOnlyList<string>? typeParameters)
        {
            if (boundAttribute == null)
            {
                // Will be null for attributes set on the component that don't match a declared component parameter
                typeParameters = null;
                return false;
            }

            if (!boundAttribute.IsGenericTypedProperty())
            {
                typeParameters = null;
                return false;
            }

            // Now we need to parse the type name and extract the generic parameters.
            // Two cases;
            // 1. name is a simple identifier like TItem
            // 2. name contains type parameters like Dictionary<string, TItem>
            typeParameters = ParseTypeParameters(globallyQualifiedTypeName ?? boundAttribute.TypeName);
            if (typeParameters.Count == 0)
            {
                typeParameters = new[] { boundAttribute.TypeName };
            }

            return true;
        }

        private static string GetContent(ComponentAttributeIntermediateNode node)
        {
            return string.Join(string.Empty, node.FindDescendantNodes<CSharpIntermediateToken>().Select(t => t.Content));
        }

        private static bool ValidateTypeArguments(ComponentIntermediateNode node, Dictionary<string, Binding> bindings)
        {
            var missing = new List<BoundAttributeDescriptor>();
            foreach (var (_, binding) in bindings)
            {
                if (binding.Node == null || string.IsNullOrWhiteSpace(binding.Content?.Content))
                {
                    missing.Add(binding.Attribute);
                }
            }

            if (missing.Count > 0)
            {
                // We add our own error for this because its likely the user will see other errors due
                // to incorrect codegen without the types. Our errors message will pretty clearly indicate
                // what to do, whereas the other errors might be confusing.
                node.AddDiagnostic(ComponentDiagnosticFactory.Create_GenericComponentMissingTypeArgument(node.Source, node, missing));
                return false;
            }

            return true;
        }

        private void RewriteTypeNames(TypeNameRewriter rewriter, ComponentIntermediateNode node, bool? hasTypeArgumentSpecified = null, IDictionary<string, Binding>? bindings = null)
        {
            // Rewrite the component type name
            rewriter.RewriteComponentTypeName(node);

            foreach (var attribute in node.Attributes)
            {
                attribute.ConcreteContainingType = node.TypeName;

                var globallyQualifiedTypeName = attribute.BoundAttribute?.GetGloballyQualifiedTypeName();

                if (attribute.TypeName != null)
                {
                    globallyQualifiedTypeName = rewriter.Rewrite(globallyQualifiedTypeName ?? attribute.TypeName);
                    attribute.GloballyQualifiedTypeName = globallyQualifiedTypeName;
                }

                if (attribute.BoundAttribute?.IsGenericTypedProperty() == true && attribute.TypeName != null)
                {
                    // If we know the type name, then replace any generic type parameter inside it with
                    // the known types.
                    attribute.TypeName = globallyQualifiedTypeName;
                    // This is a special case in which we are dealing with a property TItem.
                    // Given that TItem can have been defined explicitly by the user to a partially
                    // qualified type, (like MyType), we check if the globally qualified type name
                    // contains "global::" which will be the case in all cases as we've computed
                    // this information from the Roslyn symbol except for when the symbol is a generic
                    // type parameter. In which case, we mark it with an additional annotation to
                    // account for that during code generation and avoid trying to fully qualify
                    // the type name.
                    Debug.Assert(bindings != null || hasTypeArgumentSpecified == true);
                    if (hasTypeArgumentSpecified == true)
                    {
                        attribute.HasExplicitTypeName = true;
                    }
                    else if (attribute.BoundAttribute?.IsEventCallbackProperty() ?? false)
                    {
                        Debug.Assert(attribute.TypeName is not null);
                        var typeParameters = ParseTypeParameters(attribute.TypeName);
                        for (int i = 0; i < typeParameters.Count; i++)
                        {
                            var parameter = typeParameters[i];
                            if (bindings!.ContainsKey(parameter))
                            {
                                attribute.IsOpenGeneric = true;
                                break;
                            }
                        }
                    }
                }
                else if (attribute.TypeName == null && (attribute.BoundAttribute?.IsDelegateProperty() ?? false))
                {
                    // This is a weakly typed delegate, treat it as Action<object>
                    attribute.TypeName = "global::System.Action<global::System.Object>";
                }
                else if (attribute.TypeName == null && (attribute.BoundAttribute?.IsEventCallbackProperty() ?? false))
                {
                    // This is a weakly typed event-callback, treat it as EventCallback (non-generic)
                    attribute.TypeName = $"global::{ComponentsApi.EventCallback.FullTypeName}";
                }
                else if (attribute.TypeName == null)
                {
                    // This is a weakly typed attribute, treat it as System.Object
                    attribute.TypeName = "global::System.Object";
                }
            }

            foreach (var capture in node.Captures)
            {
                if (capture.IsComponentCapture)
                {
                    capture.UpdateComponentCaptureTypeName(rewriter.Rewrite(capture.ComponentCaptureTypeName));
                }
            }

            foreach (var childContent in node.ChildContents)
            {
                if (childContent.BoundAttribute?.IsGenericTypedProperty() == true && childContent.TypeName != null)
                {
                    // If we know the type name, then replace any generic type parameter inside it with
                    // the known types.
                    childContent.TypeName = rewriter.Rewrite(childContent.TypeName);
                }
                else if (childContent.IsParameterized)
                {
                    // This is a non-generic parameterized child content like RenderFragment<int>, leave it as is.
                }
                else
                {
                    // This is a weakly typed child content, treat it as RenderFragment
                    childContent.TypeName = ComponentsApi.RenderFragment.FullTypeName;
                }
            }
        }

        private void CreateTypeInferenceMethod(DocumentIntermediateNode documentNode, ComponentIntermediateNode node, List<CascadingGenericTypeParameter>? receivesCascadingGenericTypes)
        {
            var @namespace = documentNode.FindPrimaryNamespace().AssumeNotNull().Name;
            @namespace = string.IsNullOrEmpty(@namespace) ? "__Blazor" : "__Blazor." + @namespace;
            @namespace += "." + documentNode.FindPrimaryClass().AssumeNotNull().Name;

            using var genericTypeConstraints = new PooledArrayBuilder<string>();

            foreach (var attribute in node.Component.BoundAttributes)
            {
                if (attribute.Metadata is TypeParameterMetadata { Constraints: string constraints })
                {
                    genericTypeConstraints.Add(constraints);
                }
            }

            var typeInferenceNode = new ComponentTypeInferenceMethodIntermediateNode()
            {
                Component = node,

                // Method name is generated and guaranteed not to collide, since it's unique for each
                // component call site.
                MethodName = $"Create{CSharpIdentifier.SanitizeIdentifier(node.TagName.AsSpanOrDefault())}_{_id++}",
                FullTypeName = @namespace + ".TypeInference",

                ReceivesCascadingGenericTypes = receivesCascadingGenericTypes,
                GenericTypeConstraints = genericTypeConstraints.ToArrayAndClear()
            };

            node.TypeInferenceNode = typeInferenceNode;

            // Now we need to insert the type inference node into the tree.
            var namespaceNode = documentNode.Children
                .OfType<NamespaceDeclarationIntermediateNode>()
                .FirstOrDefault(n => n.IsGenericTyped);
            if (namespaceNode == null)
            {
                namespaceNode = new NamespaceDeclarationIntermediateNode()
                {
                    Name = @namespace,
                    IsGenericTyped = true,
                };

                documentNode.Children.Add(namespaceNode);
            }

            var classNode = namespaceNode.Children
                .OfType<ClassDeclarationIntermediateNode>()
                .FirstOrDefault(n => n.Name == "TypeInference");
            if (classNode == null)
            {
                classNode = new ClassDeclarationIntermediateNode()
                {
                    Name = "TypeInference",
                    Modifiers = CommonModifiers.InternalStatic
                };
                namespaceNode.Children.Add(classNode);
            }

            classNode.Children.Add(typeInferenceNode);
        }

        public IReadOnlyList<string> ParseTypeParameters(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            var parsed = SyntaxFactory.ParseTypeName(typeName);

            if (parsed is IdentifierNameSyntax identifier)
            {
                return Array.Empty<string>();
            }

            if (TryParseCore(parsed) is { IsDefault: false } list)
            {
                return list;
            }

            return parsed.DescendantNodesAndSelf()
                .OfType<TypeArgumentListSyntax>()
                .SelectMany(arg => arg.Arguments)
                .SelectMany(t => ParseCore(t)).ToArray();

            static ImmutableArray<string> TryParseCore(TypeSyntax parsed)
            {
                if (parsed is ArrayTypeSyntax array)
                {
                    return ParseCore(array.ElementType);
                }

                if (parsed is TupleTypeSyntax tuple)
                {
                    return tuple.Elements.SelectManyAsArray(a => ParseCore(a.Type));
                }

                return default;
            }

            static ImmutableArray<string> ParseCore(TypeSyntax parsed)
            {
                // Recursively drill into arrays `T[]` and tuples `(T, T)`.
                if (TryParseCore(parsed) is { IsDefault: false } list)
                {
                    return list;
                }

                // When we encounter an identifier, we assume it's a type parameter.
                if (parsed is IdentifierNameSyntax identifier)
                {
                    return ImmutableArray.Create(identifier.Identifier.Text);
                }

                // Generic names like `C<T>` are ignored here because we will visit their type argument list
                // via the `.DescendantNodesAndSelf().OfType<TypeArgumentListSyntax>()` call above.
                return ImmutableArray<string>.Empty;
            }
        }
    }



    private class Binding
    {
        public Binding(BoundAttributeDescriptor attribute) => Attribute = attribute;

        public BoundAttributeDescriptor Attribute { get; }

        public IntermediateToken? Content { get; set; }

        public ComponentTypeArgumentIntermediateNode? Node { get; set; }

        public bool CoveredByLambda { get; set; }
    }
}
