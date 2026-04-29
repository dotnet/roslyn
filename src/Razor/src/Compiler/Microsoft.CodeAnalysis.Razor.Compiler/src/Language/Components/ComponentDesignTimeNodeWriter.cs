// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

// Based on the DesignTimeNodeWriter from Razor repo.
internal class ComponentDesignTimeNodeWriter : ComponentNodeWriter
{
    private const string DesignTimeVariable = "__o";

    public ComponentDesignTimeNodeWriter(RazorLanguageVersion version) : base(version)
    {
    }

    // Avoid using `AddComponentParameter` in design time where we currently don't detect its availability.
    protected override bool CanUseAddComponentParameter(CodeRenderingContext context) => false;

    public override void WriteMarkupBlock(CodeRenderingContext context, MarkupBlockIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // Do nothing
    }

    public override void WriteMarkupElement(CodeRenderingContext context, MarkupElementIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        context.RenderChildren(node);
    }

    public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.Source is { FilePath: not null } sourceSpan)
        {
            using (context.BuildLinePragma(sourceSpan, suppressLineDefaultAndHidden: !node.AppendLineDefaultAndHidden))
            {
                context.AddSourceMappingFor(node);
                context.CodeWriter.WriteUsing(node.Content);
            }
        }
        else
        {
            context.CodeWriter.WriteUsing(node.Content);

            if (node.AppendLineDefaultAndHidden)
            {
                context.CodeWriter.WriteLine("#line default");
                context.CodeWriter.WriteLine("#line hidden");
            }
        }
    }

    public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        WriteCSharpExpressionInnards(context, node);
    }

    private void WriteCSharpExpressionInnards(CodeRenderingContext context, CSharpExpressionIntermediateNode node, string? type = null)
    {
        if (node.Children.Count == 0)
        {
            return;
        }

        if (node.Source != null)
        {
            using (context.BuildLinePragma(node.Source.Value))
            {
                var offset = DesignTimeVariable.Length + " = ".Length;

                if (type != null)
                {
                    offset += type.Length + 2; // two parenthesis
                }

                context.CodeWriter.WritePadding(offset, node.Source, context);
                context.CodeWriter.WriteStartAssignment(DesignTimeVariable);

                if (type != null)
                {
                    context.CodeWriter.Write("(");
                    TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, type);
                    context.CodeWriter.Write(")");
                }

                foreach (var child in node.Children)
                {
                    if (child is CSharpIntermediateToken token)
                    {
                        context.AddSourceMappingFor(token);
                        context.CodeWriter.Write(token.Content);
                    }
                    else
                    {
                        // There may be something else inside the expression like a Template or another extension node.
                        context.RenderNode(child);
                    }
                }

                context.CodeWriter.WriteLine(";");
            }
        }
        else
        {
            context.CodeWriter.WriteStartAssignment(DesignTimeVariable);

            foreach (var child in node.Children)
            {
                if (child is CSharpIntermediateToken token)
                {
                    context.CodeWriter.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    context.RenderNode(child);
                }
            }

            context.CodeWriter.WriteLine(";");
        }
    }

    public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
    {
        var isWhiteSpace = true;

        foreach (var child in node.Children)
        {
            if (child is not CSharpIntermediateToken token || !token.Content.IsNullOrWhiteSpace())
            {
                isWhiteSpace = false;
                break;
            }
        }

        // Don't write whitespace if there is no line mapping for it.
        if (isWhiteSpace && node.Source is null)
        {
            return;
        }

        var writer = context.CodeWriter;

        if (node.Source is SourceSpan nodeSource && !isWhiteSpace)
        {
            using (context.BuildLinePragma(nodeSource))
            {
                writer.WritePadding(0, nodeSource, context);
                RenderCSharpCode(context, node);
            }
        }
        else
        {
            writer.WritePadding(0, node.Source, context);

            RenderCSharpCode(context, node);
            writer.WriteLine();
        }
    }

    public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // This expression may contain code so we have to render it or else the design-time
        // exprience is broken.
        if (node.AttributeNameExpression is CSharpExpressionIntermediateNode expression)
        {
            WriteCSharpExpressionInnards(context, expression, "string");
        }

        context.RenderChildren(node);
    }

    public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // Do nothing, this can't contain code.
    }

    public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.Children.Count == 0)
        {
            return;
        }

        context.CodeWriter.WriteStartAssignment(DesignTimeVariable);

        foreach (var child in node.Children)
        {
            if (child is CSharpIntermediateToken token)
            {
                WriteCSharpToken(context, token);
            }
            else
            {
                // There may be something else inside the expression like a Template or another extension node.
                context.RenderNode(child);
            }
        }

        context.CodeWriter.WriteLine(";");
    }

    public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // Do nothing
    }

    protected override void BeginWriteAttribute(CodeRenderingContext context, string key)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{nameof(ComponentsApi.RenderTreeBuilder.AddAttribute)}")
            .Write("-1")
            .WriteParameterSeparator()
            .WriteStringLiteral(key);
    }

    protected override void BeginWriteAttribute(CodeRenderingContext context, IntermediateNode expression)
    {
        context.CodeWriter.WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddAttribute}");
        context.CodeWriter.Write("-1");
        context.CodeWriter.WriteParameterSeparator();

        WriteCSharpTokens(context, GetCSharpTokens(expression));
    }

    public override void WriteComponent(CodeRenderingContext context, ComponentIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // We might need a scope for inferring types,
        CodeWriterExtensions.CSharpCodeWritingScope? typeInferenceCaptureScope = null;
        string? typeInferenceLocalName = null;

        var suppressTypeInference = ShouldSuppressTypeInferenceCall(node);
        if (suppressTypeInference)
        {
        }
        else if (node.TypeInferenceNode == null)
        {
            // Writes something like:
            //
            // __builder.OpenComponent<MyComponent>(0);
            // __builder.AddAttribute(1, "Foo", ...);
            // __builder.AddAttribute(2, "ChildContent", ...);
            // __builder.SetKey(someValue);
            // __builder.AddElementCapture(3, (__value) => _field = __value);
            // __builder.CloseComponent();

            foreach (var typeArgument in node.TypeArguments)
            {
                context.RenderNode(typeArgument);
            }

            // We need to preserve order for attributes and attribute splats since the ordering
            // has a semantic effect.

            foreach (var child in node.Children)
            {
                if (child is ComponentAttributeIntermediateNode attribute)
                {
                    context.RenderNode(attribute);
                }
                else if (child is SplatIntermediateNode splat)
                {
                    context.RenderNode(splat);
                }
                else if (child is RenderModeIntermediateNode renderMode)
                {
                    context.RenderNode(renderMode);
                }
            }

            if (node.ChildContents.Any())
            {
                foreach (var childContent in node.ChildContents)
                {
                    context.RenderNode(childContent);
                }
            }
            else
            {
                // We eliminate 'empty' child content when building the tree so that usage like
                // '<MyComponent>\r\n</MyComponent>' doesn't create a child content.
                //
                // Consider what would happen if the user's cursor was inside the element. At
                // design -time we want to render an empty lambda to provide proper scoping
                // for any code that the user types.
                context.RenderNode(new ComponentChildContentIntermediateNode()
                {
                    TypeName = ComponentsApi.RenderFragment.FullTypeName,
                });
            }

            foreach (var setKey in node.SetKeys)
            {
                context.RenderNode(setKey);
            }

            foreach (var capture in node.Captures)
            {
                context.RenderNode(capture);
            }
        }
        else
        {
            var parameters = GetTypeInferenceMethodParameters(node.TypeInferenceNode);

            // If this component is going to cascade any of its generic types, we have to split its type inference
            // into two parts. First we call an inference method that captures all the parameters in local variables,
            // then we use those to call the real type inference method that emits the component. The reason for this
            // is so the captured variables can be used by descendants without re-evaluating the expressions.
            if (node.Component.SuppliesCascadingGenericParameters())
            {
                typeInferenceCaptureScope = context.CodeWriter.BuildScope();
                context.CodeWriter.Write("global::");
                context.CodeWriter.Write(node.TypeInferenceNode.FullTypeName);
                context.CodeWriter.Write(".");
                context.CodeWriter.Write(node.TypeInferenceNode.MethodName);
                context.CodeWriter.Write("_CaptureParameters(");
                var isFirst = true;
                foreach (var parameter in parameters.Where(p => p.UsedForTypeInference))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        context.CodeWriter.Write(", ");
                    }

                    WriteTypeInferenceMethodParameterInnards(context, parameter);
                    context.CodeWriter.Write(", out var ");

                    var variableName = new TypeInferenceArgName(ScopeStack.Depth, parameter.ParameterName);
                    context.CodeWriter.Write(variableName);

                    UseCapturedCascadingGenericParameterVariable(node, parameter, variableName);
                }
                context.CodeWriter.WriteLine(");");
            }

            // When we're doing type inference, we can't write all of the code inline to initialize
            // the component on the builder. We generate a method elsewhere, and then pass all of the information
            // to that method. We pass in all of the attribute values + the sequence numbers.
            //
            // __Blazor.MyComponent.TypeInference.CreateMyComponent_0(__builder, 0, 1, ..., 2, ..., 3, ....);

            // We don't need an instance of this component, but having its type information is useful later for allowing
            // Roslyn to bind to properties that represent component attributes.
            // It's a bit silly that this variable will be called __typeInference_CreateMyComponent_0 with "Create" in the
            // name, but since we've already done the work to create a unique name, we should reuse it.

            typeInferenceLocalName = $"__typeInference_{node.TypeInferenceNode.MethodName}";

            context.CodeWriter.Write("var ");
            context.CodeWriter.Write(typeInferenceLocalName);
            context.CodeWriter.Write(" = ");

            context.CodeWriter.Write("global::");
            context.CodeWriter.Write(node.TypeInferenceNode.FullTypeName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(node.TypeInferenceNode.MethodName);
            context.CodeWriter.Write("(");

            context.CodeWriter.Write(BuilderVariableName);
            context.CodeWriter.Write(", ");

            context.CodeWriter.Write("-1");

            foreach (var parameter in parameters)
            {
                context.CodeWriter.Write(", ");

                if (parameter.SeqName != null)
                {
                    context.CodeWriter.Write("-1");
                    context.CodeWriter.Write(", ");
                }

                WriteTypeInferenceMethodParameterInnards(context, parameter);
            }

            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();
        }

        // We need to write property access here in case we're in a scope for capturing types, because we need to re-use
        // the type inference local for accessing property names.
        // We also need to disable BL0005, which is an analyzer provided by the runtime that will warn if a component
        // parameter is explicitly set, but that's exactly what we will be doing in order to represent the attribute
        // being set.

        if (!suppressTypeInference)
        {
            var wrotePragmaDisable = false;
            foreach (var child in node.Children)
            {
                if (child is ComponentAttributeIntermediateNode attribute)
                {
                    WritePropertyAccess(context, attribute, node, typeInferenceLocalName, shouldWriteBL0005Disable: !wrotePragmaDisable, out var wrotePropertyAccess);

                    if (wrotePropertyAccess)
                    {
                        wrotePragmaDisable = true;
                    }
                }
            }

            if (wrotePragmaDisable)
            {
                // Restore the warning in case the user has written other code that explicitly sets a property
                context.CodeWriter.WriteLine("#pragma warning restore BL0005");
            }
        }

        typeInferenceCaptureScope?.Dispose();

        // We want to generate something that references the Component type to avoid
        // the "usings directive is unnecessary" message.
        // Looks like:
        // __o = typeof(SomeNamespace.SomeComponent);
        using (context.BuildLinePragma(node.Source.AssumeNotNull()))
        {
            context.CodeWriter.Write(DesignTimeVariable);
            context.CodeWriter.Write(" = ");
            context.CodeWriter.Write("typeof(");
            context.CodeWriter.Write("global::");
            if (!node.Component.IsGenericTypedComponent())
            {
                context.CodeWriter.Write(node.Component.Name);
            }
            else
            {
                // The tags can be unqualified or fully qualified, the TagName always equals
                // the class name so we rely on that to compute the globally fully qualified
                // type name
                if (!node.TagName.Contains("."))
                {
                    // The tag is not fully qualified
                    context.CodeWriter.Write(node.Component.TypeNamespace.AssumeNotNull());
                    context.CodeWriter.Write(".");
                }
                context.CodeWriter.Write(node.TagName);
                context.CodeWriter.Write("<");
                var typeArgumentCount = node.Component.GetTypeParameters().Count();
                for (var i = 1; i < typeArgumentCount; i++)
                {
                    context.CodeWriter.Write(",");
                }
                context.CodeWriter.Write(">");
            }
            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();
        }
    }

    public override void WriteComponentTypeInferenceMethod(CodeRenderingContext context, ComponentTypeInferenceMethodIntermediateNode node)
    {
        base.WriteComponentTypeInferenceMethod(context, node, returnComponentType: true, allowNameof: false, mapComponentStartTag: false);
    }

    private void WriteTypeInferenceMethodParameterInnards(CodeRenderingContext context, TypeInferenceMethodParameter parameter)
    {
        switch (parameter.Source)
        {
            case ComponentAttributeIntermediateNode attribute:
                // Don't type check generics, since we can't actually write the type name.
                // The type checking with happen anyway since we defined a method and we're generating
                // a call to it.
                WriteComponentAttributeInnards(context, attribute, canTypeCheck: false);
                break;
            case SplatIntermediateNode splat:
                WriteSplatInnards(context, splat, canTypeCheck: false);
                break;
            case ComponentChildContentIntermediateNode childNode:
                WriteComponentChildContentInnards(context, childNode);
                break;
            case SetKeyIntermediateNode setKey:
                WriteSetKeyInnards(context, setKey);
                break;
            case ReferenceCaptureIntermediateNode capture:
                WriteReferenceCaptureInnards(context, capture, shouldTypeCheck: false);
                break;
            case CascadingGenericTypeParameter syntheticArg:
                // The value should be populated before we use it, because we emit code for creating ancestors
                // first, and that's where it's populated. However if this goes wrong somehow, we don't want to
                // throw, so use a fallback
                if (syntheticArg.ValueExpression is IWriteableValue writeableValue)
                {
                    writeableValue.WriteTo(context.CodeWriter);
                }
                else
                {
                    var valueExpression = syntheticArg.ValueExpression as string ?? "default";
                    context.CodeWriter.Write(valueExpression);

                    if (!context.Options.SuppressNullabilityEnforcement && IsDefaultExpression(valueExpression))
                    {
                        context.CodeWriter.Write("!");
                    }
                }

                break;

            case TypeInferenceCapturedVariable capturedVariable:
                context.CodeWriter.Write(capturedVariable.VariableName);
                break;
            case RenderModeIntermediateNode renderMode:
                WriteCSharpCode(context, new CSharpCodeIntermediateNode() { Source = renderMode.Source, Children = { renderMode.Children[0] } });
                break;
            default:
                throw new InvalidOperationException($"Not implemented: type inference method parameter from source {parameter.Source}");
        }
    }

    public override void WriteComponentAttribute(CodeRenderingContext context, ComponentAttributeIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // This attribute might only be here in order to allow us to generate code in WritePropertyAccess
        if (node.IsDesignTimePropertyAccessHelper)
        {
            return;
        }

        // Looks like:
        // __o = 17;
        context.CodeWriter.Write(DesignTimeVariable);
        context.CodeWriter.Write(" = ");

        // Following the same design pattern as the runtime codegen
        WriteComponentAttributeInnards(context, node, canTypeCheck: true);

        context.CodeWriter.Write(";");
        context.CodeWriter.WriteLine();
    }

    private void WritePropertyAccess(CodeRenderingContext context, ComponentAttributeIntermediateNode node, ComponentIntermediateNode componentNode, string? typeInferenceLocalName, bool shouldWriteBL0005Disable, out bool wrotePropertyAccess)
    {
        wrotePropertyAccess = false;
        if (node?.TagHelper?.Name is null || node.OriginalAttributeSpan is null)
        {
            return;
        }

        if (node.BoundAttribute.Metadata is PropertyMetadata { IsInitOnlyProperty: true })
        {
            // If a component property is init only then the code we generate for it won't compile.
            return;
        }

        // Write the name of the property, for rename support.
        // __o = ((global::ComponentName)default).PropertyName;
        var originalAttributeName = node.OriginalAttributeName ?? node.AttributeName;

        int offset;
        if (originalAttributeName == node.PropertyName)
        {
            offset = 0;
        }
        else if (originalAttributeName.StartsWith($"@bind-{node.PropertyName}", StringComparison.Ordinal))
        {
            offset = 5;
        }
        else
        {
            return;
        }

        if (shouldWriteBL0005Disable)
        {
            context.CodeWriter.WriteLine("#pragma warning disable BL0005");
        }

        var attributeSourceSpan = (SourceSpan)node.OriginalAttributeSpan;
        attributeSourceSpan = new SourceSpan(attributeSourceSpan.FilePath, attributeSourceSpan.AbsoluteIndex + offset, attributeSourceSpan.LineIndex, attributeSourceSpan.CharacterIndex + offset, node.PropertyName.Length, attributeSourceSpan.LineCount, attributeSourceSpan.CharacterIndex + offset + node.PropertyName.Length);

        if (componentNode.TypeInferenceNode == null)
        {
            context.CodeWriter.Write("((");
            TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, componentNode.TypeName);
            context.CodeWriter.Write(")default)");
        }
        else
        {
            if (typeInferenceLocalName is null)
            {
                throw new InvalidOperationException("No type inference local name was supplied, but type inference is required to reference a component type.");
            }

            // Earlier when we did the type inference stuff, we captured a variable which the compiler would know the type information
            // for explicitly for the purposes of using it now
            context.CodeWriter.Write(typeInferenceLocalName);
        }

        context.CodeWriter.Write(".");
        context.CodeWriter.WriteLine();

        using (context.BuildLinePragma(attributeSourceSpan))
        {
            context.CodeWriter.WritePadding(0, attributeSourceSpan, context);
            context.CodeWriter.WriteIdentifierEscapeIfNeeded(node.PropertyName);
            context.AddSourceMappingFor(attributeSourceSpan);
            context.CodeWriter.WriteLine(node.PropertyName);
        }

        context.CodeWriter.Write(" = default;");
        context.CodeWriter.WriteLine();

        wrotePropertyAccess = true;
    }

    private void WriteComponentAttributeInnards(CodeRenderingContext context, ComponentAttributeIntermediateNode node, bool canTypeCheck)
    {
        if (node.Children.Count > 1)
        {
            Debug.Assert(node.HasDiagnostics, "We should have reported an error for mixed content.");
            // We render the children anyway, so tooling works.
        }

        // We limit component attributes to simple cases. However there is still a lot of complexity
        // to handle here, since there are a few different cases for how an attribute might be structured.
        //
        // This roughly follows the design of the runtime writer for simplicity.
        if (node.AttributeStructure == AttributeStructure.Minimized)
        {
            // Minimized attributes always map to 'true'
            context.CodeWriter.Write("true");
        }
        else if (node.Children.Count == 1 && node.Children[0] is HtmlContentIntermediateNode)
        {
            // We don't actually need the content at designtime, an empty string will do.
            context.CodeWriter.Write("\"\"");
        }
        else
        {
            // There are a few different forms that could be used to contain all of the tokens, but we don't really care
            // exactly what it looks like - we just want all of the content.
            //
            // This can include an empty list in some cases like the following (sic):
            //      <MyComponent Value="
            //
            // Or a CSharpExpressionIntermediateNode when the attribute has an explicit transition like:
            //      <MyComponent Value="@value" />
            //
            // Of a list of tokens directly in the attribute.
            var tokens = GetCSharpTokens(node);

            if ((node.BoundAttribute?.IsDelegateProperty() ?? false) ||
                (node.BoundAttribute?.IsChildContentProperty() ?? false))
            {
                // We always surround the expression with the delegate constructor. This makes type
                // inference inside lambdas, and method group conversion do the right thing.
                if (canTypeCheck)
                {
                    context.CodeWriter.Write("new ");
                    WriteGloballyQualifiedTypeName(context, node);
                    context.CodeWriter.Write("(");
                }
                context.CodeWriter.WriteLine();

                WriteCSharpTokens(context, tokens);

                if (canTypeCheck)
                {
                    context.CodeWriter.Write(")");
                }
            }
            else if (node.BoundAttribute?.IsEventCallbackProperty() ?? false)
            {
                // This is the case where we are writing an EventCallback (a delegate with super-powers).
                //
                // An event callback can either be passed verbatim, or it can be created by the EventCallbackFactory.
                // Since we don't look at the code the user typed inside the attribute value, this is always
                // resolved via overloading.
                var explicitType = node.HasExplicitTypeName;
                var isInferred = node.IsOpenGeneric;
                if (canTypeCheck && NeedsTypeCheck(node))
                {
                    context.CodeWriter.Write(ComponentsApi.RuntimeHelpers.TypeCheck);
                    context.CodeWriter.Write("<");
                    QualifyEventCallback(context.CodeWriter, node.TypeName, explicitType);
                    context.CodeWriter.Write(">");
                    context.CodeWriter.Write("(");
                }

                // Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, ...) OR
                // Microsoft.AspNetCore.Components.EventCallback.Factory.Create<T>(this, ...)

                context.CodeWriter.Write("global::");
                context.CodeWriter.Write(ComponentsApi.EventCallback.FactoryAccessor);
                context.CodeWriter.Write(".");
                context.CodeWriter.Write(ComponentsApi.EventCallbackFactory.CreateMethod);

                if (isInferred != true && node.TryParseEventCallbackTypeArgument(out ReadOnlyMemory<char> argument))
                {
                    context.CodeWriter.Write("<");
                    if (explicitType)
                    {
                        context.CodeWriter.Write(argument);
                    }
                    else
                    {
                        TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, argument);
                    }

                    context.CodeWriter.Write(">");
                }

                context.CodeWriter.Write("(");
                context.CodeWriter.Write("this");
                context.CodeWriter.Write(", ");

                context.CodeWriter.WriteLine();

                WriteCSharpTokens(context, tokens);

                context.CodeWriter.Write(")");

                if (canTypeCheck && NeedsTypeCheck(node))
                {
                    context.CodeWriter.Write(")");
                }
            }
            else
            {
                // This is the case when an attribute contains C# code
                //
                // If we have a parameter type, then add a type check.
                if (canTypeCheck && NeedsTypeCheck(node))
                {
                    context.CodeWriter.Write(ComponentsApi.RuntimeHelpers.TypeCheck);
                    context.CodeWriter.Write("<");
                    WriteGloballyQualifiedTypeName(context, node);
                    context.CodeWriter.Write(">");
                    context.CodeWriter.Write("(");
                }

                WriteCSharpTokens(context, tokens);

                if (canTypeCheck && NeedsTypeCheck(node))
                {
                    context.CodeWriter.Write(")");
                }
            }

            static void QualifyEventCallback(CodeWriter codeWriter, string typeName, bool? explicitType)
            {
                if (ComponentAttributeIntermediateNode.TryGetEventCallbackArgument(typeName.AsMemory(), out var argument))
                {
                    codeWriter.Write("global::");
                    codeWriter.Write(ComponentsApi.EventCallback.FullTypeName);
                    codeWriter.Write("<");
                    if (explicitType == true)
                    {
                        codeWriter.Write(argument);
                    }
                    else
                    {
                        TypeNameHelper.WriteGloballyQualifiedName(codeWriter, argument);
                    }
                    codeWriter.Write(">");
                }
                else
                {
                    TypeNameHelper.WriteGloballyQualifiedName(codeWriter, typeName);
                }
            }
        }

        static bool NeedsTypeCheck(ComponentAttributeIntermediateNode n)
        {
            // Weakly typed attributes will have their TypeName set to null.
            return n.BoundAttribute != null && n.TypeName != null;
        }
    }

    private static ImmutableArray<CSharpIntermediateToken> GetCSharpTokens(IntermediateNode node)
    {
        // We generally expect all children to be CSharp, this is here just in case.
        return node.FindDescendantNodes<CSharpIntermediateToken>();
    }

    public override void WriteComponentChildContent(CodeRenderingContext context, ComponentChildContentIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // Writes something like:
        //
        // __builder.AddAttribute(1, "ChildContent", (RenderFragment)((__builder73) => { ... }));
        // OR
        // __builder.AddAttribute(1, "ChildContent", (RenderFragment<Person>)((person) => (__builder73) => { ... }));
        BeginWriteAttribute(context, node.AttributeName);
        context.CodeWriter.WriteParameterSeparator();
        context.CodeWriter.Write("(");
        WriteGloballyQualifiedTypeName(context, node);
        context.CodeWriter.Write(")(");

        WriteComponentChildContentInnards(context, node);

        context.CodeWriter.Write(")");
        context.CodeWriter.WriteEndMethodInvocation();
    }

    private void WriteComponentChildContentInnards(CodeRenderingContext context, ComponentChildContentIntermediateNode node)
    {
        // Writes something like:
        //
        // ((__builder73) => { ... })
        // OR
        // ((person) => (__builder73) => { })
        var parameterName = node.IsParameterized ? node.ParameterName : null;

        using (ScopeStack.OpenComponentScope(context,  parameterName))
        {
            foreach (var child in node.Children)
            {
                context.RenderNode(child);
            }
        }
    }

    public override void WriteComponentTypeArgument(CodeRenderingContext context, ComponentTypeArgumentIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // At design type we want write the equivalent of:
        //
        // __o = typeof(TItem);
        context.CodeWriter.Write(DesignTimeVariable);
        context.CodeWriter.Write(" = ");
        context.CodeWriter.Write("typeof(");

        WriteCSharpTokens(context, GetCSharpTokens(node));

        context.CodeWriter.Write(");");
        context.CodeWriter.WriteLine();
    }

    public override void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        // Looks like:
        //
        // (__builder73) => { ... }
        using (ScopeStack.OpenTemplateScope(context))
        {
            context.RenderChildren(node);
        }
    }

    public override void WriteSetKey(CodeRenderingContext context, SetKeyIntermediateNode node)
    {
        // Looks like:
        //
        // __builder.SetKey(_keyValue);

        var codeWriter = context.CodeWriter;

        codeWriter.WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.SetKey}");
        WriteSetKeyInnards(context, node);
        codeWriter.WriteEndMethodInvocation();
    }

    private void WriteSetKeyInnards(CodeRenderingContext context, SetKeyIntermediateNode node)
    {
        WriteCSharpCode(context, new CSharpCodeIntermediateNode
        {
            Source = node.Source,
            Children = { node.KeyValueToken }
        });
    }

    public override void WriteSplat(CodeRenderingContext context, SplatIntermediateNode node)
    {
        // Looks like:
        //
        // __builder.AddMultipleAttributes(2, ...);
        context.CodeWriter.WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddMultipleAttributes}");
        context.CodeWriter.Write("-1");
        context.CodeWriter.WriteParameterSeparator();

        WriteSplatInnards(context, node, canTypeCheck: true);

        context.CodeWriter.WriteEndMethodInvocation();
    }

    private static void WriteSplatInnards(CodeRenderingContext context, SplatIntermediateNode node, bool canTypeCheck)
    {
        var writer = context.CodeWriter;

        if (canTypeCheck)
        {
            writer.Write($"{ComponentsApi.RuntimeHelpers.TypeCheck}<{ComponentsApi.AddMultipleAttributesTypeFullName}>(");
        }

        using var tokens = new PooledArrayBuilder<CSharpIntermediateToken>();
        node.CollectDescendantNodes(ref tokens.AsRef());

        WriteCSharpTokens(context, in tokens);

        if (canTypeCheck)
        {
            writer.Write(")");
        }
    }

    public sealed override void WriteFormName(CodeRenderingContext context, FormNameIntermediateNode node)
    {
        if (node.Children.Count > 1)
        {
            Debug.Assert(node.HasDiagnostics, "We should have reported an error for mixed content.");
        }

        foreach (var token in GetCSharpTokens(node))
        {
            context.CodeWriter.Write(ComponentsApi.RuntimeHelpers.TypeCheck);
            context.CodeWriter.Write("<string>(");

            WriteCSharpToken(context, token);

            context.CodeWriter.WriteLine(");");
        }
    }

    public override void WriteReferenceCapture(CodeRenderingContext context, ReferenceCaptureIntermediateNode node)
    {
        // Looks like:
        //
        // __field = default(MyComponent);
        WriteReferenceCaptureInnards(context, node, shouldTypeCheck: true);
    }

    protected override void WriteReferenceCaptureInnards(CodeRenderingContext context, ReferenceCaptureIntermediateNode node, bool shouldTypeCheck)
    {
        // We specialize this code based on whether or not we can type check. When we're calling into
        // a type-inferenced component, we can't do the type check. See the comments in WriteTypeInferenceMethod.
        if (shouldTypeCheck)
        {
            // The runtime node writer moves the call elsewhere. At design time we
            // just want sufficiently similar code that any unknown-identifier or type
            // errors will be equivalent

            var assignmentText = string.Build((node.FieldTypeName, context.Options.SuppressNullabilityEnforcement), (ref builder, state) =>
            {
                builder.Append(" = default(");
                builder.Append(state.FieldTypeName);
                builder.Append(")");

                if (!state.SuppressNullabilityEnforcement)
                {
                    builder.Append("!");
                }

                builder.Append(";");
            });

            var assignmentToken = IntermediateNodeFactory.CSharpToken(assignmentText);

            WriteCSharpCode(context, new CSharpCodeIntermediateNode
            {
                Source = node.Source,
                Children = { node.IdentifierToken, assignmentToken }
            });
        }
        else
        {
            // Looks like:
            //
            // (__value) = { _field = (MyComponent)__value; }
            // OR
            // (__value) = { _field = (ElementRef)__value; }
            const string RefCaptureParamName = "__value";
            const string DefaultAssignment = $" = {RefCaptureParamName};";

            using (context.CodeWriter.BuildLambda(RefCaptureParamName))
            {
                WriteCSharpCode(context, new CSharpCodeIntermediateNode
                {
                    Source = node.Source,
                    Children =
                    {
                        node.IdentifierToken,
                        IntermediateNodeFactory.CSharpToken(DefaultAssignment)
                    }
                });
            }
        }
    }

    public override void WriteRenderMode(CodeRenderingContext context, RenderModeIntermediateNode node)
    {
        // Looks like:
        // __o = (global::Microsoft.AspNetCore.Components.IComponentRenderMode)(expression);
        context.CodeWriter.Write($"{DesignTimeVariable} = (global::{ComponentsApi.IComponentRenderMode.FullTypeName})(");

        WriteCSharpCode(context, new CSharpCodeIntermediateNode
        {
            Source = node.Source,
            Children = { node.Children[0] }
        });

        context.CodeWriter.WriteLine(");");
    }

    private static void WriteCSharpTokens(CodeRenderingContext context, ImmutableArray<CSharpIntermediateToken> tokens)
    {
        foreach (var token in tokens)
        {
            WriteCSharpToken(context, token);
        }
    }

    private static void WriteCSharpTokens(CodeRenderingContext context, ref readonly PooledArrayBuilder<CSharpIntermediateToken> tokens)
    {
        foreach (var token in tokens)
        {
            WriteCSharpToken(context, token);
        }
    }

    private static void WriteCSharpToken(CodeRenderingContext context, CSharpIntermediateToken token)
    {
        if (string.IsNullOrWhiteSpace(token.Content))
        {
            return;
        }

        if (token.Source?.FilePath == null)
        {
            context.CodeWriter.Write(token.Content);
            return;
        }

        using (context.BuildLinePragma(token.Source))
        {
            context.CodeWriter.WritePadding(0, token.Source.Value, context);
            context.AddSourceMappingFor(token);
            context.CodeWriter.Write(token.Content);
        }
    }
}
