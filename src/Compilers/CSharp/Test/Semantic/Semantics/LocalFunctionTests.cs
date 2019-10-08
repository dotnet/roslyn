// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocalFunctionsTestBase : CSharpTestBase
    {
        internal static readonly CSharpParseOptions DefaultParseOptions = TestOptions.Regular;

        internal static void VerifyDiagnostics(string source, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilationWithMscorlib45AndCSharp(source, options: TestOptions.ReleaseDll, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(expected);
        }
    }

    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public class LocalFunctionTests : LocalFunctionsTestBase
    {
        [Fact, WorkItem(29656, "https://github.com/dotnet/roslyn/issues/29656")]
        public void RefReturningAsyncLocalFunction()
        {
            var source = @"
public class C
{
    async ref System.Threading.Tasks.Task M() { }

    public void M2()
    {
        _ = local();

        async ref System.Threading.Tasks.Task local() { }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,11): error CS1073: Unexpected token 'ref'
                //     async ref System.Threading.Tasks.Task M() { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(4, 11),
                // (4,43): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async ref System.Threading.Tasks.Task M() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 43),
                // (10,15): error CS1073: Unexpected token 'ref'
                //         async ref System.Threading.Tasks.Task local() { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(10, 15),
                // (10,47): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         async ref System.Threading.Tasks.Task local() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "local").WithLocation(10, 47)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void LocalFunctionResetsLockScopeFlag()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        lock (new Object())
        {
            async Task localFunc()
            {
                Console.Write(""localFunc"");
                await Task.Yield();
            }

            localFunc();
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "localFunc");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void LocalFunctionResetsTryCatchFinallyScopeFlags()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        try
        {
            IEnumerable<int> localFunc()
            {
                yield return 1;
            }

            foreach (int i in localFunc())
            {
                Console.Write(i);
            }

            throw new Exception();
        }
        catch (Exception)
        {
            IEnumerable<int> localFunc()
            {
                yield return 2;
            }

            foreach (int i in localFunc())
            {
                Console.Write(i);
            }
        }
        finally
        {
            IEnumerable<int> localFunc()
            {
                yield return 3;
            }

            foreach (int i in localFunc())
            {
                Console.Write(i);
            }
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "123");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void LocalFunctionDoesNotOverwriteInnerLockScopeFlag()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    void M(List<Task> listOfTasks)
    {
        lock (new Object())
        {
            async Task localFunc()
            {
                lock (new Object())
                {
                    await Task.Yield();
                }
            }

            listOfTasks.Add(localFunc());
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (16,21): error CS1996: Cannot await in the body of a lock statement
                //                     await Task.Yield();
                Diagnostic(ErrorCode.ERR_BadAwaitInLock, "await Task.Yield()").WithLocation(16, 21));
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void LocalFunctionDoesNotOverwriteInnerTryCatchFinallyScopeFlags()
        {
            var source = @"
using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        try
        {
            IEnumerable<int> localFunc()
            {
                try
                {
                    yield return 1;
                }
                catch (Exception) {}
            }

            localFunc();
        }
        catch (Exception)
        {
            IEnumerable<int> localFunc()
            {
                try {}
                catch (Exception)
                {
                    yield return 2;
                }
            }

            localFunc();
        }
        finally
        {
            IEnumerable<int> localFunc()
            {
                try {}
                finally
                {
                    yield return 3;
                }
            }

            localFunc();
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (15,21): error CS1626: Cannot yield a value in the body of a try block with a catch clause
                //                     yield return 1;
                Diagnostic(ErrorCode.ERR_BadYieldInTryOfCatch, "yield").WithLocation(15, 21),
                // (29,21): error CS1631: Cannot yield a value in the body of a catch clause
                //                     yield return 2;
                Diagnostic(ErrorCode.ERR_BadYieldInCatch, "yield").WithLocation(29, 21),
                // (42,21): error CS1625: Cannot yield in the body of a finally clause
                //                     yield return 3;
                Diagnostic(ErrorCode.ERR_BadYieldInFinally, "yield").WithLocation(42, 21));
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void RethrowingExceptionsInCatchInsideLocalFuncIsAllowed()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        try
        {
            throw new Exception();
        }
        catch (Exception)
        {
            void localFunc()
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception)
                {
                    Console.Write(""localFunc"");
                    throw;
                }
            }

            try
            {
                localFunc();
            }
            catch (Exception)
            {
                Console.Write(""_thrown"");
            }
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "localFunc_thrown");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void RethrowingExceptionsInLocalFuncInsideCatchIsNotAllowed()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        try {}
        catch (Exception)
        {
            void localFunc()
            {
                throw;
            }

            localFunc();
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib46(source);
            comp.VerifyDiagnostics(
                // (13,17): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                //                 throw;
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw").WithLocation(13, 17));
        }

        [Fact]
        public void LocalFunctionTypeParametersUseCorrectBinder()
        {
            var text = @"
class C
{
    static void M()
    {
        void local<[X]T>() {}
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);

            var newTree = SyntaxFactory.ParseSyntaxTree(text + " ");
            var m = newTree.GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            Assert.True(model.TryGetSpeculativeSemanticModelForMethodBody(m.Body.SpanStart, m, out model));

            var x = newTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Single();
            Assert.Equal("X", x.Identifier.Text);

            // If we aren't using the right binder here, the compiler crashes going through the binder factory
            var info = model.GetSymbolInfo(x);
            Assert.Null(info.Symbol);

            comp.VerifyDiagnostics(
                // (6,21): error CS0246: The type or namespace name 'XAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void local<[X]T>() {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("XAttribute").WithLocation(6, 21),
                // (6,21): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //         void local<[X]T>() {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(6, 21),
                // (6,14): warning CS8321: The local function 'local' is declared but never used
                //         void local<[X]T>() {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(6, 14));
        }

        [Fact]
        public void LocalFunctionAttribute_TypeParameter()
        {
            const string text = @"
using System;
class A : Attribute {}

class C
{
    static void M()
    {
        void local<[A]T>() {}
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var a = tree.GetRoot().DescendantNodes()
                .OfType<IdentifierNameSyntax>().ElementAt(2);
            Assert.Equal("A", a.Identifier.Text);
            var attrInfo = model.GetSymbolInfo(a);
            var attrType = comp.GlobalNamespace.GetTypeMember("A");
            var attrCtor = attrType.GetMember(".ctor");
            Assert.Equal(attrCtor, attrInfo.Symbol);

            // Assert that this is also true for the speculative semantic model
            var newTree = SyntaxFactory.ParseSyntaxTree(text + " ");
            var m = newTree.GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            Assert.True(model.TryGetSpeculativeSemanticModelForMethodBody(m.Body.SpanStart, m, out model));

            a = newTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().ElementAt(2);
            Assert.Equal("A", a.Identifier.Text);

            // If we aren't using the right binder here, the compiler crashes going through the binder factory
            var info = model.GetSymbolInfo(a);
            // This behavior is wrong. See https://github.com/dotnet/roslyn/issues/24135
            Assert.Equal(attrType, info.Symbol);
        }

        [Fact]
        public void LocalFunctionAttribute_OnFunction()
        {
            const string text = @"
using System;
class A : Attribute { }

class C
{
    void M()
    {
        [A]
        void local() { }
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,14): warning CS8321: The local function 'local' is declared but never used
                //         void local() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(10, 14));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var localFunction = tree.GetRoot().DescendantNodes()
                .OfType<LocalFunctionStatementSyntax>()
                .Single();

            var attributeList = localFunction.AttributeLists.Single();
            Assert.Null(attributeList.Target);

            var attribute = attributeList.Attributes.Single();
            Assert.Equal("A", ((SimpleNameSyntax)attribute.Name).Identifier.ValueText);

            var symbol = (IMethodSymbol)model.GetDeclaredSymbol(localFunction);
            Assert.NotNull(symbol);

            var attributes = symbol.GetAttributes();
            Assert.Single(attributes);

            var attributeData = attributes[0];
            var aAttribute = comp.GetTypeByMetadataName("A");
            Assert.Equal(aAttribute, attributeData.AttributeClass);
            Assert.Equal(aAttribute.InstanceConstructors.Single(), attributeData.AttributeConstructor);

            var returnAttributes = symbol.GetReturnTypeAttributes();
            Assert.Empty(returnAttributes);
        }

        [Fact]
        public void LocalFunctionAttribute_OnFunction_Argument()
        {
            const string text = @"
using System;
class A : Attribute
{
    internal A(int i) { }
}

class C
{
    void M()
    {
        [A(42)]
        void local() { }
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (13,14): warning CS8321: The local function 'local' is declared but never used
                //         void local() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(13, 14));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var localFunction = tree.GetRoot().DescendantNodes()
                .OfType<LocalFunctionStatementSyntax>()
                .Single();

            var attributeList = localFunction.AttributeLists.Single();
            Assert.Null(attributeList.Target);

            var attribute = attributeList.Attributes.Single();
            Assert.Equal("A", ((SimpleNameSyntax)attribute.Name).Identifier.ValueText);

            var symbol = (IMethodSymbol)model.GetDeclaredSymbol(localFunction);
            Assert.NotNull(symbol);

            var attributes = symbol.GetAttributes();
            Assert.Single(attributes);

            var attributeData = attributes[0];
            var aAttribute = comp.GetTypeByMetadataName("A");
            Assert.Equal(aAttribute, attributeData.AttributeClass);
            Assert.Equal(aAttribute.InstanceConstructors.Single(), attributeData.AttributeConstructor);
            Assert.Equal(42, attributeData.ConstructorArguments.Single().Value);

            var returnAttributes = symbol.GetReturnTypeAttributes();
            Assert.Empty(returnAttributes);
        }

        [Fact]
        public void LocalFunctionAttribute_Return()
        {
            const string text = @"
using System;
class A : Attribute { }

class C
{
    void M()
    {
        [return: A]
        int local() => 42;
    }
}
";
            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,13): warning CS8321: The local function 'local' is declared but never used
                //         int local() => 42;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(10, 13));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var localFunction = tree.GetRoot().DescendantNodes()
                .OfType<LocalFunctionStatementSyntax>()
                .Single();

            var attributeList = localFunction.AttributeLists.Single();
            Assert.Equal(SyntaxKind.ReturnKeyword, attributeList.Target.Identifier.Kind());

            var attribute = attributeList.Attributes.Single();
            Assert.Equal("A", ((SimpleNameSyntax)attribute.Name).Identifier.ValueText);

            var symbol = (IMethodSymbol)model.GetDeclaredSymbol(localFunction);
            Assert.NotNull(symbol);

            var returnAttributes = symbol.GetReturnTypeAttributes();
            Assert.Single(returnAttributes);

            var attributeData = returnAttributes[0];
            var aAttribute = comp.GetTypeByMetadataName("A");
            Assert.Equal(aAttribute, attributeData.AttributeClass);
            Assert.Equal(aAttribute.InstanceConstructors.Single(), attributeData.AttributeConstructor);

            var attributes = symbol.GetAttributes();
            Assert.Empty(attributes);
        }

        [Fact]
        public void LocalFunctionAttribute_Parameter()
        {
            var source = @"
using System;
class A : Attribute { }

class C
{
    void M()
    {
        int local([A] int i) => i;
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                    // (9,13): warning CS8321: The local function 'local' is declared but never used
                    //         int local([A] int i) => i;
                    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(9, 13));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var localFunction = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var parameter = localFunction.ParameterList.Parameters.Single();
            var paramSymbol = model.GetDeclaredSymbol(parameter);

            var attrs = paramSymbol.GetAttributes();
            Assert.Single(attrs);
            Assert.Equal(comp.GetTypeByMetadataName("A"), attrs[0].AttributeClass);
        }

        [Fact]
        public void LocalFunctionAttribute_LangVersionError()
        {
            const string text = @"
using System;
class A : Attribute { }

class C
{
    void M()
    {
        [A]
        void local1() { }

        [return: A]
        void local2() { }

        void local3([A] int i) { }

        void local4<[A] T>() { }
    }
}
";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): error CS8652: The feature 'local function attributes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         [A]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "[A]").WithArguments("local function attributes").WithLocation(9, 9),
                // (10,14): warning CS8321: The local function 'local1' is declared but never used
                //         void local1() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local1").WithArguments("local1").WithLocation(10, 14),
                // (12,9): error CS8652: The feature 'local function attributes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         [return: A]
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "[return: A]").WithArguments("local function attributes").WithLocation(12, 9),
                // (13,14): warning CS8321: The local function 'local2' is declared but never used
                //         void local2() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local2").WithArguments("local2").WithLocation(13, 14),
                // (15,14): warning CS8321: The local function 'local3' is declared but never used
                //         void local3([A] int i) { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local3").WithArguments("local3").WithLocation(15, 14),
                // (15,21): error CS8652: The feature 'local function attributes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         void local3([A] int i) { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "[A]").WithArguments("local function attributes").WithLocation(15, 21),
                // (17,14): warning CS8321: The local function 'local4' is declared but never used
                //         void local4<[A] T>() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local4").WithArguments("local4").WithLocation(17, 14),
                // (17,21): error CS8652: The feature 'local function attributes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         void local4<[A] T>() { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "[A]").WithArguments("local function attributes").WithLocation(17, 21));
        }

        [Fact]
        public void LocalFunctionAttribute_BadAttributeLocation()
        {
            const string text = @"
using System;

[AttributeUsage(AttributeTargets.Property)]
class PropAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
class MethodAttribute : Attribute { }

[AttributeUsage(AttributeTargets.ReturnValue)]
class ReturnAttribute : Attribute { }

public class C {
    public void M() {
        [Prop] // 1
        [Return] // 2
        [Method]
        [return: Prop] // 3
        [return: Return]
        [return: Method] // 4
        void local() { } // 5
    }
}
";

            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (15,10): error CS0592: Attribute 'Prop' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                //         [Prop] // 1
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Prop").WithArguments("Prop", "property, indexer").WithLocation(15, 10),
                // (16,10): error CS0592: Attribute 'Return' is not valid on this declaration type. It is only valid on 'return' declarations.
                //         [Return] // 2
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Return").WithArguments("Return", "return").WithLocation(16, 10),
                // (18,18): error CS0592: Attribute 'Prop' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                //         [return: Prop] // 3
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Prop").WithArguments("Prop", "property, indexer").WithLocation(18, 18),
                // (20,18): error CS0592: Attribute 'Method' is not valid on this declaration type. It is only valid on 'method' declarations.
                //         [return: Method] // 4
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Method").WithArguments("Method", "method").WithLocation(20, 18),
                // (21,14): warning CS8321: The local function 'local' is declared but never used
                //         void local() { } // 5
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local").WithArguments("local").WithLocation(21, 14));

            var tree = comp.SyntaxTrees.Single();
            var localFunction = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            var model = comp.GetSemanticModel(tree);

            var symbol = (IMethodSymbol)model.GetDeclaredSymbol(localFunction);
            Assert.NotNull(symbol);

            var attributes = symbol.GetAttributes();
            Assert.Equal(3, attributes.Length);
            Assert.Equal(comp.GetTypeByMetadataName("PropAttribute"), attributes[0].AttributeClass);
            Assert.Equal(comp.GetTypeByMetadataName("ReturnAttribute"), attributes[1].AttributeClass);
            Assert.Equal(comp.GetTypeByMetadataName("MethodAttribute"), attributes[2].AttributeClass);

            var returnAttributes = symbol.GetReturnTypeAttributes();
            Assert.Equal(3, returnAttributes.Length);
            Assert.Equal(comp.GetTypeByMetadataName("PropAttribute"), returnAttributes[0].AttributeClass);
            Assert.Equal(comp.GetTypeByMetadataName("ReturnAttribute"), returnAttributes[1].AttributeClass);
            Assert.Equal(comp.GetTypeByMetadataName("MethodAttribute"), returnAttributes[2].AttributeClass);
        }

        [Fact]
        public void LocalFunctionAttribute_AttributeSemanticModel()
        {
            const string text = @"
using System;
class A : Attribute { }

class C
{
    void M()
    {
        [A]
        void local1() { }

        [return: A]
        void local2() { }

        void local3([A] int i) { }

        void local4<[A] T>() { }
    }
}
";

            var comp = CreateCompilation(text, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,14): warning CS8321: The local function 'local1' is declared but never used
                //         void local1() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local1").WithArguments("local1").WithLocation(10, 14),
                // (13,14): warning CS8321: The local function 'local2' is declared but never used
                //         void local2() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local2").WithArguments("local2").WithLocation(13, 14),
                // (15,14): warning CS8321: The local function 'local3' is declared but never used
                //         void local3([A] int i) { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local3").WithArguments("local3").WithLocation(15, 14),
                // (17,14): warning CS8321: The local function 'local4' is declared but never used
                //         void local4<[A] T>() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "local4").WithArguments("local4").WithLocation(17, 14));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var attributeSyntaxes = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().ToList();
            Assert.Equal(4, attributeSyntaxes.Count);

            var attributeConstructor = comp.GetTypeByMetadataName("A").InstanceConstructors.Single();
            foreach (var attributeSyntax in attributeSyntaxes)
            {
                var symbol = model.GetSymbolInfo(attributeSyntaxes[0]).Symbol;
                Assert.Equal(attributeConstructor, symbol);
            }
        }

        [Fact]
        public void UnsafeLocal()
        {
            var source = @"
class C
{
    void M()
    {
        var bytesA = local();

        unsafe byte[] local()
        {
            var bytes = new byte[sizeof(int)];
            fixed (byte* ptr = &bytes[0])
            {
                *(int*)ptr = sizeof(int);
            }
            return bytes;
        }
    }
}";

            var comp = CreateCompilation(source);
            Assert.Empty(comp.GetDeclarationDiagnostics());
            comp.VerifyDiagnostics(
                // (8,23): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //         unsafe byte[] local()
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "local").WithLocation(8, 23)
                );

            var compWithUnsafe = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            compWithUnsafe.VerifyDiagnostics();
        }

        [Fact]
        public void LocalInUnsafeStruct()
        {
            var source = @"
unsafe struct C
{
    void A()
    {
        var bytesA = local();
        var bytesB = B();

        byte[] local()
        {
            var bytes = new byte[sizeof(int)];
            fixed (byte* ptr = &bytes[0])
            {
                *(int*)ptr = sizeof(int);
            }
            return bytes;
        }
    }

    byte[] B()
    {
        var bytes = new byte[sizeof(long)];
        fixed (byte* ptr = &bytes[0])
        {
            *(long*)ptr = sizeof(long);
        }
        return bytes;
    }
}";
            // no need to declare local function `local` or method `B` as unsafe
            var compWithUnsafe = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            compWithUnsafe.VerifyDiagnostics();
        }

        [Fact]
        public void LocalInUnsafeBlock()
        {
            var source = @"
struct C
{
    void A()
    {
        unsafe
        {
            var bytesA = local();

            byte[] local()
            {
                var bytes = new byte[sizeof(int)];
                fixed (byte* ptr = &bytes[0])
                {
                    *(int*)ptr = sizeof(int);
                }
                return bytes;
            }
        }
    }
}";
            // no need to declare local function `local` as unsafe
            var compWithUnsafe = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            compWithUnsafe.VerifyDiagnostics();
        }

        [Fact]
        public void ConstraintBinding()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        void Local<T, U>()
            where T : U
            where U : class
        { }

        Local<object, object>();
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ConstraintBinding2()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        void Local<T, U>(T t)
            where T : U
            where U : t
        { }

        Local<object, object>(null);
    }
}");
            comp.VerifyDiagnostics(
                // (8,23): error CS0246: The type or namespace name 't' could not be found (are you missing a using directive or an assembly reference?)
                //             where U : t
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "t").WithArguments("t").WithLocation(8, 23),
                // (11,9): error CS0311: The type 'object' cannot be used as type parameter 'U' in the generic type or method 'Local<T, U>(T)'. There is no implicit reference conversion from 'object' to 't'.
                //         Local<object, object>(null);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "Local<object, object>").WithArguments("Local<T, U>(T)", "t", "U", "object").WithLocation(11, 9));
        }

        [Fact]
        [WorkItem(17014, "https://github.com/dotnet/roslyn/pull/17014")]
        public void RecursiveLocalFuncsAsParameterTypes()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int L(L2 l2) => 0;
        int L2(L l1) => 0;
    }
}");
            comp.VerifyDiagnostics(
                // (6,15): error CS0246: The type or namespace name 'L2' could not be found (are you missing a using directive or an assembly reference?)
                //         int L(L2 l2) => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "L2").WithArguments("L2").WithLocation(6, 15),
                // (7,16): error CS0246: The type or namespace name 'L' could not be found (are you missing a using directive or an assembly reference?)
                //         int L2(L l1) => 0;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "L").WithArguments("L").WithLocation(7, 16),
                // (6,13): warning CS8321: The local function 'L' is declared but never used
                //         int L(L2 l2) => 0;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "L").WithArguments("L").WithLocation(6, 13),
                // (7,13): warning CS8321: The local function 'L2' is declared but never used
                //         int L2(L l1) => 0;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "L2").WithArguments("L2").WithLocation(7, 13));
        }

        [Fact]
        [WorkItem(16451, "https://github.com/dotnet/roslyn/issues/16451")]
        public void BadGenericConstraint()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M<T>(T value) where T : class, object { }
}");
            comp.VerifyDiagnostics(
                // (4,48): error CS0702: Constraint cannot be special class 'object'
                //     public void M<T>(T value) where T : class, object { }
                Diagnostic(ErrorCode.ERR_SpecialTypeAsBound, "object").WithArguments("object").WithLocation(4, 48)
                );
        }

        [Fact]
        [WorkItem(16451, "https://github.com/dotnet/roslyn/issues/16451")]
        public void RecursiveDefaultParameter()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        int Local(int j = Local()) => 0;
        Local();
    }
}");
            comp.VerifyDiagnostics(
                // (6,27): error CS1736: Default parameter value for 'j' must be a compile-time constant
                //         int Local(int j = Local()) => 0;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "Local()").WithArguments("j").WithLocation(6, 27));
            comp.DeclarationDiagnostics.Verify();
        }

        [Fact]
        [WorkItem(16451, "https://github.com/dotnet/roslyn/issues/16451")]
        public void RecursiveDefaultParameter2()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    void M()
    {
        int Local(Action a = Local) => 0;
        Local();
    }
}");
            comp.VerifyDiagnostics(
                // (7,30): error CS1736: Default parameter value for 'a' must be a compile-time constant
                //         int Local(Action a = Local) => 0;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "Local").WithArguments("a").WithLocation(7, 30));
            comp.DeclarationDiagnostics.Verify();
        }

        [Fact]
        [WorkItem(16451, "https://github.com/dotnet/roslyn/issues/16451")]
        public void MutuallyRecursiveDefaultParameters()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int Local1(int p = Local2()) => 0;
        int Local2(int p = Local1()) => 0;
        Local1();
        Local2();
    }
}");
            comp.VerifyDiagnostics(
                // (6,28): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //         int Local1(int p = Local2()) => 0;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "Local2()").WithArguments("p").WithLocation(6, 28),
                // (7,28): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //         int Local2(int p = Local1()) => 0;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "Local1()").WithArguments("p").WithLocation(7, 28));
            comp.DeclarationDiagnostics.Verify();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/16652")]
        public void FetchLocalFunctionSymbolThroughLocal()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
class C
{
    public void M()
    {
        void Local<[A, B, CLSCompliant, D]T>() 
        {
            var x = new object(); 
        }
        Local<int>();
    }
}");
            var comp = CreateCompilation(tree);
            comp.DeclarationDiagnostics.Verify();
            comp.VerifyDiagnostics(
                // (7,20): error CS8204: Attributes are not allowed on local function parameters or type parameters
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_AttributesInLocalFuncDecl, "[A, B, CLSCompliant, D]").WithLocation(7, 20),
                // (7,21): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(7, 21),
                // (7,21): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(7, 21),
                // (7,24): error CS0246: The type or namespace name 'BAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("BAttribute").WithLocation(7, 24),
                // (7,24): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(7, 24),
                // (7,41): error CS0246: The type or namespace name 'DAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("DAttribute").WithLocation(7, 41),
                // (7,41): error CS0246: The type or namespace name 'D' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("D").WithLocation(7, 41),
                // (7,27): error CS7036: There is no argument given that corresponds to the required formal parameter 'isCompliant' of 'CLSCompliantAttribute.CLSCompliantAttribute(bool)'
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "CLSCompliant").WithArguments("isCompliant", "System.CLSCompliantAttribute.CLSCompliantAttribute(bool)").WithLocation(7, 27));


            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v => v.Identifier.ValueText == "x").Single();
            var localSymbol = (LocalFunctionSymbol)model.GetDeclaredSymbol(x).ContainingSymbol;
            var typeParam = localSymbol.TypeParameters.Single();
            var attrs = typeParam.GetAttributes();

            Assert.True(attrs[0].AttributeClass.IsErrorType());
            Assert.True(attrs[1].AttributeClass.IsErrorType());
            Assert.False(attrs[2].AttributeClass.IsErrorType());
            Assert.Equal(comp.GlobalNamespace
                             .GetMember<NamespaceSymbol>("System")
                             .GetMember<NamedTypeSymbol>("CLSCompliantAttribute"),
                attrs[2].AttributeClass);
            Assert.True(attrs[3].AttributeClass.IsErrorType());
            comp.DeclarationDiagnostics.Verify();
        }

        [Fact]
        public void TypeParameterAttributesInSemanticModel()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    public void M()
    {
        void Local<[A]T, [CLSCompliant]U>() { }
    }
}", parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,21): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A]T, [CLSCompliant]U>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(7, 21),
                // (7,21): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A]T, [CLSCompliant]U>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(7, 21),
                // (7,27): error CS7036: There is no argument given that corresponds to the required formal parameter 'isCompliant' of 'CLSCompliantAttribute.CLSCompliantAttribute(bool)'
                //         void Local<[A]T, [CLSCompliant]U>() { }
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "CLSCompliant").WithArguments("isCompliant", "System.CLSCompliantAttribute.CLSCompliantAttribute(bool)").WithLocation(7, 27),
                // (7,14): warning CS8321: The local function 'Local' is declared but never used
                //         void Local<[A]T, [CLSCompliant]U>() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local").WithArguments("Local").WithLocation(7, 14));

            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();

            var model = comp.GetSemanticModel(tree);

            var a = root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(id => id.Identifier.ValueText == "A")
                .Single();

            Assert.Null(model.GetDeclaredSymbol(a));

            var aSymbolInfo = model.GetSymbolInfo(a);
            Assert.Equal(0, aSymbolInfo.CandidateSymbols.Length);
            Assert.Null(aSymbolInfo.Symbol);

            var aTypeInfo = model.GetTypeInfo(a);
            Assert.Equal(TypeKind.Error, aTypeInfo.Type.TypeKind);

            Assert.Null(model.GetAliasInfo(a));

            Assert.Empty(model.LookupNamespacesAndTypes(a.SpanStart, name: "A"));

            var clsCompliant = root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(id => id.Identifier.ValueText == "CLSCompliant")
                .Single();
            var clsCompliantSymbol = comp.GlobalNamespace
                .GetMember<NamespaceSymbol>("System")
                .GetTypeMember("CLSCompliantAttribute");

            Assert.Null(model.GetDeclaredSymbol(clsCompliant));

            // This should be null because there is no CLSCompliant ctor with no args
            var clsCompliantSymbolInfo = model.GetSymbolInfo(clsCompliant);
            Assert.Null(clsCompliantSymbolInfo.Symbol);
            Assert.Equal(clsCompliantSymbol.GetMember<MethodSymbol>(".ctor"),
                clsCompliantSymbolInfo.CandidateSymbols.Single());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, clsCompliantSymbolInfo.CandidateReason);

            Assert.Equal(clsCompliantSymbol, model.GetTypeInfo(clsCompliant).Type);

            Assert.Null(model.GetAliasInfo(clsCompliant));

            Assert.Equal(clsCompliantSymbol,
                model.LookupNamespacesAndTypes(clsCompliant.SpanStart, name: "CLSCompliantAttribute").Single());
            comp.DeclarationDiagnostics.Verify();
        }

        [Fact]
        public void ParameterAttributesInSemanticModel()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    public void M()
    {
        void Local([A]int x, [CLSCompliant]int y) { }
    }
}", parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,21): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local([A]int x, [CLSCompliant]int y) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(7, 21),
                // (7,21): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local([A]int x, [CLSCompliant]int y) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(7, 21),
                // (7,31): error CS7036: There is no argument given that corresponds to the required formal parameter 'isCompliant' of 'CLSCompliantAttribute.CLSCompliantAttribute(bool)'
                //         void Local([A]int x, [CLSCompliant]int y) { }
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "CLSCompliant").WithArguments("isCompliant", "System.CLSCompliantAttribute.CLSCompliantAttribute(bool)").WithLocation(7, 31),
                // (7,14): warning CS8321: The local function 'Local' is declared but never used
                //         void Local([A]int x, [CLSCompliant]int y) { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local").WithArguments("Local").WithLocation(7, 14));

            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();

            var model = comp.GetSemanticModel(tree);

            var a = root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(id => id.Identifier.ValueText == "A")
                .Single();

            Assert.Null(model.GetDeclaredSymbol(a));

            var aSymbolInfo = model.GetSymbolInfo(a);
            Assert.Equal(0, aSymbolInfo.CandidateSymbols.Length);
            Assert.Null(aSymbolInfo.Symbol);

            var aTypeInfo = model.GetTypeInfo(a);
            Assert.Equal(TypeKind.Error, aTypeInfo.Type.TypeKind);

            Assert.Null(model.GetAliasInfo(a));

            Assert.Empty(model.LookupNamespacesAndTypes(a.SpanStart, name: "A"));

            var clsCompliant = root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(id => id.Identifier.ValueText == "CLSCompliant")
                .Single();
            var clsCompliantSymbol = comp.GlobalNamespace
                .GetMember<NamespaceSymbol>("System")
                .GetTypeMember("CLSCompliantAttribute");

            Assert.Null(model.GetDeclaredSymbol(clsCompliant));

            // This should be null because there is no CLSCompliant ctor with no args
            var clsCompliantSymbolInfo = model.GetSymbolInfo(clsCompliant);
            Assert.Null(clsCompliantSymbolInfo.Symbol);
            Assert.Equal(clsCompliantSymbol.GetMember<MethodSymbol>(".ctor"),
                clsCompliantSymbolInfo.CandidateSymbols.Single());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, clsCompliantSymbolInfo.CandidateReason);

            Assert.Equal(clsCompliantSymbol, model.GetTypeInfo(clsCompliant).Type);

            Assert.Null(model.GetAliasInfo(clsCompliant));

            Assert.Equal(clsCompliantSymbol,
                model.LookupNamespacesAndTypes(clsCompliant.SpanStart, name: "CLSCompliantAttribute").Single());
            comp.DeclarationDiagnostics.Verify();
        }

        [Fact]
        public void LocalFunctionAttribute_TypeParameter_Errors()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
