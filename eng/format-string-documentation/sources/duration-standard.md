---
title: "Standard TimeSpan format strings"
description: Review standard TimeSpan format strings, which use a single format specifier to define the text representation of a TimeSpan value in .NET.
ms.date: "03/30/2017"
ms.topic: reference
dev_langs:
  - "csharp"
  - "vb"
helpviewer_keywords:
  - "format specifiers, standard time interval"
  - "format strings"
  - "standard time interval format strings"
  - "standard format strings, time intervals"
  - "format specifiers, time intervals"
  - "time intervals [.NET], formatting"
  - "time [.NET], formatting"
  - "formatting [.NET], time"
  - "standard TimeSpan format strings"
  - "formatting [.NET], time intervals"
---
# Standard TimeSpan format strings

A standard <xref:System.TimeSpan> format string uses a single format specifier to define the text representation of a <xref:System.TimeSpan> value that results from a formatting operation. Any format string that contains more than one character, including white space, is interpreted as a custom <xref:System.TimeSpan> format string. For more information, see [Custom TimeSpan format strings](custom-timespan-format-strings.md).

 The string representations of <xref:System.TimeSpan> values are produced by calls to the overloads of the <xref:System.TimeSpan.ToString*?displayProperty=nameWithType> method, as well as by methods that support composite formatting, such as <xref:System.String.Format*?displayProperty=nameWithType>. For more information, see [Formatting Types](formatting-types.md) and [Composite Formatting](composite-formatting.md). The following example illustrates the use of standard format strings in formatting operations.

 [!code-csharp[Conceptual.TimeSpan.Standard#2](../../../samples/snippets/csharp/VS_Snippets_CLR/conceptual.timespan.standard/cs/formatexample1.cs#2)]
 [!code-vb[Conceptual.TimeSpan.Standard#2](../../../samples/snippets/visualbasic/VS_Snippets_CLR/conceptual.timespan.standard/vb/formatexample1.vb#2)]

 Standard <xref:System.TimeSpan> format strings are also used by the <xref:System.TimeSpan.ParseExact*?displayProperty=nameWithType> and <xref:System.TimeSpan.TryParseExact*?displayProperty=nameWithType> methods to define the required format of input strings for parsing operations. (Parsing converts the string representation of a value to that value.) The following example illustrates the use of standard format strings in parsing operations.

 [!code-csharp[Conceptual.TimeSpan.Standard#3](../../../samples/snippets/csharp/VS_Snippets_CLR/conceptual.timespan.standard/cs/parseexample1.cs#3)]
 [!code-vb[Conceptual.TimeSpan.Standard#3](../../../samples/snippets/visualbasic/VS_Snippets_CLR/conceptual.timespan.standard/vb/parseexample1.vb#3)]

The following table lists the standard time interval format specifiers.

|Format specifier|Name|Description|Examples|
|----------------------|----------|-----------------|--------------|
|"c"|Constant (invariant) format|This specifier is not culture-sensitive. It takes the form `[-][d'.']hh':'mm':'ss['.'fffffff]`.<br /><br /> (The "t" and "T" format strings produce the same results.)<br /><br /> More information: [The Constant ("c") Format Specifier](#the-constant-c-format-specifier).|`TimeSpan.Zero` -> 00:00:00<br /><br /> `New TimeSpan(0, 0, 30, 0)` -> 00:30:00<br /><br /> `New TimeSpan(3, 17, 25, 30, 500)` -> 3.17:25:30.5000000|
|"g"|General short format|This specifier outputs only what is needed. It is culture-sensitive and takes the form `[-][d':']h':'mm':'ss[.FFFFFFF]`.<br /><br /> More information: [The General Short ("g") Format Specifier](#the-general-short-g-format-specifier).|`New TimeSpan(1, 3, 16, 50, 500)` -> 1:3:16:50.5 (en-US)<br /><br /> `New TimeSpan(1, 3, 16, 50, 500)` -> 1:3:16:50,5 (fr-FR)<br /><br /> `New TimeSpan(1, 3, 16, 50, 599)` -> 1:3:16:50.599 (en-US)<br /><br /> `New TimeSpan(1, 3, 16, 50, 599)` -> 1:3:16:50,599 (fr-FR)|
|"G"|General long format|This specifier always outputs days and seven fractional digits. It is culture-sensitive and takes the form `[-]d':'hh':'mm':'ss.fffffff`.<br /><br /> More information: [The General Long ("G") Format Specifier](#the-general-long-g-format-specifier).|`New TimeSpan(18, 30, 0)` -> 0:18:30:00.0000000 (en-US)<br /><br /> `New TimeSpan(18, 30, 0)` -> 0:18:30:00,0000000 (fr-FR)|

## The Constant ("c") Format Specifier

The "c" format specifier returns the string representation of a <xref:System.TimeSpan> value in the following form:

`[-][d.]hh:mm:ss[.fffffff]`

Elements in square brackets ([ and ]) are optional. The period (.) and colon (:) are literal symbols. The following table describes the remaining elements.

|Element|Description|
|-------------|-----------------|
|*-*|An optional negative sign, which indicates a negative time interval.|
|*d*|The optional number of days, with no leading zeros.|
|*hh*|The number of hours, which ranges from "00" to "23".|
|*mm*|The number of minutes, which ranges from "00" to "59".|
|*ss*|The number of seconds, which ranges from "0" to "59".|
|*fffffff*|The optional fractional portion of a second.  Its value can range from "0000001" (one tick, or one ten-millionth of a second)  to "9999999" (9,999,999 ten-millionths of a second, or one second less one tick).|

 Unlike the "g" and "G" format specifiers, the "c" format specifier is not culture-sensitive. It produces the string representation of a <xref:System.TimeSpan> value that is invariant and that's common to versions prior to .NET Framework 4. "c" is the default <xref:System.TimeSpan> format string; the <xref:System.TimeSpan.ToString?displayProperty=nameWithType> method formats a time interval value by using the "c" format string.

> [!NOTE]
> <xref:System.TimeSpan> also supports the "t" and "T" standard format strings, which are identical in behavior to the "c" standard format string.

 The following example instantiates two <xref:System.TimeSpan> objects, uses them to perform arithmetic operations, and displays the result. In each case, it uses composite formatting to display the <xref:System.TimeSpan> value by using the "c" format specifier.

 [!code-csharp[Conceptual.TimeSpan.Standard#1](../../../samples/snippets/csharp/VS_Snippets_CLR/conceptual.timespan.standard/cs/standardc1.cs#1)]
 [!code-vb[Conceptual.TimeSpan.Standard#1](../../../samples/snippets/visualbasic/VS_Snippets_CLR/conceptual.timespan.standard/vb/standardc1.vb#1)]

## The General Short ("g") Format Specifier

The "g" <xref:System.TimeSpan> format specifier returns the string representation of a <xref:System.TimeSpan> value in a compact form by including only the elements that are necessary. It has the following form:

`[-][d:]h:mm:ss[.FFFFFFF]`

Elements in square brackets ([ and ]) are optional. The colon (:) is a literal symbol. The following table describes the remaining elements.

|Element|Description|
|-------------|-----------------|
|*-*|An optional negative sign, which indicates a negative time interval.|
|*d*|The optional number of days, with no leading zeros.|
|*h*|The number of hours, which ranges from "0" to "23", with no leading zeros.|
|*mm*|The number of minutes, which ranges from "00" to "59".|
|*ss*|The number of seconds, which ranges from "00" to "59".|
|*.*|The fractional seconds separator. It is equivalent to the specified culture's <xref:System.Globalization.NumberFormatInfo.NumberDecimalSeparator> property without user overrides.|
|*FFFFFFF*|The fractional seconds. As few digits as possible are displayed.|

 Like the "G" format specifier, the "g" format specifier is localized. Its fractional seconds separator is based on either the current culture or a specified culture's <xref:System.Globalization.NumberFormatInfo.NumberDecimalSeparator> property.

 The following example instantiates two <xref:System.TimeSpan> objects, uses them to perform arithmetic operations, and displays the result. In each case, it uses composite formatting to display the <xref:System.TimeSpan> value by using the "g" format specifier. In addition, it formats the <xref:System.TimeSpan> value by using the formatting conventions of the current system culture (which, in this case, is English - United States or en-US) and the French - France (fr-FR) culture.

 [!code-csharp[Conceptual.TimeSpan.Standard#4](../../../samples/snippets/csharp/VS_Snippets_CLR/conceptual.timespan.standard/cs/standardshort1.cs#4)]
 [!code-vb[Conceptual.TimeSpan.Standard#4](../../../samples/snippets/visualbasic/VS_Snippets_CLR/conceptual.timespan.standard/vb/standardshort1.vb#4)]

## The General Long ("G") Format Specifier

The "G" <xref:System.TimeSpan> format specifier returns the string representation of a <xref:System.TimeSpan> value in a long form that always includes both days and fractional seconds. The string that results from the "G" standard format specifier has the following form:

`[-]d:hh:mm:ss.fffffff`

Elements in square brackets ([ and ]) are optional. The colon (:) is a literal symbol. The following table describes the remaining elements.

|Element|Description|
|-------------|-----------------|
|*-*|An optional negative sign, which indicates a negative time interval.|
|*d*|The number of days, with no leading zeros.|
|*hh*|The number of hours, which ranges from "00" to "23".|
|*mm*|The number of minutes, which ranges from "00" to "59".|
|*ss*|The number of seconds, which ranges from "00" to "59".|
|*.*|The fractional seconds separator. It is equivalent to the specified culture's <xref:System.Globalization.NumberFormatInfo.NumberDecimalSeparator> property without user overrides.|
|*fffffff*|The fractional seconds.|

 Like the "G" format specifier, the "g" format specifier is localized. Its fractional seconds separator is based on either the current culture or a specified culture's <xref:System.Globalization.NumberFormatInfo.NumberDecimalSeparator> property.

 The following example instantiates two <xref:System.TimeSpan> objects, uses them to perform arithmetic operations, and displays the result. In each case, it uses composite formatting to display the <xref:System.TimeSpan> value by using the "G" format specifier. In addition, it formats the <xref:System.TimeSpan> value by using the formatting conventions of the current system culture (which, in this case, is English - United States or en-US) and the French - France (fr-FR) culture.

 [!code-csharp[Conceptual.TimeSpan.Standard#5](../../../samples/snippets/csharp/VS_Snippets_CLR/conceptual.timespan.standard/cs/standardlong1.cs#5)]
 [!code-vb[Conceptual.TimeSpan.Standard#5](../../../samples/snippets/visualbasic/VS_Snippets_CLR/conceptual.timespan.standard/vb/standardlong1.vb#5)]

## See also

- [Formatting Types](formatting-types.md)
- [Custom TimeSpan Format Strings](custom-timespan-format-strings.md)
- [Parsing Strings](parsing-strings.md)
