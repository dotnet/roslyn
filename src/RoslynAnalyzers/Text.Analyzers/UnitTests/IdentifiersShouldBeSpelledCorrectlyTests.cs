﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
        public async Task NoMisspellings_Verify_NoDiagnosticsAsync(string source)
        {
            await VerifyCSharpAsync(source);
        }

        [Fact]
        public async Task MisspellingAllowedByGlobalXmlDictionary_Verify_NoDiagnosticsAsync()
        {
            var source = "class Clazz { }";
            var dictionary = CreateXmlDictionary(new[] { "clazz" });

            await VerifyCSharpAsync(source, dictionary);
        }

        [Fact]
        public async Task MisspellingAllowedByGlobalDicDictionary_Verify_NoDiagnosticsAsync()
        {
            var source = "class Clazz { }";
            var dictionary = CreateDicDictionary(new[] { "clazz" });

            await VerifyCSharpAsync(source, dictionary);
        }

        [Fact]
        public async Task MisspellingsAllowedByMultipleGlobalDictionaries_Verify_NoDiagnosticsAsync()
        {
            var source = @"class Clazz { const string Naem = ""foo""; }";
            var xmlDictionary = CreateXmlDictionary(new[] { "clazz" });
            var dicDictionary = CreateDicDictionary(new[] { "naem" });

            await VerifyCSharpAsync(source, new[] { xmlDictionary, dicDictionary });
        }

        [Fact]
        public async Task CorrectWordDisallowedByGlobalXmlDictionary_Verify_EmitsDiagnosticAsync()
        {
            var source = "class {|#0:Program|} { }";
            var dictionary = CreateXmlDictionary(null, new[] { "program" });

            await VerifyCSharpAsync(
                source,
                dictionary,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeRule)
                    .WithLocation(0)
                    .WithArguments("Program", "Program"));
        }

        [Fact]
        public async Task MisspellingAllowedByProjectDictionary_Verify_NoDiagnosticsAsync()
        {
            var source = "class Clazz {}";
            var dictionary = CreateDicDictionary(new[] { "clazz" });

            await VerifyCSharpAsync(source, dictionary);
        }

        [Fact]
        public async Task MisspellingAllowedByDifferentProjectDictionary_Verify_EmitsDiagnosticAsync()
        {
            var source = "class {|#0:Clazz|} {}";
            var dictionary = CreateDicDictionary(new[] { "clazz" });
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalProjects =
                    {
                        ["OtherProject"] =
                        {
                            AdditionalFiles = { dictionary }
                        },
                    },
                    AdditionalProjectReferences = { "OtherProject" }
                },
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeRule)
                        .WithLocation(0)
                        .WithArguments("Clazz", "Clazz")
                }
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task AssemblyMisspelled_Verify_EmitsDiagnosticAsync()
        {
            var source = "{|#0:class Program {}|}";
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                SolutionTransforms =
                {
                    RenameProjectAssembly,
                },
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.AssemblyRule)
                        .WithLocation(0)
                        .WithArguments("Assambly", "MyAssambly")
                }
            };

            await test.RunAsync();

            static Solution RenameProjectAssembly(Solution solution, ProjectId projectId)
            {
                return solution.WithProjectAssemblyName(projectId, "MyAssambly");
            }
        }

        [Fact]
        public async Task AssemblyUnmeaningful_Verify_EmitsDiagnosticAsync()
        {
            var source = "{|#0:class Program {}|}";
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                SolutionTransforms =
                {
                    RenameProjectAssembly,
                },
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.AssemblyMoreMeaningfulNameRule)
                        .WithLocation(0)
                        .WithArguments("A")
                }
            };

            await test.RunAsync();

            static Solution RenameProjectAssembly(Solution solution, ProjectId projectId)
            {
                return solution.WithProjectAssemblyName(projectId, "A");
            }
        }

        [Fact]
        public async Task NamespaceMisspelled_Verify_EmitsDiagnosticAsync()
        {
            var source = "namespace Tests.{|#0:MyNarmspace|} {}";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.NamespaceRule)
                    .WithLocation(0)
                    .WithArguments("Narmspace", "Tests.MyNarmspace"));
        }

        [Fact]
        public async Task NamespaceUnmeaningful_Verify_EmitsDiagnosticAsync()
        {
            var source = "namespace Tests.{|#0:A|} {}";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.NamespaceMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments("A"));
        }

        [Theory]
        [InlineData("namespace MyNamespace { class {|#0:MyClazz|} {} }", "Clazz", "MyNamespace.MyClazz")]
        [InlineData("namespace MyNamespace { struct {|#0:MyStroct|} {} }", "Stroct", "MyNamespace.MyStroct")]
        [InlineData("namespace MyNamespace { enum {|#0:MyEnim|} {} }", "Enim", "MyNamespace.MyEnim")]
        [InlineData("namespace MyNamespace { interface {|#0:IMyFase|} {} }", "Fase", "MyNamespace.IMyFase")]
        [InlineData("namespace MyNamespace { delegate int {|#0:MyDelegete|}(); }", "Delegete", "MyNamespace.MyDelegete")]
        public async Task TypeMisspelled_Verify_EmitsDiagnosticAsync(string source, string misspelling, string typeName)
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
        public async Task TypeUnmeaningful_Verify_EmitsDiagnosticAsync(string source, string typeName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments(typeName));
        }

        [Theory]
        [MemberData(nameof(MisspelledMembers))]
        public async Task MemberMisspelled_Verify_EmitsDiagnosticAsync(string source, string misspelling, string memberName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberRule)
                    .WithLocation(0)
                    .WithArguments(misspelling, memberName));
        }

        [Fact]
        public async Task MemberOverrideMisspelled_Verify_EmitsDiagnosticOnlyAtDefinitionAsync()
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
        public async Task MemberUnmeaningful_Verify_EmitsDiagnosticAsync(string source, string memberName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments(memberName));
        }

        [Fact]
        public async Task VariableMisspelled_Verify_EmitsDiagnosticAsync()
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
        public async Task MemberParameterMisspelled_Verify_EmitsDiagnosticAsync(string source, string misspelling, string parameterName, string memberName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(0)
                    .WithArguments(memberName, misspelling, parameterName));
        }

        [Fact]
        public async Task MemberParameterUnmeaningful_Verify_EmitsDiagnosticAsync()
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
        public async Task MemberParameterMisspelledInterfaceImplementation_Verify_EmitsDiagnosticOnlyAtDefinitionAsync()
        {
            var source = @"
        interface IProgram
        {
            void Method(string {|#0:explaintain|});

            string this[int {|#1:indxe|}] { get; }
        }

        class Program : IProgram
        {
            public void Method(string explaintain)
            {
            }

            public string this[int indxe] => null;

            public void Method2(long {|#2:enviromentId|})
            {
            }

        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(0)
                    .WithArguments("IProgram.Method(string)", "explaintain", "explaintain"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(1)
                    .WithArguments("IProgram.this[int]", "indxe", "indxe"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(2)
                    .WithArguments("Program.Method2(long)", "enviroment", "enviromentId"));
        }

        [Fact]
        public async Task MemberParameterUnmeaningfulInterfaceImplementation_Verify_EmitsDiagnosticOnlyAtDefinitionAsync()
        {
            var source = @"
        interface IProgram
        {
            void Method(string {|#0:a|});

            string this[int {|#1:i|}] { get; }
        }

        class Program : IProgram
        {
            public void Method(string a)
            {
            }

            public string this[int i] => null;

            public void Method2(long {|#2:x|})
            {
            }

        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments("IProgram.Method(string)", "a"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterMoreMeaningfulNameRule)
                    .WithLocation(1)
                    .WithArguments("IProgram.this[int]", "i"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterMoreMeaningfulNameRule)
                    .WithLocation(2)
                    .WithArguments("Program.Method2(long)", "x"));
        }

        [Fact]
        public async Task MemberParameterUnmeaningfulExplicitInterfaceImplementation_Verify_EmitsDiagnosticOnlyAtDefinitionAsync()
        {
            var source = @"
        interface IProgram
        {
            void Method(string {|#0:a|});

            string this[int {|#1:i|}] { get; }
        }

        class Program : IProgram
        {
            void IProgram.Method(string a)
            {
            }

            string IProgram.this[int i] => null;

            public void Method2(long {|#2:x|})
            {
            }

        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments("IProgram.Method(string)", "a"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterMoreMeaningfulNameRule)
                    .WithLocation(1)
                    .WithArguments("IProgram.this[int]", "i"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterMoreMeaningfulNameRule)
                    .WithLocation(2)
                    .WithArguments("Program.Method2(long)", "x"));
        }

        [Fact]
        public async Task MemberParameterMisspelledExplicitInterfaceImplementation_Verify_EmitsDiagnosticOnlyAtDefinitionAsync()
        {
            var source = @"
        interface IProgram
        {
            void Method(string {|#0:explaintain|});

            string this[int {|#1:indxe|}] { get; }
        }

        class Program : IProgram
        {
            void IProgram.Method(string explaintain)
            {
            }

            string IProgram.this[int indxe] => null;

            public void Method2(long {|#2:enviromentId|})
            {
            }

        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(0)
                    .WithArguments("IProgram.Method(string)", "explaintain", "explaintain"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(1)
                    .WithArguments("IProgram.this[int]", "indxe", "indxe"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(2)
                    .WithArguments("Program.Method2(long)", "enviroment", "enviromentId"));
        }

        [Fact]
        public async Task MemberParameterMisspelledOverrideImplementation_Verify_EmitsDiagnosticOnlyAtDefinitionAsync()
        {
            var source = @"
        public abstract class Base
        {
            public abstract void Method(string {|#0:explaintain|});
        }

        public class Derived : Base
        {
            public override void Method(string explaintain)
            {
            }

            public void Method2(long {|#1:enviromentId|})
            {
            }

        }";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(0)
                    .WithArguments("Base.Method(string)", "explaintain", "explaintain"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(1)
                    .WithArguments("Derived.Method2(long)", "enviroment", "enviromentId"));
        }

        [Fact]
        public async Task MemberParameterMisspelledOverrideImplementationWithNameMismatch_Verify_EmitsDiagnosticOnlyAtDefinitionAsync()
        {
            var source = @"
        public abstract class Base
        {
            public abstract void Method(string {|#0:explaintain|});
            public abstract void Method2(string {|#1:inupts|});
        }

        public class Mid : Base
        {
            public override void Method(string explaintain)
            {
            }

            public override void Method2(string {|#2:paarmeter|})
            {
            }
        }

        public class Derived : Mid
        {
            public override void Method(string explanation)
            {
            }

            public override void Method2(string {|#3:paarmeterId|})
            {
            }

        }

        public class Derived2 : Mid
        {
            public override void Method(string {|#4:explaintaing|})
            {
            }

            public override void Method2(string {|#5:strValue|})
            {
            }

        }

";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(0)
                    .WithArguments("Base.Method(string)", "explaintain", "explaintain"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(1)
                    .WithArguments("Base.Method2(string)", "inupts", "inupts"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(2)
                    .WithArguments("Mid.Method2(string)", "paarmeter", "paarmeter"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(3)
                    .WithArguments("Derived.Method2(string)", "paarmeter", "paarmeterId"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(4)
                    .WithArguments("Derived2.Method(string)", "explaintaing", "explaintaing"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(5)
                    .WithArguments("Derived2.Method2(string)", "str", "strValue"));
        }

        [Fact]
        public async Task MemberParameterMisspelledIndexerOVerrideWithNameMismatch_Verify_EmitsDiagnosticOnlyAtDefinitionAsync()
        {
            var source = @"
        public interface IProgram
        {
            string this[int {|#0:indxe|}] { get; }
        }

        public class Program : IProgram
        {
            public virtual string this[int indxe] => null;
        }

        public class DerivedProgram : Program
        {
            public override string this[int indxe] => null;
        }

        public class DerivedProgram2 : Program
        {
            public override string this[int {|#1:indexe|}] => null;
        }

";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(0)
                    .WithArguments("IProgram.this[int]", "indxe", "indxe"),
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.MemberParameterRule)
                    .WithLocation(1)
                    .WithArguments("DerivedProgram2.this[int]", "indexe", "indexe"));
        }

        [Fact]
        public async Task DelegateParameterMisspelled_Verify_EmitsDiagnosticAsync()
        {
            var source = "delegate void MyDelegate(string {|#0:firstNaem|});";

            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.DelegateParameterRule)
                    .WithLocation(0)
                    .WithArguments("MyDelegate", "Naem", "firstNaem"));
        }

        [Fact]
        public async Task DelegateParameterUnmeaningful_Verify_EmitsDiagnosticAsync()
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

        public async Task TypeTypeParameterMisspelled_Verify_EmitsDiagnosticAsync(string source, string typeName, string misspelling, string typeParameterName)
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
        public async Task TypeTypeParameterUnmeaningful_Verify_EmitsDiagnosticAsync(string source, string typeName, string typeParameterName)
        {
            await VerifyCSharpAsync(
                source,
                VerifyCS.Diagnostic(IdentifiersShouldBeSpelledCorrectlyAnalyzer.TypeTypeParameterMoreMeaningfulNameRule)
                    .WithLocation(0)
                    .WithArguments(typeName, typeParameterName));
        }

        [Fact]
        public async Task MethodTypeParameterMisspelled_Verify_EmitsDiagnosticAsync()
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
        public async Task MethodTypeParameterUnmeaningful_Verify_EmitsDiagnosticAsync()
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
        public async Task MisspellingContainsOnlyCapitalizedLetters_Verify_NoDiagnosticsAsync()
        {
            var source = "class FCCA { }";

            await VerifyCSharpAsync(source);
        }

        [Theory]
        [InlineData("0x0")]
        [InlineData("0xDEADBEEF")]
        public async Task MisspellingStartsWithADigit_Verify_NoDiagnosticsAsync(string misspelling)
        {
            var source = $"enum Name {{ My{misspelling} }}";

            await VerifyCSharpAsync(source);
        }

        [Fact]
        public async Task MalformedXmlDictionary_Verify_EmitsDiagnosticAsync()
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
                    // Ignore diagnostic message comparison because the actual message
                    // includes localized content from an exception's message
                    .WithMessage(null));
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

        private static (string Path, string Text) CreateXmlDictionary(IEnumerable<string>? recognizedWords, IEnumerable<string>? unrecognizedWords = null)
            => CreateXmlDictionary("CodeAnalysisDictionary.xml", recognizedWords, unrecognizedWords);

        private static (string Path, string Text) CreateXmlDictionary(string filename, IEnumerable<string>? recognizedWords, IEnumerable<string>? unrecognizedWords = null)
        {
            var contents = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Dictionary>
    <Words>
        <Recognized>{CreateXml(recognizedWords)}</Recognized>
        <Unrecognized>{CreateXml(unrecognizedWords)}</Unrecognized>
    </Words>
</Dictionary>";

            return (filename, contents);

            static string CreateXml(IEnumerable<string>? words) =>
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
            => $@"using System;

class {typeName} {{ event EventHandler<string> {eventName}; }}";
    }
}
