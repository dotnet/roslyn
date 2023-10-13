// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

// These tests cover the handling of System.Diagnostics.CodeAnalysis.ExperimentalAttribute.
public class ExperimentalAttributeTests : CSharpTestBase
{
    private const string experimentalAttributeSrc = """
#nullable enable

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }

        public string? UrlFormat { get; set; }
    }
}
""";

    private const string DefaultHelpLinkUri = "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS9204)";

    [Theory, CombinatorialData]
    public void Simple(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void OnAssembly_UsedFromSource()
    {
        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc });
        comp.VerifyDiagnostics();

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);
    }

    [Fact]
    public void OnAssembly_UsedFromMetadata()
    {
        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);

        // Note: the assembly-level [Experimental] is equivalent to marking every type and member as experimental,
        // whereas a type-level [Experimental] is not equivalent to marking every nested type and member as experimental.
        comp.VerifyDiagnostics(
            // (1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true),
            // (1,1): error DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );

        foreach (var diag in comp.GetDiagnostics())
        {
            Assert.Equal("DiagID1", diag.Id);
            Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
            Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
        }
    }

    [Fact]
    public void OnAssembly_DefinedInMetadata_UsedFromSource()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(new[] { src, libSrc }, references: new[] { attrRef });
        comp.VerifyDiagnostics();

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.None, comp.GetTypeByMetadataName("C").ContainingModule.ObsoleteKind);
    }

    [Fact]
    public void OnAssembly_DefinedInMetadata_UsedFromMetadata()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(libSrc, references: new[] { attrRef }).EmitToImageReference(), attrRef });

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.None, comp.GetTypeByMetadataName("C").ContainingModule.ObsoleteKind);

        comp.VerifyDiagnostics(
            // (1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true),
            // (1,1): error DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );

        foreach (var diag in comp.GetDiagnostics())
        {
            Assert.Equal("DiagID1", diag.Id);
            Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
            Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
        }
    }

    [Fact]
    public void OnAssembly_DefinedInMetadata_UsedFromMetadata_ObsoleteType()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]

[System.Obsolete("error", true)]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(libSrc, references: new[] { attrRef }).EmitToImageReference(), attrRef });

        Assert.Equal(ObsoleteAttributeKind.Obsolete, comp.GetTypeByMetadataName("C").ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.None, comp.GetTypeByMetadataName("C").ContainingModule.ObsoleteKind);

        comp.VerifyDiagnostics(
            // (1,1): error CS0619: 'C' is obsolete: 'error'
            // C.M();
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C").WithArguments("C", "error").WithLocation(1, 1),
            // (1,1): error DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Fact]
    public void OnAssembly_DefinedInMetadata_AppliedWithinModule_UsedFromSource()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc1 = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
""";
        var moduleComp = CreateCompilation(libSrc1, options: TestOptions.DebugModule, references: new[] { attrRef });
        var moduleRef = moduleComp.EmitToImageReference();

        var libSrc = """
public class C
{
    public static void M() { }
}
""";
        var src = """
C.M();
""";

        var comp = CreateCompilation(new[] { src, libSrc }, references: new[] { attrRef, moduleRef });
        comp.VerifyDiagnostics();

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);
    }

    [Fact]
    public void OnAssembly_DefinedInMetadata_AppliedWithinModule_UsedFromMetadata()
    {
        // An assembly-level [Experimental] compiled into a module applies to the entire assembly
        // the module gets compiled into
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc1 = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
""";
        var moduleComp = CreateCompilation(libSrc1, options: TestOptions.DebugModule, references: new[] { attrRef });
        var moduleRef = moduleComp.EmitToImageReference();

        var libSrc = """
public class C
{
    public static void M() { }
}
""";
        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(libSrc, references: new[] { attrRef, moduleRef }).EmitToImageReference(), attrRef });

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);

        comp.VerifyDiagnostics(
            // (1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true),
            // (1,1): error DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );

        foreach (var diag in comp.GetDiagnostics())
        {
            Assert.Equal("DiagID1", diag.Id);
            Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
            Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
        }
    }

    [Fact]
    public void OnAssembly_ObsoleteType()
    {
        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]

