// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.QuickInfo;

public static class ToolTipAssert
{
    public static void EqualContent(object expected, object? actual)
    {
        try
        {
            Assert.IsType(expected.GetType(), actual);

            if (expected is ContainerElement containerElement)
            {
                EqualContainerElement(containerElement, (ContainerElement)actual!);
                return;
            }

            if (expected is ImageElement imageElement)
            {
                EqualImageElement(imageElement, (ImageElement)actual!);
                return;
            }

            if (expected is ClassifiedTextElement classifiedTextElement)
            {
                EqualClassifiedTextElement(classifiedTextElement, (ClassifiedTextElement)actual!);
                return;
            }

            if (expected is ClassifiedTextRun classifiedTextRun)
            {
                EqualClassifiedTextRun(classifiedTextRun, (ClassifiedTextRun)actual!);
                return;
            }

            throw ExceptionUtilities.Unreachable();
        }
        catch (Exception)
        {
            var renderedExpected = ContainerToString(expected);
            var renderedActual = ContainerToString(actual!);
            AssertEx.EqualOrDiff(renderedExpected, renderedActual);

            // This is not expected to be hit, but it will be hit if the difference cannot be detected within the diff
            throw;
        }
    }

    private static void EqualContainerElement(ContainerElement expected, ContainerElement actual)
    {
        Assert.Equal(expected.Style, actual.Style);
        Assert.Equal(expected.Elements.Count(), actual.Elements.Count());
        foreach (var (expectedElement, actualElement) in expected.Elements.Zip(actual.Elements, (expectedElement, actualElement) => (expectedElement, actualElement)))
        {
            EqualContent(expectedElement, actualElement);
        }
    }

    private static void EqualImageElement(ImageElement expected, ImageElement actual)
    {
        Assert.Equal(expected.ImageId.Guid, actual.ImageId.Guid);
        Assert.Equal(expected.ImageId.Id, actual.ImageId.Id);
        Assert.Equal(expected.AutomationName, actual.AutomationName);
    }

    private static void EqualClassifiedTextElement(ClassifiedTextElement expected, ClassifiedTextElement actual)
    {
        Assert.Equal(expected.Runs.Count(), actual.Runs.Count());
        foreach (var (expectedRun, actualRun) in expected.Runs.Zip(actual.Runs, (expectedRun, actualRun) => (expectedRun, actualRun)))
        {
            EqualClassifiedTextRun(expectedRun, actualRun);
        }
    }

    private static void EqualClassifiedTextRun(ClassifiedTextRun expected, ClassifiedTextRun actual)
    {
        Assert.Equal(expected.ClassificationTypeName, actual.ClassificationTypeName);
        Assert.Equal(expected.Text, actual.Text);
        Assert.Equal(expected.Tooltip, actual.Tooltip);
        Assert.Equal(expected.Style, actual.Style);

        if (expected.NavigationAction is null)
        {
            Assert.Equal(expected.NavigationAction, actual.NavigationAction);
        }
        else if (expected.NavigationAction.Target is QuickInfoHyperLink hyperLink)
        {
            Assert.Same(expected.NavigationAction, hyperLink.NavigationAction);
            var actualTarget = Assert.IsType<QuickInfoHyperLink>(actual.NavigationAction.Target);
            Assert.Same(actual.NavigationAction, actualTarget.NavigationAction);
            Assert.Equal(hyperLink, actualTarget);
        }
        else
        {
            // Cannot validate this navigation action
            Assert.NotNull(actual.NavigationAction);
            Assert.IsNotType<QuickInfoHyperLink>(actual.NavigationAction.Target);
        }
    }

    private static string ContainerToString(object element)
    {
        var result = new StringBuilder();
        ContainerToString(element, "", result);
        return result.ToString();
    }

