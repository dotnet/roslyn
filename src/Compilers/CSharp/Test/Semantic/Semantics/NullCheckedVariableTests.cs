// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) null-checked variables and lambdas.
    /// </summary>
    public class NullCheckedVariableTests : CompilingTestBase
    {
        [Fact]
        public void NullCheckedMethodDeclaration()
        {
            var source = @"
delegate void Del(int x!, int y);
class C
{
    Del d = delegate(int k!, int j) { /* ... */ };
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (2, 19): error CS8713: Parameter 'int x!' can only have exclamation - point null checking in implementation methods.
                    // delegate void Del(int x!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "int x!").WithArguments("int x!").WithLocation(2, 19));
        }

        [Fact]
        public void NullCheckedAbstractMethod()
        {
            var source = @"
abstract class C
{
    abstract public int M(int x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4, 27): error CS8713: Parameter 'int x!' can only have exclamation - point null checking in implementation methods.
                    // delegate void Del(int x!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "int x!").WithArguments("int x!").WithLocation(4, 27));
        }
    }
}
