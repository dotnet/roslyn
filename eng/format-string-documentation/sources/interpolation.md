---
title: "$ - string interpolation - format string output"
description: String interpolation using the `$` token provides a more readable and convenient syntax to format string output than traditional string composite formatting.
ms.date: 01/14/2026
f1_keywords:
    - "$_CSharpKeyword"
    - "$"
helpviewer_keywords:
    - "$ special character [C#]"
    - "string interpolation [C#]"
    - "interpolated string [C#]"
author: pkulikov
---

# String interpolation using `$`

Use the `$` character to identify a string literal as an _interpolated string_. An interpolated string is a string literal that might contain _interpolation expressions_. When you resolve an interpolated string to a result string, the compiler replaces items with interpolation expressions by the string representations of the expression results.

[!INCLUDE[csharp-version-note](../includes/initial-version.md)]

String interpolation provides a more readable, convenient syntax to format strings. It's easier to read than [string composite formatting](../../../standard/base-types/composite-formatting.md). The following example uses both features to produce the same output:

:::code language="csharp" source="./snippets/string-interpolation.cs" id="CompareWithCompositeFormatting":::

You can use an interpolated string to initialize a [constant](../keywords/const.md) string. You can do that only if all interpolation expressions within the interpolated string are constant strings as well.

## Structure of an interpolated string

To make a string literal an interpolated string, add the `$` symbol at the beginning. Don't include any white space between the `$` and the `"` that starts a string literal.

The structure of an item with an interpolation expression is as follows:

```csharp
{<interpolationExpression>[,<width>][:<formatString>]}
```

Elements in square brackets are optional. The following table describes each element:

