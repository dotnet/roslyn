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

        [Fact(Skip = "Assembly names are disabled for now")]
        public async Task AssemblyMisspelled_Verify_EmitsDiagnostic()
        {
            var source = "class Program {}";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.AssemblyRule)
                    .WithArguments("Assambly", "MyAssambly"));
        }

        [Fact(Skip = "Unmeaningful rules disabled for now")]
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

        [Fact(Skip = "Unmeaningful rules disabled for now")]
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

        // [Theory]
        // [MemberData(nameof(MisspelledMemberParameters))]
        // public void MemberParameterMisspelled_Verify_EmitsDiagnostic(string source, string misspelling, string parameterName, string memberName)
        // {
        //     var testFile = new AutoTestFile(source, GetMemberParameterRule(memberName, misspelling, parameterName));

        //     VerifyDiagnostics(testFile);
        // }

        // [Fact(Skip = "Unmeaningful rules disabled for now")]
        // public void MemberParameterUnmeaningful_Verify_EmitsDiagnostic()
        // {
        //     var source = @"
        // class Program
        // {
        //     public void Method(string <?>a)
        //     {
        //     }

        //     public string this[int <?>i] => null;
        // }";
        //     var testFile = new AutoTestFile(
        //         source,
        //         GetMemberParameterUnmeaningfulRule("Program.Method(string)", "a"),
        //         GetMemberParameterUnmeaningfulRule("Program.this[int]", "i"));

        //     VerifyDiagnostics(testFile);
        // }

        // [Fact]
        // public void DelegateParameterMisspelled_Verify_EmitsDiagnostic()
        // {
        //     var testFile = new AutoTestFile(
        //         "delegate void MyDelegate(string <?>firstNaem);",
        //         GetDelegateParameterRule("MyDelegate", "Naem", "firstNaem"));

        //     VerifyDiagnostics(testFile);
        // }

        // [Fact(Skip = "Unmeaningful rules disabled for now")]
        // public void DelegateParameterUnmeaningful_Verify_EmitsDiagnostic()
        // {
        //     var testFile = new AutoTestFile(
        //         "delegate void MyDelegate(string <?>a);",
        //         GetDelegateParameterUnmeaningfulRule("MyDelegate", "a"));

        //     VerifyDiagnostics(testFile);
        // }

        // [Theory]
        // [InlineData("class MyClass<TCorrect, <?>TWroong> { }", "MyClass<TCorrect, TWroong>", "Wroong", "TWroong")]
        // [InlineData("struct MyStructure<<?>TWroong> { }", "MyStructure<TWroong>", "Wroong", "TWroong")]
        // [InlineData("interface IInterface<<?>TWroong> { }", "IInterface<TWroong>", "Wroong", "TWroong")]
        // [InlineData("delegate int MyDelegate<<?>TWroong>();", "MyDelegate<TWroong>", "Wroong", "TWroong")]

        // public void TypeTypeParameterMisspelled_Verify_EmitsDiagnostic(string source, string typeName, string misspelling, string typeParameterName)
        // {
        //     var testFile = new AutoTestFile(source, GetTypeTypeParameterRule(typeName, misspelling, typeParameterName));

        //     VerifyDiagnostics(testFile);
        // }

        // [Theory(Skip = "Unmeaningful rules disabled for now")]
        // [InlineData("class MyClass<<?>A> { }", "MyClass<A>", "A")]
        // [InlineData("struct MyStructure<<?>B> { }", "MyStructure<B>", "B")]
        // [InlineData("interface IInterface<<?>C> { }", "IInterface<C>", "C")]
        // [InlineData("delegate int MyDelegate<<?>D>();", "MyDelegate<D>", "D")]
        // public void TypeTypeParameterUnmeaningful_Verify_EmitsDiagnostic(string source, string typeName, string typeParameterName)
        // {
        //     var testFile = new AutoTestFile(source, GetTypeTypeParameterUnmeaningfulRule(typeName, typeParameterName));

        //     VerifyDiagnostics(testFile);
        // }

        // [Fact]
        // public void MethodTypeParameterMisspelled_Verify_EmitsDiagnostic()
        // {
        //     var source = @"
        // class Program
        // {
        //     void Method<<?>TTipe>(TTipe item)
        //     {
        //     }
        // }";
        //     var testFile = new AutoTestFile(source, GetMethodTypeParameterRule("Program.Method<TTipe>(TTipe)", "Tipe", "TTipe"));

        //     VerifyDiagnostics(testFile);
        // }

        // [Fact(Skip = "Unmeaningful rules disabled for now")]
        // public void MethodTypeParameterUnmeaningful_Verify_EmitsDiagnostic()
        // {
        //     var source = @"
        // class Program
        // {
        //     void Method<<?>TA>(TA parameter)
        //     {
        //     }
        // }";
        //     var testFile = new AutoTestFile(source, GetMethodTypeParameterUnmeaningfulRule("Program.Method<TA>(TA)", "TA"));

        //     VerifyDiagnostics(testFile);
        // }

        // [Fact]
        // public void MisspellingContainsOnlyCapitalizedLetters_Verify_NoDiagnostics()
        // {
        //     var testFile = new TestFile("class FCCA { }");

        //     VerifyDiagnostics(testFile);
        // }

        // [Theory]
        // [InlineData("0x0")]
        // [InlineData("0xDEADBEEF")]
        // public void MisspellingStartsWithADigit_Verify_NoDiagnostics(string misspelling)
        // {
        //     var testFile = new TestFile($"enum Name {{ My{misspelling} }}");

        //     VerifyDiagnostics(testFile);
        // }

        // [Fact]
        // public void MalformedXmlDictionary_Verify_EmitsDiagnostic()
        // {
        //     var contents = @"<?xml version=""1.0"" encoding=""utf-8""?>
        // <Dictionary>
        //     <Words>
        //         <Recognized>
        //             <Word>okay</Word>
        //             <word>bad</Word> <!-- xml tags are case-sensitive -->
        //         </Recognized>
        //     </Words>
        // </Dictionary>";
        //     var dictionary = new TestAdditionalDocument("CodeAnalysisDictionary.xml", contents);

        //     VerifyDiagnostics(
        //         new TestFile("class Program { }"),
        //         dictionary,
        //         GetFileParseResult(
        //             "CodeAnalysisDictionary.xml",
        //             "The 'word' start tag on line 6 position 14 does not match the end tag of 'Word'. Line 6, position 24."));
        // }

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