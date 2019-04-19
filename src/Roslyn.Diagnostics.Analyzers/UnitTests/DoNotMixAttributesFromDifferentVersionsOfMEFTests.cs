// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.DoNotMixAttributesFromDifferentVersionsOfMEFAnalyzer,
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpDoNotMixAttributesFromDifferentVersionsOfMEFFixer>;
using VerifyVB = Microsoft.CodeAnalysis.VisualBasic.Testing.XUnit.CodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.DoNotMixAttributesFromDifferentVersionsOfMEFAnalyzer,
    Roslyn.Diagnostics.VisualBasic.Analyzers.BasicDoNotMixAttributesFromDifferentVersionsOfMEFFixer>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class DoNotMixAttributesFromDifferentVersionsOfMEFTests
    {
        private const string CSharpWellKnownAttributesDefinition = @"
namespace System.Composition
{
    public class ExportAttribute : System.Attribute
    {
        public ExportAttribute(System.Type contractType){ }
    }

    public class MetadataAttributeAttribute : System.Attribute
    {
        public MetadataAttributeAttribute() { }
    }

    public class ImportAttribute : System.Attribute
    {
        public ImportAttribute() { }
    }

    public class ImportingConstructorAttribute : System.Attribute
    {
        public ImportingConstructorAttribute() { }
    }
}

[System.Composition.MetadataAttribute]
public class SystemCompositionMetadataAttribute : System.Attribute
{
    public class ExportAttribute : System.Attribute
    {
        public ExportAttribute(System.Type contractType){ }
    }

    public class MetadataAttributeAttribute : System.Attribute
    {
        public MetadataAttributeAttribute() { }
    }

    public class ImportAttribute : System.Attribute
    {
        public ImportAttribute() { }
    }

    public class ImportingConstructorAttribute : System.Attribute
    {
        public ImportingConstructorAttribute() { }
    }
}

namespace System.ComponentModel.Composition
{
    public class ExportAttribute : System.Attribute
    {
        public ExportAttribute(System.Type contractType){ }
    }

    public class MetadataAttributeAttribute : System.Attribute
    {
        public MetadataAttributeAttribute() { }
    }

    public class ImportAttribute : System.Attribute
    {
        public ImportAttribute() { }
    }

    public class ImportingConstructorAttribute : System.Attribute
    {
        public ImportingConstructorAttribute() { }
    }
}

[System.ComponentModel.Composition.MetadataAttribute]
public class SystemComponentModelCompositionMetadataAttribute : System.Attribute
{
}
";
        private const string BasicWellKnownAttributesDefinition = @"
Namespace System.Composition
	Public Class ExportAttribute
		Inherits System.Attribute
		Public Sub New(contractType As System.Type)
		End Sub
	End Class

	Public Class MetadataAttributeAttribute
		Inherits System.Attribute
		Public Sub New()
		End Sub
	End Class

    Public Class ImportAttribute
	    Inherits System.Attribute
	    Public Sub New()
	    End Sub
    End Class

    Public Class ImportingConstructorAttribute
	    Inherits System.Attribute
	    Public Sub New()
	    End Sub
    End Class
End Namespace

<System.Composition.MetadataAttribute> _
Public Class SystemCompositionMetadataAttribute
	Inherits System.Attribute
End Class

Namespace System.ComponentModel.Composition
	Public Class ExportAttribute
		Inherits System.Attribute
		Public Sub New(contractType As System.Type)
		End Sub
	End Class

    Public Class MetadataAttributeAttribute
		Inherits System.Attribute
		Public Sub New()
		End Sub
	End Class

        Public Class ImportAttribute
	    Inherits System.Attribute
	    Public Sub New()
	    End Sub
    End Class

    Public Class ImportingConstructorAttribute
	    Inherits System.Attribute
	    Public Sub New()
	    End Sub
    End Class
End Namespace

<System.ComponentModel.Composition.MetadataAttribute> _
Public Class SystemComponentModelCompositionMetadataAttribute
	Inherits System.Attribute
End Class
";

        #region No Diagnostic Tests

        [Fact]
        public async Task NoDiagnosticCases_SingleMefAttribute()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

[System.Composition.Export(typeof(C))]
public class C
{
}

[System.ComponentModel.Composition.Export(typeof(C2))]
public class C2
{
}
" + CSharpWellKnownAttributesDefinition);

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System

<System.Composition.Export(GetType(C))> _
Public Class C
End Class

<System.ComponentModel.Composition.Export(GetType(C2))> _
Public Class C2
End Class
" + BasicWellKnownAttributesDefinition);
        }

        [Fact]
        public async Task NoDiagnosticCases_SingleMefAttributeAndValidMetadataAttribute()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

