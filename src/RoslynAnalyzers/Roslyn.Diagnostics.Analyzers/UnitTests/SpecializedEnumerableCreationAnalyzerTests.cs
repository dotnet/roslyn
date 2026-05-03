// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpSpecializedEnumerableCreationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Roslyn.Diagnostics.VisualBasic.Analyzers.BasicSpecializedEnumerableCreationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class SpecializedEnumerableCreationAnalyzerTests
    {
        private readonly string _csharpSpecializedCollectionsDefinition = """

            namespace Roslyn.Utilities
            {
                public class SpecializedCollections { }
            }

            """;
        private readonly string _basicSpecializedCollectionsDefinition = """

            Namespace Roslyn.Utilities
                Public Class SpecializedCollections
                End Class
            End Namespace

            """;

        private static DiagnosticResult GetCSharpEmptyEnumerableResultAt(int line, int column) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule).WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs

        private static DiagnosticResult GetBasicEmptyEnumerableResultAt(int line, int column) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic(SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule).WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs

        private static DiagnosticResult GetCSharpSingletonEnumerableResultAt(int line, int column) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule).WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs

        private static DiagnosticResult GetBasicSingletonEnumerableResultAt(int line, int column) =>
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyVB.Diagnostic(SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule).WithLocation(line, column);
#pragma warning restore RS0030 // Do not use banned APIs

        [Fact]
        public Task ReturnEmptyArrayCSharpAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M1() { return new int[0]; }
                    IEnumerable<int> M2() { return new int[0] { }; }
                    int[] M3() { return new int[0]; }
                }

                """ + _csharpSpecializedCollectionsDefinition,
                GetCSharpEmptyEnumerableResultAt(6, 36),
                GetCSharpEmptyEnumerableResultAt(7, 36));

        [Fact]
        public Task ReturnSingletonArrayCSharpAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M1() { return new int[1]; }
                    IEnumerable<int> M2() { return new int[1] { 1 }; }
                    IEnumerable<int> M3() { return new[] { 1 }; }
                    int[] M4() { return new[] { 1 }; }
                }

                """ + _csharpSpecializedCollectionsDefinition,
                GetCSharpSingletonEnumerableResultAt(6, 36),
                GetCSharpSingletonEnumerableResultAt(7, 36),
                GetCSharpSingletonEnumerableResultAt(8, 36));

        [Fact]
        public Task ReturnLinqEmptyEnumerableCSharpAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

                using System.Collections.Generic;
                using System.Linq;

                class C
                {
                    IEnumerable<int> M1() { return Enumerable.Empty<int>(); }
                }

                """ + _csharpSpecializedCollectionsDefinition,
                GetCSharpEmptyEnumerableResultAt(7, 36));

        [Fact(Skip = "855425")]
        public Task ReturnArrayWithinExpressionCSharpAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M1() { return 0 == 1 ? new[] { 1 } : new[] { 2 }; }
                    IEnumerable<int> M2() { return null ?? new int[0]; }
                }

                """ + _csharpSpecializedCollectionsDefinition,
                GetCSharpSingletonEnumerableResultAt(6, 45),
                GetCSharpSingletonEnumerableResultAt(6, 59),
                GetCSharpEmptyEnumerableResultAt(7, 44));

        [Fact]
        public Task ReturnLinqEmptyEnumerableWithinExpressionCSharpAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

                using System.Collections.Generic;
                using System.Linq;

                class C
                {
                    IEnumerable<int> M1() { return 0 == 1 ? Enumerable.Empty<int>() : null; }
                    IEnumerable<int> M2() { return null ?? Enumerable.Empty<int>(); }
                }

                """ + _csharpSpecializedCollectionsDefinition,
                GetCSharpEmptyEnumerableResultAt(7, 45),
                GetCSharpEmptyEnumerableResultAt(8, 44));

        [Fact]
        public Task ReturnMultiElementArrayCSharpAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M1() { return new int[2]; }
                    IEnumerable<int> M2() { return new int[2] { 1, 2 }; }
                    IEnumerable<int> M3() { return new[] { 1, 2 }; }
                    int[] M4() { return new[] { 1, 2 }; }
                }

                """ + _csharpSpecializedCollectionsDefinition);

        [Fact]
        public Task ReturnJaggedArrayCSharpAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int[]> M1() { return new int[2][] { new int[0], new int[0] }; }
                    IEnumerable<int[]> M2() { return new[] { new[] { 1 } }; }
                    IEnumerable<int[]> M3() { return new[] { new[] { 1, 2, 3 }, new[] { 1 } }; }
                }

                """ + _csharpSpecializedCollectionsDefinition,
                GetCSharpSingletonEnumerableResultAt(7, 38));

        [Fact(Skip = "855425")]
        public Task ImplicitConversionToNestedEnumerableCSharpAsync()
            => VerifyCS.VerifyAnalyzerAsync("""

                using System.Collections.Generic;

                class C
                {
                    IEnumerable<IEnumerable<int>> M1() { return new[] { new[] { 1 } }; }
                }

                """ + _csharpSpecializedCollectionsDefinition,
                GetCSharpSingletonEnumerableResultAt(5, 49),
                GetCSharpSingletonEnumerableResultAt(5, 57));

        [Fact]
        public Task ReturnEmptyArrayBasicAsync()
            => VerifyVB.VerifyAnalyzerAsync("""

                Imports System.Collections.Generic

                Class C
                    Function M1() As IEnumerable(Of Integer)
                        Return New Integer(0) {}
                    End Function
                    Function M2() As IEnumerable(Of Integer)
                        Return {}
                    End Function
                End Class

                """ + _basicSpecializedCollectionsDefinition,
            GetBasicEmptyEnumerableResultAt(6, 16),
            GetBasicEmptyEnumerableResultAt(9, 16));

        [Fact]
        public Task ReturnLinqEmptyEnumerableBasicAsync()
            => VerifyVB.VerifyAnalyzerAsync("""

                Imports System.Collections.Generic
                Imports System.Linq

                Class C
                    Function M1() As IEnumerable(Of Integer)
                        Return Enumerable.Empty(Of Integer)()
                    End Function
                End Class

                """ + _basicSpecializedCollectionsDefinition,
                GetBasicEmptyEnumerableResultAt(7, 16));

        [Fact]
        public Task ReturnSingletonArrayBasicAsync()
            => VerifyVB.VerifyAnalyzerAsync("""

                Imports System.Collections.Generic

                Class C
                    Function M1() As IEnumerable(Of Integer)
                        Return New Integer(0) {1}
                    End Function
                    Function M2() As IEnumerable(Of Integer)
                        Return {1}
                    End Function
                End Class

                """ + _basicSpecializedCollectionsDefinition,
                GetBasicSingletonEnumerableResultAt(6, 16),
                GetBasicSingletonEnumerableResultAt(9, 16));

        [Fact(Skip = "855425")]
        public Task ReturnArrayWithinExpressionBasicAsync()
            => VerifyVB.VerifyAnalyzerAsync("""

                Imports System.Collections.Generic

                Class C
                    Function M1() As IEnumerable(Of Integer)
                        Return If(True, {1}, {2})
                    End Function
                    Function M2() As IEnumerable(Of Integer)
                        Return If(True, {1})
                    End Function
                End Class

                """ + _basicSpecializedCollectionsDefinition,
                GetBasicSingletonEnumerableResultAt(6, 25),
                GetBasicSingletonEnumerableResultAt(6, 30),
                GetBasicSingletonEnumerableResultAt(9, 25));

        [Fact]
        public Task ReturnLinqEmptyEnumerableWithinExpressionBasicAsync()
            => VerifyVB.VerifyAnalyzerAsync("""

                Imports System.Collections.Generic
                Imports System.Linq

                Class C
                    Function M1() As IEnumerable(Of Integer)
                        Return If(True, Enumerable.Empty(Of Integer)(), Nothing)
                    End Function
                    Function M2() As IEnumerable(Of Integer)
                        Return If({|BC33107:True|}, Enumerable.Empty(Of Integer)())
                    End Function
                End Class

                """ + _basicSpecializedCollectionsDefinition,
                GetBasicEmptyEnumerableResultAt(7, 25),
                GetBasicEmptyEnumerableResultAt(10, 25));

        [Fact]
        public Task ReturnMultiElementArrayBasicAsync()
            => VerifyVB.VerifyAnalyzerAsync("""

                Imports System.Collections.Generic

                Class C
                    Function M1() As IEnumerable(Of Integer)
                        Return New Integer(1) {1, 2}
                    End Function
                    Function M2() As IEnumerable(Of Integer)
                        Return {1, 2}
                    End Function
                End Class

                """ + _basicSpecializedCollectionsDefinition);

        [Fact]
        public Task ReturnJaggedArrayBasicAsync()
            => VerifyVB.VerifyAnalyzerAsync("""

                Imports System.Collections.Generic

                Class C
                    Function M1() As IEnumerable(Of Integer())
                        Return New Integer(1)() {New Integer() {}, New Integer() {}}
                    End Function
                    Function M2() As IEnumerable(Of Integer())
                        Return {({1})}
                    End Function
                    Function M3() As IEnumerable(Of Integer())
                        Return {({1, 2, 3}), ({1})}
                    End Function
                End Class

                """ + _basicSpecializedCollectionsDefinition,
                GetBasicSingletonEnumerableResultAt(9, 16));

        [Fact(Skip = "855425")]
        public Task ImplicitConversionToNestedEnumerableBasicAsync()
            => VerifyVB.VerifyAnalyzerAsync("""

                Imports System.Collections.Generic

                Class C
                    Function M1() As IEnumerable(Of IEnumerable(Of Integer))
                        Return {({1})}
                    End Function
                End Class

                """ + _basicSpecializedCollectionsDefinition,
                GetBasicSingletonEnumerableResultAt(6, 16),
                GetBasicSingletonEnumerableResultAt(6, 17));
    }
}
