// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    public class InstructionDecoderTests : ExpressionCompilerTestBase
    {
        [Fact, WorkItem(1107977)]
        private void GetNameGenericAsync()
        {
            var source = @"
using System.Threading.Tasks;
class C
{
    static async Task<T> M<T>(T x)
    {
        await Task.Yield();
        return x;
    }
}";

            Assert.Equal(
                    "C.M<T>(T x)",
                    GetName(source, "C.<M>d__0.MoveNext", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));
        }
        [Fact]

        private void GetNameLambda()
        {
            var source = @"
using System;
class C
{
    void M()
    {
        Func<int> f = () => 3;
    }
}";

            Assert.Equal(
                "C.M.AnonymousMethod__0_0()",
                GetName(source, "C.<>c.<M>b__0_0", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));
        }
        [Fact]

        private void GetNameGenericLambda()
        {
            var source = @"
using System;
class C<T>
{
    void M<U>() where U : T
    {
        Func<U, T> f = (U u) => u;
    }
}";

            Assert.Equal(
                "C<T>.M.AnonymousMethod__0_0(U u)",
                GetName(source, "C.<>c__0.<M>b__0_0", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));
        }
        [Fact]

        private void GetNameProperties()
        {
            var source = @"
class C
{
    int P { get; set; }
    int this[object x]
    {
        get { return 42; }
        set { }
    }
}";

            Assert.Equal(
                "C.P.get()",
                GetName(source, "C.get_P", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));

            Assert.Equal(
                "C.P.set(int value)",
                GetName(source, "C.set_P", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));

            Assert.Equal(
                "C.this[object].get(object x)",
                GetName(source, "C.get_Item", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));

            Assert.Equal(
                "C.this[object].set(object x, int value)",
                GetName(source, "C.set_Item", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));
        }
        [Fact]

        private void GetNameExplicitInterfaceImplementation()
        {
            var source = @"
using System;
class C : IDisposable
{
    void IDisposable.Dispose() { }
}";

            Assert.Equal(
                "C.System.IDisposable.Dispose()",
                GetName(source, "C.System.IDisposable.Dispose", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));
        }
        [Fact]

        private void GetNameExtensionMethod()
        {
            var source = @"
static class Extensions
{
    static void M(this string @this) { }
}";

            Assert.Equal(
                "Extensions.M(string this)",
                GetName(source, "Extensions.M", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));
        }
        [Fact]

        private void GetNameArgumentFlagsNone()
        {
            var source = @"
static class C
{
    static void M1() { }
    static void M2(int x, int y) { }
}";

            Assert.Equal(
                "C.M1",
                GetName(source, "C.M1", DkmVariableInfoFlags.None));

            Assert.Equal(
                "C.M2",
                GetName(source, "C.M2", DkmVariableInfoFlags.None));
        }

        private string GetName(string source, string methodName, DkmVariableInfoFlags argumentFlags, params string[] argumentValues)
        {
            Debug.Assert((argumentFlags & (DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types)) == argumentFlags,
                "Unexpected argumentFlags", "argumentFlags = {0}", argumentFlags);

            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            var runtime = CreateRuntimeInstance(compilation);
            var moduleInstances = runtime.Modules;
            var blocks = moduleInstances.SelectAsArray(m => m.MetadataBlock);
            compilation = blocks.ToCompilation();
            var frame = (PEMethodSymbol)GetMethodOrTypeBySignature(compilation, methodName);

            // Once we have the method token, we want to look up the method (again)
            // using the same helper as the product code.  This helper will also map
            // async/iterator "MoveNext" methods to the original source method.
            var method = compilation.GetSourceMethod(
                ((PEModuleSymbol)frame.ContainingModule).Module.GetModuleVersionIdOrThrow(),
                MetadataTokens.GetToken(frame.Handle));
            var includeParameterTypes = argumentFlags.Includes(DkmVariableInfoFlags.Types);
            var includeParameterNames = argumentFlags.Includes(DkmVariableInfoFlags.Names);
            ArrayBuilder<string> builder = null;
            if (argumentValues.Length > 0)
            {
                builder = ArrayBuilder<string>.GetInstance();
                builder.AddRange(argumentValues);
            }

            var frameDecoder = CSharpInstructionDecoder.Instance;
            var frameName = frameDecoder.GetName(method, includeParameterTypes, includeParameterNames, builder);
            if (builder != null)
            {
                builder.Free();
            }

            return frameName;
        }
    }
}