[System.Obsolete("error", true)]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.Obsolete, comp.GetTypeByMetadataName("C").ObsoleteKind);

        comp.VerifyDiagnostics(
            // (1,1): error CS0619: 'C' is obsolete: 'error'
            // C.M();
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C").WithArguments("C", "error").WithLocation(1, 1),
            // (1,1): error DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Fact]
    public void OnType_ObsoleteType()
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
[System.Obsolete("error", true)]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        Assert.Equal(ObsoleteAttributeKind.Obsolete, comp.GetTypeByMetadataName("C").ObsoleteKind);

        comp.VerifyDiagnostics(
            // (1,1): error CS0619: 'C' is obsolete: 'error'
            // C.M();
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C").WithArguments("C", "error").WithLocation(1, 1)
            );
    }

    [Fact]
    public void OnModule()
    {
        var libSrc = """
[module: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true),
            // (1,1): error DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Fact]
    public void OnModule_DefinedInMetadata_UsedFromSource()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[module: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(new[] { src, libSrc }, references: new[] { attrRef });
        comp.VerifyDiagnostics();

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingModule.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.None, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);
    }

    [Fact]
    public void OnModule_DefinedInMetadata_UsedFromMetadata()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[module: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(libSrc, references: new[] { attrRef }).EmitToImageReference(), attrRef });

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingModule.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.None, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);

        comp.VerifyDiagnostics(
            // (1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true),
            // (1,1): error DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );

        foreach (var diag in comp.GetDiagnostics())
        {
            Assert.Equal("DiagID1", diag.Id);
            Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
            Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
        }
    }

    [Fact]
    public void OnModuleAndAssembly_UsedFromSource()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagAssembly")]
[module: System.Diagnostics.CodeAnalysis.Experimental("DiagModule")]

public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(new[] { src, libSrc }, references: new[] { attrRef });
        comp.VerifyDiagnostics();

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingModule.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);
    }

    [Fact]
    public void OnModuleAndAssembly_UsedFromMetadata()
    {
        // Prefer reporting the module-level diagnostic
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagAssembly")]
[module: System.Diagnostics.CodeAnalysis.Experimental("DiagModule")]

public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(libSrc, references: new[] { attrRef }).EmitToImageReference(), attrRef });

        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingModule.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);

        comp.VerifyDiagnostics(
            // (1,1): error DiagModule: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagModule", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true),
            // (1,1): error DiagModule: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagModule", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );

        foreach (var diag in comp.GetDiagnostics())
        {
            Assert.Equal("DiagModule", diag.Id);
            Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
            Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
        }
    }

    [Fact]
    public void OnAssembly_CompiledIntoModule()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("AssemblyDiagSetInModule")]

public class C
{
    public static void M() { }
}
""";

        var moduleComp = CreateCompilation(libSrc, options: TestOptions.ReleaseModule, references: new[] { attrRef });
        moduleComp.VerifyDiagnostics();
        var moduleRef = moduleComp.EmitToImageReference();

        var libSrc2 = """
public class D
{
    public static void M()
    {
        C.M();
    }
}
""";
        var assemblyComp = CreateCompilation(libSrc2, references: new[] { moduleRef, attrRef });
        assemblyComp.VerifyDiagnostics();
        var assemblyRef = assemblyComp.EmitToImageReference();

        var src = """
C.M();
D.M();
""";

        // Since the module is referenced but not linked, we also need it here, but as
        // a result the diagnostics are suppressed
        var comp = CreateCompilation(src, references: new[] { assemblyRef, moduleRef, attrRef });
        comp.VerifyDiagnostics();

        Assert.Equal(ObsoleteAttributeKind.None, comp.GetTypeByMetadataName("C").ContainingModule.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("C").ContainingAssembly.ObsoleteKind);

        Assert.Equal(ObsoleteAttributeKind.None, comp.GetTypeByMetadataName("D").ContainingModule.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.Experimental, comp.GetTypeByMetadataName("D").ContainingAssembly.ObsoleteKind);
    }

    [Fact]
    public void OnTypeAndMethodAndAssembly_UsedFromSource()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("IGNORED")]

