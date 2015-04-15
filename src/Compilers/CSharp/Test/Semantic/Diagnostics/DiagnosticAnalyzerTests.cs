// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticAnalyzerTests : CompilingTestBase
    {
        [Serializable]
        private class TestDiagnostic : Diagnostic, ISerializable
        {
            private readonly string _kind;
            private readonly DiagnosticSeverity _severity;
            private readonly Location _location;
            private readonly string _message;
            private readonly object[] _arguments;
            private readonly DiagnosticDescriptor _descriptor;
            private static readonly Location[] s_emptyLocations = new Location[0];

            public TestDiagnostic(string id, string kind, DiagnosticSeverity severity, Location location, string message, params object[] arguments)
                : this(new DiagnosticDescriptor(id, string.Empty, message, id, severity, isEnabledByDefault: true), kind, severity, location, message, arguments)
            {
            }

            public TestDiagnostic(DiagnosticDescriptor descriptor, string kind, DiagnosticSeverity severity, Location location, string message, params object[] arguments)
            {
                _descriptor = descriptor;
                _kind = kind;
                _severity = severity;
                _location = location;
                _message = message;
                _arguments = arguments;
            }

            public override IReadOnlyList<Location> AdditionalLocations { get { return s_emptyLocations; } }

            public override string Id { get { return _descriptor.Id; } }

            public override DiagnosticDescriptor Descriptor { get { return _descriptor; } }

            public override Location Location { get { return _location; } }

            internal override IReadOnlyList<object> Arguments { get { return _arguments; } }

            public override DiagnosticSeverity Severity { get { return _severity; } }

            public override DiagnosticSeverity DefaultSeverity { get { return _descriptor.DefaultSeverity; } }

            public override int WarningLevel { get { return 2; } }

            public override int GetHashCode()
            {
                return Hash.Combine(_descriptor.Id.GetHashCode(), _kind.GetHashCode());
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as TestDiagnostic);
            }

            public override bool Equals(Diagnostic obj)
            {
                return Equals(obj as TestDiagnostic);
            }

            public bool Equals(TestDiagnostic other)
            {
                if (other == null || this.GetType() != other.GetType()) return false;
                return
                    _descriptor.Id == other._descriptor.Id &&
                    _kind == other._kind &&
                    _location == other._location &&
                    _message == other._message &&
                    SameData(_arguments, other._arguments);
            }

            private static bool SameData(object[] d1, object[] d2)
            {
                return (d1 == null) == (d2 == null) && (d1 == null || d1.SequenceEqual(d2));
            }

            public override string GetMessage(IFormatProvider formatProvider = null)
            {
                return string.Format(_message, _arguments);
            }

            private TestDiagnostic(SerializationInfo info, StreamingContext context)
            {
                var id = info.GetString("id");
                _kind = info.GetString("kind");
                _message = info.GetString("message");
                _location = (Location)info.GetValue("location", typeof(Location));
                _severity = (DiagnosticSeverity)info.GetValue("severity", typeof(DiagnosticSeverity));
                var defaultSeverity = (DiagnosticSeverity)info.GetValue("defaultSeverity", typeof(DiagnosticSeverity));
                _arguments = (object[])info.GetValue("arguments", typeof(object[]));
                _descriptor = new DiagnosticDescriptor(id, string.Empty, _message, id, defaultSeverity, isEnabledByDefault: true);
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("id", _descriptor.Id);
                info.AddValue("kind", _kind);
                info.AddValue("message", _message);
                info.AddValue("location", _location, typeof(Location));
                info.AddValue("severity", _severity, typeof(DiagnosticSeverity));
                info.AddValue("defaultSeverity", _descriptor.DefaultSeverity, typeof(DiagnosticSeverity));
                info.AddValue("arguments", _arguments, typeof(object[]));
            }

            internal override Diagnostic WithLocation(Location location)
            {
                // We do not implement "additional locations"
                throw new NotImplementedException();
            }

            internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                return new TestDiagnostic(_descriptor, _kind, severity, _location, _message, _arguments);
            }
        }

        private class ComplainAboutX : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor s_CA9999_UseOfVariableThatStartsWithX =
                new DiagnosticDescriptor(id: "CA9999_UseOfVariableThatStartsWithX", title: "CA9999_UseOfVariableThatStartsWithX", messageFormat: "Use of variable whose name starts with 'x': '{0}'", category: "Test", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_CA9999_UseOfVariableThatStartsWithX);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IdentifierName);
            }

            private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                var id = (IdentifierNameSyntax)context.Node;
                if (id.Identifier.ValueText.StartsWith("x", StringComparison.Ordinal))
                {
                    context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_CA9999_UseOfVariableThatStartsWithX, id.Location, id.Identifier.ValueText));
                }
            }
        }

        [WorkItem(892467, "DevDiv")]
        [Fact]
        public void SimplestDiagnosticAnalyzerTest()
        {
            string source =
@"public class C : NotFound
{
    int x1(int x2)
    {
        int x3 = x1(x2);
        return x3 + 1;
    }
}";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics(
                // (1,18): error CS0246: The type or namespace name 'NotFound' could not be found (are you missing a using directive or an assembly reference?)
                // public class C : NotFound
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NotFound").WithArguments("NotFound")
                )
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new ComplainAboutX() }, null, null, false,
                // (5,18): warning CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x1'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x1").WithArguments("x1"),
                // (5,21): warning CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x2'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x2").WithArguments("x2"),
                // (6,16): warning CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x3'
                //         return x3 + 1;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x3").WithArguments("x3")
                );
        }

        [WorkItem(892467, "DevDiv")]
        [Fact]
        public void SimplestDiagnosticAnalyzerTestInInitializer()
        {
            string source =
@"delegate int D(out int x);
public class C : NotFound
{
    static int x1 = 2;
    static int x2 = 3;
    int x3 = x1 + x2;
    D d1 = (out int x4) => (x4 = 1) + @x4;
}";
            // TODO: Compilation create doesn't accept analyzers anymore.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (2,18): error CS0246: The type or namespace name 'NotFound' could not be found (are you missing a using directive or an assembly reference?)
                // public class C : NotFound
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NotFound").WithArguments("NotFound")
                )
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new ComplainAboutX() }, null, null, false,
                // (6,14): warning CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x1'
                //     int x3 = x1 + x2;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x1").WithArguments("x1"),
                // (6,19): warning CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x2'
                //     int x3 = x1 + x2;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x2").WithArguments("x2"),
                // (7,29): warning CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x4'
                //     D d1 = (out int x4) => (x4 = 1) + @x4;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x4").WithArguments("x4"),
                // (7,39): warning CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x4'
                //     D d1 = (out int x4) => (x4 = 1) + @x4;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "@x4").WithArguments("x4")
                );
        }

        [WorkItem(892467, "DevDiv")]
        [Fact]
        public void DiagnosticAnalyzerSuppressDiagnostic()
        {
            string source = @"
public class C : NotFound
{
    int x1(int x2)
    {
        int x3 = x1(x2);
        return x3 + 1;
    }
}";
            // TODO: Compilation create doesn't accept analyzers anymore.
            var options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(
                new[] { KeyValuePair.Create("CA9999_UseOfVariableThatStartsWithX", ReportDiagnostic.Suppress) });

            CreateCompilationWithMscorlib45(source, options: options/*, analyzers: new IDiagnosticAnalyzerFactory[] { new ComplainAboutX() }*/).VerifyDiagnostics(
                // (2,18): error CS0246: The type or namespace name 'NotFound' could not be found (are you missing a using directive or an assembly reference?)
                // public class C : NotFound
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NotFound").WithArguments("NotFound"));
        }

        [WorkItem(892467, "DevDiv")]
        [Fact]
        public void DiagnosticAnalyzerWarnAsError()
        {
            string source = @"
public class C : NotFound
{
    int x1(int x2)
    {
        int x3 = x1(x2);
        return x3 + 1;
    }
}";
            // TODO: Compilation create doesn't accept analyzers anymore.
            var options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(
                new[] { KeyValuePair.Create("CA9999_UseOfVariableThatStartsWithX", ReportDiagnostic.Error) });

            CreateCompilationWithMscorlib45(source, options: options).VerifyDiagnostics(
                // (2,18): error CS0246: The type or namespace name 'NotFound' could not be found (are you missing a using directive or an assembly reference?)
                // public class C : NotFound
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NotFound").WithArguments("NotFound"))
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new ComplainAboutX() }, null, null, false,
                // (6,18): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x1'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x1").WithArguments("x1").WithWarningAsError(true),
                // (6,21): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x2'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x2").WithArguments("x2").WithWarningAsError(true),
                // (7,16): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x3'
                //         return x3 + 1;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x3").WithArguments("x3").WithWarningAsError(true)
                );
        }

        [WorkItem(892467, "DevDiv")]
        [Fact]
        public void DiagnosticAnalyzerWarnAsErrorGlobal()
        {
            string source = @"
public class C : NotFound
{
    int x1(int x2)
    {
        int x3 = x1(x2);
        return x3 + 1;
    }
}";
            var options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Error);

            CreateCompilationWithMscorlib45(source, options: options).VerifyDiagnostics(
                // (2,18): error CS0246: The type or namespace name 'NotFound' could not be found (are you missing a using directive or an assembly reference?)
                // public class C : NotFound
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NotFound").WithArguments("NotFound")
                )
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new ComplainAboutX() }, null, null, false,
                // (6,18): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x1'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x1").WithArguments("x1").WithWarningAsError(true),
                // (6,21): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x2'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x2").WithArguments("x2").WithWarningAsError(true),
                // (7,16): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x3'
                //         return x3 + 1;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x3").WithArguments("x3").WithWarningAsError(true));
        }

        [Fact, WorkItem(1038025)]
        public void TestImplicitlyDeclaredSymbolsNotAnalyzed()
        {
            string source = @"
using System;
public class C
{
    public event EventHandler e;
}";
            CreateCompilationWithMscorlib45(source)
                .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new ImplicitlyDeclaredSymbolAnalyzer() });
        }

        private class SyntaxAndSymbolAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor("XX0001", "My Syntax/Symbol Diagnostic", "My Syntax/Symbol Diagnostic for '{0}'", "Compiler", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_descriptor);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.Attribute, SyntaxKind.ClassDeclaration, SyntaxKind.UsingDirective);
                context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            }

            private void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                switch (context.Node.Kind())
                {
                    case SyntaxKind.Attribute:
                        var diag1 = CodeAnalysis.Diagnostic.Create(s_descriptor, context.Node.GetLocation(), "Attribute");
                        context.ReportDiagnostic(diag1);
                        break;

                    case SyntaxKind.ClassDeclaration:
                        var diag2 = CodeAnalysis.Diagnostic.Create(s_descriptor, context.Node.GetLocation(), "ClassDeclaration");
                        context.ReportDiagnostic(diag2);
                        break;

                    case SyntaxKind.UsingDirective:
                        var diag3 = CodeAnalysis.Diagnostic.Create(s_descriptor, context.Node.GetLocation(), "UsingDirective");
                        context.ReportDiagnostic(diag3);
                        break;
                }
            }

            private void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                var diag1 = CodeAnalysis.Diagnostic.Create(s_descriptor, context.Symbol.Locations[0], "NamedType");
                context.ReportDiagnostic(diag1);
            }
        }

        [WorkItem(914236, "DevDiv")]
        [Fact]
        public void DiagnosticAnalyzerSyntaxNodeAndSymbolAnalysis()
        {
            string source = @"
using System;

[Obsolete]
public class C { }";
            var options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Error);

            CreateCompilationWithMscorlib45(source, options: options)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new SyntaxAndSymbolAnalyzer() }, null, null, false,
                    // Symbol diagnostics
                    Diagnostic("XX0001", "C").WithArguments("NamedType").WithWarningAsError(true),
                    // Syntax diagnostics
                    Diagnostic("XX0001", "using System;").WithArguments("UsingDirective").WithWarningAsError(true), // using directive
                    Diagnostic("XX0001", "Obsolete").WithArguments("Attribute").WithWarningAsError(true), // attribute syntax
                    Diagnostic("XX0001", @"[Obsolete]
public class C { }").WithArguments("ClassDeclaration").WithWarningAsError(true)); // class declaration
        }
        [Fact]

        private void TestGetEffectiveDiagnostics()
        {
            var noneDiagDesciptor = new DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Hidden, isEnabledByDefault: true);
            var infoDiagDesciptor = new DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info, isEnabledByDefault: true);
            var warningDiagDesciptor = new DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            var errorDiagDesciptor = new DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Error, isEnabledByDefault: true);

            var noneDiag = CodeAnalysis.Diagnostic.Create(noneDiagDesciptor, Location.None);
            var infoDiag = CodeAnalysis.Diagnostic.Create(infoDiagDesciptor, Location.None);
            var warningDiag = CodeAnalysis.Diagnostic.Create(warningDiagDesciptor, Location.None);
            var errorDiag = CodeAnalysis.Diagnostic.Create(errorDiagDesciptor, Location.None);

            var diags = new[] { noneDiag, infoDiag, warningDiag, errorDiag };

            // Escalate all diagnostics to error.
            var specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(noneDiagDesciptor.Id, ReportDiagnostic.Error);
            specificDiagOptions.Add(infoDiagDesciptor.Id, ReportDiagnostic.Error);
            specificDiagOptions.Add(warningDiagDesciptor.Id, ReportDiagnostic.Error);
            var options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions);

            var comp = CreateCompilationWithMscorlib45("", options: options);
            var effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(diags.Length, effectiveDiags.Length);
            foreach (var effectiveDiag in effectiveDiags)
            {
                Assert.True(effectiveDiag.Severity == DiagnosticSeverity.Error);
            }

            // Suppress all diagnostics.
            specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(noneDiagDesciptor.Id, ReportDiagnostic.Suppress);
            specificDiagOptions.Add(infoDiagDesciptor.Id, ReportDiagnostic.Suppress);
            specificDiagOptions.Add(warningDiagDesciptor.Id, ReportDiagnostic.Suppress);
            specificDiagOptions.Add(errorDiagDesciptor.Id, ReportDiagnostic.Suppress);
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions);

            comp = CreateCompilationWithMscorlib45("", options: options);
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(0, effectiveDiags.Length);

            // Shuffle diagnostic severity.
            specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(noneDiagDesciptor.Id, ReportDiagnostic.Info);
            specificDiagOptions.Add(infoDiagDesciptor.Id, ReportDiagnostic.Hidden);
            specificDiagOptions.Add(warningDiagDesciptor.Id, ReportDiagnostic.Error);
            specificDiagOptions.Add(errorDiagDesciptor.Id, ReportDiagnostic.Warn);
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions);

            comp = CreateCompilationWithMscorlib45("", options: options);
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(diags.Length, effectiveDiags.Length);
            var diagIds = new HashSet<string>(diags.Select(d => d.Id));
            foreach (var effectiveDiag in effectiveDiags)
            {
                Assert.True(diagIds.Remove(effectiveDiag.Id));

                switch (effectiveDiag.Severity)
                {
                    case DiagnosticSeverity.Hidden:
                        Assert.Equal(infoDiagDesciptor.Id, effectiveDiag.Id);
                        break;

                    case DiagnosticSeverity.Info:
                        Assert.Equal(noneDiagDesciptor.Id, effectiveDiag.Id);
                        break;

                    case DiagnosticSeverity.Warning:
                        Assert.Equal(errorDiagDesciptor.Id, effectiveDiag.Id);
                        break;

                    case DiagnosticSeverity.Error:
                        Assert.Equal(warningDiagDesciptor.Id, effectiveDiag.Id);
                        break;

                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            Assert.Empty(diagIds);
        }

        [Fact]
        public void TestGetEffectiveDiagnosticsGlobal()
        {
            var noneDiagDesciptor = new DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Hidden, isEnabledByDefault: true);
            var infoDiagDesciptor = new DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info, isEnabledByDefault: true);
            var warningDiagDesciptor = new DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            var errorDiagDesciptor = new DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Error, isEnabledByDefault: true);

            var noneDiag = Microsoft.CodeAnalysis.Diagnostic.Create(noneDiagDesciptor, Location.None);
            var infoDiag = Microsoft.CodeAnalysis.Diagnostic.Create(infoDiagDesciptor, Location.None);
            var warningDiag = Microsoft.CodeAnalysis.Diagnostic.Create(warningDiagDesciptor, Location.None);
            var errorDiag = Microsoft.CodeAnalysis.Diagnostic.Create(errorDiagDesciptor, Location.None);

            var diags = new[] { noneDiag, infoDiag, warningDiag, errorDiag };

            var options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Default);
            var comp = CreateCompilationWithMscorlib45("", options: options);
            var effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(4, effectiveDiags.Length);

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            comp = CreateCompilationWithMscorlib45("", options: options);
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(4, effectiveDiags.Length);
            Assert.Equal(1, effectiveDiags.Count(d => d.IsWarningAsError));

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Warn);
            comp = CreateCompilationWithMscorlib45("", options: options);
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(4, effectiveDiags.Length);
            Assert.Equal(1, effectiveDiags.Count(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Equal(1, effectiveDiags.Count(d => d.Severity == DiagnosticSeverity.Warning));

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Info);
            comp = CreateCompilationWithMscorlib45("", options: options);
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(4, effectiveDiags.Length);
            Assert.Equal(1, effectiveDiags.Count(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Equal(1, effectiveDiags.Count(d => d.Severity == DiagnosticSeverity.Info));

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Hidden);
            comp = CreateCompilationWithMscorlib45("", options: options);
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(4, effectiveDiags.Length);
            Assert.Equal(1, effectiveDiags.Count(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Equal(1, effectiveDiags.Count(d => d.Severity == DiagnosticSeverity.Hidden));

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress);
            comp = CreateCompilationWithMscorlib45("", options: options);
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(2, effectiveDiags.Length);
            Assert.Equal(1, effectiveDiags.Count(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Equal(1, effectiveDiags.Count(d => d.Severity == DiagnosticSeverity.Hidden));
        }
        [Fact]

        private void TestDisabledDiagnostics()
        {
            var disabledDiagDescriptor = new DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: false);
            var enabledDiagDescriptor = new DiagnosticDescriptor("XX002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            var disabledDiag = CodeAnalysis.Diagnostic.Create(disabledDiagDescriptor, Location.None);
            var enabledDiag = CodeAnalysis.Diagnostic.Create(enabledDiagDescriptor, Location.None);

            var diags = new[] { disabledDiag, enabledDiag };

            // Verify that only the enabled diag shows up after filtering.
            var options = TestOptions.ReleaseDll;
            var comp = CreateCompilationWithMscorlib45("", options: options);
            var effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(1, effectiveDiags.Length);
            Assert.Contains(enabledDiag, effectiveDiags);

            // If the disabled diag was enabled through options, then it should show up.
            var specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(disabledDiagDescriptor.Id, ReportDiagnostic.Warn);
            specificDiagOptions.Add(enabledDiagDescriptor.Id, ReportDiagnostic.Suppress);

            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions);
            comp = CreateCompilationWithMscorlib45("", options: options);
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(1, effectiveDiags.Length);
            Assert.Contains(disabledDiag, effectiveDiags);
        }

        internal class FullyDisabledAnalyzer : DiagnosticAnalyzer
        {
            public static DiagnosticDescriptor desc1 = new DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: false);
            public static DiagnosticDescriptor desc2 = new DiagnosticDescriptor("XX002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: false);
            public static DiagnosticDescriptor desc3 = new DiagnosticDescriptor("XX003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: false, customTags: WellKnownDiagnosticTags.NotConfigurable);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(desc1, desc2, desc3); }
            }

            public override void Initialize(AnalysisContext context)
            {
            }
        }

        internal class PartiallyDisabledAnalyzer : DiagnosticAnalyzer
        {
            public static DiagnosticDescriptor desc1 = new DiagnosticDescriptor("XX003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: false);
            public static DiagnosticDescriptor desc2 = new DiagnosticDescriptor("XX004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(desc1, desc2); }
            }

            public override void Initialize(AnalysisContext context)
            {
            }
        }

        internal class ImplicitlyDeclaredSymbolAnalyzer : DiagnosticAnalyzer
        {
            public static DiagnosticDescriptor desc1 = new DiagnosticDescriptor("DummyId", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: false);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(desc1); }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(
                    (c) =>
                    {
                        Assert.False(c.Symbol.IsImplicitlyDeclared);
                    },
                    SymbolKind.Namespace, SymbolKind.NamedType, SymbolKind.Event, SymbolKind.Field, SymbolKind.Method, SymbolKind.Property);
            }
        }

        [Fact]
        private void TestDisabledAnalyzers()
        {
            var fullyDisabledAnalyzer = new FullyDisabledAnalyzer();
            var partiallyDisabledAnalyzer = new PartiallyDisabledAnalyzer();

            var options = TestOptions.ReleaseDll;
            Assert.True(fullyDisabledAnalyzer.IsDiagnosticAnalyzerSuppressed(options));
            Assert.False(partiallyDisabledAnalyzer.IsDiagnosticAnalyzerSuppressed(options));

            var specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(FullyDisabledAnalyzer.desc1.Id, ReportDiagnostic.Warn);
            specificDiagOptions.Add(PartiallyDisabledAnalyzer.desc2.Id, ReportDiagnostic.Suppress);

            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions);
            Assert.False(fullyDisabledAnalyzer.IsDiagnosticAnalyzerSuppressed(options));
            Assert.True(partiallyDisabledAnalyzer.IsDiagnosticAnalyzerSuppressed(options));

            // Verify not configurable disabled diagnostic cannot be enabled, and hence cannot affect IsDiagnosticAnalyzerSuppressed computation.
            specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(FullyDisabledAnalyzer.desc3.Id, ReportDiagnostic.Warn);
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions);
            Assert.True(fullyDisabledAnalyzer.IsDiagnosticAnalyzerSuppressed(options));
        }

        [Fact, WorkItem(1008059)]
        private void TestCodeBlockAnalyzersForNoExecutableCode()
        {
            string noExecutableCodeSource = @"
public abstract class C
{
    public int P { get; set; }
    public int field;
    public abstract int Method();
}";
            var analyzers = new DiagnosticAnalyzer[] { new CodeBlockOrSyntaxNodeAnalyzer(isCodeBlockAnalyzer: true) };

            CreateCompilationWithMscorlib45(noExecutableCodeSource)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers);
        }

        [Fact, WorkItem(1008059)]
        private void TestCodeBlockAnalyzersForBaseConstructorInitializer()
        {
            string baseCtorSource = @"
public class B
{
    public B(int x) {}
}

public class C : B
{
    public C() : base(x: 10) {}
}";
            var analyzers = new DiagnosticAnalyzer[] { new CodeBlockOrSyntaxNodeAnalyzer(isCodeBlockAnalyzer: true) };

            CreateCompilationWithMscorlib45(baseCtorSource)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, false,
                    Diagnostic("ConstructorInitializerDiagnostic"),
                    Diagnostic("CodeBlockDiagnostic"),
                    Diagnostic("CodeBlockDiagnostic"));
        }

        [Fact, WorkItem(1067286)]
        private void TestCodeBlockAnalyzersForExpressionBody()
        {
            string source = @"
public class B
{
    public int Property => 0;
    public int Method() => 0;
    public int this[int i] => 0;
}";
            var analyzers = new DiagnosticAnalyzer[] { new CodeBlockOrSyntaxNodeAnalyzer(isCodeBlockAnalyzer: true) };

            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, false,
                    Diagnostic("CodeBlockDiagnostic"),
                    Diagnostic("CodeBlockDiagnostic"),
                    Diagnostic("CodeBlockDiagnostic"),
                    Diagnostic("PropertyExpressionBodyDiagnostic"),
                    Diagnostic("IndexerExpressionBodyDiagnostic"),
                    Diagnostic("MethodExpressionBodyDiagnostic"));
        }

        [Fact, WorkItem(592)]
        private void TestSyntaxNodeAnalyzersForExpressionBody()
        {
            string source = @"
public class B
{
    public int Property => 0;
    public int Method() => 0;
    public int this[int i] => 0;
}";
            var analyzers = new DiagnosticAnalyzer[] { new CodeBlockOrSyntaxNodeAnalyzer(isCodeBlockAnalyzer: false) };

            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, false,
                    Diagnostic("PropertyExpressionBodyDiagnostic"),
                    Diagnostic("IndexerExpressionBodyDiagnostic"),
                    Diagnostic("MethodExpressionBodyDiagnostic"));
        }

        [Fact, WorkItem(592)]
        private void TestMethodSymbolAnalyzersForExpressionBody()
        {
            string source = @"
public class B
{
    public int Property => 0;
    public int Method() => 0;
    public int this[int i] => 0;
}";
            var analyzers = new DiagnosticAnalyzer[] { new MethodSymbolAnalyzer() };

            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, false,
                    Diagnostic("MethodSymbolDiagnostic", "0").WithArguments("B.Property.get").WithLocation(4, 28),
                    Diagnostic("MethodSymbolDiagnostic", "Method").WithArguments("B.Method()").WithLocation(5, 16),
                    Diagnostic("MethodSymbolDiagnostic", "0").WithArguments("B.this[int].get").WithLocation(6, 31));
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public class FieldDeclarationAnalyzer : DiagnosticAnalyzer
        {
            public const string DiagnosticId = "MyFieldDiagnostic";
            internal const string Title = "MyFieldDiagnostic";
            internal const string MessageFormat = "MyFieldDiagnostic";
            internal const string Category = "Naming";

            internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
            }

            private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
            {
                var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
                var diagnostic = CodeAnalysis.Diagnostic.Create(Rule, fieldDeclaration.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        [Fact]
        private void TestNoDuplicateCallbacksForFieldDeclaration()
        {
            string source = @"
public class B
{
    public string field = ""field"";
}";
            var analyzers = new DiagnosticAnalyzer[] { new FieldDeclarationAnalyzer() };

            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, false,
                     Diagnostic("MyFieldDiagnostic", @"public string field = ""field"";").WithLocation(4, 5));
        }

        [Fact, WorkItem(565)]
        private void TestCallbacksForFieldDeclarationWithMultipleVariables()
        {
            string source = @"
public class B
{
    public string field1, field2;
    public int field3 = 0, field4 = 1;
    public int field5, field6 = 1;
}";
            var analyzers = new DiagnosticAnalyzer[] { new FieldDeclarationAnalyzer() };

            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, false,
                     Diagnostic("MyFieldDiagnostic", @"public string field1, field2;").WithLocation(4, 5),
                     Diagnostic("MyFieldDiagnostic", @"public int field3 = 0, field4 = 1;").WithLocation(5, 5),
                     Diagnostic("MyFieldDiagnostic", @"public int field5, field6 = 1;").WithLocation(6, 5));
        }

        [Fact, WorkItem(1096600)]
        private void TestDescriptorForConfigurableCompilerDiagnostics()
        {
            // Verify that all configurable compiler diagnostics, i.e. all non-error diagnostics,
            // have a non-null and non-empty Title and Category.
            // These diagnostic descriptor fields show up in the ruleset editor and hence must have a valid value.

            var analyzer = new CSharpCompilerDiagnosticAnalyzer();
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                Assert.Equal(descriptor.IsEnabledByDefault, true);

                if (descriptor.IsNotConfigurable())
                {
                    continue;
                }

                var title = descriptor.Title.ToString();
                if (string.IsNullOrEmpty(title))
                {
                    var id = Int32.Parse(descriptor.Id.Substring(2));
                    var missingResource = Enum.GetName(typeof(ErrorCode), id) + "_Title";
                    var message = string.Format("Add resource string named '{0}' for Title of '{1}' to '{2}'", missingResource, descriptor.Id, nameof(CSharpResources));

                    // This assert will fire if you are adding a new compiler diagnostic (non-error severity),
                    // but did not add a title resource string for the diagnostic.
                    Assert.True(false, message);
                }

                var category = descriptor.Category;
                if (string.IsNullOrEmpty(title))
                {
                    var message = string.Format("'{0}' must have a non-null non-empty 'Category'", descriptor.Id);
                    Assert.True(false, message);
                }
            }
        }

        public class CodeBlockOrSyntaxNodeAnalyzer : DiagnosticAnalyzer
        {
            private readonly bool _isCodeBlockAnalyzer;

            public static DiagnosticDescriptor Desciptor1 = DescriptorFactory.CreateSimpleDescriptor("CodeBlockDiagnostic");
            public static DiagnosticDescriptor Desciptor2 = DescriptorFactory.CreateSimpleDescriptor("EqualsValueDiagnostic");
            public static DiagnosticDescriptor Desciptor3 = DescriptorFactory.CreateSimpleDescriptor("ConstructorInitializerDiagnostic");
            public static DiagnosticDescriptor Desciptor4 = DescriptorFactory.CreateSimpleDescriptor("PropertyExpressionBodyDiagnostic");
            public static DiagnosticDescriptor Desciptor5 = DescriptorFactory.CreateSimpleDescriptor("IndexerExpressionBodyDiagnostic");
            public static DiagnosticDescriptor Desciptor6 = DescriptorFactory.CreateSimpleDescriptor("MethodExpressionBodyDiagnostic");

            public CodeBlockOrSyntaxNodeAnalyzer(bool isCodeBlockAnalyzer)
            {
                _isCodeBlockAnalyzer = isCodeBlockAnalyzer;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(Desciptor1, Desciptor2, Desciptor3, Desciptor4, Desciptor5, Desciptor6); }
            }

            public override void Initialize(AnalysisContext context)
            {
                if (_isCodeBlockAnalyzer)
                {
                    context.RegisterCodeBlockStartAction<SyntaxKind>(OnCodeBlockStarted);
                    context.RegisterCodeBlockAction(OnCodeBlockEnded);
                }
                else
                {
                    Action<Action<SyntaxNodeAnalysisContext>, ImmutableArray<SyntaxKind>> registerMethod =
                        (action, Kinds) => context.RegisterSyntaxNodeAction(action, Kinds);
                    var analyzer = new NodeAnalyzer();
                    analyzer.Initialize(registerMethod);
                }
            }

            public static void OnCodeBlockEnded(CodeBlockAnalysisContext context)
            {
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Desciptor1, Location.None));
            }

            public static void OnCodeBlockStarted(CodeBlockStartAnalysisContext<SyntaxKind> context)
            {
                Action<Action<SyntaxNodeAnalysisContext>, ImmutableArray<SyntaxKind>> registerMethod =
                    (action, Kinds) => context.RegisterSyntaxNodeAction(action, Kinds);
                var analyzer = new NodeAnalyzer();
                analyzer.Initialize(registerMethod);
            }

            protected class NodeAnalyzer
            {
                public void Initialize(Action<Action<SyntaxNodeAnalysisContext>, ImmutableArray<SyntaxKind>> registerSyntaxNodeAction)
                {
                    registerSyntaxNodeAction(context => { context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Desciptor2, Location.None)); },
                        ImmutableArray.Create(SyntaxKind.EqualsValueClause));

                    registerSyntaxNodeAction(context => { context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Desciptor3, Location.None)); },
                        ImmutableArray.Create(SyntaxKind.BaseConstructorInitializer));

                    registerSyntaxNodeAction(context =>
                    {
                        var descriptor = default(DiagnosticDescriptor);
                        switch (CSharpExtensions.Kind(context.Node.Parent))
                        {
                            case SyntaxKind.PropertyDeclaration:
                                descriptor = Desciptor4;
                                break;
                            case SyntaxKind.IndexerDeclaration:
                                descriptor = Desciptor5;
                                break;
                            default:
                                descriptor = Desciptor6;
                                break;
                        }

                        context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(descriptor, Location.None));
                    }, ImmutableArray.Create(SyntaxKind.ArrowExpressionClause));
                }
            }
        }

        public class MethodSymbolAnalyzer : DiagnosticAnalyzer
        {
            public static DiagnosticDescriptor Desciptor1 = new DiagnosticDescriptor("MethodSymbolDiagnostic", "MethodSymbolDiagnostic", "{0}", "MethodSymbolDiagnostic", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(Desciptor1); }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(ctxt =>
                {
                    var method = ((IMethodSymbol)ctxt.Symbol);
                    ctxt.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Desciptor1, method.Locations[0], method.ToDisplayString()));
                }, SymbolKind.Method);
            }
        }

        [Fact, WorkItem(252, "https://github.com/dotnet/roslyn/issues/252"), WorkItem(1392, "https://github.com/dotnet/roslyn/issues/1392")]
        public void TestReportingUnsupportedDiagnostic()
        {
            string source = @"";
            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerReportingUnsupportedDiagnostic() };
            string message = new ArgumentException(string.Format(AnalyzerDriverResources.UnsupportedDiagnosticReported, AnalyzerReportingUnsupportedDiagnostic.UnsupportedDescriptor.Id), "diagnostic").Message;

            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: true,
                     expected: Diagnostic("AD0001")
                     .WithArguments("Microsoft.CodeAnalysis.CSharp.UnitTests.DiagnosticAnalyzerTests+AnalyzerReportingUnsupportedDiagnostic", "System.ArgumentException", message)
                     .WithLocation(1, 1));
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        public class AnalyzerReportingUnsupportedDiagnostic : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor SupportedDescriptor =
                new DiagnosticDescriptor("ID_1", "DummyTitle", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public static readonly DiagnosticDescriptor UnsupportedDescriptor =
                new DiagnosticDescriptor("ID_2", "DummyTitle", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(SupportedDescriptor);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationAction(compilationContext =>
                    compilationContext.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(UnsupportedDescriptor, Location.None)));
            }
        }

        [Fact, WorkItem(1473, "https://github.com/dotnet/roslyn/issues/1473")]
        public void TestReportingNotConfigurableDiagnostic()
        {
            string source = @"";
            var analyzers = new DiagnosticAnalyzer[] { new NotConfigurableDiagnosticAnalyzer() };

            // Verify, not configurable enabled diagnostic is always reported and disabled diagnostic is never reported..
            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: false, expected: Diagnostic(NotConfigurableDiagnosticAnalyzer.EnabledRule.Id));

            // Verify not configurable enabled diagnostic cannot be suppressed.
            var specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(NotConfigurableDiagnosticAnalyzer.EnabledRule.Id, ReportDiagnostic.Suppress);
            var options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions);

            CreateCompilationWithMscorlib45(source, options: options)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: false, expected: Diagnostic(NotConfigurableDiagnosticAnalyzer.EnabledRule.Id));

            // Verify not configurable disabled diagnostic cannot be enabled.
            specificDiagOptions.Clear();
            specificDiagOptions.Add(NotConfigurableDiagnosticAnalyzer.DisabledRule.Id, ReportDiagnostic.Warn);
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions);

            CreateCompilationWithMscorlib45(source, options: options)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: false, expected: Diagnostic(NotConfigurableDiagnosticAnalyzer.EnabledRule.Id));
        }

        [Fact, WorkItem(1709, "https://github.com/dotnet/roslyn/issues/1709")]
        public void TestCodeBlockAction()
        {
            string source = @"
class C
{
    public void M() {}
}";
            var analyzers = new DiagnosticAnalyzer[] { new CodeBlockActionAnalyzer() };

            // Verify, code block action diagnostics.
            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: false,
                    expected: new[] {
                        Diagnostic(CodeBlockActionAnalyzer.CodeBlockTopLevelRule.Id, "M").WithArguments("M").WithLocation(4, 17),
                        Diagnostic(CodeBlockActionAnalyzer.CodeBlockPerCompilationRule.Id, "M").WithArguments("M").WithLocation(4, 17)
                    });
        }

        [Fact, WorkItem(1709, "https://github.com/dotnet/roslyn/issues/1709")]
        public void TestCodeBlockAction_OnlyStatelessAction()
        {
            string source = @"
class C
{
    public void M() {}
}";
            var analyzers = new DiagnosticAnalyzer[] { new CodeBlockActionAnalyzer(onlyStatelessAction: true) };

            // Verify, code block action diagnostics.
            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: false,
                    expected: Diagnostic(CodeBlockActionAnalyzer.CodeBlockTopLevelRule.Id, "M").WithArguments("M").WithLocation(4, 17));
        }
    }
}
