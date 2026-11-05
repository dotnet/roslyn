---
title: "Standard date and time format strings"
description: Learn how to use a standard date and time format string to define the text representation of a date and time value in .NET.
ms.date: 04/05/2023
dev_langs:
  - "csharp"
  - "vb"
helpviewer_keywords:
  - "formatting [.NET], dates"
  - "custom DateTime format string"
  - "format specifiers, custom date and time"
  - "format strings"
  - "custom date and time format strings"
  - "formatting [.NET], time"
  - "date and time strings"
ms.topic: reference
---
# Standard date and time format strings

A standard date and time format string uses a single character as the format specifier to define the text representation of a <xref:System.DateTime>, <xref:System.DateTimeOffset>, <xref:System.DateOnly>, or <xref:System.TimeOnly> value. Any date and time format string that contains more than one character, including white space, is interpreted as a [custom date and time format string](custom-date-and-time-format-strings.md). A standard or custom format string can be used in two ways:

- To define the string that results from a formatting operation.

- To define the text representation of a date and time value that can be converted to a <xref:System.DateTime>, <xref:System.DateTimeOffset>, <xref:System.DateOnly>, or <xref:System.TimeOnly> value by a parsing operation.

> [!TIP]
> You can download the **Formatting Utility**, a .NET Windows Forms application that lets you apply format strings to either numeric or date and time values and display the result string. Source code is available for [C#](/samples/dotnet/samples/windowsforms-formatting-utility-cs) and [Visual Basic](/samples/dotnet/samples/windowsforms-formatting-utility-vb).

## Table of format specifiers

<a name="table"></a> The following table describes the standard date and time format specifiers. Unless otherwise noted, a particular standard date and time format specifier produces an identical string representation regardless of whether it is used with a <xref:System.DateTime> or a <xref:System.DateTimeOffset> value. Not all format specifiers can be used with <xref:System.DateOnly> and <xref:System.TimeOnly> values; for additional information, see [DateOnly and TimeOnly formatting](#dateonly-and-timeonly-formatting). For information about how regional settings on Windows and the current <xref:System.Globalization.DateTimeFormatInfo> object can affect date and time formatting, see [Control Panel Settings](#control-panel-settings) and [DateTimeFormatInfo Properties](#datetimeformatinfo-properties).

|Format specifier|Description|Examples|
|----------------------|-----------------|--------------|
|"d"|Short date pattern.<br /><br /> More information:[The short date ("d") format specifier](#ShortDate).|2009-06-15T13:45:30 -> 6/15/2009 (en-US)<br /><br /> 2009-06-15T13:45:30 -> 15/06/2009 (fr-FR)<br /><br /> 2009-06-15T13:45:30 -> 2009/06/15 (ja-JP)<br /><br /> <xref:System.DateOnly> (2009-06-15) -> 6/15/2009 (en-US)|
|"D"|Long date pattern.<br /><br /> More information:[The long date ("D") format specifier](#LongDate).|2009-06-15T13:45:30 -> Monday, June 15, 2009 (en-US)<br /><br /> 2009-06-15T13:45:30 -> понедельник, 15 июня 2009 г. (ru-RU)<br /><br /> 2009-06-15T13:45:30 -> Montag, 15. Juni 2009 (de-DE)|
|"f"|Full date/time pattern (short time).<br /><br /> More information: [The full date short time ("f") format specifier](#FullDateShortTime).|2009-06-15T13:45:30 -> Monday, June 15, 2009 1:45 PM (en-US)<br /><br /> 2009-06-15T13:45:30 -> den 15 juni 2009 13:45 (sv-SE)<br /><br /> 2009-06-15T13:45:30 -> Δευτέρα, 15 Ιουνίου 2009 1:45 μμ (el-GR)|
|"F"|Full date/time pattern (long time).<br /><br /> More information: [The full date long time ("F") format specifier](#FullDateLongTime).|2009-06-15T13:45:30 -> Monday, June 15, 2009 1:45:30 PM (en-US)<br /><br /> 2009-06-15T13:45:30 -> den 15 juni 2009 13:45:30 (sv-SE)<br /><br /> 2009-06-15T13:45:30 -> Δευτέρα, 15 Ιουνίου 2009 1:45:30 μμ (el-GR)|
|"g"|General date/time pattern (short time).<br /><br /> More information: [The general date short time ("g") format specifier](#GeneralDateShortTime).|2009-06-15T13:45:30 -> 6/15/2009 1:45 PM (en-US)<br /><br /> 2009-06-15T13:45:30 -> 15/06/2009 13:45 (es-ES)<br /><br /> 2009-06-15T13:45:30 -> 2009/6/15 13:45 (zh-CN)|
|"G"|General date/time pattern (long time).<br /><br /> More information: [The general date long time ("G") format specifier](#GeneralDateLongTime).|2009-06-15T13:45:30 -> 6/15/2009 1:45:30 PM (en-US)<br /><br /> 2009-06-15T13:45:30 -> 15/06/2009 13:45:30 (es-ES)<br /><br /> 2009-06-15T13:45:30 -> 2009/6/15 13:45:30 (zh-CN)|
|"M", "m"|Month/day pattern.<br /><br /> More information: [The month ("M", "m") format specifier](#MonthDay).|2009-06-15T13:45:30 -> June 15 (en-US)<br /><br /> 2009-06-15T13:45:30 -> 15. juni (da-DK)<br /><br /> 2009-06-15T13:45:30 -> 15 Juni (id-ID)|
|"O", "o"|round-trip date/time pattern.<br /><br /> More information: [The round-trip ("O", "o") format specifier](#Roundtrip).|<xref:System.DateTime> values:<br /><br /> 2009-06-15T13:45:30 (DateTimeKind.Local) --> 2009-06-15T13:45:30.0000000-07:00<br /><br /> 2009-06-15T13:45:30 (DateTimeKind.Utc) --> 2009-06-15T13:45:30.0000000Z<br /><br /> 2009-06-15T13:45:30 (DateTimeKind.Unspecified) --> 2009-06-15T13:45:30.0000000<br /><br /> <xref:System.DateTimeOffset> values:<br /><br /> 2009-06-15T13:45:30-07:00 --> 2009-06-15T13:45:30.0000000-07:00<br /><br /> <xref:System.DateOnly> values:<br /><br /> 2009-06-15 --> 2009-06-15<br /><br /> <xref:System.TimeOnly> values:<br /><br /> 13:45:30 --> 13:45:30.0000000|
|"R", "r"|RFC1123 pattern.<br /><br /> More information: [The RFC1123 ("R", "r") format specifier](#RFC1123).|<xref:System.DateTimeOffset> input: 2009-06-15T13:45:30 -> Mon, 15 Jun 2009 20:45:30 GMT<br/><xref:System.DateTime> input: 2009-06-15T13:45:30 -> Mon, 15 Jun 2009 13:45:30 GMT |
|"s"|Sortable date/time pattern.<br /><br /> More information: [The sortable ("s") format specifier](#Sortable).|2009-06-15T13:45:30 (DateTimeKind.Local) -> 2009-06-15T13:45:30<br /><br /> 2009-06-15T13:45:30 (DateTimeKind.Utc) -> 2009-06-15T13:45:30|
|"t"|Short time pattern.<br /><br /> More information: [The short time ("t") format specifier](#ShortTime).|2009-06-15T13:45:30 -> 1:45 PM (en-US)<br /><br /> 2009-06-15T13:45:30 -> 13:45 (hr-HR)<br /><br /> 2009-06-15T13:45:30 -> 01:45 م (ar-EG)<br /><br /> <xref:System.TimeOnly> (13:45:30) -> 1:45 PM (en-US)|
|"T"|Long time pattern.<br /><br /> More information: [The long time ("T") format specifier](#LongTime).|2009-06-15T13:45:30 -> 1:45:30 PM (en-US)<br /><br /> 2009-06-15T13:45:30 -> 13:45:30 (hr-HR)<br /><br /> 2009-06-15T13:45:30 -> 01:45:30 م (ar-EG)<br /><br /> <xref:System.TimeOnly> (13:45:30) -> 1:45:30 PM (en-US)|
|"u"|Universal sortable date/time pattern.<br /><br /> More information: [The universal sortable ("u") format specifier](#UniversalSortable).|With a <xref:System.DateTime> value: 2009-06-15T13:45:30 -> 2009-06-15 13:45:30Z<br /><br /> With a <xref:System.DateTimeOffset> value: 2009-06-15T13:45:30 -> 2009-06-15 20:45:30Z|
|"U"|Universal full date/time pattern.<br /><br /> More information: [The universal full ("U") format specifier](#UniversalFull).|2009-06-15T13:45:30 -> Monday, June 15, 2009 8:45:30 PM (en-US)<br /><br /> 2009-06-15T13:45:30 -> den 15 juni 2009 20:45:30 (sv-SE)<br /><br /> 2009-06-15T13:45:30 -> Δευτέρα, 15 Ιουνίου 2009 8:45:30 μμ (el-GR)|
|"Y", "y"|Year month pattern.<br /><br /> More information: [The year month ("Y") format specifier](#YearMonth).|2009-06-15T13:45:30 -> June 2009 (en-US)<br /><br /> 2009-06-15T13:45:30 -> juni 2009 (da-DK)<br /><br /> 2009-06-15T13:45:30 -> Juni 2009 (id-ID)|
|Any other single character|Unknown specifier.|Throws a runtime <xref:System.FormatException>.|

## How standard format strings work

In a formatting operation, a standard format string is simply an alias for a custom format string. The advantage of using an alias to refer to a custom format string is that, although the alias remains invariant, the custom format string itself can vary. This is important because the string representations of date and time values typically vary by culture. For example, the "d" standard format string indicates that a date and time value is to be displayed using a short date pattern. For the invariant culture, this pattern is "MM/dd/yyyy". For the fr-FR culture, it is "dd/MM/yyyy". For the ja-JP culture, it is "yyyy/MM/dd".

If a standard format string in a formatting operation maps to a particular culture's custom format string, your application can define the specific culture whose custom format strings are used in one of these ways:

- You can use the default (or current) culture. The following example displays a date using the current culture's short date format. In this case, the current culture is en-US.

  [!code-csharp[System.DateTime.Conceptual.Formatting#1](../../../samples/snippets/csharp/VS_Snippets_CLR_System/system.DateTime.Conceptual.Formatting/cs/StandardFormats1.cs#1)]
  [!code-vb[System.DateTime.Conceptual.Formatting#1](../../../samples/snippets/visualbasic/VS_Snippets_CLR_System/system.DateTime.Conceptual.Formatting/vb/StandardFormats1.vb#1)]

- You can pass a <xref:System.Globalization.CultureInfo> object representing the culture whose formatting is to be used to a method that has an <xref:System.IFormatProvider> parameter. The following example displays a date using the short date format of the pt-BR culture.

  [!code-csharp[System.DateTime.Conceptual.Formatting#2](../../../samples/snippets/csharp/VS_Snippets_CLR_System/system.DateTime.Conceptual.Formatting/cs/StandardFormats1.cs#2)]
  [!code-vb[System.DateTime.Conceptual.Formatting#2](../../../samples/snippets/visualbasic/VS_Snippets_CLR_System/system.DateTime.Conceptual.Formatting/vb/StandardFormats1.vb#2)]

- You can pass a <xref:System.Globalization.DateTimeFormatInfo> object that provides formatting information to a method that has an <xref:System.IFormatProvider> parameter. The following example displays a date using the short date format from a <xref:System.Globalization.DateTimeFormatInfo> object for the hr-HR culture.

  [!code-csharp[System.DateTime.Conceptual.Formatting#3](../../../samples/snippets/csharp/VS_Snippets_CLR_System/system.DateTime.Conceptual.Formatting/cs/StandardFormats1.cs#3)]
  [!code-vb[System.DateTime.Conceptual.Formatting#3](../../../samples/snippets/visualbasic/VS_Snippets_CLR_System/system.DateTime.Conceptual.Formatting/vb/StandardFormats1.vb#3)]

> [!NOTE]
> For information about customizing the patterns or strings used in formatting date and time values, see the <xref:System.Globalization.NumberFormatInfo> class topic.

In some cases, the standard format string serves as a convenient abbreviation for a longer custom format string that is invariant. Four standard format strings fall into this category: "O" (or "o"), "R" (or "r"), "s", and "u". These strings correspond to custom format strings defined by the invariant culture. They produce string representations of date and time values that are intended to be identical across cultures. The following table provides information on these four standard date and time format strings.

|Standard format string|Defined by DateTimeFormatInfo.InvariantInfo property|Custom format string|
|----------------------------|----------------------------------------------------------|--------------------------|
|"O" or "o"|None|yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK|
|"R" or "r"|<xref:System.Globalization.DateTimeFormatInfo.RFC1123Pattern*>|ddd, dd MMM yyyy HH':'mm':'ss 'GMT'|
|"s"|<xref:System.Globalization.DateTimeFormatInfo.SortableDateTimePattern>|yyyy'-'MM'-'dd'T'HH':'mm':'ss|
|"u"|<xref:System.Globalization.DateTimeFormatInfo.UniversalSortableDateTimePattern*>|yyyy'-'MM'-'dd HH':'mm':'ss'Z'|

Standard format strings can also be used in parsing operations with the <xref:System.DateTime.ParseExact*?displayProperty=nameWithType>, <xref:System.DateTimeOffset.ParseExact*?displayProperty=nameWithType>, <xref:System.DateOnly.ParseExact*?displayProperty=nameWithType>, and <xref:System.TimeOnly.ParseExact*?displayProperty=nameWithType> methods, which require an input string to exactly conform to a particular pattern for the parse operation to succeed. Many standard format strings map to multiple custom format strings, so a date and time value can be represented in a variety of formats and the parse operation will still succeed. You can determine the custom format string or strings that correspond to a standard format string by calling the <xref:System.Globalization.DateTimeFormatInfo.GetAllDateTimePatterns%28System.Char%29?displayProperty=nameWithType> method. The following example displays the custom format strings that map to the "d" (short date pattern) standard format string.

[!code-csharp[Formatting.DateAndTime.Standard#17](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/stdandparsing1.cs#17)]
[!code-vb[Formatting.DateAndTime.Standard#17](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/stdandparsing1.vb#17)]

The following sections describe the standard format specifiers for <xref:System.DateTime>, <xref:System.DateTimeOffset>, <xref:System.DateOnly>, and <xref:System.TimeOnly> values.

## Date formats

This group includes the following formats:

- [The short date ("d") format specifier](#the-short-date-d-format-specifier)
- [The long date ("D") format specifier](#the-long-date-d-format-specifier)

<a name="ShortDate"></a>

### The short date ("d") format specifier

The "d" standard format specifier represents a custom date and time format string that is defined by a specific culture's <xref:System.Globalization.DateTimeFormatInfo.ShortDatePattern?displayProperty=nameWithType> property. For example, the custom format string that is returned by the <xref:System.Globalization.DateTimeFormatInfo.ShortDatePattern> property of the invariant culture is "MM/dd/yyyy".

The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that control the formatting of the returned string.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.ShortDatePattern*>|Defines the overall format of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.DateSeparator*>|Defines the string that separates the year, month, and day components of a date.|

The following example uses the "d" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#1](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#1)]
[!code-vb[Formatting.DateAndTime.Standard#1](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#1)]

[Back to table](#table)

<a name="LongDate"></a>

### The long date ("D") format specifier

The "D" standard format specifier represents a custom date and time format string that is defined by the current <xref:System.Globalization.DateTimeFormatInfo.LongDatePattern?displayProperty=nameWithType> property. For example, the custom format string for the invariant culture is "dddd, dd MMMM yyyy".

The following table lists the properties of the <xref:System.Globalization.DateTimeFormatInfo> object that control the formatting of the returned string.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.LongDatePattern*>|Defines the overall format of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.DayNames*>|Defines the localized day names that can appear in the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.MonthNames*>|Defines the localized month names that can appear in the result string.|

The following example uses the "D" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#2](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#2)]
[!code-vb[Formatting.DateAndTime.Standard#2](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#2)]

[Back to table](#table)

## Date and time formats

This group includes the following formats:

- [The full date short time ("f") format specifier](#the-full-date-short-time-f-format-specifier)
- [The full date long time ("F") format specifier](#the-full-date-long-time-f-format-specifier)
- [The general date short time ("g") format specifier](#the-general-date-short-time-g-format-specifier)
- [The general date long time ("G") format specifier](#the-general-date-long-time-g-format-specifier)
- [The round-trip ("O", "o") format specifier](#the-round-trip-o-o-format-specifier)
- [The RFC1123 ("R", "r") format specifier](#the-rfc1123-r-r-format-specifier)
- [The sortable ("s") format specifier](#the-sortable-s-format-specifier)
- [The universal sortable ("u") format specifier](#the-universal-sortable-u-format-specifier)
- [The universal full ("U") format specifier](#the-universal-full-u-format-specifier)

<a name="FullDateShortTime"></a>

### The full date short time ("f") format specifier

The "f" standard format specifier represents a combination of the long date ("D") and short time ("t") patterns, separated by a space.

The result string is affected by the formatting information of a specific <xref:System.Globalization.DateTimeFormatInfo> object. The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that may control the formatting of the returned string. The custom format specifier returned by the <xref:System.Globalization.DateTimeFormatInfo.LongDatePattern*?displayProperty=nameWithType> and <xref:System.Globalization.DateTimeFormatInfo.ShortTimePattern?displayProperty=nameWithType> properties of some cultures may not make use of all properties.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.LongDatePattern*>|Defines the format of the date component of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.ShortTimePattern*>|Defines the format of the time component of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.DayNames*>|Defines the localized day names that can appear in the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.MonthNames*>|Defines the localized month names that can appear in the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.TimeSeparator*>|Defines the string that separates the hour, minute, and second components of a time.|
|<xref:System.Globalization.DateTimeFormatInfo.AMDesignator*>|Defines the string that indicates times from midnight to before noon in a 12-hour clock.|
|<xref:System.Globalization.DateTimeFormatInfo.PMDesignator*>|Defines the string that indicates times from noon to before midnight in a 12-hour clock.|

The following example uses the "f" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#3](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#3)]
[!code-vb[Formatting.DateAndTime.Standard#3](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#3)]

[Back to table](#table)

<a name="FullDateLongTime"></a>

### The full date long time ("F") format specifier

The "F" standard format specifier represents a custom date and time format string that is defined by the current <xref:System.Globalization.DateTimeFormatInfo.FullDateTimePattern?displayProperty=nameWithType> property. For example, the custom format string for the invariant culture is "dddd, dd MMMM yyyy HH:mm:ss".

The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that may control the formatting of the returned string. The custom format specifier that is returned by the <xref:System.Globalization.DateTimeFormatInfo.FullDateTimePattern> property of some cultures may not make use of all properties.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.FullDateTimePattern*>|Defines the overall format of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.DayNames*>|Defines the localized day names that can appear in the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.MonthNames*>|Defines the localized month names that can appear in the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.TimeSeparator*>|Defines the string that separates the hour, minute, and second components of a time.|
|<xref:System.Globalization.DateTimeFormatInfo.AMDesignator*>|Defines the string that indicates times from midnight to before noon in a 12-hour clock.|
|<xref:System.Globalization.DateTimeFormatInfo.PMDesignator*>|Defines the string that indicates times from noon to before midnight in a 12-hour clock.|

The following example uses the "F" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#4](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#4)]
[!code-vb[Formatting.DateAndTime.Standard#4](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#4)]

[Back to table](#table)

<a name="GeneralDateShortTime"></a>

### The general date short time ("g") format specifier

The "g" standard format specifier represents a combination of the short date ("d") and short time ("t") patterns, separated by a space.

The result string is affected by the formatting information of a specific <xref:System.Globalization.DateTimeFormatInfo> object. The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that may control the formatting of the returned string. The custom format specifier that is returned by the <xref:System.Globalization.DateTimeFormatInfo.ShortDatePattern*?displayProperty=nameWithType> and <xref:System.Globalization.DateTimeFormatInfo.ShortTimePattern?displayProperty=nameWithType> properties of some cultures may not make use of all properties.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.ShortDatePattern*>|Defines the format of the date component of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.ShortTimePattern*>|Defines the format of the time component of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.DateSeparator*>|Defines the string that separates the year, month, and day components of a date.|
|<xref:System.Globalization.DateTimeFormatInfo.TimeSeparator*>|Defines the string that separates the hour, minute, and second components of a time.|
|<xref:System.Globalization.DateTimeFormatInfo.AMDesignator*>|Defines the string that indicates times from midnight to before noon in a 12-hour clock.|
|<xref:System.Globalization.DateTimeFormatInfo.PMDesignator*>|Defines the string that indicates times from noon to before midnight in a 12-hour clock.|

The following example uses the "g" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#5](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#5)]
[!code-vb[Formatting.DateAndTime.Standard#5](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#5)]

[Back to table](#table)

<a name="GeneralDateLongTime"></a>

### The general date long time ("G") format specifier

The "G" standard format specifier represents a combination of the short date ("d") and long time ("T") patterns, separated by a space.

The result string is affected by the formatting information of a specific <xref:System.Globalization.DateTimeFormatInfo> object. The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that may control the formatting of the returned string. The custom format specifier that is returned by the <xref:System.Globalization.DateTimeFormatInfo.ShortDatePattern*?displayProperty=nameWithType> and <xref:System.Globalization.DateTimeFormatInfo.LongTimePattern?displayProperty=nameWithType> properties of some cultures may not make use of all properties.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.ShortDatePattern*>|Defines the format of the date component of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.LongTimePattern*>|Defines the format of the time component of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.DateSeparator*>|Defines the string that separates the year, month, and day components of a date.|
|<xref:System.Globalization.DateTimeFormatInfo.TimeSeparator*>|Defines the string that separates the hour, minute, and second components of a time.|
|<xref:System.Globalization.DateTimeFormatInfo.AMDesignator*>|Defines the string that indicates times from midnight to before noon in a 12-hour clock.|
|<xref:System.Globalization.DateTimeFormatInfo.PMDesignator*>|Defines the string that indicates times from noon to before midnight in a 12-hour clock.|

The following example uses the "G" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#6](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#6)]
[!code-vb[Formatting.DateAndTime.Standard#6](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#6)]

[Back to table](#table)

<a name="Roundtrip"></a>

### The round-trip ("O", "o") format specifier

The "O" or "o" standard format specifier represents a custom date and time format string using a pattern that preserves time zone information and emits a result string that complies with ISO 8601. For <xref:System.DateTime> values, this format specifier is designed to preserve date and time values along with the <xref:System.DateTime.Kind?displayProperty=nameWithType> property in text. The formatted string can be parsed back by using the <xref:System.DateTime.Parse%28System.String%2CSystem.IFormatProvider%2CSystem.Globalization.DateTimeStyles%29?displayProperty=nameWithType> or <xref:System.DateTime.ParseExact*?displayProperty=nameWithType> method if the `styles` parameter is set to <xref:System.Globalization.DateTimeStyles.RoundtripKind?displayProperty=nameWithType>.

For <xref:System.DateOnly> values, this format specifier produces a date-only ISO 8601 string in the format "yyyy-MM-dd". For <xref:System.TimeOnly> values, it produces a time-only ISO 8601 string in the format "HH:mm:ss.fffffff".

The "O" or "o" standard format specifier corresponds to the "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK" custom format string for <xref:System.DateTime> values and to the "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffzzz" custom format string for <xref:System.DateTimeOffset> values. In this string, the pairs of single quotation marks that delimit individual characters, such as the hyphens, the colons, and the letter "T", indicate that the individual character is a literal that cannot be changed. The apostrophes do not appear in the output string.

The "O" or "o" standard format specifier (and the "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK"  custom format string) takes advantage of the three ways that ISO 8601 represents time zone information to preserve the <xref:System.DateTime.Kind> property of <xref:System.DateTime> values:

- The time zone component of <xref:System.DateTimeKind.Local?displayProperty=nameWithType> date and time values is an offset from UTC (for example, +01:00, -07:00). All <xref:System.DateTimeOffset> values are also represented in this format.

- The time zone component of <xref:System.DateTimeKind.Utc?displayProperty=nameWithType> date and time values uses "Z" (which stands for zero offset) to represent UTC.

- <xref:System.DateTimeKind.Unspecified?displayProperty=nameWithType> date and time values have no time zone information.

Because the "O" or "o" standard format specifier conforms to an international standard, the formatting or parsing operation that uses the specifier always uses the invariant culture and the Gregorian calendar.

Strings that are passed to the `Parse`, `TryParse`, `ParseExact`, and `TryParseExact` methods of <xref:System.DateTime>, <xref:System.DateTimeOffset>, <xref:System.DateOnly>, and <xref:System.TimeOnly> can be parsed by using the "O" or "o" format specifier if they are in one of these formats. In the case of <xref:System.DateTime> objects, the parsing overload that you call should also include a `styles` parameter with a value of <xref:System.Globalization.DateTimeStyles.RoundtripKind?displayProperty=nameWithType>. Note that if you call a parsing method with the custom format string that corresponds to the "O" or "o" format specifier, you won't get the same results as "O" or "o". This is because parsing methods that use a custom format string can't parse the string representation of date and time values that lack a time zone component or use "Z" to indicate UTC.

The following example uses the "o" format specifier to display a series of <xref:System.DateTime> values and a <xref:System.DateTimeOffset> value on a system in the U.S. Pacific Time zone.

[!code-csharp[Formatting.DateAndTime.Standard#8](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/o1.cs#8)]
[!code-vb[Formatting.DateAndTime.Standard#8](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/o1.vb#8)]

The following example uses the "o" format specifier to create a formatted string, and then restores the original date and time value by calling a date and time `Parse` method.

[!code-csharp[Formatting.DateandTime.Standard#16](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Roundtrip1.cs#16)]
[!code-vb[Formatting.DateandTime.Standard#16](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/RoundTrip1.vb#16)]

[Back to table](#table)

<a name="RFC1123"></a>

### The RFC1123 ("R", "r") format specifier

The "R" or "r" standard format specifier represents a custom date and time format string that's defined by the <xref:System.Globalization.DateTimeFormatInfo.RFC1123Pattern?displayProperty=nameWithType> property. The pattern reflects a defined standard, and the property is read-only. Therefore, it is always the same, regardless of the culture used or the format provider supplied. The custom format string is "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'". When this standard format specifier is used, the formatting or parsing operation always uses the invariant culture.

The result string is affected by the following properties of the <xref:System.Globalization.DateTimeFormatInfo> object returned by the <xref:System.Globalization.DateTimeFormatInfo.InvariantInfo?displayProperty=nameWithType> property that represents the invariant culture.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.RFC1123Pattern*>|Defines the format of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.AbbreviatedDayNames*>|Defines the abbreviated day names that can appear in the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.AbbreviatedMonthNames*>|Defines the abbreviated month names that can appear in the result string.|

Although the RFC 1123 standard expresses a time as Coordinated Universal Time (UTC), the formatting operation does not modify the value of the <xref:System.DateTime> object that's being formatted. Therefore, you must convert the <xref:System.DateTime> value to UTC by calling the <xref:System.DateTime.ToUniversalTime*?displayProperty=nameWithType> method before you perform the formatting operation. In contrast, <xref:System.DateTimeOffset> values perform this conversion automatically; there's no need to call the <xref:System.DateTimeOffset.ToUniversalTime*?displayProperty=nameWithType> method before the formatting operation.

The following example uses the "r" format specifier to display a <xref:System.DateTime> and a <xref:System.DateTimeOffset> value on a system in the U.S. Pacific Time zone.

[!code-csharp[Formatting.DateAndTime.Standard#9](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#9)]
[!code-vb[Formatting.DateAndTime.Standard#9](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#9)]

[Back to table](#table)

<a name="Sortable"></a>

### The sortable ("s") format specifier

The "s" standard format specifier represents a custom date and time format string that is defined by the <xref:System.Globalization.DateTimeFormatInfo.SortableDateTimePattern?displayProperty=nameWithType> property. The pattern reflects a defined standard (ISO 8601), and the property is read-only. Therefore, it is always the same, regardless of the culture used or the format provider supplied. The custom format string is "yyyy'-'MM'-'dd'T'HH':'mm':'ss".

The purpose of the "s" format specifier is to produce result strings that sort consistently in ascending or descending order based on date and time values. As a result, although the "s" standard format specifier represents a date and time value in a consistent format, the formatting operation does not modify the value of the date and time object that is being formatted to reflect its <xref:System.DateTime.Kind?displayProperty=nameWithType> property or its <xref:System.DateTimeOffset.Offset*?displayProperty=nameWithType> value. For example, the result strings produced by formatting the date and time values 2014-11-15T18:32:17+00:00 and 2014-11-15T18:32:17+08:00 are identical.

When this standard format specifier is used, the formatting or parsing operation always uses the invariant culture.

The following example uses the "s" format specifier to display a <xref:System.DateTime> and a <xref:System.DateTimeOffset> value on a system in the U.S. Pacific Time zone.

[!code-csharp[Formatting.DateAndTime.Standard#10](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#10)]
[!code-vb[Formatting.DateAndTime.Standard#10](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#10)]

[Back to table](#table)

<a name="UniversalSortable"></a>

### The universal sortable ("u") format specifier

The "u" standard format specifier represents a custom date and time format string that is defined by the <xref:System.Globalization.DateTimeFormatInfo.UniversalSortableDateTimePattern?displayProperty=nameWithType> property. The pattern reflects a defined standard, and the property is read-only. Therefore, it is always the same, regardless of the culture used or the format provider supplied. The custom format string is "yyyy'-'MM'-'dd HH':'mm':'ss'Z'". When this standard format specifier is used, the formatting or parsing operation always uses the invariant culture.

Although the result string should express a time as Coordinated Universal Time (UTC), no conversion of the original <xref:System.DateTime> value is performed during the formatting operation. Therefore, you must convert a <xref:System.DateTime> value to UTC by calling the <xref:System.DateTime.ToUniversalTime*?displayProperty=nameWithType> method before formatting it.  In contrast, <xref:System.DateTimeOffset> values perform this conversion automatically; there is no need to call the <xref:System.DateTimeOffset.ToUniversalTime*?displayProperty=nameWithType> method before the formatting operation.

The following example uses the "u" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#13](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#13)]
[!code-vb[Formatting.DateAndTime.Standard#13](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#13)]

[Back to table](#table)

<a name="UniversalFull"></a>

### The universal full ("U") format specifier

The "U" standard format specifier represents a custom date and time format string that is defined by a specified culture's <xref:System.Globalization.DateTimeFormatInfo.FullDateTimePattern?displayProperty=nameWithType> property. The pattern is the same as the "F" pattern. However, the <xref:System.DateTime> value is automatically converted to UTC before it is formatted.

The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that may control the formatting of the returned string. The custom format specifier that is returned by the <xref:System.Globalization.DateTimeFormatInfo.FullDateTimePattern> property of some cultures may not make use of all properties.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.FullDateTimePattern*>|Defines the overall format of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.DayNames*>|Defines the localized day names that can appear in the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.MonthNames*>|Defines the localized month names that can appear in the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.TimeSeparator*>|Defines the string that separates the hour, minute, and second components of a time.|
|<xref:System.Globalization.DateTimeFormatInfo.AMDesignator*>|Defines the string that indicates times from midnight to before noon in a 12-hour clock.|
|<xref:System.Globalization.DateTimeFormatInfo.PMDesignator*>|Defines the string that indicates times from noon to before midnight in a 12-hour clock.|

The "U" format specifier is not supported by the <xref:System.DateTimeOffset> type and throws a <xref:System.FormatException> if it is used to format a <xref:System.DateTimeOffset> value.

The following example uses the "U" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#14](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#14)]
[!code-vb[Formatting.DateAndTime.Standard#14](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#14)]

[Back to table](#table)

## Time formats

This group includes the following formats:

- [The short time ("t") format specifier](#the-short-time-t-format-specifier)
- [The long time ("T") format specifier](#the-long-time-t-format-specifier)

<a name="ShortTime"></a>

### The short time ("t") format specifier

The "t" standard format specifier represents a custom date and time format string that is defined by the current <xref:System.Globalization.DateTimeFormatInfo.ShortTimePattern?displayProperty=nameWithType> property. For example, the custom format string for the invariant culture is "HH:mm".

The result string is affected by the formatting information of a specific <xref:System.Globalization.DateTimeFormatInfo> object. The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that may control the formatting of the returned string. The custom format specifier that is returned by the <xref:System.Globalization.DateTimeFormatInfo.ShortTimePattern?displayProperty=nameWithType> property of some cultures may not make use of all properties.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.ShortTimePattern*>|Defines the format of the time component of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.TimeSeparator*>|Defines the string that separates the hour, minute, and second components of a time.|
|<xref:System.Globalization.DateTimeFormatInfo.AMDesignator*>|Defines the string that indicates times from midnight to before noon in a 12-hour clock.|
|<xref:System.Globalization.DateTimeFormatInfo.PMDesignator*>|Defines the string that indicates times from noon to before midnight in a 12-hour clock.|

The following example uses the "t" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#11](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#11)]
[!code-vb[Formatting.DateAndTime.Standard#11](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#11)]

[Back to table](#table)

<a name="LongTime"></a>

### The long time ("T") format specifier

The "T" standard format specifier represents a custom date and time format string that is defined by a specific culture's <xref:System.Globalization.DateTimeFormatInfo.LongTimePattern?displayProperty=nameWithType> property. For example, the custom format string for the invariant culture is "HH:mm:ss".

The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that may control the formatting of the returned string. The custom format specifier that is returned by the <xref:System.Globalization.DateTimeFormatInfo.LongTimePattern?displayProperty=nameWithType> property of some cultures may not make use of all properties.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.LongTimePattern*>|Defines the format of the time component of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.TimeSeparator*>|Defines the string that separates the hour, minute, and second components of a time.|
|<xref:System.Globalization.DateTimeFormatInfo.AMDesignator*>|Defines the string that indicates times from midnight to before noon in a 12-hour clock.|
|<xref:System.Globalization.DateTimeFormatInfo.PMDesignator*>|Defines the string that indicates times from noon to before midnight in a 12-hour clock.|

The following example uses the "T" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#12](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#12)]
[!code-vb[Formatting.DateAndTime.Standard#12](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#12)]

[Back to table](#table)

## Partial date formats

This group includes the following formats:

- [The month ("M", "m") format specifier](#the-month-m-m-format-specifier)
- [The year month ("Y", "y") format specifier](#the-year-month-y-y-format-specifier)

<a name="MonthDay"></a>

### The month ("M", "m") format specifier

The "M" or "m" standard format specifier represents a custom date and time format string that is defined by the current <xref:System.Globalization.DateTimeFormatInfo.MonthDayPattern?displayProperty=nameWithType> property. For example, the custom format string for the invariant culture is "MMMM dd".

The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that control the formatting of the returned string.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.MonthDayPattern*>|Defines the overall format of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.MonthNames*>|Defines the localized month names that can appear in the result string.|

The following example uses the "m" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#7](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#7)]
[!code-vb[Formatting.DateAndTime.Standard#7](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#7)]

[Back to table](#table)

<a name="YearMonth"></a>

### The year month ("Y", "y") format specifier

The "Y" or "y" standard format specifier represents a custom date and time format string that is defined by the <xref:System.Globalization.DateTimeFormatInfo.YearMonthPattern?displayProperty=nameWithType> property of a specified culture. For example, the custom format string for the invariant culture is "yyyy MMMM".

The following table lists the <xref:System.Globalization.DateTimeFormatInfo> object properties that control the formatting of the returned string.

|Property|Description|
|--------------|-----------------|
|<xref:System.Globalization.DateTimeFormatInfo.YearMonthPattern*>|Defines the overall format of the result string.|
|<xref:System.Globalization.DateTimeFormatInfo.MonthNames*>|Defines the localized month names that can appear in the result string.|

The following example uses the "y" format specifier to display a date and time value.

[!code-csharp[Formatting.DateAndTime.Standard#15](../../../samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Standard/cs/Standard1.cs#15)]
[!code-vb[Formatting.DateAndTime.Standard#15](../../../samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Standard/vb/Standard1.vb#15)]

[Back to table](#table)

<a name="Notes"></a>

## DateOnly and TimeOnly formatting

<xref:System.DateOnly> and <xref:System.TimeOnly> types support a subset of the standard date and time format strings:

- **<xref:System.DateOnly>** supports date-related format specifiers:
  - "d" (short date), "D" (long date)
  - "M" or "m" (month/day)
  - "Y" or "y" (year/month)
  - "O" or "o" (round-trip, date portion only)
  - "R" or "r" (RFC1123, date portion only)

- **<xref:System.TimeOnly>** supports time-related format specifiers:
  - "t" (short time), "T" (long time)
  - "O" or "o" (round-trip, time portion only)
  - "R" or "r" (RFC1123, time portion only)

Format specifiers that combine both date and time information (such as "f", "F", "g", "G", "s", "u", "U") throw a <xref:System.FormatException> when used with <xref:System.DateOnly> or <xref:System.TimeOnly>.

## Control Panel settings

In Windows, the settings in the **Regional and Language Options** item in Control Panel influence the result string produced by a formatting operation. These settings are used to initialize the <xref:System.Globalization.DateTimeFormatInfo> object associated with the current culture, which provides values used to govern formatting. Computers that use different settings generate different result strings.

In addition, if you use the <xref:System.Globalization.CultureInfo.%23ctor%28System.String%29> constructor to instantiate a new <xref:System.Globalization.CultureInfo> object that represents the same culture as the current system culture, any customizations established by the **Regional and Language Options** item in Control Panel will be applied to the new <xref:System.Globalization.CultureInfo> object. You can use the <xref:System.Globalization.CultureInfo.%23ctor%28System.String%2CSystem.Boolean%29> constructor to create a <xref:System.Globalization.CultureInfo> object that does not reflect a system's customizations.

## DateTimeFormatInfo properties

Formatting is influenced by properties of the current <xref:System.Globalization.DateTimeFormatInfo> object, which is provided implicitly by the current culture or explicitly by the <xref:System.IFormatProvider> parameter of the method that invokes formatting. For the <xref:System.IFormatProvider> parameter, your application should specify a <xref:System.Globalization.CultureInfo> object, which represents a culture, or a <xref:System.Globalization.DateTimeFormatInfo> object, which represents a particular culture's date and time formatting conventions. Many of the standard date and time format specifiers are aliases for formatting patterns defined by properties of the current <xref:System.Globalization.DateTimeFormatInfo> object. Your application can change the result produced by some standard date and time format specifiers by changing the corresponding date and time format patterns of the corresponding <xref:System.Globalization.DateTimeFormatInfo> property.

## See also

- <xref:System.DateTime?displayProperty=nameWithType>
- <xref:System.DateTimeOffset?displayProperty=nameWithType>
- [Formatting Types](formatting-types.md)
- [Custom Date and Time Format Strings](custom-date-and-time-format-strings.md)
- [Sample: .NET Core WinForms Formatting Utility (C#)](/samples/dotnet/samples/windowsforms-formatting-utility-cs)
- [Sample: .NET Core WinForms Formatting Utility (Visual Basic)](/samples/dotnet/samples/windowsforms-formatting-utility-vb)
