---
title: "Custom numeric format strings"
description: Learn how to create a custom numeric format string to format numeric data in .NET. A custom numeric format string has one or more custom numeric specifiers.
ms.date: "06/25/2018"
ms.topic: reference
dev_langs:
  - "csharp"
  - "vb"
helpviewer_keywords:
  - "numeric format strings [.NET]"
  - "formatting [.NET], numbers"
  - "format strings"
  - "custom numeric format strings"
  - "numbers [.NET], formatting"
  - "format specifiers, numeric"
  - "formatting numbers [.NET]"
  - "format specifiers, custom numeric format strings"
---
# Custom numeric format strings

You can create a custom numeric format string, which consists of one or more custom numeric specifiers, to define how to format numeric data. A custom numeric format string is any format string that is not a [standard numeric format string](standard-numeric-format-strings.md).

Custom numeric format strings are supported by some overloads of the `ToString` method of all numeric types. For example, you can supply a numeric format string to the <xref:System.Int32.ToString%28System.String%29> and <xref:System.Int32.ToString%28System.String%2CSystem.IFormatProvider%29> methods of the <xref:System.Int32> type. Custom numeric format strings are also supported by the .NET [composite formatting feature](composite-formatting.md), which is used by some `Write` and `WriteLine` methods of the <xref:System.Console> and <xref:System.IO.StreamWriter> classes, the <xref:System.String.Format*?displayProperty=nameWithType> method, and the <xref:System.Text.StringBuilder.AppendFormat*?displayProperty=nameWithType> method. [String interpolation](../../csharp/language-reference/tokens/interpolated.md) feature also supports custom numeric format strings.