class C
{
    public void M()
    {
        void Local<[A, B, CLSCompliant, D]T>() { }
        Local<int>();
    }
}", options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            comp.DeclarationDiagnostics.Verify();
            comp.VerifyDiagnostics(
                // (7,21): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(7, 21),
                // (7,21): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(7, 21),
                // (7,24): error CS0246: The type or namespace name 'BAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("BAttribute").WithLocation(7, 24),
                // (7,24): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(7, 24),
                // (7,41): error CS0246: The type or namespace name 'DAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("DAttribute").WithLocation(7, 41),
                // (7,41): error CS0246: The type or namespace name 'D' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("D").WithLocation(7, 41),
                // (7,27): error CS7036: There is no argument given that corresponds to the required formal parameter 'isCompliant' of 'CLSCompliantAttribute.CLSCompliantAttribute(bool)'
                //         void Local<[A, B, CLSCompliant, D]T>() { }
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "CLSCompliant").WithArguments("isCompliant", "System.CLSCompliantAttribute.CLSCompliantAttribute(bool)").WithLocation(7, 27));


            var localDecl = tree.FindNodeOrTokenByKind(SyntaxKind.LocalFunctionStatement);
            var model = comp.GetSemanticModel(tree);
            var localSymbol = Assert.IsType<LocalFunctionSymbol>(model.GetDeclaredSymbol(localDecl.AsNode()));
            var typeParam = localSymbol.TypeParameters.Single();
            var attrs = typeParam.GetAttributes();

            Assert.True(attrs[0].AttributeClass.IsErrorType());
            Assert.True(attrs[1].AttributeClass.IsErrorType());
            Assert.False(attrs[2].AttributeClass.IsErrorType());
            Assert.Equal(comp.GlobalNamespace
                             .GetMember<NamespaceSymbol>("System")
                             .GetMember<NamedTypeSymbol>("CLSCompliantAttribute"),
                attrs[2].AttributeClass);
            Assert.True(attrs[3].AttributeClass.IsErrorType());

            comp.DeclarationDiagnostics.Verify();
        }

        [Fact]
        public void LocalFunctionAttribute_Parameter_Errors()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
