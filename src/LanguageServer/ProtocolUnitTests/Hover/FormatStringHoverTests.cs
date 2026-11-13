// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Hover;

public sealed class FormatStringHoverTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory]
    [InlineData("N", "12345678123456789abcdef012345678")]
    [InlineData("D", "12345678-1234-5678-9abc-def012345678")]
    [InlineData("B", "{12345678-1234-5678-9abc-def012345678}")]
    [InlineData("P", "(12345678-1234-5678-9abc-def012345678)")]
    [InlineData("X", "{0x12345678,0x1234,0x5678,{0x9a,0xbc,0xde,0xf0,0x12,0x34,0x56,0x78}}")]
    [InlineData("d", "12345678-1234-5678-9abc-def012345678")]
    public async Task GuidFormatIncludesGeneratedExample(string format, string expectedExample)
    {
        var markup = """
            class C
            {
                string M(System.Guid receiptId) => $"receipts/{receiptId:{|caret:FORMAT|}}/ack";
            }
            """.Replace("FORMAT", format);
        var hover = await GetHoverAsync(markup);
        Assert.NotNull(hover);
        Assert.Contains($"Guid format \"{format}\"", hover.Contents.Fourth.Value);
        Assert.Contains(expectedExample, hover.Contents.Fourth.Value);
        var markdown = hover.Contents.Fourth.Value.ReplaceLineEndings("\n");
        Assert.StartsWith("## Guid format", markdown);
        Assert.Contains("\n## Example", markdown);
        Assert.Contains($"```text\n{expectedExample}\n```", markdown);
        Assert.Contains("[Microsoft Learn: format strings](https://learn.microsoft.com/", markdown);
        Assert.DoesNotContain("Example output for a sample Guid:", markdown);
    }

    [Fact]
    public async Task ColonAndSpecifierShareHoverRange()
    {
        var markup = """
            class C { string M(System.Guid id) => $"{id{|caret::|}D}"; }
            """;
        var hover = await GetHoverAsync(markup);
        Assert.NotNull(hover);
        Assert.NotNull(hover.Range);
        Assert.Equal(2, hover.Range.End.Character - hover.Range.Start.Character);
    }

    [Fact]
    public async Task NullableGuidExplainsNull()
    {
        var hover = await GetHoverAsync("""
            class C { string M(System.Guid? id) => $"{id:{|caret:D|}}"; }
            """);
        Assert.NotNull(hover);
        Assert.Contains("When the expression is null", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("int", "D")]
    [InlineData("System.DateTime", "D")]
    [InlineData("object", "D")]
    [InlineData("dynamic", "D")]
    [InlineData("System.Guid", "D6")]
    [InlineData("System.Guid", "Q")]
    public async Task OtherTypesAndInvalidFormatsHaveNoGuidHover(string type, string format)
    {
        var markup = """
            class C { string M(TYPE id) => $"{id:{|caret:FORMAT|}}"; }
            """.Replace("TYPE", type).Replace("FORMAT", format);
        var hover = await GetHoverAsync(markup);
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("Guid format"));
    }

    [Fact]
    public async Task DeferredFormattingDoesNotAssumeTheFormatProvider()
    {
        var hover = await GetHoverAsync("""
            class C { System.FormattableString M(System.Guid id) => $"{id:{|caret:D|}}"; }
            """);
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("Guid format"));
    }

    [Fact]
    public async Task PlainTextClientsDoNotReceiveMarkdown()
    {
        var hover = await GetHoverAsync("""
            class C { string M(System.Guid id) => $"{id:{|caret:X|}}"; }
            """, supportsMarkdown: false);
        Assert.NotNull(hover);
        Assert.Equal(LSP.MarkupKind.PlainText, hover.Contents.Fourth.Kind);
        Assert.Contains("Four hexadecimal values enclosed in braces", hover.Contents.Fourth.Value);
        Assert.Contains("Microsoft Learn: format strings", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("##", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("```", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("int", "D6", "012345", "standard-numeric")]
    [InlineData("int", "X8", "00003039", "standard-numeric")]
    [InlineData("int", "B16", "0011000000111001", "standard-numeric")]
    [InlineData("byte", "D4", "0123", "standard-numeric")]
    [InlineData("decimal", "N2", "1,234.50", "standard-numeric")]
    [InlineData("decimal", "C2", "$1,234.50", "standard-numeric")]
    [InlineData("decimal", "F4", "1234.5000", "standard-numeric")]
    [InlineData("decimal", "G29", "1234.5", "standard-numeric")]
    [InlineData("double", "E2", "1.23E+003", "standard-numeric")]
    [InlineData("float", "R", "1234.5", "standard-numeric")]
    [InlineData("double", "P1", "123,450.0", "standard-numeric")]
    [InlineData("decimal", "0.######", "1234.5", "custom-numeric")]
    [InlineData("decimal", "000000000.000000", "000001234.500000", "custom-numeric")]
    [InlineData("decimal", "#,##0.00;(#,##0.00);'zero'", "1,234.50", "custom-numeric")]
    [InlineData("decimal", "0.0%", "123450.0%", "custom-numeric")]
    [InlineData("System.DateTime", "O", "2026-07-04T13:45:06.1234567Z", "standard-date")]
    [InlineData("System.DateTimeOffset", "O", "2026-07-04T13:45:06.1234567+02:00", "standard-date")]
    [InlineData("System.DateTime", "yyyy-MM-dd", "2026-07-04", "custom-date")]
    [InlineData("System.DateTime", "MM/dd/yyyy", "07/04/2026", "custom-date")]
    [InlineData("System.DateTime", "yyyy-MM-ddTHH:mm:ss.fffZ", "2026-07-04T13:45:06.123Z", "custom-date")]
    [InlineData("System.DateTime", "yyyy-MM-dd HH:mm 'UTC'", "2026-07-04 13:45 UTC", "custom-date")]
    [InlineData("System.DateTime", "%d", "4", "custom-date")]
    [InlineData("System.TimeSpan", "c", "2.13:45:06.1234567", "standard-timespan")]
    [InlineData("System.TimeSpan", "g", "2:13:45:06.1234567", "standard-timespan")]
    [InlineData("System.TimeSpan", "dd\\.hh\\:mm", "02.13:45", "custom-timespan")]
    [InlineData("System.Guid", "", "12345678-1234-5678-9abc-def012345678", "system.guid.tostring")]
    public async Task FrameworkFormatsHaveTypedExamples(string type, string format, string example, string documentation)
    {
        var markup = "class C { string M(" + type + " value) => $\"{value:{|caret:" + format.Replace("\\", "\\\\") + "|}}\"; }";
        var hover = await GetHoverAsync(markup);
#if NETFRAMEWORK
        if (format == "B16")
        {
            Assert.Null(hover);
            return;
        }
#endif
        Assert.NotNull(hover);
        var markdown = hover.Contents.Fourth.Value;
        Assert.Contains(example, markdown);
        Assert.Contains(documentation, markdown);
        Assert.Contains("## Example", markdown);
        Assert.DoesNotContain("Sample:", markdown);
        Assert.DoesNotContain("Culture:", markdown);
    }

    [Theory]
    [InlineData("value.ToString(\"{|caret:N2|}\")", "decimal", "1,234.50")]
    [InlineData("value.ToString(format: \"{|caret:yyyy-MM-dd|}\", provider: System.Globalization.CultureInfo.InvariantCulture)", "System.DateTime", "2026-07-04")]
    [InlineData("string.Format(\"Amount {0:{|caret:C2|}}\", value)", "decimal", "$1,234.50")]
    [InlineData("string.Format(\"{1:{|caret:D6|}} {0:N2}\", 1m, value)", "int", "012345")]
    [InlineData("string.Format(\"{{literal}} {0,12:{|caret:N2|}}\", value)", "decimal", "1,234.50")]
    [InlineData("new System.Text.StringBuilder().AppendFormat(\"{0:{|caret:N2|}}\", value).ToString()", "decimal", "1,234.50")]
    [InlineData("string.Format(arg0: value, format: \"{0:{|caret:N2|}}\")", "decimal", "1,234.50")]
    [InlineData("string.Format(\"😀 {0:{|caret:N2|}}\", value)", "decimal", "1,234.50")]
    [InlineData("string.Format(\"{3:{|caret:N2|}}\", 1, 2, 3, value)", "decimal", "1,234.50")]
    [InlineData("string.Format(\"{1:{|caret:N2|}}\", new object[] { 1, value })", "decimal", "1,234.50")]
    [InlineData("string.Format(\"{0, -12 :{|caret:N2|}}\", value)", "decimal", "1,234.50")]
    [InlineData("value.ToString(\"{|caret:D|}\")", "System.DayOfWeek", "0")]
    [InlineData("value.ToString(\"{|caret:X|}\")", "System.DayOfWeek", "00000000")]
    public async Task FormatArgumentsAreRecognized(string expression, string type, string example)
    {
        var hover = await GetHoverAsync("class C { string M(" + type + " value) => " + expression + "; }");
        Assert.NotNull(hover);
        Assert.Contains(example, hover.Contents.Fourth.Value);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("decimal", "D2")]
    [InlineData("double", "X")]
    [InlineData("System.DateTimeOffset", "U")]
    [InlineData("System.DateTime", "Q")]
    [InlineData("System.TimeSpan", "hh:mm")]
    public async Task InvalidFormatsDoNotHaveAnExplanation(string type, string format)
    {
        var hover = await GetHoverAsync("class C { string M(" + type + " value) => $\"{value:{|caret:" + format + "|}}\"; }");
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Example"));
    }

    [Fact]
    public async Task LargePrecisionDoesNotAllocateTheRequestedOutput()
    {
        var hover = await GetHoverAsync("""
            class C { string M(decimal value) => $"{value:{|caret:F999999999|}}"; }
            """);
        Assert.NotNull(hover);
        Assert.Contains("999999999", hover.Contents.Fourth.Value);
        Assert.Contains("Example omitted", hover.Contents.Fourth.Value);
        Assert.True(hover.Contents.Fourth.Value.Length < 2000);
    }

    [Fact]
    public void StandardNumericCatalogMatchesFrameworkFormatting()
    {
        var samples = new List<IFormattable>
        {
            (byte)123, (sbyte)123, (short)12345, (ushort)12345, 12345, 12345U, 12345L, 12345UL,
            1234.5f, 1234.5, 1234.5m, new BigInteger(12345), new Complex(1234.5, 6.25),
        };
#if NET
        samples.Add((Half)1234.5f);
        samples.Add((Int128)12345);
        samples.Add((UInt128)12345);
        samples.Add((nint)12345);
        samples.Add((nuint)12345);
#endif
        var compilation = CreateCatalogCompilation();
        foreach (var sample in samples)
        {
            var type = compilation.GetTypeByMetadataName(sample.GetType().FullName!);
            Assert.NotNull(type);
            foreach (var specifier in "bBcCdDeEfFgGnNpPrRxX")
            {
                foreach (var precision in new[] { "", "0", "2", "3", "4", "6", "10", "12", "29" })
                {
                    var format = specifier + precision;
                    string? expected;
                    try { expected = sample.ToString(format, CultureInfo.GetCultureInfo("en-US")); }
                    catch (FormatException) { expected = null; }
                    var found = CSharpFormatStringInfo.TryCreate(type, compilation, format, out var info);
                    Assert.True(found == (expected is not null), type.Name + ":" + format + " in " + type.ContainingNamespace + " from " + type.ContainingAssembly?.Name + "; actual=" + found + "; expected=" + expected);
                    if (found)
                        Assert.Equal(expected, info.Example);
                }
            }
        }
    }

    [Fact]
    public void StandardTemporalCatalogMatchesFrameworkFormatting()
    {
        var date = new DateTime(2026, 7, 4, 13, 45, 6, DateTimeKind.Utc).AddTicks(1234567);
        var samples = new List<IFormattable>
        {
            date,
            new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Unspecified), TimeSpan.FromHours(2)),
            new TimeSpan(2, 13, 45, 6).Add(TimeSpan.FromTicks(1234567)),
        };
#if NET
        samples.Add(DateOnly.FromDateTime(date));
        samples.Add(TimeOnly.FromDateTime(date));
#endif
        var compilation = CreateCatalogCompilation();
        foreach (var sample in samples)
        {
            var type = compilation.GetTypeByMetadataName(sample.GetType().FullName!);
            Assert.NotNull(type);
            foreach (var format in new[] { "", "d", "D", "f", "F", "g", "G", "m", "M", "o", "O", "r", "R", "s", "t", "T", "u", "U", "y", "Y", "c" })
            {
                string? expected;
                try { expected = sample.ToString(format, CultureInfo.GetCultureInfo("en-US")); }
                catch (FormatException) { expected = null; }
                var found = CSharpFormatStringInfo.TryCreate(type, compilation, format, out var info);
                Assert.True(found == (expected is not null), type.Name + ":" + format + " in " + type.ContainingNamespace + " from " + type.ContainingAssembly?.Name + "; actual=" + found + "; expected=" + expected);
                if (found)
                    Assert.Equal(expected, info.Example);
            }
        }
    }

    [Fact]
    public void CustomPatternsMatchFrameworkFormatting()
    {
        var compilation = CreateCatalogCompilation();
        var patterns = new (IFormattable Sample, string[] Formats)[]
        {
            (1234.5m, ["0", "0.#", "0.##", "0.####", "0.######", "0.00", "0.000", "0.000000", "00", "00.000000", "000", "0000", "00000", "000000000.000000", "0.0%", "0.00‰", "0.00E+00", "0,,", "0.0;(0.0);'zero'", "'USD' #,##0.00"]),
            (new DateTime(2026, 7, 4, 13, 45, 6, DateTimeKind.Utc).AddTicks(1234567), ["HH:mm:ss", "MM/dd/yyyy", "MMddyyyy", "MMMM dd, yyyy", "yyMMdd", "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm 'UTC'", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyyMMdd", "yyyyMMddHHmmss", "%d", "yyyyy MMMMM ddddd HHH:mmm:sss"]),
        };
        foreach (var (sample, formats) in patterns)
        {
            var type = compilation.GetTypeByMetadataName(sample.GetType().FullName!);
            Assert.NotNull(type);
            foreach (var format in formats)
            {
                Assert.True(CSharpFormatStringInfo.TryCreate(type, compilation, format, out var info), format);
                Assert.Equal(sample.ToString(format, CultureInfo.GetCultureInfo("en-US")), info.Example);
            }
        }
    }

    [Fact]
    public async Task CustomFormattersDoNotReceiveFrameworkExplanations()
    {
        var hover = await GetHoverAsync("""
            class Value { public string ToString(string format) => format; }
            class C { string M(Value value) => value.ToString("{|caret:N2|}"); }
            """);
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Example"));
    }

    [Fact]
    public async Task UnknownCompositeProviderDoesNotReceiveFrameworkExplanations()
    {
        var hover = await GetHoverAsync("""
            class C { string M(System.IFormatProvider provider, decimal value)
                => string.Format(provider, "{0:{|caret:N2|}}", value); }
            """);
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Example"));
    }

    [Fact]
    public async Task CustomLiteralBackticksRemainInsideTheOutputBlock()
    {
        var hover = await GetHoverAsync("""
            class C { string M(decimal value) => value.ToString("{|caret:'```\n```'0.0|}"); }
            """);
        Assert.NotNull(hover);
        Assert.Contains("````text\n```\n```1234.5\n````", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void NonNamedTypesDoNotThrowDuringTypeRecognition()
    {
        var tree = CSharpSyntaxTree.ParseText("class C<T> { void M(dynamic d, T t, int[] a) { } }");
        var compilation = CreateCatalogCompilation().AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);
        foreach (var parameter in tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax>())
        {
            var type = model.GetTypeInfo(parameter.Type!).Type;
            Assert.NotNull(type);
            Assert.False(CSharpFormatStringInfo.TryCreate(type, compilation, "D", out _));
        }
    }

    [Theory]
    [InlineData("F1000000000")]
    [InlineData("F9999999999")]
    public async Task InvalidLargePrecisionDoesNotReceiveDocumentation(string format)
    {
        var hover = await GetHoverAsync("class C { string M(decimal value) => $\"{value:{|caret:" + format + "|}}\"; }");
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Example"));
    }

    [Theory]
    [InlineData("--12")]
    [InlineData("- 12")]
    [InlineData("2147483648")]
    public async Task InvalidCompositeAlignmentDoesNotReceiveDocumentation(string alignment)
    {
        var hover = await GetHoverAsync("class C { string M(decimal value) => string.Format(\"{0," + alignment + ":{|caret:N2|}}\", value); }");
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Example"));
    }

    [Fact]
    public async Task CultureInfoVariableCanProvideCustomFormatting()
    {
        var hover = await GetHoverAsync("""
            using System;
            using System.Globalization;
            class Provider : CultureInfo, ICustomFormatter
            {
                public Provider() : base("en-US") { }
                public override object GetFormat(Type type) => this;
                public string Format(string format, object value, IFormatProvider provider) => "custom";
            }
            class C { string M(decimal value)
                { CultureInfo provider = new Provider(); return string.Format(provider, "{0:{|caret:N2|}}", value); } }
            """);
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Example"));
    }

    [Fact]
    public async Task EmptyEnumsHaveNumericFormatExamples()
    {
        var hover = await GetHoverAsync("""
            enum E { }
            class C { string M(E value) => $"{value:{|caret:D|}}"; }
            """);
        Assert.NotNull(hover);
        Assert.Contains("```text\n0\n```", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("System.Guid", "D", "guid", "D")]
    [InlineData("System.Guid", "X", "guid", "X")]
    [InlineData("System.Int32", "D", "numeric-standard", "D")]
    [InlineData("System.Decimal", "N", "numeric-standard", "N")]
    [InlineData("System.DateTime", "O", "date-standard", "O")]
    [InlineData("System.TimeSpan", "c", "duration-standard", "c")]
    public void StandardDescriptionsAreExactSourceExtracts(string typeName, string format, string family, string key)
    {
        var compilation = CreateCatalogCompilation();
        var type = compilation.GetTypeByMetadataName(typeName);
        Assert.NotNull(type);
        Assert.True(CSharpFormatStringInfo.TryCreate(type, compilation, format, out var info));
        Assert.Equal(CSharpFormatStringDocumentation.Get(family, key), info.Description);
    }

    [Theory]
    [InlineData("System.Globalization.CultureInfo.InvariantCulture")]
    [InlineData("new System.Globalization.CultureInfo(\"en-US\")")]
    [InlineData("System.Globalization.CultureInfo.GetCultureInfo(\"en-US\")")]
    public async Task KnownFrameworkCulturesHaveCompositeFormatHover(string provider)
    {
        var hover = await GetHoverAsync("class C { string M(decimal value) => string.Format(" + provider + ", \"{0:{|caret:N2|}}\", value); }");
        Assert.NotNull(hover);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("string.Format(\"{0:{|caret:N2|}}\", values)")]
    [InlineData("string.Format(\"{0:{|caret:N2|}}\", [.. values])")]
    public async Task UnknownParamsElementsDoNotGuessTheType(string expression)
    {
        var hover = await GetHoverAsync("class C { string M(object[] values) => " + expression + "; }");
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Example"));
    }

    [Fact]
    public async Task RawStringFormatUsesTheVirtualCharacterSpan()
    {
        var hover = await GetHoverAsync("class C { string M(decimal value) => string.Format(\"\"\"{0:{|caret:N2|}}\"\"\", value); }");
        Assert.NotNull(hover);
        Assert.Contains("1,234.50", hover.Contents.Fourth.Value);
        Assert.Equal(3, hover.Range!.End.Character - hover.Range.Start.Character);
    }

    [Fact]
    public async Task EscapedCompositeColonUsesTheSourceSpan()
    {
        var hover = await GetHoverAsync("class C { string M(decimal value) => string.Format(\"{0{|caret:\\u003AN2|}}\", value); }");
        Assert.NotNull(hover);
        Assert.Contains("1,234.50", hover.Contents.Fourth.Value);
        Assert.Equal(8, hover.Range!.End.Character - hover.Range.Start.Character);
    }

    [Theory]
    [InlineData("$\"{42:{|caret:D6|}}\"", "000042")]
    [InlineData("$\"{6 * 7:{|caret:X4|}}\"", "002A")]
    [InlineData("$\"{12.5m:{|caret:N2|}}\"", "12.50")]
    [InlineData("$\"{(byte)7:{|caret:D4|}}\"", "0007")]
    [InlineData("$\"{-42:{|caret:D6|}}\"", "-000042")]
    [InlineData("$\"{Number:{|caret:D6|}}\"", "000042")]
    [InlineData("(42).ToString(\"{|caret:D6|}\")", "000042")]
    [InlineData("Number.ToString(\"{|caret:X4|}\")", "002A")]
    [InlineData("string.Format(\"{0:{|caret:D6|}}\", 42)", "000042")]
    [InlineData("string.Format(arg0: Number, format: \"{0:{|caret:D6|}}\")", "000042")]
    [InlineData("string.Format(\"{3:{|caret:D6|}}\", 1, 2, 3, 42)", "000042")]
    [InlineData("string.Format(\"{1:{|caret:D6|}}\", new object[] { 1, Number })", "000042")]
    [InlineData("$\"{System.DayOfWeek.Friday:{|caret:X|}}\"", "00000005")]
    [InlineData("System.DayOfWeek.Friday.ToString(\"{|caret:D|}\")", "5")]
    [InlineData("$\"{(System.Guid?)null:{|caret:D|}}\"", "")]
    public async Task CompilerConstantsUseTheirActualValue(string expression, string expected)
    {
        var hover = await GetHoverAsync("class C { const int Number = 42; string M() => " + expression + "; }");
        Assert.NotNull(hover);
        Assert.Contains("```text\n" + expected + "\n```", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
        Assert.Contains("## Preview", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("## Example", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("amount.ToString(\"{|caret:N2|}\", System.Globalization.CultureInfo.GetCultureInfo(\"fr-FR\"))", "12,50")]
    [InlineData("$\"{amount:{|caret:N2|}}\"", "12.50")]
    [InlineData("string.Format(\"{0:{|caret:N2|}}\", amount)", "12.50")]
    public async Task UnchangedLocalProducesPreview(string expression, string expected)
    {
        var hover = await GetHoverAsync("class C { string M() { decimal amount = 12.5m; return " + expression + "; } }");
        Assert.NotNull(hover);
        Assert.Contains("## Preview", hover.Contents.Fourth.Value);
        Assert.Contains("```text\n" + expected + "\n```", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("new System.DateTime(2000, 1, 2)", "yyyy-MM-dd", "2000-01-02")]
    [InlineData("new System.DateTime(2000, 1, 2, 3, 4, 5, System.DateTimeKind.Utc).AddTicks(value: 1234)", "O", "2000-01-02T03:04:05.0001234Z")]
    [InlineData("System.Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "D", "00000000-0000-0000-0000-000000000001")]
    public async Task UnchangedFrameworkInitializerProducesPreview(string initializer, string format, string expected)
    {
        var hover = await GetHoverAsync("class C { string M() { var value = " + initializer + "; return $\"{value:{|caret:" + format + "|}}\"; } }");
        Assert.NotNull(hover);
        Assert.Contains("## Preview", hover.Contents.Fourth.Value);
        Assert.Contains("```text\n" + expected + "\n```", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("decimal price = 12.5m; var count = 3; var value = System.Math.Round(price * count + 0.126m, 2);", "value", "N2", "37.63")]
    [InlineData("var date = new System.DateTime(2000, 1, 2, 3, 4, 5, System.DateTimeKind.Utc).AddTicks(1234); var value = date.AddDays(2) + System.TimeSpan.FromHours(1);", "new System.DateTimeOffset(value)", "O", "2000-01-04T04:04:05.0001234+00:00")]
    [InlineData("var date = new System.DateTime(2000, 1, 2, 3, 4, 5); var value = System.DateOnly.FromDateTime(date);", "value", "O", "2000-01-02")]
    [InlineData("var date = new System.DateTime(2000, 1, 2, 3, 4, 5); var value = System.TimeOnly.FromDateTime(date);", "value", "O", "03:04:05.0000000")]
    [InlineData("int? number = 42; var positive = true;", "checked((byte)(positive ? number ?? 0 : 1))", "D4", "0042")]
    [InlineData("int? number = -1; var value = number ?? 0L;", "value", "X", "FFFFFFFFFFFFFFFF")]
    [InlineData("var big = new System.Numerics.BigInteger(42);", "(int)big", "D4", "0042")]
    [InlineData("var date = new System.DateTime(2000, 1, 2);", "date.DayOfWeek", "X", "00000000")]
    [InlineData("var value = new System.DateTime(2000, 1, 2);", "value", "yyyy-MM-dd 'z'", "2000-01-02 z")]
    [InlineData("decimal? value = null;", "value + 1m", "N2", "")]
    public async Task ComposedValuesProducePreviews(string declarations, string expression, string format, string expected)
    {
        var hover = await GetHoverAsync("class C { string M() { " + declarations + " return $\"{" + expression + ":{|caret:" + format + "|}}\"; } }", modernReferences: true);
#if NETFRAMEWORK
        if (declarations.Contains("System.DateOnly") || declarations.Contains("System.TimeOnly"))
        {
            Assert.Null(hover);
            return;
        }
#endif
        Assert.NotNull(hover);
        Assert.Contains("## Preview", hover.Contents.Fourth.Value);
        Assert.Contains("```text\n" + expected + "\n```", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("var date = new System.DateTime(2000, 1, 2); date = date.AddDays(1); var value = date.AddDays(2);", "value", "yyyy-MM-dd")]
    [InlineData("var value = System.DateTime.UtcNow.AddDays(2);", "value", "yyyy-MM-dd")]
    [InlineData("var value = new System.DateTimeOffset(new System.DateTime(2000, 1, 2));", "value", "yyyy-MM-dd")]
    [InlineData("var value = new System.Numerics.BigInteger(1);", "value << int.MaxValue", "D")]
    [InlineData("var value = new System.DateTime(2000, 1, 2, 0, 0, 0, System.DateTimeKind.Local);", "value", "O")]
    [InlineData("var value = \"i\".Replace(\"I\", \"123\", System.StringComparison.CurrentCultureIgnoreCase).Length;", "value", "D")]
    [InlineData("var value = new System.DateTime(2000, 1, 2);", "value", "U")]
    [InlineData("var value = new System.DateTime(2000, 1, 2);", "value", "yyyy-MM-dd zzz")]
    [InlineData("int? number = null;", "(int)number", "D4")]
    [InlineData("var number = (object)42;", "(long)number", "D4")]
    public async Task UnprovenComposedValuesKeepExamples(string declarations, string expression, string format)
    {
        var hover = await GetHoverAsync("class C { string M() { " + declarations + " return $\"{" + expression + ":{|caret:" + format + "|}}\"; } }", modernReferences: true);
        Assert.NotNull(hover);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("## Preview", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("amount = 7m;")]
    [InlineData("ref decimal alias = ref amount; alias = 7m;")]
    [InlineData("Escape(amount);")]
    [InlineData("Escape(in (true ? ref amount : ref amount));")]
    public async Task MutatedOrAliasedLocalKeepsExample(string mutation)
    {
        var hover = await GetHoverAsync("class C { static void Escape(in decimal value) {} string M() { decimal amount = 1234.5m; " + mutation + " return amount.ToString(\"{|caret:N2|}\"); } }");
        Assert.NotNull(hover);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("## Preview", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("int value", "$\"{value:{|caret:D6|}}\"", "012345")]
    [InlineData("System.Guid value", "$\"{value:{|caret:D|}}\"", "12345678-1234-5678-9abc-def012345678")]
    public async Task NonConstantValuesRetainTheFixedSample(string parameters, string expression, string expected)
    {
        var hover = await GetHoverAsync("class C { string M(" + parameters + ") => " + expression + "; }");
        Assert.NotNull(hover);
        Assert.Contains(expected, hover.Contents.Fourth.Value);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("## Preview", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("new System.Guid(\"00000000-0000-0000-0000-000000000001\").ToString(\"{|caret:D|}\")", "00000000-0000-0000-0000-000000000001")]
    [InlineData("System.Guid.Parse(\"00000000-0000-0000-0000-000000000001\").ToString(\"{|caret:D|}\")", "00000000-0000-0000-0000-000000000001")]
    [InlineData("$\"{System.Guid.Parse(input: Id):{|caret:N|}}\"", "00000000000000000000000000000001")]
    [InlineData("string.Format(\"{0:{|caret:B|}}\", System.Guid.Parse(Id))", "{00000000-0000-0000-0000-000000000001}")]
    [InlineData("$\"{new System.DateTime(2000, 1, 2):{|caret:yyyy-MM-dd|}}\"", "2000-01-02")]
    [InlineData("new System.DateTime(day: 2, year: 2000, month: 1).ToString(\"{|caret:yyyy-MM-dd|}\")", "2000-01-02")]
    [InlineData("string.Format(\"{0:{|caret:s|}}\", new System.DateTime(2000 + 1, 2, 3, 4, 5, 6))", "2001-02-03T04:05:06")]
    [InlineData("$\"{new System.DateTime(2000, 1, 2, 3, 4, 5, System.DateTimeKind.Utc):{|caret:O|}}\"", "2000-01-02T03:04:05.0000000Z")]
    [InlineData("$\"{new System.DateTime(2000, 1, 2, 3, 4, 5, 678):{|caret:O|}}\"", "2000-01-02T03:04:05.6780000")]
    [InlineData("$\"{new System.DateTime(kind: System.DateTimeKind.Utc, millisecond: 678, second: 5, minute: 4, hour: 3, day: 2, month: 1, year: 2000):{|caret:O|}}\"", "2000-01-02T03:04:05.6780000Z")]
    [InlineData("$\"{new System.DateTime(0L):{|caret:O|}}\"", "0001-01-01T00:00:00.0000000")]
    [InlineData("$\"{new System.DateTime(kind: System.DateTimeKind.Utc, ticks: 0L):{|caret:O|}}\"", "0001-01-01T00:00:00.0000000Z")]
    [InlineData("$\"{new System.DateTime():{|caret:yyyy-MM-dd|}}\"", "0001-01-01")]
    public async Task AllowlistedFrameworkCallsProducePreviews(string expression, string expected)
    {
        var hover = await GetHoverAsync("class C { const string Id = \"00000000-0000-0000-0000-000000000001\"; string M() => " + expression + "; }");
        Assert.NotNull(hover);
        Assert.Contains("```text\n" + expected + "\n```", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
        Assert.Contains("## Preview", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("## Example", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("$\"{System.Guid.Parse(input):{|caret:D|}}\"", "12345678-1234-5678-9abc-def012345678")]
    [InlineData("$\"{System.Guid.Parse(\"invalid\"):{|caret:D|}}\"", "12345678-1234-5678-9abc-def012345678")]
    [InlineData("$\"{new System.DateTime(year, 1, 2):{|caret:yyyy-MM-dd|}}\"", "2026-07-04")]
    [InlineData("$\"{new System.DateTime(2000, 13, 2):{|caret:yyyy-MM-dd|}}\"", "2026-07-04")]
    [InlineData("$\"{new System.DateTime(-1L):{|caret:yyyy-MM-dd|}}\"", "2026-07-04")]
    [InlineData("$\"{new System.DateTime(2000, 1, 2).AddTicks(year):{|caret:yyyy-MM-dd|}}\"", "2026-07-04")]
    [InlineData("$\"{new System.DateTime(0L).AddTicks(-1):{|caret:yyyy-MM-dd|}}\"", "2026-07-04")]
    [InlineData("$\"{new System.DateTime(0L, (System.DateTimeKind)99):{|caret:yyyy-MM-dd|}}\"", "2026-07-04")]
    [InlineData("$\"{new System.DateTime(2000, 1, 2, (System.Globalization.Calendar)null):{|caret:yyyy-MM-dd|}}\"", "2026-07-04")]
    [InlineData("$\"{new System.DateTime(GetYear(), 1, 2):{|caret:yyyy-MM-dd|}}\"", "2026-07-04")]
    [InlineData("$\"{GetDate(2000):{|caret:yyyy-MM-dd|}}\"", "2026-07-04")]
    [InlineData("$\"{Fake.Parse(\"00000000-0000-0000-0000-000000000001\"):{|caret:D|}}\"", "12345678-1234-5678-9abc-def012345678")]
    public async Task UnknownInvalidAndProjectCallsKeepExamples(string expression, string expected)
    {
        var hover = await GetHoverAsync("class Fake { public static System.Guid Parse(string s) => throw new System.Exception(); } class C { static int GetYear() => throw new System.Exception(); static System.DateTime GetDate(int year) => throw new System.Exception(); string M(string input, int year) => " + expression + "; }");
        Assert.NotNull(hover);
        Assert.Contains(expected, hover.Contents.Fourth.Value);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("## Preview", hover.Contents.Fourth.Value);
    }

    [Fact]
    public async Task OmittedOutputDoesNotClaimToBeAPreview()
    {
        var hover = await GetHoverAsync("class C { string M() => $\"{42:{|caret:F999999999|}}\"; }");
        Assert.NotNull(hover);
        Assert.Contains("Example omitted", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("```text", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("## Preview", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstantEnumsMatchTheFramework(bool flags)
    {
        var compilation = CreateCatalogCompilation().AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            (flags ? "[System.Flags] " : "") + "enum E : sbyte { Zero = 0, One = 1, Two = 2, Four = 4, All = -1 }"));
        var type = compilation.GetTypeByMetadataName("E")!;
        foreach (sbyte value in new sbyte[] { -128, -1, 0, 1, 2, 3, 4, 5, 7, 8, 127 })
        {
            Enum expected = flags ? (ConstantFlags)value : (ConstantEnum)value;
            foreach (var format in new[] { "", "G", "g", "F", "f", "D", "d", "X", "x" })
            {
                Assert.True(CSharpFormatStringInfo.TryCreate(type, compilation, format, out var info, value));
                Assert.Equal(expected.ToString(format), info.Example);
            }
        }
    }

    [Theory]
    [InlineData("value.ToString(\"{|caret:N2|}\", System.Globalization.CultureInfo.InvariantCulture)", "")]
    [InlineData("value.ToString(\"{|caret:N2|}\", System.Globalization.CultureInfo.GetCultureInfo(\"fr-FR\"))", "fr-FR")]
    [InlineData("value.ToString(provider: new System.Globalization.CultureInfo(\"de-DE\"), format: \"{|caret:N2|}\")", "de-DE")]
    [InlineData("value.ToString(\"{|caret:N2|}\", new System.Globalization.CultureInfo(useUserOverride: false, name: \"de-DE\"))", "de-DE")]
    [InlineData("string.Format(System.Globalization.CultureInfo.GetCultureInfo(\"fr-FR\"), \"{0:{|caret:N2|}}\", value)", "fr-FR")]
    [InlineData("string.Format(provider: System.Globalization.CultureInfo.InvariantCulture, arg0: value, format: \"{0:{|caret:N2|}}\")", "")]
    [InlineData("new System.Text.StringBuilder().AppendFormat(new System.Globalization.CultureInfo(\"de-DE\"), \"{0:{|caret:N2|}}\", value).ToString()", "de-DE")]
    [InlineData("string.Create(System.Globalization.CultureInfo.GetCultureInfo(\"fr-FR\"), $\"{value:{|caret:N2|}}\")", "fr-FR")]
    public async Task ExplicitCultureControlsNumericOutput(string expression, string culture)
    {
        var hover = await GetHoverAsync("class C { string M(decimal value) => " + expression + "; }", modernReferences: true);
        Assert.NotNull(hover);
        Assert.Contains("```text\n" + 1234.5m.ToString("N2", CultureInfo.GetCultureInfo(culture)) + "\n```", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task ExplicitCultureControlsDatePreview()
    {
        var hover = await GetHoverAsync("""
            class C { string M() => new System.DateTime(2000, 1, 2).ToString("{|caret:D|}", System.Globalization.CultureInfo.CreateSpecificCulture("fr-FR")); }
            """);
        Assert.NotNull(hover);
        Assert.Contains("```text\n" + new DateTime(2000, 1, 2).ToString("D", CultureInfo.GetCultureInfo("fr-FR")) + "\n```", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
        Assert.Contains("## Preview", hover.Contents.Fourth.Value);
    }

    [Fact]
    public async Task InvariantCultureControlsCurrencyPreview()
    {
        var hover = await GetHoverAsync("""
            class C { string M() => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:{|caret:C2|}}", 12.5m); }
            """);
        Assert.NotNull(hover);
        Assert.Contains("```text\n" + 12.5m.ToString("C2", CultureInfo.InvariantCulture) + "\n```", hover.Contents.Fourth.Value.ReplaceLineEndings("\n"));
        Assert.Contains("## Preview", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData("X")]
    [InlineData("x2")]
    public void FloatingHexFollowsHostSupportAndOfficialDescription(string format)
    {
        var compilation = CreateCatalogCompilation();
        var available = CSharpFormatStringInfo.TryCreate(compilation.GetSpecialType(SpecialType.System_Double), compilation, format, out var info);
        string expected;
        try
        {
            expected = 1234.5.ToString(format, CultureInfo.GetCultureInfo("en-US"));
        }
        catch (FormatException)
        {
            Assert.False(available);
            return;
        }
        Assert.True(available);
        Assert.Equal(expected, info.Example);
        Assert.Equal(CSharpFormatStringDocumentation.Get("numeric-hex-float", "X"), info.Description);
        Assert.Contains("dotnet-11", info.DocumentationUrl);
    }

    [Theory]
    [InlineData("provider")]
    [InlineData("System.Globalization.CultureInfo.CurrentCulture")]
    [InlineData("GetCulture()")]
    [InlineData("new System.Globalization.CultureInfo(name)")]
    [InlineData("System.Globalization.CultureInfo.GetCultureInfo(\"/\")")]
    [InlineData("new System.Globalization.CultureInfo(\"fr-FR\") { NumberFormat = new System.Globalization.NumberFormatInfo() }")]
    public async Task UnknownCulturesKeepTheDefault(string provider)
    {
        var hover = await GetHoverAsync("class C { static System.Globalization.CultureInfo GetCulture() => throw new System.Exception(); string M(decimal value, string name, System.IFormatProvider provider) => value.ToString(\"{|caret:N2|}\", " + provider + "); }");
        Assert.NotNull(hover);
        Assert.Contains("1,234.50", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("assumed", hover.Contents.Fourth.Value);
    }

    [Fact]
    public async Task ValuesOutsideTheCultureCalendarDoNotThrow()
    {
        var hover = await GetHoverAsync("""
            class C { string M() => new System.DateTime(1, 1, 1).ToString("{|caret:D|}", System.Globalization.CultureInfo.GetCultureInfo("ar-SA")); }
            """);
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Preview"));
    }

    [Fact]
    public async Task ProjectTypesCannotReplaceFrameworkCultureSemantics()
    {
        var hover = await GetHoverAsync("""
            namespace System.Globalization
            {
                class CultureInfo : System.IFormatProvider, System.ICustomFormatter
                {
                    public static CultureInfo InvariantCulture => new CultureInfo();
                    public object GetFormat(System.Type type) => this;
                    public string Format(string format, object value, System.IFormatProvider provider) => "custom";
                }
            }
            class C { string M(decimal value) => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:{|caret:N2|}}", value); }
            """);
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Example"));
    }

    [Fact]
    public async Task ProjectTypesCannotReplaceFrameworkConsoleSemantics()
    {
        var hover = await GetHoverAsync("""
            namespace System { static class Console { public static void WriteLine(string format, object arg0) { } } }
            class C { void M(decimal value) => System.Console.WriteLine("{0:{|caret:N2|}}", value); }
            """);
        Assert.True(hover is null || !hover.Contents.Fourth.Value.Contains("## Example"));
    }

    [Fact]
    public async Task NativeIntegerWidthDoesNotClaimAPreview()
    {
        var hover = await GetHoverAsync("""class C { string M() => $"{(nint)(-1):{|caret:X|}}"; }""", modernReferences: true);
        Assert.NotNull(hover);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
    }

    [Fact]
    public async Task UnsupportedTargetBinaryFormatDoesNotClaimAPreview()
    {
        var hover = await GetHoverAsync("""class C { string M() => $"{42:{|caret:B16|}}"; }""", modernReferences: true);
#if NETFRAMEWORK
        Assert.Null(hover);
#else
        Assert.NotNull(hover);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
#endif
    }

    [Theory]
    [InlineData("0.0EE", "EE", false)]
    [InlineData("0.0E0E", "E0", true)]
    [InlineData("0.0E+00E", "E+00", true)]
    public void NumericExponentDescriptionsRequireZeros(string format, string token, bool exponent)
    {
        var compilation = CreateCatalogCompilation();
        Assert.True(CSharpFormatStringInfo.TryCreate(compilation.GetSpecialType(SpecialType.System_Decimal), compilation, format, out var info));
        Assert.Contains(token + " — " + CSharpFormatStringDocumentation.Get("numeric-custom", exponent ? "E0" : "literal"), info.Description);
        Assert.Equal(1234.5m.ToString(format, CultureInfo.GetCultureInfo("en-US")), info.Example);
    }

    [Theory]
    [InlineData("G", false)]
    [InlineData("F", false)]
    [InlineData("D", true)]
    [InlineData("X", true)]
    public void EnumAliasesDoNotClaimAnExactNamePreview(string format, bool isPreview)
    {
        var compilation = CreateCatalogCompilation().AddSyntaxTrees(CSharpSyntaxTree.ParseText("enum E { Gray = 1, Grey = 1 }"));
        Assert.True(CSharpFormatStringInfo.TryCreate(compilation.GetTypeByMetadataName("E")!, compilation, format, out var info, 1));
        Assert.Equal(isPreview, info.IsPreview);
    }

    [Theory]
    [InlineData("decimal value = 12.5m; System.Action read = () => System.Console.WriteLine(value);", "value", "N2")]
    [InlineData("decimal value = 12.5m; ref readonly decimal alias = ref value;", "value", "N2")]
    [InlineData("goto L; decimal value = 12.5m; L:", "value", "N2")]
    [InlineData("", "System.Math.Sin(1)", "F2")]
    [InlineData("", "new System.DateTime(2000, 1, 2).AddDays(0.00000001)", "O")]
    [InlineData("", "System.TimeSpan.FromMilliseconds(900719925475)", "c")]
    [InlineData("", "System.TimeSpan.FromTicks(3) * 0.5", "c")]
    [InlineData("double value = 1e100;", "(int)value", "D")]
    [InlineData("var value = new System.DateTime(2000, 1, 2);", "(System.DateTimeOffset)value", "O")]
    [InlineData("", "new System.TimeOnly(12, 0).AddHours(0.00000001)", "O")]
    public async Task InferenceContractKeepsUnprovenValuesAsExamples(string declarations, string expression, string format)
    {
        var hover = await GetHoverAsync("class C { string M() { " + declarations + " return $\"{" + expression + ":{|caret:" + format + "|}}\"; } }", modernReferences: true);
#if NETFRAMEWORK
        if (expression.Contains("System.TimeOnly"))
        {
            Assert.Null(hover);
            return;
        }
#endif
        Assert.NotNull(hover);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("## Preview", hover.Contents.Fourth.Value);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task InferenceContractBoundsTheWholeAnalysisScope(bool longComment, bool localFunction)
    {
        var prefix = longComment ? "/*" + new string('x', 40000) + "*/" : string.Concat(Enumerable.Repeat("_ = 1;", 1000));
        var hover = await GetHoverAsync("class C { string M() { " + prefix + (localFunction ? "string Local() {" : "") +
            " decimal amount = 12.5m; return $\"{amount:{|caret:N2|}}\"; " + (localFunction ? "} return Local();" : "") + "} }");
        Assert.NotNull(hover);
        Assert.Contains(longComment ? "## Preview" : "## Example", hover.Contents.Fourth.Value);
    }

    [Fact]
    public async Task InferenceContractDoesNotGuessAWriterProvider()
    {
        var hover = await GetHoverAsync("""class C { void M(System.IO.TextWriter writer) => writer.Write("{0:{|caret:N2|}}", 42); }""");
        Assert.NotNull(hover);
        Assert.Contains("## Example", hover.Contents.Fourth.Value);
    }

    [Fact]
    public async Task InferenceContractOmitsLargeOutputButKeepsDocumentation()
    {
        var hover = await GetHoverAsync("""class C { string M() => $"{new System.Numerics.BigInteger(1) << 2000:{|caret:D|}}"; }""", modernReferences: true);
        Assert.NotNull(hover);
        Assert.Contains("Microsoft Learn", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("## Preview", hover.Contents.Fourth.Value);
        Assert.DoesNotContain("```text", hover.Contents.Fourth.Value);
    }

    private enum ConstantEnum : sbyte { Zero = 0, One = 1, Two = 2, Four = 4, All = -1 }
    [Flags]
    private enum ConstantFlags : sbyte { Zero = 0, One = 1, Two = 2, Four = 4, All = -1 }

    private static CSharpCompilation CreateCatalogCompilation()
        => CSharpCompilation.Create("FormatCatalog", references:
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(BigInteger).Assembly.Location),
        ]);

    private async Task<LSP.Hover?> GetHoverAsync(string markup, bool supportsMarkdown = true, bool modernReferences = false)
    {
        var capabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities
            {
                Hover = new LSP.HoverSetting { ContentFormat = [supportsMarkdown ? LSP.MarkupKind.Markdown : LSP.MarkupKind.PlainText] },
            },
        };
        var workspace = modernReferences ? LspTestWorkspace.CreateWorkspaceElement(LanguageNames.CSharp, files: [markup], commonReferences: false) : null;
        workspace?.Element("Project")!.SetAttributeValue("CommonReferencesNet7", true);
        await using var server = workspace is null
            ? await CreateTestLspServerAsync(markup, mutatingLspWorkspace: false, capabilities)
            : await CreateXmlTestLspServerAsync(workspace.ToString(), mutatingLspWorkspace: false, initializationOptions: new InitializationOptions { ClientCapabilities = capabilities });
        return await server.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(
            LSP.Methods.TextDocumentHoverName,
            CreateTextDocumentPositionParams(server.GetLocations("caret").Single()), CancellationToken.None);
    }
}
