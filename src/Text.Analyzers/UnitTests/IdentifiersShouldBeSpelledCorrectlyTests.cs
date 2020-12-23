// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Text.Analyzers.IdentifiersShouldBeSpelledCorrectlyAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Text.Analyzers.UnitTests
{
    public class IdentifiersShouldBeSpelledCorrectlyTests
    {
        public enum DictionaryType
        {
            Xml,
            Dic,
        }

        public static IEnumerable<object[]> MisspelledMembers
            => new[]
            {
                new object[] { CreateTypeWithConstructor("Clazz", constructorName: "{|#0:Clazz|}", isStatic: false), "Clazz", "Clazz.Clazz()" },
                new object[] { CreateTypeWithConstructor("Clazz", constructorName: "{|#0:Clazz|}", isStatic: true), "Clazz", "Clazz.Clazz()" },
                new object[] { CreateTypeWithField("Program", "{|#0:_fild|}"), "fild", "Program._fild" },
                new object[] { CreateTypeWithEvent("Program", "{|#0:Evt|}"), "Evt", "Program.Evt" },
                new object[] { CreateTypeWithProperty("Program", "{|#0:Naem|}"), "Naem", "Program.Naem" },
                new object[] { CreateTypeWithMethod("Program", "{|#0:SomeMathod|}"), "Mathod", "Program.SomeMathod()" },
            };

        public static IEnumerable<object[]> UnmeaningfulMembers
            => new[]
            {
                new object[] { CreateTypeWithConstructor("A", constructorName: "{|#0:A|}", isStatic: false), "A" },
                new object[] { CreateTypeWithConstructor("B", constructorName: "{|#0:B|}", isStatic: false), "B" },
                new object[] { CreateTypeWithField("Program", "{|#0:_c|}"), "c" },
                new object[] { CreateTypeWithEvent("Program", "{|#0:D|}"), "D" },
                new object[] { CreateTypeWithProperty("Program", "{|#0:E|}"), "E" },
                new object[] { CreateTypeWithMethod("Program", "{|#0:F|}"), "F" },
            };

        public static IEnumerable<object[]> MisspelledMemberParameters
            => new[]
            {
                new object[] { CreateTypeWithConstructor("Program", parameter: "int {|#0:yourNaem|}", isStatic: false), "Naem", "yourNaem", "Program.Program(int)" },
                new object[] { CreateTypeWithMethod("Program", "Method", "int {|#0:yourNaem|}"), "Naem", "yourNaem", "Program.Method(int)" },
                new object[] { CreateTypeWithIndexer("Program", "int {|#0:yourNaem|}"), "Naem", "yourNaem", "Program.this[int]" },
            };

        [Theory]
        [InlineData("namespace Bar { }")]
        [InlineData("class Program { }")]
        [InlineData("class Program { void Member() { } }")]
        [InlineData("class Program { int _variable = 1; }")]
        [InlineData("class Program { void Member(string name) { } }")]
        [InlineData("class Program { delegate int GetNumber(string name); }")]
        [InlineData("class Program<TResource> { }")]
        public async Task NoMisspellings_Verify_NoDiagnostics(string source)
        {
            await VerifyCSharpAsync(source);
        }

        [Fact]
        public async Task MisspellingAllowedByGlobalXmlDictionary_Verify_NoDiagnostics()
        {
            var source = "class Clazz { }";
            var dictionary = CreateXmlDictionary(new[] { "clazz" });

            await VerifyCSharpAsync(source, dictionary);
        }

        [Fact]
        public async Task MisspellingAllowedByGlobalDicDictionary_Verify_NoDiagnostics()
        {
            var source = "class Clazz { }";
            var dictionary = CreateDicDictionary(new[] { "clazz" });

            await VerifyCSharpAsync(source, dictionary);
        }

        [Fact]
        public async Task MisspellingsAllowedByMultipleGlobalDictionaries_Verify_NoDiagnostics()
        {
            var source = @"class Clazz { const string Naem = ""foo""; }";
            var xmlDictionary = CreateXmlDictionary(new[] { "clazz" });
            var dicDictionary = CreateDicDictionary(new[] { "naem" });

            await VerifyCSharpAsync(source, new[] { xmlDictionary, dicDictionary });
        }

        [Fact]
        public async Task CorrectWordDisallowedByGlobalXmlDictionary_Verify_EmitsDiagnostic()
        {
            var source = "class Program { }";
            var dictionary = CreateXmlDictionary(null, new[] { "program" });

            await VerifyCSharpAsync(
                source,
                dictionary,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeRule)
                    .WithLocation(1, 7)
                    .WithArguments("Program", "Program"));
        }

        [Fact]
        public async Task MisspellingAllowedByProjectDictionary_Verify_NoDiagnostics()
        {
            var source = "class Clazz {}";
            var dictionary = CreateDicDictionary(new[] { "clazz" });

            await VerifyCSharpAsync(source, dictionary);
        }

        [Fact(Skip = "Adding additional files to specific projects is not yet supported")]
        public async Task MisspellingAllowedByDifferentProjectDictionary_Verify_EmitsDiagnostic()
        {
            var source = "class Clazz {}";
            var dictionary = CreateDicDictionary(new[] { "clazz" });

            await VerifyCSharpAsync(
                source,
                dictionary,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeRule)
                    .WithLocation(1, 7)
                    .WithArguments("Clazz", "Clazz"));
        }

        [Fact(Skip = "Specifying assembly names is not yet supported")]
        public async Task AssemblyMisspelled_Verify_EmitsDiagnostic()
        {
            var source = "class Program {}";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.AssemblyRule)
                    .WithArguments("Assambly", "MyAssambly"));
        }

        [Fact]
        public async Task AssemblyUnmeaningful_Verify_EmitsDiagnostic()
        {
            var source = "class Program {}";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.AssemblyMoreMeaningfulNameRule)
                    .WithArguments("A"));
        }

        [Fact]
        public async Task NamespaceMisspelled_Verify_EmitsDiagnostic()
        {
            var source = "namespace Tests.MyNarmspace {}";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.NamespaceRule)
                    .WithLocation(1, 17)
                    .WithArguments("Narmspace", "Tests.MyNarmspace"));
        }

        [Fact]
        public async Task NamespaceUnmeaningful_Verify_EmitsDiagnostic()
        {
            var source = "namespace Tests.A {}";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.NamespaceMoreMeaningfulNameRule)
                    .WithLocation(1, 17)
                    .WithArguments("A"));
        }

        [Theory]
        [InlineData("namespace MyNamespace { class {|#0:MyClazz|} {} }", "Clazz", "MyNamespace.MyClazz")]
        [InlineData("namespace MyNamespace { struct {|#0:MyStroct|} {} }", "Stroct", "MyNamespace.MyStroct")]
        [InlineData("namespace MyNamespace { enum {|#0:MyEnim|} {} }", "Enim", "MyNamespace.MyEnim")]
        [InlineData("namespace MyNamespace { interface {|#0:IMyFase|} {} }", "Fase", "MyNamespace.IMyFase")]
        [InlineData("namespace MyNamespace { delegate int {|#0:MyDelegete|}(); }", "Delegete", "MyNamespace.MyDelegete")]
        public async Task TypeMisspelled_Verify_EmitsDiagnostic(string source, string misspelling, string typeName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeRule)
                    .WithLocation(0)
                    .WithArguments(misspelling, typeName));
        }

        [Theory]
        [InlineData("class {|#0:A|} {}", "A")]
        [InlineData("struct {|#0:B|} {}", "B")]
        [InlineData("enum {|#0:C|} {}", "C")]
        [InlineData("interface {|#0:ID|} {}", "D")]
        [InlineData("delegate int {|#0:E|}();", "E")]
        public async Task TypeUnmeaningful_Verify_EmitsDiagnostic(string source, string typeName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments(typeName));
        }

        [Theory]
        [MemberData(nameof(MisspelledMembers))]
        public async Task MemberMisspelled_Verify_EmitsDiagnostic(string source, string misspelling, string memberName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberRule)
                    .WithLocation(0)
                    .WithArguments(misspelling, memberName));
        }

        [Fact]
        public async Task MemberOverrideMisspelled_Verify_EmitsDiagnosticOnlyAtDefinition()
        {
            var source = @"
        abstract class Parent
        {
            protected abstract string {|#0:Naem|} { get; }

            public abstract int {|#1:Mathod|}();
        }

        class Child : Parent
        {
            protected override string Naem => ""child"";

            public override int Mathod() => 0;
        }

        class Grandchild : Child
        {
            protected override string Naem => ""grandchild"";

            public override int Mathod() => 1;
        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberRule)
                    .WithLocation(0)
                    .WithArguments("Naem", "Parent.Naem"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberRule)
                    .WithLocation(1)
                    .WithArguments("Mathod", "Parent.Mathod()"));
        }

        [Theory]
        [MemberData(nameof(UnmeaningfulMembers))]
        public async Task MemberUnmeaningful_Verify_EmitsDiagnostic(string source, string memberName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments(memberName));
        }

        [Fact]
        public async Task VariableMisspelled_Verify_EmitsDiagnostic()
        {
            var source = @"
        class Program
        {
            public Program()
            {
                var {|#0:myVoriable|} = 5;
            }
        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.VariableRule)
                    .WithLocation(0)
                    .WithArguments("Voriable", "myVoriable"));
        }

        [Theory]
        [MemberData(nameof(MisspelledMemberParameters))]
        public async Task MemberParameterMisspelled_Verify_EmitsDiagnostic(string source, string misspelling, string parameterName, string memberName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(0)
                    .WithArguments(memberName, misspelling, parameterName));
        }

        [Fact]
        public async Task MemberParameterUnmeaningful_Verify_EmitsDiagnostic()
        {
            var source = @"
        class Program
        {
            public void Method(string {|#0:a|})
            {
            }

            public string this[int {|#1:i|}] => null;
        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments("Program.Method(string)", "a"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterMoreMeaningfulNameRule)
                    .WithLocation(1)
                    .WithArguments("Program.this[int]", "i"));
        }

        [Fact]
        public async Task DelegateParameterMisspelled_Verify_EmitsDiagnostic()
        {
            var source = "delegate void MyDelegate(string {|#0:firstNaem|});";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.DelegateParameterRule)
                    .WithLocation(0)
                    .WithArguments("MyDelegate", "Naem", "firstNaem"));
        }

        [Fact]
        public async Task DelegateParameterUnmeaningful_Verify_EmitsDiagnostic()
        {
            var source = "delegate void MyDelegate(string {|#0:a|});";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.DelegateParameterMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments("MyDelegate", "a"));
        }

        [Theory]
        [InlineData("class MyClass<TCorrect, {|#0:TWroong|}> { }", "MyClass<TCorrect, TWroong>", "Wroong", "TWroong")]
        [InlineData("struct MyStructure<{|#0:TWroong|}> { }", "MyStructure<TWroong>", "Wroong", "TWroong")]
        [InlineData("interface IInterface<{|#0:TWroong|}> { }", "IInterface<TWroong>", "Wroong", "TWroong")]
        [InlineData("delegate int MyDelegate<{|#0:TWroong|}>();", "MyDelegate<TWroong>", "Wroong", "TWroong")]

        public async Task TypeTypeParameterMisspelled_Verify_EmitsDiagnostic(string source, string typeName, string misspelling, string typeParameterName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeTypeParameterRule)
                    .WithLocation(0)
                    .WithArguments(typeName, misspelling, typeParameterName));
        }

        [Theory]
        [InlineData("class MyClass<{|#0:A|}> { }", "MyClass<A>", "A")]
        [InlineData("struct MyStructure<{|#0:B|}> { }", "MyStructure<B>", "B")]
        [InlineData("interface IInterface<{|#0:C|}> { }", "IInterface<C>", "C")]
        [InlineData("delegate int MyDelegate<{|#0:D|}>();", "MyDelegate<D>", "D")]
        public async Task TypeTypeParameterUnmeaningful_Verify_EmitsDiagnostic(string source, string typeName, string typeParameterName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeTypeParameterMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments(typeName, typeParameterName));
        }

        [Fact]
        public async Task MethodTypeParameterMisspelled_Verify_EmitsDiagnostic()
        {
            var source = @"
        class Program
        {
            void Method<{|#0:TTipe|}>(TTipe item)
            {
            }
        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MethodTypeParameterRule)
                    .WithLocation(0)
                    .WithArguments("Program.Method<TTipe>(TTipe)", "Tipe", "TTipe"));
        }

        [Fact]
        public async Task MethodTypeParameterUnmeaningful_Verify_EmitsDiagnostic()
        {
            var source = @"
        class Program
        {
            void Method<{|#0:TA|}>(TA parameter)
            {
            }
        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MethodTypeParameterMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments("Program.Method<TA>(TA)", "TA"));
        }

        [Fact]
        public async Task MisspellingContainsOnlyCapitalizedLetters_Verify_NoDiagnostics()
        {
            var source = "class FCCA { }";

            await VerifyCSharpAsync(source);
        }

        [Theory]
        [InlineData("0x0")]
        [InlineData("0xDEADBEEF")]
        public async Task MisspellingStartsWithADigit_Verify_NoDiagnostics(string misspelling)
        {
            var source = $"enum Name {{ My{misspelling} }}";

            await VerifyCSharpAsync(source);
        }

        [Fact]
        public async Task MalformedXmlDictionary_Verify_EmitsDiagnostic()
        {
            var contents = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <Dictionary>
            <Words>
                <Recognized>
                    <Word>okay</Word>
                    <word>bad</Word> <!-- xml tags are case-sensitive -->
                </Recognized>
            </Words>
        </Dictionary>";
            var dictionary = ("CodeAnalysisDictionary.xml", contents);

            await VerifyCSharpAsync(
                "class Program {}",
                dictionary,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.FileParseRule)
                .WithArguments(
                    "CodeAnalysisDictionary.xml",
                    "The 'word' start tag on line 6 position 22 does not match the end tag of 'Word'. Line 6, position 32."));
        }

        private Task VerifyCSharpAsync(string source, params DiagnosticResult[] expected)
            => VerifyCSharpAsync(source, Array.Empty<(string Path, string Text)>(), expected);

        private Task VerifyCSharpAsync(string source, (string Path, string Text) additionalText, params DiagnosticResult[] expected)
            => VerifyCSharpAsync(source, new[] { additionalText }, expected);

        private async Task VerifyCSharpAsync(string source, (string Path, string Text)[] additionalTexts, params DiagnosticResult[] expected)
        {

            var csharpTest = new VerifyCS.Test
            {
                TestCode = source,
                TestState =
                {
                    AdditionalFilesFactories = { () => additionalTexts.Select(x => (x.Path, SourceText.From(x.Text))) }
                },
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };

            csharpTest.ExpectedDiagnostics.AddRange(expected);

            await csharpTest.RunAsync();
        }

        private static (string Path, string Text) CreateXmlDictionary(IEnumerable<string> recognizedWords, IEnumerable<string> unrecognizedWords = null) =>
            CreateXmlDictionary("CodeAnalysisDictionary.xml", recognizedWords, unrecognizedWords);

        private static (string Path, string Text) CreateXmlDictionary(string filename, IEnumerable<string> recognizedWords, IEnumerable<string> unrecognizedWords = null)
        {
            var contents = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Dictionary>
    <Words>
        <Recognized>{CreateXml(recognizedWords)}</Recognized>
        <Unrecognized>{CreateXml(unrecognizedWords)}</Unrecognized>
    </Words>
</Dictionary>";

            return (filename, contents);

            static string CreateXml(IEnumerable<string> words) =>
                string.Join(Environment.NewLine, words?.Select(x => $"<Word>{x}</Word>") ?? Enumerable.Empty<string>());
        }

        private static (string Path, string Text) CreateDicDictionary(IEnumerable<string> recognizedWords)
        {
            var contents = string.Join(Environment.NewLine, recognizedWords);
            return ("CustomDictionary.dic", contents);
        }

        private static string CreateTypeWithConstructor(string typeName, string constructorName = "", string parameter = "", bool isStatic = false)
        {
            if (string.IsNullOrEmpty(constructorName))
            {
                constructorName = typeName;
            }

            return $@"
#pragma warning disable {IdentifiersShouldBeSpelledCorrectlyAnalyzer.RuleId}
class {typeName}
#pragma warning restore {IdentifiersShouldBeSpelledCorrectlyAnalyzer.RuleId}
{{
    {(isStatic ? "static " : string.Empty)}{constructorName}({parameter}) {{ }}
}}";
        }

        private static string CreateTypeWithMethod(string typeName, string methodName, string parameter = "")
            => $"class {typeName} {{ void {methodName}({parameter}) {{ }} }}";

        private static string CreateTypeWithIndexer(string typeName, string parameter)
            => $"class {typeName} {{ int this[{parameter}] => 0; }}";

        private static string CreateTypeWithProperty(string typeName, string propertyName)
            => $"class {typeName} {{ string {propertyName} {{ get; }} }}";

        private static string CreateTypeWithField(string typeName, string fieldName)
            => $"class {typeName} {{ private string {fieldName}; }}";

        private static string CreateTypeWithEvent(string typeName, string eventName)
        {
            return $@"using System;

class {typeName} {{ event EventHandler<string> {eventName}; }}";
        }
    }
}