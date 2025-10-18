// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SymbolId;

public sealed class SymbolKeyErrorTypeTests : SymbolKeyTestBase
{
    [Fact]
    public void GenericType_NotMissingWithMissingTypeArgument()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void GenericType_MissingWithNonMissingTypeArgument()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void GenericType_MissingWithMissingTypeArgument()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void Tuple_MissingTypes()
        => VerifyResolution("""
            namespace N
            {
                public class C
                {
                    public void M((string, int) x)
                    {
                    }
                }
            }
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void Tuple_NonMissingTypes()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void Array_MissingElementType()
        => VerifyResolution("""
            namespace N
            {
                public class C
                {
                    public void M(string[] x)
                    {
                    }
                }
            }
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void Array_NonMissingElementType()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void Pointer_MissingType()
        => VerifyResolution("""
            namespace N
            {
                public class C
                {
                    public unsafe void M(int *x)
                    {
                    }
                }
            }
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void Pointer_NonMissingType()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void NestedType_MissingInGenericContainer()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void NestedType_MissingInNonGenericContainer()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.M"));

    [Fact]
    public void Method_MissingReturnType()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.Create"));

    [Fact]
    public void Method_MissingParameterType()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.Create"));

    [Fact]
    public void Constructor_MissingParameterType()
        => VerifyResolution("""
            public class C
            {
                public C(string x)
                {
                }
            }
            """, c => c.GetMember("C..ctor"));

    [Fact]
    public void Indexer_MissingParameterType()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.this[]"));

    [Fact]
    public void Indexer_MissingReturnType()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.this[]"));

    [Fact]
    public void Property_MissingReturnType()
        => VerifyResolution("""
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
            """, c => c.GetMember("N.C.P"));

    [Fact]
    public void EventField_MissingReturnType()
        => VerifyResolution("""
            namespace N
            {
                public class C
                {
                    public event System.EventHandler E;
                }
            }
            """, c => c.GetMember("N.C.E"));

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
        Assert.Equal(symbol.ToDisplayString(), resolution.Symbol.ToDisplayString());
    }
}
