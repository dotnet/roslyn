// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    internal class InternalImplementationOnlyTests : CSharpTestBase
    {
        [Fact]
        public void InternalImplementationOnly01()
        {
            const string iioaSource =
    @"namespace System.Runtime.CompilerServices
{
    internal sealed class InternalImplementationOnlyAttribute : System.Attribute
    {
    }
}";
            const string aSource =
    @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
[System.Runtime.CompilerServices.InternalImplementationOnlyAttribute]
public interface IA1 { }
public interface IA2 : IA1 { }";
            var compa = CreateCompilation(
                assemblyName: "A",
                sources: new[] { iioaSource, aSource },
                options: TestOptions.ReleaseDll,
                references: new[] { MetadataReference.CreateFromAssembly(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute).Assembly) });
            compa.VerifyDiagnostics();

            const string bSource =
    @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public interface IB1 : IA1 {} // ok: has internal access
public interface IB2 : IA2 {} // ok: IA2 not attributed
public class B1 : IA1 {} // ok: has internal access
public class B2 : IA2 {} // ok: IA2 not attributed";
            var compb = CreateCompilation(
                assemblyName: "B",
                sources: new[] { bSource },
                options: TestOptions.ReleaseDll,
                references: new[] { new CSharpCompilationReference(compa), MetadataReference.CreateFromAssembly(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute).Assembly) });
            compb.VerifyDiagnostics();
            CreateCompilation(
                assemblyName: "B",
                sources: new[] { bSource },
                options: TestOptions.ReleaseDll,
                references: new[] { compa.EmitToImageReference(), MetadataReference.CreateFromAssembly(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute).Assembly) })
                .VerifyDiagnostics();

            const string cSource =
    @"public interface IC1 : IA1 {} // ** error: do not have internal access
public interface IC2 : IA2 {} // ok: IA2 not attributed
public interface IC3 : IB1 {} // ok: IB1 not attributed
public interface IC4 : IB2 {} // ok: IB2 not attributed
public interface IC5 : IB1, IA1 {} // ** error: do not have internal access
public interface IC6 : IB2, IA2 {} // ok: IA2 not attributed
public class C1 : B1 {} // ok: B1 not attributed
public class C2 : B2 {} // ok: B2 not attributed";
            var compc = CreateCompilation(
                assemblyName: "C",
                sources: new[] { cSource },
                options: TestOptions.ReleaseDll,
                references: new[] { new CSharpCompilationReference(compa), new CSharpCompilationReference(compb), MetadataReference.CreateFromAssembly(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute).Assembly) });
            compc.VerifyDiagnostics(
                    // (5,18): error CS8096: The type 'IA1' may not be used in the base clause of 'IC5' because it has the InternalImplementationOnly attribute.
                    // public interface IC5 : IB1, IA1 {} // ** error: do not have internal access
                    Diagnostic(ErrorCode.ERR_InternalImplementationOnly, "IC5").WithArguments("IC5", "IA1").WithLocation(5, 18),
                    // (1,18): error CS8096: The type 'IA1' may not be used in the base clause of 'IC1' because it has the InternalImplementationOnly attribute.
                    // public interface IC1 : IA1 {} // ** error: do not have internal access
                    Diagnostic(ErrorCode.ERR_InternalImplementationOnly, "IC1").WithArguments("IC1", "IA1").WithLocation(1, 18)
                );
            CreateCompilation(
                assemblyName: "C",
                sources: new[] { cSource },
                options: TestOptions.ReleaseDll,
                references: new[] { compa.EmitToImageReference(), compb.EmitToImageReference(), MetadataReference.CreateFromAssembly(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute).Assembly) })
                .VerifyDiagnostics(
                    // (5,18): error CS8096: The type 'IA1' may not be used in the base clause of 'IC5' because it has the InternalImplementationOnly attribute.
                    // public interface IC5 : IB1, IA1 {} // ** error: do not have internal access
                    Diagnostic(ErrorCode.ERR_InternalImplementationOnly, "IC5").WithArguments("IC5", "IA1").WithLocation(5, 18),
                    // (1,18): error CS8096: The type 'IA1' may not be used in the base clause of 'IC1' because it has the InternalImplementationOnly attribute.
                    // public interface IC1 : IA1 {} // ** error: do not have internal access
                    Diagnostic(ErrorCode.ERR_InternalImplementationOnly, "IC1").WithArguments("IC1", "IA1").WithLocation(1, 18)
                );
        }
    }
}