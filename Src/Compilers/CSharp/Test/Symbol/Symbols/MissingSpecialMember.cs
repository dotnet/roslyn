// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MissingSpecialMember : CSharpTestBase
    {
        private CSharpCompilationOptions WithMissingMember(WellKnownMember wm)
        {
            return WithMissingMember(wm.ToString());
        }
        private CSharpCompilationOptions WithMissingMember(SpecialMember sm)
        {
            return WithMissingMember(sm.ToString());
        }
        private CSharpCompilationOptions WithMissingMember(string memberKey)
        {
            var compOptions = TestOptions.Dll.WithOptimizations(true).WithFeatures(new[] { "MakeMissingMember:" + memberKey });
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Using single-threaded build if debugger attached, to simplify debugging.
                compOptions = compOptions.WithConcurrentBuild(false);
            }
            return compOptions;
        }

        [Fact]
        public void Missing_System_Collections_Generic_IEnumerable_T__GetEnumerator()
        {
            var source =
@"using System.Collections.Generic;

public class Program
{
    public static void Main(string[] args)
    {
    }

    public IEnumerable<int> M()
    {
        yield return 0;
        yield return 1;
    }
}";
            CreateCompilationWithMscorlib(source, compOptions: WithMissingMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator)).VerifyEmitDiagnostics(
    // (10,5): error CS0656: Missing compiler required member 'System.Collections.Generic.IEnumerable`1.GetEnumerator'
    //     {
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        yield return 0;
        yield return 1;
    }").WithArguments("System.Collections.Generic.IEnumerable`1", "GetEnumerator").WithLocation(10, 5)
                );
        }

        [Fact]
        public void Missing_System_IDisposable__Dispose()
        {
            var source =
@"using System.Collections.Generic;

public class Program
{
    public static void Main(string[] args)
    {
    }

    public IEnumerable<int> M()
    {
        yield return 0;
        yield return 1;
    }
}";
            CreateCompilationWithMscorlib(source, compOptions: WithMissingMember(SpecialMember.System_IDisposable__Dispose)).VerifyEmitDiagnostics(
    // (10,5): error CS0656: Missing compiler required member 'System.IDisposable.Dispose'
    //     {
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        yield return 0;
        yield return 1;
    }").WithArguments("System.IDisposable", "Dispose").WithLocation(10, 5)
                );
        }

        [Fact]
        public void Missing_System_Diagnostics_DebuggerHiddenAttribute__ctor()
        {
            var source =
@"using System.Collections.Generic;

public class Program
{
    public static void Main(string[] args)
    {
    }

    public IEnumerable<int> M()
    {
        yield return 0;
        yield return 1;
    }
}";
            CreateCompilationWithMscorlib(source, compOptions: WithMissingMember(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor)).VerifyEmitDiagnostics(
                // the DebuggerHidden attribute is optional.
                );
        }

        [Fact]
        public void Missing_System_Runtime_CompilerServices_ExtensionAttribute__ctor()
        {
            var source =
@"using System.Collections.Generic;

public static class Program
{
    public static void Main(string[] args)
    {
    }

    public static void Extension(this string x) {}
}";
            CreateCompilationWithMscorlib(source, compOptions: WithMissingMember(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor)).VerifyEmitDiagnostics(
                // (9,34): error CS1110: Cannot define a new extension method because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
                //     public static void Extension(this string x) {}
                Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "this").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(9, 34)
                );
        }
    }
}
