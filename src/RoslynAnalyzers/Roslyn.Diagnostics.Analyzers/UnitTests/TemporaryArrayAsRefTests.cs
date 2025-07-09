// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using CSharpLanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using VerifyCS = Test.Utilities.CSharpSecurityCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.TemporaryArrayAsRefAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicSecurityCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.TemporaryArrayAsRefAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VisualBasicLanguageVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class TemporaryArrayAsRefTests
    {
        public const string TemporaryArraySource_CSharp = """

            namespace Microsoft.CodeAnalysis.Shared.Collections
            {
                internal struct TemporaryArray<T> : System.IDisposable
                {
                    public void Dispose() { }
                }

                internal static class TemporaryArrayExtensions
                {
                    public static ref TemporaryArray<T> AsRef<T>(this in TemporaryArray<T> array) => throw null;
                }
            }

            """;
        public const string TemporaryArraySource_VisualBasic = """

            Namespace Microsoft.CodeAnalysis.Shared.Collections
                Friend Structure TemporaryArray(Of T)
                    Implements System.IDisposable

                    Public Sub Dispose() Implements System.IDisposable.Dispose
                    End Sub
                End Structure

                Friend Module TemporaryArrayExtensions
                    <System.Runtime.CompilerServices.Extension>
                    Public Function AsRef(Of T)(array As TemporaryArray(Of T)) As TemporaryArray(Of T)
                        Return Nothing
                    End Function
                End Module
            End Namespace

            """;

        [Fact]
        public async Task TestUsingVariable_CSharpAsync()
        {
            var code = """

                using Microsoft.CodeAnalysis.Shared.Collections;

                class C
                {
                    void Method()
                    {
                        using (var array = new TemporaryArray<int>())
                        {
                            ref var arrayRef1 = ref array.AsRef();
                            ref var arrayRef2 = ref TemporaryArrayExtensions.AsRef(in array);
                        }
                    }
                }


                """ + TemporaryArraySource_CSharp;

            await new VerifyCS.Test
            {
                LanguageVersion = CSharpLanguageVersion.CSharp9,
                TestCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestUsingVariable_VisualBasicAsync()
        {
            var code = """

                Imports Microsoft.CodeAnalysis.Shared.Collections

                Class C
                    Sub Method()
                        Using array = New TemporaryArray(Of Integer)()
                            Dim arrayRef1 = array.AsRef()
                            Dim arrayRef2 = TemporaryArrayExtensions.AsRef(array)
                        End Using
                    End Sub
                End Class


                """ + TemporaryArraySource_VisualBasic;

            await new VerifyVB.Test
            {
                LanguageVersion = VisualBasicLanguageVersion.VisualBasic16,
                TestCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestUsingDeclarationVariable_CSharpAsync()
        {
            var code = """

                using Microsoft.CodeAnalysis.Shared.Collections;

                class C
                {
                    void Method()
                    {
                        using var array = new TemporaryArray<int>();
                        ref var arrayRef1 = ref array.AsRef();
                        ref var arrayRef2 = ref TemporaryArrayExtensions.AsRef(in array);
                    }
                }


                """ + TemporaryArraySource_CSharp;

            await new VerifyCS.Test
            {
                LanguageVersion = CSharpLanguageVersion.CSharp9,
                TestCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNonUsingVariable_CSharpAsync()
        {
            var code = """

                using Microsoft.CodeAnalysis.Shared.Collections;

                class C
                {
                    void Method()
                    {
                        var array = new TemporaryArray<int>();
                        ref var arrayRef1 = ref [|array.AsRef()|];
                        ref var arrayRef2 = ref [|TemporaryArrayExtensions.AsRef(in array)|];
                    }
                }


                """ + TemporaryArraySource_CSharp;

            await new VerifyCS.Test
            {
                LanguageVersion = CSharpLanguageVersion.CSharp9,
                TestCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNonUsingVariable_VisualBasicAsync()
        {
            var code = """

                Imports Microsoft.CodeAnalysis.Shared.Collections

                Class C
                    Sub Method()
                        Dim array = New TemporaryArray(Of Integer)()
                        Dim arrayRef1 = [|array.AsRef()|]
                        Dim arrayRef2 = [|TemporaryArrayExtensions.AsRef(array)|]
                    End Sub
                End Class


                """ + TemporaryArraySource_VisualBasic;

            await new VerifyVB.Test
            {
                LanguageVersion = VisualBasicLanguageVersion.VisualBasic16,
                TestCode = code,
            }.RunAsync();
        }
    }
}
