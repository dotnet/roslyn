// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/22943")]
        public void ArrayCreationOperationSyntax()
        {
            string source = @"
class P
{
    static void M1()
    {
        var x = new /*<bind>*/int[0]/*</bind>*/;
    }
}
";

            (var operation, var syntax) = GetOperationAndSyntaxForTest<ArrayTypeSyntax>(source);
            Assert.Equal(syntax, operation.Syntax);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/22943")]
        public void IdentifierNameSyntaxInFieldAccess()
        {
            string source = @"
class Bar
{
    public bool Field;
}

class C
{
    public void M()
    {
        var x1 = new Bar();
        x1./*<bind>*/Field/*</bind>*/ = false;
    }
}
";

            (var operation, var syntax) = GetOperationAndSyntaxForTest<IdentifierNameSyntax>(source);
            Assert.Equal(syntax, operation.Syntax);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/22943")]
        public void IdentifierNameSyntaxInPropertyAccess()
        {
            string source = @"
class Bar
{
    public bool Property { get; set; }
}

class C
{
    public void M()
    {
        var x1 = new Bar();
        x1./*<bind>*/Property/*</bind>*/ = false;
    }
}
";

            (var operation, var syntax) = GetOperationAndSyntaxForTest<IdentifierNameSyntax>(source);
            Assert.Equal(syntax, operation.Syntax);
        }
    }
}
