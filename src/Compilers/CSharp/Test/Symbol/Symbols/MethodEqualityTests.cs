// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MethodEqualityTests : CSharpTestBase
    {
        [Fact]
        public void NoGenerics()
        {
            var text = @"
class Class1
{
    void Method1() { }
    void Method2() { }
}

class Class2
{
    void Method1() { }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var class1 = global.GetTypeMembers("Class1").Single();
            var class1Method1 = (MethodSymbol)class1.GetMembers("Method1").Single();
            var class1Method2 = (MethodSymbol)class1.GetMembers("Method2").Single();

            var class2 = global.GetTypeMembers("Class2").Single();
            var class2Method1 = (MethodSymbol)class2.GetMembers("Method1").Single();

            Assert.Equal(class1Method1, class1Method1);

            //null
            Assert.NotNull(class1Method1);
            Assert.NotNull(class1Method1);

            //different type
            Assert.NotEqual<Symbol>(class1, class1Method1);
            Assert.NotEqual<Symbol>(class1Method1, class1);

            //different original definition
            Assert.NotEqual(class1Method1, class1Method2);
            Assert.NotEqual(class1Method2, class1Method1);

            //different containing type
            Assert.NotEqual(class1Method1, class2Method1);
            Assert.NotEqual(class2Method1, class1Method1);
        }

        [Fact]
        public void GenericsTypes()
        {
            var text = @"
class Base<T>
{
    void Method(T t) { }
    void Method(int i) { }
}

class Derived1<S> : Base<S>
{
}

class Derived2 : Base<int>
{
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var baseClass = global.GetTypeMembers("Base").Single();
            var baseClassMethod1 = (MethodSymbol)baseClass.GetMembers("Method").First();
            var baseClassMethod2 = (MethodSymbol)baseClass.GetMembers("Method").Last();

            var derivedClass1 = global.GetTypeMembers("Derived1").Single();
            var substitutedBaseClass = derivedClass1.BaseType();
            var substitutedBaseClassMethod1 = (MethodSymbol)substitutedBaseClass.GetMembers("Method").First();
            var substitutedBaseClassMethod2 = (MethodSymbol)substitutedBaseClass.GetMembers("Method").Last();

            var derivedClass2 = global.GetTypeMembers("Derived2").Single();
            var constructedBaseClass = derivedClass2.BaseType();
            var constructedBaseClassMethod1 = (MethodSymbol)constructedBaseClass.GetMembers("Method").First();
            var constructedBaseClassMethod2 = (MethodSymbol)constructedBaseClass.GetMembers("Method").Last();

            //different type args
            Assert.NotEqual(baseClassMethod1, substitutedBaseClassMethod1);
            Assert.NotEqual(substitutedBaseClassMethod1, baseClassMethod1);

            //different type args
            Assert.NotEqual(baseClassMethod1, constructedBaseClassMethod1);
            Assert.NotEqual(constructedBaseClassMethod1, baseClassMethod1);

            //different type args
            Assert.NotEqual(substitutedBaseClassMethod1, constructedBaseClassMethod1);
            Assert.NotEqual(constructedBaseClassMethod1, substitutedBaseClassMethod1);

            //different original definitions
            Assert.NotEqual(baseClassMethod1, baseClassMethod2);
            Assert.NotEqual(baseClassMethod2, baseClassMethod1);

            //different original definitions
            Assert.NotEqual(substitutedBaseClassMethod1, substitutedBaseClassMethod2);
            Assert.NotEqual(substitutedBaseClassMethod2, substitutedBaseClassMethod1);

            //different original definitions (though signatures and bodies now match)
            Assert.NotEqual(constructedBaseClassMethod1, constructedBaseClassMethod2);
            Assert.NotEqual(constructedBaseClassMethod2, constructedBaseClassMethod1);
        }

        [Fact]
        public void GenericsTypesAndMethods()
        {
            var text = @"
class Base<T>
{
    U Method<U>(T t) { }
    V Method<V>(int i) { }
}

class Derived1<S> : Base<S>
{
}

class Derived2 : Base<int>
{
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var baseClass = global.GetTypeMembers("Base").Single();
            var baseClassMethod1 = (MethodSymbol)baseClass.GetMembers("Method").First();
            var baseClassMethod2 = (MethodSymbol)baseClass.GetMembers("Method").Last();

            var derivedClass1 = global.GetTypeMembers("Derived1").Single();
            var substitutedBaseClass = derivedClass1.BaseType();
            var substitutedBaseClassMethod1 = (MethodSymbol)substitutedBaseClass.GetMembers("Method").First();
            var substitutedBaseClassMethod2 = (MethodSymbol)substitutedBaseClass.GetMembers("Method").Last();

            var derivedClass2 = global.GetTypeMembers("Derived2").Single();
            var constructedBaseClass = derivedClass2.BaseType();
            var constructedBaseClassMethod1 = (MethodSymbol)constructedBaseClass.GetMembers("Method").First();
            var constructedBaseClassMethod2 = (MethodSymbol)constructedBaseClass.GetMembers("Method").Last();

            //different type args
            Assert.NotEqual(baseClassMethod1, substitutedBaseClassMethod1);
            Assert.NotEqual(substitutedBaseClassMethod1, baseClassMethod1);

            //different type args
            Assert.NotEqual(baseClassMethod1, constructedBaseClassMethod1);
            Assert.NotEqual(constructedBaseClassMethod1, baseClassMethod1);

            //different type args
            Assert.NotEqual(substitutedBaseClassMethod1, constructedBaseClassMethod1);
            Assert.NotEqual(constructedBaseClassMethod1, substitutedBaseClassMethod1);

            //different original definitions
            Assert.NotEqual(baseClassMethod1, baseClassMethod2);
            Assert.NotEqual(baseClassMethod2, baseClassMethod1);

            //different original definitions
            Assert.NotEqual(substitutedBaseClassMethod1, substitutedBaseClassMethod2);
            Assert.NotEqual(substitutedBaseClassMethod2, substitutedBaseClassMethod1);

            //different original definitions (though signatures and bodies now match)
            Assert.NotEqual(constructedBaseClassMethod1, constructedBaseClassMethod2);
            Assert.NotEqual(constructedBaseClassMethod2, constructedBaseClassMethod1);
        }

        [Fact]
        public void SubstitutedGenericMethods()
        {
            var text = @"
class Class
{
    void Method<U, V>(U u, V v)
    {
        Method<char, int>('a', 1); //0
        Method<char, int>('b', 2); //1
        Method<int, char>(1, 'a'); //2
        Method<U, V>(u, v); //3
        Method<U, V>(u, v); //4
        Method<V, U>(v, u); //5
    }
}
";
            var comp = (Compilation)CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var @class = global.GetTypeMembers("Class").Single();
            var classMethodDeclaration = (IMethodSymbol)@class.GetMembers("Method").Single();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var root = tree.GetCompilationUnitRoot();
            var cDecl = (TypeDeclarationSyntax)root.Members[0];
            var mDecl = (MethodDeclarationSyntax)cDecl.Members[0];
            var stmts = mDecl.Body.Statements;

            var invokedMethods = stmts.Select(stmt =>
            {
                var exprStmt = (ExpressionStatementSyntax)stmt;
                var semanticInfo = model.GetSymbolInfo(exprStmt.Expression);
                return (IMethodSymbol)semanticInfo.Symbol;
            }).ToArray();

            Assert.Equal(6, invokedMethods.Length);

            //invocations are equal to themselves
            Assert.True(invokedMethods.All(m => m.Equals(m)));

            //invocations may be equal to declarations if type arguments are the same
            Assert.NotEqual(invokedMethods[0], classMethodDeclaration);
            Assert.NotEqual(invokedMethods[1], classMethodDeclaration);
            Assert.NotEqual(invokedMethods[2], classMethodDeclaration);
            Assert.Equal(invokedMethods[3], classMethodDeclaration);
            Assert.Equal(invokedMethods[4], classMethodDeclaration);
            Assert.NotEqual(invokedMethods[5], classMethodDeclaration);

            //invocations are equal to other invocations with the same substitutions
            Assert.Equal(invokedMethods[0], invokedMethods[1]);
            Assert.Equal(invokedMethods[1], invokedMethods[0]);

            Assert.Equal(invokedMethods[3], invokedMethods[4]);
            Assert.Equal(invokedMethods[4], invokedMethods[3]);

            //invocations with different type args are not equal
            var pairWiseNotEqual = new IMethodSymbol[]
            {
                invokedMethods[0],
                invokedMethods[2],
                invokedMethods[3],
                invokedMethods[5],
            };

            foreach (var method1 in pairWiseNotEqual)
            {
                foreach (var method2 in pairWiseNotEqual)
                {
                    if (ReferenceEquals(method1, method2))
                    {
                        continue;
                    }

                    Assert.NotEqual(method1, method2);
                    Assert.NotEqual(method2, method1);
                }
            }
        }
    }
}
