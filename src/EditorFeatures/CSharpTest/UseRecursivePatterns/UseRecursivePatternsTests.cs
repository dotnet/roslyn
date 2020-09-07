// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseRecursivePatterns
{
    public class UseRecursivePatternsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
           => new CSharpUseRecursivePatternsRefactoringProvider();

        [Fact]
        public async Task Test00()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C b)
    {
        return b.P1 == 1 [||]|| b.P1 == 2;
    }
    public int P1, P2;
}",
@"class C
{
    bool M(C b)
    {
        return b is { P1: 1 or 2 };
    }
    public int P1, P2;
}");
        }

        [Fact]
        public async Task Test01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C b)
    {
        return b.P1 == null [||]&& b.P2 == null;
    }
    public object P1, P2;
}",
@"class C
{
    bool M(C b)
    {
        return b is { P1: null, P2: null };
    }
    public object P1, P2;
}");
        }

        [Fact]
        public async Task Test02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C b)
    {
        return b.MetadataName == ""a"" &&
            b.ContainingType == null &&
            b.ContainingNamespace != null &&
            b.ContainingNamespace.Name == ""a"" [||]&&
            b.ContainingNamespace.ContainingNamespace == null;
    }
    string MetadataName;
    C ContainingType;
    C ContainingNamespace;
    string Name;
}",
@"class C
{
    bool M(C b)
    {
        return b is
        {
            MetadataName: ""a"",
            ContainingType: null,
            ContainingNamespace: { Name: ""a"", ContainingNamespace: null },
        };
    }
    string MetadataName;
    C ContainingType;
    C ContainingNamespace;
    string Name;
}");
        }

        [Fact]
        public async Task TestAllInOne()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C type, bool a)
    {
        return 
            (type.Name == 1 || 
            type.Name == 2 ||
            type.Name == 3) &&
            type.Nullable1.HasValue &&
            !type.Nullable2.HasValue &&
            type.Nullable3.Value == 5 &&
            type.SomeProp1 &&
            !type.SomeProp2 &&
            !type.SomeProp3 && 
            !(type.SomeProp4 &&
              type.SomeProp5 &&
              type.SomeProp6) &&
            (type.Kind == 4 || type.Kind == 5 || type.Kind == 6) &&
            type.ContainingSymbol is var declContainer &&
            declContainer != null &&
            declContainer is C &&
            type.ContainingSymbol.ContainingSymbol is var containingSymbol &&
            ((C)declContainer).Kind is int &&
            ((C)declContainer).Kind == 0 &&
            ((C)declContainer).Name == 123 [||]&& a; 
    }
    int Name;
    C ContainingSymbol;
    int Kind;
    int? Nullable1;
    int? Nullable2;
    int? Nullable3;
    int? Nullable4;
    bool SomeProp1;
    bool SomeProp2;
    bool SomeProp3;
    bool SomeProp4;
    bool SomeProp5;
    bool SomeProp6;
}",
@"class C
{
    bool M(C type, bool a)
    {
        return
            a && type is
            {
                Name: >= 1 and <= 3,
                Nullable1: not null,
                Nullable2: null,
                Nullable3: 5,
                SomeProp1: true,
                SomeProp2: false,
                SomeProp3: false,
                SomeProp4: false,
                SomeProp5: false,
                SomeProp6: false,
                Kind: >= 4 and <= 6,
                ContainingSymbol: C
                {
                    ContainingSymbol: var containingSymbol,
                    Kind: int and 0,
                    Name: 123,
                } declContainer,
            }; 
    }
    int Name;
    C ContainingSymbol;
    int Kind;
    int? Nullable1;
    int? Nullable2;
    int? Nullable3;
    int? Nullable4;
    bool SomeProp1;
    bool SomeProp2;
    bool SomeProp3;
    bool SomeProp4;
    bool SomeProp5;
    bool SomeProp6;
}");
        }

        [Fact]
        public async Task Test04()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C type)
    {
        return type.Name == ""ValueTuple"" &&
            type.ContainingSymbol is SomeType declContainer &&
            declContainer.Kind == 0 [||]&&
            declContainer.Name == ""System"";
    }
    string Name;
    C ContainingSymbol;
    int Kind;
    class SomeType : C {}
}",
@"class C
{
    bool M(C type)
    {
        return type is
        {
            Name: ""ValueTuple"",
            ContainingSymbol: SomeType { Kind: 0, Name: ""System"" } declContainer,
        };
    }
    string Name;
    C ContainingSymbol;
    int Kind;
    class SomeType : C {}
}");
        }

        [Fact]
        public async Task Test05()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C type)
    {
        return type.Name == ""ValueTuple"" [||]&& type.IsStruct;
    }
    string Name;
    bool IsStruct;
}",
@"class C
{
    bool M(C type)
    {
        return type is { Name: ""ValueTuple"", IsStruct: true };
    }
    string Name;
    bool IsStruct;
}");
        }

        [Fact]
        public async Task Test06()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C type)
    {
        return type.Name == ""ValueTuple"" [||]&& !type.IsClass;
    }
    string Name;
    bool IsClass;
}",
@"class C
{
    bool M(C type)
    {
        return type is { Name: ""ValueTuple"", IsClass: false };
    }
    string Name;
    bool IsClass;
}");
        }

        [Fact]
        public async Task Test07()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C type)
    {
        return type.ContainingSymbol is var declContainer &&
            declContainer.Kind == 0 &&
            declContainer.Name == ""System"" [||]&&
            (declContainer.ContainingSymbol as NamespaceSymbol)?.IsGlobalNamespace == true;
    }
    C ContainingSymbol;
    int Kind;
    string Name;
    class NamespaceSymbol : C {}
    bool IsGlobalNamespace;
}",
@"class C
{
    bool M(C type)
    {
        return type is
        {
            ContainingSymbol:
            {
                Kind: 0,
                Name: ""System"",
                ContainingSymbol: NamespaceSymbol { IsGlobalNamespace: true },
            } declContainer,
        };
    }
    C ContainingSymbol;
    int Kind;
    string Name;
    class NamespaceSymbol : C {}
    bool IsGlobalNamespace;
}");
        }

        [Fact]
        public async Task Test08()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C c)
    {
        return (c.ContainingSymbol as NamespaceSymbol)?.IsGlobalNamespace [||]== true;
    }
    C ContainingSymbol;
    bool IsGlobalNamespace;
    class NamespaceSymbol : C {}
}",
@"class C
{
    bool M(C c)
    {
        return c is
        {
            ContainingSymbol: NamespaceSymbol { IsGlobalNamespace: true },
        };
    }
    C ContainingSymbol;
    bool IsGlobalNamespace;
    class NamespaceSymbol : C {}
}");
        }

        [Fact]
        public async Task Test09()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object node, bool b)
    {
        switch (node)
        {
            case string s [||]when s.Length == 0:
                break;
        }
    }
}",
@"class C
{
    void M(object node, bool b)
    {
        switch (node)
        {
            case string { Length: 0 } s:
                break;
        }
    }
}");
        }

        [Fact]
        public async Task Test10()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object node, bool b)
    {
        switch (node)
        {
            case string s [||]when s.Length == 0 && b:
                break;
        }
    }
}",
@"class C
{
    void M(object node, bool b)
    {
        switch (node)
        {
            case string { Length: 0 } s when b:
                break;
        }
    }
}");
        }

        [Fact]
        public async Task Test16()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object node, bool b, bool c)
    {
        switch (node)
        {
            case string s [||]when b && s.Length is int && c:
                break;
        }
    }
}",
@"class C
{
    void M(object node, bool b, bool c)
    {
        switch (node)
        {
            case string { Length: int } s when b && c:
                break;
        }
    }
}");
        }

        [Fact]
        public async Task Test17()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object node, bool b, bool c)
    {
        switch (node)
        {
            case string s [||]when s.Length is int && b && c:
                break;
        }
    }
}",
@"class C
{
    void M(object node, bool b, bool c)
    {
        switch (node)
        {
            case string { Length: int } s when b && c:
                break;
        }
    }
}");
        }

        [Fact]
        public async Task Test18()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(object node, bool b, bool c)
    {
        switch (node)
        {
            case string s [||]when b && c && s.Length is int:
                break;
        }
    }
}",
@"class C
{
    void M(object node, bool b, bool c)
    {
        switch (node)
        {
            case string { Length: int } s when b && c:
                break;
        }
    }
}");
        }

        [Fact]
        public async Task Test11()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int P1, P2;

    bool M(C b, bool x)
    {
        return b.P1 == 1 && b.P2 == 2 [||]&& x;
    }
}",
@"class C
{
    int P1, P2;

    bool M(C b, bool x)
    {
        return x && b is { P1: 1, P2: 2 };
    }
}");
        }

        [Fact]
        public async Task Test12()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C b, bool x)
    {
        return x && b.P1 == 1 [||]&& b.P2 == 2;
    }
    int P1, P2;
}",
@"class C
{
    bool M(C b, bool x)
    {
        return x && b is { P1: 1, P2: 2 };
    }
    int P1, P2;
}");
        }

        [Fact]
        public async Task Test13()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C x)
    {
        return x is var v [||]&& v != null;
    }
}",
@"class C
{
    bool M(C x)
    {
        return x is { } v;
    }
}");
        }

        [Fact]
        public async Task Test14()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C x)
    {
        return x is var v [||]&& v is object;
    }
}",
@"class C
{
    bool M(C x)
    {
        return x is object v;
    }
}");
        }

        [Fact]
        public async Task Test15()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C c)
    {
        return c is var (x, y) && x is SomeType [||]&& y != null;
    }
    public void Deconstruct(out object i, out object j) => throw null;
    class SomeType {}
}",
@"class C
{
    bool M(C c)
    {
        return c is (SomeType x, { } y);
    }
    public void Deconstruct(out object i, out object j) => throw null;
    class SomeType {}
}");
        }

        [Fact]
        public async Task Test19()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C container, C _scopeBinder)
    {
        if ((object)container != null && container.IsScriptClass [||]&&
            (object)_scopeBinder.ToString() != null)
        {
        }
    }
    bool IsScriptClass;
}",
@"class C
{
    bool M(C container, C _scopeBinder)
    {
        if (container is { IsScriptClass: true } && _scopeBinder.ToString() is not null)
        {
        }
    }
    bool IsScriptClass;
}");
        }

        [Fact]
        public async Task Test20()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C fieldSymbol)
    {
        if ((object)fieldSymbol != null [||]&& fieldSymbol.IsConst)
        {
        }
    }
    bool IsConst;
}",
@"class C
{
    bool M(C fieldSymbol)
    {
        if (fieldSymbol is { IsConst: true })
        {
        }
    }
    bool IsConst;
}");
        }

        [Fact]
        public async Task Test21()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        while (expressionType [||]is ArrayTypeSymbol { ElementType: var e1, IsSZArray: var sz1, Rank: var r1 } &&
               patternType is ArrayTypeSymbol { ElementType: var e2, IsSZArray: var sz2, Rank: var r2 } &&
               sz1 == sz2 && r1 == r2)
        {
        }
    }
}");
        }

        [Fact]
        public async Task Test22()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(object uncommonData)
    {
        if (uncommonData is C deconstruction
            [||]&& deconstruction.DeconstructMethodInfo.Invocation is BoundCall call)
        {
        }
    }
    C DeconstructMethodInfo;
    C Invocation;
    class BoundCall { }
}",
@"class C
{
    bool M(object uncommonData)
    {
        if (uncommonData is C
            {
                DeconstructMethodInfo: { Invocation: BoundCall call },
            } deconstruction)
        {
        }
    }
    C DeconstructMethodInfo;
    C Invocation;
    class BoundCall { }
}");
        }

        [Fact]
        public async Task Test23()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(bool a, bool b, bool c)
    {
        return
            this.Kind == 0 &&
            a &&
            b &&
            c [||]&&
            this.Method == null;
    }
    int Kind;
    object Method;
}",
@"class C
{
    bool M(bool a, bool b, bool c)
    {
        return
            a && b && c && this is { Kind: 0, Method: null };
    }
    int Kind;
    object Method;
}");
        }

        [Fact]
        public async Task Test24()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        while (initializer.Parent != null &&
               initializer.Parent.Kind == SyntaxKind.SimpleAssignmentExpression &&
               ((AssignmentExpressionSyntax)initializer.Parent).Right == initializer &&
               initializer.Parent.Parent != null [||]&&
               initializer.Parent.Parent.Kind == SyntaxKind.ObjectInitializerExpression)
        {
        }
    }
    class AssignmentExpressionSyntax : C { }
    C Right;
    C Parent;
    SyntaxKind Kind;
    const C initializer = null;
    enum SyntaxKind { SimpleAssignmentExpression, ObjectInitializerExpression }
}",
@"class C
{
    bool M()
    {
        while (initializer is
            {
                Parent: AssignmentExpressionSyntax
                {
                    Kind: SyntaxKind.SimpleAssignmentExpression,
                    Right: initializer,
                    Parent: { Kind: SyntaxKind.ObjectInitializerExpression },
                },
            })
        {
        }
    }
    class AssignmentExpressionSyntax : C { }
    C Right;
    C Parent;
    SyntaxKind Kind;
    const C initializer = null;
    enum SyntaxKind { SimpleAssignmentExpression, ObjectInitializerExpression }
}");
        }

        [Fact]
        public async Task Test25()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(C method)
    {
        if (!method.IsStatic [||]&& method.ContainingType.IsValueType)
        {
        }
    }
    bool IsStatic;
    C ContainingType;
    bool IsValueType;
}",
@"class C
{
    bool M(C method)
    {
        if (method is { IsStatic: false, ContainingType: { IsValueType: true } })
        {
        }
    }
    bool IsStatic;
    C ContainingType;
    bool IsValueType;
}");
        }

        [Fact]
        public async Task Test28()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return this.P1 == otherMetadataImportOptions
            && this.P2 == otherReferencesSupersedeLowerVersions
            [||]&& this.ToString() != null;
    }
    object P1;
    object P2;
    const object otherMetadataImportOptions = null;
    const object otherReferencesSupersedeLowerVersions = null;
}",
@"class C
{
    bool M()
    {
        return this is
        {
            P1: otherMetadataImportOptions,
            P2: otherReferencesSupersedeLowerVersions,
        } && this.ToString() is not null;
    }
    object P1;
    object P2;
    const object otherMetadataImportOptions = null;
    const object otherReferencesSupersedeLowerVersions = null;
}");
        }

        [Fact]
        public async Task Test29()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(int i)
    {
        return i is 1 || i is 2 [||]|| i is 3;
    }
}",
@"class C
{
    bool M(int i)
    {
        return i is >= 1 and <= 3;
    }
}");
        }

        [Fact]
        public async Task Test30()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(X i)
    {
        return i is X.A || i is (X)80 || i is X.B [||]|| i is X.C;
    }
    enum X { A = 1, B = 2, C = 3 }
}",
@"class C
{
    bool M(X i)
    {
        return i is X.A or (X)80 or X.B or X.C;
    }
    enum X { A = 1, B = 2, C = 3 }
}");
        }

        [Fact]
        public async Task Test31()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M((int, int) b)
    {
        return b.Item1 == 1 [||]&& b.Item2 == 2;
    }
}",
@"class C
{
    bool M((int, int) b)
    {
        return b is (1, 2);
    }
}");
        }

        [Fact]
        public async Task Test32()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M((int, int) b)
    {
        return b is (1, 2) [||]|| b.Item1 == 3;
    }
}",
@"class C
{
    bool M((int, int) b)
    {
        return b is (1, 2) or (3, _);
    }
}");
        }

        [Fact]
        public async Task Test33()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M((int, int) b)
    {
        return b is ({ }, 2) [||]&& b.Item1 == 3;
    }
}",
@"class C
{
    bool M((int, int) b)
    {
        return b is (3, 2);
    }
}");
        }

        [Fact]
        public async Task Test34()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M((int, int) b)
    {
        return b is (1, 2) [||]&& b.Item1 == 3;
    }
}",
@"class C
{
    bool M((int, int) b)
    {
        return b is (1 and 3, 2);
    }
}");
        }

        [Fact]
        public async Task Test35()
        {
            const string iTupleSource = @"
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }
        object this[int index] { get; }
    }
}
";
            await TestInRegularAndScriptAsync(
@"using System.Runtime.CompilerServices;
class C
{
    bool M(object b)
    {
        return b is ITuple t [||]&& t[1] == 1;
    }
}" + iTupleSource,
@"using System.Runtime.CompilerServices;
class C
{
    bool M(object b)
    {
        return b is ITuple t and (_, 1);
    }
}" + iTupleSource);
        }

        [Fact]
        public async Task Test36()
        {
            const string iTupleSource = @"
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }
        object this[int index] { get; }
    }
}
";
            await TestInRegularAndScriptAsync(
@"using System.Runtime.CompilerServices;
class C
{
    bool M(object b)
    {
        return b is IMyTuple t && t[1] == 1 [||]&& t.Length == 3;
    }
    interface IMyTuple : ITuple { }
}" + iTupleSource,
@"using System.Runtime.CompilerServices;
class C
{
    bool M(object b)
    {
        return b is IMyTuple t and (_, 1, _);
    }
    interface IMyTuple : ITuple { }
}" + iTupleSource);
        }

        [Fact]
        public async Task Test37()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private static IOperation ParseConstantPattern(int x, IOperation op)
    {
        return x switch
        {
            1 [||]when op.LeftOperand.Syntax is ExpressionSyntax left => default,
            _ => null
        };
    }
    class ExpressionSyntax { }
    class IOperation
    {
        public object Syntax;
        public IOperation LeftOperand;
        public IOperation RightOperand;
    }
}",
@"class C
{
    private static IOperation ParseConstantPattern(int x, IOperation op)
    {
        return x switch
        {
            1 when op is { LeftOperand: { Syntax: ExpressionSyntax left } } => default,
            _ => null
        };
    }
    class ExpressionSyntax { }
    class IOperation
    {
        public object Syntax;
        public IOperation LeftOperand;
        public IOperation RightOperand;
    }
}");
        }

        [Fact]
        public async Task Test38()
        {
            await TestMissingAsync(
@"class C
{
    object SomeProp;

    static bool M(object node)
    {
        return !(node is C c) [||]|| c.SomeProp is C;
    }
}");
        }

        [Fact]
        public async Task TestImplicitThisReceiver()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    C b;
    bool M()
    {
        return b.P1 == 1 [||]|| b.P1 == 2;
    }
    public int P1, P2;
}",
@"class C
{
    C b;
    bool M()
    {
        return b is { P1: 1 or 2 };
    }
    public int P1, P2;
}");
        }

        [Fact]
        public async Task TestBaseMemberAccess01()
        {
            await TestInRegularAndScriptAsync(
@"class C : B
{
    bool M()
    {
        return base.b.P1 == 1 [||]|| base.b.P1 == 2;
    }
}
class B
{
    public B b;
    public int P1, P2;
}",
@"class C : B
{
    bool M()
    {
        return base.b is { P1: 1 or 2 };
    }
}
class B
{
    public B b;
    public int P1, P2;
}");
        }

        [Fact]
        public async Task TestBaseMemberAccess02()
        {
            await TestInRegularAndScriptAsync(
@"class C : B
{
    bool M()
    {
        return base.b.P1 == 1 [||]|| this.b.P1 == 2 || b.P1 == 3;
    }
}
class B
{
    public B b;
    public int P1, P2;
}",
@"class C : B
{
    bool M()
    {
        return base.b is { P1: 1 } || this is { b: { P1: 2 } } || b is { P1: 3 };
    }
}
class B
{
    public B b;
    public int P1, P2;
}");
        }

        [Fact]
        public async Task Test40()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return !this.IsUserDefined [||]||
            this.Method is object ||
            this._uncommonData?._conversionResult.Kind == 0;
    }
    bool IsUserDefined;
    object Method;
    C _uncommonData;
    C _conversionResult;
    int Kind;
}",
@"class C
{
    bool M()
    {
        return this is { IsUserDefined: false } or { Method: object } or { _uncommonData: { _conversionResult: { Kind: 0 } } };
    }
    bool IsUserDefined;
    object Method;
    C _uncommonData;
    C _conversionResult;
    int Kind;
}");
        }

        [Fact]
        public async Task Test41()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return [||]!this.IsUserDefined;
    }
    bool IsUserDefined;
}",
@"class C
{
    bool M()
    {
        return this is { IsUserDefined: false };
    }
    bool IsUserDefined;
}");
        }

        [Fact]
        public async Task Test42()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return this._uncommonData?._conversionResult.Kind [||]== 0;
    }
    bool IsUserDefined;
    object Method;
    C _uncommonData;
    C _conversionResult;
    int Kind;
}",
@"class C
{
    bool M()
    {
        return this is { _uncommonData: { _conversionResult: { Kind: 0 } } };
    }
    bool IsUserDefined;
    object Method;
    C _uncommonData;
    C _conversionResult;
    int Kind;
}");
        }
    }
}