[System.Composition.Export(typeof(C))]
[SystemCompositionMetadataAttribute]
public class C
{
}

[System.ComponentModel.Composition.Export(typeof(C2))]
[SystemComponentModelCompositionMetadataAttribute]
public class C2
{
}
" + CSharpWellKnownAttributesDefinition);

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System

<System.Composition.Export(GetType(C))> _
<SystemCompositionMetadataAttribute> _
Public Class C
End Class

<System.ComponentModel.Composition.Export(GetType(C2))> _
<SystemComponentModelCompositionMetadataAttribute> _
Public Class C2
End Class
" + BasicWellKnownAttributesDefinition);
        }

        [Fact]
        public async Task NoDiagnosticCases_SingleMefAttributeAndAnotherExportAttribute()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

[System.Composition.Export(typeof(C)), MyNamespace.Export(typeof(C))]
public class C
{
}

[System.ComponentModel.Composition.Export(typeof(C2)), MyNamespace.Export(typeof(C2))]
public class C2
{
}

namespace MyNamespace
{
    public class ExportAttribute : System.Attribute
    {
        public ExportAttribute(System.Type contractType){ }
    }
}
" + CSharpWellKnownAttributesDefinition);

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System

<System.Composition.Export(GetType(C)), MyNamespace.Export(GetType(C))> _
Public Class C
End Class

<System.ComponentModel.Composition.Export(GetType(C2)), MyNamespace.Export(GetType(C2))> _
Public Class C2
End Class

Namespace MyNamespace
	Public Class ExportAttribute
		Inherits System.Attribute
		Public Sub New(contractType As System.Type)
		End Sub
	End Class
End Namespace
" + BasicWellKnownAttributesDefinition);
        }

        [Fact]
        public async Task NoDiagnosticCases_SingleMefAttributeOnTypeAndValidMefAttributeOnMember()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

public class B { }

[System.Composition.Export(typeof(C))]
public class C
{
    [System.Composition.ImportingConstructor]
    public C([System.Composition.Import]B b) { }

    [System.Composition.Import]
    public B PropertyB { get; }
}

[System.ComponentModel.Composition.Export(typeof(C2))]
public class C2
{
    [System.ComponentModel.Composition.ImportingConstructor]
    public C2([System.ComponentModel.Composition.Import]B b) { }

    [System.ComponentModel.Composition.Import]
    public B PropertyB { get; }
}
" + CSharpWellKnownAttributesDefinition);

            await VerifyVB.VerifyAnalyzerAsync(@"
Public Class B
End Class

<System.Composition.Export(GetType(C))> _
Public Class C
	<System.Composition.ImportingConstructor> _
	Public Sub New(<System.Composition.Import> b As B)
	End Sub

	<System.Composition.Import> _
	Public ReadOnly Property PropertyB() As B
End Class

<System.ComponentModel.Composition.Export(GetType(C2))> _
Public Class C2
	<System.ComponentModel.Composition.ImportingConstructor> _
	Public Sub New(<System.ComponentModel.Composition.Import> b As B)
	End Sub

	<System.ComponentModel.Composition.Import> _
	Public ReadOnly Property PropertyB() As B
End Class
" + BasicWellKnownAttributesDefinition);
        }

        [Fact]
        public async Task NoDiagnosticCases_UnresolvedTypes()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

public class B { }

[System.{|CS0234:Composition|}.Export(typeof(C))]
public class C
{
    [System.ComponentModel.{|CS0234:Composition|}.Import]
    public B PropertyB { get; }
}
");

            await VerifyVB.VerifyAnalyzerAsync(@"
Public Class B
End Class

<{|BC30002:System.Composition.Export|}(GetType(C))> _
Public Class C
	<{|BC30002:System.ComponentModel.Composition.Import|}> _
	Public ReadOnly Property PropertyB() As B
End Class
");
        }

        [Fact]
        public async Task NoDiagnosticCases_MultiMefMetadataAttribute()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

[System.ComponentModel.Composition.Export(typeof(C)), MyNamespace.MultiMefMetadataAttribute]
public class C
{
}

namespace MyNamespace
{
    [System.ComponentModel.Composition.MetadataAttribute, System.Composition.MetadataAttribute]
    public class MultiMefMetadataAttribute : System.Attribute
    {
    }
}
" + CSharpWellKnownAttributesDefinition);

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System

