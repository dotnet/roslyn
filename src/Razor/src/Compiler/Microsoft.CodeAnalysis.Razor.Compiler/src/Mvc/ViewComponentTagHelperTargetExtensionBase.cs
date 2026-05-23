// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

internal abstract class ViewComponentTagHelperTargetExtensionBase : IViewComponentTagHelperTargetExtension
{
    private static readonly ImmutableArray<MethodParameter> s_constructorParameters =
    [
        new("helper", ViewComponentsApi.IViewComponentHelper.GloballyQualifiedTypeName)
    ];

    private static readonly string s_taskTypeName = $"global::{typeof(Task).FullName}";

    private ImmutableArray<MethodParameter> _processMethodParameters;

    public ImmutableArray<MethodParameter> ProcessMethodParameters
    {
        get
        {
            if (_processMethodParameters.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _processMethodParameters,
                    [
                        new(TagHelperContextVariableName, ViewComponentsApi.TagHelperContext.FullTypeName),
                        new(TagHelperOutputVariableName, ViewComponentsApi.TagHelperOutput.FullTypeName)
                    ]);
            }

            return _processMethodParameters;
        }
    }

    private static readonly ImmutableArray<string> s_contextualizeArguments = [ViewComponentsApi.ViewContextPropertyName];

    protected abstract string TagHelperContentVariableName { get; }
    protected abstract string TagHelperContextVariableName { get; }
    protected abstract string TagHelperOutputVariableName { get; }
    protected abstract string ViewComponentHelperVariableName { get; }

    public void WriteViewComponentTagHelper(CodeRenderingContext context, ViewComponentTagHelperIntermediateNode node)
    {
        // Add target element.
        WriteTargetElementString(context.CodeWriter, node.TagHelper);

        // Initialize declaration.
        using (context.CodeWriter.BuildClassDeclaration(
            CommonModifiers.Public,
            node.ClassName,
            new BaseTypeWithModel(ViewComponentsApi.TagHelper.FullTypeName),
            interfaces: default,
            typeParameters: default,
            context))
        {
            // Add view component helper.
            context.CodeWriter.WriteFieldDeclaration(
                CommonModifiers.PrivateReadOnly,
                ViewComponentsApi.IViewComponentHelper.GloballyQualifiedTypeName,
                ViewComponentHelperVariableName);

            // Add constructor.
            WriteConstructorString(context.CodeWriter, node.ClassName);

            // Add attributes.
            WriteAttributeDeclarations(context.CodeWriter, node.TagHelper);

            // Add process method.
            WriteProcessMethodString(context.CodeWriter, node.TagHelper);

            WriteAdditionalMembers(context, node);
        }
    }

    protected virtual void WriteAdditionalMembers(CodeRenderingContext context, ViewComponentTagHelperIntermediateNode node)
    {
    }

    protected abstract ImmutableArray<string> GetInvokeArguments(TagHelperDescriptor tagHelper);

    private static void WriteTargetElementString(CodeWriter writer, TagHelperDescriptor tagHelper)
    {
        Debug.Assert(tagHelper.TagMatchingRules.Length == 1);

        var rule = tagHelper.TagMatchingRules[0];

        writer.Write("[")
            .WriteStartMethodInvocation(ViewComponentsApi.HtmlTargetElementAttribute.FullTypeName)
            .WriteStringLiteral(rule.TagName)
            .WriteLine(")]");
    }

    private void WriteConstructorString(CodeWriter writer, string className)
    {
        using (writer.BuildConstructorDeclaration(CommonModifiers.Public, className, s_constructorParameters))
        {
            writer.WriteStartAssignment(ViewComponentHelperVariableName)
                .WriteLine("helper;");
        }
    }

    private static void WriteAttributeDeclarations(CodeWriter writer, TagHelperDescriptor tagHelper)
    {
        writer.Write("[")
          .Write(ViewComponentsApi.HtmlAttributeNotBoundAttribute.FullTypeName)
          .WriteParameterSeparator()
          .Write(ViewComponentsApi.ViewContextAttribute.GloballyQualifiedTypeName)
          .WriteLine("]");

        writer.WriteAutoPropertyDeclaration(
            CommonModifiers.Public,
            ViewComponentsApi.ViewContext.GloballyQualifiedTypeName,
            ViewComponentsApi.ViewContextPropertyName);

        foreach (var attribute in tagHelper.BoundAttributes)
        {
            writer.WriteAutoPropertyDeclaration(
                CommonModifiers.Public,
                attribute.TypeName,
                attribute.PropertyName);

            if (attribute.IndexerTypeName != null)
            {
                writer.Write(" = ")
                    .WriteStartNewObject(attribute.TypeName)
                    .WriteEndMethodInvocation();
            }
        }
    }

    private void WriteProcessMethodString(CodeWriter writer, TagHelperDescriptor tagHelper)
    {
        using (writer.BuildMethodDeclaration(
            CommonModifiers.PublicOverrideAsync,
            returnType: s_taskTypeName,
            ViewComponentsApi.ProcessAsyncMethodName,
            parameters: ProcessMethodParameters))
        {
            writer.WriteMethodInvocation(
                $"({ViewComponentHelperVariableName} as {ViewComponentsApi.IViewContextAware.GloballyQualifiedTypeName})?.{ViewComponentsApi.IViewContextAware.ContextualizeMethodName}",
                s_contextualizeArguments);

            var invokeArguments = GetInvokeArguments(tagHelper);

            writer.Write("var ")
                .WriteStartAssignment(TagHelperContentVariableName)
                .Write("await ")
                .WriteMethodInvocation($"{ViewComponentHelperVariableName}.{ViewComponentsApi.IViewComponentHelper.InvokeMethodName}", invokeArguments);

            writer.WriteStartAssignment($"{TagHelperOutputVariableName}.{ViewComponentsApi.TagHelperOutput.TagNamePropertyName}")
                .WriteLine("null;");

            writer.WriteMethodInvocation(
                $"{TagHelperOutputVariableName}.{ViewComponentsApi.TagHelperOutput.ContentPropertyName}.{ViewComponentsApi.TagHelperOutput.ContentSetMethodName}",
                arguments: [TagHelperContentVariableName]);
        }
    }
}
