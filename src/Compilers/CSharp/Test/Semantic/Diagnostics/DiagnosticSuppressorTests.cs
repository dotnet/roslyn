// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticSuppressorTests : CompilingTestBase
    {
        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_CompilerSyntaxWarning()
        {
            // NOTE: Empty switch block warning is reported by the C# language parser
            string source = @"
class C
{
    void M(int i)
    {
        switch (i)
        {
        }
    }
}";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            var expectedDiagnostic =
                // (7,9): warning CS1522: Empty switch block
                //         {
                Diagnostic(ErrorCode.WRN_EmptySwitch, "{").WithLocation(7, 9);

            compilation.VerifyDiagnostics(expectedDiagnostic);

            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS1522") };
            compilation.VerifySuppressedDiagnostics(analyzers, null, null, true, expectedDiagnostic);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_CompilerSemanticWarning()
        {
            string source = @"
class C
{
    // warning CS0169: The field 'C.f' is never used
    private readonly int f;
}";
            var expectedDiagnostic =
                // (5,26): warning CS0169: The field 'C.f' is never used
                //     private readonly int f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C.f").WithLocation(5, 26);

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics(expectedDiagnostic);

            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS0169") };
            compilation.VerifySuppressedDiagnostics(analyzers, null, null, true, expectedDiagnostic);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestNoSuppression_CompilerSyntaxError()
        {
            string source = @"
class { }";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });

            compilation.VerifyDiagnostics(
                // (2,7): error CS1001: Identifier expected
                // class { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 7));

            // Verify compiler syntax error cannot be suppressed.
            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS1001") };
            compilation.VerifySuppressedDiagnostics(analyzers);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestNoSuppression_CompilerSemanticError()
        {
            string source = @"
class C
{
    void M(UndefinedType x) { }
}";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });

            compilation.VerifyDiagnostics(
                // (4,12): error CS0246: The type or namespace name 'UndefinedType' could not be found (are you missing a using directive or an assembly reference?)
                //     void M(UndefinedType x) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "UndefinedType").WithArguments("UndefinedType").WithLocation(4, 12));

            // Verify compiler semantic error cannot be suppressed.
            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS0246") };
            compilation.VerifySuppressedDiagnostics(analyzers);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_MultipleDiagnostics()
        {
            string source = @"
class C
{
    // warning CS0169: The field 'C.f' is never used
    private readonly int f;
    // warning CS0169: The field 'C.f2' is never used
    private readonly int f2;
}";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (5,26): warning CS0169: The field 'C.f' is never used
                //     private readonly int f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C.f").WithLocation(5, 26),
                // (7,26): warning CS0169: The field 'C.f2' is never used
                //     private readonly int f2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f2").WithArguments("C.f2").WithLocation(7, 26)
            };

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics(expectedDiagnostics);

            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS0169") };
            compilation.VerifySuppressedDiagnostics(analyzers, null, null, true, expectedDiagnostics);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_MultipleSuppressors_SameDiagnostic()
        {
            string source = @"
class C
{
    // warning CS0169: The field 'C.f' is never used
    private readonly int f;
}";
            var expectedDiagnostic =
                // (5,26): warning CS0169: The field 'C.f' is never used
                //     private readonly int f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C.f").WithLocation(5, 26);

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics(expectedDiagnostic);

            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS0169"), new DiagnosticSuppressorForId("CS0169") };
            compilation.VerifySuppressedDiagnostics(analyzers, null, null, true, expectedDiagnostic);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_MultipleSuppressors_DifferentDiagnostic()
        {
            string source = @"
class C
{
    // warning CS0169: The field 'C.f' is never used
    private readonly int f;
}";
            var expectedCompilerDiagnostic =
                // (5,26): warning CS0169: The field 'C.f' is never used
                //     private readonly int f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C.f").WithLocation(5, 26);

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics(expectedCompilerDiagnostic);

            var expectedAnalyzerDiagnostic =
                Diagnostic("ID1000", @"class C
{
    // warning CS0169: The field 'C.f' is never used
    private readonly int f;
}").WithLocation(2, 1);

            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            compilation.VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { analyzer }, null, null, true,
                expectedAnalyzerDiagnostic);

            var suppressor1 = new DiagnosticSuppressorForId("CS0169");
            var suppresor2 = new DiagnosticSuppressorForId(analyzer.Descriptor.Id);

            var analyzers = new DiagnosticAnalyzer[] { analyzer, suppressor1, suppresor2 };
            compilation.VerifySuppressedDiagnostics(analyzers, null, null, true,
                expectedCompilerDiagnostic,
                expectedAnalyzerDiagnostic);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestNoSuppression_SpecificOptionsTurnsOffSuppressor()
        {
            string source = @"
class C
{
    // warning CS0169: The field 'C.f' is never used
    private readonly int f;
}";
            var expectedDiagnostic =
                // (5,26): warning CS0169: The field 'C.f' is never used
                //     private readonly int f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("C.f").WithLocation(5, 26);

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics(expectedDiagnostic);

            const string suppressionId = "SPR1001";
            var analyzers = new DiagnosticAnalyzer[] { new DiagnosticSuppressorForId("CS0169", suppressionId) };
            compilation.VerifySuppressedDiagnostics(analyzers, null, null, true, expectedDiagnostic);

            var specificDiagnosticOptions = compilation.Options.SpecificDiagnosticOptions.Add(suppressionId, ReportDiagnostic.Suppress);
            compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(specificDiagnosticOptions));
            compilation.VerifySuppressedDiagnostics(analyzers);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestSuppression_AnalyzerDiagnostics_SeveritiesAndConfigurableMatrix()
        {
            string source = @"
class C { }";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var configurations = new[] { false, true };
            var severities = Enum.GetValues(typeof(DiagnosticSeverity));
            var originalSpecificDiagnosticOptions = compilation.Options.SpecificDiagnosticOptions;

            foreach (var configurable in configurations)
            {
                foreach (DiagnosticSeverity defaultSeverity in severities)
                {
                    foreach (DiagnosticSeverity effectiveSeverity in severities)
                    {
                        var diagnostic = Diagnostic("ID1000", "class C { }")
                                            .WithLocation(2, 1)
                                            .WithDefaultSeverity(defaultSeverity)
                                            .WithEffectiveSeverity(configurable ? effectiveSeverity : defaultSeverity);

                        if (defaultSeverity == DiagnosticSeverity.Warning &&
                            effectiveSeverity == DiagnosticSeverity.Error &&
                            configurable)
                        {
                            diagnostic = diagnostic.WithWarningAsError(true);
                        }

                        var analyzer = new CompilationAnalyzerWithSeverity(defaultSeverity, configurable);
                        var suppressor = new DiagnosticSuppressorForId(analyzer.Descriptor.Id);
                        var analyzersWithoutSuppressor = new DiagnosticAnalyzer[] { analyzer };
                        var analyzersWithSuppressor = new DiagnosticAnalyzer[] { analyzer, suppressor };

                        var specificDiagnosticOptions = originalSpecificDiagnosticOptions.Add(
                            key: analyzer.Descriptor.Id,
                            value: DiagnosticDescriptor.MapSeverityToReport(effectiveSeverity));
                        compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(specificDiagnosticOptions));
                        
                        // Verify analyzer diagnostic without suppressor, also verify no suppressions.
                        compilation.VerifyAnalyzerDiagnostics(analyzersWithoutSuppressor, null, null, true, diagnostic);
                        compilation.VerifySuppressedDiagnostics(analyzersWithoutSuppressor);

                        // Verify suppressed analyzer diagnostic, except when default severity is Error or diagnostic is not-configurable.
                        if (defaultSeverity == DiagnosticSeverity.Error || !configurable)
                        {
                            compilation.VerifySuppressedDiagnostics(analyzersWithSuppressor);
                        }
                        else
                        {
                            compilation.VerifySuppressedDiagnostics(analyzersWithSuppressor, null, null, true, diagnostic);
                        }
                    }
                }
            }
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestExceptionFromSuppressor()
        {
            string source = @"
class C { }";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            testCore(new DiagnosticSuppressorThrowsExceptionFromSupportedSuppressions());
            testCore(new DiagnosticSuppressorThrowsExceptionFromReportedSuppressions(analyzer.Descriptor.Id));
            return;

            // Local functions.
            void testCore(DiagnosticSuppressor suppressor)
            {
                var analyzers = new DiagnosticAnalyzer[] { analyzer, suppressor };
                compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                    Diagnostic("AD0001").WithArguments(suppressor.ToString(),
                                                       typeof(NotImplementedException).FullName,
                                                       "The method or operation is not implemented.")
                                        .WithLocation(1, 1),
                    Diagnostic("ID1000", "class C { }").WithLocation(2, 1));

                compilation.VerifySuppressedDiagnostics(analyzers);
            }
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestUnsupportedSuppressionReported()
        {
            string source = @"
class C { }";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            const string supportedSuppressionId = "supportedId";
            const string unsupportedSuppressionId = "unsupportedId";
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var suppressor = new DiagnosticSuppressor_UnsupportedSuppressionReported(analyzer.Descriptor.Id, supportedSuppressionId, unsupportedSuppressionId);
            var analyzers = new DiagnosticAnalyzer[] { analyzer, suppressor };

            // "Reported suppression with ID '{0}' is not supported by the suppressor."
            var exceptionMessage = string.Format(CodeAnalysisResources.UnsupportedSuppressionReported, unsupportedSuppressionId);

            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                Diagnostic("AD0001").WithArguments(suppressor.ToString(),
                                                   typeof(ArgumentException).FullName,
                                                   exceptionMessage)
                                    .WithLocation(1, 1),
                Diagnostic("ID1000", "class C { }").WithLocation(2, 1));

            compilation.VerifySuppressedDiagnostics(analyzers);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestInvalidDiagnosticSuppressionReported()
        {
            string source = @"
class C { }";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            const string unsupportedSuppressedId = "UnsupportedId";
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var suppressor = new DiagnosticSuppressor_InvalidDiagnosticSuppressionReported(analyzer.Descriptor.Id, unsupportedSuppressedId);
            var analyzers = new DiagnosticAnalyzer[] { analyzer, suppressor };

            // "Suppressed diagnostic ID '{0}' does not match suppressable ID '{1}' for the given suppression descriptor."
            var exceptionMessage = string.Format(CodeAnalysisResources.InvalidDiagnosticSuppressionReported, analyzer.Descriptor.Id, unsupportedSuppressedId);

            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                Diagnostic("AD0001").WithArguments(suppressor.ToString(),
                                                   typeof(System.ArgumentException).FullName,
                                                   exceptionMessage)
                                    .WithLocation(1, 1),
                Diagnostic("ID1000", "class C { }").WithLocation(2, 1));

            compilation.VerifySuppressedDiagnostics(analyzers);
        }

        [Fact, WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        public void TestNonReportedDiagnosticCannotBeSuppressed()
        {
            string source = @"
class C { }";

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree });
            compilation.VerifyDiagnostics();

            const string nonReportedDiagnosticId = "NonReportedId";
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var suppressor = new DiagnosticSuppressor_NonReportedDiagnosticCannotBeSuppressed(analyzer.Descriptor.Id, nonReportedDiagnosticId);
            var analyzers = new DiagnosticAnalyzer[] { analyzer, suppressor };

            // "Non-reported diagnostic with ID '{0}' cannot be suppressed."
            var exceptionMessage = string.Format(CodeAnalysisResources.NonReportedDiagnosticCannotBeSuppressed, nonReportedDiagnosticId);

            compilation.VerifyAnalyzerDiagnostics(analyzers, null, null, true,
                Diagnostic("AD0001").WithArguments(suppressor.ToString(),
                                                   typeof(System.ArgumentException).FullName,
                                                   exceptionMessage)
                                    .WithLocation(1, 1),
                Diagnostic("ID1000", "class C { }").WithLocation(2, 1));

            compilation.VerifySuppressedDiagnostics(analyzers);
        }
    }
}
