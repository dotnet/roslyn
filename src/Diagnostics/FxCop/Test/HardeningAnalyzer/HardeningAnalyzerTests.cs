// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AnalyzerPowerPack.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AnalyzerPowerPack.UnitTests.HardeningAnalyzer
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
        [WorkItem(759)]
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
            var diagnostics = new List<Diagnostic>();
            var documentsAndSpan = GetDocumentsAndSpans(new[] { source }, LanguageNames.CSharp);
            AnalyzeDocumentCore(GetCSharpDiagnosticAnalyzer(), documentsAndSpan.Item1[0], diagnostics.Add, null, logAnalyzerExceptionAsDiagnostics: true);
            Assert.True(diagnostics.Count > 0);
            Assert.Equal(
                $"info AD0001: The Compiler Analyzer '{GetCSharpDiagnosticAnalyzer().GetType()}' threw an exception of type 'System.NotImplementedException' with message 'The method or operation is not implemented.'.",
                (new DiagnosticFormatter()).Format(diagnostics[0], EnsureEnglishUICulture.PreferredOrNull));
        }

        #region "Test_Class"
        private static readonly ImmutableArray<DiagnosticDescriptor> s_supportedRules = ImmutableArray.Create(CA1715DiagnosticAnalyzer.InterfaceRule, CA1715DiagnosticAnalyzer.TypeParameterRule);

        [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        internal class ExceptionThrowingSymbolAnalyzer_ThrowSymbolKindsOfInterest : DiagnosticAnalyzer
        {
            private SymbolKind[] SymbolKindsOfInterest
            {
                get
                {
                    using (new EnsureEnglishUICulture())
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return s_supportedRules;
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
