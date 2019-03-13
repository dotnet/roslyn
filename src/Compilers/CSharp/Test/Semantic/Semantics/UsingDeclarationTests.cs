﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    /// <summary>
    /// Tests related to binding (but not lowering) using declarations (i.e. using var x = ...).
    /// </summary>
    public class UsingDeclarationTests : CompilingTestBase
    {
        [Fact]
        public void UsingVariableIsNotReportedAsUnused()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        using var x = (IDisposable)null;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void DisallowGoToForwardAcrossUsingDeclarations()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        goto label1;
        using var x = (IDisposable)null;

        label1:
        return;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS8641: A goto target within the same block can not cross a using declaration.
                //         goto label1;
                Diagnostic(ErrorCode.ERR_GoToForwardJumpOverUsingVar, "goto label1;").WithLocation(7, 9),
                // (8,9): warning CS0162: Unreachable code detected
                //         using var x = (IDisposable)null;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "using").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DisallowGoToForwardAcrossUsingDeclarationsFromLowerBlock()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {   
        {
            goto label1;
        }
        using var x = (IDisposable)null;

        label1:
        return;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS8641: A goto target within the same block can not cross a using declaration.
                //         goto label1;
                Diagnostic(ErrorCode.ERR_GoToForwardJumpOverUsingVar, "goto label1;").WithLocation(8, 13),
                // (8,9): warning CS0162: Unreachable code detected
                //         using var x = (IDisposable)null;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "using").WithLocation(10, 9)
                );
        }

        [Fact]
        public void DisallowGoToForwardAcrossMultipleUsingDeclarationsGivesOnlyOneDiagnostic()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        goto label1;
        using var x = (IDisposable)null;
        using var y = (IDisposable)null;

        label1:
        return;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS8641: A goto target within the same block can not cross a using declaration.
                //         goto label1;
                Diagnostic(ErrorCode.ERR_GoToForwardJumpOverUsingVar, "goto label1;").WithLocation(7, 9),
                // (8,9): warning CS0162: Unreachable code detected
                //         using var x = (IDisposable)null;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "using").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DisallowMultipleGoToForwardAcrossMultipleUsingDeclarations()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        goto label1;
        using var x = (IDisposable)null;
        goto label2;
        using var y = (IDisposable)null;

        label1:
        label2:
        return;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS8641: A goto target within the same block can not cross a using declaration.
                //         goto label1;
                Diagnostic(ErrorCode.ERR_GoToForwardJumpOverUsingVar, "goto label1;").WithLocation(7, 9),
                // (8,9): warning CS0162: Unreachable code detected
                //         using var x = (IDisposable)null;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "using").WithLocation(8, 9),
                // (9,9): error CS8641: A goto target can not be after any using declarations.
                //         goto label2;
                Diagnostic(ErrorCode.ERR_GoToForwardJumpOverUsingVar, "goto label2;").WithLocation(9, 9)
                );
        }

        [Fact]
        public void DisallowGoToBackwardsAcrossUsingDeclarationsWhenLabelIsInTheSameScope()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        label1:
        using var x = (IDisposable)null;

        goto label1;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,9): error CS8641: A goto target within the same block can not cross a using declaration.
                //         goto label1;
                Diagnostic(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, "goto label1;").WithLocation(10, 9)
                );
        }

        [Fact]
        public void DisallowGoToBackwardsAcrossUsingDeclarationsWithMultipleLabels()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        label1: 
        label2:
        label3:
        using var x = (IDisposable)null;

        goto label3; // disallowed
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): warning CS0164: This label has not been referenced
                //         label1: 
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label1").WithLocation(7, 9),
                // (8,9): warning CS0164: This label has not been referenced
                //         label2:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label2").WithLocation(8, 9),
                // (10,9): error CS8641: A goto target within the same block can not cross a using declaration.
                //         goto label3; // disallowed
                Diagnostic(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, "goto label3;").WithLocation(12, 9)
                );
        }

        [Fact]
        public void DisallowGoToAcrossUsingDeclarationsComplexTest()
        {
            var source = @"
using System;
#pragma warning disable 162 // disable unreachable code warnings
class C
{
    static void Main()
    {
        label1:
        {
            label2:
            using var a = (IDisposable)null;
            goto label1; // allowed
            
            goto label2; // disallowed 1
        }

        label3:
        using var b = (IDisposable)null;
        {
            goto label3; // disallowed 2
        }
    
        {
            goto label4; // allowed
            goto label5; // disallowed 3
            label4:
            using var c = (IDisposable)null;
            label5:
            using var d = (IDisposable)null;
        }
        
        using var e = (IDisposable)null;
        label6:
        {
            {
                goto label6; //allowed
                label7:
                {
                    label8:
                    using var f = (IDisposable)null;      
                    goto label7; // allowed
                    {
                        using var g = (IDisposable)null;
                        goto label7; //allowed
                        goto label8; // disallowed 4
                    }
                }
            }
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,13): error CS8642: A goto cannot jump to a location before a using declaration within the same block.
                //             goto label2; // disallowed 1
                Diagnostic(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, "goto label2;").WithLocation(14, 13),
                // (20,13): error CS8642: A goto cannot jump to a location before a using declaration within the same block.
                //             goto label3; // disallowed 2
                Diagnostic(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, "goto label3;").WithLocation(20, 13),
                // (25,13): error CS8641: A goto cannot jump to a location after a using declaration.
                //             goto label5; // disallowed 3
                Diagnostic(ErrorCode.ERR_GoToForwardJumpOverUsingVar, "goto label5;").WithLocation(25, 13),
                // (45,25): error CS8642: A goto cannot jump to a location before a using declaration within the same block.
                //                         goto label8; // disallowed 4
                Diagnostic(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, "goto label8;").WithLocation(45, 25)
                );
        }

        [Fact]
        public void AllowGoToAroundUsingDeclarations()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        goto label1;
        label1:
        using var x = (IDisposable)null;
        goto label2;
        label2:
        return;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void AllowGoToBackwardsAcrossUsingDeclarationsWhenLabelIsInHigherScope()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        label1:
        {
            using var x = (IDisposable)null;
            goto label1;
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void AllowGoToForwardsAcrossUsingDeclarationsInALowerBlock()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        goto label1;
        {
            using var x = (IDisposable)null;
        }
        label1: ;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,9): warning CS0162: Unreachable code detected
                //         using var x = (IDisposable)null;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "using").WithLocation(9, 13)
                );
        }

        [Fact]
        public void AllowGoToBackwardsAcrossUsingDeclarationsInALowerBlock()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        label1:
        {
            using var x = (IDisposable)null;
        }
        goto label1;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingVariableCanBeInitializedWithExistingDisposable()
        {
            var source = @"
using System;
class C2 : IDisposable
{
    public void Dispose()
    {
        Console.Write(""Disposed; ""); 
    }
}
class C
{
    static void Main()
    {
        var x = new C2();
        using var x2 = x;
        using var x3 = x2;
        x = null;
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "Disposed; Disposed; ");
        }

        [Fact]
        public void UsingVariableCanBeInitializedWithExistingDisposableInASingleStatement()
        {
            var source = @"
using System;
class C2 : IDisposable
{
    public void Dispose()
    {
        Console.Write(""Disposed; ""); 
    }
}
class C
{
    static void Main()
    {
        using C2 x = new C2(), x2 = x;
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "Disposed; Disposed; ");
        }

        [Fact]
        public void UsingVariableCanBeInitializedWithExistingRefStructDisposable()
        {
            var source = @"
using System;
ref struct C2
{
    public void Dispose()
    {
        Console.Write(""Disposed; ""); 
    }
}
class C
{
    static void Main()
    {
        var x = new C2();
        using var x2 = x;
        using var x3 = x2;
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "Disposed; Disposed; ");
        }

        [Fact]
        public void UsingVariableCannotBeReAssigned()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        using var x = (IDisposable)null;
        x = null;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,9): error CS1656: Cannot assign to 'x' because it is a 'using variable'
                //         x = null;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x").WithArguments("x", "using variable").WithLocation(8, 9)
                );
        }

        [Fact]
        public void UsingVariableCannotBeUsedAsOutVariable()
        {
            var source = @"
using System;
class C
{
    static void Consume(out IDisposable x) 
    {
        x = null;
    }

    static void Main()
    {
        using var x = (IDisposable)null;
        Consume(out x);
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,21): error CS1657: Cannot use 'x' as a ref or out value because it is a 'using variable'
                //         Consume(out x);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "x").WithArguments("x", "using variable").WithLocation(13, 21)
                );
        }

        [Fact]
        public void UsingVariableMustHaveInitializer()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        using IDisposable x;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,27): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //         using IDisposable x;
                Diagnostic(ErrorCode.ERR_FixedMustInit, "x").WithLocation(7, 27)
                );
        }

        [Fact]
        public void UsingVariableFromExistingVariable()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (IDisposable)null;
        using var x2 = x;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingVariableFromExpression()
        {
            var source = @"
using System;
class C
{
    static IDisposable GetDisposable()
    {
        return null;
    }

    static void Main()
    {
        using IDisposable x = GetDisposable();
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingVariableFromAwaitExpression()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C
{
    static Task<IDisposable> GetDisposable()
    {
        return Task.FromResult<IDisposable>(null);
    }

    static async Task Main()
    {
        using IDisposable x = await GetDisposable();
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingVariableInSwitchCase()
        {
            var source = @"
using System;
class C1 : IDisposable
{
    public void Dispose() { }
}
class C2
{
    public static void Main()
    {
        int x = 5;
        switch (x)
        {
            case 5:
                using C1 o1 = new C1();
                break;
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,21): error CS8389: A using variable cannot be used directly within a switch section (consider using braces). 
                //                     using C1 o1 = new C1();
                Diagnostic(ErrorCode.ERR_UsingVarInSwitchCase, "using C1 o1 = new C1();").WithLocation(15, 17)
            );
        }

        [Fact]
        public void UsingVariableDiagnosticsInDeclarationAreOnlyEmittedOnce()
        {
            var source = @"
using System;
class C1 : IDisposable
{
    public void Dispose() { }
}
class C2
{
    public static void Main()
    {
        using var c1 = new C1(), c2 = new C2();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
              // (11,15): error CS0819: Implicitly-typed variables cannot have multiple declarators
              //         using var c1 = new C1(), c2 = new C2();
              Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, "var c1 = new C1(), c2 = new C2()").WithLocation(11, 15)
            );
        }

        [Fact]
        public void UsingDeclarationWithAwaitsInAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C2 : IDisposable
{
    public string ID { get; set; }
    public void Dispose()
    {
        Console.Write($""Dispose {ID}; "");
    }
}
class C
{
    static async Task<IDisposable> GetDisposable(string id)
    {
        await Task.Yield();
        return new C2(){ ID = id };
    }

    static async Task Main()
    {
        using IDisposable x = await GetDisposable(""c1"");
        await Task.Yield();
        Console.Write(""after c1; "");
        using IDisposable y = await GetDisposable(""c2"");
        Console.Write(""after c2; "");
    }
}
";
            var compilation = CreateCompilationWithTasksExtensions(source, options: TestOptions.DebugExe).VerifyDiagnostics();

            CompileAndVerify(compilation, expectedOutput: "after c1; after c2; Dispose c2; Dispose c1; ");
        }

        [Fact]
        public void UsingDeclarationsWithLangVer7_3()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        using IDisposable x = null;
    }
}
";
            var expected = new[]
            {
                // (7,9): error CS8652: The feature 'using declarations' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         using IDisposable x = null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "using").WithArguments("using declarations").WithLocation(7, 9)
            };

            CreateCompilation(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(expected);

            CreateCompilation(source, parseOptions: TestOptions.RegularDefault).VerifyDiagnostics(expected);

            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
        }

        [Fact]
        public void AwaitUsingDeclarationsWithLangVer7_3()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C
{
    static async Task Main()
    {
        await using IAsyncDisposable x = null;
    }
}
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}";
            // https://github.com/dotnet/roslyn/issues/32318 Diagnostics should be tuned. There should only be a parsing error for `using declarations` feature.
            var expected = new[]
            {
                // (8,9): error CS8652: The feature 'async streams' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         await using IAsyncDisposable x = null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "await").WithArguments("async streams").WithLocation(8, 9),
                // (8,15): error CS8652: The feature 'using declarations' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         await using IAsyncDisposable x = null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "using").WithArguments("using declarations").WithLocation(8, 15)
            };

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.RegularDefault).VerifyDiagnostics(expected);
            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(expected);

            CreateCompilationWithTasksExtensions(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
        }

        [Fact]
        public void UsingDeclarationsWithObsoleteTypes()
        {
            var source = @"
using System;

[Obsolete]
class C1 : IDisposable
{
    [Obsolete]
    public void Dispose()  
    {
    }
}

class C2 : IDisposable
{
    [Obsolete]
    public void Dispose()  
    {
    }
}

ref struct S3
{
    [Obsolete]
    public void Dispose()
    {
    }
}

class C4
{
    static void Main()
    {
        // c1 is obsolete
        using C1 c1 = new C1();

        // no warning, we don't warn on dispose being obsolete because it comes through interface
        using C2 c2 = new C2();
    
        // warning, we're calling the pattern based obsolete method for the ref struct
        using S3 S3 = new S3();
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (34,15): warning CS0612: 'C1' is obsolete
                //         using C1 c1 = new C1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "C1").WithArguments("C1").WithLocation(34, 15),
                // (34,27): warning CS0612: 'C1' is obsolete
                //         using C1 c1 = new C1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "C1").WithArguments("C1").WithLocation(34, 27),
                // (40,15): warning CS0612: 'S3.Dispose()' is obsolete
                //         using S3 S3 = new S3();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "using S3 S3 = new S3();").WithArguments("S3.Dispose()").WithLocation(40, 9)
                );
        }
    }
}
