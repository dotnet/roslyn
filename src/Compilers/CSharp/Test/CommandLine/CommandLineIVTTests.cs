// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests;

public class CommandLineIVTTests : CommandLineTestBase
{
    [Fact]
    public void InaccessibleBaseType()
    {
        var source1 = """
            namespace N1;
            internal class A {}
            """;

        var comp1 = CreateCompilation(source1, options: TestOptions.DebugDll, assemblyName: "N1", targetFramework: TargetFramework.Mscorlib461);

        var dir = Temp.CreateDirectory();

        var source2 = dir.CreateFile("B.cs").WriteAllText("""
            namespace N2;
            internal class B : N1.A {}
            """);

        var sw = new StringWriter();

        var compiler = CreateCSharpCompiler(new[] {
            "/nologo",
            "/t:library",
            "/preferreduilang:en",
            "/reportivts",
            source2.Path,
        }, additionalReferences: new[] { comp1.ToMetadataReference() });

        var errorCode = compiler.Run(sw);

        Assert.Equal(CommonCompiler.Failed, errorCode);
        var outputFilePath = $"{Path.GetFileName(dir.Path)}{Path.DirectorySeparatorChar}{Path.GetFileName(source2.Path)}";
        AssertEx.AssertEqualToleratingWhitespaceDifferences($"""
{outputFilePath}(2,23): error CS0122: 'A' is inaccessible due to its protection level
{outputFilePath}(2,23): error CS9163: 'A' is defined in assembly 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.

Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
Current assembly: 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'

Assembly reference: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'PresentationCore'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'PresentationFramework'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'System'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Core'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Numerics'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Reflection.Context'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime.UI.Xaml'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'WindowsBase'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9

Assembly reference: 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing
""", sw.ToString().Trim());
    }

