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
        return b is
        {
            P1: 1, P2: 2
        };
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
        return b.MetadataName == string.Empty &&
            b.ContainingType == null &&
            b.ContainingNamespace != null &&
            b.ContainingNamespace.Name == string.Empty [||]&&
            b.ContainingNamespace.ContainingNamespace == null;
    }
}",
@"class C
{
    bool M()
    {
        return b is
        {
            MetadataName: string.Empty, ContainingType: null, ContainingNamespace:
            {
                Name: string.Empty, ContainingNamespace: null
            }
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
            declContainer.Kind == SymbolKind.Namespace [||]&&
            declContainer.Name == ""System"";
    }
}",
@"class C
{
    bool M()
    {
        return type is
        {
            Name: ""ValueTuple"", ContainingSymbol:
            {
                Name: ""System"", Kind: SymbolKind.Namespace
            }

            declContainer
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
            declContainer.Kind == SymbolKind.Namespace [||]&&
            declContainer.Name == ""System"";
    }
}",
@"class C
{
    bool M()
    {
        return type is
        {
            Name: ""ValueTuple"", ContainingSymbol: SomeType
            {
                Name: ""System"", Kind: SymbolKind.Namespace
            }

            declContainer
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
        return type is
        {
            Name: ""ValueTuple"", IsStruct: true
        };
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
        return type is
        {
            Name: ""ValueTuple"", IsClass: false
        };
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
            declContainer.Kind == SymbolKind.Namespace &&
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
                ContainingSymbol: NamespaceSymbol
                {
                    IsGlobalNamespace: true
                }

                , Name: ""System"", Kind: SymbolKind.Namespace
            }

            declContainer
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
            ContainingSymbol: NamespaceSymbol
            {
                IsGlobalNamespace: true
            }
        };
    }
}");
        }
    }
}