using System;
class C
{
    public void M()
    {
        void Local([A, B]int x, [CLSCompliant]string s = """") { }
        Local(0);
    }
}", options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            comp.DeclarationDiagnostics.Verify();
            comp.VerifyDiagnostics(
                // (7,21): error CS0246: The type or namespace name 'AAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local([A, B]int x, [CLSCompliant]string s = "") { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("AAttribute").WithLocation(7, 21),
                // (7,21): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local([A, B]int x, [CLSCompliant]string s = "") { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(7, 21),
                // (7,24): error CS0246: The type or namespace name 'BAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local([A, B]int x, [CLSCompliant]string s = "") { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("BAttribute").WithLocation(7, 24),
                // (7,24): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                //         void Local([A, B]int x, [CLSCompliant]string s = "") { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(7, 24),
                // (7,34): error CS7036: There is no argument given that corresponds to the required formal parameter 'isCompliant' of 'CLSCompliantAttribute.CLSCompliantAttribute(bool)'
                //         void Local([A, B]int x, [CLSCompliant]string s = "") { }
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "CLSCompliant").WithArguments("isCompliant", "System.CLSCompliantAttribute.CLSCompliantAttribute(bool)").WithLocation(7, 34));

            var localDecl = tree.FindNodeOrTokenByKind(SyntaxKind.LocalFunctionStatement);
            var model = comp.GetSemanticModel(tree);
            var localSymbol = Assert.IsType<LocalFunctionSymbol>(model.GetDeclaredSymbol(localDecl.AsNode()));
            var param = localSymbol.Parameters[0];
            var attrs = param.GetAttributes();

            Assert.True(attrs[0].AttributeClass.IsErrorType());
            Assert.True(attrs[1].AttributeClass.IsErrorType());

            param = localSymbol.Parameters[1];
            attrs = param.GetAttributes();
            Assert.Equal(comp.GlobalNamespace
                             .GetMember<NamespaceSymbol>("System")
                             .GetMember<NamedTypeSymbol>("CLSCompliantAttribute"),
                attrs[0].AttributeClass);
            comp.DeclarationDiagnostics.Verify();
        }

        [Fact]
        public void TypeParameterBindingScope()
        {
            var src = @"
class C
{
    public void M()
    {
        {
            int T = 0; // Should not have error

            int Local<T>() => 0; // Should conflict with above
            Local<int>();
            T++;
        }
        {
            int T<T>() => 0;
            T<int>();
        }
        {
            int Local<T, T>() => 0;
            Local<int, int>();
        }
    }
    public void M2<T>()
    {
        {
            int Local<T>() => 0;
            Local<int>();
        }
        {
            int Local1<V>()
            {
                int Local2<V>() => 0;
                return Local2<int>();
            }
            Local1<int>();
        }
        {
            int T() => 0;
            T();
        }
        {
            int Local1<V>()
            {
                int V() => 0;
                return V();
            }
            Local1<int>();
        }
        {
            int Local1<V>()
            {
                int Local2<U>()
                {
                    // Conflicts with method type parameter
                    int T() => 0;
                    return T();
                }
                return Local2<int>();
            }
            Local1<int>();
        }
        {
            int Local1<V>()
            {
                int Local2<U>()
                {
                    // Shadows M.2<T>
                    int Local3<T>() => 0;
                    return Local3<int>();
                }
                return Local2<int>();
            }
            Local1<int>();
        }
    }
    public void V<V>() { }
}
";
            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,23): error CS0136: A local or parameter named 'T' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int Local<T>() => 0; // Should conflict with above
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "T").WithArguments("T").WithLocation(9, 23),
                // (14,19): error CS0136: A local or parameter named 'T' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int T<T>() => 0;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "T").WithArguments("T").WithLocation(14, 19),
                // (18,26): error CS0692: Duplicate type parameter 'T'
                //             int Local<T, T>() => 0;
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "T").WithArguments("T").WithLocation(18, 26),
                // (25,23): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'C.M2<T>()'
                //             int Local<T>() => 0;
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "C.M2<T>()").WithLocation(25, 23),
                // (31,28): warning CS8387: Type parameter 'V' has the same name as the type parameter from outer method 'Local1<V>()'
                //                 int Local2<V>() => 0;
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "V").WithArguments("V", "Local1<V>()").WithLocation(31, 28),
                // (37,17): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //             int T() => 0;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(37, 17),
                // (43,21): error CS0412: 'V': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //                 int V() => 0;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "V").WithArguments("V").WithLocation(43, 21),
                // (54,25): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //                     int T() => 0;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(54, 25),
                // (67,32): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'C.M2<T>()'
                //                     int Local3<T>() => 0;
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "C.M2<T>()").WithLocation(67, 32));

            comp = CreateCompilation(src, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (18,26): error CS0692: Duplicate type parameter 'T'
                //             int Local<T, T>() => 0;
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "T").WithArguments("T").WithLocation(18, 26),
                // (25,23): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'C.M2<T>()'
                //             int Local<T>() => 0;
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "C.M2<T>()").WithLocation(25, 23),
                // (31,28): warning CS8387: Type parameter 'V' has the same name as the type parameter from outer method 'Local1<V>()'
                //                 int Local2<V>() => 0;
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "V").WithArguments("V", "Local1<V>()").WithLocation(31, 28),
                // (37,17): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //             int T() => 0;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(37, 17),
                // (43,21): error CS0412: 'V': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //                 int V() => 0;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "V").WithArguments("V").WithLocation(43, 21),
                // (67,32): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'C.M2<T>()'
                //                     int Local3<T>() => 0;
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "C.M2<T>()").WithLocation(67, 32));
        }

        [Fact]
        public void LocalFuncAndTypeParameterOnType()
        {
            var comp = CreateCompilation(@"
class C2<T>
{
    public void M()
    {
        {
            int Local1()
            {
                int Local2<T>() => 0;
                return Local2<int>();
            }
            Local1();
        }
        {
            int Local1()
            {
                int Local2()
                {
                    // Shadows type parameter
                    int T() => 0;

                    // Type parameter resolves in type only context
                    T t = default(T); 

                    // Ambiguous context chooses local
                    T.M();

                    // Call chooses local
                    return T();
                }
                return Local2();
            }
            Local1();
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,28): warning CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'C2<T>'
                //                 int Local2<T>() => 0;
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "T").WithArguments("T", "C2<T>").WithLocation(9, 28),
                // (26,21): error CS0119: 'T()' is a method, which is not valid in the given context
                //                     T.M();
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T()", "method").WithLocation(26, 21),
                // (23,23): warning CS0219: The variable 't' is assigned but its value is never used
                //                     T t = default(T); 
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "t").WithArguments("t").WithLocation(23, 23));
        }

        [Fact]
        public void RefArgsInIteratorLocalFuncs()
        {
            var src = @"
using System;
using System.Collections.Generic;
class C
{
    public void M1()
    {
        IEnumerable<int> Local(ref int a) { yield break; }
        int x = 0;
        Local(ref x);
    }

    public void M2()
    {
        Action a = () =>
        {
            IEnumerable<int> Local(ref int x) { yield break; }
            int y = 0;
            Local(ref y);
            return;
        };
        a();
    }

    public Func<int> M3() => (() =>
    {
        IEnumerable<int> Local(ref int a) { yield break; }
        int x = 0;
        Local(ref x);
        return 0;
    });

    public IEnumerable<int> M4(ref int a)
    {
        yield return new Func<int>(() =>
            {
                IEnumerable<int> Local(ref int b) { yield break; }
                int x = 0;
                Local(ref x);
                return 0;
            })();
    }
}";
            VerifyDiagnostics(src,
                // (8,40): error CS1623: Iterators cannot have ref, in or out parameters
                //         IEnumerable<int> Local(ref int a) { yield break; }
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "a").WithLocation(8, 40),
                // (17,44): error CS1623: Iterators cannot have ref, in or out parameters
                //             IEnumerable<int> Local(ref int x) { yield break; }
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "x").WithLocation(17, 44),
                // (27,40): error CS1623: Iterators cannot have ref, in or out parameters
                //         IEnumerable<int> Local(ref int a) { yield break; }
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "a").WithLocation(27, 40),
                // (33,40): error CS1623: Iterators cannot have ref, in or out parameters
                //     public IEnumerable<int> M4(ref int a)
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "a").WithLocation(33, 40),
                // (37,48): error CS1623: Iterators cannot have ref, in or out parameters
                //                 IEnumerable<int> Local(ref int b) { yield break; }
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "b").WithLocation(37, 48));
        }

        [Fact]
        public void UnsafeArgsInIteratorLocalFuncs()
        {
            var src = @"
using System;
using System.Collections.Generic;
class C
{
    public unsafe void M1()
    {
        IEnumerable<int> Local(int* a) { yield break; }
        int x = 0;
        Local(&x);
    }

    public unsafe void M2()
    {
        Action a = () =>
        {
            IEnumerable<int> Local(int* x) { yield break; }
            int y = 0;
            Local(&y);
            return;
        };
        a();
    }

    public unsafe Func<int> M3() => (() =>
    {
        IEnumerable<int> Local(int* a) { yield break; }
        int x = 0;
        Local(&x);
        return 0;
    });

    public unsafe IEnumerable<int> M4(int* a)
    {
        yield return new Func<int>(() =>
            {
                IEnumerable<int> Local(int* b) { yield break; }
                int x = 0;
                Local(&x);
                return 0;
            })();
    }
}";
            CreateCompilation(src, options: TestOptions.UnsafeDebugDll)
                .VerifyDiagnostics(
                // (8,37): error CS1637: Iterators cannot have unsafe parameters or yield types
                //         IEnumerable<int> Local(int* a) { yield break; }
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "a").WithLocation(8, 37),
                // (17,41): error CS1637: Iterators cannot have unsafe parameters or yield types
                //             IEnumerable<int> Local(int* x) { yield break; }
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "x").WithLocation(17, 41),
                // (27,37): error CS1637: Iterators cannot have unsafe parameters or yield types
                //         IEnumerable<int> Local(int* a) { yield break; }
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "a").WithLocation(27, 37),
                // (33,44): error CS1637: Iterators cannot have unsafe parameters or yield types
                //     public unsafe IEnumerable<int> M4(int* a)
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "a").WithLocation(33, 44),
                // (33,36): error CS1629: Unsafe code may not appear in iterators
                //     public unsafe IEnumerable<int> M4(int* a)
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "M4").WithLocation(33, 36),
                // (37,40): error CS1629: Unsafe code may not appear in iterators
                //                 IEnumerable<int> Local(int* b) { yield break; }
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "int*").WithLocation(37, 40),
                // (39,23): error CS1629: Unsafe code may not appear in iterators
                //                 Local(&x);
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "&x").WithLocation(39, 23),
                // (39,17): error CS1629: Unsafe code may not appear in iterators
                //                 Local(&x);
                Diagnostic(ErrorCode.ERR_IllegalInnerUnsafe, "Local(&x)").WithLocation(39, 17),
                // (37,45): error CS1637: Iterators cannot have unsafe parameters or yield types
                //                 IEnumerable<int> Local(int* b) { yield break; }
                Diagnostic(ErrorCode.ERR_UnsafeIteratorArgType, "b").WithLocation(37, 45));
        }

        [Fact]
        [WorkItem(13193, "https://github.com/dotnet/roslyn/issues/13193")]
        public void LocalFunctionConflictingName()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M<TLocal>()
    {
        void TLocal() { }
        TLocal();
    }
    public void M(int Local)
    {
        void Local() { }
        Local();
    }
    public void M()
    {
        int local = 0;

        void local() { }
        local();
    }
}");
            comp.VerifyDiagnostics(
                // (6,14): error CS0412: 'TLocal': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         void TLocal() { }
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "TLocal").WithArguments("TLocal").WithLocation(6, 14),
                // (11,14): error CS0136: A local or parameter named 'Local' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void Local() { }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "Local").WithArguments("Local").WithLocation(11, 14),
                // (18,14): error CS0128: A local variable or function named 'local' is already defined in this scope
                //         void local() { }
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "local").WithArguments("local").WithLocation(18, 14),
                // (16,13): warning CS0219: The variable 'local' is assigned but its value is never used
                //         int local = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "local").WithArguments("local").WithLocation(16, 13));
        }

        [Fact]
        public void ForgotSemicolonLocalFunctionsMistake()
        {
            var src = @"
class C
{
    public void M1()
    {
    // forget closing brace

    public void BadLocal1()
    {
        this.BadLocal2();
    }

    public void BadLocal2()
    {
    }

    public int P => 0;
}";
            VerifyDiagnostics(src,
                // (8,5): error CS0106: The modifier 'public' is not valid for this item
                //     public void BadLocal1()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "public").WithArguments("public").WithLocation(8, 5),
                // (13,5): error CS0106: The modifier 'public' is not valid for this item
                //     public void BadLocal2()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "public").WithArguments("public").WithLocation(13, 5),
                // (15,6): error CS1513: } expected
                //     }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(15, 6),
                // (10,14): error CS1061: 'C' does not contain a definition for 'BadLocal2' and no extension method 'BadLocal2' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         this.BadLocal2();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "BadLocal2").WithArguments("C", "BadLocal2").WithLocation(10, 14),
                // (8,17): warning CS8321: The local function 'BadLocal1' is declared but never used
                //     public void BadLocal1()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "BadLocal1").WithArguments("BadLocal1").WithLocation(8, 17),
                // (13,17): warning CS8321: The local function 'BadLocal2' is declared but never used
                //     public void BadLocal2()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "BadLocal2").WithArguments("BadLocal2").WithLocation(13, 17));

        }

        [Fact]
        public void VarLocalFunction()
        {
            var src = @"
class C
{
    void M()
    {
        var local() => 0;
        int x = local();
   } 
}";
            VerifyDiagnostics(src,
                // (6,9): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         var local() => 0;
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(6, 9));
        }

        [Fact]
        public void VarLocalFunction2()
        {
            var comp = CreateCompilation(@"
class C
{
    private class var
    {
    }

    void M()
    {
        var local() => new var();
        var x = local();
   } 
}", parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Params)]
        public void BadParams()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void Params(params int x)
        {
            Console.WriteLine(x);
        }
        Params(2);
    }
}
";
            VerifyDiagnostics(source,
    // (8,21): error CS0225: The params parameter must be a single dimensional array
    //         void Params(params int x)
    Diagnostic(ErrorCode.ERR_ParamsMustBeArray, "params").WithLocation(8, 21)
    );
        }

        [Fact]
        public void BadRefWithDefault()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void RefOut(ref int x = 2)
        {
            x++;
        }
        int y = 2;
        RefOut(ref y);
    }
}
";
            VerifyDiagnostics(source,
    // (6,21): error CS1741: A ref or out parameter cannot have a default value
    //         void RefOut(ref int x = 2)
    Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(6, 21)
    );
        }

        [Fact]
        public void BadDefaultValueType()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void NamedOptional(string x = 2)
        {
            Console.WriteLine(x);
        }
        NamedOptional(""2"");
    }
}
";
            VerifyDiagnostics(source,
    // (8,35): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'string'
    //         void NamedOptional(string x = 2)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("int", "string").WithLocation(8, 35)
    );
        }

        [Fact]
        public void CallerMemberName()
        {
            var comp = CreateCompilationWithMscorlib46(@"
using System;
using System.Runtime.CompilerServices;
class C
{
    static void Main()
    {
        void CallerMemberName([CallerMemberName] string s = null)
        {
            Console.Write(s);
        }
        void LocalFuncName()
        {
            CallerMemberName();
        }
        LocalFuncName();
        Console.Write(' ');
        CallerMemberName();
    }
}", parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void BadCallerMemberName()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main(string[] args)
    {
        void CallerMemberName([CallerMemberName] int s = 2)
        {
            Console.WriteLine(s);
        }
        CallerMemberName();
    }
}
";
            CreateCompilationWithMscorlib45AndCSharp(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (9,32): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
                //         void CallerMemberName([CallerMemberName] int s = 2)
                Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithArguments("string", "int").WithLocation(9, 32));
        }

        [WorkItem(10708, "https://github.com/dotnet/roslyn/issues/10708")]
        [CompilerTrait(CompilerFeature.Dynamic, CompilerFeature.Params)]
        [Fact]
        public void DynamicArgumentToParams()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        void L1(int x = 0, params int[] ys) => Console.Write(x);

        dynamic val = 2;
        L1(val, val);
        L1(ys: val, x: val);
        L1(ys: val);
    }
}";
            VerifyDiagnostics(src,
                // (10,9): error CS8106: Cannot pass argument with dynamic type to params parameter 'ys' of local function 'L1'.
                //         L1(val, val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionParamsParameter, "L1(val, val)").WithArguments("ys", "L1").WithLocation(10, 9),
                // (11,9): error CS8106: Cannot pass argument with dynamic type to params parameter 'ys' of local function 'L1'.
                //         L1(ys: val, x: val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionParamsParameter, "L1(ys: val, x: val)").WithArguments("ys", "L1").WithLocation(11, 9),
                // (12,9): error CS8106: Cannot pass argument with dynamic type to params parameter 'ys' of local function 'L1'.
                //         L1(ys: val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionParamsParameter, "L1(ys: val)").WithArguments("ys", "L1").WithLocation(12, 9));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicArgOverload()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        void Overload(int i) => Console.Write(i);
        void Overload(string s) => Console.Write(s);

        dynamic val = 2;
        Overload(val);
    }
}";
            VerifyDiagnostics(src,
                // (8,14): error CS0128: A local variable named 'Overload' is already defined in this scope
                //         void Overload(string s) => Console.Write(s);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "Overload").WithArguments("Overload").WithLocation(8, 14),
                // (8,14): warning CS8321: The local function 'Overload' is declared but never used
                //         void Overload(string s) => Console.Write(s);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Overload").WithArguments("Overload").WithLocation(8, 14));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicArgWrongArity()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        void Local(int i) => Console.Write(i);

        dynamic val = 2;
        Local(val, val);
    }
}";
            VerifyDiagnostics(src,
                // (10,9): error CS1501: No overload for method 'Local' takes 2 arguments
                //         Local(val, val);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Local").WithArguments("Local", "2").WithLocation(10, 9));
        }

        [WorkItem(3923, "https://github.com/dotnet/roslyn/issues/3923")]
        [Fact]
        public void ExpressionTreeLocalFunctionUsage()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        T Id<T>(T x)
        {
            return x;
        }
        Expression<Func<T>> Local<T>(Expression<Func<T>> f)
        {
            return f;
        }
        Console.Write(Local(() => Id(2)));
        Console.Write(Local<Func<int, int>>(() => Id));
        Console.Write(Local(() => new Func<int, int>(Id)));
        Console.Write(Local(() => nameof(Id)));
    }
}
";
            VerifyDiagnostics(source,
    // (16,35): error CS8096: An expression tree may not contain a reference to a local function
    //         Console.Write(Local(() => Id(2)));
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Id(2)").WithLocation(16, 35),
    // (17,51): error CS8096: An expression tree may not contain a reference to a local function
    //         Console.Write(Local<Func<int, int>>(() => Id));
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Id").WithLocation(17, 51),
    // (18,35): error CS8096: An expression tree may not contain a reference to a local function
    //         Console.Write(Local(() => new Func<int, int>(Id)));
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Id").WithLocation(18, 54)
    );
        }

        [Fact]
        public void ExpressionTreeLocalFunctionInside()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Expression<Func<int, int>> f = x =>
        {
            int Local(int y) => y;
            return Local(x);
        };
        Console.Write(f);
    }
}
";
            VerifyDiagnostics(source,
    // (8,40): error CS0834: A lambda expression with a statement body cannot be converted to an expression tree
    //         Expression<Func<int, int>> f = x =>
    Diagnostic(ErrorCode.ERR_StatementLambdaToExpressionTree, @"x =>
        {
            int Local(int y) => y;
            return Local(x);
        }").WithLocation(8, 40),
    // (11,20): error CS8096: An expression tree may not contain a local function or a reference to a local function
    //             return Local(x);
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Local(x)").WithLocation(11, 20)
    );
        }
        [Fact]
        public void BadScoping()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        if (true)
        {
            void Local()
            {
                Console.WriteLine(2);
            }
            Local();
        }
        Local();

        Local2();
        void Local2()
        {
            Console.WriteLine(2);
        }
    }
}
";
            VerifyDiagnostics(source,
    // (16,9): error CS0103: The name 'Local' does not exist in the current context
    //         Local();
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Local").WithArguments("Local").WithLocation(16, 9)
    );
        }

        [Fact]
        public void NameConflictDuplicate()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Duplicate() { }
        void Duplicate() { }
        Duplicate();
    }
}
";
            VerifyDiagnostics(source,
    // (7,14): error CS0128: A local variable named 'Duplicate' is already defined in this scope
    //         void Duplicate() { }
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "Duplicate").WithArguments("Duplicate").WithLocation(7, 14),
    // (7,14): warning CS8321: The local function 'Duplicate' is declared but never used
    //         void Duplicate() { }
    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Duplicate").WithArguments("Duplicate").WithLocation(7, 14)
    );
        }

        [Fact]
        public void NameConflictParameter()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int x = 2;
        void Param(int x) { }
        Param(x);
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (7,24): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void Param(int x) { }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(7, 24));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NameConflictTypeParameter()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int T;
        void Generic<T>() { }
        Generic<int>();
    }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (7,22): error CS0136: A local or parameter named 'T' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void Generic<T>() { }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "T").WithArguments("T").WithLocation(7, 22),
                // (6,13): warning CS0168: The variable 'T' is declared but never used
                //         int T;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "T").WithArguments("T").WithLocation(6, 13));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,13): warning CS0168: The variable 'T' is declared but never used
                //         int T;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "T").WithArguments("T").WithLocation(6, 13));
        }

        [Fact]
        public void NameConflictNestedTypeParameter()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        T Outer<T>()
        {
            T Inner<T>()
            {
                return default(T);
            }
            return Inner<T>();
        }
        System.Console.Write(Outer<int>());
    }
}
";
            VerifyDiagnostics(source,
    // (8,21): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'Outer<T>()'
    //             T Inner<T>()
    Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "Outer<T>()").WithLocation(8, 21)
    );
        }

        [Fact]
        public void NameConflictLocalVarFirst()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int Conflict;
        void Conflict() { }
    }
}
";
            VerifyDiagnostics(source,
    // (7,14): error CS0128: A local variable named 'Conflict' is already defined in this scope
    //         void Conflict() { }
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "Conflict").WithArguments("Conflict").WithLocation(7, 14),
    // (6,13): warning CS0168: The variable 'Conflict' is declared but never used
    //         int Conflict;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(6, 13),
    // (7,14): warning CS8321: The local function 'Conflict' is declared but never used
    //         void Conflict() { }
    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Conflict").WithArguments("Conflict").WithLocation(7, 14)
    );
        }

        [Fact]
        public void NameConflictLocalVarLast()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Conflict() { }
        int Conflict;
    }
}
";
            // TODO: This is strange. Probably has to do with the fact that local variables are preferred over functions.
            VerifyDiagnostics(source,
    // (6,14): error CS0136: A local or parameter named 'Conflict' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Conflict() { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "Conflict").WithArguments("Conflict").WithLocation(6, 14),
    // (7,13): warning CS0168: The variable 'Conflict' is declared but never used
    //         int Conflict;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(7, 13),
    // (6,14): warning CS8321: The local function 'Conflict' is declared but never used
    //         void Conflict() { }
    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Conflict").WithArguments("Conflict").WithLocation(6, 14)
    );
        }

        [Fact]
        public void BadUnsafeNoKeyword()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        void Local()
        {
            int x = 2;
            Console.WriteLine(*&x);
        }
        Local();
    }
    static void Main(string[] args)
    {
        A();
    }
}
";
            VerifyDiagnostics(source,
    // (11,32): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
    //             Console.WriteLine(*&x);
    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(11, 32)
    );
        }

        [Fact]
        public void BadUnsafeKeywordDoesntApply()
        {
            var source = @"
using System;

class Program
{
    static unsafe void B()
    {
        void Local()
        {
            int x = 2;
            Console.WriteLine(*&x);
        }
        Local();
    }
    static void Main(string[] args)
    {
        B();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(true));
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void BadEmptyBody()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Local(int x);
        Local(2);
    }
}";
            VerifyDiagnostics(source,
                // (6,14): error CS8112: 'Local(int)' is a local function and must therefore always have a body.
                //         void Local(int x);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "Local").WithArguments("Local(int)").WithLocation(6, 14)
            );
        }

        [Fact]
        public void BadGotoInto()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        goto A;
        void Local()
        {
        A:  Console.Write(2);
        }
        Local();
    }
}";
            VerifyDiagnostics(source,
    // (8,14): error CS0159: No such label 'A' within the scope of the goto statement
    //         goto A;
    Diagnostic(ErrorCode.ERR_LabelNotFound, "A").WithArguments("A").WithLocation(8, 14),
    // (11,9): warning CS0164: This label has not been referenced
    //         A:  Console.Write(2);
    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "A").WithLocation(11, 9)
    );
        }

        [Fact]
        public void BadGotoOutOf()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Local()
        {
            goto A;
        }
    A:  Local();
    }
}";
            VerifyDiagnostics(source,
    // (8,13): error CS0159: No such label 'A' within the scope of the goto statement
    //             goto A;
    Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("A").WithLocation(8, 13),
    // (10,5): warning CS0164: This label has not been referenced
    //     A:  Local();
    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "A").WithLocation(10, 5)
    );
        }

        [Fact]
        public void BadDefiniteAssignmentCall()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        goto Label;
        int x = 2;
        void Local()
        {
            Console.Write(x);
        }
        Label:
        Local();
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
                // (9,9): warning CS0162: Unreachable code detected
                //         int x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(9, 9),
                // (15,9): error CS0165: Use of unassigned local variable 'x'
                //         Local();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local()").WithArguments("x").WithLocation(15, 9)
    );
        }

        [Fact]
        public void BadDefiniteAssignmentDelegateConversion()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        goto Label;
        int x = 2;
        void Local()
        {
            Console.Write(x);
        }
        Label:
        Action goo = Local;
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
                // (9,9): warning CS0162: Unreachable code detected
                //         int x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(9, 9),
                // (15,22): error CS0165: Use of unassigned local variable 'x'
                //         Action goo = Local;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local").WithArguments("x").WithLocation(15, 22)
    );
        }

        [Fact]
        public void BadDefiniteAssignmentDelegateConstruction()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        goto Label;
        int x = 2;
        void Local()
        {
            Console.Write(x);
        }
        Label:
        var bar = new Action(Local);
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
                // (9,9): warning CS0162: Unreachable code detected
                //         int x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(9, 9),
                // (15,19): error CS0165: Use of unassigned local variable 'x'
                //         var bar = new Action(Local);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "new Action(Local)").WithArguments("x").WithLocation(15, 19)
                );
        }

        [Fact]
        public void BadNotUsed()
        {
            var source = @"
class Program
{
    static void A()
    {
        void Local()
        {
        }
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
    // (6,14): warning CS8321: The local function 'Local' is declared but never used
    //         void Local()
    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local").WithArguments("Local").WithLocation(6, 14)
    );
        }

        [Fact]
        public void BadNotUsedSwitch()
        {
            var source = @"
class Program
{
    static void A()
    {
        switch (0)
        {
        case 0:
            void Local()
            {
            }
            break;
        }
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
    // (9,18): warning CS8321: The local function 'Local' is declared but never used
    //             void Local()
    Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local").WithArguments("Local").WithLocation(9, 18)
    );
        }

        [Fact]
        public void BadByRefClosure()
        {
            var source = @"
using System;

class Program
{
    static void A(ref int x)
    {
        void Local()
        {
            Console.WriteLine(x);
        }
        Local();
    }
    static void Main()
    {
    }
}";
            VerifyDiagnostics(source,
    // (10,31): error CS1628: Cannot use ref or out parameter 'x' inside an anonymous method, lambda expression, query expression, or local function
    //             Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x").WithArguments("x").WithLocation(10, 31)
    );
        }

        [Fact]
        public void BadInClosure()
        {
            var source = @"
using System;

class Program
{
    static void A(in int x)
    {
        void Local()
        {
            Console.WriteLine(x);
        }
        Local();
    }
    static void Main()
    {
    }
}";
            VerifyDiagnostics(source,
                // (10,31): error CS1628: Cannot use ref, out, or in parameter 'x' inside an anonymous method, lambda expression, query expression, or local function
                //             Console.WriteLine(x);
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x").WithArguments("x").WithLocation(10, 31)
                );
        }

        [Fact]
        public void BadArglistUse()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        void Local()
        {
            Console.WriteLine(__arglist);
        }
        Local();
    }
    static void B(__arglist)
    {
        void Local()
        {
            Console.WriteLine(__arglist);
        }
        Local();
    }
    static void C() // C and D produce different errors
    {
        void Local(__arglist)
        {
            Console.WriteLine(__arglist);
        }
        Local(__arglist());
    }
    static void D(__arglist)
    {
        void Local(__arglist)
        {
            Console.WriteLine(__arglist);
        }
        Local(__arglist());
    }
    static void Main()
    {
    }
}
";
            VerifyDiagnostics(source,
                // (10,31): error CS0190: The __arglist construct is valid only within a variable argument method
                //             Console.WriteLine(__arglist);
                Diagnostic(ErrorCode.ERR_ArgsInvalid, "__arglist").WithLocation(10, 31),
                // (18,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
                //             Console.WriteLine(__arglist);
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(18, 31),
                // (24,20): error CS1669: __arglist is not valid in this context
                //         void Local(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(24, 20),
                // (26,31): error CS0190: The __arglist construct is valid only within a variable argument method
                //             Console.WriteLine(__arglist);
                Diagnostic(ErrorCode.ERR_ArgsInvalid, "__arglist").WithLocation(26, 31),
                // (32,20): error CS1669: __arglist is not valid in this context
                //         void Local(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(32, 20),
                // (34,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
                //             Console.WriteLine(__arglist);
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(34, 31)
    );
        }

        [Fact]
        public void BadClosureStaticRefInstance()
        {
            var source = @"
using System;

class Program
{
    int _a = 0;
    static void A()
    {
        void Local()
        {
            Console.WriteLine(_a);
        }
        Local();
    }
    static void Main()
    {
    }
}
";
            VerifyDiagnostics(source,
    // (11,31): error CS0120: An object reference is required for the non-static field, method, or property 'Program._a'
    //             Console.WriteLine(_a);
    Diagnostic(ErrorCode.ERR_ObjectRequired, "_a").WithArguments("Program._a").WithLocation(11, 31)
    );
        }

        [Fact]
        public void BadRefIterator()
        {
            var source = @"
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> RefEnumerable(ref int x)
        {
            yield return x;
        }
        int y = 0;
        RefEnumerable(ref y);
    }
}
";
            VerifyDiagnostics(source,
    // (8,48): error CS1623: Iterators cannot have ref, in or out parameters
    //         IEnumerable<int> RefEnumerable(ref int x)
    Diagnostic(ErrorCode.ERR_BadIteratorArgType, "x").WithLocation(8, 48)
    );
        }

        [Fact]
        public void BadRefAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        async Task<int> RefAsync(ref int x)
        {
            return await Task.FromResult(x);
        }
        int y = 2;
        Console.Write(RefAsync(ref y).Result);
    }
}
";
            VerifyDiagnostics(source,
    // (9,42): error CS1988: Async methods cannot have ref, in or out parameters
    //         async Task<int> RefAsync(ref int x)
    Diagnostic(ErrorCode.ERR_BadAsyncArgType, "x").WithLocation(9, 42)
    );
        }

        [Fact]
        public void Extension_01()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int Local(this int x)
        {
            return x;
        }
        Console.WriteLine(Local(2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,13): error CS1106: Extension method must be defined in a non-generic static class
                //         int Local(this int x)
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "Local").WithLocation(8, 13)
                );
        }

        [Fact]
        public void Extension_02()
        {
            var source =
@"#pragma warning disable 8321
static class E
{
    static void M()
    {
        void F1(this string s) { }
        static void F2(this string s) { }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,14): error CS1106: Extension method must be defined in a non-generic static class
                //         void F1(this string s) { }
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "F1").WithLocation(6, 14),
                // (7,21): error CS1106: Extension method must be defined in a non-generic static class
                //         static void F2(this string s) { }
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "F2").WithLocation(7, 21));
        }

        [Fact]
        public void BadModifiers()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        const void LocalConst()
        {
        }
        static void LocalStatic()
        {
        }
        readonly void LocalReadonly()
        {
        }
        volatile void LocalVolatile()
        {
        }
        LocalConst();
        LocalStatic();
        LocalReadonly();
        LocalVolatile();
    }
}
";

            var baseExpected = new[]
            {
                // (6,9): error CS0106: The modifier 'const' is not valid for this item
                //         const void LocalConst()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "const").WithArguments("const").WithLocation(6, 9),
                // (12,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly void LocalReadonly()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(12, 9),
                // (15,9): error CS0106: The modifier 'volatile' is not valid for this item
                //         volatile void LocalVolatile()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "volatile").WithArguments("volatile").WithLocation(15, 9)
            };

            var extra = new[]
            {
                // (9,9): error CS8652: The feature 'static local functions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         static void LocalStatic()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "static").WithArguments("static local functions", "8.0").WithLocation(9, 9),
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                baseExpected.Concat(extra).ToArray());

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(baseExpected);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(baseExpected);
        }

        [Fact]
        public void ArglistIterator()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> Local(__arglist)
        {
            yield return 2;
        }
        Console.WriteLine(string.Join("","", Local(__arglist())));
    }
}
";
            VerifyDiagnostics(source,
                // (9,26): error CS1636: __arglist is not allowed in the parameter list of iterators
                //         IEnumerable<int> Local(__arglist)
                Diagnostic(ErrorCode.ERR_VarargsIterator, "Local").WithLocation(9, 26),
                // (9,32): error CS1669: __arglist is not valid in this context
                //         IEnumerable<int> Local(__arglist)
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(9, 32));
        }

        [Fact]
        public void ForwardReference()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Local());
        int Local() => 2;
    }
}
";
            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        public void ForwardReferenceCapture()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int x = 2;
        Console.WriteLine(Local());
        int Local() => x;
    }
}
";
            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        public void ForwardRefInLocalFunc()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int x = 2;
        Console.WriteLine(Local());
        int Local()
        {
            x = 3;
            return Local2();
        }
        int Local2() => x;
    }
}
";
            CompileAndVerify(source, expectedOutput: "3");
        }

        [Fact]
        public void LocalFuncMutualRecursion()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int x = 5;
        int y = 0;
        Console.WriteLine(Local1());
        int Local1()
        {
            x -= 1;
            return Local2(y++);
        }
        int Local2(int z)
        {
            if (x == 0)
            {
                return z;
            }
            else
            {
                return Local1();
            }
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "4");
        }

        [Fact]
        public void OtherSwitchBlock()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = int.Parse(Console.ReadLine());
        switch (x)
        {
        case 0:
            void Local()
            {
            }
            break;
        default:
            Local();
            break;
        }
    }
}
";
            VerifyDiagnostics(source);
        }

        [Fact]
        public void NoOperator()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        Program operator +(Program left, Program right)
        {
            return left;
        }
    }
}
";
            VerifyDiagnostics(source,
                // (6,17): error CS1002: ; expected
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "operator").WithLocation(6, 17),
                // (6,17): error CS1513: } expected
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_RbraceExpected, "operator").WithLocation(6, 17),
                // (6,56): error CS1002: ; expected
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 56),
                // (6,9): error CS0119: 'Program' is a type, which is not valid in the given context
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(6, 9),
                // (6,28): error CS8185: A declaration is not allowed in this context.
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "Program left").WithLocation(6, 28),
                // (6,42): error CS8185: A declaration is not allowed in this context.
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "Program right").WithLocation(6, 42),
                // (6,27): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(Program left, Program right)").WithArguments("System.ValueTuple`2").WithLocation(6, 27),
                // (6,26): error CS0023: Operator '+' cannot be applied to operand of type '(Program, Program)'
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "+(Program left, Program right)").WithArguments("+", "(Program, Program)").WithLocation(6, 26),
                // (8,13): error CS0127: Since 'Program.Main(string[])' returns void, a return keyword must not be followed by an object expression
                //             return left;
                Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("Program.Main(string[])").WithLocation(8, 13),
                // (6,28): error CS0165: Use of unassigned local variable 'left'
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Program left").WithArguments("left").WithLocation(6, 28),
                // (6,42): error CS0165: Use of unassigned local variable 'right'
                //         Program operator +(Program left, Program right)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Program right").WithArguments("right").WithLocation(6, 42)
                );
        }

        [Fact]
        public void NoProperty()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int Goo
        {
            get
            {
                return 2;
            }
        }
        int Bar => 2;
    }
}
";
            VerifyDiagnostics(source,
                // (6,16): error CS1002: ; expected
                //         int Goo
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 16),
                // (8,16): error CS1002: ; expected
                //             get
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 16),
                // (13,17): error CS1003: Syntax error, ',' expected
                //         int Bar => 2;
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(13, 17),
                // (13,20): error CS1002: ; expected
                //         int Bar => 2;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "2").WithLocation(13, 20),
                // (8,13): error CS0103: The name 'get' does not exist in the current context
                //             get
                Diagnostic(ErrorCode.ERR_NameNotInContext, "get").WithArguments("get").WithLocation(8, 13),
                // (10,17): error CS0127: Since 'Program.Main(string[])' returns void, a return keyword must not be followed by an object expression
                //                 return 2;
                Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("Program.Main(string[])").WithLocation(10, 17),
                // (13,20): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         int Bar => 2;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "2").WithLocation(13, 20),
                // (13,9): warning CS0162: Unreachable code detected
                //         int Bar => 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(13, 9),
                // (6,13): warning CS0168: The variable 'Goo' is declared but never used
                //         int Goo
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Goo").WithArguments("Goo").WithLocation(6, 13),
                // (13,13): warning CS0168: The variable 'Bar' is declared but never used
                //         int Bar => 2;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Bar").WithArguments("Bar").WithLocation(13, 13)
                );
        }

        [Fact]
        public void NoFeatureSwitch()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Local() { }
        Local();
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilation(source, options: option, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
                // (6,14): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                //         void Local() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "Local").WithArguments("local functions", "7.0").WithLocation(6, 14)
                );
        }

        [Fact, WorkItem(10521, "https://github.com/dotnet/roslyn/issues/10521")]
        public void LocalFunctionInIf()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        if () // typing at this point
        int Add(int x, int y) => x + y;
    }
}
";
            VerifyDiagnostics(source,
                // (6,13): error CS1525: Invalid expression term ')'
                //         if () // typing at this point
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 13),
                // (7,9): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //         int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "int Add(int x, int y) => x + y;").WithLocation(7, 9),
                // (7,13): warning CS8321: The local function 'Add' is declared but never used
                //         int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Add").WithArguments("Add").WithLocation(7, 13)
                );
        }

        [Fact, WorkItem(10521, "https://github.com/dotnet/roslyn/issues/10521")]
        public void LabeledLocalFunctionInIf()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        if () // typing at this point
a:      int Add(int x, int y) => x + y;
    }
}
";
            VerifyDiagnostics(source,
                // (6,13): error CS1525: Invalid expression term ')'
                //         if () // typing at this point
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 13),
                // (7,1): error CS1023: Embedded statement cannot be a declaration or labeled statement
                // a:      int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "a:      int Add(int x, int y) => x + y;").WithLocation(7, 1),
                // (7,1): warning CS0164: This label has not been referenced
                // a:      int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(7, 1),
                // (7,13): warning CS8321: The local function 'Add' is declared but never used
                // a:      int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Add").WithArguments("Add").WithLocation(7, 13)
                );
        }

        [CompilerTrait(CompilerFeature.LocalFunctions, CompilerFeature.Var)]
        public sealed class VarTests : LocalFunctionsTestBase
        {
            [Fact]
            public void IllegalAsReturn()
            {
                var source = @"
using System;
class Program
{
    static void Main()
    {
        var f() => 42;
        Console.WriteLine(f());
    }
}";
                var comp = CreateCompilationWithMscorlib45(source, parseOptions: DefaultParseOptions);
                comp.VerifyDiagnostics(
                    // (7,9): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                    //         var f() => 42;
                    Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(7, 9));
            }

            [Fact]
            public void RealTypeAsReturn()
            {
                var source = @"
using System;
class var 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Main()
    {
        var f() => new var();
        Console.WriteLine(f());
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog");
            }

            [Fact]
            public void RealTypeParameterAsReturn()
            {
                var source = @"
using System;
class test 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Test<var>(var x)
    {
        var f() => x;
        Console.WriteLine(f());
    }

    static void Main()
    {
        Test(new test());
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog");
            }

            [Fact]
            public void IdentifierAndTypeNamedVar()
            {
                var source = @"
using System;
class var 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Main()
    {
        int var = 42;
        var f() => new var();
        Console.WriteLine($""{f()}-{var}"");
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog-42");
            }
        }

        [CompilerTrait(CompilerFeature.LocalFunctions, CompilerFeature.Async)]
        public sealed class AsyncTests : LocalFunctionsTestBase
        {
            [Fact]
            public void RealTypeAsReturn()
            {
                var source = @"
using System;
class async 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Main()
    {
        async f() => new async();
        Console.WriteLine(f());
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog");
            }

            [Fact]
            public void RealTypeParameterAsReturn()
            {
                var source = @"
using System;
class test 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Test<async>(async x)
    {
        async f() => x;
        Console.WriteLine(f());
    }

    static void Main()
    {
        Test(new test());
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog");
            }

            [Fact]
            public void ManyMeaningsType()
            {
                var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class async 
{
    public override string ToString() => ""async"";
}

class Program
{
    static void Main()
    {
        async Task<async> Test(Task<async> t)
        {
            async local = await t;
            Console.WriteLine(local);
            return local;
        }

        Test(Task.FromResult<async>(new async())).Wait();
    }
}";

                var comp = CreateCompilationWithMscorlib46(source, parseOptions: DefaultParseOptions, options: TestOptions.DebugExe);
                CompileAndVerify(
                    comp,
                    expectedOutput: "async");
            }
        }

        [Fact]
        [WorkItem(12467, "https://github.com/dotnet/roslyn/issues/12467")]
        public void ParamUnassigned_01()
        {
            var src = @"
class C
{
    public void M1()
    {
        void TakeOutParam1(out int x)
        {
        }

        int y;
        TakeOutParam1(out y);
    }

        void TakeOutParam2(out int x)
        {
        }
}";
            VerifyDiagnostics(src,
                // (6,14): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         void TakeOutParam1(out int x)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "TakeOutParam1").WithArguments("x").WithLocation(6, 14),
                // (14,14): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         void TakeOutParam2(out int x)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "TakeOutParam2").WithArguments("x").WithLocation(14, 14)
                );
        }

        [Fact]
        [WorkItem(12467, "https://github.com/dotnet/roslyn/issues/12467")]
        public void ParamUnassigned_02()
        {
            var src = @"
class C
{
    public void M1()
    {
        void TakeOutParam1(out int x)
        {
            return; // 1 
        }

        int y;
        TakeOutParam1(out y);
    }

        void TakeOutParam2(out int x)
        {
            return; // 2 
        }
}";
            VerifyDiagnostics(src,
                // (8,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //             return; // 1 
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return;").WithArguments("x").WithLocation(8, 13),
                // (17,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //             return; // 2 
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return;").WithArguments("x").WithLocation(17, 13)
                );
        }

        [Fact]
        [WorkItem(12467, "https://github.com/dotnet/roslyn/issues/12467")]
        public void ParamUnassigned_03()
        {
            var src = @"
class C
{
    public void M1()
    {
        int TakeOutParam1(out int x)
        {
        }

        int y;
        TakeOutParam1(out y);
    }

        int TakeOutParam2(out int x)
        {
        }
}";
            VerifyDiagnostics(src,
                // (6,13): error CS0161: 'TakeOutParam1(out int)': not all code paths return a value
                //         int TakeOutParam1(out int x)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "TakeOutParam1").WithArguments("TakeOutParam1(out int)").WithLocation(6, 13),
                // (6,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         int TakeOutParam1(out int x)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "TakeOutParam1").WithArguments("x").WithLocation(6, 13),
                // (14,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         int TakeOutParam2(out int x)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "TakeOutParam2").WithArguments("x").WithLocation(14, 13),
                // (14,13): error CS0161: 'C.TakeOutParam2(out int)': not all code paths return a value
                //         int TakeOutParam2(out int x)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "TakeOutParam2").WithArguments("C.TakeOutParam2(out int)").WithLocation(14, 13)
                );
        }

        [Fact]
        [WorkItem(12467, "https://github.com/dotnet/roslyn/issues/12467")]
        public void ParamUnassigned_04()
        {
            var src = @"
class C
{
    public void M1()
    {
        int TakeOutParam1(out int x)
        {
            return 1;
        }

        int y;
        TakeOutParam1(out y);
    }

        int TakeOutParam2(out int x)
        {
            return 2;
        }
}";
            VerifyDiagnostics(src,
                // (8,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //             return 1;
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return 1;").WithArguments("x").WithLocation(8, 13),
                // (17,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //             return 2;
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return 2;").WithArguments("x").WithLocation(17, 13)
                );
        }

        [Fact]
        [WorkItem(13172, "https://github.com/dotnet/roslyn/issues/13172")]
        public void InheritUnsafeContext()
        {
            var comp = CreateCompilationWithMscorlib46(@"
using System;
using System.Threading.Tasks;
class C
{
    public void M1()
    {
        async Task<IntPtr> Local()
        {
            await Task.Delay(0);
            return (IntPtr)(void*)null;
        }
        var _ = Local();
    }

    public void M2()
    {
        unsafe
        {
            async Task<IntPtr> Local()
            {
                await Task.Delay(1);
                return (IntPtr)(void*)null;
            }
            var _ = Local();
        }
    }

    public unsafe void M3()
    {
        async Task<IntPtr> Local()
        {
            await Task.Delay(2);
            return (IntPtr)(void*)null;
        }
        var _ = Local();
    }
}

unsafe class D
{
    int* p = null;
    public void M()
    {
        async Task<IntPtr> Local()
        {
            await Task.Delay(3);
            return (IntPtr)p;
        }
        var _ = Local();
    }

    public unsafe void M2()
    {
        unsafe
        {
            async Task<IntPtr> Local()
            {
                await Task.Delay(4);
                return (IntPtr)(void*)null;
            }
            var _ = Local();
        }
    }
}", options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (11,29): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             return (IntPtr)(void*)null;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "void*").WithLocation(11, 29),
                // (11,28): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //             return (IntPtr)(void*)null;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "(void*)null").WithLocation(11, 28),
                // (47,13): error CS4004: Cannot await in an unsafe context
                //             await Task.Delay(3);
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Delay(3)").WithLocation(47, 13),
                // (22,17): error CS4004: Cannot await in an unsafe context
                //                 await Task.Delay(1);
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Delay(1)").WithLocation(22, 17),
                // (59,17): error CS4004: Cannot await in an unsafe context
                //                 await Task.Delay(4);
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Delay(4)").WithLocation(59, 17),
                // (33,13): error CS4004: Cannot await in an unsafe context
                //             await Task.Delay(2);
                Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Delay(2)").WithLocation(33, 13));
        }

        [Fact, WorkItem(16167, "https://github.com/dotnet/roslyn/issues/16167")]
        public void DeclarationInLocalFunctionParameterDefault()
        {
            var text = @"
class C
{
    public static void Main(int arg)
    {
        void Local1(bool b = M(arg is int z1, z1), int s1 = z1) {}
        void Local2(bool b = M(M(out int z2), z2), int s2 = z2) {}
        void Local3(bool b = M(M((int z3, int a2) = (1, 2)), z3), int a3 = z3) {}

        void Local4(bool b = M(arg is var z4, z4), int s1 = z4) {}
        void Local5(bool b = M(M(out var z5), z5), int s2 = z5) {}
        void Local6(bool b = M(M((var z6, int a2) = (1, 2)), z6), int a3 = z6) {}

        int t = z1 + z2 + z3 + z4 + z5 + z6;
    }
    static bool M(out int z) // needed to infer type of z5
    {
        z = 1;
        return true;
    }
    static bool M(params object[] args) => true;
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }
}
";
            // the scope of an expression variable introduced in the default expression
            // of a local function parameter is that default expression.
            var compilation = CreateCompilationWithMscorlib45(text);
            compilation.VerifyDiagnostics(
                // (6,30): error CS1736: Default parameter value for 'b' must be a compile-time constant
                //         void Local1(bool b = M(arg is int z1, z1), int s1 = z1) {}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M(arg is int z1, z1)").WithArguments("b").WithLocation(6, 30),
                // (6,61): error CS0103: The name 'z1' does not exist in the current context
                //         void Local1(bool b = M(arg is int z1, z1), int s1 = z1) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z1").WithArguments("z1").WithLocation(6, 61),
                // (7,30): error CS1736: Default parameter value for 'b' must be a compile-time constant
                //         void Local2(bool b = M(M(out int z2), z2), int s2 = z2) {}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M(M(out int z2), z2)").WithArguments("b").WithLocation(7, 30),
                // (7,61): error CS0103: The name 'z2' does not exist in the current context
                //         void Local2(bool b = M(M(out int z2), z2), int s2 = z2) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(7, 61),
                // (8,35): error CS8185: A declaration is not allowed in this context.
                //         void Local3(bool b = M(M((int z3, int a2) = (1, 2)), z3), int a3 = z3) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int z3").WithLocation(8, 35),
                // (8,30): error CS1736: Default parameter value for 'b' must be a compile-time constant
                //         void Local3(bool b = M(M((int z3, int a2) = (1, 2)), z3), int a3 = z3) {}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M(M((int z3, int a2) = (1, 2)), z3)").WithArguments("b").WithLocation(8, 30),
                // (8,76): error CS0103: The name 'z3' does not exist in the current context
                //         void Local3(bool b = M(M((int z3, int a2) = (1, 2)), z3), int a3 = z3) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(8, 76),
                // (10,30): error CS1736: Default parameter value for 'b' must be a compile-time constant
                //         void Local4(bool b = M(arg is var z4, z4), int s1 = z4) {}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M(arg is var z4, z4)").WithArguments("b").WithLocation(10, 30),
                // (10,61): error CS0103: The name 'z4' does not exist in the current context
                //         void Local4(bool b = M(arg is var z4, z4), int s1 = z4) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z4").WithArguments("z4").WithLocation(10, 61),
                // (11,30): error CS1736: Default parameter value for 'b' must be a compile-time constant
                //         void Local5(bool b = M(M(out var z5), z5), int s2 = z5) {}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M(M(out var z5), z5)").WithArguments("b").WithLocation(11, 30),
                // (11,61): error CS0103: The name 'z5' does not exist in the current context
                //         void Local5(bool b = M(M(out var z5), z5), int s2 = z5) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z5").WithArguments("z5").WithLocation(11, 61),
                // (12,35): error CS8185: A declaration is not allowed in this context.
                //         void Local6(bool b = M(M((var z6, int a2) = (1, 2)), z6), int a3 = z6) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var z6").WithLocation(12, 35),
                // (12,30): error CS1736: Default parameter value for 'b' must be a compile-time constant
                //         void Local6(bool b = M(M((var z6, int a2) = (1, 2)), z6), int a3 = z6) {}
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M(M((var z6, int a2) = (1, 2)), z6)").WithArguments("b").WithLocation(12, 30),
                // (12,76): error CS0103: The name 'z6' does not exist in the current context
                //         void Local6(bool b = M(M((var z6, int a2) = (1, 2)), z6), int a3 = z6) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(12, 76),
                // (14,17): error CS0103: The name 'z1' does not exist in the current context
                //         int t = z1 + z2 + z3 + z4 + z5 + z6;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z1").WithArguments("z1").WithLocation(14, 17),
                // (14,22): error CS0103: The name 'z2' does not exist in the current context
                //         int t = z1 + z2 + z3 + z4 + z5 + z6;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(14, 22),
                // (14,27): error CS0103: The name 'z3' does not exist in the current context
                //         int t = z1 + z2 + z3 + z4 + z5 + z6;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(14, 27),
                // (14,32): error CS0103: The name 'z4' does not exist in the current context
                //         int t = z1 + z2 + z3 + z4 + z5 + z6;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z4").WithArguments("z4").WithLocation(14, 32),
                // (14,37): error CS0103: The name 'z5' does not exist in the current context
                //         int t = z1 + z2 + z3 + z4 + z5 + z6;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z5").WithArguments("z5").WithLocation(14, 37),
                // (14,42): error CS0103: The name 'z6' does not exist in the current context
                //         int t = z1 + z2 + z3 + z4 + z5 + z6;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(14, 42),
                // (6,14): warning CS8321: The local function 'Local1' is declared but never used
                //         void Local1(bool b = M(arg is int z1, z1), int s1 = z1) {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local1").WithArguments("Local1").WithLocation(6, 14),
                // (7,14): warning CS8321: The local function 'Local2' is declared but never used
                //         void Local2(bool b = M(M(out int z2), z2), int s2 = z2) {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local2").WithArguments("Local2").WithLocation(7, 14),
                // (8,14): warning CS8321: The local function 'Local3' is declared but never used
                //         void Local3(bool b = M(M((int z3, int a2) = (1, 2)), z3), int a3 = z3) {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local3").WithArguments("Local3").WithLocation(8, 14),
                // (10,14): warning CS8321: The local function 'Local4' is declared but never used
                //         void Local4(bool b = M(arg is var z4, z4), int s1 = z4) {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local4").WithArguments("Local4").WithLocation(10, 14),
                // (11,14): warning CS8321: The local function 'Local5' is declared but never used
                //         void Local5(bool b = M(M(out var z5), z5), int s2 = z5) {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local5").WithArguments("Local5").WithLocation(11, 14),
                // (12,14): warning CS8321: The local function 'Local6' is declared but never used
                //         void Local6(bool b = M(M((var z6, int a2) = (1, 2)), z6), int a3 = z6) {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local6").WithArguments("Local6").WithLocation(12, 14)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var descendents = tree.GetRoot().DescendantNodes();
            for (int i = 1; i <= 6; i++)
            {
                var name = $"z{i}";
                var designation = descendents.OfType<SingleVariableDesignationSyntax>().Where(d => d.Identifier.ValueText == name).Single();
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(designation);
                Assert.NotNull(symbol);
                Assert.Equal("System.Int32", symbol.Type.ToTestDisplayString());
                var refs = descendents.OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == name).ToArray();
                Assert.Equal(3, refs.Length);
                Assert.Equal(symbol, model.GetSymbolInfo(refs[0]).Symbol);
                Assert.Null(model.GetSymbolInfo(refs[1]).Symbol);
                Assert.Null(model.GetSymbolInfo(refs[2]).Symbol);
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/16451")]
        public void RecursiveParameterDefault()
        {
            var text = @"
class C
{
    public static void Main(int arg)
    {
        int Local(int x = Local()) => 2;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(text);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        [WorkItem(16757, "https://github.com/dotnet/roslyn/issues/16757")]
        public void LocalFunctionParameterDefaultUsingConst()
        {
            var source = @"
class C
{
    public static void Main()
    {
        const int N = 2;
        void Local1(int n = N) { System.Console.Write(n); }
        Local1();
        Local1(3);
    }
}
";
            CompileAndVerify(source, expectedOutput: "23", sourceSymbolValidator: m =>
            {
                var compilation = m.DeclaringCompilation;
                // See https://github.com/dotnet/roslyn/issues/16454; this should actually produce no errors
                compilation.VerifyDiagnostics(
                    // (6,19): warning CS0219: The variable 'N' is assigned but its value is never used
                    //         const int N = 2;
                    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "N").WithArguments("N").WithLocation(6, 19)
                    );
                var tree = compilation.SyntaxTrees[0];
                var model = compilation.GetSemanticModel(tree);
                var descendents = tree.GetRoot().DescendantNodes();

                var parameter = descendents.OfType<ParameterSyntax>().Single();
                Assert.Equal("int n = N", parameter.ToString());
                Assert.Equal("[System.Int32 n = 2]", model.GetDeclaredSymbol(parameter).ToTestDisplayString());

                var name = "N";
                var declarator = descendents.OfType<VariableDeclaratorSyntax>().Where(d => d.Identifier.ValueText == name).Single();
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(declarator);
                Assert.NotNull(symbol);
                Assert.Equal("System.Int32 N", symbol.ToTestDisplayString());
                var refs = descendents.OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == name).ToArray();
                Assert.Equal(1, refs.Length);
                Assert.Same(symbol, model.GetSymbolInfo(refs[0]).Symbol);
            });
        }

        [Fact]
        [WorkItem(15536, "https://github.com/dotnet/roslyn/issues/15536")]
        public void CallFromDifferentSwitchSection_01()
        {
            var source = @"
class Program
{
    static void Main()
    {
        Test(string.Empty);
    }

    static void Test(object o)
    {
        switch (o)
        {
            case string x:
                Assign();
                Print();
                break;
            case int x:
                void Assign() { x = 5; }
                void Print() => System.Console.WriteLine(x);
                break;
        }
    }
}";

            var comp = CreateCompilationWithMscorlib46(source, parseOptions: DefaultParseOptions, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "5");
        }

        [Fact]
        [WorkItem(15536, "https://github.com/dotnet/roslyn/issues/15536")]
        public void CallFromDifferentSwitchSection_02()
        {
            var source = @"
class Program
{
    static void Main()
    {
        Test(string.Empty);
    }

    static void Test(object o)
    {
        switch (o)
        {
            case int x:
                void Assign() { x = 5; }
                void Print() => System.Console.WriteLine(x);
                break;
            case string x:
                Assign();
                Print();
                break;
        }
    }
}";

            var comp = CreateCompilationWithMscorlib46(source, parseOptions: DefaultParseOptions, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "5");
        }

        [Fact]
        [WorkItem(15536, "https://github.com/dotnet/roslyn/issues/15536")]
        public void CallFromDifferentSwitchSection_03()
        {
            var source = @"
class Program
{
    static void Main()
    {
        Test(string.Empty);
    }

    static void Test(object o)
    {
        switch (o)
        {
            case string x:
                Assign();
                System.Action p = Print;
                p();
                break;
            case int x:
                void Assign() { x = 5; }
                void Print() => System.Console.WriteLine(x);
                break;
        }
    }
}";

            var comp = CreateCompilationWithMscorlib46(source, parseOptions: DefaultParseOptions, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "5");
        }

        [Fact]
        [WorkItem(15536, "https://github.com/dotnet/roslyn/issues/15536")]
        public void CallFromDifferentSwitchSection_04()
        {
            var source = @"
class Program
{
    static void Main()
    {
        Test(string.Empty);
    }

    static void Test(object o)
    {
        switch (o)
        {
            case int x:
                void Assign() { x = 5; }
                void Print() => System.Console.WriteLine(x);
                break;
            case string x:
                Assign();
                System.Action p = Print;
                p();
                break;
        }
    }
}";

            var comp = CreateCompilationWithMscorlib46(source, parseOptions: DefaultParseOptions, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "5");
        }

        [Fact]
        [WorkItem(15536, "https://github.com/dotnet/roslyn/issues/15536")]
        public void CallFromDifferentSwitchSection_05()
        {
            var source = @"
class Program
{
    static void Main()
    {
        Test(string.Empty);
    }

    static void Test(object o)
    {
        switch (o)
        {
            case string x:
                Local1();
                break;
             case int x:
                void Local1() => Local2(x = 5);
                break;
             case char x:
                void Local2(int y)
                {
                    System.Console.WriteLine(x = 'a');
                    System.Console.WriteLine(y);
                }
                break;
        }
    }
}";

            var comp = CreateCompilationWithMscorlib46(source, parseOptions: DefaultParseOptions, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput:
@"a
5");
        }

        [Fact]
        [WorkItem(16751, "https://github.com/dotnet/roslyn/issues/16751")]
        public void SemanticModelInAttribute_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        const bool b1 = true;

        void Local1(
            [Test(p = b1)]
            [Test(p = b2)]
            int p1)
        {
        }

        Local1(1);
    }
}

class b1 {}

class Test : System.Attribute
{
    public bool p {get; set;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation.GetDiagnostics().Verify(
                // (10,23): error CS0103: The name 'b2' does not exist in the current context
                //             [Test(p = b2)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b2").WithArguments("b2").WithLocation(10, 23),
                // (6,20): warning CS0219: The variable 'b1' is assigned but its value is never used
                //         const bool b1 = true;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "b1").WithArguments("b1").WithLocation(6, 20)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var b2 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "b2").Single();
            Assert.Null(model.GetSymbolInfo(b2).Symbol);

            var b1 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "b1").Single();
            var b1Symbol = model.GetSymbolInfo(b1).Symbol;
            Assert.Equal("System.Boolean b1", b1Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, b1Symbol.Kind);
        }

        [Fact]
        [WorkItem(19778, "https://github.com/dotnet/roslyn/issues/19778")]
        public void BindDynamicInvocation()
        {
            var source =
@"using System;
class C
{
    static void M()
    {
        dynamic L<T>(Func<dynamic, T> t, object p) => p;
        L(m => L(d => d, null), null);
        L(m => L(d => d, m), null);
    }
}";
            var comp = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef, CSharpRef });
            comp.VerifyEmitDiagnostics(
                // (8,18): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                //         L(m => L(d => d, m), null);
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "d => d").WithLocation(8, 18),
                // (8,16): error CS8322: Cannot pass argument with dynamic type to generic local function 'L' with inferred type arguments.
                //         L(m => L(d => d, m), null);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L(d => d, m)").WithArguments("L").WithLocation(8, 16));
        }

        [Fact]
        [WorkItem(19778, "https://github.com/dotnet/roslyn/issues/19778")]
        public void BindDynamicInvocation_Async()
        {
            var source =
@"using System;
using System.Threading.Tasks;
class C
{
    static void M()
    {
        async Task<dynamic> L<T>(Func<dynamic, T> t, object p)
            => await L(async m => L(async d => await d, m), p);
    }
}";
            var comp = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef, CSharpRef });
            comp.VerifyEmitDiagnostics(
                // (8,37): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                //             => await L(async m => L(async d => await d, m), p);
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "async d => await d").WithLocation(8, 37),
                // (8,35): error CS8322: Cannot pass argument with dynamic type to generic local function 'L' with inferred type arguments.
                //             => await L(async m => L(async d => await d, m), p);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L(async d => await d, m)").WithArguments("L").WithLocation(8, 35),
                // (8,32): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //             => await L(async m => L(async d => await d, m), p);
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(8, 32));
        }

        [Fact]
        [WorkItem(21317, "https://github.com/dotnet/roslyn/issues/21317")]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicGenericArg()
        {
            var src = @"
using System.Collections.Generic;
class C
{
    static void M()
    {
        dynamic val = 2;
        dynamic dynamicList = new List<int>();

        void L1<T>(T x) { }
        L1(val);

        void L2<T>(int x, T y) { }
        L2(1, val);
        L2(val, 3.0f);

        void L3<T>(List<T> x) { }
        L3(dynamicList);

        void L4<T>(int x, params T[] y) { }
        L4(1, 2, val);
        L4(val, 3, 4);

        void L5<T>(T x, params int[] y) { }
        L5(val, 1, 2);
        L5(1, 3, val);
    }
}
";
            VerifyDiagnostics(src,
                // (11,9): error CS8322: Cannot pass argument with dynamic type to generic local function 'L1'. Try specifying the type arguments explicitly.
                //         L1(val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L1(val)").WithArguments("L1").WithLocation(11, 9),
                // (14,9): error CS8322: Cannot pass argument with dynamic type to generic local function 'L2'. Try specifying the type arguments explicitly.
                //         L2(1, val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L2(1, val)").WithArguments("L2").WithLocation(14, 9),
                // (15,9): error CS8322: Cannot pass argument with dynamic type to generic local function 'L2'. Try specifying the type arguments explicitly.
                //         L2(val, 3.0f);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L2(val, 3.0f)").WithArguments("L2").WithLocation(15, 9),
                // (18,9): error CS8322: Cannot pass argument with dynamic type to generic local function 'L3'. Try specifying the type arguments explicitly.
                //         L3(dynamicList);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L3(dynamicList)").WithArguments("L3").WithLocation(18, 9),
                // (21,9): error CS8322: Cannot pass argument with dynamic type to generic local function 'L4'. Try specifying the type arguments explicitly.
                //         L4(1, 2, val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L4(1, 2, val)").WithArguments("L4").WithLocation(21, 9),
                // (22,9): error CS8322: Cannot pass argument with dynamic type to generic local function 'L4'. Try specifying the type arguments explicitly.
                //         L4(val, 3, 4);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L4(val, 3, 4)").WithArguments("L4").WithLocation(22, 9),
                // (25,9): error CS8322: Cannot pass argument with dynamic type to generic local function 'L5'. Try specifying the type arguments explicitly.
                //         L5(val, 1, 2);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L5(val, 1, 2)").WithArguments("L5").WithLocation(25, 9),
                // (26,9): error CS8322: Cannot pass argument with dynamic type to generic local function 'L5'. Try specifying the type arguments explicitly.
                //         L5(1, 3, val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionTypeParameter, "L5(1, 3, val)").WithArguments("L5").WithLocation(26, 9)
                );
        }

        [Fact]
        [WorkItem(23699, "https://github.com/dotnet/roslyn/issues/23699")]
        public void GetDeclaredSymbolOnTypeParameter()
        {
            var src = @"
class C<T>
{
    void M<U>()
    {
        void LocalFunction<T, U, V>(T p1, U p2, V p3)
        {
        }
    }
}
";
            var comp = CreateCompilation(src);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var localDecl = (LocalFunctionStatementSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.LocalFunctionStatement).AsNode();

            var typeParameters = localDecl.TypeParameterList.Parameters;
            var parameters = localDecl.ParameterList.Parameters;
            verifyTypeParameterAndParameter(typeParameters[0], parameters[0], "T");
            verifyTypeParameterAndParameter(typeParameters[1], parameters[1], "U");
            verifyTypeParameterAndParameter(typeParameters[2], parameters[2], "V");

            void verifyTypeParameterAndParameter(TypeParameterSyntax typeParameter, ParameterSyntax parameter, string expected)
            {
                var symbol = model.GetDeclaredSymbol(typeParameter);
                Assert.Equal(expected, symbol.ToTestDisplayString());

                var parameterSymbol = model.GetDeclaredSymbol(parameter);
                Assert.Equal(expected, parameterSymbol.Type.ToTestDisplayString());
                Assert.Same(symbol, parameterSymbol.Type);
            }
        }

        public sealed class ScriptGlobals
        {
            public int SomeGlobal => 42;
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/28001")]
        public void CanAccessScriptGlobalsFromInsideMethod()
        {
            var source = @"
void Method()
{
    LocalFunction();
    void LocalFunction()
    {
        _ = SomeGlobal;
    }
}";
            CreateSubmission(source, new[] { ScriptTestFixtures.HostRef }, hostObjectType: typeof(ScriptGlobals))
                .VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/28001")]
        public void CanAccessScriptGlobalsFromInsideLambda()
        {
            var source = @"
var lambda = new System.Action(() =>
{
    LocalFunction();
    void LocalFunction()
    {
        _ = SomeGlobal;
    }
});";
            CreateSubmission(source, new[] { ScriptTestFixtures.HostRef }, hostObjectType: typeof(ScriptGlobals))
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void CanAccessPreviousSubmissionVariablesFromInsideMethod()
        {
            var previous = CreateSubmission("int previousSubmissionVariable = 42;")
                .VerifyEmitDiagnostics();

            var source = @"
void Method()
{
    LocalFunction();
    void LocalFunction()
    {
        _ = previousSubmissionVariable;
    }
}";
            CreateSubmission(source, previous: previous)
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void CanAccessPreviousSubmissionVariablesFromInsideLambda()
        {
            var previous = CreateSubmission("int previousSubmissionVariable = 42;")
                .VerifyEmitDiagnostics();

            var source = @"
var lambda = new System.Action(() =>
{
    LocalFunction();
    void LocalFunction()
    {
        _ = previousSubmissionVariable;
    }
});";
            CreateSubmission(source, previous: previous)
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void CanAccessPreviousSubmissionMethodsFromInsideMethod()
        {
            var previous = CreateSubmission("void PreviousSubmissionMethod() { }")
                .VerifyEmitDiagnostics();

            var source = @"
void Method()
{
    LocalFunction();
    void LocalFunction()
    {
        PreviousSubmissionMethod();
    }
}";
            CreateSubmission(source, previous: previous)
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void CanAccessPreviousSubmissionMethodsFromInsideLambda()
        {
            var previous = CreateSubmission("void PreviousSubmissionMethod() { }")
                .VerifyEmitDiagnostics();

            var source = @"
var lambda = new System.Action(() =>
{
    LocalFunction();
    void LocalFunction()
    {
        PreviousSubmissionMethod();
    }
});";
            CreateSubmission(source, previous: previous)
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void ShadowNames_01()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
class Program
{
    static void M()
    {
        void F1(object x) { string x = null; } // local
        void F2(object x, string y, int x) { } // parameter
        void F3(object x) { void x() { } } // method
        void F4<x, y>(object x) { void y() { } } // type parameter
        void F5(object M, string Program) { }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            verifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            verifyDiagnostics();

            comp = CreateCompilation(source);
            verifyDiagnostics();

            void verifyDiagnostics()
            {
                comp.VerifyDiagnostics(
                    // (7,36): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //         void F1(object x) { string x = null; } // local
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(7, 36),
                    // (8,41): error CS0100: The parameter name 'x' is a duplicate
                    //         void F2(object x, string y, int x) { } // parameter
                    Diagnostic(ErrorCode.ERR_DuplicateParamName, "x").WithArguments("x").WithLocation(8, 41),
                    // (9,34): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //         void F3(object x) { void x() { } } // method
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 34),
                    // (10,30): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                    //         void F4<x, y>(object x) { void y() { } } // type parameter
                    Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(10, 30),
                    // (10,40): error CS0412: 'y': a parameter, local variable, or local function cannot have the same name as a method type parameter
                    //         void F4<x, y>(object x) { void y() { } } // type parameter
                    Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y").WithArguments("y").WithLocation(10, 40));
            }
        }

        [Fact]
        public void ShadowNames_Local_01()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System.Linq;
class Program
{
    static void M()
    {
        object x = null;
        void F1() { object x = 0; } // local
        void F2(string x) { } // parameter
        void F3() { void x() { } } // method
        void F4<x>() { } // type parameter
        void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,28): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F1() { object x = 0; } // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 28),
                // (10,24): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F2(string x) { } // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 24),
                // (11,26): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F3() { void x() { } } // method
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 26),
                // (12,17): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F4<x>() { } // type parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(12, 17),
                // (13,30): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(13, 30));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_Local_02()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System.Linq;
class Program
{
    static void M()
    {
        void F1() { object x = 0; } // local
        void F2(string x) { } // parameter
        void F3() { void x() { } } // method
        void F4<x>() { } // type parameter
        void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
        object x = null;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (8,28): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F1() { object x = 0; } // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(8, 28),
                // (9,24): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F2(string x) { } // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 24),
                // (10,26): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F3() { void x() { } } // method
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 26),
                // (11,17): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F4<x>() { } // type parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 17),
                // (12,30): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(12, 30));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_Local_03()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System.Linq;
class Program
{
    static void M()
    {
        static void F1() { object x = 0; } // local
        static void F2(string x) { } // parameter
        static void F3() { void x() { } } // method
        static void F4<x>() { } // type parameter
        static void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
        object x = null;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_Parameter()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System.Linq;
class Program
{
    static void M(object x)
    {
        void F1() { object x = 0; } // local
        void F2(string x) { } // parameter
        void F3() { void x() { } } // method
        void F4<x>() { } // type parameter
        void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            // The conflict between the type parameter in F4<x>() and the parameter
            // in M(object x) is not reported, for backwards compatibility.
            comp.VerifyDiagnostics(
                // (8,28): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F1() { object x = 0; } // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(8, 28),
                // (9,24): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F2(string x) { } // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 24),
                // (10,26): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F3() { void x() { } } // method
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 26),
                // (12,30): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(12, 30));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_TypeParameter()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System.Linq;
class Program
{
    static void M<x>()
    {
        void F1() { object x = 0; } // local
        void F2(string x) { } // parameter
        void F3() { void x() { } } // method
        void F4<x>() { } // type parameter
        void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (8,28): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         void F1() { object x = 0; } // local
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(8, 28),
                // (9,24): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         void F2(string x) { } // parameter
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(9, 24),
                // (10,26): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         void F3() { void x() { } } // method
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(10, 26),
                // (11,17): warning CS8387: Type parameter 'x' has the same name as the type parameter from outer method 'Program.M<x>()'
                //         void F4<x>() { } // type parameter
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "x").WithArguments("x", "Program.M<x>()").WithLocation(11, 17),
                // (12,30): error CS1948: The range variable 'x' cannot have the same name as a method type parameter
                //         void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableSameAsTypeParam, "x").WithArguments("x").WithLocation(12, 30));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,17): warning CS8387: Type parameter 'x' has the same name as the type parameter from outer method 'Program.M<x>()'
                //         void F4<x>() { } // type parameter
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "x").WithArguments("x", "Program.M<x>()").WithLocation(11, 17));
        }

        [Fact]
        public void ShadowNames_LocalFunction_01()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System.Linq;
class Program
{
    static void M()
    {
        void x() { }
        void F1() { object x = 0; } // local
        void F2(string x) { } // parameter
        void F3() { void x() { } } // method
        void F4<x>() { } // type parameter
        void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,28): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F1() { object x = 0; } // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 28),
                // (10,24): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F2(string x) { } // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 24),
                // (11,26): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F3() { void x() { } } // method
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 26),
                // (12,17): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F4<x>() { } // type parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(12, 17),
                // (13,30): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         void F5() { _ = from x in new[] { 1, 2, 3 } select x; } // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(13, 30));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_LocalFunction_02()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
class Program
{
    static void M1()
    {
        void M1() { }
    }
    static void M2(object x)
    {
        void x() { }
    }
    static void M3()
    {
        object x = null;
        void x() { }
    }
    static void M4<T>()
    {
        void T() { }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            verifyDiagnostics();

            comp = CreateCompilation(source);
            verifyDiagnostics();

            void verifyDiagnostics()
            {
                comp.VerifyDiagnostics(
                    // (11,14): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //         void x() { }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 14),
                    // (16,14): error CS0128: A local variable or function named 'x' is already defined in this scope
                    //         void x() { }
                    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(16, 14),
                    // (20,14): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                    //         void T() { }
                    Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(20, 14));
            }
        }

        [Fact]
        public void ShadowNames_ThisLocalFunction()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System.Linq;
class Program
{
    static void M()
    {
        void F1() { object F1 = 0; } // local
        void F2(string F2) { } // parameter
        void F3() { void F3() { } } // method
        void F4<F4>() { } // type parameter
        void F5() { _ = from F5 in new[] { 1, 2, 3 } select F5; } // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (8,28): error CS0136: A local or parameter named 'F1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F1() { object F1 = 0; } // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "F1").WithArguments("F1").WithLocation(8, 28),
                // (9,24): error CS0136: A local or parameter named 'F2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F2(string F2) { } // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "F2").WithArguments("F2").WithLocation(9, 24),
                // (10,26): error CS0136: A local or parameter named 'F3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F3() { void F3() { } } // method
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "F3").WithArguments("F3").WithLocation(10, 26),
                // (11,17): error CS0136: A local or parameter named 'F4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         void F4<F4>() { } // type parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "F4").WithArguments("F4").WithLocation(11, 17),
                // (12,30): error CS1931: The range variable 'F5' conflicts with a previous declaration of 'F5'
                //         void F5() { _ = from F5 in new[] { 1, 2, 3 } select F5; } // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "F5").WithArguments("F5").WithLocation(12, 30));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_LocalFunctionInsideLocalFunction_01()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
class Program
{
    static void M<T>(object x)
    {
        void F()
        {
            void G1(int x) { }
            void G2() { int T = 0; }
            void G3<T>() { }
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,25): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             void G1(int x) { }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 25),
                // (10,29): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //             void G2() { int T = 0; }
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(10, 29),
                // (11,21): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'Program.M<T>(object)'
                //             void G3<T>() { }
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "Program.M<T>(object)").WithLocation(11, 21));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,21): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'Program.M<T>(object)'
                //             void G3<T>() { }
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "Program.M<T>(object)").WithLocation(11, 21));
        }

        [Fact]
        public void ShadowNames_LocalFunctionInsideLocalFunction_02()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
class Program
{
    static void M<T>(object x)
    {
        static void F1()
        {
            void G1(int x) { }
        }
        void F2()
        {
            static void G2() { int T = 0; }
        }
        static void F3()
        {
            static void G3<T>() { }
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (17,28): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'Program.M<T>(object)'
                //             static void G3<T>() { }
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "Program.M<T>(object)").WithLocation(17, 28));
        }

        [Fact]
        public void ShadowNames_LocalFunctionInsideLambda_01()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
class Program
{
    static void M()
    {
        Action a1 = () =>
        {
            int x = 0;
            void F1() { object x = null; }
        };
        Action a2 = () =>
        {
            int T = 0;
            void F2<T>() { }
        };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (11,32): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             void F1() { object x = null; }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 32),
                // (16,21): error CS0136: A local or parameter named 'T' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             void F2<T>() { }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "T").WithArguments("T").WithLocation(16, 21));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_LocalFunctionInsideLambda_02()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
class Program
{
    static void M()
    {
        Action<int> a1 = x =>
        {
            void F1(object x) { }
        };
        Action<int> a2 = (int T) =>
        {
            void F2<T>() { }
        };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            // The conflict between the type parameter in F2<T>() and the parameter
            // in a2 is not reported, for backwards compatibility.
            comp.VerifyDiagnostics(
                // (10,28): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             void F1(object x) { }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 28));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_LocalFunctionInsideLambda_03()
        {
            var source =
@"using System;
class Program
{
    static void M()
    {
        Action<int> a = x =>
        {
            void x() { }
            x();
        };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            // The conflict between the local function and the parameter is not reported,
            // for backwards compatibility.
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void StaticWithThisReference()
        {
            var source =
@"#pragma warning disable 8321
class C
{
    void M()
    {
        static object F1() => this.GetHashCode();
        static object F2() => base.GetHashCode();
        static void F3()
        {
            object G3() => ToString();
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,31): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //         static object F1() => this.GetHashCode();
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "this").WithLocation(6, 31),
                // (7,31): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //         static object F2() => base.GetHashCode();
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "base").WithLocation(7, 31),
                // (10,28): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //             object G3() => ToString();
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "ToString").WithLocation(10, 28));
        }

        [Fact]
        public void StaticWithVariableReference()
        {
            var source =
@"#pragma warning disable 8321
class C
{
    static void M(object x)
    {
        object y = null;
        static object F1() => x;
        static object F2() => y;
        static void F3()
        {
            object G3() => x;
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,31): error CS8421: A static local function cannot contain a reference to 'x'.
                //         static object F1() => x;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "x").WithArguments("x").WithLocation(7, 31),
                // (8,31): error CS8421: A static local function cannot contain a reference to 'y'.
                //         static object F2() => y;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "y").WithArguments("y").WithLocation(8, 31),
                // (11,28): error CS8421: A static local function cannot contain a reference to 'x'.
                //             object G3() => x;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "x").WithArguments("x").WithLocation(11, 28));
        }

        [Fact]
        public void StaticWithLocalFunctionVariableReference_01()
        {
            var source =
@"#pragma warning disable 8321
class C
{
    static void M()
    {
        static void F(object x)
        {
            object y = null;
            object G1() => x ?? y;
            static object G2() => x ?? y;
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,35): error CS8421: A static local function cannot contain a reference to 'x'.
                //             static object G2() => x ?? y;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "x").WithArguments("x").WithLocation(10, 35),
                // (10,40): error CS8421: A static local function cannot contain a reference to 'y'.
                //             static object G2() => x ?? y;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "y").WithArguments("y").WithLocation(10, 40));
        }

        [Fact]
        public void StaticWithLocalFunctionVariableReference_02()
        {
            var source =
@"#pragma warning disable 8321
class C
{
    static void M(int x)
    {
        static void F1(int y)
        {
            int F2() => x + y;
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,25): error CS8421: A static local function cannot contain a reference to 'x'.
                //             int F2() => x + y;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "x").WithArguments("x").WithLocation(8, 25));
        }

        [Fact]
        public void Conditional_ThisReferenceInStatic()
        {
            var source =
@"#pragma warning disable 0649
#pragma warning disable 8321
using System.Diagnostics;
class A
{
    internal object _f;
}
class B : A
{
    [Conditional(""MyDefine"")]
    static void F(object o)
    {
    }
    void M()
    {
        static void F1() { F(this); }
        static void F2() { F(base._f); }
        static void F3() { F(_f); }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithPreprocessorSymbols("MyDefine"));
            verifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular);
            verifyDiagnostics();

            void verifyDiagnostics()
            {
                comp.VerifyDiagnostics(
                    // (16,30): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                    //         static void F1() { F(this); }
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "this").WithLocation(16, 30),
                    // (17,30): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                    //         static void F2() { F(base._f); }
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "base").WithLocation(17, 30),
                    // (18,30): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                    //         static void F3() { F(_f); }
                    Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "_f").WithLocation(18, 30));
            }
        }

        [Fact]
        public void NameOf_ThisReferenceInStatic()
        {
            var source =
@"#pragma warning disable 8321
class C
{
    void M()
    {
        static object F1() => nameof(this.ToString);
        static object F2() => nameof(base.GetHashCode);
        static object F3() => nameof(M);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NameOf_InstanceMemberInStatic()
        {
            var source =
@"#pragma warning disable 0649
#pragma warning disable 8321
class C
{
    object _f;
    static void M()
    {
        _ = nameof(_f);
        static object F() => nameof(_f);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = GetNameOfExpressions(tree)[1];
            var symbol = model.GetSymbolInfo(expr).Symbol;
            Assert.Equal("System.Object C._f", symbol.ToTestDisplayString());
        }

        [Fact]
        public void NameOf_CapturedVariableInStatic()
        {
            var source =
@"#pragma warning disable 8321
class C
{
    static void M(object x)
    {
        static object F() => nameof(x);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = GetNameOfExpressions(tree)[0];
            var symbol = model.GetSymbolInfo(expr).Symbol;
            Assert.Equal("System.Object x", symbol.ToTestDisplayString());
        }

        /// <summary>
        /// nameof(x) should bind to shadowing symbol.
        /// </summary>
        [Fact]
        public void NameOf_ShadowedVariable()
        {
            var source =
@"#pragma warning disable 8321
class C
{
    static void M(object x)
    {
        object F()
        {
            int x = 0;
            return nameof(x);
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = GetNameOfExpressions(tree)[0];
            var symbol = model.GetSymbolInfo(expr).Symbol;
            Assert.Equal(SymbolKind.Local, symbol.Kind);
            Assert.Equal("System.Int32 x", symbol.ToTestDisplayString());
        }

        /// <summary>
        /// nameof(T) should bind to shadowing symbol.
        /// </summary>
        [Fact]
        public void NameOf_ShadowedTypeParameter()
        {
            var source =
@"#pragma warning disable 8321
class C
{
    static void M<T>()
    {
        object F1()
        {
            int T = 0;
            return nameof(T);
        }
        object F2<T>()
        {
            return nameof(T);
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,19): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'C.M<T>()'
                //         object F2<T>()
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "C.M<T>()").WithLocation(11, 19));
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = GetNameOfExpressions(tree);
            var symbol = model.GetSymbolInfo(exprs[0]).Symbol;
            Assert.Equal(SymbolKind.Local, symbol.Kind);
            Assert.Equal("System.Int32 T", symbol.ToTestDisplayString());
            symbol = model.GetSymbolInfo(exprs[1]).Symbol;
            Assert.Equal(SymbolKind.TypeParameter, symbol.Kind);
            Assert.Equal("System.Object F2<T>()", symbol.ContainingSymbol.ToTestDisplayString());
        }

        /// <summary>
        /// typeof(T) should bind to nearest type.
        /// </summary>
        [Fact]
        public void TypeOf_ShadowedTypeParameter()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
class C
{
    static void M<T>()
    {
        object F1()
        {
            int T = 0;
            return typeof(T);
        }
        object F2<T>()
        {
            return typeof(T);
        }
        object F3<U>()
        {
            return typeof(U);
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,19): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'C.M<T>()'
                //         object F2<T>()
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "C.M<T>()").WithLocation(12, 19));
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Select(n => n.Type).ToImmutableArray();
            var symbol = model.GetSymbolInfo(exprs[0]).Symbol;
            Assert.Equal(SymbolKind.TypeParameter, symbol.Kind);
            Assert.Equal("void C.M<T>()", symbol.ContainingSymbol.ToTestDisplayString());
            symbol = model.GetSymbolInfo(exprs[1]).Symbol;
            Assert.Equal(SymbolKind.TypeParameter, symbol.Kind);
            Assert.Equal("System.Object F2<T>()", symbol.ContainingSymbol.ToTestDisplayString());
            symbol = model.GetSymbolInfo(exprs[2]).Symbol;
            Assert.Equal(SymbolKind.TypeParameter, symbol.Kind);
            Assert.Equal("System.Object F3<U>()", symbol.ContainingSymbol.ToTestDisplayString());
        }

        /// <summary>
        /// sizeof(T) should bind to nearest type.
        /// </summary>
        [Fact]
        public void SizeOf_ShadowedTypeParameter()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
unsafe class C
{
    static void M<T>() where T : unmanaged
    {
        object F1()
        {
            int T = 0;
            return sizeof(T);
        }
        object F2<T>() where T : unmanaged
        {
            return sizeof(T);
        }
        object F3<U>() where U : unmanaged
        {
            return sizeof(U);
        }
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (12,19): warning CS8387: Type parameter 'T' has the same name as the type parameter from outer method 'C.M<T>()'
                //         object F2<T>()
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter, "T").WithArguments("T", "C.M<T>()").WithLocation(12, 19));
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<SizeOfExpressionSyntax>().Select(n => n.Type).ToImmutableArray();
            var symbol = model.GetSymbolInfo(exprs[0]).Symbol;
            Assert.Equal(SymbolKind.TypeParameter, symbol.Kind);
            Assert.Equal("void C.M<T>()", symbol.ContainingSymbol.ToTestDisplayString());
            symbol = model.GetSymbolInfo(exprs[1]).Symbol;
            Assert.Equal(SymbolKind.TypeParameter, symbol.Kind);
            Assert.Equal("System.Object F2<T>()", symbol.ContainingSymbol.ToTestDisplayString());
            symbol = model.GetSymbolInfo(exprs[2]).Symbol;
            Assert.Equal(SymbolKind.TypeParameter, symbol.Kind);
            Assert.Equal("System.Object F3<U>()", symbol.ContainingSymbol.ToTestDisplayString());
        }

        private static ImmutableArray<ExpressionSyntax> GetNameOfExpressions(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes().
                OfType<InvocationExpressionSyntax>().
                Where(n => n.Expression.ToString() == "nameof").
                Select(n => n.ArgumentList.Arguments[0].Expression).
                ToImmutableArray();
        }

        [Fact]
        public void ShadowWithSelfReferencingLocal()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        int x = 13;
        void Local()
        {
            int x = (x = 0) + 42;
            Console.WriteLine(x);
        }
        Local();
        Console.WriteLine(x);
    }
}";
            CompileAndVerify(source, expectedOutput:
@"42
13");
        }

        [Fact, WorkItem(38129, "https://github.com/dotnet/roslyn/issues/38129")]
        public void StaticLocalFunctionLocalFunctionReference()
        {
            var source =
@"#pragma warning disable 8321
class C
{
    static void M()
    {
        void F1() {}
        static void F2() {}

        static void F3()
        {
            F1();
            F2();
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,13): error CS8421: A static local function cannot contain a reference to 'F1'.
                //             F1();
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "F1()").WithArguments("F1").WithLocation(11, 13));
        }

        [Fact, WorkItem(38240, "https://github.com/dotnet/roslyn/issues/38240")]
        public void StaticLocalFunctionLocalFunctionDelegateReference()
        {
            var source =
@"#pragma warning disable 8321
using System;
class C
{
    static void M()
    {
        void F1() {}

        static void F2()
        {
            Action a = F1;
            _ = new Action(F1);
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,24): error CS8421: A static local function cannot contain a reference to 'F1'.
                //             Action a = F1;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "F1").WithArguments("F1").WithLocation(11, 24),
                // (12,28): error CS8421: A static local function cannot contain a reference to 'F1'.
                //             _ = new Action(F1);
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "F1").WithArguments("F1").WithLocation(12, 28));
        }

        [Fact, WorkItem(38240, "https://github.com/dotnet/roslyn/issues/38240")]
        public void StaticLocalFunctionStaticFunctionsDelegateReference()
        {
            var source =
@"#pragma warning disable 8321
using System;
class C
{
    static void M()
    {
        static void F1() {}
        
        static void F2()
        {
            Action m = M;
            Action f1 = F1;
            _ = new Action(M);
            _ = new Action(F1);
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(38240, "https://github.com/dotnet/roslyn/issues/38240")]
        public void StaticLocalFunctionThisAndBaseDelegateReference()
        {
            var source =
@"#pragma warning disable 8321
using System;
class B
{
    public virtual void M() {}
}

class C : B
{
    public override void M()
    {
        static void F()
        {
            Action a1 = base.M;
            Action a2 = this.M;
            Action a3 = M;
            _ = new Action(base.M);
            _ = new Action(this.M);
            _ = new Action(M);
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (14,25): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //             Action a1 = base.M;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "base").WithLocation(14, 25),
                // (15,25): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //             Action a2 = this.M;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "this").WithLocation(15, 25),
                // (16,25): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //             Action a3 = M;
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "M").WithLocation(16, 25),
                // (17,28): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //             _ = new Action(base.M);
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "base").WithLocation(17, 28),
                // (18,28): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //             _ = new Action(this.M);
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "this").WithLocation(18, 28),
                // (19,28): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //             _ = new Action(M);
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "M").WithLocation(19, 28));
        }

        [Fact, WorkItem(38240, "https://github.com/dotnet/roslyn/issues/38240")]
        public void StaticLocalFunctionDelegateReferenceWithReceiver()
        {
            var source =
@"#pragma warning disable 649
#pragma warning disable 8321
using System;
class C
{
    object f;
    
    void M()
    {
        object l;
        
        static void F1()
        {
            _ = new Func<int>(f.GetHashCode);
            _ = new Func<int>(l.GetHashCode);
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (14,31): error CS8422: A static local function cannot contain a reference to 'this' or 'base'.
                //             _ = new Func<int>(f.GetHashCode);
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis, "f").WithLocation(14, 31),
                // (15,31): error CS8421: A static local function cannot contain a reference to 'l'.
                //             _ = new Func<int>(l.GetHashCode);
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "l").WithArguments("l").WithLocation(15, 31));
        }
    }
}
