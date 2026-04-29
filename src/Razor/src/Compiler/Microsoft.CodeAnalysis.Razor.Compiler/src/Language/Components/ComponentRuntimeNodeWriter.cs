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

/// <summary>
/// Generates the C# code corresponding to Razor source document contents.
/// </summary>
internal class ComponentRuntimeNodeWriter : ComponentNodeWriter
{
    private readonly ImmutableArray<IntermediateToken>.Builder _currentAttributeValues = ImmutableArray.CreateBuilder<IntermediateToken>();
    private int _sourceSequence;

    public ComponentRuntimeNodeWriter(RazorLanguageVersion version) : base(version)
    {
    }

    public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
    {
        var isWhitespaceStatement = true;
        foreach (var child in node.Children)
        {
            if (child is not IntermediateToken token || !string.IsNullOrWhiteSpace(token.Content))
            {
                isWhitespaceStatement = false;
                break;
            }
        }

        if (node.Source is null && isWhitespaceStatement)
        {
            // If source is null, we won't create source mappings, and if we're not creating source mappings,
            // there is no point emitting whitespace
            return;
        }

        foreach (var child in node.Children)
        {
            if (child is CSharpIntermediateToken token)
            {
                WriteCSharpToken(context, token);
            }
            else
            {
                // There may be something else inside the statement like an extension node.
                context.RenderNode(child);
            }
        }

        context.CodeWriter.WriteLine();
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

        var characterOffset = BuilderVariableName.Length // for "__builder"
            + 1 // for '.'
            + ComponentsApi.RenderTreeBuilder.AddContent.Length
            + 1 // for '('
            + _sourceSequence.CountDigits() // for the sequence number
            + 2; // for ', '

        // Sequence points can only be emitted when the eval stack is empty. That means we can't arbitrarily map everything that could be in
        // the node. Instead we map just the first C# child node by putting the pragma before we start the method invocation and offset it.
        // This is not a perfect mapping, but generally this works for most cases:
        // - Common case: there is only a single node and it is C#, so it maps correctly
        // - There is some C# followed by a render template: the C# gets mapped, and the render template issues a lambda call which conceptually
        //   is another method so a sequence point can be emitted. Unfortunately any trailing C# is not mapped, although in many cases it's uninteresting
        //   such as closing parenthesis.
        // - Error cases: there are no nodes, so we do nothing
        var firstCSharpChild = node.Children.OfType<CSharpIntermediateToken>().FirstOrDefault();
        using (context.BuildEnhancedLinePragma(firstCSharpChild?.Source, characterOffset))
        {
            context.CodeWriter
                .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddContent}")
                .WriteIntegerLiteral(_sourceSequence++)
                .WriteParameterSeparator();

            if (firstCSharpChild is not null)
            {
                context.CodeWriter.Write(firstCSharpChild.Content);
            }
        }

        // render the remaining children. We still emit the #line pragmas for the remaining csharp tokens but
        // these wont actually generate any sequence points for debugging.

        foreach (var child in node.Children)
        {
            if (child == firstCSharpChild)
            {
                continue;
            }

            if (child is CSharpIntermediateToken csharpToken)
            {
                WriteCSharpToken(context, csharpToken);
            }
            else
            {
                // There may be something else inside the expression like a Template or another extension node.
                context.RenderNode(child);
            }
        }

