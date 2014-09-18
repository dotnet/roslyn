// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Performance;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Performance;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Performance;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class CA1813FixerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new CA1813DiagnosticAnalyzer();
        }

        [WorkItem(858655)]
        protected override ICodeFixProvider GetBasicCodeFixProvider()
        {
            return new CA1813BasicCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CA1813DiagnosticAnalyzer();
        }

        [WorkItem(858655)]
        protected override ICodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CA1813CSharpCodeFixProvider();
        }

        #region CodeFix Tests

        [Fact(Skip = "Bug 858655"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1813CSharpCodeFixProviderTestFired()
        {
            VerifyCSharpFix(@"
public class AttributeClass : Attribute
{
}", @"
public sealed class AttributeClass : Attribute
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1813VisualBasicCodeFixProviderTestFired()
        {
            VerifyBasicFix(@"
Imports System

Public Class AttributeClass
    Inherits Attribute
End Class", @"
Imports System

Public NotInheritable Class AttributeClass
    Inherits Attribute
End Class");
        }

        #endregion
    }
}