[System.Diagnostics.CodeAnalysis.Experimental("DiagType")]
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagMethod")]
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(new[] { src, libSrc }, references: new[] { attrRef });
        comp.VerifyDiagnostics();

        var c = comp.GetTypeByMetadataName("C");
        Assert.Equal(ObsoleteAttributeKind.Experimental, c.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.Experimental, c.ContainingAssembly.ObsoleteKind);

        var m = comp.GetMember("C.M");
        Assert.Equal(ObsoleteAttributeKind.Experimental, m.ObsoleteKind);
    }

    [Fact]
    public void OnTypeAndMethodAndAssembly_UsedFromMetadata()
    {
        // Prefer reporting the type-level and method-level diagnostic
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("IGNORED")]

[System.Diagnostics.CodeAnalysis.Experimental("DiagType")]
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagMethod")]
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(libSrc, references: new[] { attrRef }).EmitToImageReference(), attrRef });

        comp.VerifyDiagnostics(
            // (1,1): error DiagType: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagType", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true),
            // (1,1): error DiagMethod: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagMethod", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );

        var c = comp.GetTypeByMetadataName("C");
        Assert.Equal(ObsoleteAttributeKind.Experimental, c.ObsoleteKind);
        Assert.Equal(ObsoleteAttributeKind.Experimental, c.ContainingAssembly.ObsoleteKind);

        var m = comp.GetMember("C.M");
        Assert.Equal(ObsoleteAttributeKind.Experimental, m.ObsoleteKind);
    }

    [Fact]
    public void OnTypeAndAssembly_UsedFromSource()
    {
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagAssembly")]

[System.Diagnostics.CodeAnalysis.Experimental("DiagType")]
public class C
{
    public class Nested
    {
        public static void M() { }
    }
}
""";

        var src = """
C.Nested.M();
""";

        var comp = CreateCompilation(new[] { src, libSrc }, references: new[] { attrRef });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void OnTypeAndAssembly_UsedFromMetadata()
    {
        // Prefer reporting the type-level and method-level diagnostic
        var attrComp = CreateCompilation(experimentalAttributeSrc);
        var attrRef = attrComp.EmitToImageReference();

        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagAssembly")]

[System.Diagnostics.CodeAnalysis.Experimental("DiagType")]
public class C
{
    public class Nested
    {
        public static void M() { }
    }
}
""";

        var src = """
C.Nested.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(libSrc, references: new[] { attrRef }).EmitToImageReference(), attrRef });

        comp.VerifyDiagnostics(
            // (1,1): error DiagType: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.Nested.M();
            Diagnostic("DiagType", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true),
            // (1,1): error DiagAssembly: 'C.Nested' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.Nested.M();
            Diagnostic("DiagAssembly", "C.Nested").WithArguments("C.Nested").WithLocation(1, 1).WithWarningAsError(true),
            // (1,1): error DiagAssembly: 'C.Nested.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.Nested.M();
            Diagnostic("DiagAssembly", "C.Nested.M()").WithArguments("C.Nested.M()").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void OnOverridden(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("Diag")]
    public virtual void M() { }
}
""";

        var src = """
public class Derived : C
{
    public override void M() { }

    public void M2()
    {
        base.M();
        M();
    }
}
""";

        var comp = inSource
         ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
         : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(7,9): error Diag: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            //         base.M();
            Diagnostic("Diag", "base.M()").WithArguments("C.M()").WithLocation(7, 9).WithWarningAsError(true),
            // 0.cs(8,9): error Diag: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            //         M();
            Diagnostic("Diag", "M()").WithArguments("C.M()").WithLocation(8, 9).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void OnOverride(bool inSource)
    {
        var libSrc = """
public class C
{
    public virtual void M() { }
}
""";

        var src = """
public class Derived : C
{
    [System.Diagnostics.CodeAnalysis.Experimental("Diag")]
    public override void M() { }

    public void M2()
    {
        base.M();
        M();
    }
}

public class DerivedDerived : Derived
{
    public void M3()
    {
        base.M(); // 1
        M();
    }
}
""";

        var comp = inSource
         ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
         : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(17,9): error Diag: 'Derived.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            //         base.M(); // 1
            Diagnostic("Diag", "base.M()").WithArguments("Derived.M()").WithLocation(17, 9).WithWarningAsError(true)
            );
    }

    [Fact]
    public void OnOverride_ExperimentalFromAssembly_UsedFromSource()
    {
        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("Diag")]
