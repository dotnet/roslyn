// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public partial class AvoidUnsealedAttributeTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new AvoidUnsealedAttributesAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AvoidUnsealedAttributesAnalyzer();
        }

        #region Diagnostic Tests

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1813CSharpDiagnosticProviderTestFired()
        {
            VerifyCSharp(@"
using System;

public class C
{
    public class AttributeClass: Attribute
    {
    }

    private class AttributeClass2: Attribute
    {
    }
}
",
            GetCA1813CSharpResultAt(6, 18),
            GetCA1813CSharpResultAt(10, 19));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1813CSharpDiagnosticProviderTestFiredWithScope()
        {
            VerifyCSharp(@"
using System;

[|public class AttributeClass: Attribute
{
}|]

private class AttributeClass2: Attribute
{
}
",
            GetCA1813CSharpResultAt(4, 14));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1813CSharpDiagnosticProviderTestNotFired()
        {
            VerifyCSharp(@"
using System;

public sealed class AttributeClass: Attribute
{
    private abstract class AttributeClass2: Attribute
    {
        public abstract void F();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1813VisualBasicDiagnosticProviderTestFired()
        {
            VerifyBasic(@"
Imports System

Public Class AttributeClass
    Inherits Attribute
End Class

Private Class AttributeClass2
    Inherits Attribute
End Class
",
            GetCA1813BasicResultAt(4, 14),
            GetCA1813BasicResultAt(8, 15));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1813VisualBasicDiagnosticProviderTestFiredwithScope()
        {
            VerifyBasic(@"
Imports System

Public Class AttributeClass
    Inherits Attribute
End Class

[|Private Class AttributeClass2
    Inherits Attribute
End Class|]
",
            GetCA1813BasicResultAt(8, 15));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1813VisualBasicDiagnosticProviderTestNotFired()
        {
            VerifyBasic(@"
Imports System

Public NotInheritable Class AttributeClass
    Inherits Attribute

    Private MustInherit Class AttributeClass2
        Inherits Attribute
        MustOverride Sub F()
    End Class
End Class
");
        }

        #endregion

        internal static string CA1813Name = "CA1813";

        private static DiagnosticResult GetCA1813CSharpResultAt(int line, int column)
        {
            return GetCSharpResultAt(line, column, CA1813Name, SystemRuntimeAnalyzersResources.SealAttributeTypesForImprovedPerf);
        }

        private static DiagnosticResult GetCA1813BasicResultAt(int line, int column)
        {
            return GetBasicResultAt(line, column, CA1813Name, SystemRuntimeAnalyzersResources.SealAttributeTypesForImprovedPerf);
        }
    }
}
