// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.SigningTestHelpers;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;

[UseExportProvider]
public class SymbolEquivalenceComparerTests
{
    public static readonly CS.CSharpCompilationOptions CSharpDllOptions = new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
    public static readonly CS.CSharpCompilationOptions CSharpSignedDllOptions = new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
        WithCryptoKeyFile(SigningTestHelpers.KeyPairFile).
        WithStrongNameProvider(DefaultDesktopStrongNameProvider);

    [Fact]
    public async Task TestArraysAreEquivalent()
    {
        var csharpCode =
@"class C
{
    int intField1;
    int[] intArrayField1;
    string[] stringArrayField1;
    int[][] intArrayArrayField1;
    int[,] intArrayRank2Field1;
    System.Int32 int32Field1;

    int intField2;
    int[] intArrayField2;
    string[] stringArrayField2;
    int[][] intArrayArrayField2;
    int[,] intArrayRank2Field2;
    System.Int32 int32Field2;
}";

        using var workspace = TestWorkspace.CreateCSharp(csharpCode);
        var type = (ITypeSymbol)(await workspace.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();

        var intField1 = (IFieldSymbol)type.GetMembers("intField1").Single();
        var intArrayField1 = (IFieldSymbol)type.GetMembers("intArrayField1").Single();
        var stringArrayField1 = (IFieldSymbol)type.GetMembers("stringArrayField1").Single();
        var intArrayArrayField1 = (IFieldSymbol)type.GetMembers("intArrayArrayField1").Single();
        var intArrayRank2Field1 = (IFieldSymbol)type.GetMembers("intArrayRank2Field1").Single();
        var int32Field1 = (IFieldSymbol)type.GetMembers("int32Field1").Single();

        var intField2 = (IFieldSymbol)type.GetMembers("intField2").Single();
        var intArrayField2 = (IFieldSymbol)type.GetMembers("intArrayField2").Single();
        var stringArrayField2 = (IFieldSymbol)type.GetMembers("stringArrayField2").Single();
        var intArrayArrayField2 = (IFieldSymbol)type.GetMembers("intArrayArrayField2").Single();
        var intArrayRank2Field2 = (IFieldSymbol)type.GetMembers("intArrayRank2Field2").Single();
        var int32Field2 = (IFieldSymbol)type.GetMembers("int32Field2").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(intField1.Type, intField1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(intField1.Type, intField2.Type));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(intField1.Type),
                     SymbolEquivalenceComparer.Instance.GetHashCode(intField2.Type));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(intArrayField1.Type, intArrayField1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(intArrayField1.Type, intArrayField2.Type));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(intArrayField1.Type),
                     SymbolEquivalenceComparer.Instance.GetHashCode(intArrayField2.Type));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(stringArrayField1.Type, stringArrayField1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(stringArrayField1.Type, stringArrayField2.Type));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(intArrayArrayField1.Type, intArrayArrayField1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(intArrayArrayField1.Type, intArrayArrayField2.Type));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(intArrayRank2Field1.Type, intArrayRank2Field1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(intArrayRank2Field1.Type, intArrayRank2Field2.Type));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(int32Field1.Type, int32Field1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(int32Field1.Type, int32Field2.Type));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(intField1.Type, intArrayField1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(intArrayField1.Type, stringArrayField1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(stringArrayField1.Type, intArrayArrayField1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(intArrayArrayField1.Type, intArrayRank2Field1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(intArrayRank2Field1.Type, int32Field1.Type));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(int32Field1.Type, intField1.Type));
    }

    [Fact]
    public async Task TestArraysInDifferentLanguagesAreEquivalent()
    {
        var csharpCode =
@"class C
{
    int intField1;
    int[] intArrayField1;
    string[] stringArrayField1;
    int[][] intArrayArrayField1;
    int[,] intArrayRank2Field1;
    System.Int32 int32Field1;
}";
        var vbCode =
@"class C
    dim intField1 as Integer;
    dim intArrayField1 as Integer()
    dim stringArrayField1 as String()
    dim intArrayArrayField1 as Integer()()
    dim intArrayRank2Field1 as Integer(,)
    dim int32Field1 as System.Int32
end class";

        using var csharpWorkspace = TestWorkspace.CreateCSharp(csharpCode);
        using var vbWorkspace = TestWorkspace.CreateVisualBasic(vbCode);
        var csharpType = (ITypeSymbol)(await csharpWorkspace.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();
        var vbType = (await vbWorkspace.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();

        var csharpIntField1 = (IFieldSymbol)csharpType.GetMembers("intField1").Single();
        var csharpIntArrayField1 = (IFieldSymbol)csharpType.GetMembers("intArrayField1").Single();
        var csharpStringArrayField1 = (IFieldSymbol)csharpType.GetMembers("stringArrayField1").Single();
        var csharpIntArrayArrayField1 = (IFieldSymbol)csharpType.GetMembers("intArrayArrayField1").Single();
        var csharpIntArrayRank2Field1 = (IFieldSymbol)csharpType.GetMembers("intArrayRank2Field1").Single();
        var csharpInt32Field1 = (IFieldSymbol)csharpType.GetMembers("int32Field1").Single();

        var vbIntField1 = (IFieldSymbol)vbType.GetMembers("intField1").Single();
        var vbIntArrayField1 = (IFieldSymbol)vbType.GetMembers("intArrayField1").Single();
        var vbStringArrayField1 = (IFieldSymbol)vbType.GetMembers("stringArrayField1").Single();
        var vbIntArrayArrayField1 = (IFieldSymbol)vbType.GetMembers("intArrayArrayField1").Single();
        var vbIntArrayRank2Field1 = (IFieldSymbol)vbType.GetMembers("intArrayRank2Field1").Single();
        var vbInt32Field1 = (IFieldSymbol)vbType.GetMembers("int32Field1").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(csharpIntField1.Type, vbIntField1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(csharpIntArrayField1.Type, vbIntArrayField1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(csharpStringArrayField1.Type, vbStringArrayField1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(csharpIntArrayArrayField1.Type, vbIntArrayArrayField1.Type));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(csharpInt32Field1.Type, vbInt32Field1.Type));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpIntField1.Type, vbIntArrayField1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(vbIntArrayField1.Type, csharpStringArrayField1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpStringArrayField1.Type, vbIntArrayArrayField1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(vbIntArrayArrayField1.Type, csharpIntArrayRank2Field1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpIntArrayRank2Field1.Type, vbInt32Field1.Type));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(csharpInt32Field1.Type, vbIntField1.Type));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(vbIntField1.Type, csharpIntArrayField1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpIntArrayField1.Type, vbStringArrayField1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(vbStringArrayField1.Type, csharpIntArrayArrayField1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpIntArrayArrayField1.Type, vbIntArrayRank2Field1.Type));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(vbIntArrayRank2Field1.Type, csharpInt32Field1.Type));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(vbInt32Field1.Type, csharpIntField1.Type));
    }

    [Fact]
    public async Task TestFields()
    {
        var csharpCode1 =
@"class Type1
{
    int field1;
    string field2;
}

class Type2
{
    bool field3;
    short field4;
}";

        var csharpCode2 =
@"class Type1
{
    int field1;
    short field4;
}

class Type2
{
    bool field3;
    string field2;
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type2_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type2").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type2_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type2").Single();

        var field1_v1 = type1_v1.GetMembers("field1").Single();
        var field1_v2 = type1_v2.GetMembers("field1").Single();
        var field2_v1 = type1_v1.GetMembers("field2").Single();
        var field2_v2 = type2_v2.GetMembers("field2").Single();
        var field3_v1 = type2_v1.GetMembers("field3").Single();
        var field3_v2 = type2_v2.GetMembers("field3").Single();
        var field4_v1 = type2_v1.GetMembers("field4").Single();
        var field4_v2 = type1_v2.GetMembers("field4").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(field1_v1, field1_v2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(field1_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(field1_v2));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(field2_v1, field2_v2));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(field3_v1, field3_v2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(field4_v1, field4_v2));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538124")]
    public async Task TestFieldsAcrossLanguages()
    {
        var csharpCode1 =
@"class Type1
{
    int field1;
    string field2;
}

class Type2
{
    bool field3;
    short field4;
}";

        var vbCode1 =
@"class Type1
    dim field1 as Integer;
    dim field4 as Short;
end class

class Type2
    dim field3 as Boolean;
    dim field2 as String;
end class";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateVisualBasic(vbCode1);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type2_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type2").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type2_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type2").Single();

        var field1_v1 = type1_v1.GetMembers("field1").Single();
        var field1_v2 = type1_v2.GetMembers("field1").Single();
        var field2_v1 = type1_v1.GetMembers("field2").Single();
        var field2_v2 = type2_v2.GetMembers("field2").Single();
        var field3_v1 = type2_v1.GetMembers("field3").Single();
        var field3_v2 = type2_v2.GetMembers("field3").Single();
        var field4_v1 = type2_v1.GetMembers("field4").Single();
        var field4_v2 = type1_v2.GetMembers("field4").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(field1_v1, field1_v2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(field2_v1, field2_v2));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(field3_v1, field3_v2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(field4_v1, field4_v2));
    }

    [Fact]
    public async Task TestFieldsInGenericTypes()
    {
        var code =
@"class C<T>
{
    int goo;
    C<int> intInstantiation1;
    C<string> stringInstantiation;
    C<T> instanceInstantiation;
}

class D
{
    C<int> intInstantiation2;
}
";

        using var workspace = TestWorkspace.CreateCSharp(code);
        var typeC = (await workspace.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();
        var typeD = (await workspace.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("D").Single();

        var intInstantiation1 = (IFieldSymbol)typeC.GetMembers("intInstantiation1").Single();
        var stringInstantiation = (IFieldSymbol)typeC.GetMembers("stringInstantiation").Single();
        var instanceInstantiation = (IFieldSymbol)typeC.GetMembers("instanceInstantiation").Single();
        var intInstantiation2 = (IFieldSymbol)typeD.GetMembers("intInstantiation2").Single();

        var goo = typeC.GetMembers("goo").Single();
        var goo_intInstantiation1 = intInstantiation1.Type.GetMembers("goo").Single();
        var goo_stringInstantiation = stringInstantiation.Type.GetMembers("goo").Single();
        var goo_instanceInstantiation = instanceInstantiation.Type.GetMembers("goo").Single();
        var goo_intInstantiation2 = intInstantiation2.Type.GetMembers("goo").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(goo, goo_intInstantiation1));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(goo, goo_intInstantiation2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(goo, goo_stringInstantiation));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(goo_intInstantiation1, goo_stringInstantiation));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(goo, goo_instanceInstantiation));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(goo),
                     SymbolEquivalenceComparer.Instance.GetHashCode(goo_instanceInstantiation));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(goo_intInstantiation1, goo_intInstantiation2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(goo_intInstantiation1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(goo_intInstantiation2));
    }