public class C
{
    public virtual void M() { }
}
""";

        var src = """
public class Derived : C { public override void M() { } }
""";

        var comp = CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void OnOverride_ExperimentalFromAssembly_UsedFromMetadata()
    {
        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("Diag")]
public class C
{
    public virtual void M() { }
}
""";

        var src = """
public class Derived : C { public override void M() { } }
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        // CONSIDER narrowing the location on constructor initializer obsolete/experimental attributes
        comp.VerifyDiagnostics(
            // (1,1): error Diag: 'C.C()' is for evaluation purposes only and is subject to change or removal in future updates.
            // public class Derived : C { public override void M() { } }
            Diagnostic("Diag", "public class Derived : C { public override void M() { } }").WithArguments("C.C()").WithLocation(1, 1).WithWarningAsError(true),
            // (1,24): error Diag: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // public class Derived : C { public override void M() { } }
            Diagnostic("Diag", "C").WithArguments("C").WithLocation(1, 24).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void OnExplicitMethodImplementation(bool inSource)
    {
        var libSrc = """
public interface I
{
    [System.Diagnostics.CodeAnalysis.Experimental("Diag")]
    public void M();
}
""";

        var src = """
public class C : I
{
    void I.M() { }
}
""";

        var comp = inSource
         ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
         : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void OnExplicitMethodImplementation_Obsolete(bool inSource)
    {
        var libSrc = """
public interface I
{
    [System.Obsolete("message", true)]
    public void M();
}
""";

        var src = """
public class C : I
{
    void I.M() { }
}
""";

        var comp = inSource
         ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
         : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Fact]
    public void MissingAssemblyAndModule()
    {
        var missingRef = CreateCompilation("public class Base { }", assemblyName: "missing").EmitToImageReference();

        var libSrc = """
public class C : Base
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(libSrc, references: new[] { missingRef }).EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,3): error CS0012: The type 'Base' is defined in an assembly that is not referenced. You must add a reference to assembly 'missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
            // C.M();
            Diagnostic(ErrorCode.ERR_NoTypeDef, "M").WithArguments("Base", "missing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 3)
            );

        var missingType = comp.GlobalNamespace.GetTypeMember("C").BaseTypeNoUseSiteDiagnostics;
        Assert.True(missingType.ContainingAssembly is MissingAssemblySymbol);
        Assert.Equal(ObsoleteAttributeKind.None, missingType.ContainingAssembly.ObsoleteKind);
        Assert.True(missingType.ContainingModule is MissingModuleSymbol);
        Assert.Equal(ObsoleteAttributeKind.None, missingType.ContainingModule.ObsoleteKind);
    }

    [Fact]
    public void RetargetingAssembly_Experimental()
    {
        var attrRef = CreateCompilation(experimentalAttributeSrc).EmitToImageReference();

        var retargetedCode = """
public class C { }
""";

        var originalC = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);
        var retargetedC = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var derivedSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]

public class Derived : C { }
""";

        var derivedComp = CreateCompilation(derivedSrc, new[] { originalC.ToMetadataReference(), attrRef }, targetFramework: TargetFramework.Standard);
        derivedComp.VerifyDiagnostics();

        var comp = CreateCompilation("_ = new Derived();", new[] { derivedComp.ToMetadataReference(), retargetedC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);
        comp.VerifyDiagnostics(
            // (1,5): error DiagID1: 'Derived.Derived()' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = new Derived();
            Diagnostic("DiagID1", "new Derived()").WithArguments("Derived.Derived()").WithLocation(1, 5).WithWarningAsError(true),
            // (1,9): error DiagID1: 'Derived' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = new Derived();
            Diagnostic("DiagID1", "Derived").WithArguments("Derived").WithLocation(1, 9).WithWarningAsError(true)
            );

        var derived = comp.GetTypeByMetadataName("Derived");
        Assert.IsType<RetargetingNamedTypeSymbol>(derived);
        Assert.IsType<RetargetingAssemblySymbol>(derived.ContainingAssembly);
        Assert.Equal(ObsoleteAttributeKind.Experimental, derived.ContainingAssembly.ObsoleteKind);
    }

    [Fact]
    public void RetargetingAssembly_NotExperimental()
    {
        var attrRef = CreateCompilation(experimentalAttributeSrc).EmitToImageReference();

        var retargetedCode = """