<System.ComponentModel.Composition.Export(GetType(C)), MyNamespace.MultiMefMetadataAttribute> _
Public Class C
End Class

Namespace MyNamespace
    <System.ComponentModel.Composition.MetadataAttribute, System.Composition.MetadataAttribute> _
	Public Class MultiMefMetadataAttribute
		Inherits System.Attribute
	End Class
End Namespace
" + BasicWellKnownAttributesDefinition);
        }

        #endregion

        #region Diagnostic Tests

        [Fact]
        public async Task DiagnosticCases_BadMetadataAttribute()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

[System.Composition.Export(typeof(C))]
[SystemComponentModelCompositionMetadataAttribute]
public class C
{
}

[System.ComponentModel.Composition.Export(typeof(C2))]
[SystemCompositionMetadataAttribute]
public class C2
{
}
" + CSharpWellKnownAttributesDefinition,
    // Test0.cs(5,2): warning RS0006: Attribute 'SystemComponentModelCompositionMetadataAttribute' comes from a different version of MEF than the export attribute on 'C'
    GetCSharpResultAt(5, 2, "SystemComponentModelCompositionMetadataAttribute", "C"),
    // Test0.cs(11,2): warning RS0006: Attribute 'SystemCompositionMetadataAttribute' comes from a different version of MEF than the export attribute on 'C2'
    GetCSharpResultAt(11, 2, "SystemCompositionMetadataAttribute", "C2"));

            await VerifyVB.VerifyAnalyzerAsync(@"
Imports System

<System.Composition.Export(GetType(C))> _
<SystemComponentModelCompositionMetadataAttribute> _
Public Class C
End Class

<System.ComponentModel.Composition.Export(GetType(C2))> _
<SystemCompositionMetadataAttribute> _
Public Class C2
End Class
" + BasicWellKnownAttributesDefinition,
    // Test0.vb(5,2): warning RS0006: Attribute 'SystemComponentModelCompositionMetadataAttribute' comes from a different version of MEF than the export attribute on 'C'
    GetBasicResultAt(5, 2, "SystemComponentModelCompositionMetadataAttribute", "C"),
    // Test0.vb(10,2): warning RS0006: Attribute 'SystemCompositionMetadataAttribute' comes from a different version of MEF than the export attribute on 'C2'
    GetBasicResultAt(10, 2, "SystemCompositionMetadataAttribute", "C2"));
        }

        [Fact]
        public async Task DiagnosticCases_BadMefAttributeOnMember()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

public class B { }

[System.Composition.Export(typeof(C))]
public class C
{
    [System.ComponentModel.Composition.ImportingConstructor]
    public C([System.Composition.Import]B b) { }

    [System.ComponentModel.Composition.Import]
    public B PropertyB { get; }
}

[System.ComponentModel.Composition.Export(typeof(C2))]
public class C2
{
    [System.Composition.ImportingConstructor]
    public C2([System.ComponentModel.Composition.Import]B b) { }

    [System.Composition.Import]
    public B PropertyB { get; }
}
" + CSharpWellKnownAttributesDefinition,
    // Test0.cs(9,6): warning RS0006: Attribute 'ImportingConstructorAttribute' comes from a different version of MEF than the export attribute on 'C'
    GetCSharpResultAt(9, 6, "ImportingConstructorAttribute", "C"),
    // Test0.cs(12,6): warning RS0006: Attribute 'ImportAttribute' comes from a different version of MEF than the export attribute on 'C'
    GetCSharpResultAt(12, 6, "ImportAttribute", "C"),
    // Test0.cs(19,6): warning RS0006: Attribute 'ImportingConstructorAttribute' comes from a different version of MEF than the export attribute on 'C2'
    GetCSharpResultAt(19, 6, "ImportingConstructorAttribute", "C2"),
    // Test0.cs(22,6): warning RS0006: Attribute 'ImportAttribute' comes from a different version of MEF than the export attribute on 'C2'
    GetCSharpResultAt(22, 6, "ImportAttribute", "C2")
);

            await VerifyVB.VerifyAnalyzerAsync(@"
Public Class B
End Class

<System.Composition.Export(GetType(C))> _
Public Class C
	<System.ComponentModel.Composition.ImportingConstructor> _
	Public Sub New(<System.Composition.Import> b As B)
	End Sub

	<System.ComponentModel.Composition.Import> _
	Public ReadOnly Property PropertyB() As B
End Class

<System.ComponentModel.Composition.Export(GetType(C2))> _
Public Class C2
	<System.Composition.ImportingConstructor> _
	Public Sub New(<System.ComponentModel.Composition.Import> b As B)
	End Sub

	<System.Composition.Import> _
	Public ReadOnly Property PropertyB() As B
End Class
" + BasicWellKnownAttributesDefinition,
    // Test0.vb(7,3): warning RS0006: Attribute 'ImportingConstructorAttribute' comes from a different version of MEF than the export attribute on 'C'
    GetBasicResultAt(7, 3, "ImportingConstructorAttribute", "C"),
    // Test0.vb(11,3): warning RS0006: Attribute 'ImportAttribute' comes from a different version of MEF than the export attribute on 'C'
    GetBasicResultAt(11, 3, "ImportAttribute", "C"),
    // Test0.vb(17,3): warning RS0006: Attribute 'ImportingConstructorAttribute' comes from a different version of MEF than the export attribute on 'C2'
    GetBasicResultAt(17, 3, "ImportingConstructorAttribute", "C2"),
    // Test0.vb(21,3): warning RS0006: Attribute 'ImportAttribute' comes from a different version of MEF than the export attribute on 'C2'
    GetBasicResultAt(21, 3, "ImportAttribute", "C2")
);
        }

        [Fact]
        public async Task DiagnosticCases_BadMefAttributeOnParameter()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;

