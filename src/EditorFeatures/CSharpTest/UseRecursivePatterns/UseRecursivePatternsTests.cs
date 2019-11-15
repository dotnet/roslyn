// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseRecursivePatterns
{
    public partial class UseRecursivePatternsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
           => new CSharpUseRecursivePatternsCodeRefactoringProvider();

        [Fact]
        public async Task Test1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return b.P1 == 1 [||]&& b.P2 == 2;
    }
}",
@"class C
{
    bool M()
    {
        return b is { P1: 1, P2: 2 };
    }
}");
        }

        [Fact]
        public async Task Test2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return b.MetadataName == ""a"" &&
            b.ContainingType == null &&
            b.ContainingNamespace != null &&
            b.ContainingNamespace.Name == ""a"" [||]&&
            b.ContainingNamespace.ContainingNamespace == null;
    }
}",
@"class C
{
    bool M()
    {
        return b is
        {
            MetadataName: ""a"",
            ContainingType: null,
            ContainingNamespace: { Name: ""a"", ContainingNamespace: null }
        };
    }
}");
        }

        [Fact]
        public async Task Test3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return type.Name == ""ValueTuple"" && 
            type.ContainingSymbol is var declContainer &&
            declContainer.Kind == 0 [||]&&
            declContainer.Name == ""System"";
    }
}",
@"class C
{
    bool M()
    {
        return type is
        {
            Name: ""ValueTuple"",
            ContainingSymbol: { Name: ""System"", Kind: 0 } declContainer
        };
    }
}");
        }

        [Fact]
        public async Task Test4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return type.Name == ""ValueTuple"" &&
            type.ContainingSymbol is SomeType declContainer &&
            declContainer.Kind == 0 [||]&&
            declContainer.Name == ""System"";
    }
}",
@"class C
{
    bool M()
    {
        return type is
        {
            Name: ""ValueTuple"",
            ContainingSymbol: SomeType { Name: ""System"", Kind: 0 } declContainer
        };
    }
}");
        }

        [Fact]
        public async Task Test5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return type.Name == ""ValueTuple"" [||]&& type.IsStruct;
    }
}",
@"class C
{
    bool M()
    {
        return type is { Name: ""ValueTuple"", IsStruct: true };
    }
}");
        }

        [Fact]
        public async Task Test6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return type.Name == ""ValueTuple"" [||]&& !type.IsClass;
    }
}",
@"class C
{
    bool M()
    {
        return type is { Name: ""ValueTuple"", IsClass: false };
    }
}");
        }

        [Fact]
        public async Task Test7()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return type.ContainingSymbol is var declContainer &&
            declContainer.Kind == 0 &&
            declContainer.Name == ""System"" [||]&&
            (declContainer.ContainingSymbol as NamespaceSymbol)?.IsGlobalNamespace == true;
    }
}",
@"class C
{
    bool M()
    {
        return type is
        {
            ContainingSymbol:
            {
                ContainingSymbol: NamespaceSymbol { IsGlobalNamespace: true },
                Name: ""System"",
                Kind: 0
            } declContainer
        };
    }
}");
        }

        [Fact]
        public async Task Test8()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return (declContainer.ContainingSymbol as NamespaceSymbol)?.IsGlobalNamespace [||]== true;
    }
}",
@"class C
{
    bool M()
    {
        return declContainer is
        {
            ContainingSymbol: NamespaceSymbol { IsGlobalNamespace: true }
        };
    }
}");
        }

        [Fact]
        public async Task Test9()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (node)
        {
            case String s [||]when s.Length == 0:
                break;
        }
    }
}",
@"class C
{
    void M()
    {
        switch (node)
        {
            case String { Length: 0 } s:
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
    void M()
    {
        switch (node)
        {
            case String s [||]when s.Length == 0 && b:
                break;
        }
    }
}",
@"class C
{
    void M()
    {
        switch (node)
        {
            case String { Length: 0 } s when b:
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
    void M()
    {
        switch (node)
        {
            case String s [||]when b && s.Length is int && c:
                break;
        }
    }
}",
@"class C
{
    void M()
    {
        switch (node)
        {
            case String { Length: int _ } s when b && c:
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
    void M()
    {
        switch (node)
        {
            case String s [||]when s.Length is int && b && c:
                break;
        }
    }
}",
@"class C
{
    void M()
    {
        switch (node)
        {
            case String { Length: int _ } s when b && c:
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
    void M()
    {
        switch (node)
        {
            case String s [||]when b && c && s.Length is int:
                break;
        }
    }
}",
@"class C
{
    void M()
    {
        switch (node)
        {
            case String { Length: int _ } s when b && c:
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
    bool M()
    {
        return b.P1 == 1 && b.P2 == 2 [||]&& x;
    }
}",
@"class C
{
    bool M()
    {
        return b is { P1: 1, P2: 2 } && x;
    }
}");
        }

        [Fact]
        public async Task Test12()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return x && b.P1 == 1 [||]&& b.P2 == 2;
    }
}",
@"class C
{
    bool M()
    {
        return x && b is { P1: 1, P2: 2 };
    }
}");
        }

        [Fact]
        public async Task Test13()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return x is var v [||]&& v != null;
    }
}",
@"class C
{
    bool M()
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
        return x is { } v;
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
        return c is var (x, y) [||]&& x is SomeType && y != null;
    }
}",
@"class C
{
    bool M(C c)
    {
        return c is (SomeType x, { } y);
    }
}");
        }

        [Fact]
        public async Task Test19()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        if ((object)container != null [||]&& container.IsScriptClass &&
            (object)_scopeBinder.LookupDeclaredField(designation) != null)
        {
        }
    }
}",
@"class C
{
    bool M()
    {
        if (container is { IsScriptClass: true } &&
            (object)_scopeBinder.LookupDeclaredField(designation) is { })
        {
        }
    }
}");
        }

        [Fact]
        public async Task Test20()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        if ((object)fieldSymbol != null [||]&& fieldSymbol.IsConst)
        {
        }
    }
}",
@"class C
{
    bool M()
    {
        if (fieldSymbol is { IsConst: true })
        {
        }
    }
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
    bool M()
    {
        if (uncommonData [||]is DeconstructionUncommonData deconstruction
            && deconstruction.DeconstructMethodInfo.Invocation is BoundCall call)
        {
        }
    }
}",
@"class C
{
    bool M()
    {
        if (uncommonData is DeconstructionUncommonData
        {
            DeconstructMethodInfo: { Invocation: BoundCall call }
        } deconstruction)
        {
        }
    }
}");
        }

        [Fact]
        public async Task Test23()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return
            this.Kind [||]== 0 &&
            TypeSymbol.Equals(this.LeftType, other.LeftType, TypeCompareKind.ConsiderEverything2) &&
            TypeSymbol.Equals(this.RightType, other.RightType, TypeCompareKind.ConsiderEverything2) &&
            TypeSymbol.Equals(this.ReturnType, other.ReturnType, TypeCompareKind.ConsiderEverything2) &&
            this.Method == null;
    }
}",
@"class C
{
    bool M()
    {
        return
            this is { Kind: 0, Method: null } &&
            TypeSymbol.Equals(this.LeftType, other.LeftType, TypeCompareKind.ConsiderEverything2) &&
            TypeSymbol.Equals(this.RightType, other.RightType, TypeCompareKind.ConsiderEverything2) &&
            TypeSymbol.Equals(this.ReturnType, other.ReturnType, TypeCompareKind.ConsiderEverything2);
    }
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
               initializer.Parent.Parent != null &&
               initializer.Parent.Parent.Kind [||]== SyntaxKind.ObjectInitializerExpression)
        {
        }
    }
    const object initializer = null;
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
                Right: initializer,
                Kind: SyntaxKind.SimpleAssignmentExpression,
                Parent: { Kind: SyntaxKind.ObjectInitializerExpression }
            }
        })
        {
        }
    }
    const object initializer = null;
    enum SyntaxKind { SimpleAssignmentExpression, ObjectInitializerExpression }
}");
        }

        [Fact]
        public async Task Test25()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        if (!method.IsStatic [||]&& method.ContainingType.IsValueType)
        {
        }
    }
}",
@"class C
{
    bool M()
    {
        if (method is { IsStatic: false, ContainingType: { IsValueType: true } })
        {
        }
    }
}");
        }

        [Fact]
        public async Task Test26()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        return type.IsStructType() && type.Name == ""ValueTuple"" && type.GetArity() == 0 &&
            type.ContainingSymbol is var declContainer && declContainer.Kind == SymbolKind.Namespace [||]&& declContainer.Name == ""System"" &&
            (declContainer.ContainingSymbol as NamespaceSymbol)?.IsGlobalNamespace == true;
    }
    enum SymbolKind { Namespace }
}",
@"class C
{
    bool M()
    {
        return type.IsStructType() && type is
        {
            Name: ""ValueTuple"",
            ContainingSymbol:
            {
                ContainingSymbol: NamespaceSymbol { IsGlobalNamespace: true },
                Name: ""System"",
                Kind: SymbolKind.Namespace
            } declContainer
        } && type.GetArity() is 0;
    }
    enum SymbolKind { Namespace }
}");
        }

        [Fact]
        public async Task Test27()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M()
    {
        if (_lastCompleteBlock [||]!= null &&
            _lastCompleteBlock.BranchCode == ILOpCode.Nop &&
            _lastCompleteBlock.LastILMarker >= 0 &&
            _allocatedILMarkers[_lastCompleteBlock.LastILMarker].BlockOffset == _lastCompleteBlock.RegularInstructionsLength)
        {
        }
    }
    enum ILOpCode { Nop }
}",
@"class C
{
    bool M()
    {
        if (_lastCompleteBlock is { BranchCode: ILOpCode.Nop } && _lastCompleteBlock.LastILMarker >= 0 &&
            _allocatedILMarkers[_lastCompleteBlock.LastILMarker].BlockOffset == _lastCompleteBlock.RegularInstructionsLength)
        {
        }
    }
    enum ILOpCode { Nop }
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
        return this.MetadataImportOptions == otherMetadataImportOptions
            && this.ReferencesSupersedeLowerVersions == otherReferencesSupersedeLowerVersions
            && this.OutputKind.IsNetModule() [||]== other.OutputKind.IsNetModule();
    }
    const object otherMetadataImportOptions = null;
    const object otherReferencesSupersedeLowerVersions = null;
}",
@"class C
{
    bool M()
    {
        return this is
        {
            MetadataImportOptions: otherMetadataImportOptions,
            ReferencesSupersedeLowerVersions: otherReferencesSupersedeLowerVersions
        } &&
this.OutputKind.IsNetModule() == other.OutputKind.IsNetModule();
    }
    const object otherMetadataImportOptions = null;
    const object otherReferencesSupersedeLowerVersions = null;
}");
        }
    }
}
