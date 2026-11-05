---
title: "Custom date and time format strings"
description: Learn to use custom date and time format strings to convert DateTime or DateTimeOffset values into text representations, or to parse strings for dates & times.
ms.date: 05/12/2022
ms.topic: reference
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
ms.assetid: 98b374e3-0cc2-4c78-ab44-efb671d71984
ai-usage: ai-assisted
---

# Custom date and time format strings

A date and time format string defines the text representation of a <xref:System.DateTime> or <xref:System.DateTimeOffset> value that results from a formatting operation. It can also define the representation of a date and time value that is required in a parsing operation in order to successfully convert the string to a date and time. A custom format string consists of one or more custom date and time format specifiers. Any string that is not a [standard date and time format string](standard-date-and-time-format-strings.md) is interpreted as a custom date and time format string.

> [!TIP]
> You can download the **Formatting Utility**, a .NET Core Windows Forms application that lets you apply format strings to either numeric or date and time values and displays the result string. Source code is available for [C#](/samples/dotnet/samples/windowsforms-formatting-utility-cs) and [Visual Basic](/samples/dotnet/samples/windowsforms-formatting-utility-vb).

Custom date and time format strings can be used with both <xref:System.DateTime> and <xref:System.DateTimeOffset> values.

In formatting operations, custom date and time format strings can be used either with the `ToString` method of a date and time instance or with a method that supports composite formatting. The following example illustrates both uses.

[!code-csharp[Formatting.DateAndTime.Custom#17](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/custandformatting1.cs#17)]
[!code-vb[Formatting.DateAndTime.Custom#17](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/custandformatting1.vb#17)]

In parsing operations, custom date and time format strings can be used with the <xref:System.DateTime.ParseExact*?displayProperty=nameWithType>, <xref:System.DateTime.TryParseExact*?displayProperty=nameWithType>, <xref:System.DateTimeOffset.ParseExact*?displayProperty=nameWithType>, and <xref:System.DateTimeOffset.TryParseExact*?displayProperty=nameWithType> methods. These methods require that an input string conforms exactly to a particular pattern for the parse operation to succeed. The following example illustrates a call to the <xref:System.DateTimeOffset.ParseExact%28System.String%2CSystem.String%2CSystem.IFormatProvider%29?displayProperty=nameWithType> method to parse a date that must include a day, a month, and a two-digit year.

[!code-csharp[Formatting.DateAndTime.Custom#18](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/custandparsing1.cs#18)]
[!code-vb[Formatting.DateAndTime.Custom#18](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/custandparsing1.vb#18)]

<a name="table"></a> The following table describes the custom date and time format specifiers and displays a result string produced by each format specifier. By default, result strings reflect the formatting conventions of the en-US culture. If a particular format specifier produces a localized result string, the example also notes the culture to which the result string applies. For more information about using custom date and time format strings, see the [Notes](#notes) section.

| Format specifier | Description | Examples |
|--|--|--|
| "d" | The day of the month, from 1 to 31.<br /><br /> More information: [The "d" Custom Format Specifier](#dSpecifier). | 2009-06-01T13:45:30 -> 1<br /><br /> 2009-06-15T13:45:30 -> 15 |
| "dd" | The day of the month, from 01 to 31.<br /><br /> More information: [The "dd" Custom Format Specifier](#ddSpecifier). | 2009-06-01T13:45:30 -> 01<br /><br /> 2009-06-15T13:45:30 -> 15 |
| "ddd" | The abbreviated name of the day of the week.<br /><br /> More information: [The "ddd" Custom Format Specifier](#dddSpecifier). | 2009-06-15T13:45:30 -> Mon (en-US)<br /><br /> 2009-06-15T13:45:30 -> Пн (ru-RU)<br /><br /> 2009-06-15T13:45:30 -> lun. (fr-FR) |
| "dddd" | The full name of the day of the week.<br /><br /> More information: [The "dddd" Custom Format Specifier](#ddddSpecifier). | 2009-06-15T13:45:30 -> Monday (en-US)<br /><br /> 2009-06-15T13:45:30 -> понедельник (ru-RU)<br /><br /> 2009-06-15T13:45:30 -> lundi (fr-FR) |
| "f" | The tenths of a second in a date and time value.<br /><br /> More information: [The "f" Custom Format Specifier](#fSpecifier). | 2009-06-15T13:45:30.6170000 -> 6<br /><br /> 2009-06-15T13:45:30.05 -> 0 |
| "ff" | The hundredths of a second in a date and time value.<br /><br /> More information: [The "ff" Custom Format Specifier](#ffSpecifier). | 2009-06-15T13:45:30.6170000 -> 61<br /><br /> 2009-06-15T13:45:30.0050000 -> 00 |
| "fff" | The milliseconds in a date and time value.<br /><br /> More information: [The "fff" Custom Format Specifier](#fffSpecifier). | 6/15/2009 13:45:30.617 -> 617<br /><br /> 6/15/2009 13:45:30.0005 -> 000 |
| "ffff" | The ten thousandths of a second in a date and time value.<br /><br /> More information: [The "ffff" Custom Format Specifier](#ffffSpecifier). | 2009-06-15T13:45:30.6175000 -> 6175<br /><br /> 2009-06-15T13:45:30.0000500  -> 0000 |
| "fffff" | The hundred thousandths of a second in a date and time value.<br /><br /> More information: [The "fffff" Custom Format Specifier](#fffffSpecifier). | 2009-06-15T13:45:30.6175400 -> 61754<br /><br /> 6/15/2009 13:45:30.000005 -> 00000 |
| "ffffff" | The millionths of a second in a date and time value.<br /><br /> More information: [The "ffffff" Custom Format Specifier](#ffffffSpecifier). | 2009-06-15T13:45:30.6175420 -> 617542<br /><br /> 2009-06-15T13:45:30.0000005 -> 000000 |
| "fffffff" | The ten millionths of a second in a date and time value.<br /><br /> More information: [The "fffffff" Custom Format Specifier](#fffffffSpecifier). | 2009-06-15T13:45:30.6175425 -> 6175425<br /><br /> 2009-06-15T13:45:30.0001150 -> 0001150 |
| "F" | If non-zero, the tenths of a second in a date and time value.<br /><br /> More information: [The "F" Custom Format Specifier](#F_Specifier). | 2009-06-15T13:45:30.6170000 -> 6<br /><br /> 2009-06-15T13:45:30.0500000 -> (no output) |
| "FF" | If non-zero, the hundredths of a second in a date and time value.<br /><br /> More information: [The "FF" Custom Format Specifier](#FF_Specifier). | 2009-06-15T13:45:30.6170000 -> 61<br /><br /> 2009-06-15T13:45:30.0050000 -> (no output) |
| "FFF" | If non-zero, the milliseconds in a date and time value.<br /><br /> More information: [The "FFF" Custom Format Specifier](#FFF_Specifier). | 2009-06-15T13:45:30.6170000 -> 617<br /><br /> 2009-06-15T13:45:30.0005000 -> (no output) |
| "FFFF" | If non-zero, the ten thousandths of a second in a date and time value.<br /><br /> More information: [The "FFFF" Custom Format Specifier](#FFFF_Specifier). | 2009-06-15T13:45:30.5275000 -> 5275<br /><br /> 2009-06-15T13:45:30.0000500 -> (no output) |
| "FFFFF" | If non-zero, the hundred thousandths of a second in a date and time value.<br /><br /> More information: [The "FFFFF" Custom Format Specifier](#FFFFF_Specifier). | 2009-06-15T13:45:30.6175400 -> 61754<br /><br /> 2009-06-15T13:45:30.0000050 -> (no output) |
| "FFFFFF" | If non-zero, the millionths of a second in a date and time value.<br /><br /> More information: [The "FFFFFF" Custom Format Specifier](#FFFFFF_Specifier). | 2009-06-15T13:45:30.6175420 -> 617542<br /><br /> 2009-06-15T13:45:30.0000005 -> (no output) |
| "FFFFFFF" | If non-zero, the ten millionths of a second in a date and time value.<br /><br /> More information: [The "FFFFFFF" Custom Format Specifier](#FFFFFFF_Specifier). | 2009-06-15T13:45:30.6175425 -> 6175425<br /><br /> 2009-06-15T13:45:30.0001150 -> 000115 |
| "g", "gg" | The period or era.<br /><br /> More information: [The "g" or "gg" Custom Format Specifier](#gSpecifier). | 2009-06-15T13:45:30.6170000 -> A.D. |
| "h" | The hour, using a 12-hour clock from 1 to 12.<br /><br /> More information: [The "h" Custom Format Specifier](#hSpecifier). | 2009-06-15T01:45:30 -> 1<br /><br /> 2009-06-15T13:45:30 -> 1 |
| "hh" | The hour, using a 12-hour clock from 01 to 12.<br /><br /> More information: [The "hh" Custom Format Specifier](#hhSpecifier). | 2009-06-15T01:45:30 -> 01<br /><br /> 2009-06-15T13:45:30 -> 01 |
| "H" | The hour, using a 24-hour clock from 0 to 23.<br /><br /> More information: [The "H" Custom Format Specifier](#H_Specifier). | 2009-06-15T01:45:30 -> 1<br /><br /> 2009-06-15T13:45:30 -> 13 |
| "HH" | The hour, using a 24-hour clock from 00 to 23.<br /><br /> More information: [The "HH" Custom Format Specifier](#HH_Specifier). | 2009-06-15T01:45:30 -> 01<br /><br /> 2009-06-15T13:45:30 -> 13 |
| "K" | Time zone information.<br /><br /> More information: [The "K" Custom Format Specifier](#KSpecifier). | With <xref:System.DateTime> values:<br /><br /> 2009-06-15T13:45:30, Kind Unspecified -><br /><br /> 2009-06-15T13:45:30, Kind Utc -> Z<br /><br /> 2009-06-15T13:45:30, Kind Local -> -07:00 (depends on local computer settings)<br /><br /> With <xref:System.DateTimeOffset> values:<br /><br /> 2009-06-15T01:45:30-07:00 --> -07:00<br /><br /> 2009-06-15T08:45:30+00:00 --> +00:00 |
| "m" | The minute, from 0 to 59.<br /><br /> More information: [The "m" Custom Format Specifier](#mSpecifier). | 2009-06-15T01:09:30 -> 9<br /><br /> 2009-06-15T13:29:30 -> 29 |
| "mm" | The minute, from 00 to 59.<br /><br /> More information: [The "mm" Custom Format Specifier](#mmSpecifier). | 2009-06-15T01:09:30 -> 09<br /><br /> 2009-06-15T01:45:30 -> 45 |
| "M" | The month, from 1 to 12.<br /><br /> More information: [The "M" Custom Format Specifier](#M_Specifier). | 2009-06-15T13:45:30 -> 6 |
| "MM" | The month, from 01 to 12.<br /><br /> More information: [The "MM" Custom Format Specifier](#MM_Specifier). | 2009-06-15T13:45:30 -> 06 |
| "MMM" | The abbreviated name of the month. When used with "d" or "dd" specifiers, uses genitive names, except with .NET Framework. (This is known issue that's fixed in .NET.)<br /><br /> More information: [The "MMM" Custom Format Specifier](#MMM_Specifier). | 2009-06-15T13:45:30 -> Jun (en-US)<br /><br /> 2009-06-15T13:45:30 -> juin (fr-FR)<br /><br /> 2009-06-15T13:45:30 -> Jun (zu-ZA) |
| "MMMM" | The full name of the month. When used with "d" or "dd" specifiers, uses genitive names.<br /><br /> More information: [The "MMMM" Custom Format Specifier](#MMMM_Specifier). | 2009-06-15T13:45:30 -> June (en-US)<br /><br /> 2009-06-15T13:45:30 -> juni (da-DK)<br /><br /> 2009-06-15T13:45:30 -> uJuni (zu-ZA) |
| "s" | The second, from 0 to 59.<br /><br /> More information: [The "s" Custom Format Specifier](#sSpecifier). | 2009-06-15T13:45:09 -> 9 |
| "ss" | The second, from 00 to 59.<br /><br /> More information: [The "ss" Custom Format Specifier](#ssSpecifier). | 2009-06-15T13:45:09 -> 09 |
| "t" | The first character of the AM/PM designator.<br /><br /> More information: [The "t" Custom Format Specifier](#tSpecifier). | 2009-06-15T13:45:30 -> P (en-US)<br /><br /> 2009-06-15T13:45:30 -> 午 (ja-JP)<br /><br /> 2009-06-15T13:45:30 ->  (fr-FR) |
| "tt" | The AM/PM designator.<br /><br /> More information: [The "tt" Custom Format Specifier](#ttSpecifier). | 2009-06-15T13:45:30 -> PM (en-US)<br /><br /> 2009-06-15T13:45:30 -> 午後 (ja-JP)<br /><br /> 2009-06-15T13:45:30 ->  (fr-FR) |
| "y" | The year, from 0 to 99.<br /><br /> More information: [The "y" Custom Format Specifier](#ySpecifier). | 0001-01-01T00:00:00 -> 1<br /><br /> 0900-01-01T00:00:00 -> 0<br /><br /> 1900-01-01T00:00:00 -> 0<br /><br /> 2009-06-15T13:45:30 -> 9<br /><br /> 2019-06-15T13:45:30 -> 19 |
| "yy" | The year, from 00 to 99.<br /><br /> More information: [The "yy" Custom Format Specifier](#yySpecifier). | 0001-01-01T00:00:00 -> 01<br /><br /> 0900-01-01T00:00:00 -> 00<br /><br /> 1900-01-01T00:00:00 -> 00<br /><br /> 2019-06-15T13:45:30 -> 19 |
| "yyy" | The year, with a minimum of three digits.<br /><br /> More information: [The "yyy" Custom Format Specifier](#yyySpecifier). | 0001-01-01T00:00:00 -> 001<br /><br /> 0900-01-01T00:00:00 -> 900<br /><br /> 1900-01-01T00:00:00 -> 1900<br /><br /> 2009-06-15T13:45:30 -> 2009 |
| "yyyy" | The year as a four-digit number.<br /><br /> More information: [The "yyyy" Custom Format Specifier](#yyyySpecifier). | 0001-01-01T00:00:00 -> 0001<br /><br /> 0900-01-01T00:00:00 -> 0900<br /><br /> 1900-01-01T00:00:00 -> 1900<br /><br /> 2009-06-15T13:45:30 -> 2009 |
| "yyyyy" | The year as a five-digit number.<br /><br /> More information: [The "yyyyy" Custom Format Specifier](#yyyyySpecifier). | 0001-01-01T00:00:00 -> 00001<br /><br /> 2009-06-15T13:45:30 -> 02009 |
| "z" | Hours offset from UTC, with no leading zeros.<br /><br /> More information: [The "z" Custom Format Specifier](#zSpecifier). | 2009-06-15T13:45:30-07:00 -> -7 |
| "zz" | Hours offset from UTC, with a leading zero for a single-digit value.<br /><br /> More information: [The "zz" Custom Format Specifier](#zzSpecifier). | 2009-06-15T13:45:30-07:00 -> -07 |
| "zzz" | Hours and minutes offset from UTC.<br /><br /> More information: [The "zzz" Custom Format Specifier](#zzzSpecifier). | 2009-06-15T13:45:30-07:00 -> -07:00 |
| ":" | The time separator.<br /><br /> More information: [The ":" Custom Format Specifier](#timeSeparator). | 2009-06-15T13:45:30 -> : (en-US)<br /><br /> 2009-06-15T13:45:30 -> . (it-IT)<br /><br /> 2009-06-15T13:45:30 -> : (ja-JP) |
| "/" | The date separator.<br /><br /> More Information: [The "/" Custom Format Specifier](#dateSeparator). | 2009-06-15T13:45:30 -> / (en-US)<br /><br /> 2009-06-15T13:45:30 -> - (ar-DZ)<br /><br /> 2009-06-15T13:45:30 -> . (tr-TR) |
| "*string*"<br /><br /> '*string*' | Literal string delimiter.<br /><br /> More information: [Character literals](#Literals). | 2009-06-15T13:45:30 ("arr:" h:m t) -> arr: 1:45 P<br /><br /> 2009-06-15T13:45:30 ('arr:' h:m t) -> arr: 1:45 P |
| % | Defines the following character as a custom format specifier.<br /><br /> More information: [Using Single Custom Format Specifiers](#UsingSingleSpecifiers). | 2009-06-15T13:45:30 (%h) -> 1 |
| &#92; | The escape character.<br /><br /> More information: [Character literals](#Literals) and [Using the Escape Character](#escape). | 2009-06-15T13:45:30 (h \h) -> 1 h |
| Any other character | The character is copied to the result string unchanged.<br /><br /> More information: [Character literals](#Literals). | 2009-06-15T01:45:30 (arr hh:mm t) -> arr 01:45 A |

The following sections provide additional information about each custom date and time format specifier. Unless otherwise noted, each specifier produces an identical string representation regardless of whether it's used with a <xref:System.DateTime> value or a <xref:System.DateTimeOffset> value.

## Day "d" format specifier

### <a name="dSpecifier"></a> The "d" custom format specifier

The "d" custom format specifier represents the day of the month as a number from 1 to 31. A single-digit day is formatted without a leading zero.

If the "d" format specifier is used without other custom format specifiers, it's interpreted as the "d" standard date and time format specifier. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "d" custom format specifier in several format strings.

[!code-csharp[Formatting.DateAndTime.Custom#1](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#1)]
[!code-vb[Formatting.DateAndTime.Custom#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#1)]

[Back to table](#table)

### <a name="ddSpecifier"></a> The "dd" custom format specifier

The "dd" custom format string represents the day of the month as a number from 01 to 31. A single-digit day is formatted with a leading zero.

The following example includes the "dd" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#2](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#2)]
[!code-vb[Formatting.DateAndTime.Custom#2](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#2)]

[Back to table](#table)

### <a name="dddSpecifier"></a> The "ddd" custom format specifier

The "ddd" custom format specifier represents the abbreviated name of the day of the week. The localized abbreviated name of the day of the week is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.AbbreviatedDayNames?displayProperty=nameWithType> property of the current or specified culture.

The following example includes the "ddd" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#3](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#3)]
[!code-vb[Formatting.DateAndTime.Custom#3](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#3)]

[Back to table](#table)

### <a name="ddddSpecifier"></a> The "dddd" custom format specifier

The "dddd" custom format specifier (plus any number of additional "d" specifiers) represents the full name of the day of the week. The localized name of the day of the week is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.DayNames?displayProperty=nameWithType> property of the current or specified culture.

The following example includes the "dddd" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#4](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#4)]
[!code-vb[Formatting.DateAndTime.Custom#4](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#4)]

[Back to table](#table)

## Lowercase seconds "f" fraction specifier

### <a name="fSpecifier"></a> The "f" custom format specifier

The "f" custom format specifier represents the most significant digit of the seconds fraction; that is, it represents the tenths of a second in a date and time value.

If the "f" format specifier is used without other format specifiers, it's interpreted as the "f" standard date and time format specifier. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

When you use "f" format specifiers as part of a format string supplied to the <xref:System.DateTime.ParseExact*>, <xref:System.DateTime.TryParseExact*>, <xref:System.DateTimeOffset.ParseExact*>, or <xref:System.DateTimeOffset.TryParseExact*> method, the number of "f" format specifiers indicates the number of most significant digits of the seconds fraction that must be present to successfully parse the string.

The following example includes the "f" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#5](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#5)]
[!code-vb[Formatting.DateAndTime.Custom#5](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#5)]

[Back to table](#table)

### <a name="ffSpecifier"></a> The "ff" custom format specifier

The "ff" custom format specifier represents the two most significant digits of the seconds fraction; that is, it represents the hundredths of a second in a date and time value.

following example includes the "ff" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#5](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#5)]
[!code-vb[Formatting.DateAndTime.Custom#5](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#5)]

[Back to table](#table)

### <a name="fffSpecifier"></a> The "fff" custom format specifier

The "fff" custom format specifier represents the three most significant digits of the seconds fraction; that is, it represents the milliseconds in a date and time value.

The following example includes the "fff" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#5](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#5)]
[!code-vb[Formatting.DateAndTime.Custom#5](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#5)]

[Back to table](#table)

### <a name="ffffSpecifier"></a> The "ffff" custom format specifier

The "ffff" custom format specifier represents the four most significant digits of the seconds fraction; that is, it represents the ten thousandths of a second in a date and time value.

Although it's possible to display the ten thousandths of a second component of a time value, that value may not be meaningful. The precision of date and time values depends on the resolution of the system clock. On the Windows NT version 3.5 (and later) and Windows Vista operating systems, the clock's resolution is approximately 10-15 milliseconds.

[Back to table](#table)

### <a name="fffffSpecifier"></a> The "fffff" custom format specifier

The "fffff" custom format specifier represents the five most significant digits of the seconds fraction; that is, it represents the hundred thousandths of a second in a date and time value.

Although it's possible to display the hundred thousandths of a second component of a time value, that value may not be meaningful. The precision of date and time values depends on the resolution of the system clock. On the Windows NT 3.5 (and later) and Windows Vista operating systems, the clock's resolution is approximately 10-15 milliseconds.

[Back to table](#table)

### <a name="ffffffSpecifier"></a> The "ffffff" custom format specifier

The "ffffff" custom format specifier represents the six most significant digits of the seconds fraction; that is, it represents the millionths of a second in a date and time value.

Although it's possible to display the millionths of a second component of a time value, that value may not be meaningful. The precision of date and time values depends on the resolution of the system clock. On the Windows NT 3.5 (and later) and Windows Vista operating systems, the clock's resolution is approximately 10-15 milliseconds.

[Back to table](#table)

### <a name="fffffffSpecifier"></a> The "fffffff" custom format specifier

The "fffffff" custom format specifier represents the seven most significant digits of the seconds fraction; that is, it represents the ten millionths of a second in a date and time value.

Although it's possible to display the ten millionths of a second component of a time value, that value may not be meaningful. The precision of date and time values depends on the resolution of the system clock. On the Windows NT 3.5 (and later) and Windows Vista operating systems, the clock's resolution is approximately 10-15 milliseconds.

[Back to table](#table)

## Uppercase seconds "F" fraction specifier

### <a name="F_Specifier"></a> The "F" custom format specifier

The "F" custom format specifier represents the most significant digit of the seconds fraction; that is, it represents the tenths of a second in a date and time value. Nothing is displayed if the digit is zero, and the decimal point that follows the number of seconds is also not displayed.

If the "F" format specifier is used without other format specifiers, it's interpreted as the "F" standard date and time format specifier. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The number of "F" format specifiers used with the <xref:System.DateTime.ParseExact*>, <xref:System.DateTime.TryParseExact*>, <xref:System.DateTimeOffset.ParseExact*>, or <xref:System.DateTimeOffset.TryParseExact*> method indicates the maximum number of most significant digits of the seconds fraction that can be present to successfully parse the string.

The following example includes the "F" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#5](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#5)]
[!code-vb[Formatting.DateAndTime.Custom#5](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#5)]

[Back to table](#table)

### <a name="FF_Specifier"></a> The "FF" custom format specifier

The "FF" custom format specifier represents the two most significant digits of the seconds fraction; that is, it represents the hundredths of a second in a date and time value. Trailing zeros aren't displayed. Nothing is displayed if the two significant digits are zero, and in that case the decimal point that follows the number of seconds is also not displayed.

The following example includes the "FF" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#5](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#5)]
[!code-vb[Formatting.DateAndTime.Custom#5](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#5)]

[Back to table](#table)

### <a name="FFF_Specifier"></a> The "FFF" custom format specifier

The "FFF" custom format specifier represents the three most significant digits of the seconds fraction; that is, it represents the milliseconds in a date and time value. Trailing zeros aren't displayed. Nothing is displayed if the three significant digits are zero, and in that case the decimal point that follows the number of seconds is also not displayed.

The following example includes the "FFF" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#5](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#5)]
[!code-vb[Formatting.DateAndTime.Custom#5](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#5)]

[Back to table](#table)

### <a name="FFFF_Specifier"></a> The "FFFF" custom format specifier

The "FFFF" custom format specifier represents the four most significant digits of the seconds fraction; that is, it represents the ten thousandths of a second in a date and time value. Trailing zeros aren't displayed. Nothing is displayed if the four significant digits are zero, and in that case the decimal point that follows the number of seconds is also not displayed.

Although it's possible to display the ten thousandths of a second component of a time value, that value may not be meaningful. The precision of date and time values depends on the resolution of the system clock. On the Windows NT 3.5 (and later) and Windows Vista operating systems, the clock's resolution is approximately 10-15 milliseconds.

[Back to table](#table)

### <a name="FFFFF_Specifier"></a> The "FFFFF" custom format specifier

The "FFFFF" custom format specifier represents the five most significant digits of the seconds fraction; that is, it represents the hundred thousandths of a second in a date and time value. Trailing zeros aren't displayed. Nothing is displayed if the five significant digits are zero, and in that case the decimal point that follows the number of seconds is also not displayed.

Although it's possible to display the hundred thousandths of a second component of a time value, that value may not be meaningful. The precision of date and time values depends on the resolution of the system clock. On the Windows NT 3.5 (and later) and Windows Vista operating systems, the clock's resolution is approximately 10-15 milliseconds.

[Back to table](#table)

### <a name="FFFFFF_Specifier"></a> The "FFFFFF" custom format specifier

The "FFFFFF" custom format specifier represents the six most significant digits of the seconds fraction; that is, it represents the millionths of a second in a date and time value. Trailing zeros aren't displayed. Nothing is displayed if the six significant digits are zero, and in that case the decimal point that follows the number of seconds is also not displayed.

Although it's possible to display the millionths of a second component of a time value, that value may not be meaningful. The precision of date and time values depends on the resolution of the system clock. On the Windows NT 3.5 (and later) and Windows Vista operating systems, the clock's resolution is approximately 10-15 milliseconds.

[Back to table](#table)

### <a name="FFFFFFF_Specifier"></a> The "FFFFFFF" custom format specifier

The "FFFFFFF" custom format specifier represents the seven most significant digits of the seconds fraction; that is, it represents the ten millionths of a second in a date and time value. Trailing zeros aren't displayed. Nothing is displayed if the seven significant digits are zero, and in that case the decimal point that follows the number of seconds is also not displayed.

Although it's possible to display the ten millionths of a second component of a time value, that value may not be meaningful. The precision of date and time values depends on the resolution of the system clock. On the Windows NT 3.5 (and later) and Windows Vista operating systems, the clock's resolution is approximately 10-15 milliseconds.

[Back to table](#table)

## Era "g" format specifier

### <a name="gSpecifier"></a> The "g" or "gg" custom format specifier

The "g" or "gg" custom format specifiers (plus any number of additional "g" specifiers) represents the period or era, such as A.D. The formatting operation ignores this specifier if the date to be formatted doesn't have an associated period or era string.

If the "g" format specifier is used without other custom format specifiers, it's interpreted as the "g" standard date and time format specifier. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "g" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#6](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#6)]
[!code-vb[Formatting.DateAndTime.Custom#6](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#6)]

[Back to table](#table)

## Lowercase hour "h" format specifier

### <a name="hSpecifier"></a> The "h" custom format specifier

The "h" custom format specifier represents the hour as a number from 1 to 12; that is, the hour is represented by a 12-hour clock that counts the whole hours since midnight or noon. A particular hour after midnight is indistinguishable from the same hour after noon. The hour is not rounded, and a single-digit hour is formatted without a leading zero. For example, given a time of 5:43 in the morning or afternoon, this custom format specifier displays "5".

If the "h" format specifier is used without other custom format specifiers, it's interpreted as a standard date and time format specifier and throws a <xref:System.FormatException>. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "h" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#7](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#7)]
[!code-vb[Formatting.DateAndTime.Custom#7](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#7)]

[Back to table](#table)

### <a name="hhSpecifier"></a> The "hh" custom format specifier

The "hh" custom format specifier (plus any number of additional "h" specifiers) represents the hour as a number from 01 to 12; that is, the hour is represented by a 12-hour clock that counts the whole hours since midnight or noon. A particular hour after midnight is indistinguishable from the same hour after noon. The hour is not rounded, and a single-digit hour is formatted with a leading zero. For example, given a time of 5:43 in the morning or afternoon, this format specifier displays "05".

The following example includes the "hh" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#8](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#8)]
[!code-vb[Formatting.DateAndTime.Custom#8](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#8)]

[Back to table](#table)

## Uppercase hour "H" format specifier

### <a name="H_Specifier"></a> The "H" custom format specifier

The "H" custom format specifier represents the hour as a number from 0 to 23; that is, the hour is represented by a zero-based 24-hour clock that counts the hours since midnight. A single-digit hour is formatted without a leading zero.

If the "H" format specifier is used without other custom format specifiers, it's interpreted as a standard date and time format specifier and throws a <xref:System.FormatException>. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "H" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#9](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#9)]
[!code-vb[Formatting.DateAndTime.Custom#9](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#9)]

[Back to table](#table)

### <a name="HH_Specifier"></a> The "HH" custom format specifier

The "HH" custom format specifier (plus any number of additional "H" specifiers) represents the hour as a number from 00 to 23; that is, the hour is represented by a zero-based 24-hour clock that counts the hours since midnight. A single-digit hour is formatted with a leading zero.

> [!NOTE]
> Don't combine "HH" or "H" with the "t" or "tt" AM/PM designator specifier in a parsing format string. The 24-hour clock is incompatible with the AM/PM designator in parsing operations, and throws a <xref:System.FormatException>. Use "h" or "hh" instead.

The following example includes the "HH" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#10](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#10)]
[!code-vb[Formatting.DateAndTime.Custom#10](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#10)]

[Back to table](#table)

## Time zone "K" format specifier

### <a name="KSpecifier"></a> The "K" custom format specifier

The "K" custom format specifier represents the time zone information of a date and time value. When this format specifier is used with <xref:System.DateTime> values, the result string is defined by the value of the <xref:System.DateTime.Kind?displayProperty=nameWithType> property:

- For the local time zone (a <xref:System.DateTime.Kind?displayProperty=nameWithType> property value of <xref:System.DateTimeKind.Local?displayProperty=nameWithType>), this specifier produces a result string containing the local offset from Coordinated Universal Time (UTC); for example, "-07:00".

- For a UTC time (a <xref:System.DateTime.Kind?displayProperty=nameWithType> property value of <xref:System.DateTimeKind.Utc?displayProperty=nameWithType>), the result string includes a "Z" character to represent a UTC date.

- For a time from an unspecified time zone (a time whose <xref:System.DateTime.Kind?displayProperty=nameWithType> property equals <xref:System.DateTimeKind.Unspecified?displayProperty=nameWithType>), the result is equivalent to <xref:System.String.Empty?displayProperty=nameWithType>.

For <xref:System.DateTimeOffset> values, the "K" format specifier is equivalent to the "zzz" format specifier, and produces a result string containing the <xref:System.DateTimeOffset> value's offset from UTC.

If the "K" format specifier is used without other custom format specifiers, it's interpreted as a standard date and time format specifier and throws a <xref:System.FormatException>. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example displays the string that results from using the "K" custom format specifier with various <xref:System.DateTime> and <xref:System.DateTimeOffset> values on a system in the U.S. Pacific Time zone.

[!code-csharp[Formatting.DateAndTime.Custom#12](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#12)]
[!code-vb[Formatting.DateAndTime.Custom#12](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#12)]

[Back to table](#table)

## Minute "m" format specifier

### <a name="mSpecifier"></a> The "m" custom format specifier

The "m" custom format specifier represents the minute as a number from 0 to 59. The minute represents whole minutes that have passed since the last hour. A single-digit minute is formatted without a leading zero.

If the "m" format specifier is used without other custom format specifiers, it's interpreted as the "m" standard date and time format specifier. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "m" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#7](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#7)]
[!code-vb[Formatting.DateAndTime.Custom#7](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#7)]

[Back to table](#table)

### <a name="mmSpecifier"></a> The "mm" custom format specifier

The "mm" custom format specifier (plus any number of additional "m" specifiers) represents the minute as a number from 00 to 59. The minute represents whole minutes that have passed since the last hour. A single-digit minute is formatted with a leading zero.

The following example includes the "mm" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#8](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#8)]
[!code-vb[Formatting.DateAndTime.Custom#8](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#8)]

[Back to table](#table)

## Month "M" format specifier

### <a name="M_Specifier"></a> The "M" custom format specifier

The "M" custom format specifier represents the month as a number from 1 to 12 (or from 1 to 13 for calendars that have 13 months). A single-digit month is formatted without a leading zero.

If the "M" format specifier is used without other custom format specifiers, it's interpreted as the "M" standard date and time format specifier. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "M" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#11](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#11)]
[!code-vb[Formatting.DateAndTime.Custom#11](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#11)]

[Back to table](#table)

### <a name="MM_Specifier"></a> The "MM" custom format specifier

The "MM" custom format specifier represents the month as a number from 01 to 12 (or from 1 to 13 for calendars that have 13 months). A single-digit month is formatted with a leading zero.

The following example includes the "MM" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#2](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#2)]
[!code-vb[Formatting.DateAndTime.Custom#2](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#2)]

[Back to table](#table)

### <a name="MMM_Specifier"></a> The "MMM" custom format specifier

The "MMM" custom format specifier represents the abbreviated name of the month. The localized abbreviated name of the month is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.AbbreviatedMonthNames?displayProperty=nameWithType> property of the current or specified culture. If there is a "d" or "dd" custom format specifier in the custom format string, it is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.AbbreviatedMonthGenitiveNames?displayProperty=nameWithType> property instead, except in .NET Framework. (This is known issue that's fixed in .NET.)

The following example includes the "MMM" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#3](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#3)]
[!code-vb[Formatting.DateAndTime.Custom#3](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#3)]

[Back to table](#table)

### <a name="MMMM_Specifier"></a> The "MMMM" custom format specifier

The "MMMM" custom format specifier represents the full name of the month. The localized name of the month is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.MonthNames?displayProperty=nameWithType> property of the current or specified culture. If there is a "d" or "dd" custom format specifier in the custom format string, it is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.MonthGenitiveNames?displayProperty=nameWithType> property instead.

The following example includes the "MMMM" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#4](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#4)]
[!code-vb[Formatting.DateAndTime.Custom#4](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#4)]

[Back to table](#table)

## Seconds "s" format specifier

### <a name="sSpecifier"></a> The "s" custom format specifier

The "s" custom format specifier represents the seconds as a number from 0 to 59. The result represents whole seconds that have passed since the last minute. A single-digit second is formatted without a leading zero.

If the "s" format specifier is used without other custom format specifiers, it's interpreted as the "s" standard date and time format specifier. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "s" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#7](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#7)]
[!code-vb[Formatting.DateAndTime.Custom#7](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#7)]

[Back to table](#table)

### <a name="ssSpecifier"></a> The "ss" custom format specifier

The "ss" custom format specifier (plus any number of additional "s" specifiers) represents the seconds as a number from 00 to 59. The result represents whole seconds that have passed since the last minute. A single-digit second is formatted with a leading zero.

The following example includes the "ss" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#8](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#8)]
[!code-vb[Formatting.DateAndTime.Custom#8](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#8)]

[Back to table](#table)

## Meridiem "t" format specifier

### <a name="tSpecifier"></a> The "t" custom format specifier

The "t" custom format specifier represents the first character of the AM/PM designator. The appropriate localized designator is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.AMDesignator*?displayProperty=nameWithType> or <xref:System.Globalization.DateTimeFormatInfo.PMDesignator?displayProperty=nameWithType> property of the current or specific culture. The AM designator is used for all times from 0:00:00 (midnight) to 11:59:59.999. The PM designator is used for all times from 12:00:00 (noon) to 23:59:59.999.

If the "t" format specifier is used without other custom format specifiers, it's interpreted as the "t" standard date and time format specifier. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "t" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#7](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#7)]
[!code-vb[Formatting.DateAndTime.Custom#7](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#7)]

[Back to table](#table)

### <a name="ttSpecifier"></a> The "tt" custom format specifier

The "tt" custom format specifier (plus any number of additional "t" specifiers) represents the entire AM/PM designator. The appropriate localized designator is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.AMDesignator*?displayProperty=nameWithType> or <xref:System.Globalization.DateTimeFormatInfo.PMDesignator?displayProperty=nameWithType> property of the current or specific culture. The AM designator is used for all times from 0:00:00 (midnight) to 11:59:59.999. The PM designator is used for all times from 12:00:00 (noon) to 23:59:59.999.

Make sure to use the "tt" specifier for languages for which it's necessary to maintain the distinction between AM and PM. An example is Japanese, for which the AM and PM designators differ in the second character instead of the first character.

> [!NOTE]
> When you parse a string that includes an AM/PM designator, use the "h" or "hh" hour specifier (12-hour clock) instead of "H" or "HH" (24-hour clock). The 24-hour clock specifier is incompatible with the AM/PM designator in a parsing operation, and throws a <xref:System.FormatException>.

The following example includes the "tt" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#8](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#8)]
[!code-vb[Formatting.DateAndTime.Custom#8](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#8)]

[Back to table](#table)

## Year "y" format specifier

### <a name="ySpecifier"></a> The "y" custom format specifier

The "y" custom format specifier represents the year as a one-digit or two-digit number. If the year has more than two digits, only the two low-order digits appear in the result. If the first digit of a two-digit year begins with a zero (for example, 2008), the number is formatted without a leading zero.

If the "y" format specifier is used without other custom format specifiers, it's interpreted as the "y" standard date and time format specifier. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "y" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#13](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#13)]
[!code-vb[Formatting.DateAndTime.Custom#13](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#13)]

[Back to table](#table)

### <a name="yySpecifier"></a> The "yy" custom format specifier

The "yy" custom format specifier represents the year as a two-digit number. If the year has more than two digits, only the two low-order digits appear in the result. If the two-digit year has fewer than two significant digits, the number is padded with leading zeros to produce two digits.

In a parsing operation, a two-digit year that is parsed using the "yy" custom format specifier is interpreted based on the <xref:System.Globalization.Calendar.TwoDigitYearMax?displayProperty=nameWithType> property of the format provider's current calendar. The following example parses the string representation of a date that has a two-digit year by using the default Gregorian calendar of the en-US culture, which, in this case, is the current culture. It then changes the current culture's <xref:System.Globalization.CultureInfo> object to use a <xref:System.Globalization.GregorianCalendar> object whose <xref:System.Globalization.GregorianCalendar.TwoDigitYearMax> property has been modified.

[!code-csharp[Formatting.DateAndTime.Custom#19](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/parseexact2digityear1.cs#19)]
[!code-vb[Formatting.DateAndTime.Custom#19](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/parseexact2digityear1.vb#19)]

The following example includes the "yy" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#13](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#13)]
[!code-vb[Formatting.DateAndTime.Custom#13](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#13)]

[Back to table](#table)

### <a name="yyySpecifier"></a> The "yyy" custom format specifier

The "yyy" custom format specifier represents the year with a minimum of three digits. If the year has more than three significant digits, they are included in the result string. If the year has fewer than three digits, the number is padded with leading zeros to produce three digits.

> [!NOTE]
> For the Thai Buddhist calendar, which can have five-digit years, this format specifier displays all significant digits.

The following example includes the "yyy" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#13](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#13)]
[!code-vb[Formatting.DateAndTime.Custom#13](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#13)]

[Back to table](#table)

### <a name="yyyySpecifier"></a> The "yyyy" custom format specifier

The "yyyy" custom format specifier represents the year with a minimum of four digits. If the year has more than four significant digits, they are included in the result string. If the year has fewer than four digits, the number is padded with leading zeros to produce four digits.

> [!NOTE]
> For the Thai Buddhist calendar, which can have five-digit years, this format specifier displays a minimum of four digits.

The following example includes the "yyyy" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#13](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#13)]
[!code-vb[Formatting.DateAndTime.Custom#13](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#13)]

[Back to table](#table)

### <a name="yyyyySpecifier"></a> The "yyyyy" custom format specifier

The "yyyyy" custom format specifier (plus any number of additional "y" specifiers) represents the year with a minimum of five digits. If the year has more than five significant digits, they are included in the result string. If the year has fewer than five digits, the number is padded with leading zeros to produce five digits.

If there are additional "y" specifiers, the number is padded with as many leading zeros as necessary to produce the number of "y" specifiers.

The following example includes the "yyyyy" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#13](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#13)]
[!code-vb[Formatting.DateAndTime.Custom#13](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#13)]

[Back to table](#table)

## Offset "z" format specifier

### <a name="zSpecifier"></a> The "z" custom format specifier

With <xref:System.DateTime> values, the "z" custom format specifier represents the signed offset of the specified time zone from Coordinated Universal Time (UTC), measured in hours. The offset is always displayed with a leading sign. A plus sign (+) indicates hours ahead of UTC, and a minus sign (-) indicates hours behind UTC. A single-digit offset is formatted *without* a leading zero.

The following table shows how the offset value changes depending on <xref:System.DateTimeKind>.

| <xref:System.DateTimeKind> value | Offset value |
| - | - |
| <xref:System.DateTimeKind.Local> | The signed offset of the local operating system's time zone from UTC. |
| <xref:System.DateTimeKind.Unspecified> | The signed offset of the local operating system's time zone from UTC. |
| <xref:System.DateTimeKind.Utc> | `+0` on .NET Core and .NET 5+. <br/><br/> On .NET Framework, the signed offset of the local operating system's time zone from UTC. |

With <xref:System.DateTimeOffset> values, this format specifier represents the <xref:System.DateTimeOffset> value's offset from UTC in hours.

If the "z" format specifier is used without other custom format specifiers, it's interpreted as a standard date and time format specifier and throws a <xref:System.FormatException>. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

The following example includes the "z" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#14](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#14)]
[!code-vb[Formatting.DateAndTime.Custom#14](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#14)]

[Back to table](#table)

### <a name="zzSpecifier"></a> The "zz" custom format specifier

With <xref:System.DateTime> values, the "zz" custom format specifier represents the signed offset of the specified time zone from UTC, measured in hours. The offset is always displayed with a leading sign. A plus sign (+) indicates hours ahead of UTC, and a minus sign (-) indicates hours behind UTC. A single-digit offset is formatted *with* a leading zero.

The following table shows how the offset value changes depending on <xref:System.DateTimeKind>.

| <xref:System.DateTimeKind> value | Offset value |
| - | - |
| <xref:System.DateTimeKind.Local> | The signed offset of the local operating system's time zone from UTC. |
| <xref:System.DateTimeKind.Unspecified> | The signed offset of the local operating system's time zone from UTC. |
| <xref:System.DateTimeKind.Utc> | `+00` on .NET Core and .NET 5+. <br/><br/> On .NET Framework, the signed offset of the local operating system's time zone from UTC. |

With <xref:System.DateTimeOffset> values, this format specifier represents the <xref:System.DateTimeOffset> value's offset from UTC in hours.

The following example includes the "zz" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#14](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#14)]
[!code-vb[Formatting.DateAndTime.Custom#14](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#14)]

[Back to table](#table)

### <a name="zzzSpecifier"></a> The "zzz" custom format specifier

With <xref:System.DateTime> values, the "zzz" custom format specifier represents the signed offset of the specified time zone from UTC, measured in hours and minutes. The offset is always displayed with a leading sign. A plus sign (+) indicates hours ahead of UTC, and a minus sign (-) indicates hours behind UTC. A single-digit offset is formatted with a leading zero.

The following table shows how the offset value changes depending on <xref:System.DateTimeKind>.

| <xref:System.DateTimeKind> value | Offset value |
| - | - |
| <xref:System.DateTimeKind.Local> | The signed offset of the local operating system's time zone from UTC. |
| <xref:System.DateTimeKind.Unspecified> | The signed offset of the local operating system's time zone from UTC. |
| <xref:System.DateTimeKind.Utc> | `+00:00` on .NET Core and .NET 5+. <br/><br/> On .NET Framework, the signed offset of the local operating system's time zone from UTC. |

With <xref:System.DateTimeOffset> values, this format specifier represents the <xref:System.DateTimeOffset> value's offset from UTC in hours and minutes.

The following example includes the "zzz" custom format specifier in a custom format string.

[!code-csharp[Formatting.DateAndTime.Custom#14](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/Custom1.cs#14)]
[!code-vb[Formatting.DateAndTime.Custom#14](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/Custom1.vb#14)]

[Back to table](#table)

## Date and time separator specifiers

### <a name="timeSeparator"></a> The ":" custom format specifier

The ":" custom format specifier represents the time separator, which is used to differentiate hours, minutes, and seconds. The appropriate localized time separator is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.TimeSeparator?displayProperty=nameWithType> property of the current or specified culture.

> [!NOTE]
> To change the time separator for a particular date and time string, specify the separator character within a literal string delimiter. For example, the custom format string `hh'_'dd'_'ss` produces a result string in which "\_" (an underscore) is always used as the time separator. To change the time separator for all dates for a culture, either change the value of the <xref:System.Globalization.DateTimeFormatInfo.TimeSeparator?displayProperty=nameWithType> property of the current culture, or instantiate a <xref:System.Globalization.DateTimeFormatInfo> object, assign the character to its <xref:System.Globalization.DateTimeFormatInfo.TimeSeparator> property, and call an overload of the formatting method that includes an <xref:System.IFormatProvider> parameter.

If the ":" format specifier is used without other custom format specifiers, it's interpreted as a standard date and time format specifier and throws a <xref:System.FormatException>. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

[Back to table](#table)

### <a name="dateSeparator"></a> The "/" custom format specifier

The "/" custom format specifier represents the date separator, which is used to differentiate years, months, and days. The appropriate localized date separator is retrieved from the <xref:System.Globalization.DateTimeFormatInfo.DateSeparator?displayProperty=nameWithType> property of the current or specified culture.

> [!NOTE]
> To change the date separator for a particular date and time string, specify the separator character within a literal string delimiter. For example, the custom format string `mm'/'dd'/'yyyy` produces a result string in which "/" is always used as the date separator. To change the date separator for all dates for a culture, either change the value of the <xref:System.Globalization.DateTimeFormatInfo.DateSeparator?displayProperty=nameWithType> property of the current culture, or instantiate a <xref:System.Globalization.DateTimeFormatInfo> object, assign the character to its <xref:System.Globalization.DateTimeFormatInfo.DateSeparator> property, and call an overload of the formatting method that includes an <xref:System.IFormatProvider> parameter.

If the "/" format specifier is used without other custom format specifiers, it's interpreted as a standard date and time format specifier and throws a <xref:System.FormatException>. For more information about using a single format specifier, see [Using Single Custom Format Specifiers](#UsingSingleSpecifiers) later in this article.

[Back to table](#table)

## <a name="Literals"></a> Character literals

The following characters in a custom date and time format string are reserved and are always interpreted as formatting characters or, in the case of `"`, `'`, `/`, and `\`, as special characters.

- `F`
- `H`
- `K`
- `M`
- `d`
- `f`
- `g`
- `h`
- `m`
- `s`
- `t`
- `y`
- `z`
- `%`
- `:`
- `/`
- `"`
- `'`
- `\`

All other characters are always interpreted as character literals and, in a formatting operation, are included in the result string unchanged.  In a parsing operation, they must match the characters in the input string exactly; the comparison is case-sensitive.

The following example includes the literal characters "PST" (for Pacific Standard Time) and "PDT" (for Pacific Daylight Time) to represent the local time zone in a format string. Note that the string is included in the result string, and that a string that includes the local time zone string also parses successfully.

[!code-csharp[Formatting.DateAndTime.Custom#20](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/LiteralsEx1.cs#20)]
[!code-vb[Formatting.DateAndTime.Custom#20](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/LiteralsEx1.vb#20)]

There are two ways to indicate that characters are to be interpreted as literal characters and not as reserve characters, so that they can be included in a result string or successfully parsed in an input string:

- By escaping each reserved character. For more information, see [Using the Escape Character](#escape).

The following example includes the literal characters "pst" (for Pacific Standard time) to represent the local time zone in a format string. Because both "s" and "t" are custom format strings, both characters must be escaped to be interpreted as character literals.

[!code-csharp[Formatting.DateAndTime.Custom#21](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/LiteralsEx2.cs#21)]
[!code-vb[Formatting.DateAndTime.Custom#21](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/LiteralsEx2.vb#21)]

- By enclosing the entire literal string in quotation marks or apostrophes. The following example is like the previous one, except that "pst" is enclosed in quotation marks to indicate that the entire delimited string should be interpreted as character literals.

[!code-csharp[Formatting.DateAndTime.Custom#22](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/LiteralsEx3.cs#22)]
[!code-vb[Formatting.DateAndTime.Custom#22](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/LiteralsEx3.vb#22)]

## Notes

### <a name="UsingSingleSpecifiers"></a> Using single custom format specifiers

A custom date and time format string consists of two or more characters. Date and time formatting methods interpret any single-character string as a standard date and time format string. If they don't recognize the character as a valid format specifier, they throw a <xref:System.FormatException>. For example, a format string that consists only of the specifier "h" is interpreted as a standard date and time format string. However, in this particular case, an exception is thrown because there is no "h" standard date and time format specifier.

To use any of the custom date and time format specifiers as the only specifier in a format string (that is, to use the "d", "f", "F", "g", "h", "H", "K", "m", "M", "s", "t", "y", "z", ":", or "/" custom format specifier by itself), include a space before the specifier, or include a percent ("%") format specifier before the single custom date and time specifier.

For example, "`%h"` is interpreted as a custom date and time format string that displays the hour represented by the current date and time value. You can also use the " h" format string, although this includes a space in the result string along with the hour. The following example illustrates these format strings.

[!code-csharp[Formatting.DateAndTime.Custom#16](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/literal1.cs#16)]
[!code-vb[Formatting.DateAndTime.Custom#16](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/literal1.vb#16)]

#### <a name="escape"></a> Using the Escape character

The "d", "f", "F", "g", "h", "H", "K", "m", "M", "s", "t", "y", "z", ":", or "/" characters in a format string are interpreted as custom format specifiers rather than as literal characters. To prevent a character from being interpreted as a format specifier, you can precede it with a backslash (\\), which is the escape character. The escape character signifies that the following character is a character literal that should be included in the result string unchanged.

To include a backslash in a result string, you must escape it with another backslash (`\\`).

> [!NOTE]
> Some compilers, such as the C++ and C# compilers, may also interpret a single backslash character as an escape character. To ensure that a string is interpreted correctly when formatting, you can use the verbatim string literal character (the @ character) before the string in C#, or add another backslash character before each backslash in C# and C++. The following C# example illustrates both approaches.

The following example uses the escape character to prevent the formatting operation from interpreting the "h" and "m" characters as format specifiers.

[!code-csharp[Formatting.DateAndTime.Custom#15](~/samples/snippets/csharp/VS_Snippets_CLR/Formatting.DateAndTime.Custom/cs/escape1.cs#15)]
[!code-vb[Formatting.DateAndTime.Custom#15](~/samples/snippets/visualbasic/VS_Snippets_CLR/Formatting.DateAndTime.Custom/vb/escape1.vb#15)]

### Control Panel settings

The **Regional and Language Options** settings in Control Panel influence the result string produced by a formatting operation that includes many of the custom date and time format specifiers. These settings are used to initialize the <xref:System.Globalization.DateTimeFormatInfo> object associated with the current culture, which provides values used to govern formatting. Computers that use different settings generate different result strings.

In addition, if you use the <xref:System.Globalization.CultureInfo.%23ctor%28System.String%29> constructor to instantiate a new <xref:System.Globalization.CultureInfo> object that represents the same culture as the current system culture, any customizations established by the **Regional and Language Options** item in Control Panel will be applied to the new <xref:System.Globalization.CultureInfo> object. You can use the <xref:System.Globalization.CultureInfo.%23ctor%28System.String%2CSystem.Boolean%29> constructor to create a <xref:System.Globalization.CultureInfo> object that doesn't reflect a system's customizations.

### DateTimeFormatInfo properties

Formatting is influenced by properties of the current <xref:System.Globalization.DateTimeFormatInfo> object, which is provided implicitly by the current culture or explicitly by the <xref:System.IFormatProvider> parameter of the method that invokes formatting. For the <xref:System.IFormatProvider> parameter, you should specify a <xref:System.Globalization.CultureInfo> object, which represents a culture, or a <xref:System.Globalization.DateTimeFormatInfo> object.

The result string produced by many of the custom date and time format specifiers also depends on properties of the current <xref:System.Globalization.DateTimeFormatInfo> object. Your application can change the result produced by some custom date and time format specifiers by changing the corresponding <xref:System.Globalization.DateTimeFormatInfo> property. For example, the "ddd" format specifier adds an abbreviated weekday name found in the <xref:System.Globalization.DateTimeFormatInfo.AbbreviatedDayNames*> string array to the result string. Similarly, the "MMMM" format specifier adds a full month name found in the <xref:System.Globalization.DateTimeFormatInfo.MonthNames*> string array to the result string.

## See also

- <xref:System.DateTime?displayProperty=nameWithType>
- <xref:System.IFormatProvider?displayProperty=nameWithType>
- [Formatting types](formatting-types.md)
- [Standard Date and Time format strings](standard-date-and-time-format-strings.md)
- [Sample: .NET Core WinForms formatting utility (C#)](/samples/dotnet/samples/windowsforms-formatting-utility-cs)
- [Sample: .NET Core WinForms formatting utility (Visual Basic)](/samples/dotnet/samples/windowsforms-formatting-utility-vb)
