---
title: "Enumeration format strings"
description: Create enumeration format strings using the Enum.ToString method in .NET. Format numeric, hexadecimal, or string values of enumeration members.
ms.date: 06/19/2023
dev_langs:
  - "csharp"
  - "vb"
helpviewer_keywords:
  - "format specifiers, enumeration format strings"
  - "enumeration format strings"
  - "formatting [.NET], enumeration"
ms.assetid: dd1ff672-1052-42cf-8666-4924fb6cd1a1
---

# Enumeration format strings

You can use the <xref:System.Enum.ToString*?displayProperty=nameWithType> method to create a new string object that represents the numeric, hexadecimal, or string value of an enumeration member. This method takes one of the enumeration formatting strings to specify the value that you want returned.

The following sections list the enumeration formatting strings and the values they return. These format specifiers aren't case-sensitive.

## G or g

Displays the enumeration entry as a string value, if possible, and otherwise displays the integer value of the current instance. If the enumeration is defined with the <xref:System.FlagsAttribute> set, the string values of each valid entry are concatenated together, separated by commas. If the `Flags` attribute isn't set, an invalid value is displayed as a numeric entry. The following example illustrates the `G` format specifier.

[!code-csharp[Formatting.Enum#1](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.Enum/cs/enum1.cs#1)]
[!code-vb[Formatting.Enum#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.Enum/vb/enum1.vb#1)]

## F or f

Displays the enumeration entry as a string value, if possible. If the value can be displayed as a summation of the entries in the enumeration (even if the `Flags` attribute isn't present), the string values of each valid entry are concatenated together, separated by commas. If the value can't be determined by the enumeration entries, then the value is formatted as the integer value. The following example illustrates the `F` format specifier.

[!code-csharp[Formatting.Enum#2](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.Enum/cs/enum1.cs#2)]
[!code-vb[Formatting.Enum#2](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.Enum/vb/enum1.vb#2)]

## D or d

Displays the enumeration entry as an integer value in the shortest representation possible. The following example illustrates the `D` format specifier.

[!code-csharp[Formatting.Enum#3](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.Enum/cs/enum1.cs#3)]
[!code-vb[Formatting.Enum#3](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.Enum/vb/enum1.vb#3)]

## X or x

Displays the enumeration entry as a hexadecimal value. The value is represented with leading zeros as necessary, to ensure that the result string has two characters for each byte in the enumeration type's [underlying numeric type](xref:System.Enum.GetUnderlyingType%2A). The following example illustrates the X format specifier. In the example, the underlying types of <xref:System.DayOfWeek>, <xref:System.ConsoleColor> and <xref:System.IO.FileAttributes> is <xref:System.Int32>, or a 32-bit (or 4-byte) integer, which produces an 8-character result string.

[!code-csharp[Formatting.Enum#4](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.Enum/cs/enum1.cs#4)]
[!code-vb[Formatting.Enum#4](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.Enum/vb/enum1.vb#4)]

## Example

The following example defines an enumeration called `Colors` that consists of three entries: `Red`, `Blue`, and `Green`.

[!code-csharp[Formatting.Enum#5](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.Enum/cs/enum1.cs#5)]
[!code-vb[Formatting.Enum#5](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.Enum/vb/enum1.vb#5)]

After the enumeration is defined, an instance can be declared in the following manner.

[!code-csharp[Formatting.Enum#6](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.Enum/cs/enum1.cs#6)]
[!code-vb[Formatting.Enum#6](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.Enum/vb/enum1.vb#6)]

The `Color.ToString(System.String)` method can then be used to display the enumeration value in different ways, depending on the format specifier passed to it.

[!code-csharp[Formatting.Enum#7](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.Enum/cs/enum1.cs#7)]
[!code-vb[Formatting.Enum#7](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.Enum/vb/enum1.vb#7)]

## See also

- [Formatting types](formatting-types.md)