public class B { }

[System.Composition.Export(typeof(C))]
public class C
{
    [System.Composition.ImportingConstructor]
    public C([System.ComponentModel.Composition.Import]B b) { }

    [System.Composition.Import]
    public B PropertyB { get; }
}

[System.ComponentModel.Composition.Export(typeof(C2))]
public class C2
{
    [System.ComponentModel.Composition.ImportingConstructor]
    public C2([System.Composition.Import]B b) { }

    [System.ComponentModel.Composition.Import]
    public B PropertyB { get; }
}
" + CSharpWellKnownAttributesDefinition,
    // Test0.cs(10,15): warning RS0006: Attribute 'ImportAttribute' comes from a different version of MEF than the export attribute on 'C'
    GetCSharpResultAt(10, 15, "ImportAttribute", "C"),
    // Test0.cs(20,16): warning RS0006: Attribute 'ImportAttribute' comes from a different version of MEF than the export attribute on 'C2'
    GetCSharpResultAt(20, 16, "ImportAttribute", "C2"));

            await VerifyVB.VerifyAnalyzerAsync(@"
Public Class B
End Class

<System.Composition.Export(GetType(C))> _
Public Class C
	<System.Composition.ImportingConstructor> _
	Public Sub New(<System.ComponentModel.Composition.Import> b As B)
	End Sub

	<System.Composition.Import> _
	Public ReadOnly Property PropertyB() As B
End Class

<System.ComponentModel.Composition.Export(GetType(C2))> _
Public Class C2
	<System.ComponentModel.Composition.ImportingConstructor> _
	Public Sub New(<System.Composition.Import> b As B)
	End Sub

	<System.ComponentModel.Composition.Import> _
	Public ReadOnly Property PropertyB() As B
End Class
" + BasicWellKnownAttributesDefinition,
    // Test0.vb(8,18): warning RS0006: Attribute 'ImportAttribute' comes from a different version of MEF than the export attribute on 'C'
    GetBasicResultAt(8, 18, "ImportAttribute", "C"),
    // Test0.vb(18,18): warning RS0006: Attribute 'ImportAttribute' comes from a different version of MEF than the export attribute on 'C2'
    GetBasicResultAt(18, 18, "ImportAttribute", "C2"));
        }

        #endregion

        private static DiagnosticResult GetCSharpResultAt(int line, int column, string attributeName, string typeName)
        {
            return new DiagnosticResult(RoslynDiagnosticIds.MixedVersionsOfMefAttributesRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(line, column)
                .WithMessageFormat(RoslynDiagnosticsAnalyzersResources.DoNotMixAttributesFromDifferentVersionsOfMEFMessage)
                .WithArguments(attributeName, typeName);
        }

        private static DiagnosticResult GetBasicResultAt(int line, int column, string attributeName, string typeName)
        {
            return new DiagnosticResult(RoslynDiagnosticIds.MixedVersionsOfMefAttributesRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
                .WithLocation(line, column)
                .WithMessageFormat(RoslynDiagnosticsAnalyzersResources.DoNotMixAttributesFromDifferentVersionsOfMEFMessage)
                .WithArguments(attributeName, typeName);
        }
    }
}