// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Numerics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

/// <summary>
/// Framework format descriptions follow the linked Microsoft Learn format tables.
/// Examples prefer known values and otherwise use fixed framework samples, without executing project code.
/// </summary>
internal readonly record struct CSharpFormatStringInfo(
    string TypeName, string Description, string Example, string DocumentationUrl,
    CSharpFormatStringValue SourceValue = default, CSharpFormatStringContext Context = default, FormatStringOutputKind OutputKind = FormatStringOutputKind.Example)
{
    private const string Docs = "https://learn.microsoft.com/en-us/dotnet/standard/base-types/";
    public bool IsPreview => OutputKind == FormatStringOutputKind.Preview && SourceValue.IsKnown;
    private static readonly IFormattable s_guid = new Guid("12345678-1234-5678-9abc-def012345678");
    private static readonly IFormattable s_date = new DateTime(2026, 7, 4, 13, 45, 6, DateTimeKind.Utc).AddTicks(1234567);
    private static readonly IFormattable s_dateOffset = new DateTimeOffset(DateTime.SpecifyKind((DateTime)s_date, DateTimeKind.Unspecified), TimeSpan.FromHours(2));
    private static readonly IFormattable s_duration = new TimeSpan(2, 13, 45, 6).Add(TimeSpan.FromTicks(1234567));

    public static bool TryCreate(ITypeSymbol type, Compilation compilation, string format, out CSharpFormatStringInfo info, Optional<object?> constantValue = default, CultureInfo? culture = null)
        => TryCreate(type, compilation, format, out info, CSharpFormatStringValue.Create(constantValue, type),
            new CSharpFormatStringContext(culture, culture is null ? FormatStringProviderKind.Absent : FormatStringProviderKind.ExplicitCulture));

    internal static bool TryCreate(ITypeSymbol type, Compilation compilation, string format, out CSharpFormatStringInfo info,
        CSharpFormatStringValue sourceValue, CSharpFormatStringContext context)
    {
        info = default;
        if (format.Length > CSharpFormatStringValueEvaluator.StringLimit)
            return false;
        if (type.TypeKind == TypeKind.Enum)
            return TryCreateEnum((INamedTypeSymbol)type, compilation, format, sourceValue, context, out info);
        var constantValue = context.HasUnknownFormatter ? default : sourceValue.Value;
        if (type is not INamedTypeSymbol)
            return false;

        var name = type.MetadataName;
        var ns = type.ContainingNamespace;
        var isSystem = ns.Name == "System" && ns.ContainingNamespace.IsGlobalNamespace;
        var isNumerics = ns.Name == "Numerics" && ns.ContainingNamespace.Name == "System" && ns.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;
        var coreAssembly = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
        if (!((isSystem || isNumerics) && SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, coreAssembly)) &&
            !(isNumerics && name is "BigInteger" or "Complex" && type.ContainingAssembly?.Name is "System.Runtime.Numerics" or "System.Numerics"))
        {
            return false;
        }

        string? description;
        string url;
        IFormattable? sample;
        var previewOmitted = false;
        if (name == "Guid")
        {
            description = CSharpFormatStringDocumentation.Get("guid", format.Length == 0 ? "D" : format.ToUpperInvariant());
            sample = s_guid;
            url = "https://learn.microsoft.com/en-us/dotnet/api/system.guid.tostring#system-guid-tostring(system-string)";
        }
        else if (name is "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly")
        {
            if (format.Length <= 1)
            {
                description = GetStandardDateDescription(format.Length == 0 ? name == "DateOnly" ? 'd' : name == "TimeOnly" ? 't' : 'G' : format[0]);
                url = Docs + "standard-date-and-time-format-strings";
            }
            else
            {
                description = DescribeCustom(format, date: true, duration: false);
                url = Docs + "custom-date-and-time-format-strings";
            }

            sample = name == "DateTimeOffset" ? s_dateOffset : s_date;
            if (name is "DateOnly" or "TimeOnly")
            {
                var modernSample = name == "DateOnly" ? ModernSamples.DateOnly : ModernSamples.TimeOnly;
                if (modernSample is null)
                    return false;
                sample = modernSample;
            }
        }
        else if (name == "TimeSpan")
        {
            sample = s_duration;
            description = format.Length <= 1
                ? CSharpFormatStringDocumentation.Get("duration-standard", format is "" or "t" or "T" ? "c" : format)
                : DescribeCustom(format, date: false, duration: true);
            url = Docs + (format.Length <= 1 ? "standard-timespan-format-strings" : "custom-timespan-format-strings");
        }
        else
        {
            var integral = true;
            sample = type.SpecialType switch
            {
                SpecialType.System_Byte => (byte)123,
                SpecialType.System_SByte => (sbyte)123,
                SpecialType.System_Int16 => (short)12345,
                SpecialType.System_UInt16 => (ushort)12345,
                SpecialType.System_Int32 => 12345,
                SpecialType.System_UInt32 => 12345U,
                SpecialType.System_Int64 => 12345L,
                SpecialType.System_UInt64 => 12345UL,
                SpecialType.System_IntPtr => 12345L,
                SpecialType.System_UIntPtr => 12345UL,
                _ => null,
            };
            if (sample is null)
            {
                integral = name is "Int128" or "UInt128" or "BigInteger";
                sample = name switch
                {
                    "Single" => 1234.5f,
                    "Double" => 1234.5,
                    "Decimal" => 1234.5m,
                    "BigInteger" => new BigInteger(12345),
                    "Complex" => new Complex(1234.5, 6.25),
                    _ => null,
                };
                sample ??= name switch
                {
                    "Half" => ModernSamples.Half,
                    "Int128" => ModernSamples.Int128,
                    "UInt128" => ModernSamples.UInt128,
                    _ => null,
                };
            }

            if (sample is null)
                return false;

            if (IsStandardNumeric(format, out var specifier, out var precision))
            {
                var normalizedSpecifier = char.ToUpperInvariant(specifier);
                var floatingHex = normalizedSpecifier == 'X' && name is "Single" or "Double" or "Half" or "Complex";
                if (!integral && (normalizedSpecifier is 'B' or 'D' ||
                    normalizedSpecifier == 'X' && (!floatingHex || coreAssembly.Identity.Version.Major < 11 || !FloatingHex.IsSupported)))
                {
                    return false;
                }
                description = CSharpFormatStringDocumentation.Get(floatingHex ? "numeric-hex-float" : "numeric-standard", normalizedSpecifier.ToString());
                if (description is null)
                    return false;
                if (normalizedSpecifier == 'B')
                {
                    description += "\n\n" + CSharpFormatStringDocumentation.Get("numeric-support", "B");
                    if (coreAssembly.Identity.Version.Major < 8)
                        constantValue = default;
                }
                if (precision.Length > 0)
                {
                    if (!int.TryParse(precision, NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count > 999999999)
                        return false;
                    if (!floatingHex)
                    {
                        description += "\n\n" + (normalizedSpecifier == 'G' && count == 0
                            ? CSharpFormatStringDocumentation.Get("numeric-special", "G0")
                            : precision + " — " + CSharpFormatStringDocumentation.Get("numeric-precision", normalizedSpecifier.ToString()));
                    }
                    // .NET permits precisions up to 999,999,999. A hover must not allocate that output.
                    previewOmitted = count > 100;
                }
                url = floatingHex
                    ? "https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#floating-point-hex-formatting-and-parsing"
                    : Docs + "standard-numeric-format-strings";
            }
            else
            {
                description = DescribeCustom(format, date: false, duration: false);
                url = Docs + "custom-numeric-format-strings";
            }
        }

        if (description is null)
            return false;

        // Native integer constants do not establish the target process's pointer width.
        if (type.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr)
            constantValue = default;

        // These DateTime formats need a local time zone that the source does not specify.
        if (constantValue.Value is DateTime { Kind: DateTimeKind.Unspecified } && UsesLocalTimeZone(format))
            constantValue = default;

        // Inputs and precision are bounded before formatting. An invalid actual value must not become a sample Preview.
        string example;
        try
        {
            example = previewOmitted || constantValue is { HasValue: true, Value: null }
                ? "" : (constantValue.Value as IFormattable ?? sample).ToString(format, context.EffectiveCulture);
            if (example.Length > CSharpFormatStringValueEvaluator.StringLimit)
            {
                example = "";
                previewOmitted = true;
            }
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            // A valid date can be outside the selected culture's calendar range.
            example = "";
            previewOmitted = true;
        }
        if (previewOmitted)
            description += "\n\n" + CSharpFeaturesResources.Format_example_omitted;
        info = new(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), description, example, url,
            sourceValue, context, previewOmitted ? FormatStringOutputKind.Omitted :
                constantValue.HasValue ? FormatStringOutputKind.Preview : FormatStringOutputKind.Example);
        return true;
    }

    private static bool UsesLocalTimeZone(string format)
    {
        if (format.Length <= 1)
            return format == "U";
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '\\')
            {
                i++;
            }
            else if (c is '\'' or '"')
            {
                while (++i < format.Length && format[i] != c)
                {
                    if (format[i] == '\\')
                        i++;
                }
            }
            else if (c == 'z')
            {
                return true;
            }
        }
        return false;
    }

    private static class FloatingHex
    {
        public static readonly bool IsSupported = CanFormat();

        private static bool CanFormat()
        {
            try
            {
                _ = 1.0.ToString("X", CultureInfo.InvariantCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    // The netstandard build can run on either .NET Framework or modern .NET. Resolve only these
    // fixed framework samples, once, so their formatting is never approximated with another type.
    private static class ModernSamples
    {
        public static readonly IFormattable? DateOnly = Parse("System.DateOnly", "2026-07-04");
        public static readonly IFormattable? TimeOnly = Parse("System.TimeOnly", "13:45:06.1234567");
        public static readonly IFormattable? Half = Parse("System.Half", "1234.5");
        public static readonly IFormattable? Int128 = Parse("System.Int128", "12345");
        public static readonly IFormattable? UInt128 = Parse("System.UInt128", "12345");

        private static IFormattable? Parse(string name, string value)
        {
            var type = typeof(object).Assembly.GetType(name);
            var parse = type?.GetMethod("Parse", [typeof(string), typeof(IFormatProvider)]);
            return (IFormattable?)parse?.Invoke(null, [value, CultureInfo.InvariantCulture]);
        }
    }

    private static bool IsStandardNumeric(string format, out char specifier, out string precision)
    {
        specifier = format.Length == 0 ? 'G' : format[0];
        precision = "";
        if (format.Length == 0)
            return true;
        if (specifier is not (>= 'A' and <= 'Z') and not (>= 'a' and <= 'z'))
            return false;
        for (var i = 1; i < format.Length; i++)
        {
            if (format[i] is < '0' or > '9')
                return false;
        }
        precision = format.Length > 1 ? format.Substring(1) : "";
        return true;
    }

    private static bool TryCreateEnum(INamedTypeSymbol type, Compilation compilation, string format,
        CSharpFormatStringValue sourceValue, CSharpFormatStringContext context, out CSharpFormatStringInfo info)
    {
        info = default;
        var constantValue = context.HasUnknownFormatter ? default : sourceValue.Value;
        var specifier = format.Length == 0 ? "G" : format.ToUpperInvariant();
        var description = CSharpFormatStringDocumentation.Get("enum", specifier);
        if (description is null)
            return false;
        var hexWidth = type.EnumUnderlyingType!.SpecialType switch
        {
            SpecialType.System_Byte or SpecialType.System_SByte => 2,
            SpecialType.System_Int16 or SpecialType.System_UInt16 => 4,
            SpecialType.System_Int64 or SpecialType.System_UInt64 => 16,
            _ => 8,
        };
        if (constantValue.HasValue)
        {
            var isPreview = true;
            var omitted = false;
            var example = constantValue.Value is not IFormattable value ? "" : specifier switch
            {
                "D" => value.ToString(null, CultureInfo.InvariantCulture),
                "X" => value.ToString("X" + hexWidth, CultureInfo.InvariantCulture),
                _ => FormatEnumConstant(type, compilation, value, specifier, out isPreview, out omitted),
            };
            omitted |= example.Length > CSharpFormatStringValueEvaluator.StringLimit;
            info = new(type.Name, description, omitted ? "" : example, Docs + "enumeration-format-strings", sourceValue, context,
                omitted ? FormatStringOutputKind.Omitted : isPreview ? FormatStringOutputKind.Preview : FormatStringOutputKind.Example);
            return true;
        }
        foreach (var member in type.GetMembers())
        {
            if (member is not IFieldSymbol { HasConstantValue: true, ConstantValue: IFormattable value } field)
                continue;
            var example = specifier switch
            {
                "D" => value.ToString(null, CultureInfo.InvariantCulture),
                "X" => value.ToString("X" + hexWidth, CultureInfo.InvariantCulture),
                _ => field.Name,
            };
            var omitted = example.Length > CSharpFormatStringValueEvaluator.StringLimit;
            info = new(type.Name, description, omitted ? "" : example, Docs + "enumeration-format-strings", sourceValue, context,
                omitted ? FormatStringOutputKind.Omitted : FormatStringOutputKind.Example);
            return true;
        }
        var zero = specifier == "X" ? new string('0', hexWidth) : "0";
        info = new(type.Name, description, zero, Docs + "enumeration-format-strings", sourceValue, context);
        return true;
    }

    private static string FormatEnumConstant(INamedTypeSymbol type, Compilation compilation, IFormattable value, string specifier, out bool isPreview, out bool omitted)
    {
        isPreview = true;
        omitted = false;
        string? exactName = null;
        using var _2 = PooledHashSet<ulong>.GetInstance(out var values);
        var underlyingType = type.EnumUnderlyingType!.SpecialType;
        var remaining = underlyingType.ConvertUnderlyingValueToUInt64(value);
        using var _ = ArrayBuilder<(ulong Value, string Name)>.GetInstance(out var fields);
        var remainingMembers = CSharpFormatStringValueEvaluator.ScopeNodeLimit;
        foreach (var member in type.GetMembers())
        {
            if (--remainingMembers < 0)
            {
                omitted = true;
                return "";
            }
            if (member is IFieldSymbol { HasConstantValue: true, ConstantValue: { } fieldValue } field)
            {
                var number = underlyingType.ConvertUnderlyingValueToUInt64(fieldValue);
                // The runtime does not guarantee which name an aliased enum value uses.
                if (!values.Add(number))
                    isPreview = false;
                if (number == remaining)
                    exactName ??= field.Name;
                if (number != 0)
                    fields.Add((number, field.Name));
            }
        }

        if (exactName is not null)
            return exactName;

        var isFlags = specifier == "F";
        if (!isFlags)
        {
            var flagsAttribute = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly.GetTypeByMetadataName("System.FlagsAttribute");
            foreach (var attribute in type.GetAttributes())
                isFlags |= SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, flagsAttribute);
        }

        if (isFlags && remaining != 0)
        {
            fields.Sort(static (x, y) => y.Value.CompareTo(x.Value));
            using var namesDisposer = ArrayBuilder<string>.GetInstance(out var names);
            var length = 0;
            foreach (var field in fields)
            {
                if ((remaining & field.Value) == field.Value)
                {
                    length += field.Name.Length + (names.Count == 0 ? 0 : 2);
                    if (length > CSharpFormatStringValueEvaluator.StringLimit)
                    {
                        omitted = true;
                        return "";
                    }
                    remaining &= ~field.Value;
                    names.Add(field.Name);
                }
            }
            if (remaining == 0)
            {
                names.ReverseContents();
                return string.Join(", ", names);
            }
        }

        return value.ToString(null, CultureInfo.InvariantCulture);
    }

    private static string? GetStandardDateDescription(char format)
        => CSharpFormatStringDocumentation.Get("date-standard", format.ToString());

    private static string DescribeCustom(string format, bool date, bool duration)
    {
        var family = date ? "date-custom" : duration ? "duration-custom" : "numeric-custom";
        using var _1 = ArrayBuilder<string>.GetInstance(out var parts);
        using var _2 = PooledHashSet<string>.GetInstance(out var seen);
        for (var i = 0; i < format.Length; i++)
        {
            var start = i;
            var c = format[i];
            string? description;
            string? token = null;
            if (c is '\'' or '"')
            {
                while (++i < format.Length && format[i] != c)
                {
                    if (format[i] == '\\' && i + 1 < format.Length)
                        i++;
                }
                if (i == format.Length)
                    break;
                description = CSharpFormatStringDocumentation.Get(family, "quoted");
            }
            else if (c == '\\' && i + 1 < format.Length)
            {
                i++;
                description = CSharpFormatStringDocumentation.Get(family, "escape");
            }
            else if ((date || duration) && c == '%' && i + 1 < format.Length)
            {
                i++;
                description = GetCustomPartDescription(family, date ? format.Substring(i, 1) : format.Substring(start, 2));
            }
            else
            {
                // Numeric exponent tokens include their optional sign and zero placeholders.
                var exponent = false;
                var exponentEnd = i + 1;
                if (!date && !duration && c is 'E' or 'e')
                {
                    if (exponentEnd < format.Length && format[exponentEnd] is '+' or '-')
                        exponentEnd++;
                    var zeros = exponentEnd;
                    while (exponentEnd < format.Length && format[exponentEnd] == '0')
                        exponentEnd++;
                    if (exponentEnd > zeros)
                    {
                        i = exponentEnd - 1;
                        exponent = true;
                    }
                }
                while (!exponent && i + 1 < format.Length && format[i + 1] == c)
                    i++;
                token = format.Substring(start, i - start + 1);
                description = exponent ? CSharpFormatStringDocumentation.Get(family, "E0") : GetCustomPartDescription(family, token);
            }

            token ??= format.Substring(start, i - start + 1);
            if (description is not null && seen.Add(token))
            {
                if (parts.Count > 0)
                    parts.Add("\n\n");
                parts.Add(token);
                parts.Add(" — ");
                parts.Add(description);
            }
        }
        return string.Concat(parts);
    }

    private static string? GetCustomPartDescription(string family, string part)
    {
        if (family == "numeric-custom")
        {
            return CSharpFormatStringDocumentation.Get(family, part[0].ToString()) ?? CSharpFormatStringDocumentation.Get(family, "literal");
        }
        var description = CSharpFormatStringDocumentation.Get(family, part);
        if (description is not null)
            return description;
        if (family == "date-custom")
        {
            var canonical = part[0] switch
            {
                'd' => "dddd",
                'M' => "MMMM",
                'h' => "hh",
                'H' => "HH",
                'm' => "mm",
                's' => "ss",
                't' => "tt",
                'z' => "zzz",
                'g' => "gg",
                ':' => ":",
                '/' => "/",
                'y' => "extended-year",
                _ => "literal",
            };
            return CSharpFormatStringDocumentation.Get(family, canonical);
        }
        return CSharpFormatStringDocumentation.Get(family, "literal");
    }
}
