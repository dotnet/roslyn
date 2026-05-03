// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal abstract class ComponentNodeWriter : IntermediateNodeWriter, ITemplateTargetExtension
{
    private readonly RazorLanguageVersion _version;
    protected readonly ScopeStack ScopeStack = new();

    protected ComponentNodeWriter(RazorLanguageVersion version)
    {
        _version = version;
    }

    public BuilderVariableName BuilderVariableName => ScopeStack.BuilderVariableName;
    public RenderModeVariableName RenderModeVariableName => ScopeStack.RenderModeVariableName;
    public FormNameVariableName FormNameVariableName => ScopeStack.FormNameVariableName;

    protected virtual bool CanUseAddComponentParameter(CodeRenderingContext context)
    {
        return !context.Options.SuppressAddComponentParameter && _version >= RazorLanguageVersion.Version_8_0;
    }

    protected string GetAddComponentParameterMethodName(CodeRenderingContext context)
    {
        return CanUseAddComponentParameter(context)
            ? ComponentsApi.RenderTreeBuilder.AddComponentParameter
            : ComponentsApi.RenderTreeBuilder.AddAttribute;
    }

    protected abstract void BeginWriteAttribute(CodeRenderingContext context, string key);

    protected abstract void BeginWriteAttribute(CodeRenderingContext context, IntermediateNode expression);

    protected abstract void WriteReferenceCaptureInnards(CodeRenderingContext context, ReferenceCaptureIntermediateNode node, bool shouldTypeCheck);

    public abstract void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node);

    public sealed override void BeginWriterScope(CodeRenderingContext context, string writer)
    {
        throw new NotImplementedException(nameof(BeginWriterScope));
    }

    public sealed override void EndWriterScope(CodeRenderingContext context)
    {
        throw new NotImplementedException(nameof(EndWriterScope));
    }

    public sealed override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
    {
        // We used to support syntaxes like <elem onsomeevent=@{ /* some C# code */ } /> but this is no longer the
        // case.
        //
        // We provide an error for this case just to be friendly.
        var content = string.Join("", node.Children.OfType<IntermediateToken>().Select(t => t.Content));
        context.AddDiagnostic(ComponentDiagnosticFactory.Create_CodeBlockInAttribute(node.Source, content));
        return;
    }

    protected bool ShouldSuppressTypeInferenceCall(ComponentIntermediateNode node)
    {
        // When RZ10001 (type of component cannot be inferred) is reported, we want to suppress the equivalent CS0411 errors,
        // so we don't generate the call to TypeInference.CreateComponent.
        return node.Diagnostics.Any(d => d.Id == ComponentDiagnosticFactory.GenericComponentTypeInferenceUnderspecified.Id);
    }

    protected void WriteComponentTypeInferenceMethod(CodeRenderingContext context, ComponentTypeInferenceMethodIntermediateNode node, bool returnComponentType, bool allowNameof, bool mapComponentStartTag)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var parameters = GetTypeInferenceMethodParameters(node);

        // This is really similar to the code in WriteComponentAttribute and WriteComponentChildContent - except simpler because
        // attributes and child contents look like variables.
        //
        // Looks like:
        //
        //  public static void CreateFoo_0<T1, T2>(RenderTreeBuilder __builder, int seq, int __seq0, T1 __arg0, int __seq1, global::System.Collections.Generic.List<T2> __arg1, int __seq2, string __arg2)
        //  {
        //      builder.OpenComponent<Foo<T1, T2>>();
        //      builder.AddComponentParameter(__seq0, nameof(Foo<T1, T2>.Attr0), __arg0);
        //      builder.AddComponentParameter(__seq1, nameof(Foo<T1, T2>.Attr1), __arg1);
        //      builder.AddComponentParameter(__seq2, nameof(Foo<T1, T2>.Attr2), __arg2);
        //      builder.CloseComponent();
        //  }
        //
        // As a special case, we need to generate a thunk for captures in this block instead of using
        // them verbatim.
        //
        // The problem is that RenderTreeBuilder wants an Action<object>. The caller can't write the type
        // name if it contains generics, and we can't write the variable they want to assign to.
        var writer = context.CodeWriter;

        writer.Write("public static ");
        if (returnComponentType)
        {
            writer.Write(node.Component.TypeName);
        }
        else
        {
            writer.Write("void");
        }
        writer.Write(" ");
        writer.Write(node.MethodName);
        writer.Write("<");
        writer.Write(string.Join(", ", node.Component.Component.GetTypeParameters().Select(serializeTypeParameter)));
        writer.Write(">");

        writer.Write("(");
        writer.Write("global::");
        writer.Write(ComponentsApi.RenderTreeBuilder.FullTypeName);
        writer.Write(" ");
        writer.Write(ComponentsApi.RenderTreeBuilder.BuilderParameter);
        writer.Write(", ");
        writer.Write("int seq");

        if (parameters.Count > 0)
        {
            writer.Write(", ");
        }

        for (var i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].SeqName is SeqName seqName)
            {
                writer.Write($"int {seqName}, ");
            }

            writer.Write(parameters[i].TypeName);
            writer.Write(" ");
            writer.Write(parameters[i].ParameterName);

            if (i < parameters.Count - 1)
            {
                writer.Write(", ");
            }
        }

        writer.Write(")");

        writeConstraints(writer, node);

        writer.WriteLine("{");

        // _builder.OpenComponent<TComponent>(42);
        context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.BuilderParameter);
        context.CodeWriter.Write(".");
        context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.OpenComponent);
        context.CodeWriter.Write("<");

        if (mapComponentStartTag)
        {
            var nonGenericTypeName = TypeNameHelper.GetNonGenericTypeName(node.Component.TypeName, out var genericTypeParameterList);
            WriteComponentTypeName(context, node.Component, nonGenericTypeName);
            context.CodeWriter.Write(genericTypeParameterList);
        }
        else
        {
            context.CodeWriter.Write(node.Component.TypeName);
        }

        context.CodeWriter.Write(">(");
        context.CodeWriter.Write("seq");
        context.CodeWriter.Write(");");
        context.CodeWriter.WriteLine();

        ParameterName? renderModeParameterName = null;

        foreach (var parameter in parameters)
        {
            switch (parameter.Source)
            {
                case ComponentAttributeIntermediateNode attribute:
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, GetAddComponentParameterMethodName(context));
                    context.CodeWriter.Write(parameter.SeqName.AssumeNotNull());
                    context.CodeWriter.Write(", ");
                    WriteComponentAttributeName(context, attribute, allowNameof);
                    context.CodeWriter.Write(", ");

                    if (!CanUseAddComponentParameter(context))
                    {
                        context.CodeWriter.Write("(object)");
                    }

                    context.CodeWriter.Write(parameter.ParameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case SplatIntermediateNode:
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.AddMultipleAttributes);
                    context.CodeWriter.Write(parameter.SeqName.AssumeNotNull());
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write(parameter.ParameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case ComponentChildContentIntermediateNode childContent:
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, GetAddComponentParameterMethodName(context));
                    context.CodeWriter.Write(parameter.SeqName.AssumeNotNull());
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write($"\"{childContent.AttributeName}\"");
                    context.CodeWriter.Write(", ");

                    if (!CanUseAddComponentParameter(context))
                    {
                        context.CodeWriter.Write("(object)");
                    }

                    context.CodeWriter.Write(parameter.ParameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case SetKeyIntermediateNode:
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.SetKey);
                    context.CodeWriter.Write(parameter.ParameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case ReferenceCaptureIntermediateNode capture:
                    var methodName = capture.IsComponentCapture
                        ? ComponentsApi.RenderTreeBuilder.AddComponentReferenceCapture
                        : ComponentsApi.RenderTreeBuilder.AddElementReferenceCapture;

                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, methodName);
                    context.CodeWriter.Write(parameter.SeqName.AssumeNotNull());
                    context.CodeWriter.Write(", ");

                    var cast = capture.IsComponentCapture ? $"({capture.FieldTypeName})" : string.Empty;
                    context.CodeWriter.Write($"(__value) => {{ {parameter.ParameterName}({cast}__value); }}");
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case CascadingGenericTypeParameter:
                    // We only use the synthetic cascading parameters for type inference
                    break;

                case RenderModeIntermediateNode:
                    renderModeParameterName = parameter.ParameterName;
                    break;

                default:
                    throw new InvalidOperationException($"Not implemented: type inference method parameter from source {parameter.Source}");
            }
        }

        if (renderModeParameterName is ParameterName parameterName)
        {
            WriteAddComponentRenderMode(context, BuilderVariableName.Default, parameterName);
        }

        context.CodeWriter.WriteInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.CloseComponent);

        if (returnComponentType)
        {
            writer.WriteLine("return default;");
        }

        writer.WriteLine("}");

        if (node.Component.Component.SuppliesCascadingGenericParameters())
        {
            // If this component cascades any generic parameters, we'll need to be able to capture its type inference
            // args at the call site. The point of this is to ensure that:
            //
            // [1] We only evaluate each expression once
            // [2] We evaluate them in the correct order matching the developer's source
            // [3] We can even make variables for lambdas or other expressions that can't just be assigned to implicitly-typed vars.
            //
            // We do that by emitting a method like the following. It has exactly the same generic type inference
            // characteristics as the regular CreateFoo_0 method emitted earlier
            //
            //  public static void CreateFoo_0_CaptureParameters<T1, T2>(T1 __arg0, out T1 __arg0_out, global::System.Collections.Generic.List<T2> __arg1, out global::System.Collections.Generic.List<T2> __arg1_out, int __seq2, string __arg2, out string __arg2_out)
            //  {
            //      __arg0_out = __arg0;
            //      __arg1_out = __arg1;
            //      __arg2_out = __arg2;
            //  }
            //
            writer.WriteLine();
            writer.Write("public static void ");
            writer.Write(node.MethodName);
            writer.Write("_CaptureParameters<");
            writer.Write(string.Join(", ", node.Component.Component.GetTypeParameters().Select(a => a.Name)));
            writer.Write(">");

            writer.Write("(");
            var isFirst = true;
            foreach (var parameter in parameters.Where(p => p.UsedForTypeInference))
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    writer.Write(", ");
                }

                writer.Write(parameter.TypeName);
                writer.Write(" ");
                writer.Write(parameter.ParameterName);
                writer.Write(", out ");
                writer.Write(parameter.TypeName);
                writer.Write(" ");
                writer.Write(parameter.ParameterName);
                writer.Write("_out");
            }

            writer.Write(")");

            writeConstraints(writer, node);

            writer.WriteLine("{");
            foreach (var parameter in parameters.Where(p => p.UsedForTypeInference))
            {
                writer.Write("    ");
                writer.Write(parameter.ParameterName);
                writer.Write("_out = ");
                writer.Write(parameter.ParameterName);
                writer.WriteLine(";");
            }
            writer.WriteLine("}");
        }

        static void writeConstraints(CodeWriter writer, ComponentTypeInferenceMethodIntermediateNode node)
        {
            // Writes out a list of generic type constraints with indentation
            // public void Foo<T, U>(T t, U u)
            //      where T: new()
            //      where U: Foo, notnull
            foreach (var constraint in node.GenericTypeConstraints)
            {
                writer.WriteLine();
                writer.Indent(writer.CurrentIndent + writer.TabSize);
                writer.Write(constraint);
            }

            writer.WriteLine();
        }

        static string serializeTypeParameter(BoundAttributeDescriptor attribute)
        {
            if (attribute.Metadata is TypeParameterMetadata { NameWithAttributes: string withAttributes })
            {
                return withAttributes;
            }

            return attribute.Name;
        }
    }

    protected static void WriteComponentAttributeName(CodeRenderingContext context, ComponentAttributeIntermediateNode attribute, bool allowNameof = true)
    {
        if (allowNameof && attribute.BoundAttribute?.ContainingType is string containingType)
        {
            containingType = attribute.ConcreteContainingType ?? containingType;

            // nameof(containingType.PropertyName)
            // This allows things like Find All References to work in the IDE as we have an actual reference to the parameter
            context.CodeWriter.Write("nameof(");
            TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, containingType);
            context.CodeWriter.Write(".");

            if (!attribute.IsSynthesized)
            {
                var attributeSourceSpan = (SourceSpan)(attribute.PropertySpan ?? attribute.OriginalAttributeSpan);
                var requiresEscaping = attribute.PropertyName.IdentifierRequiresEscaping();
                using (context.BuildEnhancedLinePragma(attributeSourceSpan, characterOffset: requiresEscaping ? 1 : 0))
                {
                    context.CodeWriter.WriteIdentifierEscapeIfNeeded(attribute.PropertyName);
                    context.CodeWriter.WriteLine(attribute.PropertyName);
                }
            }
            else
            {
                context.CodeWriter.Write(attribute.PropertyName);
            }
            context.CodeWriter.Write(")");
        }
        else
        {
            context.CodeWriter.WriteStringLiteral(attribute.AttributeName);
        }
    }

    protected List<TypeInferenceMethodParameter> GetTypeInferenceMethodParameters(ComponentTypeInferenceMethodIntermediateNode node)
    {
        var p = new List<TypeInferenceMethodParameter>();

        // Preserve order between attributes and splats
        foreach (var child in node.Component.Children)
        {
            if (child is ComponentAttributeIntermediateNode attribute)
            {
                // Some nodes just exist to help with property access at design time, and don't need anything else written
                if (attribute.IsDesignTimePropertyAccessHelper)
                {
                    continue;
                }

                string typeName;
                if (attribute.GloballyQualifiedTypeName != null)
                {
                    typeName = attribute.GloballyQualifiedTypeName;
                }
                else
                {
                    typeName = attribute.TypeName;
                    if (attribute.BoundAttribute != null && !attribute.BoundAttribute.IsGenericTypedProperty())
                    {
                        typeName = typeName.StartsWith("global::", StringComparison.Ordinal) ? typeName : $"global::{typeName}";
                    }
                }

                p.Add(new TypeInferenceMethodParameter(new(p.Count), typeName, new(p.Count), usedForTypeInference: true, attribute));
            }
            else if (child is SplatIntermediateNode splat)
            {
                var typeName = ComponentsApi.AddMultipleAttributesTypeFullName;
                p.Add(new TypeInferenceMethodParameter(new(p.Count), typeName, new(p.Count), usedForTypeInference: false, splat));
            }
            else if (child is RenderModeIntermediateNode renderMode)
            {
                var typeName = ComponentsApi.IComponentRenderMode.FullTypeName;
                p.Add(new TypeInferenceMethodParameter(new(p.Count), typeName, new(p.Count), usedForTypeInference: false, renderMode));
            }
        }

        foreach (var childContent in node.Component.ChildContents)
        {
            var typeName = childContent.TypeName;
            if (childContent.BoundAttribute != null && !childContent.BoundAttribute.IsGenericTypedProperty())
            {
                typeName = childContent.BoundAttribute.GetGloballyQualifiedTypeName();
            }
            p.Add(new TypeInferenceMethodParameter(new(p.Count), typeName, new(p.Count), usedForTypeInference: false, childContent));
        }

        foreach (var capture in node.Component.SetKeys)
        {
            p.Add(new TypeInferenceMethodParameter(new(p.Count), "object", new(p.Count), usedForTypeInference: false, capture));
        }

        foreach (var capture in node.Component.Captures)
        {
            // The capture type name should already contain the global:: prefix.
            p.Add(new TypeInferenceMethodParameter(new(p.Count), capture.TypeName, new(p.Count), usedForTypeInference: false, capture));
        }

        // Insert synthetic args for cascaded type inference at the start of the list
        // We do this last so that the indices above aren't affected
        if (node.ReceivesCascadingGenericTypes != null)
        {
            var i = 0;
            foreach (var cascadingGenericType in node.ReceivesCascadingGenericTypes)
            {
                p.Insert(i, new TypeInferenceMethodParameter(null, cascadingGenericType.ValueType, new(i, isSynthetic: true), usedForTypeInference: true, cascadingGenericType));
                i++;
            }
        }

        return p;
    }

    protected static void UseCapturedCascadingGenericParameterVariable(ComponentIntermediateNode node, TypeInferenceMethodParameter parameter, TypeInferenceArgName variableName)
    {
        // If this captured variable corresponds to a generic type we want to cascade to
        // descendants, supply that info to descendants
        if (node.ProvidesCascadingGenericTypes != null)
        {
            foreach (var cascadeGeneric in node.ProvidesCascadingGenericTypes.Values)
            {
                if (cascadeGeneric.ValueSourceNode == parameter.Source)
                {
                    cascadeGeneric.ValueExpression = variableName;
                }
            }
        }

        // Since we've now evaluated and captured this expression, use the variable
        // instead of the expression from now on
        parameter.ReplaceSourceWithCapturedVariable(variableName);
    }

    protected static bool IsDefaultExpression(string expression)
    {
        return expression == "default" || expression.StartsWith("default(", StringComparison.Ordinal);
    }

    protected static void WriteAddComponentRenderMode<T>(CodeRenderingContext context, BuilderVariableName builderName, T renderModeName)
        where T : IWriteableValue
        => context.CodeWriter.WriteLine($"{builderName}.{ComponentsApi.RenderTreeBuilder.AddComponentRenderMode}({renderModeName});");

    protected static void WriteGloballyQualifiedTypeName(CodeRenderingContext context, ComponentAttributeIntermediateNode node)
    {
        if (node.HasExplicitTypeName)
        {
            context.CodeWriter.Write(node.TypeName);
        }
        else if (node.BoundAttribute?.GetGloballyQualifiedTypeName() is string typeName)
        {
            context.CodeWriter.Write(typeName);
        }
        else
        {
            TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, node.TypeName);
        }
    }

    protected static void WriteGloballyQualifiedTypeName(CodeRenderingContext context, ComponentChildContentIntermediateNode node)
    {
        if (node.BoundAttribute?.GetGloballyQualifiedTypeName() is string typeName &&
            !node.BoundAttribute.IsGenericTypedProperty())
        {
            context.CodeWriter.Write(typeName);
        }
        else
        {
            TypeNameHelper.WriteGloballyQualifiedName(context.CodeWriter, node.TypeName);
        }
    }

    protected static void WriteComponentTypeName(CodeRenderingContext context, ComponentIntermediateNode node, ReadOnlyMemory<char> nonGenericTypeName)
    {
        // The type name we are given may or may not be globally qualified, and we want to map it to the component start
        // tag, which may or may not be fully qualified. ie "global::My.Fun.Component" could map to just "Component"

        // Write out "global::" if it's present, and trim it off
        var lastColon = nonGenericTypeName.Span.LastIndexOf(':');
        if (lastColon > -1)
        {
            lastColon++;
            context.CodeWriter.Write(nonGenericTypeName[0..lastColon]);
            nonGenericTypeName = nonGenericTypeName.Slice(lastColon);
        }

        // If the start tag is shorter than the type name, then it must not be a fully qualified tag, so write out
        // the namespace parts and trim. Razor components don't support nested types, so this logic doesn't either.
        if (node.StartTagSpan.Length < nonGenericTypeName.Length)
        {
            var lastDot = nonGenericTypeName.Span.LastIndexOf('.');
            if (lastDot > -1)
            {
                lastDot++;
                context.CodeWriter.Write(nonGenericTypeName[0..lastDot]);
                nonGenericTypeName = nonGenericTypeName.Slice(lastDot);
            }
        }

        var offset = nonGenericTypeName.Span.StartsWith('@')
            ? 1
            : 0;
        context.AddSourceMappingFor(node.StartTagSpan, offset);
        context.CodeWriter.Write(nonGenericTypeName);
    }

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    protected internal readonly struct SeqName(int index) : IWriteableValue
    {
        public void WriteTo(CodeWriter writer)
        {
            writer.Write("__seq");
            writer.WriteIntegerLiteral(index);
        }

        internal string GetDebuggerDisplay()
            => $"__seq{index}";
    }

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    protected internal readonly struct ParameterName(int index, bool isSynthetic = false) : IWriteableValue
    {
        public void WriteTo(CodeWriter writer)
        {
            if (isSynthetic)
            {
                writer.Write("__syntheticArg");
            }
            else
            {
                writer.Write("__arg");
            }

            writer.WriteIntegerLiteral(index);
        }

        internal string GetDebuggerDisplay()
            => isSynthetic ? $"__syntheticArg{index}" : $"__arg{index}";
    }

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    protected internal readonly struct TypeInferenceArgName(int depth, ParameterName parameterName) : IWriteableValue
    {
        public void WriteTo(CodeWriter writer)
        {
            writer.Write("__typeInferenceArg_");
            writer.WriteIntegerLiteral(depth);
            writer.Write($"_{parameterName}");
        }

        internal string GetDebuggerDisplay()
            => $"__typeInferenceArg_{depth}_{parameterName.GetDebuggerDisplay()}";
    }

    protected class TypeInferenceMethodParameter
    {
        public SeqName? SeqName { get; }
        public string TypeName { get; }
        public ParameterName ParameterName { get; }
        public bool UsedForTypeInference { get; }
        public object Source { get; private set; }

        public TypeInferenceMethodParameter(SeqName? seqName, string typeName, ParameterName parameterName, bool usedForTypeInference, object source)
        {
            SeqName = seqName;
            TypeName = typeName;
            ParameterName = parameterName;
            UsedForTypeInference = usedForTypeInference;
            Source = source;
        }

        public void ReplaceSourceWithCapturedVariable(TypeInferenceArgName variableName)
        {
            Source = new TypeInferenceCapturedVariable(variableName);
        }
    }

    protected sealed class TypeInferenceCapturedVariable(TypeInferenceArgName variableName)
    {
        public TypeInferenceArgName VariableName { get; } = variableName;
    }
}
