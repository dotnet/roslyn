// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    public partial class CodeGenerationTests
    {
        [UseExportProvider]
        public class Shared
        {
            [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public async Task TestSorting()
            {
                var initial = "namespace [|N|] { }";
                var generationSource = @"
using System;

namespace N
{
    public class [|C|]
    {
        private delegate void DAccessA();
        internal delegate void DAccessB();
        protected internal delegate void DAccessC();
        protected delegate void DAccessD();
        public delegate void DAccessE();
        public delegate void DGeneric<T1, T2>(T1 a, T2 b);
        public delegate void DGeneric<T>(T t, int i);

        public class CNotStatic { }
        public static class CStatic { }
        private class CAccessA { }
        internal class CAccessB { }
        protected internal class CAccessC { }
        protected class CAccessD { }
        public class CAccessE { }
        public class CGeneric<T1, T2> { }
        public class CGeneric<T> { }

        private struct SAccessA { }
        internal struct SAccessB { }
        protected internal struct SAccessC { }
        protected struct SAccessD { }
        public struct SAccessE { }
        public struct SGeneric<T1, T2> { }
        public struct SGeneric<T> { }
        public struct SNameB { }
        public struct SNameA { }

        private enum EAccessA { }
        internal enum EAccessB { }
        protected internal enum EAccessC { }
        protected enum EAccessD { }
        public enum EAccessE { }
        public enum ENameB { }
        public enum ENameA { }

        private interface IAccessA { }
        internal interface IAccessB { }
        protected internal interface IAccessC { }
        protected interface IAccessD { }
        public interface IAccessE { }
        public interface IGeneric<T1, T2> { }
        public interface IGeneric<T> { }

        public static C operator !(C c) { return c; }
        public static C operator +(C c) { return c; }

        public void MNotStatic() { }
        public static void MStatic() { }
        private void MAccessA() { }
        internal void MAccessB() { }
        protected internal void MAccessC() { }
        protected void MAccessD() { }
        public void MAccessE() { }
        public void MGeneric<T1, T2>() { }
        public void MGeneric<T>(int param) { }
        public void MGeneric<T>() { }

        public int M2NotStatic() { return 0; }
        public static int M2Static() { return 0; }
        private int M2AccessA() { return 0; }
        internal int M2AccessB() { return 0; }
        protected internal int M2AccessC() { return 0; }
        protected int M2AccessD() { return 0; }
        public int M2AccessE() { return 0; }
        public int M2Generic<T1, T2>() { return 0; }
        public int M2Generic<T>(int param) { return 0; }
        public int M2Generic<T>() { return 0; }

        public int PNotStatic { get { return 0; } }
        public static int PStatic { get { return 0; } }
        private int PAccessA { get { return 0; } }
        internal int PAccessB { get { return 0; } }
        protected internal int PAccessC { get { return 0; } }
        protected int PAccessD { get { return 0; } }
        public int PAccessE { get { return 0; } }

        public int this[int index1, int index2] { get { return 0; } }
        public int this[int index] { get { return 0; } }

        public event Action EFNotStatic;
        public static event Action EFStatic;
        private event Action EFAccessA;
        internal event Action EFAccessB;
        protected event Action EFAccessC;
        protected internal event Action EFAccessD;
        public event Action EFAccessE;

        private C(string s);
        internal C(long l);
        protected C(char c);
        protected internal C(short s);
        public C(int a);
        public C(int a, int b);
        public C();

        public string FNotStatic;
        public static string FStatic;
        public string FNotConst;
        public const string FConst = ""Const, Indeed"";
        private string FAccessA;
        internal string FAccessB;
        protected string FAccessC;
        protected internal string FAccessD;
        public string FAccessE;
    }
}";
                var expected = @"namespace N
{
    public class C
    {
        public const string FConst;
        public static string FStatic;
        public string FNotStatic;
        public string FNotConst;
        public string FAccessE;
        protected string FAccessC;
        protected internal string FAccessD;
        internal string FAccessB;
        private string FAccessA;

        public C();
        public C(int a);
        public C(int a, int b);
        protected C(char c);
        protected internal C(short s);
        internal C(long l);
        private C(string s);

        public int this[int index] { get; }
        public int this[int index1, int index2] { get; }

        public static int PStatic { get; }
        public int PNotStatic { get; }
        public int PAccessE { get; }
        protected int PAccessD { get; }
        protected internal int PAccessC { get; }
        internal int PAccessB { get; }
        private int PAccessA { get; }

        public static event Action EFStatic;
        public event Action EFNotStatic;
        public event Action EFAccessE;
        protected event Action EFAccessC;
        protected internal event Action EFAccessD;
        internal event Action EFAccessB;
        private event Action EFAccessA;

        public static int M2Static();
        public static void MStatic();
        public int M2AccessE();
        public int M2Generic<T1, T2>();
        public int M2Generic<T>(int param);
        public int M2Generic<T>();
        public int M2NotStatic();
        public void MAccessE();
        public void MGeneric<T1, T2>();
        public void MGeneric<T>(int param);
        public void MGeneric<T>();
        public void MNotStatic();
        protected int M2AccessD();
        protected void MAccessD();
        protected internal int M2AccessC();
        protected internal void MAccessC();
        internal int M2AccessB();
        internal void MAccessB();
        private int M2AccessA();
        private void MAccessA();

        public static C operator +(C c);
        public static C operator !(C c);

        public enum EAccessE
        {
        }

        public enum ENameB
        {
        }

        public enum ENameA
        {
        }

        protected enum EAccessD
        {
        }

        protected internal enum EAccessC
        {
        }

        internal enum EAccessB
        {
        }

        private enum EAccessA
        {
        }

        public interface IAccessE
        {
        }

        public interface IGeneric<T1, T2>
        {
        }

        public interface IGeneric<T>
        {
        }

        protected interface IAccessD
        {
        }

        protected internal interface IAccessC
        {
        }

        internal interface IAccessB
        {
        }

        private interface IAccessA
        {
        }

        public struct SAccessE
        {
        }

        public struct SGeneric<T1, T2>
        {
        }

        public struct SGeneric<T>
        {
        }

        public struct SNameB
        {
        }

        public struct SNameA
        {
        }

        protected struct SAccessD
        {
        }

        protected internal struct SAccessC
        {
        }

        internal struct SAccessB
        {
        }

        private struct SAccessA
        {
        }

        public static class CStatic
        {
        }

        public class CNotStatic
        {
        }

        public class CAccessE
        {
        }

        public class CGeneric<T1, T2>
        {
        }

        public class CGeneric<T>
        {
        }

        protected class CAccessD
        {
        }

        protected internal class CAccessC
        {
        }

        internal class CAccessB
        {
        }

        private class CAccessA
        {
        }

        public delegate void DAccessE();

        public delegate void DGeneric<T1, T2>(T1 a, T2 b);

        public delegate void DGeneric<T>(T t, int i);

        protected delegate void DAccessD();

        protected internal delegate void DAccessC();

        internal delegate void DAccessB();

        private delegate void DAccessA();
    }
}";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false),
                    forceLanguage: LanguageNames.CSharp);

                initial = "Namespace [|N|] \n End Namespace";
                expected = @"Namespace N
    Public Class C
        Public Const FConst As String
        Public Shared FStatic As String
        Public FNotStatic As String
        Public FNotConst As String
        Public FAccessE As String
        Protected FAccessC As String
        Protected Friend FAccessD As String
        Friend FAccessB As String
        Private FAccessA As String
        Public Sub New()
        Public Sub New(a As Integer)
        Public Sub New(a As Integer, b As Integer)
        Protected Sub New(c As Char)
        Protected Friend Sub New(s As Short)
        Friend Sub New(l As Long)
        Private Sub New(s As String)
        Public Shared ReadOnly Property PStatic As Integer
        Public ReadOnly Property PNotStatic As Integer
        Public ReadOnly Property PAccessE As Integer
        Default Public ReadOnly Property this[](index1 As Integer, index2 As Integer) As Integer
        Default Public ReadOnly Property this[](index As Integer) As Integer
        Protected ReadOnly Property PAccessD As Integer
        Protected Friend ReadOnly Property PAccessC As Integer
        Friend ReadOnly Property PAccessB As Integer
        Private ReadOnly Property PAccessA As Integer
        Public Shared Event EFStatic As Action
        Public Event EFNotStatic As Action
        Public Event EFAccessE As Action
        Protected Event EFAccessC As Action
        Protected Friend Event EFAccessD As Action
        Friend Event EFAccessB As Action
        Private Event EFAccessA As Action
        Public Shared Sub MStatic()
        Public Sub MNotStatic()
        Public Sub MAccessE()
        Public Sub MGeneric(Of T1, T2)()
        Public Sub MGeneric(Of T)(param As Integer)
        Public Sub MGeneric(Of T)()
        Protected Sub MAccessD()
        Protected Friend Sub MAccessC()
        Friend Sub MAccessB()
        Private Sub MAccessA()
        Public Shared Function M2Static() As Integer
        Public Function M2NotStatic() As Integer
        Public Function M2AccessE() As Integer
        Public Function M2Generic(Of T1, T2)() As Integer
        Public Function M2Generic(Of T)(param As Integer) As Integer
        Public Function M2Generic(Of T)() As Integer
        Protected Function M2AccessD() As Integer
        Protected Friend Function M2AccessC() As Integer
        Friend Function M2AccessB() As Integer
        Private Function M2AccessA() As Integer
        Public Shared Operator +(c As C) As C
        Public Shared Operator Not(c As C) As C

        Public Enum EAccessE
        End Enum

        Public Enum ENameB
        End Enum

        Public Enum ENameA
        End Enum

        Protected Enum EAccessD
        End Enum

        Protected Friend Enum EAccessC
        End Enum

        Friend Enum EAccessB
        End Enum

        Private Enum EAccessA
        End Enum

        Public Interface IAccessE
        End Interface

        Public Interface IGeneric(Of T1, T2)
        End Interface

        Public Interface IGeneric(Of T)
        End Interface

        Protected Interface IAccessD
        End Interface

        Protected Friend Interface IAccessC
        End Interface

        Friend Interface IAccessB
        End Interface

        Private Interface IAccessA
        End Interface

        Public Structure SAccessE
        End Structure

        Public Structure SGeneric(Of T1, T2)
        End Structure

        Public Structure SGeneric(Of T)
        End Structure

        Public Structure SNameB
        End Structure

        Public Structure SNameA
        End Structure

        Protected Structure SAccessD
        End Structure

        Protected Friend Structure SAccessC
        End Structure

        Friend Structure SAccessB
        End Structure

        Private Structure SAccessA
        End Structure

        Public Class CNotStatic
        End Class

        Public Class CStatic
        End Class

        Public Class CAccessE
        End Class

        Public Class CGeneric(Of T1, T2)
        End Class

        Public Class CGeneric(Of T)
        End Class

        Protected Class CAccessD
        End Class

        Protected Friend Class CAccessC
        End Class

        Friend Class CAccessB
        End Class

        Private Class CAccessA
        End Class

        Public Delegate Sub DAccessE()
        Public Delegate Sub DGeneric(Of T1, T2)(a As T1, b As T2)
        Public Delegate Sub DGeneric(Of T)(t As T, i As Integer)
        Protected Delegate Sub DAccessD()
        Protected Friend Delegate Sub DAccessC()
        Friend Delegate Sub DAccessB()
        Private Delegate Sub DAccessA()
    End Class
End Namespace";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false),
                    forceLanguage: LanguageNames.VisualBasic);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public async Task TestSortingDefaultTypeMemberAccessibility1()
            {
                var generationSource = "public class [|C|] { private string B; public string C; }";
                var initial = "public class [|C|] { string A; }";
                var expected = @"public class C {
    public string C;
    string A;
    private string B;
}";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true);

                initial = "public struct [|S|] { string A; }";
                expected = @"public struct S {
    public string C;
    string A;
    private string B;
}";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true);

                initial = "Public Class [|C|] \n Dim A As String \n End Class";
                expected = @"Public Class C
    Public C As String
    Dim A As String
    Private B As String
End Class";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true);

                initial = "Public Module [|M|] \n Dim A As String \n End Module";
                expected = @"Public Module M
    Public C As String
    Dim A As String
    Private B As String
End Module";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true);

                initial = "Public Structure [|S|] \n Dim A As String \n End Structure";
                expected = @"Public Structure S 
 Dim A As String
    Public C As String
    Private B As String
End Structure";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public async Task TestDefaultTypeMemberAccessibility2()
            {
                var codeGenOptionNoBody = new CodeGenerationOptions(generateMethodBodies: false);

                var generationSource = "public class [|C|] { private void B(){} public void C(){}  }";
                var initial = "public interface [|I|] { void A(); }";
                var expected = @"public interface I { void A();
    void B();
    void C();
}";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true, codeGenerationOptions: codeGenOptionNoBody);

                initial = "Public Interface [|I|] \n Sub A() \n End Interface";
                expected = @"Public Interface I 
 Sub A()
    Sub B()
    Sub C()
End Interface";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true, codeGenerationOptions: codeGenOptionNoBody);

                initial = "Public Class [|C|] \n Sub A() \n End Sub \n End Class";
                expected = @"Public Class C 
 Sub A() 
 End Sub

    Public Sub C()
    End Sub

    Private Sub B()
    End Sub
End Class";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true);

                initial = "Public Module [|M|] \n Sub A() \n End Sub \n End Module";
                expected = @"Public Module M 
 Sub A() 
 End Sub

    Public Sub C()
    End Sub

    Private Sub B()
    End Sub
End Module";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public async Task TestDefaultNamespaceMemberAccessibility1()
            {
                var generationSource = "internal class [|B|]{}";
                var initial = "namespace [|N|] { class A{} }";
                var expected = @"namespace N { class A{}

    internal class B
    {
    }
}";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected);

                initial = "Namespace [|N|] \n Class A \n End Class \n End Namespace";
                expected = @"Namespace N 
 Class A 
 End Class

    Friend Class B
    End Class
End Namespace";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public async Task TestDefaultNamespaceMemberAccessibility2()
            {
                var generationSource = "public class [|C|]{}";
                var initial = "namespace [|N|] { class A{} }";
                var expected = "namespace N { public class C { } class A{} }";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected);

                initial = "Namespace [|N|] \n Class A \n End Class \n End Namespace";
                expected = @"Namespace N
    Public Class C
    End Class

    Class A 
 End Class 
 End Namespace";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            public async Task TestDocumentationComment()
            {
                var generationSource = @"
public class [|C|]
{
    /// <summary>When in need, a documented method is a friend, indeed.</summary>
    public C() { }
}";
                var initial = "public class [|C|] { }";
                var expected = @"public class C
{
    /// 
    /// <member name=""M:C.#ctor"">
    ///     <summary>When in need, a documented method is a friend, indeed.</summary>
    /// </member>
    /// 
    public C();
}";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false, generateDocumentationComments: true),
                    onlyGenerateMembers: true);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task TestModifiers()
            {
                var generationSource = @"
namespace [|N|]
{
    public class A 
    {
        public virtual string Property { get { return null; } }
        public static abstract string Property1 { get; }

        public virtual void Method1() {}
        public static abstract void Method2() {}
    }

    public class C
    {
        public sealed override string Property { get { return null; } }
        public sealed override void Method1() {} 
    }
}";

                var initial = "namespace [|N|] { }";
                var expected = @"namespace N {
    namespace N
    {
        public class A
        {
            public static abstract string Property1 { get; }
            public virtual string Property { get; }

            public abstract static void Method2();
            public virtual void Method1();
        }

        public class C
        {
            public sealed override string Property { get; }

            public sealed override void Method1();
        }
    }
}";
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));

                var initialVB = "Namespace [|N|] End Namespace";
                var expectedVB = @"Namespace N End NamespaceNamespace N
        Public Class A
            Public Shared MustOverride ReadOnly Property Property1 As String
            Public Overridable ReadOnly Property [Property] As String
            Public MustOverride Shared Sub Method2()
            Public Overridable Sub Method1()
        End Class

        Public Class C
            Public Overrides NotOverridable ReadOnly Property [Property] As String
            Public NotOverridable Overrides Sub Method1()
        End Class
    End Namespace";
                await TestGenerateFromSourceSymbolAsync(generationSource, initialVB, expectedVB,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }
        }
    }
}