    private static void ContainerToString(object element, string indent, StringBuilder result)
    {
        result.Append($"{indent}New {element.GetType().Name}(");

        if (element is ContainerElement container)
        {
            result.AppendLine();
            indent += "    ";
            result.AppendLine($"{indent}{ContainerStyleToString(container.Style)},");
            var elements = container.Elements.ToArray();
            for (var i = 0; i < elements.Length; i++)
            {
                ContainerToString(elements[i], indent, result);

                if (i < elements.Length - 1)
                    result.AppendLine(",");
                else
                    result.Append(')');
            }

            return;
        }

        if (element is ImageElement image)
        {
            var guid = GetKnownImageGuid(image.ImageId.Guid);
            var id = GetKnownImageId(image.ImageId.Id);
            result.Append($"New {nameof(ImageId)}({guid}, {id}))");
            return;
        }

        if (element is ClassifiedTextElement classifiedTextElement)
        {
            result.AppendLine();
            indent += "    ";
            var runs = classifiedTextElement.Runs.ToArray();
            for (var i = 0; i < runs.Length; i++)
            {
                ContainerToString(runs[i], indent, result);

                if (i < runs.Length - 1)
                    result.AppendLine(",");
                else
                    result.Append(')');
            }

            return;
        }

        if (element is ClassifiedTextRun classifiedTextRun)
        {
            var classification = GetKnownClassification(classifiedTextRun.ClassificationTypeName);
            result.Append($"""
                {classification}, "{classifiedTextRun.Text.Replace("\"", "\"\"")}"
                """);
            if (classifiedTextRun.NavigationAction is object || !string.IsNullOrEmpty(classifiedTextRun.Tooltip))
            {
                var tooltip = classifiedTextRun.Tooltip is object ? $"""
                    "{classifiedTextRun.Tooltip.Replace("\"", "\"\"")}"
                    """ : "Nothing";
                if (classifiedTextRun.NavigationAction?.Target is QuickInfoHyperLink hyperLink)
                {
                    result.Append($", QuickInfoHyperLink.TestAccessor.CreateNavigationAction(new Uri(\"{hyperLink.Uri}\", UriKind.Absolute))");
                }
                else
                {
                    result.Append(", navigationAction:=Sub() Return");
                }

                result.Append($", {tooltip}");
            }

            if (classifiedTextRun.Style != ClassifiedTextRunStyle.Plain)
            {
                result.Append($", {TextRunStyleToString(classifiedTextRun.Style)}");
            }

            result.Append(')');
            return;
        }

        throw ExceptionUtilities.Unreachable();
    }

    private static string ContainerStyleToString(ContainerElementStyle style)
    {
        var stringValue = style.ToString();
        return string.Join(" Or ", stringValue.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries).Select(value => $"{nameof(ContainerElementStyle)}.{value}"));
    }

    private static string TextRunStyleToString(ClassifiedTextRunStyle style)
    {
        var stringValue = style.ToString();
        return string.Join(" Or ", stringValue.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries).Select(value => $"{nameof(ClassifiedTextRunStyle)}.{value}"));
    }

    private static string GetKnownClassification(string classification)
    {
        foreach (var field in typeof(ClassificationTypeNames).GetFields())
        {
            if (!field.IsStatic)
                continue;

            var value = field.GetValue(null) as string;
            if (value == classification)
                return $"{nameof(ClassificationTypeNames)}.{field.Name}";
        }

        return $"""
            "{classification}"
            """;
    }

    private static string GetKnownImageGuid(Guid guid)
    {
        foreach (var field in typeof(KnownImageIds).GetFields())
        {
            if (!field.IsStatic)
                continue;

            var value = field.GetValue(null) as Guid?;
            if (value == guid)
                return $"{nameof(KnownImageIds)}.{field.Name}";
        }

        return guid.ToString();
    }

    private static string GetKnownImageId(int id)
    {
        foreach (var field in typeof(KnownImageIds).GetFields())
        {
            if (!field.IsStatic)
                continue;

            var value = field.GetValue(null) as int?;
            if (value == id)
                return $"{nameof(KnownImageIds)}.{field.Name}";
        }

        return id.ToString();
    }
}