> [!TIP]
> You can download the **Formatting Utility**, a .NET Core Windows Forms application that lets you apply format strings to either numeric or date and time values and displays the result string. Source code is available for [C#](/samples/dotnet/samples/windowsforms-formatting-utility-cs) and [Visual Basic](/samples/dotnet/samples/windowsforms-formatting-utility-vb).

<a name="table"></a> The following table describes the custom numeric format specifiers and displays sample output produced by each format specifier. See the [Notes](#NotesCustomFormatting) section for additional information about using custom numeric format strings, and the [Example](#example) section for a comprehensive illustration of their use.

|Format specifier|Name|Description|Examples|
|----------------------|----------|-----------------|--------------|
|"0"|Zero placeholder|Replaces the zero with the corresponding digit if one is present; otherwise, zero appears in the result string.<br /><br /> More information: [The "0" Custom Specifier](#Specifier0).|1234.5678 ("00000") -> 01235<br /><br /> 0.45678 ("0.00", en-US) -> 0.46<br /><br /> 0.45678 ("0.00", fr-FR) -> 0,46|
|"#"|Digit placeholder|Replaces the "#" symbol with the corresponding digit if one is present; otherwise, no digit appears in the result string.<br /><br /> Note that no digit appears in the result string if the corresponding digit in the input string is a non-significant 0. For example, 0003 ("####") -> 3.<br /><br /> More information: [The "#" Custom Specifier](#SpecifierD).|1234.5678 ("#####") -> 1235<br /><br /> 0.45678 ("#.##", en-US) -> .46<br /><br /> 0.45678 ("#.##", fr-FR) -> ,46|
|"."|Decimal point|Determines the location of the decimal separator in the result string.<br /><br /> More information: [The "." Custom Specifier](#SpecifierPt).|0.45678 ("0.00", en-US) -> 0.46<br /><br /> 0.45678 ("0.00", fr-FR) -> 0,46|
|","|Group separator and number scaling|Serves as both a group separator and a number scaling specifier. As a group separator, it inserts a localized group separator character between each group. As a number scaling specifier, it divides a number by 1000 for each comma specified.<br /><br /> More information: [The "," Custom Specifier](#SpecifierTh).|Group separator specifier:<br /><br /> 2147483647 ("##,#", en-US) -> 2,147,483,647<br /><br /> 2147483647 ("##,#", es-ES) -> 2.147.483.647<br /><br /> Scaling specifier:<br /><br /> 2147483647 ("#,#,,", en-US) -> 2,147<br /><br /> 2147483647 ("#,#,,", es-ES) -> 2.147|
|"%"|Percentage placeholder|Multiplies a number by 100 and inserts a localized percentage symbol in the result string.<br /><br /> More information: [The "%" Custom Specifier](#SpecifierPct).|0.3697 ("%#0.00", en-US) -> %36.97<br /><br /> 0.3697 ("%#0.00", el-GR) -> %36,97<br /><br /> 0.3697 ("##.0 %", en-US) -> 37.0 %<br /><br /> 0.3697 ("##.0 %", el-GR) -> 37,0 %|
|"‰"|Per mille placeholder|Multiplies a number by 1000 and inserts a localized per mille symbol in the result string.<br /><br /> More information: [The "‰" Custom Specifier](#SpecifierPerMille).|0.03697 ("#0.00‰", en-US) -> 36.97‰<br /><br /> 0.03697 ("#0.00‰", ru-RU) -> 36,97‰|
|"E0"<br /><br /> "E+0"<br /><br /> "E-0"<br /><br /> "e0"<br /><br /> "e+0"<br /><br /> "e-0"|Exponential notation|If followed by at least one 0 (zero), formats the result using exponential notation. The case of "E" or "e" indicates the case of the exponent symbol in the result string. The number of zeros following the "E" or "e" character determines the minimum number of digits in the exponent. A plus sign (+) indicates that a sign character always precedes the exponent. A minus sign (-) indicates that a sign character precedes only negative exponents.<br /><br /> More information: [The "E" and "e" Custom Specifiers](#SpecifierExponent).|987654 ("#0.0e0") -> 98.8e4<br /><br /> 1503.92311 ("0.0##e+00") -> 1.504e+03<br /><br /> 1.8901385E-16 ("0.0e+00") -> 1.9e-16|
|"\\"|Escape character|Causes the next character to be interpreted as a literal rather than as a custom format specifier.<br /><br /> More information: [The "\\" Escape Character](#SpecifierEscape).|987654 ("\\###00\\#") -> #987654#|
|'*string*'<br /><br /> "*string*"|Literal string delimiter|Indicates that the enclosed characters should be copied to the result string unchanged.<br/><br/>More information: [Character literals](#character-literals).|68 ("# 'degrees'") -> 68 degrees<br /><br /> 68 ("#' degrees'") -> 68 degrees|
|;|Section separator|Defines sections with separate format strings for positive, negative, and zero numbers.<br /><br /> More information: [The ";" Section Separator](#SectionSeparator).|12.345 ("#0.0#;(#0.0#);-\0-") -> 12.35<br /><br /> 0 ("#0.0#;(#0.0#);-\0-") -> -0-<br /><br /> -12.345 ("#0.0#;(#0.0#);-\0-") -> (12.35)<br /><br /> 12.345 ("#0.0#;(#0.0#)") -> 12.35<br /><br /> 0 ("#0.0#;(#0.0#)") -> 0.0<br /><br /> -12.345 ("#0.0#;(#0.0#)") -> (12.35)|
|Other|All other characters|The character is copied to the result string unchanged.<br/><br/>More information: [Character literals](#character-literals).|68 ("# °") -> 68 °|

The following sections provide detailed information about each of the custom numeric format specifiers.

<a name="Specifier0"></a>

## The "0" custom specifier

The "0" custom format specifier serves as a zero-placeholder symbol. If the value that is being formatted has a digit in the position where the zero appears in the format string, that digit is copied to the result string; otherwise, a zero appears in the result string. The position of the leftmost zero before the decimal point and the rightmost zero after the decimal point determines the range of digits that are always present in the result string.

The "00" specifier causes the value to be rounded to the nearest digit preceding the decimal, where rounding away from zero is always used. For example, formatting 34.5 with "00" would result in the value 35.

The following example displays several values that are formatted by using custom format strings that include zero placeholders.

[!code-csharp[Formatting.Numeric.Custom#1](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/custom.cs#1)]
[!code-vb[Formatting.Numeric.Custom#1](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/Custom.vb#1)]

[Back to table](#table)

<a name="SpecifierD"></a>

## The "#" custom specifier

The "#" custom format specifier serves as a digit-placeholder symbol. If the value that is being formatted has a digit in the position where the "#" symbol appears in the format string, that digit is copied to the result string. Otherwise, nothing is stored in that position in the result string.

Note that this specifier never displays a zero that is not a significant digit, even if zero is the only digit in the string. It will display zero only if it is a significant digit in the number that is being displayed.

The "##" format string causes the value to be rounded to the nearest digit preceding the decimal, where rounding away from zero is always used. For example, formatting 34.5 with "##" would result in the value 35.

The following example displays several values that are formatted by using custom format strings that include digit placeholders.

[!code-csharp[Formatting.Numeric.Custom#2](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/custom.cs#2)]
[!code-vb[Formatting.Numeric.Custom#2](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/Custom.vb#2)]

To return a result string in which absent digits or leading zeroes are replaced by spaces, use the [composite formatting feature](composite-formatting.md) and specify a field width, as the following example illustrates.

[!code-csharp[Formatting.Numeric.Custom#12](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/SpaceOrDigit1.cs#12)]
[!code-vb[Formatting.Numeric.Custom#12](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/SpaceOrDigit1.vb#12)]

[Back to table](#table)

<a name="SpecifierPt"></a>

## The "." custom specifier

The "." custom format specifier inserts a localized decimal separator into the result string. The first period in the format string determines the location of the decimal separator in the formatted value; any additional periods are ignored. If the format specifier ends with a "." only the significant digits are formatted into the result string.

The character that is used as the decimal separator in the result string is not always a period; it is determined by the <xref:System.Globalization.NumberFormatInfo.NumberDecimalSeparator> property of the <xref:System.Globalization.NumberFormatInfo> object that controls formatting.

The following example uses the "." format specifier to define the location of the decimal point in several result strings.

[!code-csharp[Formatting.Numeric.Custom#3](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/custom.cs#3)]
[!code-vb[Formatting.Numeric.Custom#3](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/Custom.vb#3)]

[Back to table](#table)

<a name="SpecifierTh"></a>

## The "," custom specifier

The "," character serves as both a group separator and a number scaling specifier.

- Group separator: If one or more commas are specified between two digit placeholders (0 or #) that format the integral digits of a number, a group separator character is inserted between each number group in the integral part of the output.

  The <xref:System.Globalization.NumberFormatInfo.NumberGroupSeparator*> and <xref:System.Globalization.NumberFormatInfo.NumberGroupSizes> properties of the current <xref:System.Globalization.NumberFormatInfo> object determine the character used as the number group separator and the size of each number group. For example, if the string "#,#" and the invariant culture are used to format the number 1000, the output is "1,000".

- Number scaling specifier: If one or more commas are specified immediately to the left of the explicit or implicit decimal point, the number to be formatted is divided by 1000 for each comma. For example, if the string "0,," is used to format the number 100 million, the output is "100".

You can use group separator and number scaling specifiers in the same format string. For example, if the string "#,0,," and the invariant culture are used to format the number one billion, the output is "1,000".

The following example illustrates the use of the comma as a group separator.

[!code-csharp[Formatting.Numeric.Custom#4](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/custom.cs#4)]
[!code-vb[Formatting.Numeric.Custom#4](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/Custom.vb#4)]

The following example illustrates the use of the comma as a specifier for number scaling.

[!code-csharp[Formatting.Numeric.Custom#5](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/custom.cs#5)]
[!code-vb[Formatting.Numeric.Custom#5](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/Custom.vb#5)]

[Back to table](#table)

<a name="SpecifierPct"></a>

## The "%" custom specifier

A percent sign (%) in a format string causes a number to be multiplied by 100 before it is formatted. The localized percent symbol is inserted in the number at the location where the % appears in the format string. The percent character used is defined by the <xref:System.Globalization.NumberFormatInfo.PercentSymbol> property of the current <xref:System.Globalization.NumberFormatInfo> object.

The following example defines several custom format strings that include the "%" custom specifier.

[!code-csharp[Formatting.Numeric.Custom#6](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/custom.cs#6)]
[!code-vb[Formatting.Numeric.Custom#6](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/Custom.vb#6)]

[Back to table](#table)

<a name="SpecifierPerMille"></a>

## The "‰" custom specifier

A per mille character (‰ or \u2030) in a format string causes a number to be multiplied by 1000 before it is formatted. The appropriate per mille symbol is inserted in the returned string at the location where the ‰ symbol appears in the format string. The per mille character used is defined by the <xref:System.Globalization.NumberFormatInfo.PerMilleSymbol?displayProperty=nameWithType> property of the object that provides culture-specific formatting information.

The following example defines a custom format string that includes the "‰" custom specifier.

[!code-csharp[Formatting.Numeric.Custom#9](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/custom.cs#9)]
[!code-vb[Formatting.Numeric.Custom#9](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/Custom.vb#9)]

[Back to table](#table)

<a name="SpecifierExponent"></a>

## The "E" and "e" custom specifiers

If any of the strings "E", "E+", "E-", "e", "e+", or "e-" are present in the format string and are followed immediately by at least one zero, the number is formatted by using scientific notation with an "E" or "e" inserted between the number and the exponent. The number of zeros following the scientific notation indicator determines the minimum number of digits to output for the exponent. The "E+" and "e+" formats indicate that a plus sign or minus sign should always precede the exponent. The "E", "E-", "e", or "e-" formats indicate that a sign character should precede only negative exponents.

The following example formats several numeric values using the specifiers for scientific notation.

[!code-csharp[Formatting.Numeric.Custom#7](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/custom.cs#7)]
[!code-vb[Formatting.Numeric.Custom#7](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/Custom.vb#7)]

[Back to table](#table)

<a name="SpecifierEscape"></a>

## The "\\" escape character

The "#", "0", ".", ",", "%", and "‰" symbols in a format string are interpreted as format specifiers rather than as literal characters. Depending on their position in a custom format string, the uppercase and lowercase "E" as well as the + and - symbols may also be interpreted as format specifiers.

To prevent a character from being interpreted as a format specifier, you can precede it with a backslash, which is the escape character. The escape character signifies that the following character is a character literal that should be included in the result string unchanged.

To include a backslash in a result string, you must escape it with another backslash (`\\`).

> [!NOTE]
> Some compilers, such as the C++ and C# compilers, may also interpret a single backslash character as an escape character. To ensure that a string is interpreted correctly when formatting, you can use the verbatim string literal character (the @ character) before the string in C#, or add another backslash character before each backslash in C# and C++. The following C# example illustrates both approaches.

The following example uses the escape character to prevent the formatting operation from interpreting the "#", "0", and "\\" characters as either escape characters or format specifiers. The C# examples uses an additional backslash to ensure that a backslash is interpreted as a literal character.

[!code-csharp[Formatting.Numeric.Custom#11](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/escape1.cs#11)]
[!code-vb[Formatting.Numeric.Custom#11](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/escape1.vb#11)]

[Back to table](#table)

<a name="SectionSeparator"></a>

## The ";" section separator

The semicolon (;) is a conditional format specifier that applies different formatting to a number depending on whether its value is positive, negative, or zero. To produce this behavior, a custom format string can contain up to three sections separated by semicolons. These sections are described in the following table.

|Number of sections|Description|
|------------------------|-----------------|
|One section|The format string applies to all values.|
|Two sections|The first section applies to positive values and zeros, and the second section applies to negative values.<br /><br /> If the number to be formatted is negative, but becomes zero after rounding according to the format in the second section, the resulting zero is formatted according to the first section.|
|Three sections|The first section applies to positive values, the second section applies to negative values, and the third section applies to zeros.<br /><br /> The second section can be left empty (by having nothing between the semicolons), in which case the first section applies to all nonzero values.<br /><br /> If the number to be formatted is nonzero, but becomes zero after rounding according to the format in the first or second section, the resulting zero is formatted according to the third section.|

Section separators ignore any preexisting formatting associated with a number when the final value is formatted. For example, negative values are always displayed without a minus sign when section separators are used. If you want the final formatted value to have a minus sign, you should explicitly include the minus sign as part of the custom format specifier.

The following example uses the ";" format specifier to format positive, negative, and zero numbers differently.

[!code-csharp[Formatting.Numeric.Custom#8](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/custom.cs#8)]
[!code-vb[Formatting.Numeric.Custom#8](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/Custom.vb#8)]

[Back to table](#table)

## Character literals

Format specifiers that appear in a custom numeric format string are always interpreted as formatting characters and never as literal characters. This includes the following characters:

- [0](#Specifier0)
- [\#](#SpecifierD)
- [%](#SpecifierPct)
- [‰](#SpecifierPerMille)
- '
- [\\](#SpecifierEscape)
- [.](#SpecifierPt)
- [,](#SpecifierTh)
- [E or e](#SpecifierExponent), depending on its position in the format string.

All other characters are always interpreted as character literals and, in a formatting operation, are included in the result string unchanged.  In a parsing operation, they must match the characters in the input string exactly; the comparison is case-sensitive.

The following example illustrates one common use of literal character units (in this case, thousands):

[!code-csharp[literal characters](~/samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/literal2.cs#1)]
[!code-vb[literal characters](~/samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/literal2.vb#1)]

There are two ways to indicate that characters are to be interpreted as literal characters and not as formatting characters, so that they can be included in a result string or successfully parsed in an input string:

- By escaping a formatting character. For more information, see [The "\\" escape character](#SpecifierEscape).

- By enclosing the entire literal string in quotation apostrophes.

The following example uses both approaches to include reserved characters in a custom numeric format string.

[!code-csharp[including reserved characters](~/samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/literal1.cs#1)]
[!code-vb[including reserved characters](~/samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/literal1.vb#1)]

<a name="NotesCustomFormatting"></a>

## Notes

### Floating-Point infinities and NaN

Regardless of the format string, if the value of a <xref:System.Half>, <xref:System.Single>, or <xref:System.Double> floating-point type is positive infinity, negative infinity, or not a number (NaN), the formatted string is the value of the respective <xref:System.Globalization.NumberFormatInfo.PositiveInfinitySymbol*>, <xref:System.Globalization.NumberFormatInfo.NegativeInfinitySymbol*>, or <xref:System.Globalization.NumberFormatInfo.NaNSymbol> property specified by the currently applicable <xref:System.Globalization.NumberFormatInfo> object.

### Control Panel settings

The settings in the **Regional and Language Options** item in Control Panel influence the result string produced by a formatting operation. Those settings are used to initialize the <xref:System.Globalization.NumberFormatInfo> object associated with the current culture, and the current culture provides values used to govern formatting. Computers that use different settings generate different result strings.

In addition, if you use the <xref:System.Globalization.CultureInfo.%23ctor%28System.String%29> constructor to instantiate a new <xref:System.Globalization.CultureInfo> object that represents the same culture as the current system culture, any customizations established by the **Regional and Language Options** item in Control Panel will be applied to the new <xref:System.Globalization.CultureInfo> object. You can use the <xref:System.Globalization.CultureInfo.%23ctor%28System.String%2CSystem.Boolean%29> constructor to create a <xref:System.Globalization.CultureInfo> object that does not reflect a system's customizations.

### Rounding and fixed-point format strings

For fixed-point format strings (that is, format strings that do not contain scientific notation format characters), numbers are rounded to as many decimal places as there are digit placeholders to the right of the decimal point. If the format string does not contain a decimal point, the number is rounded to the nearest integer. If the number has more digits than there are digit placeholders to the left of the decimal point, the extra digits are copied to the result string immediately before the first digit placeholder.

[Back to table](#table)

<a name="example"></a>

## Example

The following example demonstrates two custom numeric format strings. In both cases, the digit placeholder (`#`) displays the numeric data, and all other characters are copied to the result string.

[!code-csharp[Formatting.Numeric.Custom#10](../../../samples/snippets/csharp/VS_Snippets_CLR/formatting.numeric.custom/cs/example1.cs#10)]
[!code-vb[Formatting.Numeric.Custom#10](../../../samples/snippets/visualbasic/VS_Snippets_CLR/formatting.numeric.custom/vb/example1.vb#10)]

[Back to table](#table)

## See also

- <xref:System.Globalization.NumberFormatInfo?displayProperty=nameWithType>
- [Formatting Types](formatting-types.md)
- [Standard Numeric Format Strings](standard-numeric-format-strings.md)
- [How to: Pad a Number with Leading Zeros](how-to-pad-a-number-with-leading-zeros.md)
- [Sample: .NET Core WinForms Formatting Utility (C#)](/samples/dotnet/samples/windowsforms-formatting-utility-cs)
- [Sample: .NET Core WinForms Formatting Utility (Visual Basic)](/samples/dotnet/samples/windowsforms-formatting-utility-vb)