| Element                   | Description |
|---------------------------|-------------|
| `interpolationExpression` | The expression that produces a result to be formatted. When the expression is `null`, the output is the empty string (<xref:System.String.Empty?displayProperty=nameWithType>). |
| `width`               | The constant expression whose value defines the minimum number of characters in the string representation of the expression result. If positive, the string representation is right-aligned; if negative, left-aligned. For more information, see the [Width component](../../../standard/base-types/composite-formatting.md#width-component) section of the [Composite formatting](../../../standard/base-types/composite-formatting.md) article. |
| `formatString`            | A format string supported by the type of the expression result. For more information, see the [Format string component](../../../standard/base-types/composite-formatting.md#format-string-component) section of the [Composite formatting](../../../standard/base-types/composite-formatting.md) article. |

The following example uses optional formatting components described in the preceding table:

:::code language="csharp" source="./snippets/string-interpolation.cs" id="AlignAndSpecifyFormat":::

To make the code more readable, use new lines within an interpolation expression. The following example shows how new lines can improve the readability of an expression involving [pattern matching](../operators/patterns.md):

:::code language="csharp" source="./snippets/string-interpolation.cs" id="MultiLineExpression":::

## Interpolated raw string literals

You can use an interpolated [raw string literal](../builtin-types/reference-types.md#string-literals), as the following example shows:

:::code language="csharp" source="./snippets/string-interpolation.cs" id="InterpolatedRawStringLiteral":::

To embed `{` and `}` characters in the result string, start an interpolated raw string literal with multiple `$` characters. When you do that, any sequence of `{` or `}` characters shorter than the number of `$` characters is embedded in the result string. To enclose any interpolation expression within that string, you need to use the same number of braces as the number of `$` characters, as the following example shows:

:::code language="csharp" source="./snippets/string-interpolation.cs" id="InterpolatedRawStringLiteralWithBraces":::

In the preceding example, an interpolated raw string literal starts with two `$` characters. You need to put every interpolation expression between double braces (`{{` and `}}`). A single brace is embedded into a result string. If you need to embed repeated `{` or `}` characters into a result string, use an appropriately greater number of `$` characters to designate an interpolated raw string literal. If the string literal has more repeated braces than the number of `$` characters, the `{` and `}` characters are grouped from inside to outside. In the preceding example, the literal `The point {{{X}}, {{Y}}}` interprets `{{X}}` and `{{Y}}` as interpolated expressions. The outer `{` and `}` are included verbatim in the output string.

[!INCLUDE[raw-string-tip](../../includes/raw-string-parsing.md)]

## Special characters

To include a brace, "{" or "}", in the text produced by an interpolated string, use two braces, "{{" or "}}". For more information, see the [Escaping braces](../../../standard/base-types/composite-formatting.md#escaping-braces) section of the [Composite formatting](../../../standard/base-types/composite-formatting.md) article.

The colon (":") has special meaning in an interpolation expression item. To use a [conditional operator](../operators/conditional-operator.md) in an interpolation expression, enclose that expression in parentheses.

The following example shows how to include a brace in a result string. It also shows how to use a conditional operator:

:::code language="csharp" source="./snippets/string-interpolation.cs" id="BraceAndConditional":::

An interpolated [verbatim](verbatim.md) string starts with both the `$` and `@` characters. You can use `$` and `@` in any order: both `$@"..."` and `@$"..."` are valid interpolated verbatim strings. For more information about verbatim strings, see the [string](../builtin-types/reference-types.md) and [verbatim identifier](verbatim.md) articles.

## Culture-specific formatting

By default, an interpolated string uses the current culture defined by the <xref:System.Globalization.CultureInfo.CurrentCulture?displayProperty=nameWithType> property for all formatting operations.

To resolve an interpolated string to a culture-specific result string, use the <xref:System.String.Create(System.IFormatProvider,System.Runtime.CompilerServices.DefaultInterpolatedStringHandler%40)?displayProperty=nameWithType> method, which is available beginning with .NET 6. The following example shows how to do that:

:::code language="csharp" source="./snippets/string-interpolation.cs" id="CultureSpecific":::

In .NET 5 and earlier versions of .NET, use implicit conversion of an interpolated string to a <xref:System.FormattableString> instance. Then, you can use an instance <xref:System.FormattableString.ToString(System.IFormatProvider)?displayProperty=nameWithType> method or a static <xref:System.FormattableString.Invariant*?displayProperty=nameWithType> method to produce a culture-specific result string. The following example shows how to do that:

:::code language="csharp" source="./snippets/string-interpolation.cs" id="CultureSpecificByFormattableString":::

For more information about custom formatting, see the [Custom formatting with ICustomFormatter](../../../standard/base-types/formatting-types.md#custom-formatting-with-icustomformatter) section of the [Formatting types in .NET](../../../standard/base-types/formatting-types.md) article.

## Other resources

If you're new to string interpolation, see the [String interpolation in C#](../../fundamentals/tutorials/string-interpolation.md) tutorial. That tutorial demonstrates how to use interpolated strings to produce formatted strings.

## Compilation of interpolated strings

The compiler checks if an interpolated string is assigned to a type that satisfies the [_interpolated string handler pattern_](~/_csharpstandard/standard/attributes.md#235111-custom-interpolated-string-expression-handlers). An _interpolated string handler_ is a type that converts the interpolated string into a result string. When an interpolated string has the type `string`, the <xref:System.Runtime.CompilerServices.DefaultInterpolatedStringHandler?displayProperty=fullName> processes it. For the example of a custom interpolated string handler, see the [Write a custom string interpolation handler](../../advanced-topics/performance/interpolated-string-handler.md) tutorial. Use of an interpolated string handler is an advanced scenario, typically required for performance reasons.

> [!NOTE]
> One side effect of interpolated string handlers is that a custom handler, including <xref:System.Runtime.CompilerServices.DefaultInterpolatedStringHandler?displayProperty=nameWithType>, might not evaluate all the interpolation expressions within the interpolated string under all conditions. That behavior means side effects of those expressions might not occur.

If an interpolated string has the type `string`, the compiler typically transforms it into a <xref:System.String.Format*?displayProperty=nameWithType> method call. The compiler can replace <xref:System.String.Format*?displayProperty=nameWithType> with <xref:System.String.Concat*?displayProperty=nameWithType> if the analyzed behavior would be equivalent to concatenation.

If an interpolated string has the type <xref:System.IFormattable> or <xref:System.FormattableString>, the compiler generates a call to the <xref:System.Runtime.CompilerServices.FormattableStringFactory.Create*?displayProperty=nameWithType> method.

## C# language specification

For more information, see the [Interpolated string expressions](~/_csharpstandard/standard/expressions.md#1283-interpolated-string-expressions) and [custom interpolated string expression handlers](~/_csharpstandard/standard/attributes.md#235111-custom-interpolated-string-expression-handlers) sections of the [C# language specification](~/_csharpstandard/standard/README.md). See also [String literals](~/_csharpstandard/standard/lexical-structure.md#6456-string-literals) for raw string literals.

## See also

- [C# special characters](index.md)
- [Strings](../../programming-guide/strings/index.md)
- [Standard numeric format strings](../../../standard/base-types/standard-numeric-format-strings.md)
- [Composite formatting](../../../standard/base-types/composite-formatting.md)
- <xref:System.String.Format*?displayProperty=nameWithType>
- [Simplify interpolation (style rule IDE0071)](../../../fundamentals/code-analysis/style-rules/ide0071.md)
- [String interpolation in C# 10 and .NET 6 (.NET blog)](https://devblogs.microsoft.com/dotnet/string-interpolation-in-c-10-and-net-6/)
