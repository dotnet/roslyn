// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticAnalyzerTests : CompilingTestBase
    {
        [Serializable]
        class TestDiagnostic : Diagnostic, ISerializable
        {
            private readonly string id;
            private readonly string kind;
            private readonly DiagnosticSeverity severity;
            private readonly Location location;
            private readonly string message;
            private readonly bool isWarningAsError;
            private readonly object[] arguments;
            private static readonly Location[] emptyLocations = new Location[0];

            public TestDiagnostic(string id, string kind, DiagnosticSeverity severity, Location location, string message, bool isWarningAsError, params object[] arguments)
            {
                this.id = id;
                this.kind = kind;
                this.severity = severity;
                this.location = location;
                this.message = message;
                this.isWarningAsError = isWarningAsError;
                this.arguments = arguments;
            }

            public override IReadOnlyList<Location> AdditionalLocations { get { return emptyLocations; } }

            public override string Id { get { return id; } }

            public override string Category { get { return kind; } }

            public override Location Location { get { return location; } }

            internal override IReadOnlyList<object> Arguments { get { return arguments; } }

            public override DiagnosticSeverity Severity { get { return severity; } }

            public override int WarningLevel { get { return 2; } }

            public override bool IsWarningAsError { get { return isWarningAsError; } }

            public override bool Equals(Diagnostic obj)
            {
                if (obj == null || this.GetType() != obj.GetType()) return false;
                TestDiagnostic other = (TestDiagnostic)obj;
                return
                    this.id == other.id &&
                    this.kind == other.kind &&
                    this.location == other.location &&
                    this.message == other.message &&
                    SameData(this.arguments, other.arguments);
            }

            private static bool SameData(object[] d1, object[] d2)
            {
                return (d1 == null) == (d2 == null) && (d1 == null || d1.SequenceEqual(d2));
            }

            public override string GetMessage(CultureInfo culture = null)
            {
                return string.Format(message, arguments);
            }

            private TestDiagnostic(SerializationInfo info, StreamingContext context)
            {
                this.id = info.GetString("id");
                this.kind = info.GetString("kind");
                this.message = info.GetString("message");
                this.location = (Location)info.GetValue("location", typeof(Location));
                this.severity = (DiagnosticSeverity)info.GetValue("severity", typeof(DiagnosticSeverity));
                this.isWarningAsError = info.GetBoolean("isWarningAsError");
                this.arguments = (object[])info.GetValue("arguments", typeof(object[]));
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("id", this.id);
                info.AddValue("kind", this.kind);
                info.AddValue("message", this.message);
                info.AddValue("location", this.location, typeof(Location));
                info.AddValue("severity", this.severity, typeof(DiagnosticSeverity));
                info.AddValue("isWarningAsError", this.isWarningAsError);
                info.AddValue("arguments", this.arguments, typeof(object[]));
            }

            internal override Diagnostic WithLocation(Location location)
            {
                // We do not implement "additional locations"
                throw new NotImplementedException();
            }

            internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                throw new NotImplementedException();
            }

            internal override Diagnostic WithWarningAsError(bool isWarningAsError)
            {
                if (isWarningAsError && severity == DiagnosticSeverity.Warning)
                {
                    return new TestDiagnostic(id, kind, DiagnosticSeverity.Error, location, message, true, arguments);
                }
                else
                {
                    return this;
                }
            }
        }

        class ComplainAboutX : ISyntaxNodeAnalyzer<SyntaxKind>
        {
            private static readonly DiagnosticDescriptor CA9999_UseOfVariableThatStartsWithX =
                new DiagnosticDescriptor(id: "CA9999", description: "CA9999_UseOfVariableThatStartsWithX", messageFormat: "Use of variable whose name starts with 'x': '{0}'", category: "Test", defaultSeverity: DiagnosticSeverity.Warning);

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(CA9999_UseOfVariableThatStartsWithX);
                }
            }

            private static readonly ImmutableArray<SyntaxKind> kindsOfInterest = ImmutableArray.Create<SyntaxKind>
            (
                // A use of an identifier. Note that identifiers used in a definitional context use a different syntax
                SyntaxKind.IdentifierName
            );

            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get { return kindsOfInterest; } }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                var id = (IdentifierNameSyntax)node;
                if (id.Identifier.ValueText.StartsWith("x"))
                {
                    addDiagnostic(new TestDiagnostic("CA9999_UseOfVariableThatStartsWithX", "CsTest", DiagnosticSeverity.Warning, id.Location, "Use of variable whose name starts with 'x': '{0}'", false, id.Identifier.ValueText));
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
            .VerifyAnalyzerDiagnostics(new IDiagnosticAnalyzer[] { new ComplainAboutX() }, 
                // (5,18): warning CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x1'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x1").WithArguments("x1"),
                // (5,21): warning CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x2'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x2").WithArguments("x2"),
                // (6,16): warning CA9999_Use7OfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x3'
                //         return x3 + 1;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x3").WithArguments("x3")
                )
            .VerifyAnalyzerDiagnostics3(new IDiagnosticAnalyzer[] { new ComplainAboutX() },
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
            .VerifyAnalyzerDiagnostics(new IDiagnosticAnalyzer[] { new ComplainAboutX() },
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
                )
            .VerifyAnalyzerDiagnostics3(new IDiagnosticAnalyzer[] { new ComplainAboutX() },
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
            var options = TestOptions.Dll.WithSpecificDiagnosticOptions(
                new[] { KeyValuePair.Create("CA9999_UseOfVariableThatStartsWithX", ReportDiagnostic.Suppress) });

            CreateCompilationWithMscorlib45(source, compOptions: options/*, analyzers: new IDiagnosticAnalyzerFactory[] { new ComplainAboutX() }*/).VerifyDiagnostics(
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
            var options = TestOptions.Dll.WithSpecificDiagnosticOptions(
                new[] { KeyValuePair.Create("CA9999_UseOfVariableThatStartsWithX", ReportDiagnostic.Error) });

            CreateCompilationWithMscorlib45(source, compOptions: options).VerifyDiagnostics(
                // (2,18): error CS0246: The type or namespace name 'NotFound' could not be found (are you missing a using directive or an assembly reference?)
                // public class C : NotFound
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NotFound").WithArguments("NotFound"))
            .VerifyAnalyzerDiagnostics(new IDiagnosticAnalyzer[] { new ComplainAboutX() },
                // (6,18): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x1'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x1").WithArguments("x1").WithWarningAsError(true),
                // (6,21): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x2'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x2").WithArguments("x2").WithWarningAsError(true),
                // (7,16): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x3'
                //         return x3 + 1;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x3").WithArguments("x3").WithWarningAsError(true)
                )
            .VerifyAnalyzerDiagnostics3(new IDiagnosticAnalyzer[] { new ComplainAboutX() },
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
            var options = TestOptions.Dll.WithGeneralDiagnosticOption(ReportDiagnostic.Error);

            CreateCompilationWithMscorlib45(source, compOptions: options).VerifyDiagnostics(
                // (2,18): error CS0246: The type or namespace name 'NotFound' could not be found (are you missing a using directive or an assembly reference?)
                // public class C : NotFound
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "NotFound").WithArguments("NotFound")
                )
            .VerifyAnalyzerDiagnostics(new IDiagnosticAnalyzer[] { new ComplainAboutX() },
                // (6,18): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x1'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x1").WithArguments("x1").WithWarningAsError(true),
                // (6,21): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x2'
                //         int x3 = x1(x2);
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x2").WithArguments("x2").WithWarningAsError(true),
                // (7,16): error CA9999_UseOfVariableThatStartsWithX: Use of variable whose name starts with 'x': 'x3'
                //         return x3 + 1;
                Diagnostic("CA9999_UseOfVariableThatStartsWithX", "x3").WithArguments("x3").WithWarningAsError(true))
            .VerifyAnalyzerDiagnostics3(new IDiagnosticAnalyzer[] { new ComplainAboutX() },
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

        class SyntaxAndSymbolAnalyzer : ISyntaxNodeAnalyzer<SyntaxKind>, ISymbolAnalyzer
        {
            private static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor("XX0001", "My Syntax/Symbol Diagnostic", "My Syntax/Symbol Diagnostic for '{0}'", "Compiler", DiagnosticSeverity.Warning);
            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(descriptor);
                }
            }

            public ImmutableArray<SymbolKind> SymbolKindsOfInterest
            {
                get
                {
                    return ImmutableArray.Create(SymbolKind.NamedType);
                }
            }

            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
            {
                get
                {
                    return ImmutableArray.Create(SyntaxKind.Attribute, SyntaxKind.ClassDeclaration, SyntaxKind.UsingStatement);
                }
            }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                switch (node.CSharpKind())
                {
                    case SyntaxKind.Attribute:
                        var diag1 = CodeAnalysis.Diagnostic.Create(descriptor, node.GetLocation(), "Attribute");
                        addDiagnostic(diag1);
                        break;

                    case SyntaxKind.ClassDeclaration:
                        var diag2 = CodeAnalysis.Diagnostic.Create(descriptor, node.GetLocation(), "ClassDeclaration");
                        addDiagnostic(diag2);
                        break;

                    case SyntaxKind.UsingStatement:
                        var diag3 = CodeAnalysis.Diagnostic.Create(descriptor, node.GetLocation(), "UsingStatement");
                        addDiagnostic(diag3);
                        break;
                }
            }

            public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                var diag1 = CodeAnalysis.Diagnostic.Create(descriptor, symbol.Locations[0], "NamedType");
                addDiagnostic(diag1);
            }
        }

        [WorkItem(914236, "DevDiv")]
        [Fact(Skip = "914236")]
        public void DiagnosticAnalyzerSyntaxNodeAndSymbolAnalysis()
        {
            string source = @"
using System;

[Obsolete]
public class C { }";
            var options = TestOptions.Dll.WithGeneralDiagnosticOption(ReportDiagnostic.Error);

            CreateCompilationWithMscorlib45(source, compOptions: options)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(new IDiagnosticAnalyzer[] { new SyntaxAndSymbolAnalyzer() },
                    // Symbol diagnostics
                    Diagnostic("XX0001", "C").WithWarningAsError(true),
                    // Syntax diagnostics
                    Diagnostic("XX0001", "using System;").WithWarningAsError(true),
                    Diagnostic("XX0001", "[Obsolete]").WithWarningAsError(true),
                    Diagnostic("XX0001", "C").WithWarningAsError(true))
                .VerifyAnalyzerDiagnostics3(new IDiagnosticAnalyzer[] { new SyntaxAndSymbolAnalyzer() },
                    // Symbol diagnostics
                    Diagnostic("XX0001", "C").WithWarningAsError(true),
                    // Syntax diagnostics
                    Diagnostic("XX0001", "using System;").WithWarningAsError(true),
                    Diagnostic("XX0001", "[Obsolete]").WithWarningAsError(true),
                    Diagnostic("XX0001", "C").WithWarningAsError(true));

        }

        [Fact]
        void TestGetEffectiveDiagnostics()
        {
            var noneDiagDesciptor = new DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.None);
            var infoDiagDesciptor = new DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info);
            var warningDiagDesciptor = new DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning);
            var errorDiagDesciptor = new DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Error);

            var noneDiag = Microsoft.CodeAnalysis.Diagnostic.Create(noneDiagDesciptor, Location.None);
            var infoDiag = Microsoft.CodeAnalysis.Diagnostic.Create(infoDiagDesciptor, Location.None);
            var warningDiag = Microsoft.CodeAnalysis.Diagnostic.Create(warningDiagDesciptor, Location.None);
            var errorDiag = Microsoft.CodeAnalysis.Diagnostic.Create(errorDiagDesciptor, Location.None);

            var diags = new[] { noneDiag, infoDiag, warningDiag, errorDiag };

            // Escalate all diagnostics to error.
            var specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(noneDiagDesciptor.Id, ReportDiagnostic.Error);
            specificDiagOptions.Add(infoDiagDesciptor.Id, ReportDiagnostic.Error);
            specificDiagOptions.Add(warningDiagDesciptor.Id, ReportDiagnostic.Error);
            var options = TestOptions.Dll.WithSpecificDiagnosticOptions(specificDiagOptions);

            var comp = CreateCompilationWithMscorlib45("", compOptions: options);
            var effectiveDiags = AnalyzerDriver.GetEffectiveDiagnostics(diags, comp).ToArray();
            Assert.Equal(diags.Length, effectiveDiags.Length);
            foreach (var effectiveDiag in effectiveDiags)
            {
                Assert.True(effectiveDiag.Severity == DiagnosticSeverity.Error ||
                    (effectiveDiag.Severity == DiagnosticSeverity.Warning && effectiveDiag.IsWarningAsError));
            }

            // Suppress all diagnostics.
            // NOTE: Diagnostics with default severity error cannot be suppressed and its severity cannot be lowered.
            specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(noneDiagDesciptor.Id, ReportDiagnostic.Suppress);
            specificDiagOptions.Add(infoDiagDesciptor.Id, ReportDiagnostic.Suppress);
            specificDiagOptions.Add(warningDiagDesciptor.Id, ReportDiagnostic.Suppress);
            specificDiagOptions.Add(errorDiagDesciptor.Id, ReportDiagnostic.Suppress);
            options = TestOptions.Dll.WithSpecificDiagnosticOptions(specificDiagOptions);

            comp = CreateCompilationWithMscorlib45("", compOptions: options);
            effectiveDiags = AnalyzerDriver.GetEffectiveDiagnostics(diags, comp).ToArray();
            Assert.Equal(1, effectiveDiags.Length);
            Assert.Equal(errorDiagDesciptor.Id, effectiveDiags[0].Id);

            // Shuffle diagnostic severity.
            specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(noneDiagDesciptor.Id, ReportDiagnostic.Info);
            specificDiagOptions.Add(infoDiagDesciptor.Id, ReportDiagnostic.Warn);
            specificDiagOptions.Add(warningDiagDesciptor.Id, ReportDiagnostic.Error);
            specificDiagOptions.Add(errorDiagDesciptor.Id, ReportDiagnostic.Warn);
            options = TestOptions.Dll.WithSpecificDiagnosticOptions(specificDiagOptions);

            comp = CreateCompilationWithMscorlib45("", compOptions: options);
            effectiveDiags = AnalyzerDriver.GetEffectiveDiagnostics(diags, comp).ToArray();
            Assert.Equal(diags.Length, effectiveDiags.Length);
            var diagIds = new HashSet<string>(diags.Select(d => d.Id));
            foreach (var effectiveDiag in effectiveDiags)
            {
                Assert.True(diagIds.Remove(effectiveDiag.Id));

                switch (effectiveDiag.Severity)
                {
                    case DiagnosticSeverity.Info:
                        Assert.Equal(noneDiagDesciptor.Id, effectiveDiag.Id);
                        break;

                    case DiagnosticSeverity.Warning:
                        if (!effectiveDiag.IsWarningAsError)
                        {
                            Assert.Equal(infoDiagDesciptor.Id, effectiveDiag.Id);
                        }
                        else
                        {
                            Assert.Equal(warningDiagDesciptor.Id, effectiveDiag.Id);
                        }

                        break;

                    case DiagnosticSeverity.Error:
                        Assert.Equal(errorDiagDesciptor.Id, effectiveDiag.Id);
                        break;

                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            Assert.Empty(diagIds);
        }
    }
}
