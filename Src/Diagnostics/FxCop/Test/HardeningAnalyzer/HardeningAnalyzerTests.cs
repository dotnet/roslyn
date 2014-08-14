// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.HardeningAnalyzer
{
    public class HardeningAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        protected override IDiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            throw new NotImplementedException();
        }

        protected override IDiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ExceptionThrowingSymbolAnalyzer_ThrowSymbolKindsOfInterest();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestTypeParameterNamesCSharp()
        {
            var source = @"
public class Class5<_Type1>
{
    public void Method<_K, _V>(_K key, _V value)
    {
        Console.WriteLine(key.ToString() + value.ToString());
    }
}

public class Class6<TTypeParameter>
{
}
";
            var diagnosticsBag = DiagnosticBag.GetInstance();
            var documentsAndSpan = GetDocumentsAndSpans(new[] { source }, LanguageNames.CSharp);
            AnalyzeDocumentCore(GetCSharpDiagnosticAnalyzer(), documentsAndSpan.Item1[0], diagnosticsBag.Add, null, continueOnAnalyzerException: DiagnosticExtensions.AlwaysCatchAnalyzerExceptions);
            var diagnostics = diagnosticsBag.ToReadOnlyAndFree();
            Assert.True(diagnostics.Length > 0);
            Assert.Equal(diagnostics[0].ToString(), "info AnalyzerDriver: The Compiler Analyzer '" + GetCSharpDiagnosticAnalyzer().GetType() + "' threw an exception with message 'The method or operation is not implemented.'.");
        }

#region "Test_Class"
        internal const string RuleId = "CA1715_Test";
        internal static readonly DiagnosticDescriptor InterfaceRule = new DiagnosticDescriptor(RuleId,
                                                                                      FxCopRulesResources.InterfaceNamesShouldStartWithI,
                                                                                      FxCopRulesResources.InterfaceNamesShouldStartWithI,
                                                                                      FxCopDiagnosticCategory.Naming,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: true);
        internal static readonly DiagnosticDescriptor TypeParameterRule = new DiagnosticDescriptor(RuleId,
                                                                                      FxCopRulesResources.TypeParameterNamesShouldStartWithT,
                                                                                      FxCopRulesResources.TypeParameterNamesShouldStartWithT,
                                                                                      FxCopDiagnosticCategory.Naming,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: true);
        private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(InterfaceRule, TypeParameterRule);

        [DiagnosticAnalyzer]
        [ExportDiagnosticAnalyzer(LanguageNames.CSharp)]
        internal class ExceptionThrowingSymbolAnalyzer_ThrowSymbolKindsOfInterest : ISymbolAnalyzer
        {
            public ImmutableArray<SymbolKind> SymbolKindsOfInterest
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return SupportedRules;
                }
            }
        }
#endregion
    }
}
