// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class IteratorTests : CompilingTestBase
    {
        [Fact]
        public void BasicIterators01()
        {
            var text =
@"using System.Collections.Generic;

class Test
{
    IEnumerable<int> I()
    {
        yield return 1;
        yield break;
    }
}";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void BasicIterators02()
        {
            var text =
@"using System.Collections.Generic;

class Test
{
    IEnumerable<int> I()
    {
        yield return 1;
    }
}";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WrongYieldType()
        {
            var text =
@"using System.Collections.Generic;

class Test
{
    IEnumerable<int> I()
    {
        yield return 1.1;
        yield break;
    }
}";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
                // (7,22): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         yield return 1.1;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1.1").WithArguments("double", "int").WithLocation(7, 22)
                );
        }

        [Fact]
        public void NoYieldInLambda()
        {
            var text =
@"using System;
using System.Collections.Generic;

class Test
{
    IEnumerable<int> I()
    {
        Func<IEnumerable<int>> i = () => { yield break; };
        yield break;
    }
}";
            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics(
                // (8,44): error CS1621: The yield statement cannot be used inside an anonymous method or lambda expression
                //         Func<IEnumerable<int>> i = () => { yield break; };
                Diagnostic(ErrorCode.ERR_YieldInAnonMeth, "yield").WithLocation(8, 44)
                );
        }

        [WorkItem(546081, "DevDiv")]
        [Fact]
        public void IteratorBlockWithUnreachableCode()
        {
            var text =
@"using System;
using System.Collections;

public class Stack : IEnumerable
{
    int value;
    public IEnumerator GetEnumerator() { return BottomToTop.GetEnumerator(); }

    public IEnumerable BottomToTop
    {
        get
        {
            throw new Exception();
            yield return value;
        }
    }

}

class Test
{
    static void Main()
    {
    }
}";
            var comp = CreateCompilationWithMscorlib(text);

            EmitResult emitResult;
            using (var output = new MemoryStream())
            {
                emitResult = comp.Emit(output, null, null, null);
            }

            Assert.True(emitResult.Success);
        }

        [WorkItem(546364, "DevDiv")]
        [Fact]
        public void IteratorWithEnumeratorMoveNext()
        {
            var text =
@"using System.Collections;
using System.Collections.Generic;
public class Item
{
}
public class Program
{
    private IEnumerable<Item> DeferredFeedItems(IEnumerator elements, bool hasMoved)
    {
        while (hasMoved)
        {
            object o = elements.Current;
            if (o != null)
            {
                Item target = new Item();
                yield return target;
            }
            hasMoved = elements.MoveNext();
        }
    }
    public static void Main()
    {
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [WorkItem(813557, "DevDiv")]
        [Fact]
        public void IteratorWithDelegateCreationExpression()
        {
            var text =
@"using System.Collections.Generic;

delegate void D();
public class Program
{
    private IEnumerable<int> M1()
    {
        D d = new D(M2);
        yield break;
    }
    void M2(int x) {}
    static void M2() {}

    public static void Main()
    {
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [WorkItem(888254, "DevDiv")]
        [Fact]
        public void IteratorWithTryCatch()
        {
            var text =
@"using System;
using System.Collections.Generic;

namespace RoslynYield
{
    class Program
    {
        static void Main(string[] args)
        {
        }
        private IEnumerable<int> Failure()
        {
            // int x;
            try
            {
                int x;  // int x = 3;
                switch (1)
                {
                    default:
                        x = 4;
                        break;
                }
                Console.Write(x);
            }
            catch (Exception)
            {
            }

            yield break;
        }
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [WorkItem(888254, "DevDiv")]
        [Fact]
        public void IteratorWithTryCatchFinally()
        {
            var text =
@"using System;
using System.Collections.Generic;

namespace RoslynYield
{
    class Program
    {
        static void Main(string[] args)
        {
        }
        private IEnumerable<int> Failure()
        {
            try
            {
                int x;
                switch (1)
                {
                    default:
                        x = 4;
                        break;
                }
                Console.Write(x);
            }
            catch (Exception)
            {
                int x;
                switch (1)
                {
                    default:
                        x = 4;
                        break;
                }
                Console.Write(x);
            }
            finally
            {
                int x;
                switch (1)
                {
                    default:
                        x = 4;
                        break;
                }
                Console.Write(x);
            }

            yield break;
        }
    }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(5390, "https://github.com/dotnet/roslyn/issues/5390")]
        public void TopLevelYieldReturn()
        {
            // The imcomplete statement is intended
            var text =
@"yield return int.";
            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.Script);
            comp.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 18),
                                   Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 18),
                                   Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Threading.Tasks.Task", "GetAwaiter").WithLocation(1, 1),
                                   Diagnostic(ErrorCode.ERR_NoSuchMember, "").WithArguments("int", "").WithLocation(1, 18));

            var tree = comp.SyntaxTrees[0];     
            var yieldNode = (YieldStatementSyntax)tree.GetRoot().DescendantNodes().Where(n => n is YieldStatementSyntax).SingleOrDefault();

            Assert.NotNull(yieldNode);
            Assert.Equal(SyntaxKind.YieldReturnStatement, yieldNode.Kind());

            var model = comp.GetSemanticModel(tree);          
            var typeInfo = model.GetTypeInfo(yieldNode.Expression);

            Assert.Equal(TypeKind.Error, typeInfo.Type.TypeKind);
        }

        [Fact]
        [WorkItem(5390, "https://github.com/dotnet/roslyn/issues/5390")]
        public void TopLevelYieldBreak()
        {
            var text =
@"yield break;";
            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.Script);
            comp.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Threading.Tasks.Task", "GetAwaiter").WithLocation(1, 1),
                                   Diagnostic(ErrorCode.WRN_UnreachableCode, "yield").WithLocation(1, 1));

            var tree = comp.SyntaxTrees[0];
            var yieldNode = (YieldStatementSyntax)tree.GetRoot().DescendantNodes().Where(n => n is YieldStatementSyntax).SingleOrDefault();

            Assert.NotNull(yieldNode);
            Assert.Equal(SyntaxKind.YieldBreakStatement, yieldNode.Kind()); 
        }
    }
}
