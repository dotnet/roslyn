// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class InstructionDecoderTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void GetNameGenerics()
        {
            var source = @"
using System;
class Class1<T>
{
    void M1<U>(Action<Int32> a)
    {
    }
    void M2<U>(Action<T> a)
    {
    }
    void M3<U>(Action<U> a)
    {
    }
}";

            Assert.Equal(
                "Class1<T>.M1<U>(System.Action<int> a)",
                GetName(source, "Class1.M1", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));

            Assert.Equal(
                "Class1<T>.M2<U>(System.Action<T> a)",
                GetName(source, "Class1.M2", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));

            Assert.Equal(
                "Class1<T>.M3<U>(System.Action<U> a)",
                GetName(source, "Class1.M3", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types));

            Assert.Equal(
                "Class1<string>.M1<decimal>(System.Action<int> a)",
                GetName(source, "Class1.M1", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types, new[] { typeof(string), typeof(decimal) }));

            Assert.Equal(
                "Class1<string>.M2<decimal>(System.Action<string> a)",
                GetName(source, "Class1.M2", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types, new[] { typeof(string), typeof(decimal) }));

            Assert.Equal(
                "Class1<string>.M3<decimal>(System.Action<decimal> a)",
                GetName(source, "Class1.M3", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types, new[] { typeof(string), typeof(decimal) }));
        }

        [Fact]
        public void GetNameNullTypeArguments()
        {
            var source = @"
using System;
class Class1<T>
{
    void M<U>(Action<U> a)
    {
    }
}";

            Assert.Equal(
                "Class1<T>.M<U>(System.Action<U> a)",
                GetName(source, "Class1.M", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types, typeArguments: new Type[] { null, null }));

            Assert.Equal(
                "Class1<T>.M<U>(System.Action<U> a)",
                GetName(source, "Class1.M", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types, typeArguments: new[] { typeof(string), null }));

            Assert.Equal(
                "Class1<T>.M<U>(System.Action<U> a)",
                GetName(source, "Class1.M", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types, typeArguments: new[] { null, typeof(decimal) }));
        }

        [Fact]
        public void GetNameGenericArgumentTypeNotInReferences()
        {
            var source = @"
class Class1
{
}";

            var serializedTypeArgumentName = "Class1, " + nameof(InstructionDecoderTests) + ", Culture=neutral, PublicKeyToken=null";
            Assert.Equal(
                "System.Collections.Generic.Comparer<Class1>.Create(System.Comparison<Class1> comparison)",
                GetName(source, "System.Collections.Generic.Comparer.Create", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types, typeArguments: new[] { serializedTypeArgumentName }));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107977")]
        public void GetNameGenericAsync()
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
                    "C.M<System.Exception>(System.Exception x)",
                    GetName(source, "C.<M>d__0.MoveNext", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types, new[] { typeof(Exception) }));
        }

        [Fact]
        public void GetNameLambda()
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
        public void GetNameGenericLambda()
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
                "C<System.Exception>.M.AnonymousMethod__0_0(System.ArgumentException u)",
                GetName(source, "C.<>c__0.<M>b__0_0", DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types, new[] { typeof(Exception), typeof(ArgumentException) }));
        }

        [Fact]
        public void GetNameProperties()
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
        public void GetNameExplicitInterfaceImplementation()
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
        public void GetNameExtensionMethod()
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
        public void GetNameArgumentFlagsNone()
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107978")]
        public void GetNameRefAndOutParameters()
        {
            var source = @"
class C
{
    static void M(ref int x, out int y)
    {
        y = x;
    }
}";

            Assert.Equal(
                "C.M",
                GetName(source, "C.M", DkmVariableInfoFlags.None));

            Assert.Equal(
                "C.M(1, 2)",
                GetName(source, "C.M", DkmVariableInfoFlags.None, argumentValues: ["1", "2"]));

            Assert.Equal(
                "C.M(ref int, out int)",
                GetName(source, "C.M", DkmVariableInfoFlags.Types));

            Assert.Equal(
                "C.M(x, y)",
                GetName(source, "C.M", DkmVariableInfoFlags.Names));

            Assert.Equal(
                "C.M(ref int x, out int y)",
                GetName(source, "C.M", DkmVariableInfoFlags.Types | DkmVariableInfoFlags.Names));
        }

        [Fact]
        public void GetNameParamsParameters()
        {
            var source = @"
class C
{
    static void M(params int[] x)
    {
    }
}";

            Assert.Equal(
                "C.M(int[] x)",
                GetName(source, "C.M", DkmVariableInfoFlags.Types | DkmVariableInfoFlags.Names));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1154945")]
        public void GetNameIncorrectNumberOfArgumentValues()
        {
            var source = @"
class C
{
    void M(int x, int y)
    {
    }
}";
            var expected = "C.M(int x, int y)";

            Assert.Equal(expected,
                GetName(source, "C.M", DkmVariableInfoFlags.Types | DkmVariableInfoFlags.Names, argumentValues: []));

            Assert.Equal(expected,
                GetName(source, "C.M", DkmVariableInfoFlags.Types | DkmVariableInfoFlags.Names, argumentValues: ["1"]));

            Assert.Equal(expected,
                GetName(source, "C.M", DkmVariableInfoFlags.Types | DkmVariableInfoFlags.Names, argumentValues: ["1", "2", "3"]));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1134081")]
        public void GetFileNameWithoutExtension()
        {
            Assert.Equal(".", MetadataUtilities.GetFileNameWithoutExtension("."));
            Assert.Equal(".a", MetadataUtilities.GetFileNameWithoutExtension(".a"));
            Assert.Equal("a.", MetadataUtilities.GetFileNameWithoutExtension("a."));
            Assert.Equal(".dll.", MetadataUtilities.GetFileNameWithoutExtension(".dll."));
            Assert.Equal("a.b", MetadataUtilities.GetFileNameWithoutExtension("a.b"));
            Assert.Equal("a", MetadataUtilities.GetFileNameWithoutExtension("a.dll"));
            Assert.Equal("a", MetadataUtilities.GetFileNameWithoutExtension("a.exe"));
            Assert.Equal("a", MetadataUtilities.GetFileNameWithoutExtension("a.netmodule"));
            Assert.Equal("a", MetadataUtilities.GetFileNameWithoutExtension("a.winmd"));
            Assert.Equal("a.b.c", MetadataUtilities.GetFileNameWithoutExtension("a.b.c"));
            Assert.Equal("a.b.c", MetadataUtilities.GetFileNameWithoutExtension("a.b.c.dll"));
            Assert.Equal("mscorlib.nlp", MetadataUtilities.GetFileNameWithoutExtension("mscorlib.nlp"));
            Assert.Equal("Microsoft.CodeAnalysis", MetadataUtilities.GetFileNameWithoutExtension("Microsoft.CodeAnalysis"));
            Assert.Equal("Microsoft.CodeAnalysis", MetadataUtilities.GetFileNameWithoutExtension("Microsoft.CodeAnalysis.dll"));
        }

        [Fact]
        public void GetReturnTypeNamePrimitive()
        {
            var source = @"
static class C
{
    static uint M1() { return 42; }
}";

            Assert.Equal("uint", GetReturnTypeName(source, "C.M1"));
        }

        [Fact]
        public void GetReturnTypeNameNested()
        {
            var source = @"
static class C
{
    static N.D.E M1() { return default(N.D.E); }
}
namespace N
{
    class D
    {
        internal struct E
        {
        }
    }
}";

            Assert.Equal("N.D.E", GetReturnTypeName(source, "C.M1"));
        }

        [Fact]
        public void GetReturnTypeNameGenericOfPrimitive()
        {
            var source = @"
using System;
class C
{
    Action<Int32> M1() { return null; }
}";

            Assert.Equal("System.Action<int>", GetReturnTypeName(source, "C.M1"));
        }

        [Fact]
        public void GetReturnTypeNameGenericOfNested()
        {
            var source = @"
using System;
class C
{
    Action<D> M1() { return null; }
    class D
    {
    }
}";

            Assert.Equal("System.Action<C.D>", GetReturnTypeName(source, "C.M1"));
        }

        [Fact]
        public void GetReturnTypeNameGenericOfGeneric()
        {
            var source = @"
using System;
class C
{
    Action<Func<T>> M1<T>() { return null; }
}";

            Assert.Equal("System.Action<System.Func<object>>", GetReturnTypeName(source, "C.M1", [typeof(object)]));
        }

        private string GetName(string source, string methodName, DkmVariableInfoFlags argumentFlags, Type[] typeArguments = null, string[] argumentValues = null)
        {
            var serializedTypeArgumentNames = typeArguments?.Select(t => t?.AssemblyQualifiedName).ToArray();
            return GetName(source, methodName, argumentFlags, serializedTypeArgumentNames, argumentValues);
        }

        private string GetName(string source, string methodName, DkmVariableInfoFlags argumentFlags, string[] typeArguments, string[] argumentValues = null)
        {
            Debug.Assert((argumentFlags & (DkmVariableInfoFlags.Names | DkmVariableInfoFlags.Types)) == argumentFlags,
                "Unexpected argumentFlags", "argumentFlags = {0}", argumentFlags);

            var instructionDecoder = CSharpInstructionDecoder.Instance;
            var method = GetConstructedMethod(source, methodName, typeArguments, instructionDecoder);

            var includeParameterTypes = argumentFlags.Includes(DkmVariableInfoFlags.Types);
            var includeParameterNames = argumentFlags.Includes(DkmVariableInfoFlags.Names);
            ArrayBuilder<string> builder = null;
            if (argumentValues != null)
            {
                builder = ArrayBuilder<string>.GetInstance();
                builder.AddRange(argumentValues);
            }

            var name = instructionDecoder.GetName(method, includeParameterTypes, includeParameterNames, builder);
            builder?.Free();

            return name;
        }

        private string GetReturnTypeName(string source, string methodName, Type[] typeArguments = null)
        {
            var instructionDecoder = CSharpInstructionDecoder.Instance;
            var serializedTypeArgumentNames = typeArguments?.Select(t => t?.AssemblyQualifiedName).ToArray();
            var method = GetConstructedMethod(source, methodName, serializedTypeArgumentNames, instructionDecoder);

            return instructionDecoder.GetReturnTypeName(method);
        }

        private MethodSymbol GetConstructedMethod(string source, string methodName, string[] serializedTypeArgumentNames, CSharpInstructionDecoder instructionDecoder)
        {
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugDll, assemblyName: nameof(InstructionDecoderTests));
            var runtime = CreateRuntimeInstance(compilation);
            var moduleInstances = runtime.Modules;
            var blocks = moduleInstances.SelectAsArray(m => m.MetadataBlock);
            compilation = blocks.ToCompilation(default(Guid), MakeAssemblyReferencesKind.AllAssemblies);
            var frame = (PEMethodSymbol)GetMethodOrTypeBySignature(compilation, methodName);

            // Once we have the method token, we want to look up the method (again)
            // using the same helper as the product code.  This helper will also map
            // async/iterator "MoveNext" methods to the original source method.
            MethodSymbol method = compilation.GetSourceMethod(
                ((PEModuleSymbol)frame.ContainingModule).Module.GetModuleVersionIdOrThrow(),
                frame.Handle);
            if (serializedTypeArgumentNames != null)
            {
                Assert.NotEmpty(serializedTypeArgumentNames);
                var typeParameters = instructionDecoder.GetAllTypeParameters(method);
                Assert.NotEmpty(typeParameters);
                // Use the same helper method as the FrameDecoder to get the TypeSymbols for the
                // generic type arguments (rather than using EETypeNameDecoder directly).
                var typeArguments = instructionDecoder.GetTypeSymbols(compilation, method, serializedTypeArgumentNames);
                if (!typeArguments.IsEmpty)
                {
                    method = instructionDecoder.ConstructMethod(method, typeParameters, typeArguments);
                }
            }

            return method;
        }
    }
}