    [Fact]
    public void AccessibleBaseType()
    {
        var source1 = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("B")]
            namespace N1;
            internal class A {}
            """;

        var comp1 = CreateCompilation(source1, options: TestOptions.DebugDll, assemblyName: "N1", targetFramework: TargetFramework.Mscorlib461);

        var dir = Temp.CreateDirectory();

        var source2 = dir.CreateFile("B.cs").WriteAllText("""
            namespace N2;
            internal class B : N1.A {}
            """);

        var sw = new StringWriter();

        var compiler = CreateCSharpCompiler(new[] {
            "/nologo",
            "/t:library",
            "/preferreduilang:en",
            "/reportivts",
            source2.Path,
        }, additionalReferences: new[] { comp1.ToMetadataReference() });

        var errorCode = compiler.Run(sw);

        Assert.Equal(CommonCompiler.Succeeded, errorCode);
        var outputFilePath = $"{Path.GetFileName(dir.Path)}{Path.DirectorySeparatorChar}{Path.GetFileName(source2.Path)}";
        AssertEx.AssertEqualToleratingWhitespaceDifferences($"""

Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
Current assembly: 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'

Assembly reference: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'PresentationCore'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'PresentationFramework'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'System'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Core'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Numerics'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Reflection.Context'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime.UI.Xaml'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'WindowsBase'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9

Assembly reference: 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: True
  Grants IVTs to:
    Assembly name: 'B'
    Public Keys:
""", sw.ToString().Trim());
    }

    [Fact]
    public void InaccessibleGetterSetter()
    {
        var source1 = """
            namespace N1;
            public class A
            {
                internal string Prop { get; set; }
            }
            """;

        var comp1 = CreateCompilation(source1, options: TestOptions.DebugDll, assemblyName: "N1", targetFramework: TargetFramework.Mscorlib461);

        var dir = Temp.CreateDirectory();

        var source2 = dir.CreateFile("B.cs").WriteAllText("""
            var a = new N1.A();
            _ = a.Prop;
            a.Prop = "hello";
            """);

        var sw = new StringWriter();

        var compiler = CreateCSharpCompiler(new[] {
            "/nologo",
            "/t:exe",
            "/preferreduilang:en",
            "/reportivts",
            source2.Path,
        }, additionalReferences: new[] { comp1.ToMetadataReference() });

        var errorCode = compiler.Run(sw);

        Assert.Equal(CommonCompiler.Failed, errorCode);
        var outputFilePath = $"{Path.GetFileName(dir.Path)}{Path.DirectorySeparatorChar}{Path.GetFileName(source2.Path)}";
        AssertEx.AssertEqualToleratingWhitespaceDifferences($"""
{outputFilePath}(2,7): error CS0122: 'A.Prop' is inaccessible due to its protection level
{outputFilePath}(3,3): error CS0122: 'A.Prop' is inaccessible due to its protection level
{outputFilePath}(2,7): error CS9163: 'A.Prop' is defined in assembly 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
{outputFilePath}(3,3): error CS9163: 'A.Prop' is defined in assembly 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.

Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
Current assembly: '?, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'

Assembly reference: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'PresentationCore'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'PresentationFramework'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'System'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Core'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Numerics'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Reflection.Context'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime.UI.Xaml'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'WindowsBase'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9

Assembly reference: 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing

""", sw.ToString().Trim());
    }

    [Fact]
    public void InaccessibleImplicitInterfaceImpl()
    {
        var source1 = """
            namespace N1;
            public interface A
            {
                internal void M();
            }
            """;

        var comp1 = CreateCompilation(source1, options: TestOptions.DebugDll, assemblyName: "N1", targetFramework: TargetFramework.Mscorlib461);

        var dir = Temp.CreateDirectory();

        var source2 = dir.CreateFile("B.cs").WriteAllText("""
            namespace N2;
            public class B : N1.A
            {
                public void M() { }
            }
            """);

        var sw = new StringWriter();

        var compiler = CreateCSharpCompiler(new[] {
            "/nologo",
            "/t:library",
            "/preferreduilang:en",
            "/reportivts",
            source2.Path,
        }, additionalReferences: new[] { comp1.ToMetadataReference() });

        var errorCode = compiler.Run(sw);

        Assert.Equal(CommonCompiler.Failed, errorCode);
        var outputFilePath = $"{Path.GetFileName(dir.Path)}{Path.DirectorySeparatorChar}{Path.GetFileName(source2.Path)}";
        AssertEx.AssertEqualToleratingWhitespaceDifferences($"""
{outputFilePath}(4,17): error CS9044: 'B' does not implement interface member 'A.M()'. 'B.M()' cannot implicitly implement an inaccessible member.
{outputFilePath}(4,17): error CS9163: 'A.M()' is defined in assembly 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.

Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
Current assembly: 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'

Assembly reference: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKey=00000000000000000400000000000000'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Assembly name: 'PresentationCore'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'PresentationFramework'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9
    Assembly name: 'System'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Core'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Numerics'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Reflection.Context'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'System.Runtime.WindowsRuntime.UI.Xaml'
    Public Keys:
      00000000000000000400000000000000
    Assembly name: 'WindowsBase'
    Public Keys:
      0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9

Assembly reference: 'N1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
  Grants IVT to current assembly: False
  Grants IVTs to:
    Nothing
""", sw.ToString().Trim());
    }

    [Theory]
    [InlineData("/reportivts")]
    [InlineData("/reportivts+")]
    public void TurnOnLast(string onFlag)
    {
        var compiler = CreateCSharpCompiler(new[] {
            "/nologo",
            "/reportivts-",
            onFlag,
        });

        Assert.True(compiler.Arguments.ReportInternalsVisibleToAttributes);
    }

    [Theory]
    [InlineData("/reportivts")]
    [InlineData("/reportivts+")]
    public void TurnOffLast(string onFlag)
    {
        var compiler = CreateCSharpCompiler(new[] {
            "/nologo",
            onFlag,
            "/reportivts-",
        });

        Assert.False(compiler.Arguments.ReportInternalsVisibleToAttributes);
    }

    [Fact]
    public void BadReportIvtsValue()
    {
        var compiler = CreateCSharpCompiler(new[] {
            "/nologo",
            "/reportivts:bad",
        });

        Assert.False(compiler.Arguments.ReportInternalsVisibleToAttributes);
        compiler.Arguments.Errors.Verify(
            Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/reportivts:bad").WithLocation(1, 1),
            Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
            Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1)
        );
    }
}
