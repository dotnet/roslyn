// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Naming;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.HardeningAnalyzer
{
    public class HardeningAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            throw new NotImplementedException();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
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
            Assert.Equal("info AD0001: The Compiler Analyzer '" + GetCSharpDiagnosticAnalyzer().GetType() + "' threw an exception with message 'The method or operation is not implemented.'.", diagnostics[0].ToString());
        }

#region "Test_Class"
        private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(CA1715DiagnosticAnalyzer.InterfaceRule, CA1715DiagnosticAnalyzer.TypeParameterRule);

        [DiagnosticAnalyzer]
        internal class ExceptionThrowingSymbolAnalyzer_ThrowSymbolKindsOfInterest : DiagnosticAnalyzer
        {
            private SymbolKind[] SymbolKindsOfInterest
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return SupportedRules;
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(
                    (symbolContext) => { },
                    SymbolKindsOfInterest);
            }
        }
#endregion
    }
}