public class C { }
""";

        var originalC = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);
        var retargetedC = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var derivedSrc = """
public class Derived : C { }
""";

        var derivedComp = CreateCompilation(derivedSrc, new[] { originalC.ToMetadataReference(), attrRef }, targetFramework: TargetFramework.Standard);
        derivedComp.VerifyDiagnostics();

        var comp = CreateCompilation("_ = new Derived();", new[] { derivedComp.ToMetadataReference(), retargetedC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);
        comp.VerifyDiagnostics();

        var derived = comp.GetTypeByMetadataName("Derived");
        Assert.IsType<RetargetingNamedTypeSymbol>(derived);
        Assert.IsType<RetargetingAssemblySymbol>(derived.ContainingAssembly);
        Assert.Equal(ObsoleteAttributeKind.None, derived.ContainingAssembly.ObsoleteKind);
    }

    [Fact]
    public void RetargetingModule_Experimental()
    {
        var attrRef = CreateCompilation(experimentalAttributeSrc).EmitToImageReference();

        var retargetedCode = """
public class C { }
""";

        var originalC = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);
        var retargetedC = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var @base = """
[module: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]

public class Derived : C { }
""";

        var derivedComp = CreateCompilation(@base, new[] { originalC.ToMetadataReference(), attrRef }, targetFramework: TargetFramework.Standard);
        derivedComp.VerifyDiagnostics();

        var comp = CreateCompilation("_ = new Derived();", new[] { derivedComp.ToMetadataReference(), retargetedC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);
        comp.VerifyDiagnostics(
            // (1,5): error DiagID1: 'Derived.Derived()' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = new Derived();
            Diagnostic("DiagID1", "new Derived()").WithArguments("Derived.Derived()").WithLocation(1, 5).WithWarningAsError(true),
            // (1,9): error DiagID1: 'Derived' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = new Derived();
            Diagnostic("DiagID1", "Derived").WithArguments("Derived").WithLocation(1, 9).WithWarningAsError(true)
            );

        var derived = comp.GetTypeByMetadataName("Derived");
        Assert.IsType<RetargetingNamedTypeSymbol>(derived);
        Assert.IsType<RetargetingModuleSymbol>(derived.ContainingModule);
        Assert.Equal(ObsoleteAttributeKind.Experimental, derived.ContainingModule.ObsoleteKind);
    }

    [Fact]
    public void RetargetingModule_NotExperimental()
    {
        var attrRef = CreateCompilation(experimentalAttributeSrc).EmitToImageReference();

        var retargetedCode = """
public class C { }
""";

        var originalC = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);
        var retargetedC = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), retargetedCode, TargetFrameworkUtil.StandardReferences);

        var @base = """
