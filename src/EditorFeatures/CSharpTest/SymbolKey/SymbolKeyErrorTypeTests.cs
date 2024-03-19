// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SymbolId
{
    public class SymbolKeyErrorTypeTests : SymbolKeyTestBase
    {
        [Fact]
        public void GenericType_NotMissingWithMissingTypeArgument()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public void M(D<string> x)
                        {
                        }
                    }

                    public class D<T>
                    {
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void GenericType_MissingWithNonMissingTypeArgument()
        {
            var source = """
                using System.Collections.Generic;

                namespace N
                {
                    public class C
                    {
                        public void M(List<D> x)
                        {
                        }
                    }

                    public class D
                    {
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void GenericType_MissingWithMissingTypeArgument()
        {
            var source = """
                using System.Collections.Generic;

                namespace N
                {
                    public class C
                    {
                        public void M(List<string> x)
                        {
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void Tuple_MissingTypes()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public void M((string, int) x)
                        {
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void Tuple_NonMissingTypes()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public void M((C, D) x)
                        {
                        }
                    }

                    public class D
                    {
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void Array_MissingElementType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public void M(string[] x)
                        {
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void Array_NonMissingElementType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public void M(D[] x)
                        {
                        }
                    }

                    public class D
                    {
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void Pointer_MissingType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public unsafe void M(int *x)
                        {
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void Pointer_NonMissingType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public unsafe void M(S *x)
                        {
                        }
                    }

                    public struct S
                    {
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void NestedType_MissingInGenericContainer()
        {
            var source = """
                using System.Collections.Generic;

                namespace N
                {
                    public class C
                    {
                        public void M(List<int>.Enumerator x)
                        {
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void NestedType_MissingInNonGenericContainer()
        {
            var source = """
                using System.Diagnostics;

                namespace N
                {
                    public class C
                    {
                        public void M(DebuggableAttribute.DebuggingModes x)
                        {
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.M"));
        }

        [Fact]
        public void Method_MissingReturnType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public string Create()
                        {
                            return new string('c', 1);
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.Create"));
        }

        [Fact]
        public void Method_MissingParameterType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public C Create(string x)
                        {
                            return new C();
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.Create"));
        }

        [Fact]
        public void Constructor_MissingParameterType()
        {
            var source = """
                public class C
                {
                    public C(string x)
                    {
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("C..ctor"));
        }

        [Fact]
        public void Indexer_MissingParameterType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public C this[string x]
                        {
                            get { return null; }
                            set { }
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.this[]"));
        }

        [Fact]
        public void Indexer_MissingReturnType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public string this[C x]
                        {
                            get { return null; }
                            set { }
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.this[]"));
        }

        [Fact]
        public void Property_MissingReturnType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public string P
                        {
                            get { return null; }
                            set { }
                        }
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.P"));
        }

        [Fact]
        public void EventField_MissingReturnType()
        {
            var source = """
                namespace N
                {
                    public class C
                    {
                        public event System.EventHandler E;
                    }
                }
                """;

            VerifyResolution(source, c => c.GetMember("N.C.E"));
        }

        private static void VerifyResolution(string source, Func<Compilation, ISymbol> symbolToResolve)
        {
            var sourceCompilation = (Compilation)CreateCompilation(source, options: new(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
            var symbol = symbolToResolve(sourceCompilation);

            Assert.NotNull(symbol);

            var symbolKey = SymbolKey.CreateString(symbol);

            // Create a compilation that references our library, but doesn't reference types needed by the library.
            // This emulates the experience in Go To Definition when navigating to metadata from the .NET runtime when
            // implementations are split over multiple assemblies with various type forwards in play.
            // For example:
            //   System.Uri exists in System.Private.Uri.dll, but System.String exists in System.Private.CoreLib.dll
            //   and we want to allow a symbol for System.Uri.Create(System.String) to resolve correctly even when the
            //   System.Private.CoreLib reference is missing.
            var emptyCompilation = CSharpCompilation.Create("empty", options: new(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false))
                .AddReferences(sourceCompilation.EmitToImageReference());

            var resolution = SymbolKey.ResolveString(symbolKey, emptyCompilation, ignoreAssemblyKey: true, out var failureReason, CancellationToken.None);

            Assert.Null(failureReason);
            Assert.NotNull(resolution.Symbol);

            // Since we expect some types to be error types, we just use display string to make sure we found the right
            // symbol.
            Assert.Equal(symbol.ToDisplayString(), resolution.Symbol!.ToDisplayString());
        }
    }
}
