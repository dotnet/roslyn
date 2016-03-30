// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;
using Roslyn.Diagnostics.Analyzers.CSharp;
using Roslyn.Diagnostics.Analyzers.VisualBasic;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Performance
{
    public class SpecializedEnumerableCreationAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        private readonly string _csharpSpecializedCollectionsDefinition = @"
namespace Roslyn.Utilities
{
    public class SpecializedCollections { }
}
";
        private readonly string _basicSpecializedCollectionsDefinition = @"
Namespace Roslyn.Utilities
    Public Class SpecializedCollections
    End Class
End Namespace
";

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpSpecializedEnumerableCreationAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicSpecializedEnumerableCreationAnalyzer();
        }

        [Fact]
        public void ReturnEmptyArrayCSharp()
        {
            VerifyCSharp(@"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M1() { return new int[0]; }
    IEnumerable<int> M2() { return new int[0] { }; }
    int[] M3() { return new int[0]; }
}
" + _csharpSpecializedCollectionsDefinition,
                GetCSharpResultAt(6, 36, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule),
                GetCSharpResultAt(7, 36, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule));
        }

        [Fact]
        public void ReturnSingletonArrayCSharp()
        {
            VerifyCSharp(@"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M1() { return new int[1]; }
    IEnumerable<int> M2() { return new int[1] { 1 }; }
    IEnumerable<int> M3() { return new[] { 1 }; }
    int[] M4() { return new[] { 1 }; }
}
" + _csharpSpecializedCollectionsDefinition,
                GetCSharpResultAt(6, 36, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule),
                GetCSharpResultAt(7, 36, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule),
                GetCSharpResultAt(8, 36, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule));
        }

        [Fact]
        public void ReturnLinqEmptyEnumerableCSharp()
        {
            VerifyCSharp(@"
using System.Collections.Generic;
using System.Linq;

class C
{
    IEnumerable<int> M1() { return Enumerable.Empty<int>(); }
}
" + _csharpSpecializedCollectionsDefinition,
                GetCSharpResultAt(7, 36, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule));
        }

        [Fact(Skip = "855425")]
        public void ReturnArrayWithinExpressionCSharp()
        {
            VerifyCSharp(@"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M1() { return 0 == 1 ? new[] { 1 } : new[] { 2 }; }
    IEnumerable<int> M2() { return null ?? new int[0]; }
}
" + _csharpSpecializedCollectionsDefinition,
                GetCSharpResultAt(6, 45, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule),
                GetCSharpResultAt(6, 59, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule),
                GetCSharpResultAt(7, 44, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule));
        }

        [Fact(Skip = "855425")]
        public void ReturnLinqEmptyEnumerableWithinExpressionCSharp()
        {
            VerifyCSharp(@"
using System.Collections.Generic;
using System.Linq;

class C
{
    IEnumerable<int> M1() { return 0 == 1 ? Enumerable.Empty<int>() : null; }
    IEnumerable<int> M2() { return null ?? Enumerable.Empty<int>(); }
}
" + _csharpSpecializedCollectionsDefinition,
                GetCSharpResultAt(7, 45, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule),
                GetCSharpResultAt(8, 44, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule));
        }

        [Fact]
        public void ReturnMultiElementArrayCSharp()
        {
            VerifyCSharp(@"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M1() { return new int[2]; }
    IEnumerable<int> M2() { return new int[2] { 1, 2 }; }
    IEnumerable<int> M3() { return new[] { 1, 2 }; }
    int[] M4() { return new[] { 1, 2 }; }
}
" + _csharpSpecializedCollectionsDefinition);
        }

        [Fact]
        public void ReturnJaggedArrayCSharp()
        {
            VerifyCSharp(@"
using System.Collections.Generic;

class C
{
    IEnumerable<int[]> M1() { return new int[2][] { new int[0], new int[0] }; }
    IEnumerable<int[]> M2() { return new[] { new[] { 1 } }; }
    IEnumerable<int[]> M3() { return new[] { new[] { 1, 2, 3 }, new[] { 1 } }; }
}
" + _csharpSpecializedCollectionsDefinition,
                GetCSharpResultAt(7, 38, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule));
        }

        [Fact(Skip = "855425")]
        public void ImplicitConversionToNestedEnumerableCSharp()
        {
            VerifyCSharp(@"
using System.Collections.Generic;

class C
{
    IEnumerable<IEnumerable<int>> M1() { return new[] { new[] { 1 } }; }
}
" + _csharpSpecializedCollectionsDefinition,
                GetCSharpResultAt(5, 49, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule),
                GetCSharpResultAt(5, 57, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule));
        }

        [Fact]
        public void ReturnEmptyArrayBasic()
        {
            VerifyBasic(@"
Imports System.Collections.Generic

Class C
    Function M1() As IEnumerable(Of Integer)
        Return New Integer(0) {}
    End Function
    Function M2() As IEnumerable(Of Integer)
        Return {}
    End Function
End Class
" + _basicSpecializedCollectionsDefinition,
            GetBasicResultAt(6, 16, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule),
            GetBasicResultAt(9, 16, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule));
        }

        [Fact]
        public void ReturnLinqEmptyEnumerableBasic()
        {
            VerifyBasic(@"
Imports System.Collections.Generic
Imports System.Linq

Class C
    Function M1() As IEnumerable(Of Integer)
        Return Enumerable.Empty(Of Integer)()
    End Function
End Class
" + _basicSpecializedCollectionsDefinition,
                GetBasicResultAt(7, 16, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule));
        }

        [Fact]
        public void ReturnSingletonArrayBasic()
        {
            VerifyBasic(@"
Imports System.Collections.Generic

Class C
    Function M1() As IEnumerable(Of Integer)
        Return New Integer(1) {1}
    End Function
    Function M2() As IEnumerable(Of Integer)
        Return {1}
    End Function
End Class
" + _basicSpecializedCollectionsDefinition,
                GetBasicResultAt(6, 16, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule),
                GetBasicResultAt(9, 16, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule));
        }

        [Fact(Skip = "855425")]
        public void ReturnArrayWithinExpressionBasic()
        {
            VerifyBasic(@"
Imports System.Collections.Generic

Class C
    Function M1() As IEnumerable(Of Integer)
        Return If(True, {1}, {2})
    End Function
    Function M2() As IEnumerable(Of Integer)
        Return If(True, {1})
    End Function
End Class
" + _basicSpecializedCollectionsDefinition,
                GetBasicResultAt(6, 25, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule),
                GetBasicResultAt(6, 30, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule),
                GetBasicResultAt(9, 25, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule));
        }

        [Fact(Skip = "855425")]
        public void ReturnLinqEmptyEnumerableWithinExpressionBasic()
        {
            VerifyBasic(@"
Imports System.Collections.Generic
Imports System.Linq

Class C
    Function M1() As IEnumerable(Of Integer)
        Return If(True, Enumerable.Empty(Of Integer)(), Nothing)
    End Function
    Function M2() As IEnumerable(Of Integer)
        Return If(True, Enumerable.Empty(Of Integer)())
    End Function
End Class
" + _basicSpecializedCollectionsDefinition,
                GetBasicResultAt(7, 25, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule),
                GetBasicResultAt(10, 25, SpecializedEnumerableCreationAnalyzer.UseEmptyEnumerableRule));
        }

        [Fact]
        public void ReturnMultiElementArrayBasic()
        {
            VerifyBasic(@"
Imports System.Collections.Generic

Class C
    Function M1() As IEnumerable(Of Integer)
        Return New Integer(2) {1, 2}
    End Function
    Function M2() As IEnumerable(Of Integer)
        Return {1, 2}
    End Function
End Class
" + _basicSpecializedCollectionsDefinition);
        }

        [Fact]
        public void ReturnJaggedArrayBasic()
        {
            VerifyBasic(@"
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
" + _basicSpecializedCollectionsDefinition,
                GetBasicResultAt(9, 16, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule));
        }

        [Fact(Skip = "855425")]
        public void ImplicitConversionToNestedEnumerableBasic()
        {
            VerifyBasic(@"
Imports System.Collections.Generic

Class C
    Function M1() As IEnumerable(Of IEnumerable(Of Integer))
        Return {({1})}
    End Function
End Class
" + _basicSpecializedCollectionsDefinition,
                GetBasicResultAt(6, 16, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule),
                GetBasicResultAt(6, 17, SpecializedEnumerableCreationAnalyzer.UseSingletonEnumerableRule));
        }
    }
}