public class Derived : C { }
""";

        var derivedComp = CreateCompilation(@base, new[] { originalC.ToMetadataReference(), attrRef }, targetFramework: TargetFramework.Standard);
        derivedComp.VerifyDiagnostics();

        var comp = CreateCompilation("_ = new Derived();", new[] { derivedComp.ToMetadataReference(), retargetedC.ToMetadataReference() }, targetFramework: TargetFramework.Standard);
        comp.VerifyDiagnostics();

        var derived = comp.GetTypeByMetadataName("Derived");
        Assert.IsType<RetargetingNamedTypeSymbol>(derived);
        Assert.IsType<RetargetingModuleSymbol>(derived.ContainingModule);
        Assert.Equal(ObsoleteAttributeKind.None, derived.ContainingModule.ObsoleteKind);
    }

    [Theory, CombinatorialData]
    public void OnStruct(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public struct S
{
    public static void M() { }
}
""";

        var src = """
S.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'S' is for evaluation purposes only and is subject to change or removal in future updates.
            // S.M();
            Diagnostic("DiagID1", "S").WithArguments("S").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnEnum(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public enum E { }
""";

        var src = """
E e = default;
e.ToString();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'E' is for evaluation purposes only and is subject to change or removal in future updates.
            // E e = default;
            Diagnostic("DiagID1", "E").WithArguments("E").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnConstructor(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public C() { }
}
""";

        var src = """
_ = new C();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,5): error DiagID1: 'C.C()' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = new C();
            Diagnostic("DiagID1", "new C()").WithArguments("C.C()").WithLocation(1, 5).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnMethod(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnProperty(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static int P => 0;
}
""";

        var src = """
_ = C.P;
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,5): error DiagID1: 'C.P' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = C.P;
            Diagnostic("DiagID1", "C.P").WithArguments("C.P").WithLocation(1, 5).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnField(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static int field = 0;
}
""";

        var src = """
_ = C.field;
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,5): error DiagID1: 'C.field' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = C.field;
            Diagnostic("DiagID1", "C.field").WithArguments("C.field").WithLocation(1, 5).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void OnEvent(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static event System.Action Event;

    static void M()
    {
        Event();
    }
}
""";

        var src = """
C.Event += () => { };
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'C.Event' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.Event += () => { };
            Diagnostic("DiagID1", "C.Event").WithArguments("C.Event").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void OnInterface(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public interface I
{
    void M();
}
""";

        var src = """
I i = null;
i.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'I' is for evaluation purposes only and is subject to change or removal in future updates.
            // I i = null;
            Diagnostic("DiagID1", "I").WithArguments("I").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void OnDelegate(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public delegate void D();
""";

        var src = """
D d = null;
d();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'D' is for evaluation purposes only and is subject to change or removal in future updates.
            // D d = null;
            Diagnostic("DiagID1", "D").WithArguments("D").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void OnParameter(bool inSource)
    {
        // Ignored on parameters
        var libSrc = """
public class C
{
    public static void M([System.Diagnostics.CodeAnalysis.Experimental("DiagID1")] int i) { }
}
""";

        var src = """
C.M(42);
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void OnReturnValue(bool inSource)
    {
        // Ignored on return value
        var libSrc = """
public class C
{
    [return: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static int M() => 0;
}
""";

        var src = """
_ = C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void OnTypeParameter(bool inSource)
    {
        // Ignored on type parameters
        var libSrc = """
public class C<[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")] T> { }
""";

        var src = """
C<int> c = null;
c.ToString();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void NullDiagnosticId(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental(null)]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error CS9204: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic(ErrorCode.WRN_Experimental, "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void DiagnosticIdWithTrailingNewline(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("Diag\n")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error Diag : 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("Diag\n", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void DiagnosticIdWithNewline(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("Diag\n01")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error Diag 01: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("Diag\n01", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void WhitespaceDiagnosticId(bool inSource,
        [CombinatorialValues("\"\"", "\" \"", "\"\\n\"")] string whitespace)
    {
        var libSrc = $$"""
[System.Diagnostics.CodeAnalysis.Experimental({{whitespace}})]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new CSharpTestSource[] { (src, "0.cs"), libSrc, experimentalAttributeSrc })
            : CreateCompilation((src, "0.cs"), references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error CS9204: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic(ErrorCode.WRN_Experimental, "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Fact]
    public void SpacedDiagnosticId()
    {
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("Diag 01")]
class C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): error Diag 01: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("Diag 01", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Fact]
    public void BadAttribute_IntParameter()
    {
        // In source, if the attribute is improperly declared, but with the right number of parameters, we still recognize it
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental(42)]
class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(int diagnosticId)
        {
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,1): error CS9204: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic(ErrorCode.WRN_Experimental, "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Fact]
    public void BadAttribute_IntParameter_Metadata()
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental(42)]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(int diagnosticId)
        {
        }
    }
}
""";

        var libComp = CreateCompilation(libSrc);

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { libComp.EmitToImageReference() });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void BadAttribute_TwoStringParameters()
    {
        // If the attribute is improperly declared, with a wrong number of parameters, we ignore it
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("ignored", "ignored")]
class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId, string urlFormat)
        {
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void BadAttribute_IntUrlFormatProperty_Metadata()
    {
        // A "UrlFormat" property with a type other than 'string' is ignored
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID", UrlFormat = 42)]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }
        public int UrlFormat { get; set; }
    }
}
""";

        var libComp = CreateCompilation(libSrc);

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { libComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,1): error DiagID: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void BadAttribute_UrlFormatField_Metadata()
    {
        // A field named "UrlFormat" is ignored
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID", UrlFormat = "hello")]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }
        public string UrlFormat = "hello";
    }
}
""";

        var libComp = CreateCompilation(libSrc);

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { libComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,1): error DiagID: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void BadAttribute_OtherProperty_Metadata()
    {
        // A property that isn't named "UrlFormat" is ignored
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID", NotUrlFormat = "hello")]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }
        public string NotUrlFormat { get; set; }
    }
}
""";

        var libComp = CreateCompilation(libSrc);

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { libComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,1): error DiagID: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void UrlFormat()
    {
        // Combine the DiagnosticId with the UrlFormat if present
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat = "https://example.org/{0}")]
class C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates. (https://example.org/DiagID1)
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal("https://example.org/DiagID1", diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void BadUrlFormat()
    {
        // We use a default help URL if the UrlFormat is improper
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat = "https://example.org/{0}{1}")]
class C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void EmptyUrlFormat(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat = "")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal("", diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void NullUrlFormat()
    {
        // We use a default help URL if the UrlFormat is improper
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat = null)]
class C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1).WithWarningAsError(true)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void FullyQualified()
    {
        var src = """
