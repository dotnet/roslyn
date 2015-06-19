// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public partial class AvoidUnsealedAttributeFixerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new AvoidUnsealedAttributesAnalyzer();
        }

        [WorkItem(858655)]
        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new AvoidUnsealedAttributesFixer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AvoidUnsealedAttributesAnalyzer();
        }

        [WorkItem(858655)]
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AvoidUnsealedAttributesFixer();
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