    [Fact]
    public async Task TestMethodsWithDifferentReturnTypeNotEquivalent()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo() {}
}";

        var csharpCode2 =
@"class Type1
{
    int Goo() {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact]
    public async Task TestMethodsWithDifferentNamesAreNotEquivalent()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo() {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo1() {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo1").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact]
    public async Task TestMethodsWithDifferentAritiesAreNotEquivalent()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo() {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo<T>() {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact]
    public async Task TestMethodsWithDifferentParametersAreNotEquivalent()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo() {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo(int a) {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact]
    public async Task TestMethodsWithDifferentTypeParameters()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo<A>(A a) {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo<B>(B a) {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(method_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(method_v2));
    }

    [Fact]
    public async Task TestMethodsWithSameParameters()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo(int a) {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo(int a) {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(method_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(method_v2));
    }

    [Fact]
    public async Task TestMethodsWithDifferentParameterNames()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo(int a) {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo(int b) {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(method_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(method_v2));
    }

    [Fact]
    public async Task TestMethodsAreEquivalentOutToRef()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo(out int a) {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo(ref int a) {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact]
    public async Task TestMethodsNotEquivalentRemoveOut()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo(out int a) {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo(int a) {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact]
    public async Task TestMethodsAreEquivalentIgnoreParams()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo(params int[] a) {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo(int[] a) {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(method_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(method_v2));
    }

    [Fact]
    public async Task TestMethodsNotEquivalentDifferentParameterTypes()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo(int[] a) {}
}";

        var csharpCode2 =
@"class Type1
{
    void Goo(string[] a) {}
}";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact]
    public async Task TestMethodsAcrossLanguages()
    {
        var csharpCode1 =
@"
using System.Collections.Generic;

class Type1
{
    T Goo<T>(IList<T> list, int a) {}
    void Bar() { }
}";

        var vbCode1 =
@"
Imports System.Collections.Generic

class Type1
    function Goo(of U)(list as IList(of U), a as Integer) as U
    end function
    sub Quux()
    end sub
end class";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateVisualBasic(vbCode1);
        var csharpType1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var vbType1 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var csharpGooMethod = csharpType1.GetMembers("Goo").Single();
        var csharpBarMethod = csharpType1.GetMembers("Bar").Single();
        var vbGooMethod = vbType1.GetMembers("Goo").Single();
        var vbQuuxMethod = vbType1.GetMembers("Quux").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(csharpGooMethod, vbGooMethod));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(csharpGooMethod),
                     SymbolEquivalenceComparer.Instance.GetHashCode(vbGooMethod));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpGooMethod, csharpBarMethod));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpGooMethod, vbQuuxMethod));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpBarMethod, csharpGooMethod));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpBarMethod, vbGooMethod));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpBarMethod, vbQuuxMethod));
    }

    [Fact]
    public async Task TestMethodsInGenericTypesAcrossLanguages()
    {
        var csharpCode1 =
@"
using System.Collections.Generic;

class Type1<X>
{
    T Goo<T>(IList<T> list, X a) {}
    void Bar(X x) { }
}";

        var vbCode1 =
@"
Imports System.Collections.Generic

class Type1(of M)
    function Goo(of U)(list as IList(of U), a as M) as U
    end function
    sub Bar(x as Object)
    end sub
end class";
        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateVisualBasic(vbCode1);
        var csharpType1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var vbType1 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var csharpGooMethod = csharpType1.GetMembers("Goo").Single();
        var csharpBarMethod = csharpType1.GetMembers("Bar").Single();
        var vbGooMethod = vbType1.GetMembers("Goo").Single();
        var vbBarMethod = vbType1.GetMembers("Bar").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(csharpGooMethod, vbGooMethod));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(csharpGooMethod),
                     SymbolEquivalenceComparer.Instance.GetHashCode(vbGooMethod));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpGooMethod, csharpBarMethod));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpGooMethod, vbBarMethod));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpBarMethod, csharpGooMethod));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpBarMethod, vbGooMethod));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(csharpBarMethod, vbBarMethod));
    }

    [Fact]
    public async Task TestObjectAndDynamicAreNotEqualNormally()
    {
        var csharpCode1 =
@"class Type1
{
    object field1;
    dynamic field2;
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var field1_v1 = type1_v1.GetMembers("field1").Single();
        var field2_v1 = type1_v1.GetMembers("field2").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(field1_v1, field2_v1));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(field2_v1, field1_v1));
    }

    [Fact]
    public async Task TestObjectAndDynamicAreEqualInSignatures()
    {
        var csharpCode1 =
@"class Type1
{
    void Goo(object o1) { }
}";

        var csharpCode2 =
@"class Type1
{
    void Goo(dynamic o1) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v2, method_v1));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(method_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(method_v2));
    }

    [Fact]
    public async Task TestUnequalGenericsInSignatures()
    {
        var csharpCode1 =
@"
using System.Collections.Generic;

class Type1
{
    void Goo(IList<int> o1) { }
}";

        var csharpCode2 =
@"
using System.Collections.Generic;

class Type1
{
    void Goo(IList<string> o1) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v2, method_v1));
    }

    [Fact]
    public async Task TestGenericsWithDynamicAndObjectInSignatures()
    {
        var csharpCode1 =
@"
using System.Collections.Generic;

class Type1
{
    void Goo(IList<object> o1) { }
}";

        var csharpCode2 =
@"
using System.Collections.Generic;

class Type1
{
    void Goo(IList<dynamic> o1) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v2, method_v1));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(method_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(method_v2));
    }

    [Fact]
    public async Task TestDynamicAndUnrelatedTypeInSignatures()
    {
        var csharpCode1 =
@"
using System.Collections.Generic;

class Type1
{
    void Goo(dynamic o1) { }
}";

        var csharpCode2 =
@"
using System.Collections.Generic;

class Type1
{
    void Goo(string o1) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v2, method_v1));
    }

    [Fact]
    public async Task TestNamespaces()
    {
        var csharpCode1 =
@"namespace Outer
{
    namespace Inner
    {
        class Type
        {
        }
    }

    class Type
    {
    }
}
";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode1);
        var outer1 = (INamespaceSymbol)(await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetMembers("Outer").Single();
        var outer2 = (INamespaceSymbol)(await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetMembers("Outer").Single();

        var inner1 = (INamespaceSymbol)outer1.GetMembers("Inner").Single();
        var inner2 = (INamespaceSymbol)outer2.GetMembers("Inner").Single();

        var outerType1 = outer1.GetTypeMembers("Type").Single();
        var outerType2 = outer2.GetTypeMembers("Type").Single();

        var innerType1 = inner1.GetTypeMembers("Type").Single();
        var innerType2 = inner2.GetTypeMembers("Type").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(outer1, outer2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(outer1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(outer2));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(inner1, inner2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(inner1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(inner2));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(outerType1, outerType2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(outerType1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(outerType2));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(innerType1, innerType2));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(innerType1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(innerType2));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(outer1, inner1));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(inner1, outerType1));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(outerType1, innerType1));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(innerType1, outer1));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(outer1, inner1.ContainingSymbol));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(outer1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(inner1.ContainingSymbol));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(outer1, innerType1.ContainingSymbol.ContainingSymbol));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(outer1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(innerType1.ContainingSymbol.ContainingSymbol));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(inner1, innerType1.ContainingSymbol));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(inner1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(innerType1.ContainingSymbol));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(outer1, outerType1.ContainingSymbol));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(outer1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(outerType1.ContainingSymbol));
    }

    [Fact]
    public async Task TestNamedTypesEquivalent()
    {
        var csharpCode1 =
@"
class Type1
{
}

class Type2<X>
{
}
";

        var csharpCode2 =
@"
class Type1
{
  void Goo();
}

class Type2<Y>
{
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type2_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type2").Single();
        var type2_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type2").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(type1_v1, type1_v2));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(type1_v2, type1_v1));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(type1_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(type1_v2));

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(type2_v1, type2_v2));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(type2_v2, type2_v1));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(type2_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(type2_v2));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type1_v1, type2_v1));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type2_v1, type1_v1));
    }

    [Fact]
    public async Task TestNamedTypesDifferentIfNameChanges()
    {
        var csharpCode1 =
@"
class Type1
{
}";

        var csharpCode2 =
@"
class Type2
{
  void Goo();
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type2").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type1_v1, type1_v2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type1_v2, type1_v1));
    }

    [Fact]
    public async Task TestNamedTypesDifferentIfTypeKindChanges()
    {
        var csharpCode1 =
@"
struct Type1
{
}";

        var csharpCode2 =
@"
class Type1
{
  void Goo();
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type1_v1, type1_v2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type1_v2, type1_v1));
    }

    [Fact]
    public async Task TestNamedTypesDifferentIfArityChanges()
    {
        var csharpCode1 =
@"
class Type1
{
}";

        var csharpCode2 =
@"
class Type1<T>
{
  void Goo();
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type1_v1, type1_v2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type1_v2, type1_v1));
    }

    [Fact]
    public async Task TestNamedTypesDifferentIfContainerDifferent()
    {
        var csharpCode1 =
@"
class Outer
{
    class Type1
    {
    }
}";

        var csharpCode2 =
@"
class Other
{
    class Type1
    {
        void Goo();
    }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var outer = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Outer").Single();
        var other = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Other").Single();
        var type1_v1 = outer.GetTypeMembers("Type1").Single();
        var type1_v2 = other.GetTypeMembers("Type1").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type1_v1, type1_v2));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(type1_v2, type1_v1));
    }

    [Fact]
    public async Task TestAliasedTypes1()
    {
        var csharpCode1 =
@"
using i = System.Int32;

class Type1
{
    void Goo(i o1) { }
}";

        var csharpCode2 =
@"
class Type1
{
    void Goo(int o1) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("Type1").Single();

        var method_v1 = type1_v1.GetMembers("Goo").Single();
        var method_v2 = type1_v2.GetMembers("Goo").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v2, method_v1));
        Assert.Equal(SymbolEquivalenceComparer.Instance.GetHashCode(method_v1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(method_v2));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/599")]
    public async Task TestRefVersusOut()
    {
        var csharpCode1 =
@"
class C
{
    void M(out int i) { }
}";

        var csharpCode2 =
@"
class C
{
    void M(ref int i) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();

        var method_v1 = type1_v1.GetMembers("M").Single();
        var method_v2 = type1_v2.GetMembers("M").Single();

        var trueComp = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: false, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);
        var falseComp = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: false, tupleNamesMustMatch: false, ignoreNullableAnnotations: false, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);

        Assert.False(trueComp.Equals(method_v1, method_v2));
        Assert.False(trueComp.Equals(method_v2, method_v1));
        // The hashcodes of distinct objects don't have to be distinct.

        Assert.True(falseComp.Equals(method_v1, method_v2));
        Assert.True(falseComp.Equals(method_v2, method_v1));
        Assert.Equal(falseComp.GetHashCode(method_v1),
                     falseComp.GetHashCode(method_v2));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1388780")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1391743")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393352")]
    public async Task TestTuples1()
    {
        var csharpCode1 =
@"
class C
{
    void M((int, int) i) { }
}";

        var csharpCode2 =
@"
class C
{
    void M(int i) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();

        var method_v1 = type1_v1.GetMembers("M").Single();
        var method_v2 = type1_v2.GetMembers("M").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1388780")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1391743")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393352")]
    public async Task TestTuples2()
    {
        var csharpCode1 =
@"
class C
{
    void M((int, int) i) { }
}";

        var csharpCode2 =
@"
class C
{
    void M(System.ValueTuple<int> i) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();

        var method_v1 = type1_v1.GetMembers("M").Single();
        var method_v2 = type1_v2.GetMembers("M").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1388780")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1391743")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393352")]
    public async Task TestTuples3()
    {
        var csharpCode1 =
@"
class C
{
    void M((int, int) i) { }
}";

        var csharpCode2 =
@"
class C
{
    void M(System.ValueTuple<int, int> i) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();

        var method_v1 = type1_v1.GetMembers("M").Single();
        var method_v2 = type1_v2.GetMembers("M").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.True(SymbolEquivalenceComparer.TupleNamesMustMatchInstance.Equals(method_v1, method_v2));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1388780")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1391743")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393352")]
    public async Task TestTuples4()
    {
        var csharpCode1 =
@"
class C
{
    void M((int a, int b) i) { }
}";

        var csharpCode2 =
@"
class C
{
    void M(System.ValueTuple<int, int> i) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();

        var method_v1 = type1_v1.GetMembers("M").Single();
        var method_v2 = type1_v2.GetMembers("M").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.False(SymbolEquivalenceComparer.TupleNamesMustMatchInstance.Equals(method_v1, method_v2));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1388780")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1391743")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393352")]
    public async Task TestTuples5()
    {
        var csharpCode1 =
@"
class C
{
    void M((int a, int b) i) { }
}";

        var csharpCode2 =
@"
class C
{
    void M((int, int) i) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();

        var method_v1 = type1_v1.GetMembers("M").Single();
        var method_v2 = type1_v2.GetMembers("M").Single();

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
        Assert.False(SymbolEquivalenceComparer.TupleNamesMustMatchInstance.Equals(method_v1, method_v2));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56133")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1388780")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1391743")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1393352")]
    public async Task TestTuples6()
    {
        var csharpCode1 =
@"
class C
{
    void M((int a, int b) i) { }
}";

        var csharpCode2 =
@"
class C
{
    void M((int a, int b, int c) i) { }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var type1_v1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();
        var type1_v2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("C").Single();

        var method_v1 = type1_v1.GetMembers("M").Single();
        var method_v2 = type1_v2.GetMembers("M").Single();

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(method_v1, method_v2));
    }

    [Fact]
    public async Task TestNullableBothEnabled()
    {
        // Both are nullable enabled.  So `T?` should not be equal to `T` unless we are ignoring nullability.
        var csharpCode1 =
@"
#nullable enable
class T
{
    string? A;
    string[]? B;
    dynamic? C;
    dynamic?[]? D;
}";

        var csharpCode2 =
@"
#nullable enable
class T
{
    string A;
    string[] B;
    dynamic C;
    dynamic[] D;
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var t1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("T").Single();
        var t2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("T").Single();

        var a1 = ((IFieldSymbol)t1.GetMembers("A").Single()).Type;
        var b1 = ((IFieldSymbol)t1.GetMembers("B").Single()).Type;
        var c1 = ((IFieldSymbol)t1.GetMembers("C").Single()).Type;
        var d1 = ((IFieldSymbol)t1.GetMembers("D").Single()).Type;
        var a2 = ((IFieldSymbol)t2.GetMembers("A").Single()).Type;
        var b2 = ((IFieldSymbol)t2.GetMembers("B").Single()).Type;
        var c2 = ((IFieldSymbol)t2.GetMembers("C").Single()).Type;
        var d2 = ((IFieldSymbol)t2.GetMembers("D").Single()).Type;

        Assert.Equal(NullableAnnotation.Annotated, a1.NullableAnnotation);
        Assert.Equal(NullableAnnotation.NotAnnotated, a2.NullableAnnotation);

        var ignoreComparer = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: true, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);
        var notIgnoreComparer = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: false, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);

        Assert.True(ignoreComparer.Equals(a1, a2));
        Assert.True(ignoreComparer.Equals(b1, b2));
        Assert.True(ignoreComparer.Equals(c1, c2));
        Assert.True(ignoreComparer.Equals(d1, d2));
        Assert.False(notIgnoreComparer.Equals(a1, a2));
        Assert.False(notIgnoreComparer.Equals(b1, b2));
        Assert.False(notIgnoreComparer.Equals(c1, c2));
        Assert.False(notIgnoreComparer.Equals(d1, d2));

        // The hashcodes of distinct objects don't have to be distinct.
        Assert.Equal(ignoreComparer.GetHashCode(a1), ignoreComparer.GetHashCode(a2));
        Assert.Equal(ignoreComparer.GetHashCode(b1), ignoreComparer.GetHashCode(b2));
        Assert.Equal(ignoreComparer.GetHashCode(c1), ignoreComparer.GetHashCode(c2));
        Assert.Equal(ignoreComparer.GetHashCode(d1), ignoreComparer.GetHashCode(d2));
    }

    [Fact]
    public async Task TestNullableDisableVsEnable1()
    {
        // One side is nullable disabled. Since we don't know the nullability, we should consider it equals to the
        // type without nullability.

        var csharpCode1 =
@"
#nullable disable
class T
{
    string A;
    string[] B;
    dynamic C;
    dynamic[] D;
}";

        var csharpCode2 =
@"
#nullable enable
class T
{
    string A;
    string[] B;
    dynamic C;
    dynamic[] D;
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var t1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("T").Single();
        var t2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("T").Single();

        var a1 = ((IFieldSymbol)t1.GetMembers("A").Single()).Type;
        var b1 = ((IFieldSymbol)t1.GetMembers("B").Single()).Type;
        var c1 = ((IFieldSymbol)t1.GetMembers("C").Single()).Type;
        var d1 = ((IFieldSymbol)t1.GetMembers("D").Single()).Type;
        var a2 = ((IFieldSymbol)t2.GetMembers("A").Single()).Type;
        var b2 = ((IFieldSymbol)t2.GetMembers("B").Single()).Type;
        var c2 = ((IFieldSymbol)t2.GetMembers("C").Single()).Type;
        var d2 = ((IFieldSymbol)t2.GetMembers("D").Single()).Type;

        Assert.Equal(NullableAnnotation.None, a1.NullableAnnotation);
        Assert.Equal(NullableAnnotation.NotAnnotated, a2.NullableAnnotation);

        var ignoreComparer = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: true, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);
        var notIgnoreComparer = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: false, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);

        Assert.True(ignoreComparer.Equals(a1, a2));
        Assert.True(ignoreComparer.Equals(b1, b2));
        Assert.True(ignoreComparer.Equals(c1, c2));
        Assert.True(ignoreComparer.Equals(d1, d2));
        Assert.True(notIgnoreComparer.Equals(a1, a2));
        Assert.True(notIgnoreComparer.Equals(b1, b2));
        Assert.True(notIgnoreComparer.Equals(c1, c2));
        Assert.True(notIgnoreComparer.Equals(d1, d2));

        // The hashcodes of distinct objects don't have to be distinct.
        Assert.Equal(ignoreComparer.GetHashCode(a1), ignoreComparer.GetHashCode(a2));
        Assert.Equal(ignoreComparer.GetHashCode(b1), ignoreComparer.GetHashCode(b2));
        Assert.Equal(ignoreComparer.GetHashCode(c1), ignoreComparer.GetHashCode(c2));
        Assert.Equal(ignoreComparer.GetHashCode(d1), ignoreComparer.GetHashCode(d2));
    }

    [Fact]
    public async Task TestNullableDisableVsEnable2()
    {
        // One side is nullable disabled. One side definitely annotated.  Treat as different.

        var csharpCode1 =
@"
#nullable disable
class T
{
    string A;
    string[] B;
    dynamic C;
    dynamic[] D;
}";

        var csharpCode2 =
@"
#nullable enable
class T
{
    string? A;
    string?[] B;
    dynamic? C;
    dynamic?[] D;
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var t1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("T").Single();
        var t2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("T").Single();

        var a1 = ((IFieldSymbol)t1.GetMembers("A").Single()).Type;
        var b1 = ((IFieldSymbol)t1.GetMembers("B").Single()).Type;
        var c1 = ((IFieldSymbol)t1.GetMembers("C").Single()).Type;
        var d1 = ((IFieldSymbol)t1.GetMembers("D").Single()).Type;
        var a2 = ((IFieldSymbol)t2.GetMembers("A").Single()).Type;
        var b2 = ((IFieldSymbol)t2.GetMembers("B").Single()).Type;
        var c2 = ((IFieldSymbol)t2.GetMembers("C").Single()).Type;
        var d2 = ((IFieldSymbol)t2.GetMembers("D").Single()).Type;

        Assert.Equal(NullableAnnotation.None, a1.NullableAnnotation);
        Assert.Equal(NullableAnnotation.Annotated, a2.NullableAnnotation);

        var ignoreComparer = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: true, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);
        var notIgnoreComparer = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: false, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);

        Assert.True(ignoreComparer.Equals(a1, a2));
        Assert.True(ignoreComparer.Equals(b1, b2));
        Assert.True(ignoreComparer.Equals(c1, c2));
        Assert.True(ignoreComparer.Equals(d1, d2));
        Assert.False(notIgnoreComparer.Equals(a1, a2));
        Assert.False(notIgnoreComparer.Equals(b1, b2));
        Assert.False(notIgnoreComparer.Equals(c1, c2));
        Assert.False(notIgnoreComparer.Equals(d1, d2));

        // The hashcodes of distinct objects don't have to be distinct.
        Assert.Equal(ignoreComparer.GetHashCode(a1), ignoreComparer.GetHashCode(a2));
        Assert.Equal(ignoreComparer.GetHashCode(b1), ignoreComparer.GetHashCode(b2));
        Assert.Equal(ignoreComparer.GetHashCode(c1), ignoreComparer.GetHashCode(c2));
        Assert.Equal(ignoreComparer.GetHashCode(d1), ignoreComparer.GetHashCode(d2));
    }

    [Fact]
    public async Task TestNullableDisableVsEnable3()
    {
        // One side is nullable disabled. One side definitely annotated.  Treat as different.

        var csharpCode1 =
@"
#nullable disable
class T
{
    string A;
    string[] B;
    dynamic C;
    dynamic[] D;
}";

        var csharpCode2 =
@"
#nullable enable
class T
{
    string? A;
    string[]? B;
    dynamic? C;
    dynamic[]? D;
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode1);
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode2);
        var t1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("T").Single();
        var t2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetTypeMembers("T").Single();

        var a1 = ((IFieldSymbol)t1.GetMembers("A").Single()).Type;
        var b1 = ((IFieldSymbol)t1.GetMembers("B").Single()).Type;
        var c1 = ((IFieldSymbol)t1.GetMembers("C").Single()).Type;
        var d1 = ((IFieldSymbol)t1.GetMembers("D").Single()).Type;
        var a2 = ((IFieldSymbol)t2.GetMembers("A").Single()).Type;
        var b2 = ((IFieldSymbol)t2.GetMembers("B").Single()).Type;
        var c2 = ((IFieldSymbol)t2.GetMembers("C").Single()).Type;
        var d2 = ((IFieldSymbol)t2.GetMembers("D").Single()).Type;

        Assert.Equal(NullableAnnotation.None, a1.NullableAnnotation);
        Assert.Equal(NullableAnnotation.Annotated, a2.NullableAnnotation);

        var ignoreComparer = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: true, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);
        var notIgnoreComparer = new SymbolEquivalenceComparer(assemblyComparer: null, distinguishRefFromOut: true, tupleNamesMustMatch: false, ignoreNullableAnnotations: false, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);

        Assert.True(ignoreComparer.Equals(a1, a2));
        Assert.True(ignoreComparer.Equals(b1, b2));
        Assert.True(ignoreComparer.Equals(c1, c2));
        Assert.True(ignoreComparer.Equals(d1, d2));
        Assert.False(notIgnoreComparer.Equals(a1, a2));
        Assert.False(notIgnoreComparer.Equals(b1, b2));
        Assert.False(notIgnoreComparer.Equals(c1, c2));
        Assert.False(notIgnoreComparer.Equals(d1, d2));

        // The hashcodes of distinct objects don't have to be distinct.
        Assert.Equal(ignoreComparer.GetHashCode(a1), ignoreComparer.GetHashCode(a2));
        Assert.Equal(ignoreComparer.GetHashCode(b1), ignoreComparer.GetHashCode(b2));
        Assert.Equal(ignoreComparer.GetHashCode(c1), ignoreComparer.GetHashCode(c2));
        Assert.Equal(ignoreComparer.GetHashCode(d1), ignoreComparer.GetHashCode(d2));
    }

    [Fact]
    public async Task TestCSharpReducedExtensionMethodsAreEquivalent()
    {
        var code = @"
class Zed {}

public static class Extensions
{
   public static void NotGeneric(this Zed z, int data) { }
   public static void GenericThis<T>(this T me, int data) where T : Zed { }
   public static void GenericNotThis<T>(this Zed z, T data) { }
   public static void GenericThisAndMore<T,S>(this T me, S data) where T : Zed { }
   public static void GenericThisAndOther<T>(this T me, T data) where T : Zed { } 
}

class Test
{    
    void NotGeneric() 
    {
        Zed z;
        int n;
        z.NotGeneric(n);
    }

    void GenericThis() 
    {
        Zed z;
        int n;
        z.GenericThis(n);
    }

    void GenericNotThis() 
    {
        Zed z;
        int n;
        z.GenericNotThis(n);
    }

    void GenericThisAndMore() 
    {
        Zed z;
        int n;
        z.GenericThisAndMore(n);
    }

    void GenericThisAndOther() 
    {
        Zed z;
        z.GenericThisAndOther(z);
    } 
}
";
        using var workspace1 = TestWorkspace.CreateCSharp(code);
        using var workspace2 = TestWorkspace.CreateCSharp(code);
        var comp1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync());
        var comp2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync());

        TestReducedExtension<CS.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "NotGeneric");
        TestReducedExtension<CS.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "GenericThis");
        TestReducedExtension<CS.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "GenericNotThis");
        TestReducedExtension<CS.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "GenericThisAndMore");
        TestReducedExtension<CS.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "GenericThisAndOther");
    }

    [Fact]
    public async Task TestVisualBasicReducedExtensionMethodsAreEquivalent()
    {
        var code = @"
Imports System.Runtime.CompilerServices

Class Zed
End Class

Module Extensions
   <Extension>
   Public Sub NotGeneric(z As Zed, data As Integer) 
   End Sub

   <Extension>
   Public Sub GenericThis(Of T As Zed)(m As T, data as Integer) 
   End Sub

   <Extension>
   Public Sub GenericNotThis(Of T)(z As Zed, data As T)
   End Sub

   <Extension>
   Public Sub GenericThisAndMore(Of T As Zed, S)(m As T, data As S)
   End Sub

   <Extension>
   Public Sub GenericThisAndOther(Of T As Zed)(m As T, data As T)
   End Sub
End Module

Class Test
    Sub NotGeneric() 
        Dim z As Zed
        Dim n As Integer
        z.NotGeneric(n)
    End Sub

    Sub GenericThis() 
        Dim z As Zed
        Dim n As Integer
        z.GenericThis(n)
    End Sub

    Sub GenericNotThis() 
        Dim z As Zed
        Dim n As Integer
        z.GenericNotThis(n)
    End Sub

    Sub GenericThisAndMore() 
        Dim z As Zed
        Dim n As Integer
        z.GenericThisAndMore(n)
    End Sub

    Sub GenericThisAndOther() 
        Dim z As Zed
        z.GenericThisAndOther(z)
    End Sub
End Class
";
        using var workspace1 = TestWorkspace.CreateVisualBasic(code);
        using var workspace2 = TestWorkspace.CreateVisualBasic(code);
        var comp1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync());
        var comp2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync());

        TestReducedExtension<VB.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "NotGeneric");
        TestReducedExtension<VB.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "GenericThis");
        TestReducedExtension<VB.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "GenericNotThis");
        TestReducedExtension<VB.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "GenericThisAndMore");
        TestReducedExtension<VB.Syntax.InvocationExpressionSyntax>(comp1, comp2, "Test", "GenericThisAndOther");
    }

    [Fact]
    public async Task TestDifferentModules()
    {
        var csharpCode =
@"namespace N
{
    namespace M
    {
    }
}";

        using var workspace1 = TestWorkspace.CreateCSharp(csharpCode, compilationOptions: new CS.CSharpCompilationOptions(OutputKind.NetModule, moduleName: "GooModule"));
        using var workspace2 = TestWorkspace.CreateCSharp(csharpCode, compilationOptions: new CS.CSharpCompilationOptions(OutputKind.NetModule, moduleName: "BarModule"));
        var namespace1 = (await workspace1.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetNamespaceMembers().Single(n => n.Name == "N").GetNamespaceMembers().Single(n => n.Name == "M");
        var namespace2 = (await workspace2.CurrentSolution.Projects.Single().GetCompilationAsync()).GlobalNamespace.GetNamespaceMembers().Single(n => n.Name == "N").GetNamespaceMembers().Single(n => n.Name == "M");

        Assert.True(SymbolEquivalenceComparer.IgnoreAssembliesInstance.Equals(namespace1, namespace2));
        Assert.Equal(SymbolEquivalenceComparer.IgnoreAssembliesInstance.GetHashCode(namespace1),
                     SymbolEquivalenceComparer.IgnoreAssembliesInstance.GetHashCode(namespace2));

        Assert.False(SymbolEquivalenceComparer.Instance.Equals(namespace1, namespace2));
        Assert.NotEqual(SymbolEquivalenceComparer.Instance.GetHashCode(namespace1),
                     SymbolEquivalenceComparer.Instance.GetHashCode(namespace2));
    }

    [Fact]
    public void AssemblyComparer1()
    {
        var references = new[] { NetFramework.mscorlib };

        var source = "public class T {}";
        var sourceV1 = "[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")] public class T {}";
        var sourceV2 = "[assembly: System.Reflection.AssemblyVersion(\"2.0.0.0\")] public class T {}";

        var a1 = (Compilation)CS.CSharpCompilation.Create("a", [CS.SyntaxFactory.ParseSyntaxTree(source)], references, CSharpDllOptions);
        var a2 = (Compilation)CS.CSharpCompilation.Create("a", [CS.SyntaxFactory.ParseSyntaxTree(source)], references, CSharpDllOptions);

        var b1 = (Compilation)CS.CSharpCompilation.Create("b", [CS.SyntaxFactory.ParseSyntaxTree(sourceV1)], references, CSharpSignedDllOptions);
        var b2 = (Compilation)CS.CSharpCompilation.Create("b", [CS.SyntaxFactory.ParseSyntaxTree(sourceV2)], references, CSharpSignedDllOptions);
        var b3 = (Compilation)CS.CSharpCompilation.Create("b", [CS.SyntaxFactory.ParseSyntaxTree(sourceV2)], references, CSharpSignedDllOptions);

        var ta1 = (ITypeSymbol)a1.GlobalNamespace.GetMembers("T").Single();
        var ta2 = (ITypeSymbol)a2.GlobalNamespace.GetMembers("T").Single();
        var tb1 = (ITypeSymbol)b1.GlobalNamespace.GetMembers("T").Single();
        var tb2 = (ITypeSymbol)b2.GlobalNamespace.GetMembers("T").Single();
        var tb3 = (ITypeSymbol)b3.GlobalNamespace.GetMembers("T").Single();

        var identityComparer = new SymbolEquivalenceComparer(AssemblySymbolIdentityComparer.Instance, distinguishRefFromOut: false, tupleNamesMustMatch: false, ignoreNullableAnnotations: false, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);

        // same name:
        Assert.True(SymbolEquivalenceComparer.IgnoreAssembliesInstance.Equals(ta1, ta2));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(ta1, ta2));
        Assert.True(identityComparer.Equals(ta1, ta2));

        // different name:
        Assert.True(SymbolEquivalenceComparer.IgnoreAssembliesInstance.Equals(ta1, tb1));
        Assert.False(SymbolEquivalenceComparer.Instance.Equals(ta1, tb1));
        Assert.False(identityComparer.Equals(ta1, tb1));

        // different identity
        Assert.True(SymbolEquivalenceComparer.IgnoreAssembliesInstance.Equals(tb1, tb2));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(tb1, tb2));
        Assert.False(identityComparer.Equals(tb1, tb2));

        // same identity
        Assert.True(SymbolEquivalenceComparer.IgnoreAssembliesInstance.Equals(tb2, tb3));
        Assert.True(SymbolEquivalenceComparer.Instance.Equals(tb2, tb3));
        Assert.True(identityComparer.Equals(tb2, tb3));
    }

    private sealed class AssemblySymbolIdentityComparer : IEqualityComparer<IAssemblySymbol>
    {
        public static readonly IEqualityComparer<IAssemblySymbol> Instance = new AssemblySymbolIdentityComparer();

        public bool Equals(IAssemblySymbol x, IAssemblySymbol y)
            => x.Identity.Equals(y.Identity);

        public int GetHashCode(IAssemblySymbol obj)
            => obj.Identity.GetHashCode();
    }

    [Fact]
    public void CustomModifiers_Methods1()
    {
        const string ilSource = @"
.class public C
{
  .method public instance int32 [] modopt([mscorlib]System.Int64) F(         // 0
      int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a, 
      int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) b)
  {
      ldnull     
      throw
  }

  .method public instance int32 [] modopt([mscorlib]System.Boolean) F(       // 1
      int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) a, 
      int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) b)
  {
      ldnull     
      throw
 }

  .method public instance int32[] F(                                         // 2
      int32 a, 
      int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) b)
  {
      ldnull     
      throw
  }

  .method public instance int32[] F(                                         // 3
      int32 a, 
      int32 b)
  {
      ldnull     
      throw
  }
}
";
        MetadataReference r1, r2;
        using (var tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource))
        {
            var bytes = File.ReadAllBytes(tempAssembly.Path);
            r1 = MetadataReference.CreateFromImage(bytes);
            r2 = MetadataReference.CreateFromImage(bytes);
        }

        var c1 = (Compilation)CS.CSharpCompilation.Create("comp1", Array.Empty<SyntaxTree>(), [NetFramework.mscorlib, r1]);
        var c2 = (Compilation)CS.CSharpCompilation.Create("comp2", Array.Empty<SyntaxTree>(), [NetFramework.mscorlib, r2]);
        var type1 = (ITypeSymbol)c1.GlobalNamespace.GetMembers("C").Single();
        var type2 = (ITypeSymbol)c2.GlobalNamespace.GetMembers("C").Single();

        var identityComparer = new SymbolEquivalenceComparer(AssemblySymbolIdentityComparer.Instance, distinguishRefFromOut: false, tupleNamesMustMatch: false, ignoreNullableAnnotations: false, objectAndDynamicCompareEqually: true, arrayAndReadOnlySpanCompareEqually: false);

        var f1 = type1.GetMembers("F");
        var f2 = type2.GetMembers("F");

        Assert.True(identityComparer.Equals(f1[0], f2[0]));
        Assert.False(identityComparer.Equals(f1[0], f2[1]));
        Assert.False(identityComparer.Equals(f1[0], f2[2]));
        Assert.False(identityComparer.Equals(f1[0], f2[3]));

        Assert.False(identityComparer.Equals(f1[1], f2[0]));
        Assert.True(identityComparer.Equals(f1[1], f2[1]));
        Assert.False(identityComparer.Equals(f1[1], f2[2]));
        Assert.False(identityComparer.Equals(f1[1], f2[3]));

        Assert.False(identityComparer.Equals(f1[2], f2[0]));
        Assert.False(identityComparer.Equals(f1[2], f2[1]));
        Assert.True(identityComparer.Equals(f1[2], f2[2]));
        Assert.False(identityComparer.Equals(f1[2], f2[3]));

        Assert.False(identityComparer.Equals(f1[3], f2[0]));
        Assert.False(identityComparer.Equals(f1[3], f2[1]));
        Assert.False(identityComparer.Equals(f1[3], f2[2]));
        Assert.True(identityComparer.Equals(f1[3], f2[3]));
    }

    private static void TestReducedExtension<TInvocation>(Compilation comp1, Compilation comp2, string typeName, string methodName)
        where TInvocation : SyntaxNode
    {
        var method1 = GetInvokedSymbol<TInvocation>(comp1, typeName, methodName);
        var method2 = GetInvokedSymbol<TInvocation>(comp2, typeName, methodName);

        Assert.NotNull(method1);
        Assert.Equal(MethodKind.ReducedExtension, method1.MethodKind);

        Assert.NotNull(method2);
        Assert.Equal(MethodKind.ReducedExtension, method2.MethodKind);

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(method1, method2));

        var cfmethod1 = method1.ConstructedFrom;
        var cfmethod2 = method2.ConstructedFrom;

        Assert.True(SymbolEquivalenceComparer.Instance.Equals(cfmethod1, cfmethod2));
    }

    private static IMethodSymbol GetInvokedSymbol<TInvocation>(Compilation compilation, string typeName, string methodName)
        where TInvocation : SyntaxNode
    {
        var type1 = compilation.GlobalNamespace.GetTypeMembers(typeName).Single();
        var method = type1.GetMembers(methodName).Single();
        var method_root = method.DeclaringSyntaxReferences[0].GetSyntax();

        var invocation = method_root.DescendantNodes().OfType<TInvocation>().FirstOrDefault();
        // vb method root is statement, but we need block to find body with invocation
        invocation ??= method_root.Parent.DescendantNodes().OfType<TInvocation>().First();

        var model = compilation.GetSemanticModel(invocation.SyntaxTree);
        var info = model.GetSymbolInfo(invocation);
        return info.Symbol as IMethodSymbol;
    }
}
