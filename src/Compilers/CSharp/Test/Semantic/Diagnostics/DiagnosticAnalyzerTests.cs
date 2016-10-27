﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticAnalyzerTests : CompilingTestBase
    {
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

        [WorkItem(892467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/892467")]
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

        [WorkItem(892467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/892467")]
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

        [WorkItem(892467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/892467")]
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

        [WorkItem(892467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/892467")]
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

        [WorkItem(892467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/892467")]
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

        [Fact, WorkItem(1038025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038025")]
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

        [WorkItem(914236, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/914236")]
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

        public void TestGetEffectiveDiagnostics()
        {
            var noneDiagDescriptor = new DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Hidden, isEnabledByDefault: true);
            var infoDiagDescriptor = new DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info, isEnabledByDefault: true);
            var warningDiagDescriptor = new DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            var errorDiagDescriptor = new DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Error, isEnabledByDefault: true);

            var noneDiag = CodeAnalysis.Diagnostic.Create(noneDiagDescriptor, Location.None);
            var infoDiag = CodeAnalysis.Diagnostic.Create(infoDiagDescriptor, Location.None);
            var warningDiag = CodeAnalysis.Diagnostic.Create(warningDiagDescriptor, Location.None);
            var errorDiag = CodeAnalysis.Diagnostic.Create(errorDiagDescriptor, Location.None);

            var diags = new[] { noneDiag, infoDiag, warningDiag, errorDiag };

            // Escalate all diagnostics to error.
            var specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(noneDiagDescriptor.Id, ReportDiagnostic.Error);
            specificDiagOptions.Add(infoDiagDescriptor.Id, ReportDiagnostic.Error);
            specificDiagOptions.Add(warningDiagDescriptor.Id, ReportDiagnostic.Error);
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
            specificDiagOptions.Add(noneDiagDescriptor.Id, ReportDiagnostic.Suppress);
            specificDiagOptions.Add(infoDiagDescriptor.Id, ReportDiagnostic.Suppress);
            specificDiagOptions.Add(warningDiagDescriptor.Id, ReportDiagnostic.Suppress);
            specificDiagOptions.Add(errorDiagDescriptor.Id, ReportDiagnostic.Suppress);
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions);

            comp = CreateCompilationWithMscorlib45("", options: options);
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray();
            Assert.Equal(0, effectiveDiags.Length);

            // Shuffle diagnostic severity.
            specificDiagOptions = new Dictionary<string, ReportDiagnostic>();
            specificDiagOptions.Add(noneDiagDescriptor.Id, ReportDiagnostic.Info);
            specificDiagOptions.Add(infoDiagDescriptor.Id, ReportDiagnostic.Hidden);
            specificDiagOptions.Add(warningDiagDescriptor.Id, ReportDiagnostic.Error);
            specificDiagOptions.Add(errorDiagDescriptor.Id, ReportDiagnostic.Warn);
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
                        Assert.Equal(infoDiagDescriptor.Id, effectiveDiag.Id);
                        break;

                    case DiagnosticSeverity.Info:
                        Assert.Equal(noneDiagDescriptor.Id, effectiveDiag.Id);
                        break;

                    case DiagnosticSeverity.Warning:
                        Assert.Equal(errorDiagDescriptor.Id, effectiveDiag.Id);
                        break;

                    case DiagnosticSeverity.Error:
                        Assert.Equal(warningDiagDescriptor.Id, effectiveDiag.Id);
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
            var noneDiagDescriptor = new DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Hidden, isEnabledByDefault: true);
            var infoDiagDescriptor = new DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info, isEnabledByDefault: true);
            var warningDiagDescriptor = new DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            var errorDiagDescriptor = new DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Error, isEnabledByDefault: true);

            var noneDiag = Microsoft.CodeAnalysis.Diagnostic.Create(noneDiagDescriptor, Location.None);
            var infoDiag = Microsoft.CodeAnalysis.Diagnostic.Create(infoDiagDescriptor, Location.None);
            var warningDiag = Microsoft.CodeAnalysis.Diagnostic.Create(warningDiagDescriptor, Location.None);
            var errorDiag = Microsoft.CodeAnalysis.Diagnostic.Create(errorDiagDescriptor, Location.None);

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

        public void TestDisabledDiagnostics()
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
        public void TestDisabledAnalyzers()
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

        [Fact, WorkItem(1008059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1008059")]
        public void TestCodeBlockAnalyzersForNoExecutableCode()
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

        [Fact, WorkItem(1008059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1008059")]
        public void TestCodeBlockAnalyzersForBaseConstructorInitializer()
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

        [Fact, WorkItem(1067286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067286")]
        public void TestCodeBlockAnalyzersForExpressionBody()
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

        [Fact, WorkItem(592, "https://github.com/dotnet/roslyn/issues/592")]
        public void TestSyntaxNodeAnalyzersForExpressionBody()
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

        [Fact, WorkItem(592, "https://github.com/dotnet/roslyn/issues/592")]
        public void TestMethodSymbolAnalyzersForExpressionBody()
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
        public void TestNoDuplicateCallbacksForFieldDeclaration()
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

        [Fact, WorkItem(565, "https://github.com/dotnet/roslyn/issues/565")]
        public void TestCallbacksForFieldDeclarationWithMultipleVariables()
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

        [Fact, WorkItem(1096600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1096600")]
        public void TestDescriptorForConfigurableCompilerDiagnostics()
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

            public static DiagnosticDescriptor Descriptor1 = DescriptorFactory.CreateSimpleDescriptor("CodeBlockDiagnostic");
            public static DiagnosticDescriptor Descriptor2 = DescriptorFactory.CreateSimpleDescriptor("EqualsValueDiagnostic");
            public static DiagnosticDescriptor Descriptor3 = DescriptorFactory.CreateSimpleDescriptor("ConstructorInitializerDiagnostic");
            public static DiagnosticDescriptor Descriptor4 = DescriptorFactory.CreateSimpleDescriptor("PropertyExpressionBodyDiagnostic");
            public static DiagnosticDescriptor Descriptor5 = DescriptorFactory.CreateSimpleDescriptor("IndexerExpressionBodyDiagnostic");
            public static DiagnosticDescriptor Descriptor6 = DescriptorFactory.CreateSimpleDescriptor("MethodExpressionBodyDiagnostic");

            public CodeBlockOrSyntaxNodeAnalyzer(bool isCodeBlockAnalyzer)
            {
                _isCodeBlockAnalyzer = isCodeBlockAnalyzer;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(Descriptor1, Descriptor2, Descriptor3, Descriptor4, Descriptor5, Descriptor6); }
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
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor1, Location.None));
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
                    registerSyntaxNodeAction(context => { context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor2, Location.None)); },
                        ImmutableArray.Create(SyntaxKind.EqualsValueClause));

                    registerSyntaxNodeAction(context => { context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor3, Location.None)); },
                        ImmutableArray.Create(SyntaxKind.BaseConstructorInitializer));

                    registerSyntaxNodeAction(context =>
                    {
                        var descriptor = default(DiagnosticDescriptor);
                        switch (CSharpExtensions.Kind(context.Node.Parent))
                        {
                            case SyntaxKind.PropertyDeclaration:
                                descriptor = Descriptor4;
                                break;
                            case SyntaxKind.IndexerDeclaration:
                                descriptor = Descriptor5;
                                break;
                            default:
                                descriptor = Descriptor6;
                                break;
                        }

                        context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(descriptor, Location.None));
                    }, ImmutableArray.Create(SyntaxKind.ArrowExpressionClause));
                }
            }
        }

        public class MethodSymbolAnalyzer : DiagnosticAnalyzer
        {
            public static DiagnosticDescriptor Descriptor1 = new DiagnosticDescriptor("MethodSymbolDiagnostic", "MethodSymbolDiagnostic", "{0}", "MethodSymbolDiagnostic", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(Descriptor1); }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(ctxt =>
                {
                    var method = ((IMethodSymbol)ctxt.Symbol);
                    ctxt.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor1, method.Locations[0], method.ToDisplayString()));
                }, SymbolKind.Method);
            }
        }

        [Fact, WorkItem(252, "https://github.com/dotnet/roslyn/issues/252"), WorkItem(1392, "https://github.com/dotnet/roslyn/issues/1392")]
        public void TestReportingUnsupportedDiagnostic()
        {
            string source = @"";
            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerReportingUnsupportedDiagnostic() };
            string message = new ArgumentException(string.Format(CodeAnalysisResources.UnsupportedDiagnosticReported, AnalyzerReportingUnsupportedDiagnostic.UnsupportedDescriptor.Id), "diagnostic").Message;

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

        [Fact, WorkItem(4376, "https://github.com/dotnet/roslyn/issues/4376")]
        public void TestReportingDiagnosticWithInvalidId()
        {
            string source = @"";
            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerWithInvalidDiagnosticId() };
            string message = new ArgumentException(string.Format(CodeAnalysisResources.InvalidDiagnosticIdReported, AnalyzerWithInvalidDiagnosticId.Descriptor.Id), "diagnostic").Message;

            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: true,
                     expected: Diagnostic("AD0001")
                     .WithArguments("Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers+AnalyzerWithInvalidDiagnosticId", "System.ArgumentException", message)
                     .WithLocation(1, 1));
        }

        [Fact, WorkItem(7173, "https://github.com/dotnet/roslyn/issues/7173")]
        public void TestReportingDiagnosticWithInvalidLocation()
        {
            var source1 = @"class C1 { void M() { int i = 0; i++; } }";
            var source2 = @"class C2 { void M() { int i = 0; i++; } }";
            var compilation = CreateCompilationWithMscorlib45(source1, parseOptions: TestOptions.RegularWithIOperationFeature);
            var anotherCompilation = CreateCompilationWithMscorlib45(source2);
            var treeInAnotherCompilation = anotherCompilation.SyntaxTrees.Single();

            string message = new ArgumentException(
                string.Format(CodeAnalysisResources.InvalidDiagnosticLocationReported, AnalyzerWithInvalidDiagnosticLocation.Descriptor.Id, treeInAnotherCompilation.FilePath), "diagnostic").Message;

            compilation.VerifyDiagnostics();

            foreach (AnalyzerWithInvalidDiagnosticLocation.ActionKind actionKind in Enum.GetValues(typeof(AnalyzerWithInvalidDiagnosticLocation.ActionKind)))
            {
                var analyzer = new AnalyzerWithInvalidDiagnosticLocation(treeInAnotherCompilation, actionKind);
                var analyzers = new DiagnosticAnalyzer[] { analyzer };
                compilation
                    .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: true,
                        expected:
                        Diagnostic("AD0001")
                            .WithArguments("Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers+AnalyzerWithInvalidDiagnosticLocation", "System.ArgumentException", message)
                            .WithLocation(1, 1)
                    );
            }
        }

        [Fact, WorkItem(13120, "https://github.com/dotnet/roslyn/issues/13120")]
        public void TestRegisteringAsyncAnalyzerMethod()
        {
            string source = @"";
            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerWithAsyncMethodRegistration() };
            string message = new ArgumentException(string.Format(CodeAnalysisResources.AsyncAnalyzerActionCannotBeRegistered), "action").Message;

            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: true,
                     expected: Diagnostic("AD0001")
                     .WithArguments("Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers+AnalyzerWithAsyncMethodRegistration", "System.ArgumentException", message)
                     .WithLocation(1, 1));
        }

        [Fact, WorkItem(13120, "https://github.com/dotnet/roslyn/issues/13120")]
        public void TestRegisteringAsyncAnalyzerLambda()
        {
            string source = @"";
            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerWithAsyncLambdaRegistration() };
            string message = new ArgumentException(string.Format(CodeAnalysisResources.AsyncAnalyzerActionCannotBeRegistered), "action").Message;

            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: true,
                     expected: Diagnostic("AD0001")
                     .WithArguments("Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers+AnalyzerWithAsyncLambdaRegistration", "System.ArgumentException", message)
                     .WithLocation(1, 1));
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

        [Fact, WorkItem(2614, "https://github.com/dotnet/roslyn/issues/2614")]
        public void TestGenericName()
        {
            var source = @"
using System;
using System.Text;

namespace ConsoleApplication1
{
    class MyClass
    {   
        private Nullable<int> myVar = 5;
        void Method()
        {

        }
    }
}";

            TestGenericNameCore(source, new CSharpGenericNameAnalyzer());
        }

        private void TestGenericNameCore(string source, params DiagnosticAnalyzer[] analyzers)
        {
            // Verify, no duplicate diagnostics on generic name.
            CreateCompilationWithMscorlib45(source)
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: false,
                    expected: Diagnostic(CSharpGenericNameAnalyzer.DiagnosticId, @"Nullable<int>").WithLocation(9, 17));
        }

        [Fact, WorkItem(4745, "https://github.com/dotnet/roslyn/issues/4745")]
        public void TestNamespaceDeclarationAnalyzer()
        {
            var source = @"
namespace Foo.Bar.FooBar { }
";
            var analyzers = new DiagnosticAnalyzer[] { new CSharpNamespaceDeclarationAnalyzer() };

            // Verify, no duplicate diagnostics on qualified name.
            CreateCompilationWithMscorlib45(source)
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: false,
                    expected: Diagnostic(CSharpNamespaceDeclarationAnalyzer.DiagnosticId, @"namespace Foo.Bar.FooBar { }").WithLocation(2, 1));
        }

        [Fact, WorkItem(2980, "https://github.com/dotnet/roslyn/issues/2980")]
        public void TestAnalyzerWithNoActions()
        {
            var source = @"
using System;
using System.Text;

namespace ConsoleApplication1
{
    class MyClass
    {   
        private Nullable<int> myVar = 5;
        void Method()
        {

        }
    }
}";

            // Ensure that adding a dummy analyzer with no actions doesn't bring down entire analysis.
            // See https://github.com/dotnet/roslyn/issues/2980 for details.
            TestGenericNameCore(source, new AnalyzerWithNoActions(), new CSharpGenericNameAnalyzer());
        }

        [Fact, WorkItem(4055, "https://github.com/dotnet/roslyn/issues/4055")]
        public void TestAnalyzerWithNoSupportedDiagnostics()
        {
            var source = @"
class MyClass
{
}";
            // Ensure that adding a dummy analyzer with no supported diagnostics doesn't bring down entire analysis.
            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerWithNoSupportedDiagnostics() };
            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers);
        }

        private static void TestEffectiveSeverity(
            DiagnosticSeverity defaultSeverity,
            ReportDiagnostic expectedEffectiveSeverity,
            Dictionary<string, ReportDiagnostic> specificOptions = null,
            ReportDiagnostic generalOption = ReportDiagnostic.Default,
            bool isEnabledByDefault = true)
        {
            specificOptions = specificOptions ?? new Dictionary<string, ReportDiagnostic>();
            var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication, generalDiagnosticOption: generalOption, specificDiagnosticOptions: specificOptions);
            var descriptor = new DiagnosticDescriptor(id: "Test0001", title: "Test0001", messageFormat: "Test0001", category: "Test0001", defaultSeverity: defaultSeverity, isEnabledByDefault: isEnabledByDefault);
            var effectiveSeverity = descriptor.GetEffectiveSeverity(options);
            Assert.Equal(expectedEffectiveSeverity, effectiveSeverity);
        }

        [Fact]
        [WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")]
        [WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")]
        public void EffectiveSeverity_DiagnosticDefault1()
        {
            TestEffectiveSeverity(DiagnosticSeverity.Warning, ReportDiagnostic.Warn);
        }

        [Fact]
        [WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")]
        [WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")]
        public void EffectiveSeverity_DiagnosticDefault2()
        {
            var specificOptions = new Dictionary<string, ReportDiagnostic>() { { "Test0001", ReportDiagnostic.Default } };
            var generalOption = ReportDiagnostic.Error;

            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity: ReportDiagnostic.Warn, specificOptions: specificOptions, generalOption: generalOption);
        }

        [Fact]
        [WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")]
        [WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")]
        public void EffectiveSeverity_GeneralOption()
        {
            var generalOption = ReportDiagnostic.Error;
            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity: generalOption, generalOption: generalOption);
        }

        [Fact]
        [WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")]
        [WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")]
        public void EffectiveSeverity_SpecificOption()
        {
            var specificOption = ReportDiagnostic.Suppress;
            var specificOptions = new Dictionary<string, ReportDiagnostic>() { { "Test0001", specificOption } };
            var generalOption = ReportDiagnostic.Error;

            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity: specificOption, specificOptions: specificOptions, generalOption: generalOption);
        }

        [Fact]
        [WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")]
        [WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")]
        public void EffectiveSeverity_GeneralOptionDoesNotEnableDisabledDiagnostic()
        {
            var generalOption = ReportDiagnostic.Error;
            var enabledByDefault = false;

            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity: ReportDiagnostic.Suppress, generalOption: generalOption, isEnabledByDefault: enabledByDefault);
        }


        [Fact()]
        [WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")]
        [WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")]
        public void EffectiveSeverity_SpecificOptionEnablesDisabledDiagnostic()
        {
            var specificOption = ReportDiagnostic.Warn;
            var specificOptions = new Dictionary<string, ReportDiagnostic>() { { "Test0001", specificOption } };
            var generalOption = ReportDiagnostic.Error;
            var enabledByDefault = false;

            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity: specificOption, specificOptions: specificOptions, generalOption: generalOption, isEnabledByDefault: enabledByDefault);
        }

        [Fact, WorkItem(5463, "https://github.com/dotnet/roslyn/issues/5463")]
        public void TestObjectCreationInCodeBlockAnalyzer()
        {
            string source = @"
class C { }
class D
{
    public C x = new C();
}";
            var analyzers = new DiagnosticAnalyzer[] { new CSharpCodeBlockObjectCreationAnalyzer() };

            // Verify, code block action diagnostics.
            CreateCompilationWithMscorlib45(source)
                .VerifyDiagnostics()
                .VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: false,
                    expected: new[] {
                        Diagnostic(CSharpCodeBlockObjectCreationAnalyzer.DiagnosticDescriptor.Id, "new C()").WithLocation(5, 18)
                    });
        }

        private static Compilation GetCompilationWithConcurrentBuildEnabled(string source)
        {
            var compilation = CreateCompilationWithMscorlib45(source);

            // NOTE: We set the concurrentBuild option to true after creating the compilation as CreateCompilationWithMscorlib
            //       always sets concurrentBuild to false if debugger is attached, even if we had passed options with concurrentBuild = true to that API.
            //       We want the tests using GetCompilationWithConcurrentBuildEnabled to have identical behavior with and without debugger being attached.
            var options = compilation.Options.WithConcurrentBuild(true);
            return compilation.WithOptions(options);
        }

        [Fact, WorkItem(6737, "https://github.com/dotnet/roslyn/issues/6737")]
        public void TestNonConcurrentAnalyzer()
        {
            var builder = new StringBuilder();
            var typeCount = 100;
            for (int i = 1; i <= typeCount; i++)
            {
                var typeName = $"C{i}";
                builder.Append($"\r\nclass {typeName} {{ }}");
            }

            var source = builder.ToString();
            var analyzers = new DiagnosticAnalyzer[] { new NonConcurrentAnalyzer() };

            // Verify no diagnostics.
            var compilation = GetCompilationWithConcurrentBuildEnabled(source);
            compilation.VerifyDiagnostics();
            compilation.VerifyAnalyzerDiagnostics(analyzers);
        }

        [Fact, WorkItem(6737, "https://github.com/dotnet/roslyn/issues/6737")]
        public void TestConcurrentAnalyzer()
        {
            if (Environment.ProcessorCount <= 1)
            {
                // Don't test for non-concurrent environment.
                return;
            }

            var builder = new StringBuilder();
            var typeCount = 100;
            var typeNames = new string[typeCount];
            for (int i = 1; i <= typeCount; i++)
            {
                var typeName = $"C{i}";
                typeNames[i - 1] = typeName;
                builder.Append($"\r\nclass {typeName} {{ }}");
            }

            var source = builder.ToString();
            var compilation = GetCompilationWithConcurrentBuildEnabled(source);
            compilation.VerifyDiagnostics();

            // Verify analyzer diagnostics for Concurrent analyzer only.
            var analyzers = new DiagnosticAnalyzer[] { new ConcurrentAnalyzer(typeNames) };
            var expected = new DiagnosticDescription[typeCount];
            for (int i = 0; i < typeCount; i++)
            {
                var typeName = $"C{i + 1}";
                expected[i] = Diagnostic(ConcurrentAnalyzer.Descriptor.Id, typeName)
                    .WithArguments(typeName)
                    .WithLocation(i + 2, 7);
            }

            compilation.VerifyAnalyzerDiagnostics(analyzers, expected: expected);

            // Verify analyzer diagnostics for Concurrent and NonConcurrent analyzer together (latter reports diagnostics only for error cases).
            analyzers = new DiagnosticAnalyzer[] { new ConcurrentAnalyzer(typeNames), new NonConcurrentAnalyzer() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, expected: expected);
        }

        [Fact, WorkItem(6998, "https://github.com/dotnet/roslyn/issues/6998")]
        public void TestGeneratedCodeAnalyzer()
        {
            string source = @"
[System.CodeDom.Compiler.GeneratedCodeAttribute(""tool"", ""version"")]
class GeneratedCode{0}
{{
    private class Nested{0} {{ }}
}}

class NonGeneratedCode{0}
{{
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""tool"", ""version"")]
    private class NestedGeneratedCode{0} {{ }}
}}
";
            var generatedFileNames = new string[]
            {
                "TemporaryGeneratedFile_036C0B5B-1481-4323-8D20-8F5ADCB23D92.cs",
                "Test.designer.cs",
                "Test.Designer.cs",
                "Test.generated.cs",
                "Test.g.cs",
                "Test.g.i.cs"
            };

            var builder = ImmutableArray.CreateBuilder<SyntaxTree>();
            int treeNum = 0;

            // Trees with non-generated code file names
            var tree = CSharpSyntaxTree.ParseText(string.Format(source, treeNum++), path: "SourceFileRegular.cs");
            builder.Add(tree);
            tree = CSharpSyntaxTree.ParseText(string.Format(source, treeNum++), path: "AssemblyInfo.cs");
            builder.Add(tree);

            // Trees with generated code file names
            foreach (var fileName in generatedFileNames)
            {
                tree = CSharpSyntaxTree.ParseText(string.Format(source, treeNum++), path: fileName);
                builder.Add(tree);
            }

            var autoGeneratedPrefixes = new[] { @"// <auto-generated>", @"// <autogenerated>", @"/* <auto-generated> */" };

            for (var i = 0; i < autoGeneratedPrefixes.Length; i++)
            {
                // Tree with '<auto-generated>' comment
                var autoGeneratedPrefix = autoGeneratedPrefixes[i];
                tree = CSharpSyntaxTree.ParseText(string.Format(autoGeneratedPrefix + source, treeNum++), path: $"SourceFileWithAutoGeneratedComment{i++}.cs");
                builder.Add(tree);
            }

            // Verify no compiler diagnostics.
            var trees = builder.ToImmutable();
            var compilation = CreateCompilationWithMscorlib45(trees, new MetadataReference[] { SystemRef });
            compilation.VerifyDiagnostics();

            Func<string, bool> isGeneratedFile = fileName => fileName.Contains("SourceFileWithAutoGeneratedComment") || generatedFileNames.Contains(fileName);

            // (1) Verify default mode of analysis when there is no generated code configuration.
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, generatedCodeAnalysisFlagsOpt: null);

            // (2) Verify ConfigureGeneratedCodeAnalysis with different combinations of GeneratedCodeAnalysisFlags.
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, GeneratedCodeAnalysisFlags.None);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, AnalyzerDriver.DefaultGeneratedCodeAnalysisFlags);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, GeneratedCodeAnalysisFlags.Analyze);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, GeneratedCodeAnalysisFlags.ReportDiagnostics);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            // (4) Ensure warnaserror doesn't produce noise in generated files.
            var options = compilation.Options.WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            var warnAsErrorCompilation = compilation.WithOptions(options);
            VerifyGeneratedCodeAnalyzerDiagnostics(warnAsErrorCompilation, isGeneratedFile, generatedCodeAnalysisFlagsOpt: null);
        }

        [Fact, WorkItem(6998, "https://github.com/dotnet/roslyn/issues/6998")]
        public void TestGeneratedCodeAnalyzerPartialType()
        {
            string source = @"
[System.CodeDom.Compiler.GeneratedCodeAttribute(""tool"", ""version"")]
partial class PartialType
{
}

partial class PartialType
{
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "SourceFileRegular.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree }, new MetadataReference[] { SystemRef });
            compilation.VerifyDiagnostics();

            var builder = ArrayBuilder<DiagnosticDescription>.GetInstance();

            // Expected symbol diagnostics
            var squiggledText = "PartialType";
            var diagnosticArgument = squiggledText;
            var line = 3;
            var column = 15;
            AddExpectedLocalDiagnostics(builder, false, squiggledText, line, column, GeneratedCodeAnalysisFlags.ReportDiagnostics, diagnosticArgument);

            // Expected tree diagnostics
            squiggledText = "}";
            diagnosticArgument = tree.FilePath;
            line = 9;
            column = 1;
            AddExpectedLocalDiagnostics(builder, false, squiggledText, line, column, GeneratedCodeAnalysisFlags.ReportDiagnostics, diagnosticArgument);

            // Expected compilation diagnostics
            AddExpectedNonLocalDiagnostic(builder, "PartialType", compilation.SyntaxTrees[0].FilePath);

            var expected = builder.ToArrayAndFree();

            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, generatedCodeAnalysisFlagsOpt: null);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, GeneratedCodeAnalysisFlags.None);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, AnalyzerDriver.DefaultGeneratedCodeAnalysisFlags);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, GeneratedCodeAnalysisFlags.Analyze);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, GeneratedCodeAnalysisFlags.ReportDiagnostics);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        }

        [Fact, WorkItem(11217, "https://github.com/dotnet/roslyn/issues/11217")]
        public void TestGeneratedCodeAnalyzerNoReportDiagnostics()
        {
            string source1 = @"
class TypeInUserFile { }
";
            string source2 = @"
class TypeInGeneratedFile { }
";
            var tree1 = CSharpSyntaxTree.ParseText(source1, path: "SourceFileRegular.cs");
            var tree2 = CSharpSyntaxTree.ParseText(source2, path: "SourceFileRegular.Designer.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree1, tree2 }, new MetadataReference[] { SystemRef });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new GeneratedCodeAnalyzer2() };
            compilation.VerifyAnalyzerDiagnostics(analyzers,
                expected: Diagnostic("GeneratedCodeAnalyzer2Warning", "TypeInUserFile").WithArguments("TypeInUserFile", "2").WithLocation(2, 7));
        }

        internal class OwningSymbolTestAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor ExpressionDescriptor = new DiagnosticDescriptor(
                "Expression",
                "Expression",
                "Expression found.",
                "Testing",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(ExpressionDescriptor); }
            }

            public sealed override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(
                     (nodeContext) =>
                     {
                         if (nodeContext.ContainingSymbol.Name.StartsWith("Funky") && nodeContext.Compilation.Language == "C#")
                         {
                             nodeContext.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(ExpressionDescriptor, nodeContext.Node.GetLocation()));
                         }
                     },
                     SyntaxKind.IdentifierName,
                     SyntaxKind.NumericLiteralExpression);
            }
        }

        [Fact]
        public void OwningSymbolTest()
        {
            const string source = @"
class C
{
    public void UnFunkyMethod()
    {
        int x = 0;
        int y = x;
    }

    public void FunkyMethod()
    {
        int x = 0;
        int y = x;
    }

    public int FunkyField = 12;
    public int UnFunkyField = 12;
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new OwningSymbolTestAnalyzer() }, null, null, false,
                Diagnostic(OwningSymbolTestAnalyzer.ExpressionDescriptor.Id, "0").WithLocation(12, 17),
                Diagnostic(OwningSymbolTestAnalyzer.ExpressionDescriptor.Id, "x").WithLocation(13, 17),
                Diagnostic(OwningSymbolTestAnalyzer.ExpressionDescriptor.Id, "12").WithLocation(16, 29)
                );
        }

        private static void VerifyGeneratedCodeAnalyzerDiagnostics(Compilation compilation, Func<string, bool> isGeneratedFileName, GeneratedCodeAnalysisFlags? generatedCodeAnalysisFlagsOpt)
        {
            var expected = GetExpectedGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFileName, generatedCodeAnalysisFlagsOpt);
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, generatedCodeAnalysisFlagsOpt);
        }

        private static void VerifyGeneratedCodeAnalyzerDiagnostics(Compilation compilation, DiagnosticDescription[] expected, GeneratedCodeAnalysisFlags? generatedCodeAnalysisFlagsOpt)
        {
            var analyzers = new DiagnosticAnalyzer[] { new GeneratedCodeAnalyzer(generatedCodeAnalysisFlagsOpt) };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, logAnalyzerExceptionAsDiagnostics: false, expected: expected);
        }

        private static DiagnosticDescription[] GetExpectedGeneratedCodeAnalyzerDiagnostics(Compilation compilation, Func<string, bool> isGeneratedFileName, GeneratedCodeAnalysisFlags? generatedCodeAnalysisFlagsOpt)
        {
            var analyzers = new DiagnosticAnalyzer[] { new GeneratedCodeAnalyzer(generatedCodeAnalysisFlagsOpt) };
            var files = compilation.SyntaxTrees.Select(t => t.FilePath).ToImmutableArray();
            var sortedCallbackSymbolNames = new SortedSet<string>();
            var sortedCallbackTreePaths = new SortedSet<string>();
            var builder = ArrayBuilder<DiagnosticDescription>.GetInstance();
            for (int i = 0; i < compilation.SyntaxTrees.Count(); i++)
            {
                var file = files[i];
                var isGeneratedFile = isGeneratedFileName(file);

                // Type "GeneratedCode{0}"
                var squiggledText = string.Format("GeneratedCode{0}", i);
                var diagnosticArgument = squiggledText;
                var line = 3;
                var column = 7;
                var isGeneratedCode = true;
                AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument);

                // Type "Nested{0}"
                squiggledText = string.Format("Nested{0}", i);
                diagnosticArgument = squiggledText;
                line = 5;
                column = 19;
                isGeneratedCode = true;
                AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument);

                // Type "NonGeneratedCode{0}"
                squiggledText = string.Format("NonGeneratedCode{0}", i);
                diagnosticArgument = squiggledText;
                line = 8;
                column = 7;
                isGeneratedCode = isGeneratedFile;
                AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument);

                // Type "NestedGeneratedCode{0}"
                squiggledText = string.Format("NestedGeneratedCode{0}", i);
                diagnosticArgument = squiggledText;
                line = 11;
                column = 19;
                isGeneratedCode = true;
                AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument);

                // File diagnostic
                squiggledText = "}"; // last token in file.
                diagnosticArgument = file;
                line = 12;
                column = 1;
                isGeneratedCode = isGeneratedFile;
                AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument);

                // Compilation end summary diagnostic (verify callbacks into analyzer)
                // Analyzer always called for generated code, unless generated code analysis is explicitly disabled.
                if (generatedCodeAnalysisFlagsOpt == null || (generatedCodeAnalysisFlagsOpt & GeneratedCodeAnalysisFlags.Analyze) != 0)
                {
                    sortedCallbackSymbolNames.Add(string.Format("GeneratedCode{0}", i));
                    sortedCallbackSymbolNames.Add(string.Format("Nested{0}", i));
                    sortedCallbackSymbolNames.Add(string.Format("NonGeneratedCode{0}", i));
                    sortedCallbackSymbolNames.Add(string.Format("NestedGeneratedCode{0}", i));

                    sortedCallbackTreePaths.Add(file);
                }
                else if (!isGeneratedFile)
                {
                    // Analyzer always called for non-generated code.
                    sortedCallbackSymbolNames.Add(string.Format("NonGeneratedCode{0}", i));
                    sortedCallbackTreePaths.Add(file);
                }
            }

            // Compilation end summary diagnostic (verify callbacks into analyzer)
            var arg1 = sortedCallbackSymbolNames.Join(",");
            var arg2 = sortedCallbackTreePaths.Join(",");
            AddExpectedNonLocalDiagnostic(builder, arguments: new[] { arg1, arg2 });

            if (compilation.Options.GeneralDiagnosticOption == ReportDiagnostic.Error)
            {
                for (int i = 0; i < builder.Count; i++)
                {
                    if (((string)builder[i].Code) != GeneratedCodeAnalyzer.Error.Id)
                    {
                        builder[i] = builder[i].WithWarningAsError(true);
                    }
                }
            }

            return builder.ToArrayAndFree();
        }

        private static void AddExpectedLocalDiagnostics(
            ArrayBuilder<DiagnosticDescription> builder,
            bool isGeneratedCode,
            string squiggledText,
            int line,
            int column,
            GeneratedCodeAnalysisFlags? generatedCodeAnalysisFlagsOpt,
            params string[] arguments)
        {
            // Always report diagnostics in generated code, unless explicitly suppressed or we are not even analyzing generated code.
            var reportInGeneratedCode = generatedCodeAnalysisFlagsOpt == null ||
                ((generatedCodeAnalysisFlagsOpt & GeneratedCodeAnalysisFlags.ReportDiagnostics) != 0 &&
                 (generatedCodeAnalysisFlagsOpt & GeneratedCodeAnalysisFlags.Analyze) != 0);

            if (!isGeneratedCode || reportInGeneratedCode)
            {
                var diagnostic = Diagnostic(GeneratedCodeAnalyzer.Warning.Id, squiggledText).WithArguments(arguments).WithLocation(line, column);
                builder.Add(diagnostic);

                diagnostic = Diagnostic(GeneratedCodeAnalyzer.Error.Id, squiggledText).WithArguments(arguments).WithLocation(line, column);
                builder.Add(diagnostic);
            }
        }

        private static void AddExpectedNonLocalDiagnostic(ArrayBuilder<DiagnosticDescription> builder, params string[] arguments)
        {
            AddExpectedDiagnostic(builder, GeneratedCodeAnalyzer.Summary.Id, squiggledText: null, line: 1, column: 1, arguments: arguments);
        }

        private static void AddExpectedDiagnostic(ArrayBuilder<DiagnosticDescription> builder, string diagnosticId, string squiggledText, int line, int column, params string[] arguments)
        {
            var diagnostic = Diagnostic(diagnosticId, squiggledText).WithArguments(arguments).WithLocation(line, column);
            builder.Add(diagnostic);
        }

        [Fact]
        public void TestEnsureNoMergedNamespaceSymbolAnalyzer()
        {
            var source = @"namespace N1.N2 { }";

            var metadataReference = CreateCompilationWithMscorlib(source).ToMetadataReference();
            var compilation = CreateCompilationWithMscorlib(source, new[] { metadataReference });
            compilation.VerifyDiagnostics();

            // Analyzer reports a diagnostic if it receives a merged namespace symbol across assemblies in compilation.
            var analyzers = new DiagnosticAnalyzer[] { new EnsureNoMergedNamespaceSymbolAnalyzer() };
            compilation.VerifyAnalyzerDiagnostics(analyzers);
        }

        [Fact, WorkItem(6324, "https://github.com/dotnet/roslyn/issues/6324")]
        public void TestSharedStateAnalyzer()
        {
            string source1 = @"
public partial class C { }
";
            string source2 = @"
public partial class C2 { }
";
            string source3 = @"
public partial class C33 { }
";
            var tree1 = CSharpSyntaxTree.ParseText(source1, path: "Source1_File1.cs");
            var tree2 = CSharpSyntaxTree.ParseText(source1, path: "Source1_File2.cs");
            var tree3 = CSharpSyntaxTree.ParseText(source2, path: "Source2_File3.cs");
            var tree4 = CSharpSyntaxTree.ParseText(source3, path: "Source3_File4.generated.cs");
            var tree5 = CSharpSyntaxTree.ParseText(source3, path: "Source3_File5.designer.cs");

            var compilation = CreateCompilationWithMscorlib45(new[] { tree1, tree2, tree3, tree4, tree5 });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new SharedStateAnalyzer() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                Diagnostic("UserCodeDiagnostic").WithArguments("Source1_File1.cs").WithLocation(1, 1),
                Diagnostic("UniqueTextFileDiagnostic").WithArguments("Source1_File1.cs").WithLocation(1, 1),
                Diagnostic("GeneratedCodeDiagnostic", "C33").WithArguments("C33").WithLocation(2, 22),
                Diagnostic("UserCodeDiagnostic", "C2").WithArguments("C2").WithLocation(2, 22),
                Diagnostic("UserCodeDiagnostic", "C").WithArguments("C").WithLocation(2, 22),
                Diagnostic("UserCodeDiagnostic").WithArguments("Source1_File2.cs").WithLocation(1, 1),
                Diagnostic("UniqueTextFileDiagnostic").WithArguments("Source1_File2.cs").WithLocation(1, 1),
                Diagnostic("UserCodeDiagnostic").WithArguments("Source2_File3.cs").WithLocation(1, 1),
                Diagnostic("UniqueTextFileDiagnostic").WithArguments("Source2_File3.cs").WithLocation(1, 1),
                Diagnostic("GeneratedCodeDiagnostic").WithArguments("Source3_File4.generated.cs").WithLocation(1, 1),
                Diagnostic("UniqueTextFileDiagnostic").WithArguments("Source3_File4.generated.cs").WithLocation(1, 1),
                Diagnostic("GeneratedCodeDiagnostic").WithArguments("Source3_File5.designer.cs").WithLocation(1, 1),
                Diagnostic("UniqueTextFileDiagnostic").WithArguments("Source3_File5.designer.cs").WithLocation(1, 1),
                Diagnostic("NumberOfUniqueTextFileDescriptor").WithArguments("3").WithLocation(1, 1));
        }

        [Fact, WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_InConstructor()
        {
            string source = @"
public class C
{
    public C(int a, int b)
    {
    }
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("Parameter_ID", "a").WithLocation(4, 18),
                    Diagnostic("Parameter_ID", "b").WithLocation(4, 25));
        }

        [Fact, WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_InRegularMethod()
        {
            string source = @"
public class C
{
    void M1(string a, string b)
    {
    }
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("Parameter_ID", "a").WithLocation(4, 20),
                    Diagnostic("Parameter_ID", "b").WithLocation(4, 30));
        }

        [Fact, WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_InIndexers()
        {
            string source = @"
public class C
{
    public int this[int index]
    {
        get { return 0; }
        set { }
    }
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("Parameter_ID", "index").WithLocation(4, 25));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14061"), WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_Lambdas()
        {
            string source = @"
public class C
{
    void M2()
    {
        System.Func<int, int, int> x = (int a, int b) => b;
    }
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("Local_ID", "x").WithLocation(6, 36),
                    Diagnostic("Parameter_ID", "a").WithLocation(6, 45),
                    Diagnostic("Parameter_ID", "b").WithLocation(6, 52));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14061"), WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_InAnonymousMethods()
        {
            string source = @"
public class C
{
    void M3()
    {
        M4(delegate (int x, int y) { });
    }

    void M4(System.Action<int, int> a) { }
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                        Diagnostic("Parameter_ID", "a").WithLocation(9, 37),
                        Diagnostic("Parameter_ID", "x").WithLocation(6, 26),
                        Diagnostic("Parameter_ID", "y").WithLocation(6, 33));
        }

        [Fact, WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_InDelegateTypes()
        {
            string source = @"
public class C
{
    delegate void D(int x, string y);
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("Parameter_ID", "x").WithLocation(4, 25),
                    Diagnostic("Parameter_ID", "y").WithLocation(4, 35));
        }

        [Fact, WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_InOperators()
        {
            string source = @"
public class C
{
    public static implicit operator int (C c) { return 0; }
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("Parameter_ID", "c").WithLocation(4, 44));
        }

        [Fact, WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_InExplicitInterfaceImplementations()
        {
            string source = @"
interface I
{
    void M(int a, int b);
}

public class C : I
{
    void I.M(int c, int d) { }
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("Parameter_ID", "c").WithLocation(9, 18),
                    Diagnostic("Parameter_ID", "d").WithLocation(9, 25),
                    Diagnostic("Parameter_ID", "a").WithLocation(4, 16),
                    Diagnostic("Parameter_ID", "b").WithLocation(4, 23));
        }

        [Fact, WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_InExtensionMethods()
        {
            string source = @"
public static class C
{
    static void M(this int x, int y) { }
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("Parameter_ID", "x").WithLocation(4, 28),
                    Diagnostic("Parameter_ID", "y").WithLocation(4, 35));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14061"), WorkItem(8753, "https://github.com/dotnet/roslyn/issues/8753")]
        public void TestParametersAnalyzer_InLocalFunctions()
        {
            string source = @"
public class C
{
    void M1() 
    { 
        M2(1, 2);

        void M2(int a, int b)
        {
        }
    }
}
";
            var tree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzers = new DiagnosticAnalyzer[] { new AnalyzerForParameters() };
            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("Parameter_ID", "a").WithLocation(4, 18), // ctor
                    Diagnostic("Parameter_ID", "b").WithLocation(4, 25),
                    Diagnostic("Local_ID", "c").WithLocation(6, 13),
                    Diagnostic("Local_ID", "d").WithLocation(6, 20),
                    Diagnostic("Parameter_ID", "a").WithLocation(10, 20), // M1
                    Diagnostic("Parameter_ID", "b").WithLocation(10, 30),
                    Diagnostic("Local_ID", "c").WithLocation(12, 11),
                    Diagnostic("Local_ID", "x").WithLocation(18, 36), // M2
                    Diagnostic("Parameter_ID", "a").WithLocation(26, 37), // M4
                    Diagnostic("Parameter_ID", "index").WithLocation(28, 25)); // indexer
        }
    }
}
