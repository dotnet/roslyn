// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

internal sealed class ViewComponentTagHelperTargetExtension : ViewComponentTagHelperTargetExtensionBase
{
    private ImmutableArray<MethodParameter> _processInvokeAsyncArgsMethodParameters;

    private ImmutableArray<MethodParameter> ProcessInvokeAsyncArgsMethodParameters
    {
        get
        {
            if (_processInvokeAsyncArgsMethodParameters.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _processInvokeAsyncArgsMethodParameters,
                    [new(TagHelperContextVariableName, ViewComponentsApi.TagHelperContext.FullTypeName)]);
            }

            return _processInvokeAsyncArgsMethodParameters;
        }
    }

    protected override string TagHelperContentVariableName => "__helperContent";
    protected override string TagHelperContextVariableName => "__context";
    protected override string TagHelperOutputVariableName => "__output";
    protected override string ViewComponentHelperVariableName => "__helper";

    protected override void WriteAdditionalMembers(CodeRenderingContext context, ViewComponentTagHelperIntermediateNode node)
    {
        // We pre-process the arguments passed to `InvokeAsync` to ensure that the
        // provided markup attributes (in kebab-case) are matched to the associated
        // properties in the VCTH class.
        WriteProcessInvokeAsyncArgsMethodString(context.CodeWriter, node.TagHelper);
    }

    private void WriteProcessInvokeAsyncArgsMethodString(CodeWriter writer, TagHelperDescriptor tagHelper)
    {
        const string DictionaryType = "Dictionary<string, object>";

        using (writer.BuildMethodDeclaration(
            CommonModifiers.Private,
            DictionaryType,
            ViewComponentsApi.ProcessInvokeAsyncArgsMethodName,
            ProcessInvokeAsyncArgsMethodParameters))
        {
            writer.WriteStartAssignment($"{DictionaryType} args")
                .WriteStartNewObject(DictionaryType)
                .WriteEndMethodInvocation();

            foreach (var attribute in tagHelper.BoundAttributes)
            {
                var attributeName = attribute.Name;
                var parameterName = attribute.PropertyName;

                writer.WriteLine($"if (__context.AllAttributes.ContainsName(\"{attributeName}\"))");
                using (writer.BuildScope())
                {
                    writer.WriteLine($"args[nameof({parameterName})] = {parameterName};");
                }
            }

            writer.WriteLine("return args;");
        }
    }

    protected override ImmutableArray<string> GetInvokeArguments(TagHelperDescriptor tagHelper)
    {
        var viewComponentName = tagHelper.ViewComponentName;

        return [$"\"{viewComponentName}\"", $"{ViewComponentsApi.ProcessInvokeAsyncArgsMethodName}({TagHelperContextVariableName})"];
    }
}