N.C.M();

namespace N
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    class C
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): error DiagID1: 'N.C' is for evaluation purposes only and is subject to change or removal in future updates.
            // N.C.M();
            Diagnostic("DiagID1", "N.C").WithArguments("N.C").WithLocation(1, 1).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void Suppressed(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
#pragma warning disable DiagID1
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InObsoleteMethod(bool inSource)
    {
        // Diagnostics for [Experimental] are not suppressed in [Obsolete] members
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static void M() { }
}
""";

        var src = """
class D
{
    [System.Obsolete("obsolete", true)]
    void M2()
    {
        C.M();
    }
}
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(6,9): error DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            //         C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(6, 9).WithWarningAsError(true)
            );
    }

    [Theory, CombinatorialData]
    public void InExperimentalMethod(bool inSource)
    {
        // Diagnostics for [Experimental] are suppressed in [Experimental] context
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static void M() { }
}
""";

        var src = """
class D
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID2")]
    void M2()
    {
        C.M();
    }
}
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InExperimentalType(bool inSource)
    {
        // Diagnostics for [Experimental] are suppressed in [Experimental] context
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static void M() { }
}
""";

        var src = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID2")]
class D
{
    void M2()
    {
        C.M();
    }
}
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InExperimentalNestedType(bool inSource)
    {
        // Diagnostics for [Experimental] are suppressed in [Experimental] context
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static void M() { }
}
""";

        var src = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID2")]
class D
{
    class Nested
    {
        void M2()
        {
            C.M();
        }
    }
}
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InExperimentalModule(bool inSource)
    {
        // Diagnostics for [Experimental] are suppressed in [Experimental] context
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static void M() { }
}
""";

        var src = """
[module: System.Diagnostics.CodeAnalysis.Experimental("DiagID2")]
class D
{
    void M2()
    {
        C.M();
    }
}
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InExperimentalAssembly(bool inSource)
    {
        // Diagnostics for [Experimental] are suppressed in [Experimental] context
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static void M() { }
}
""";

        var src = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID2")]
class D
{
    void M2()
    {
        C.M();
    }
}
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void WithObsolete(bool inSource)
    {
        var libSrc = """
[System.Obsolete("error", true)]
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
}
""";

        var src = """
class D
{
    void M(C c)
    {
    }
}
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        if (inSource)
        {
            comp.VerifyDiagnostics(
                // 0.cs(3,12): error DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
                //     void M(C c)
                Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(3, 12).WithWarningAsError(true)
                );
        }
        else
        {
            comp.VerifyDiagnostics(
                // (3,12): error CS0619: 'C' is obsolete: 'error'
                //     void M(C c)
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C").WithArguments("C", "error").WithLocation(3, 12)
                );
        }
    }
}
