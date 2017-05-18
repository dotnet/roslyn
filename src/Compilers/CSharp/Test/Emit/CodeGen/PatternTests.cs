// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class PatternTests : EmitMetadataTestBase
    {
        [Fact, WorkItem(18811, "https://github.com/dotnet/roslyn/issues/18811")]
        public void MissingNullable_01()
        {
            var source = @"namespace System {
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}
static class C {
    public static bool M() => ((object)123) is int i;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.GetDiagnostics().Verify();
            compilation.GetEmitDiagnostics().Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (9,48): error CS0518: Predefined type 'System.Nullable`1' is not defined or imported
                //     public static bool M() => ((object)123) is int i;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int i").WithArguments("System.Nullable`1").WithLocation(9, 48),
                // (9,48): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //     public static bool M() => ((object)123) is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(9, 48),
                // (9,48): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //     public static bool M() => ((object)123) is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(9, 48)
                );
        }

        [Fact, WorkItem(18811, "https://github.com/dotnet/roslyn/issues/18811")]
        public void MissingNullable_02()
        {
            var source = @"namespace System {
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public struct Nullable<T> where T : struct { }
}
static class C {
    public static bool M() => ((object)123) is int i;
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.GetDiagnostics().Verify();
            compilation.GetEmitDiagnostics().Verify(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion),
                // (10,48): error CS0656: Missing compiler required member 'System.Nullable`1.GetValueOrDefault'
                //     public static bool M() => ((object)123) is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "GetValueOrDefault").WithLocation(10, 48),
                // (10,48): error CS0656: Missing compiler required member 'System.Nullable`1.get_HasValue'
                //     public static bool M() => ((object)123) is int i;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "int i").WithArguments("System.Nullable`1", "get_HasValue").WithLocation(10, 48)
                );
        }
    }
}
