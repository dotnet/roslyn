// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class DefaultTagHelperTargetExtension : IDefaultTagHelperTargetExtension
{
    private static readonly ImmutableArray<string> s_fieldUninitializedModifiers = ["0649"];
    private static readonly ImmutableArray<string> s_fieldUnusedModifiers = ["0169"];
    private static readonly ImmutableArray<string> s_privateModifiers = ["private"];

    public string RunnerVariableName { get; set; } = "__tagHelperRunner";

    public string StringValueBufferVariableName { get; set; } = "__tagHelperStringValueBuffer";

    public string CreateTagHelperMethodName { get; set; } = "CreateTagHelper";

    public string ExecutionContextTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext";

    public string ExecutionContextVariableName { get; set; } = "__tagHelperExecutionContext";

    public string ExecutionContextAddMethodName { get; set; } = "Add";

    public string TagHelperRunnerTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner";

    public string ExecutionContextOutputPropertyName { get; set; } = "Output";

    public string ExecutionContextSetOutputContentAsyncMethodName { get; set; } = "SetOutputContentAsync";

    public string ExecutionContextAddHtmlAttributeMethodName { get; set; } = "AddHtmlAttribute";

    public string ExecutionContextAddTagHelperAttributeMethodName { get; set; } = "AddTagHelperAttribute";

    public string RunnerRunAsyncMethodName { get; set; } = "RunAsync";

    public string ScopeManagerTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager";

    public string ScopeManagerVariableName { get; set; } = "__tagHelperScopeManager";

    public string ScopeManagerBeginMethodName { get; set; } = "Begin";

    public string ScopeManagerEndMethodName { get; set; } = "End";

    public string StartTagHelperWritingScopeMethodName { get; set; } = "StartTagHelperWritingScope";

    public string EndTagHelperWritingScopeMethodName { get; set; } = "EndTagHelperWritingScope";

    public string TagModeTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode";

    public string HtmlAttributeValueStyleTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle";

    public string TagHelperOutputIsContentModifiedPropertyName { get; set; } = "IsContentModified";

    public string BeginAddHtmlAttributeValuesMethodName { get; set; } = "BeginAddHtmlAttributeValues";

    public string EndAddHtmlAttributeValuesMethodName { get; set; } = "EndAddHtmlAttributeValues";

    public string BeginWriteTagHelperAttributeMethodName { get; set; } = "BeginWriteTagHelperAttribute";

    public string EndWriteTagHelperAttributeMethodName { get; set; } = "EndWriteTagHelperAttribute";

    public string MarkAsHtmlEncodedMethodName { get; set; } = "Html.Raw";

    public string FormatInvalidIndexerAssignmentMethodName { get; set; } = "InvalidTagHelperIndexerAssignment";

    public string WriteTagHelperOutputMethod { get; set; } = "Write";

    public void WriteTagHelperBody(CodeRenderingContext context, DefaultTagHelperBodyIntermediateNode node)
    {
        if (context.Parent as TagHelperIntermediateNode == null)
        {
            var message = Resources.FormatIntermediateNodes_InvalidParentNode(node.GetType(), typeof(TagHelperIntermediateNode));
            throw new InvalidOperationException(message);
        }

        if (context.Options.DesignTime)
        {
            context.RenderChildren(node);
        }
        else
        {
            // Call into the tag helper scope manager to start a new tag helper scope.
            // Also capture the value as the current execution context.
            context.CodeWriter
                .WriteStartAssignment(ExecutionContextVariableName)
                .WriteStartInstanceMethodInvocation(
                    ScopeManagerVariableName,
                    ScopeManagerBeginMethodName);

            // Assign a unique ID for this instance of the source HTML tag. This must be unique
            // per call site, e.g. if the tag is on the view twice, there should be two IDs.
            var uniqueId = GetDeterministicId(context);
            
            context.CodeWriter.WriteStringLiteral(node.TagName)
                .WriteParameterSeparator()
                .Write($"{TagModeTypeName}.{node.TagMode}")
                .WriteParameterSeparator()
                .WriteStringLiteral(uniqueId)
                .WriteParameterSeparator();

            using (context.CodeWriter.BuildAsyncLambda())
            {
                // We remove and redirect writers so TagHelper authors can retrieve content.
                context.RenderChildren(node, RuntimeNodeWriter.Instance);
            }

            context.CodeWriter.WriteEndMethodInvocation();
        }
    }

    public void WriteTagHelperCreate(CodeRenderingContext context, DefaultTagHelperCreateIntermediateNode node)
    {
        if (context.Parent as TagHelperIntermediateNode == null)
        {
            var message = Resources.FormatIntermediateNodes_InvalidParentNode(node.GetType(), typeof(TagHelperIntermediateNode));
            throw new InvalidOperationException(message);
        }

        context.CodeWriter
            .WriteStartAssignment(node.FieldName)
            .Write(CreateTagHelperMethodName)
            .WriteLine($"<global::{node.TypeName}>();");

        if (!context.Options.DesignTime)
        {
            context.CodeWriter.WriteInstanceMethodInvocation(
                ExecutionContextVariableName,
                ExecutionContextAddMethodName,
                node.FieldName);
        }
    }

    public void WriteTagHelperExecute(CodeRenderingContext context, DefaultTagHelperExecuteIntermediateNode node)
    {
        if (context.Parent as TagHelperIntermediateNode == null)
        {
            var message = Resources.FormatIntermediateNodes_InvalidParentNode(node.GetType(), typeof(TagHelperIntermediateNode));
            throw new InvalidOperationException(message);
        }

        // We always render `await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);` to notify users of the requirement for a method
        // to be asynchronous.

        context.CodeWriter
            .Write("await ")
            .WriteStartInstanceMethodInvocation(
                RunnerVariableName,
                RunnerRunAsyncMethodName)
            .Write(ExecutionContextVariableName)
            .WriteEndMethodInvocation();

        if (!context.Options.DesignTime)
        {
            var tagHelperOutputAccessor = $"{ExecutionContextVariableName}.{ExecutionContextOutputPropertyName}";

            context.CodeWriter
                .WriteLine($"if (!{tagHelperOutputAccessor}.{TagHelperOutputIsContentModifiedPropertyName})");

            using (context.CodeWriter.BuildScope())
            {
                context.CodeWriter
                    .Write("await ")
                    .WriteInstanceMethodInvocation(
                        ExecutionContextVariableName,
                        ExecutionContextSetOutputContentAsyncMethodName);
            }

            context.CodeWriter
                .WriteStartMethodInvocation(WriteTagHelperOutputMethod)
                .Write(tagHelperOutputAccessor)
                .WriteEndMethodInvocation()
                .WriteStartAssignment(ExecutionContextVariableName)
                .WriteInstanceMethodInvocation(
                    ScopeManagerVariableName,
                    ScopeManagerEndMethodName);
        }
    }

    public void WriteTagHelperHtmlAttribute(CodeRenderingContext context, DefaultTagHelperHtmlAttributeIntermediateNode node)
    {
        if (context.Parent as TagHelperIntermediateNode == null)
        {
            var message = Resources.FormatIntermediateNodes_InvalidParentNode(node.GetType(), typeof(TagHelperIntermediateNode));
            throw new InvalidOperationException(message);
        }

        if (context.Options.DesignTime)
        {
            context.RenderChildren(node);
        }
        else
        {
            var attributeValueStyleParameter = $"{HtmlAttributeValueStyleTypeName}.{node.AttributeStructure}";
            var isConditionalAttributeValue = node.Children.Any(
                child => child is CSharpExpressionAttributeValueIntermediateNode || child is CSharpCodeAttributeValueIntermediateNode);

            // All simple text and minimized attributes will be pre-allocated.
            if (isConditionalAttributeValue)
            {
                // Dynamic attribute value should be run through the conditional attribute removal system. It's
                // unbound and contains C#.

                // TagHelper attribute rendering is buffered by default. We do not want to write to the current
                // writer.
                var valuePieceCount = node.Children.Count(
                    child =>
                        child is HtmlAttributeValueIntermediateNode ||
                        child is CSharpExpressionAttributeValueIntermediateNode ||
                        child is CSharpCodeAttributeValueIntermediateNode ||
                        child is ExtensionIntermediateNode);

                context.CodeWriter
                    .WriteStartMethodInvocation(BeginAddHtmlAttributeValuesMethodName)
                    .Write(ExecutionContextVariableName)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.AttributeName)
                    .WriteParameterSeparator()
                    .WriteIntegerLiteral(valuePieceCount)
                    .WriteParameterSeparator()
                    .Write(attributeValueStyleParameter)
                    .WriteEndMethodInvocation();

                context.RenderChildren(node, TagHelperHtmlAttributeRuntimeNodeWriter.Instance);

                context.CodeWriter
                    .WriteMethodInvocation(
                        EndAddHtmlAttributeValuesMethodName,
                        ExecutionContextVariableName);
            }
            else
            {
                // This is a data-* attribute which includes C#. Do not perform the conditional attribute removal or
                // other special cases used when IsDynamicAttributeValue(). But the attribute must still be buffered to
                // determine its final value.

                // Attribute value is not plain text, must be buffered to determine its final value.
                context.CodeWriter.WriteMethodInvocation(BeginWriteTagHelperAttributeMethodName);

                // We're building a writing scope around the provided chunks which captures everything written from the
                // page. Therefore, we do not want to write to any other buffer since we're using the pages buffer to
                // ensure we capture all content that's written, directly or indirectly.
                context.RenderChildren(node, RuntimeNodeWriter.Instance);

                context.CodeWriter
                    .WriteStartAssignment(StringValueBufferVariableName)
                    .WriteMethodInvocation(EndWriteTagHelperAttributeMethodName)
                    .WriteStartInstanceMethodInvocation(
                        ExecutionContextVariableName,
                        ExecutionContextAddHtmlAttributeMethodName)
                    .WriteStringLiteral(node.AttributeName)
                    .WriteParameterSeparator()
                    .WriteStartMethodInvocation(MarkAsHtmlEncodedMethodName)
                    .Write(StringValueBufferVariableName)
                    .WriteEndMethodInvocation(endLine: false)
                    .WriteParameterSeparator()
                    .Write(attributeValueStyleParameter)
                    .WriteEndMethodInvocation();
            }
        }
    }

    public void WriteTagHelperProperty(CodeRenderingContext context, DefaultTagHelperPropertyIntermediateNode node)
    {
        var tagHelperNode = context.Parent as TagHelperIntermediateNode;
        if (context.Parent == null)
        {
            var message = Resources.FormatIntermediateNodes_InvalidParentNode(node.GetType(), typeof(TagHelperIntermediateNode));
            throw new InvalidOperationException(message);
        }

        if (!context.Options.DesignTime)
        {
            // Ensure that the property we're trying to set has initialized its dictionary bound properties.
            if (node.IsIndexerNameMatch &&
                object.ReferenceEquals(FindFirstUseOfIndexer(tagHelperNode, node), node))
            {
                // Throw a reasonable Exception at runtime if the dictionary property is null.
                context.CodeWriter
                    .WriteLine($"if ({node.FieldName}.{node.PropertyName} == null)");
                using (context.CodeWriter.BuildScope())
                {
                    // System is in Host.NamespaceImports for all MVC scenarios. No need to generate FullName
                    // of InvalidOperationException type.
                    context.CodeWriter
                        .Write("throw ")
                        .WriteStartNewObject(nameof(InvalidOperationException))
                        .WriteStartMethodInvocation(FormatInvalidIndexerAssignmentMethodName)
                        .WriteStringLiteral(node.AttributeName)
                        .WriteParameterSeparator()
                        .WriteStringLiteral(node.TagHelper.TypeName)
                        .WriteParameterSeparator()
                        .WriteStringLiteral(node.PropertyName)
                        .WriteEndMethodInvocation(endLine: false)   // End of method call
                        .WriteEndMethodInvocation();   // End of new expression / throw statement
                }
            }
        }

        // If this is not the first use of the attribute value, we need to evaluate the expression and assign it to
        // the tag helper property.
        //
        // Otherwise, the value has already been computed and assigned to another tag helper. We just need to
        // copy from that tag helper to this one.
        //
        // This is important because we can't evaluate the expression twice due to side-effects.
        var firstUseOfAttribute = FindFirstUseOfAttribute(tagHelperNode, node);
        if (!object.ReferenceEquals(firstUseOfAttribute, node))
        {
            // If we get here, this value has already been used. We just need to copy the value.
            WritePropertyAccessorStartAssignment(context.CodeWriter, node);

            WritePropertyAccessor(context.CodeWriter, firstUseOfAttribute)
                .WriteLine(";");

            return;
        }

        // If we get there, this is the first time seeing this property so we need to evaluate the expression.
        if (node.BoundAttribute.ExpectsStringValue(node.AttributeName))
        {
            if (context.Options.DesignTime)
            {
                context.RenderChildren(node);

                WritePropertyAccessorStartAssignment(context.CodeWriter, node);
                if (node.Children.Count == 1 && node.Children.First() is HtmlContentIntermediateNode htmlNode)
                {
                    var content = GetContent(htmlNode);
                    context.CodeWriter.WriteStringLiteral(content);
                }
                else
                {
                    context.CodeWriter.Write("string.Empty");
                }
                context.CodeWriter.WriteLine(";");
            }
            else
            {
                context.CodeWriter.WriteMethodInvocation(BeginWriteTagHelperAttributeMethodName);

                context.RenderChildren(node, LiteralRuntimeNodeWriter.Instance);

                context.CodeWriter
                    .WriteStartAssignment(StringValueBufferVariableName)
                    .WriteMethodInvocation(EndWriteTagHelperAttributeMethodName);

                WritePropertyAccessorStartAssignment(context.CodeWriter, node)
                    .WriteLine($"{StringValueBufferVariableName};");
            }
        }
        else
        {
            if (context.Options.DesignTime)
            {
                var firstMappedChild = node.Children.FirstOrDefault(child => child.Source != null) as IntermediateNode;
                var valueStart = firstMappedChild?.Source;

                using (context.BuildLinePragma(node.Source))
                {
                    var accessorLength = GetPropertyAccessorLength(node);
                    var assignmentPrefixLength = accessorLength + " = ".Length;
                    if (node.BoundAttribute.IsEnum &&
                        node.Children is [CSharpIntermediateToken token])
                    {
                        assignmentPrefixLength += $"global::{node.BoundAttribute.TypeName}.".Length;

                        if (valueStart != null)
                        {
                            context.CodeWriter.WritePadding(assignmentPrefixLength, node.Source, context);
                        }

                        WritePropertyAccessorStartAssignment(context.CodeWriter, node)
                            .Write($"global::{node.BoundAttribute.TypeName}.");
                    }
                    else
                    {
                        if (valueStart != null)
                        {
                            context.CodeWriter.WritePadding(assignmentPrefixLength, node.Source, context);
                        }

                        WritePropertyAccessorStartAssignment(context.CodeWriter, node);
                    }

                    if (node.Children.Count == 0 &&
                        node.AttributeStructure == AttributeStructure.Minimized &&
                        node.BoundAttribute.ExpectsBooleanValue(node.AttributeName))
                    {
                        // If this is a minimized boolean attribute, set the value to true.
                        context.CodeWriter.Write("true");
                    }
                    else
                    {
                        RenderTagHelperAttributeInline(context, node, node.Source);
                    }

                    context.CodeWriter.WriteLine(";");
                }
            }
            else
            {
                WritePropertyAccessorStartAssignment(context.CodeWriter, node);

                if (node.BoundAttribute.IsEnum &&
                    node.Children is [CSharpIntermediateToken token])
                {
                    context.CodeWriter
                        .Write($"global::{node.BoundAttribute.TypeName}.");
                }

                if (node.Children.Count == 0 &&
                    node.AttributeStructure == AttributeStructure.Minimized &&
                    node.BoundAttribute.ExpectsBooleanValue(node.AttributeName))
                {
                    // If this is a minimized boolean attribute, set the value to true.
                    context.CodeWriter.Write("true");
                }
                else
                {
                    RenderTagHelperAttributeInline(context, node, node.Source);
                }

                context.CodeWriter.WriteLine(";");
            }
        }

        if (!context.Options.DesignTime)
        {
            // We need to inform the context of the attribute value.
            context.CodeWriter
                .WriteStartInstanceMethodInvocation(
                    ExecutionContextVariableName,
                    ExecutionContextAddTagHelperAttributeMethodName)
                .WriteStringLiteral(node.AttributeName)
                .WriteParameterSeparator();

            WritePropertyAccessor(context.CodeWriter, node)
                .WriteParameterSeparator()
                .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.AttributeStructure}")
                .WriteEndMethodInvocation();
        }
    }

    public void WriteTagHelperRuntime(CodeRenderingContext context, DefaultTagHelperRuntimeIntermediateNode node)
    {
        context.CodeWriter.WriteLine("#line hidden");
        context.CodeWriter.WriteField(s_fieldUninitializedModifiers, s_privateModifiers, ExecutionContextTypeName, ExecutionContextVariableName);

        context.CodeWriter
            .WriteLine($"private {TagHelperRunnerTypeName} {RunnerVariableName} = new {TagHelperRunnerTypeName}();");

        if (!context.Options.DesignTime)
        {
            context.CodeWriter.WriteField(s_fieldUnusedModifiers, s_privateModifiers, "string", StringValueBufferVariableName);

            var backedScopeManageVariableName = "__backed" + ScopeManagerVariableName;
            context.CodeWriter
                .Write("private ")
                .WriteVariableDeclaration(
                    ScopeManagerTypeName,
                    backedScopeManageVariableName,
                    value: null);

            context.CodeWriter
                .WriteLine($"private {ScopeManagerTypeName} {ScopeManagerVariableName}");

            using (context.CodeWriter.BuildScope())
            {
                context.CodeWriter.WriteLine("get");
                using (context.CodeWriter.BuildScope())
                {
                    context.CodeWriter
                        .WriteLine($"if ({backedScopeManageVariableName} == null)");

                    using (context.CodeWriter.BuildScope())
                    {
                        context.CodeWriter
                            .WriteStartAssignment(backedScopeManageVariableName)
                            .WriteStartNewObject(ScopeManagerTypeName)
                            .Write(StartTagHelperWritingScopeMethodName)
                            .WriteParameterSeparator()
                            .Write(EndTagHelperWritingScopeMethodName)
                            .WriteEndMethodInvocation();
                    }

                    context.CodeWriter
                        .WriteLine($"return {backedScopeManageVariableName};");
                }
            }
        }
    }

    private void RenderTagHelperAttributeInline(
        CodeRenderingContext context,
        DefaultTagHelperPropertyIntermediateNode property,
        SourceSpan? span)
    {
        for (var i = 0; i < property.Children.Count; i++)
        {
            RenderTagHelperAttributeInline(context, property, property.Children[i], span);
        }
    }

    // Internal for testing
    internal void RenderTagHelperAttributeInline(
        CodeRenderingContext context,
        DefaultTagHelperPropertyIntermediateNode property,
        IntermediateNode node,
        SourceSpan? span)
    {
        if (node is CSharpExpressionIntermediateNode || node is HtmlContentIntermediateNode)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                RenderTagHelperAttributeInline(context, property, node.Children[i], span);
            }
        }
        else if (node is IntermediateToken token)
        {
            if (context.Options.DesignTime)
            {
                if (node.Source != null)
                {
                    context.AddSourceMappingFor(node);
                }

                context.CodeWriter.Write(token.Content);
            }
            else
            {
                using (context.BuildEnhancedLinePragma(token.Source))
                {
                    context.CodeWriter.Write(token.Content);
                }
            }
        }
        else if (node is CSharpCodeIntermediateNode)
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_CodeBlocksNotSupportedInAttributes(span);
            context.AddDiagnostic(diagnostic);
        }
        else if (node is TemplateIntermediateNode)
        {
            var expectedTypeName = property.IsIndexerNameMatch ? property.BoundAttribute.IndexerTypeName : property.BoundAttribute.TypeName;
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InlineMarkupBlocksNotSupportedInAttributes(span, expectedTypeName);
            context.AddDiagnostic(diagnostic);
        }
    }

    private static DefaultTagHelperPropertyIntermediateNode FindFirstUseOfIndexer(
        TagHelperIntermediateNode tagHelperNode,
        DefaultTagHelperPropertyIntermediateNode propertyNode)
    {
        Debug.Assert(tagHelperNode.Children.Contains(propertyNode));
        Debug.Assert(propertyNode.IsIndexerNameMatch);

        for (var i = 0; i < tagHelperNode.Children.Count; i++)
        {
            if (tagHelperNode.Children[i] is DefaultTagHelperPropertyIntermediateNode otherPropertyNode &&
                otherPropertyNode.TagHelper.Equals(propertyNode.TagHelper) &&
                otherPropertyNode.BoundAttribute.Equals(propertyNode.BoundAttribute) &&
                otherPropertyNode.IsIndexerNameMatch)
            {
                return otherPropertyNode;
            }
        }

        // This is unreachable, we should find 'propertyNode' in the list of children.
        throw new InvalidOperationException();
    }

    private static DefaultTagHelperPropertyIntermediateNode FindFirstUseOfAttribute(
        TagHelperIntermediateNode tagHelperNode,
        DefaultTagHelperPropertyIntermediateNode propertyNode)
    {
        for (var i = 0; i < tagHelperNode.Children.Count; i++)
        {
            if (tagHelperNode.Children[i] is DefaultTagHelperPropertyIntermediateNode otherPropertyNode &&
                string.Equals(otherPropertyNode.AttributeName, propertyNode.AttributeName, StringComparison.Ordinal))
            {
                return otherPropertyNode;
            }
        }

        // This is unreachable, we should find 'propertyNode' in the list of children.
        throw new InvalidOperationException();
    }

    private string GetContent(HtmlContentIntermediateNode node)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < node.Children.Count; i++)
        {
            if (node.Children[i] is HtmlIntermediateToken token)
            {
                builder.Append(token.Content);
            }
        }

        return builder.ToString();
    }

    // Internal for testing
    internal static string GetDeterministicId(CodeRenderingContext context)
    {
        var uniqueId = context.Options.SuppressUniqueIds;
        if (uniqueId is null)
        {
            // Use the file checksum along with the absolute position in the generated code to create a unique id for each tag helper call site.
            var checksum = ChecksumUtilities.BytesToString(context.SourceDocument.Text.GetChecksum());
            uniqueId = checksum + context.CodeWriter.Location.AbsoluteIndex;
        }
        return uniqueId;
    }

    private static int GetPropertyAccessorLength(DefaultTagHelperPropertyIntermediateNode node)
    {
        var propertyAccessorLength =
            node.FieldName.Length
            + ".".Length
            + node.PropertyName.Length;

        if (node.IsIndexerNameMatch)
        {
            propertyAccessorLength +=
                "[\"".Length
                + (node.AttributeName.Length - node.BoundAttribute.IndexerNamePrefix.Length)
                + "\"]".Length;
        }

        return propertyAccessorLength;
    }

    private static CodeWriter WritePropertyAccessor(CodeWriter writer, DefaultTagHelperPropertyIntermediateNode node)
    {
        writer
            .Write($"{node.FieldName}.{node.PropertyName}");

        if (node.IsIndexerNameMatch)
        {
            var dictionaryKey = node.AttributeName.AsMemory()[node.BoundAttribute.IndexerNamePrefix.Length..];

            writer
                .Write($"[\"{dictionaryKey}\"]");
        }

        return writer;
    }

    private static CodeWriter WritePropertyAccessorStartAssignment(CodeWriter writer, DefaultTagHelperPropertyIntermediateNode node)
    {
        return WritePropertyAccessor(writer, node)
            .Write(" = ");
    }
}