        context.CodeWriter.WriteEndMethodInvocation();
    }

    public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
    {
        // In cases like "somestring @variable", Razor tokenizes it as:
        //  [0] HtmlContent="somestring"
        //  [1] CsharpContent="variable" Prefix=" "
        // ... so to avoid losing whitespace, convert the prefix to a further token in the list
        if (!string.IsNullOrEmpty(node.Prefix))
        {
            _currentAttributeValues.Add(IntermediateNodeFactory.HtmlToken(node.Prefix));
        }

        foreach (var child in node.Children)
        {
            _currentAttributeValues.Add((IntermediateToken)child);
        }
    }

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

        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddMarkupContent}")
            .WriteIntegerLiteral(_sourceSequence++)
            .WriteParameterSeparator()
            .WriteStringLiteral(node.Content)
            .WriteEndMethodInvocation();
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

        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.OpenElement}")
            .WriteIntegerLiteral(_sourceSequence++)
            .WriteParameterSeparator()
            .WriteStringLiteral(node.TagName)
            .WriteEndMethodInvocation();

        bool hasFormName = false;

        // Render attributes and splats (in order) before creating the scope.
        foreach (var child in node.Children)
        {
            if (child is HtmlAttributeIntermediateNode attribute)
            {
                context.RenderNode(attribute);
            }
            else if (child is ComponentAttributeIntermediateNode componentAttribute)
            {
                context.RenderNode(componentAttribute);
            }
            else if (child is SplatIntermediateNode splat)
            {
                context.RenderNode(splat);
            }
            else if (child is FormNameIntermediateNode formName)
            {
                Debug.Assert(!hasFormName);
                context.RenderNode(formName);
                hasFormName = true;
            }
        }

        foreach (var setKey in node.SetKeys)
        {
            context.RenderNode(setKey);
        }

        foreach (var capture in node.Captures)
        {
            context.RenderNode(capture);
        }

        // AddNamedEvent must be called after all attributes (but before child content).
        if (hasFormName)
        {
            // _builder.AddNamedEvent("onsubmit", __formName);
            context.CodeWriter.WriteLine($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddNamedEvent}(\"onsubmit\", {FormNameVariableName});");
            ScopeStack.IncrementFormName();
        }

        // Render body of the tag inside the scope
        foreach (var child in node.Body)
        {
            context.RenderNode(child);
        }

        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.CloseElement}")
            .WriteEndMethodInvocation();
    }

    public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
    {
        Debug.Assert(_currentAttributeValues.Count == 0);
        context.RenderChildren(node);

        if (node.AttributeNameExpression == null)
        {
            WriteAttribute(context, node.AttributeName, _currentAttributeValues.ToImmutableAndClear());
        }
        else
        {
            WriteAttribute(context, node.AttributeNameExpression, _currentAttributeValues.ToImmutableAndClear());
        }

        if (!string.IsNullOrEmpty(node.EventUpdatesAttributeName))
        {
            context.CodeWriter
                .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.SetUpdatesAttributeName}")
                .WriteStringLiteral(node.EventUpdatesAttributeName)
                .WriteEndMethodInvocation();
        }
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

        var stringContent = ((IntermediateToken)node.Children.Single()).Content;
        _currentAttributeValues.Add(IntermediateNodeFactory.HtmlToken(node.Prefix + stringContent));
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

        // Text node
        var content = GetHtmlContent(node);
        var renderApi = ComponentsApi.RenderTreeBuilder.AddContent;
        if (node.HasEncodedContent)
        {
            // This content is already encoded.
            renderApi = ComponentsApi.RenderTreeBuilder.AddMarkupContent;
        }

        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{renderApi}")
            .WriteIntegerLiteral(_sourceSequence++)
            .WriteParameterSeparator()
            .WriteStringLiteral(content)
            .WriteEndMethodInvocation();
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
            using (context.BuildEnhancedLinePragma(sourceSpan, suppressLineDefaultAndHidden: true))
            {
                context.CodeWriter.WriteUsing(node.Content, endLine: node.HasExplicitSemicolon);
            }
            if (!node.HasExplicitSemicolon)
            {
                context.CodeWriter.WriteLine(";");
            }
            if (node.AppendLineDefaultAndHidden)
            {
                context.CodeWriter.WriteLine("#line default");
                context.CodeWriter.WriteLine("#line hidden");
            }
        }
        else
        {
            context.CodeWriter.WriteUsing(node.Content, endLine: true);

            if (node.AppendLineDefaultAndHidden)
            {
                context.CodeWriter.WriteLine("#line default");
                context.CodeWriter.WriteLine("#line hidden");
            }
        }
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

        if (ShouldSuppressTypeInferenceCall(node))
        {
        }
        else if (node.TypeInferenceNode == null)
        {
            // If the component is not using type inference then we just write an open/close with a series
            // of add attribute calls in between.
            //
            // Writes something like:
            //
            // _builder.OpenComponent<MyComponent>(0);
            // _builder.AddComponentParameter(1, "Foo", ...);
            // _builder.AddComponentParameter(2, "ChildContent", ...);
            // _builder.SetKey(someValue);
            // _builder.AddElementCapture(3, (__value) => _field = __value);
            // _builder.CloseComponent();

            // _builder.OpenComponent<TComponent>(42);
            context.CodeWriter.Write(BuilderVariableName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.OpenComponent);
            context.CodeWriter.Write("<");

            var nonGenericTypeName = TypeNameHelper.GetNonGenericTypeName(node.TypeName, out _);
            TypeNameHelper.WriteGlobalPrefixIfNeeded(context.CodeWriter, nonGenericTypeName);
            WriteComponentTypeName(context, node, nonGenericTypeName);

            if (!node.OrderedTypeArguments.IsDefaultOrEmpty)
            {
                context.CodeWriter.Write("<");
                for (var i = 0; i < node.OrderedTypeArguments.Length; i++)
                {
                    var typeArg = node.OrderedTypeArguments[i];
                    WriteComponentTypeArgument(context, typeArg);
                    if (i != node.OrderedTypeArguments.Length - 1)
                    {
                        context.CodeWriter.Write(", ");
                    }
                }
                context.CodeWriter.Write(">");
            }

            context.CodeWriter.Write(">(");
            context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);
            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();

            // We can skip type arguments during runtime codegen, they are handled in the
            // type/parameter declarations.

            bool hasRenderMode = false;

            // Preserve order of attributes and splats
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
                    Debug.Assert(!hasRenderMode);
                    context.RenderNode(renderMode);
                    hasRenderMode = true;
                }
            }

            foreach (var childContent in node.ChildContents)
            {
                context.RenderNode(childContent);
            }

            foreach (var setKey in node.SetKeys)
            {
                context.RenderNode(setKey);
            }

            foreach (var capture in node.Captures)
            {
                context.RenderNode(capture);
            }

            if (hasRenderMode)
            {
                // _builder.AddComponentRenderMode(__renderMode_0);
                WriteAddComponentRenderMode(context, BuilderVariableName, RenderModeVariableName);
                ScopeStack.IncrementRenderMode();
            }

            // _builder.CloseComponent();
            context.CodeWriter.Write(BuilderVariableName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.CloseComponent);
            context.CodeWriter.Write("();");
            context.CodeWriter.WriteLine();
        }
        else
        {
            var parameters = GetTypeInferenceMethodParameters(node.TypeInferenceNode);

            // If this component is going to cascade any of its generic types, we have to split its type inference
            // into two parts. First we call an inference method that captures all the parameters in local variables,
            // then we use those to call the real type inference method that emits the component. The reason for this
            // is so the captured variables can be used by descendants without re-evaluating the expressions.
            CodeWriterExtensions.CSharpCodeWritingScope? typeInferenceCaptureScope = null;
            if (node.Component.SuppliesCascadingGenericParameters())
            {
                typeInferenceCaptureScope = context.CodeWriter.BuildScope();
                TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, node.TypeInferenceNode.FullTypeName);
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
            // __Blazor.MyComponent.TypeInference.CreateMyComponent_0(builder, 0, 1, ..., 2, ..., 3, ...);

            TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, node.TypeInferenceNode.FullTypeName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(node.TypeInferenceNode.MethodName);
            context.CodeWriter.Write("(");

            context.CodeWriter.Write(BuilderVariableName);
            context.CodeWriter.Write(", ");

            context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);

            foreach (var parameter in parameters)
            {
                context.CodeWriter.Write(", ");

                if (parameter.SeqName != null)
                {
                    context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);
                    context.CodeWriter.Write(", ");
                }

                WriteTypeInferenceMethodParameterInnards(context, parameter);
            }

            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();

            if (typeInferenceCaptureScope.HasValue)
            {
                foreach (var localToClear in parameters.Select(p => p.Source).OfType<TypeInferenceCapturedVariable>())
                {
                    // Ensure we're not interfering with the GC lifetime of these captured values
                    // We don't need the values any longer (code in closures only uses its types for compile-time inference)
                    context.CodeWriter.Write(localToClear.VariableName);
                    context.CodeWriter.WriteLine(" = default;");
                }

                typeInferenceCaptureScope.Value.Dispose();
            }
        }
    }

    public override void WriteComponentTypeInferenceMethod(CodeRenderingContext context, ComponentTypeInferenceMethodIntermediateNode node)
    {
        WriteComponentTypeInferenceMethod(context, node, returnComponentType: false, allowNameof: true, mapComponentStartTag: true);
    }

    private void WriteTypeInferenceMethodParameterInnards(CodeRenderingContext context, TypeInferenceMethodParameter parameter)
    {
        switch (parameter.Source)
        {
            case ComponentAttributeIntermediateNode attribute:
                // Don't type check generics, since we can't actually write the type name.
                // The type checking will happen anyway since we defined a method and we're generating
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

        if (node.IsDesignTimePropertyAccessHelper)
        {
            WriteDesignTimePropertyAccessor(context, node);
            return;
        }

        var addAttributeMethod = node.AddAttributeMethodName ?? GetAddComponentParameterMethodName(context);

        // _builder.AddComponentParameter(1, nameof(Component.Property), 42);
        context.CodeWriter.Write(BuilderVariableName);
        context.CodeWriter.Write(".");
        context.CodeWriter.Write(addAttributeMethod);
        context.CodeWriter.Write("(");
        context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);
        context.CodeWriter.Write(", ");

        WriteComponentAttributeName(context, node);
        context.CodeWriter.Write(", ");

        if (addAttributeMethod == ComponentsApi.RenderTreeBuilder.AddAttribute)
        {
            context.CodeWriter.Write("(object)(");
        }

        WriteComponentAttributeInnards(context, node, canTypeCheck: true);

        if (addAttributeMethod == ComponentsApi.RenderTreeBuilder.AddAttribute)
        {
            context.CodeWriter.Write(")");
        }

        context.CodeWriter.Write(");");
        context.CodeWriter.WriteLine();
    }

    private static void WriteDesignTimePropertyAccessor(CodeRenderingContext context, ComponentAttributeIntermediateNode attribute)
    {
        // These attributes don't really exist in the emitted code, but have a representation in the razor document.
        // We emit a small piece of empty code that is elided by the compiler, so that the IDE has something to reference
        // for Find All References etc.
        Debug.Assert(attribute.BoundAttribute?.ContainingType is not null);
        context.CodeWriter.Write(" _ = ");
        WriteComponentAttributeName(context, attribute);
        context.CodeWriter.WriteLine(";");
    }

    private void WriteComponentAttributeInnards(CodeRenderingContext context, ComponentAttributeIntermediateNode node, bool canTypeCheck)
    {
        if (node.Children.Count > 1)
        {
            Debug.Assert(node.HasDiagnostics, "We should have reported an error for mixed content.");
            // We render the children anyway, so tooling works.
        }

        if (node.AttributeStructure == AttributeStructure.Minimized)
        {
            // Minimized attributes always map to 'true'
            context.CodeWriter.Write("true");
        }
        else if (node.Children.Count == 1 && node.Children[0] is HtmlContentIntermediateNode htmlNode)
        {
            // This is how string attributes are lowered by default, a single HTML node with a single HTML token.
            var content = string.Join(string.Empty, GetHtmlTokens(htmlNode).Select(t => t.Content));
            context.CodeWriter.WriteStringLiteral(content);
        }
        else
        {
            // See comments in ComponentDesignTimeNodeWriter for a description of the cases that are possible.
            var tokens = GetCSharpTokens(node);
            if ((node.BoundAttribute?.IsDelegateProperty() ?? false) ||
                (node.BoundAttribute?.IsChildContentProperty() ?? false))
            {
                if (canTypeCheck)
                {
                    context.CodeWriter.Write("(");
                    WriteGloballyQualifiedTypeName(context, node);
                    context.CodeWriter.Write(")");
                    context.CodeWriter.Write("(");
                }

                WriteCSharpTokens(context, tokens);

                if (canTypeCheck)
                {
                    context.CodeWriter.Write(")");
                }
            }
            else if (node.BoundAttribute?.IsEventCallbackProperty() ?? false)
            {
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

                WriteCSharpTokens(context, tokens);

                context.CodeWriter.Write(")");

                if (canTypeCheck && NeedsTypeCheck(node))
                {
                    context.CodeWriter.Write(")");
                }
            }
            else
            {
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
            return n.BoundAttribute != null && !n.BoundAttribute.IsWeaklyTyped;
        }
    }

    private static ImmutableArray<HtmlIntermediateToken> GetHtmlTokens(IntermediateNode node)
    {
        // We generally expect all children to be HTML, this is here just in case.
        return node.FindDescendantNodes<HtmlIntermediateToken>();
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
        // _builder.AddComponentParameter(1, "ChildContent", (RenderFragment)((__builder73) => { ... }));
        // OR
        // _builder.AddComponentParameter(1, "ChildContent", (RenderFragment<Person>)((person) => (__builder73) => { ... }));
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

        using (ScopeStack.OpenComponentScope(context, parameterName))
        {
            foreach (var child in node.Children)
            {
                context.RenderNode(child);
            }
        }
    }

    public override void WriteComponentTypeArgument(CodeRenderingContext context, ComponentTypeArgumentIntermediateNode node)
    {
        WriteCSharpToken(context, node.Value);
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
        // _builder.SetKey(_keyValue);

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
        // _builder.AddMultipleAttributes(2, ...);
        context.CodeWriter.WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddMultipleAttributes}");
        context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);
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

        // string __formName = expression;
        context.CodeWriter.Write($"string {FormNameVariableName} = {ComponentsApi.RuntimeHelpers.TypeCheck}<string>(");
        WriteAttributeValue(context, node.FindDescendantNodes<IntermediateToken>());
        context.CodeWriter.WriteLine(");");
    }

    public override void WriteReferenceCapture(CodeRenderingContext context, ReferenceCaptureIntermediateNode node)
    {
        // Looks like:
        //
        // _builder.AddComponentReferenceCapture(2, (__value) = { _field = (MyComponent)__value; });
        // OR
        // _builder.AddElementReferenceCapture(2, (__value) = { _field = (ElementReference)__value; });
        var codeWriter = context.CodeWriter;

        var methodName = node.IsComponentCapture
            ? ComponentsApi.RenderTreeBuilder.AddComponentReferenceCapture
            : ComponentsApi.RenderTreeBuilder.AddElementReferenceCapture;
        codeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{methodName}")
            .WriteIntegerLiteral(_sourceSequence++)
            .WriteParameterSeparator();

        WriteReferenceCaptureInnards(context, node, shouldTypeCheck: true);

        codeWriter.WriteEndMethodInvocation();
    }

    protected override void WriteReferenceCaptureInnards(CodeRenderingContext context, ReferenceCaptureIntermediateNode node, bool shouldTypeCheck)
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
            shouldTypeCheck = shouldTypeCheck && node.IsComponentCapture;

            var assignmentToken = shouldTypeCheck
                ? IntermediateNodeFactory.CSharpToken($" = ({node.FieldTypeName}){RefCaptureParamName};")
                : IntermediateNodeFactory.CSharpToken(DefaultAssignment);

            WriteCSharpCode(context, new CSharpCodeIntermediateNode
            {
                Source = node.Source,
                Children = { node.IdentifierToken, assignmentToken }
            });
        }
    }

    public override void WriteRenderMode(CodeRenderingContext context, RenderModeIntermediateNode node)
    {
        // Looks like:
        // global::Microsoft.AspNetCore.Components.IComponentRenderMode __renderMode0 = expression;
        context.CodeWriter.Write($"global::{ComponentsApi.IComponentRenderMode.FullTypeName} {RenderModeVariableName} = ");

        WriteCSharpCode(context, new CSharpCodeIntermediateNode
        {
            Source = node.Source,
            Children = { node.Children[0] }
        });

        context.CodeWriter.WriteLine(";");
    }

    private void WriteAttribute(CodeRenderingContext context, string key, ImmutableArray<IntermediateToken> value)
    {
        BeginWriteAttribute(context, key);

        if (value.Length > 0)
        {
            context.CodeWriter.WriteParameterSeparator();
            WriteAttributeValue(context, value);
        }
        else if (!context.Options.OmitMinimizedComponentAttributeValues)
        {
            // In version 5+, there's no need to supply a value for a minimized attribute.
            // But for older language versions, minimized attributes were represented as "true".
            context.CodeWriter.WriteParameterSeparator();
            context.CodeWriter.WriteBooleanLiteral(true);
        }

        context.CodeWriter.WriteEndMethodInvocation();
    }

    private void WriteAttribute(CodeRenderingContext context, IntermediateNode nameExpression, ImmutableArray<IntermediateToken> value)
    {
        BeginWriteAttribute(context, nameExpression);

        if (value.Length > 0)
        {
            context.CodeWriter.WriteParameterSeparator();
            WriteAttributeValue(context, value);
        }

        context.CodeWriter.WriteEndMethodInvocation();
    }

    protected override void BeginWriteAttribute(CodeRenderingContext context, string key)
    {
        context.CodeWriter
            .WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddAttribute}")
            .WriteIntegerLiteral(_sourceSequence++)
            .WriteParameterSeparator()
            .WriteStringLiteral(key);
    }

    protected override void BeginWriteAttribute(CodeRenderingContext context, IntermediateNode nameExpression)
    {
        context.CodeWriter.WriteStartMethodInvocation($"{BuilderVariableName}.{ComponentsApi.RenderTreeBuilder.AddAttribute}");
        context.CodeWriter.WriteIntegerLiteral(_sourceSequence++);
        context.CodeWriter.WriteParameterSeparator();

        var tokens = GetCSharpTokens(nameExpression);
        for (var i = 0; i < tokens.Length; i++)
        {
            WriteCSharpToken(context, tokens[i]);
        }
    }

    private static string GetHtmlContent(HtmlContentIntermediateNode node)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        var htmlTokens = node.Children.OfType<HtmlIntermediateToken>();

        foreach (var child in node.Children)
        {
            if (child is HtmlIntermediateToken htmlToken)
            {
                builder.Append(htmlToken.Content);
            }
        }

        return builder.ToString();
    }

    // There are a few cases here, we need to handle:
    // - Pure HTML
    // - Pure CSharp
    // - Mixed HTML and CSharp
    //
    // Only the mixed case is complicated, we want to turn it into code that will concatenate
    // the values into a string at runtime.

    private static void WriteAttributeValue(CodeRenderingContext context, ImmutableArray<IntermediateToken> tokens)
    {
        if (tokens.Length == 0)
        {
            return;
        }

        var writer = context.CodeWriter;
        var hasHtml = false;
        var hasCSharp = false;

        foreach (var token in tokens)
        {
            if (token is CSharpIntermediateToken)
            {
                hasCSharp |= true;
            }
            else
            {
                Debug.Assert(token is HtmlIntermediateToken);
                hasHtml |= true;
            }
        }

        if (!hasCSharp && !hasHtml)
        {
            Assumed.Unreachable("Found attribute whose value is neither HTML nor CSharp");
        }

        // If we only have C# tokens, we write them out directly.
        if (hasCSharp && !hasHtml)
        {
            foreach (var token in tokens)
            {
                WriteCSharpToken(context, (CSharpIntermediateToken)token);
            }

            return;
        }

        // If we only have HTML tokens, we write out a single string literal.
        if (hasHtml && !hasCSharp)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            foreach (var token in tokens)
            {
                Debug.Assert(token is HtmlIntermediateToken);
                builder.Append(token.Content);
            }

            writer.WriteStringLiteral(builder.ToString());
            return;
        }

        // If it's a C# expression, we have to wrap it in parentheses, otherwise things like ternary
        // expressions don't compose with concatenation. However, this is a little complicated
        // because C# tokens themselves aren't guaranteed to be distinct expressions. We want
        // to treat all contiguous C# tokens as a single expression.
        var insideCSharp = false;
        var first = true;
        foreach (var token in tokens)
        {
            if (token is CSharpIntermediateToken csharpToken)
            {
                if (!insideCSharp)
                {
                    // Transition to a new C# expression
                    if (!first)
                    {
                        writer.Write(" + ");
                    }

                    writer.Write("(");
                    insideCSharp = true;
                }

                WriteCSharpToken(context, csharpToken);
            }
            else
            {
                if (insideCSharp)
                {
                    // Transition to HTML, close out the C# expression
                    writer.Write(")");
                    insideCSharp = false;
                }

                if (!first)
                {
                    writer.Write(" + ");
                }

                writer.WriteStringLiteral(token.Content);
            }

            if (first)
            {
                first = false;
            }
        }

        if (insideCSharp)
        {
            writer.Write(")");
        }
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
        if (token.Source?.FilePath == null)
        {
            context.CodeWriter.Write(token.Content);
            return;
        }

        using (context.BuildEnhancedLinePragma(token.Source))
        {
            context.CodeWriter.Write(token.Content);
        }
    }
}
