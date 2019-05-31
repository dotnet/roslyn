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
            CreateCompilation(source).VerifyDiagnostics();
        }
    }
}
