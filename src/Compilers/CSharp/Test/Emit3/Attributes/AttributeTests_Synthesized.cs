// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// NYI: PEVerify currently fails for netmodules with error: "The module X was expected to contain an assembly manifest".
    /// Verification was disabled for net modules for now. Add it back once module support has been added.
    /// See tests having verify: !outputKind.IsNetModule()
    /// https://github.com/dotnet/roslyn/issues/23475
    /// </summary>
    public class AttributeTests_Synthesized : WellKnownAttributesTestBase
    {
        #region Theory Data
        public static IEnumerable<object[]> OptimizationLevelTheoryData
        {
            get
            {
                foreach (var level in Enum.GetValues(typeof(OptimizationLevel)))
                {
                    yield return new object[] { level };
                }
            }
        }

        public static IEnumerable<object[]> FullMatrixTheoryData
        {
            get
            {
                foreach (var kind in Enum.GetValues(typeof(OutputKind)))
                {
                    foreach (var level in Enum.GetValues(typeof(OptimizationLevel)))
                    {
                        yield return new object[] { kind, level };
                    }
                }
            }
        }
        #endregion

        #region Helpers
        private void VerifyCompilationRelaxationsAttribute(CSharpAttributeData attribute, bool isSynthesized)
        {
            Assert.Equal("System.Runtime.CompilerServices.CompilationRelaxationsAttribute", attribute.AttributeClass.ToTestDisplayString());
            Assert.Equal("System.Int32", attribute.AttributeConstructor.Parameters.Single().TypeWithAnnotations.ToTestDisplayString());
            Assert.Empty(attribute.CommonNamedArguments);

            int expectedArgValue = isSynthesized ? (int)CompilationRelaxations.NoStringInterning : 0;
            Assert.Equal(1, attribute.CommonConstructorArguments.Length);
            attribute.VerifyValue(0, TypedConstantKind.Primitive, expectedArgValue);
        }

        private void VerifyRuntimeCompatibilityAttribute(CSharpAttributeData attribute, bool isSynthesized)
        {
            Assert.Equal("System.Runtime.CompilerServices.RuntimeCompatibilityAttribute", attribute.AttributeClass.ToTestDisplayString());
            Assert.Empty(attribute.AttributeConstructor.Parameters);
            Assert.Empty(attribute.CommonConstructorArguments);

            if (isSynthesized)
            {
                Assert.Equal(1, attribute.CommonNamedArguments.Length);
                attribute.VerifyNamedArgumentValue<bool>(0, "WrapNonExceptionThrows", TypedConstantKind.Primitive, true);
            }
            else
            {
                Assert.Equal(0, attribute.CommonNamedArguments.Length);
            }
        }

        private void VerifyDebuggableAttribute(CSharpAttributeData attribute, OptimizationLevel optimizations, bool isSynthesized)
        {
            Assert.Equal("System.Diagnostics.DebuggableAttribute", attribute.AttributeClass.ToTestDisplayString());
            Assert.Equal("System.Diagnostics.DebuggableAttribute.DebuggingModes", attribute.AttributeConstructor.Parameters.Single().TypeWithAnnotations.ToTestDisplayString());
            Assert.Empty(attribute.CommonNamedArguments);

            Assert.Equal(1, attribute.CommonConstructorArguments.Length);

            var expectedDebuggingMode = DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints;

            if (isSynthesized && optimizations == OptimizationLevel.Debug)
            {
                expectedDebuggingMode |=
                    DebuggableAttribute.DebuggingModes.Default |
                    DebuggableAttribute.DebuggingModes.DisableOptimizations |
                    DebuggableAttribute.DebuggingModes.EnableEditAndContinue;
            }

            attribute.VerifyValue(0, TypedConstantKind.Enum, (int)expectedDebuggingMode);
        }
        #endregion

        #region CompilerGeneratedAttribute, DebuggerBrowsableAttribute, DebuggerStepThroughAttribute, DebuggerDisplayAttribute
        [Fact]
        [WorkItem(546632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546632")]
        public void PrivateImplementationDetails()
        {
            string source = @"
class C
{
    int[] a = new[] { 1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9,1,2,3,4,5,6,7,8,9, };
}
";
            var reference = CreateCompilation(source).EmitToImageReference();

            var comp = CreateEmptyCompilation("", new[] { reference }, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));

            var pid = (NamedTypeSymbol)comp.GlobalNamespace.GetMembers().Where(s => s.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)).Single();

            var expectedAttrs = new[] { "CompilerGeneratedAttribute" };
            var actualAttrs = GetAttributeNames(pid.GetAttributes());

            AssertEx.SetEqual(expectedAttrs, actualAttrs);
        }

        [Fact]
        [WorkItem(546958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546958")]
        public void FixedSizeBuffers()
        {
            string source = @"
unsafe struct S
{
    public fixed char C[5];
}
";
            var reference = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).EmitToImageReference();
            var comp = CreateEmptyCompilation("", new[] { reference }, options: TestOptions.UnsafeReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));

            var s = (NamedTypeSymbol)comp.GlobalNamespace.GetMembers("S").Single();
            var bufferType = (NamedTypeSymbol)s.GetMembers().Where(t => t.Name == "<C>e__FixedBuffer").Single();

            var expectedAttrs = new[] { "CompilerGeneratedAttribute", "UnsafeValueTypeAttribute" };
            var actualAttrs = GetAttributeNames(bufferType.GetAttributes());

            AssertEx.SetEqual(expectedAttrs, actualAttrs);
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        [WorkItem(546927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546927")]
        public void BackingFields_Property(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System;

class Test
{
    public string MyProp { get; set; }
    public event Func<int> MyEvent;
}
";
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(source, options: options, symbolValidator: module =>
            {
                var peModule = (PEModuleSymbol)module;
                var type = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                var property = type.GetMember<PEFieldSymbol>(GeneratedNames.MakeBackingFieldName("MyProp"));
                Verify(property.Handle);

                var eventField = (PEFieldSymbol)type.GetMember<PEEventSymbol>("MyEvent").AssociatedField;
                Verify(eventField.Handle);

                void Verify(EntityHandle token)
                {
                    var attributes = peModule.GetCustomAttributesForToken(token);

                    if (optimizationLevel == OptimizationLevel.Debug)
                    {
                        Assert.Equal(2, attributes.Length);

                        Assert.Equal("CompilerGeneratedAttribute", attributes[0].AttributeClass.Name);
                        Assert.Equal("DebuggerBrowsableAttribute", attributes[1].AttributeClass.Name);
                        Assert.Equal(DebuggerBrowsableState.Never, (DebuggerBrowsableState)attributes[1].ConstructorArguments.Single().Value);
                    }
                    else
                    {
                        Assert.Equal("CompilerGeneratedAttribute", attributes.Single().AttributeClass.Name);
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        [WorkItem(546927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546927")]
        public void Accessors(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System;

abstract class C
{
    public int P { get; set; }
    public abstract int Q { get; set; }
    public event Func<int> E;
}
";
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(source, options: options, symbolValidator: module =>
            {
                var peModule = (PEModuleSymbol)module;
                var c = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

                var p = c.GetMember<PropertySymbol>("P");
                Assert.Equal("CompilerGeneratedAttribute", peModule.GetCustomAttributesForToken(((PEMethodSymbol)p.GetMethod).Handle).Single().AttributeClass.Name);
                Assert.Equal("CompilerGeneratedAttribute", peModule.GetCustomAttributesForToken(((PEMethodSymbol)p.SetMethod).Handle).Single().AttributeClass.Name);

                // no attributes on abstract property accessors
                var q = c.GetMember<PropertySymbol>("Q");
                Assert.Empty(peModule.GetCustomAttributesForToken(((PEMethodSymbol)q.GetMethod).Handle));
                Assert.Empty(peModule.GetCustomAttributesForToken(((PEMethodSymbol)q.SetMethod).Handle));

                var e = c.GetMember<EventSymbol>("E");
                Assert.Equal("CompilerGeneratedAttribute", peModule.GetCustomAttributesForToken(((PEMethodSymbol)e.AddMethod).Handle).Single().AttributeClass.Name);
                Assert.Equal("CompilerGeneratedAttribute", peModule.GetCustomAttributesForToken(((PEMethodSymbol)e.RemoveMethod).Handle).Single().AttributeClass.Name);
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void Lambdas(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System;

class C
{
    void Goo()
    {
        int a = 1, b = 2;
        Func<int, int, int> d = (x, y) => a*x+b*y; 
    }
}
";
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilation(source, options: options), symbolValidator: m =>
            {
                var displayClass = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<>c__DisplayClass0_0");
                AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(displayClass.GetAttributes()));

                foreach (var member in displayClass.GetMembers())
                {
                    Assert.Equal(0, member.GetAttributes().Length);
                }
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void AnonymousTypes(OptimizationLevel optimizationLevel)
        {
            string source = @"
class C
{
    void Goo()
    {
        var x = new { X = 1, Y = 2 };
    }
}
";
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilation(source, options: options), symbolValidator: m =>
            {
                var anon = m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousType0`2");

                string[] expected;
                if (options.OptimizationLevel == OptimizationLevel.Debug)
                {
                    expected = new[] { "DebuggerDisplayAttribute", "CompilerGeneratedAttribute" };
                }
                else
                {
                    expected = new[] { "CompilerGeneratedAttribute" };
                }

                AssertEx.SetEqual(expected, GetAttributeNames(anon.GetAttributes()));

                foreach (var member in anon.GetMembers())
                {
                    var actual = GetAttributeNames(member.GetAttributes());

                    switch (member.Name)
                    {
                        case "<X>i__Field":
                        case "<Y>i__Field":
                            expected = new[] { "DebuggerBrowsableAttribute" };
                            break;

                        case ".ctor":
                        case "Equals":
                        case "GetHashCode":
                        case "ToString":
                            expected = new[] { "DebuggerHiddenAttribute" };
                            break;

                        case "X":
                        case "get_X":
                        case "Y":
                        case "get_Y":
                            expected = new string[] { };
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(member.Name);
                    }

                    AssertEx.SetEqual(expected, actual);
                }
            });
        }

        [Fact]
        public void AnonymousTypes_DebuggerDisplay()
        {
            string source = @"
public class C
{
   public void Goo() 
   {
	  var _0 = new { };
	  var _1 = new { X0 = 1 };
	  var _2 = new { X0 = 1, X1 = 1 };
	  var _3 = new { X0 = 1, X1 = 1, X2 = 1 };
	  var _4 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1 };
	  var _5 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1 };
	  var _6 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1 };
	  var _7 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1 };
	  var _8 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1, X7 = 1 };
      var _10 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1, X7 = 1, X8 = 1 };   
      var _11 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1, X7 = 1, X8 = 1, X9 = 1 }; 
      var _12 = new { X0 = 1, X1 = 1, X2 = 1, X3 = 1, X4 = 1, X5 = 1, X6 = 1, X7 = 1, X8 = 1, X9 = 1, X10 = 1 }; 
      var _13 = new { 
	     X10 = 1, X11 = 1, X12 = 1, X13 = 1, X14 = 1, X15 = 1, X16 = 1, X17 = 1,
	     X20 = 1, X21 = 1, X22 = 1, X23 = 1, X24 = 1, X25 = 1, X26 = 1, X27 = 1,
	     X30 = 1, X31 = 1, X32 = 1, X33 = 1, X34 = 1, X35 = 1, X36 = 1, X37 = 1,
	     X40 = 1, X41 = 1, X42 = 1, X43 = 1, X44 = 1, X45 = 1, X46 = 1, X47 = 1,
	     X50 = 1, X51 = 1, X52 = 1, X53 = 1, X54 = 1, X55 = 1, X56 = 1, X57 = 1,
	     X60 = 1, X61 = 1, X62 = 1, X63 = 1, X64 = 1, X65 = 1, X66 = 1, X67 = 1,
      };  
   }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            CompileAndVerify(comp, symbolValidator: m =>
            {
                var assembly = m.ContainingAssembly;
                Assert.Equal(@"\{ }", GetDebuggerDisplayString(assembly, 0, 0));
                Assert.Equal(@"\{ X0 = {X0} }", GetDebuggerDisplayString(assembly, 1, 1));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1} }", GetDebuggerDisplayString(assembly, 2, 2));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2} }", GetDebuggerDisplayString(assembly, 3, 3));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3} }", GetDebuggerDisplayString(assembly, 4, 4));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4} }", GetDebuggerDisplayString(assembly, 5, 5));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5} }", GetDebuggerDisplayString(assembly, 6, 6));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6} }", GetDebuggerDisplayString(assembly, 7, 7));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6}, X7 = {X7} }", GetDebuggerDisplayString(assembly, 8, 8));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6}, X7 = {X7}, X8 = {X8} }", GetDebuggerDisplayString(assembly, 9, 9));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6}, X7 = {X7}, X8 = {X8}, X9 = {X9} }", GetDebuggerDisplayString(assembly, 10, 10));
                Assert.Equal(@"\{ X0 = {X0}, X1 = {X1}, X2 = {X2}, X3 = {X3}, X4 = {X4}, X5 = {X5}, X6 = {X6}, X7 = {X7}, X8 = {X8}, X9 = {X9} ... }", GetDebuggerDisplayString(assembly, 11, 11));

                Assert.Equal(@"\{ X10 = {X10}, X11 = {X11}, X12 = {X12}, X13 = {X13}, X14 = {X14}, X15 = {X15}, X16 = {X16}, X17 = {X17}, X20 = {X20}, X21 = {X21} ... }",
                    GetDebuggerDisplayString(assembly, 12, 48));
            });

            string GetDebuggerDisplayString(AssemblySymbol assembly, int ordinal, int fieldCount)
            {
                NamedTypeSymbol anon;
                if (fieldCount == 0)
                {
                    anon = assembly.GetTypeByMetadataName("<>f__AnonymousType0");
                }
                else
                {
                    anon = assembly.GetTypeByMetadataName("<>f__AnonymousType" + ordinal + "`" + fieldCount);
                }

                var dd = anon.GetAttributes().Where(a => a.AttributeClass.Name == "DebuggerDisplayAttribute").Single();
                return (string)dd.ConstructorArguments.Single().Value;
            }
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void Iterator(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Collections.Generic;

public class C
{
    public IEnumerable<int> Iterator()
    {
        yield return 1;
    }
}
";
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilation(source, options: options), symbolValidator: module =>
            {
                var iter = module.ContainingAssembly.GetTypeByMetadataName("C+<Iterator>d__0");
                AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(iter.GetAttributes()));

                foreach (var member in iter.GetMembers().Where(member => member is MethodSymbol))
                {
                    switch (member.Name)
                    {
                        case ".ctor":
                        case "System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator":
                        case "System.Collections.IEnumerable.GetEnumerator":
                        case "System.Collections.IEnumerator.Reset":
                        case "System.IDisposable.Dispose":
                        case "System.Collections.Generic.IEnumerator<System.Int32>.get_Current":
                        case "System.Collections.IEnumerator.get_Current":
                            AssertEx.SetEqual(new[] { "DebuggerHiddenAttribute" }, GetAttributeNames(member.GetAttributes()));
                            break;

                        case "System.Collections.IEnumerator.Current":
                        case "System.Collections.Generic.IEnumerator<System.Int32>.Current":
                        case "MoveNext":
                            AssertEx.SetEqual(new string[] { }, GetAttributeNames(member.GetAttributes()));
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(member.Name);
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void Async(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    public async Task<int> Goo()
    {
        for (int x = 1; x < 10; x++)
        {
            await Goo();
        }
        
        return 1;
    }
}
";
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilationWithMscorlib461(source, options: options), symbolValidator: module =>
            {
                var goo = module.GlobalNamespace.GetMember<MethodSymbol>("C.Goo");
                AssertEx.SetEqual(options.OptimizationLevel == OptimizationLevel.Debug ?
                                    new[] { "AsyncStateMachineAttribute", "DebuggerStepThroughAttribute" } :
                                    new[] { "AsyncStateMachineAttribute" }, GetAttributeNames(goo.GetAttributes()));

                var iter = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<Goo>d__0");
                AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute" }, GetAttributeNames(iter.GetAttributes()));

                foreach (var member in iter.GetMembers().Where(s => s.Kind == SymbolKind.Method))
                {
                    switch (member.Name)
                    {
                        case ".ctor":
                            break;

                        case "SetStateMachine":
                            AssertEx.SetEqual(new[] { "DebuggerHiddenAttribute" }, GetAttributeNames(member.GetAttributes()));
                            break;

                        case "MoveNext":
                            AssertEx.SetEqual(new string[] { }, GetAttributeNames(member.GetAttributes()));
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(member.Name);
                    }
                }
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        [WorkItem(431, "https://github.com/dotnet/roslyn/issues/431")]
        public void BaseMethodWrapper(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Threading.Tasks;

class A
{
    public virtual async Task<int> GetIntAsync()
    {
        return 42;
    }
}
class B : A
{
    public override async Task<int> GetIntAsync()
    {
        return await base.GetIntAsync();
    }
}
";
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilationWithMscorlib461(source, options: options), symbolValidator: module =>
            {
                var attributes = module.GlobalNamespace.GetTypeMember("B").GetMember<MethodSymbol>("<>n__0").GetAttributes();

                AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute", "DebuggerHiddenAttribute" }, GetAttributeNames(attributes));
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void BaseMethodWrapper_DoNotInheritAttributes(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Threading.Tasks;

class Attr : System.Attribute { }

class A
{
    [Attr]
    [return: Attr]
    public virtual async Task<int> GetIntAsync([Attr] int x)
    {
        return 42;
    }
}
class B : A
{
    public override async Task<int> GetIntAsync(int x)
    {
        return await base.GetIntAsync(x);
    }
}
";
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilationWithMscorlib461(source, options: options), symbolValidator: module =>
            {
                var baseMethodWrapper = module.GlobalNamespace.GetTypeMember("B").GetMember<MethodSymbol>("<>n__0");
                AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute", "DebuggerHiddenAttribute" }, GetAttributeNames(baseMethodWrapper.GetAttributes()));
                Assert.Empty(baseMethodWrapper.GetReturnTypeAttributes());

                var parameter = baseMethodWrapper.Parameters.Single();
                Assert.Empty(parameter.GetAttributes());
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        [WorkItem(38801, "https://github.com/dotnet/roslyn/issues/38801")]
        public void BaseMethodWrapper_DoNotInheritAttributes_TypeParameter(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Threading.Tasks;

class Attr : System.Attribute { }

class A
{
    [Attr]
    [return: Attr]
    public virtual async Task<T> GetAsync<[Attr] T>([Attr] T t)
    {
        return t;
    }
}
class B : A
{
    [Attr]
    [return: Attr]
    public override async Task<T> GetAsync<[Attr] T>([Attr] T t)
    {
        return await base.GetAsync(t);
    }
}
";
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilationWithMscorlib461(source, options: options), symbolValidator: module =>
            {
                var baseMethodWrapper = module.GlobalNamespace.GetTypeMember("B").GetMember<MethodSymbol>("<>n__0");
                AssertEx.SetEqual(new[] { "CompilerGeneratedAttribute", "DebuggerHiddenAttribute" }, GetAttributeNames(baseMethodWrapper.GetAttributes()));
                Assert.Empty(baseMethodWrapper.GetReturnTypeAttributes());

                var parameter = baseMethodWrapper.Parameters.Single();
                Assert.Empty(parameter.GetAttributes());

                var typeParameter = baseMethodWrapper.TypeParameters.Single();
                Assert.Empty(typeParameter.GetAttributes());
            });
        }

        [Fact]
        public void SubstitutedTypeParameter_Attributes()
        {
            string source = @"
class Attr : System.Attribute { }

internal class C1<T1>
{
    internal class C2<[Attr] T2> { }
}
";
            var comp = CreateCompilation(source);
            var c1OfInt = comp.GetTypeByMetadataName("C1`1").Construct(comp.GetSpecialType(SpecialType.System_Int32));

            var c2 = c1OfInt.GetTypeMember("C2");
            var typeParam = c2.TypeParameters.Single();
            Assert.Equal(new[] { "Attr" }, GetAttributeNames(typeParam.GetAttributes()));
        }

        [Fact]
        [WorkItem(46439, "https://github.com/dotnet/roslyn/issues/46439")]
        public void RecordSynthesizedMembers()
        {
            string source = @"
record R
{
    public int MyProperty { get; }
}
";
            CompileAndVerify(source, symbolValidator: validate, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            static void validate(ModuleSymbol module)
            {
                var record = module.GlobalNamespace.GetTypeMember("R");
                Assert.Equal(15, record.GetMembers().Length); // If a new record member is added, extend the test with its behavior regarding CompilerGeneratedAttribute.

                var equalityContractGetter = record.GetMember("get_EqualityContract");
                validateCompilerGeneratedAttribute(equalityContractGetter);

                var toString = record.GetMember(WellKnownMemberNames.ObjectToString);
                validateCompilerGeneratedAttribute(toString);

                var printMembers = record.GetMember(WellKnownMemberNames.PrintMembersMethodName);
                validateCompilerGeneratedAttribute(printMembers);

                var op_Equality = record.GetMember(WellKnownMemberNames.EqualityOperatorName);
                validateCompilerGeneratedAttribute(op_Equality);

                var op_Inequality = record.GetMember(WellKnownMemberNames.InequalityOperatorName);
                validateCompilerGeneratedAttribute(op_Inequality);

                var getHashCode = record.GetMember(WellKnownMemberNames.ObjectGetHashCode);
                validateCompilerGeneratedAttribute(getHashCode);

                var equals = record.GetMembers(WellKnownMemberNames.ObjectEquals);
                Assert.Equal(2, equals.Length);
                validateCompilerGeneratedAttribute(equals[0]);
                validateCompilerGeneratedAttribute(equals[1]);

                var clone = record.GetMember(WellKnownMemberNames.CloneMethodName);
                validateCompilerGeneratedAttribute(clone);

                var ctor = record.GetMembers(WellKnownMemberNames.InstanceConstructorName);
                Assert.Equal(2, ctor.Length);
                Assert.Equal("R..ctor(R original)", ctor[0].ToTestDisplayString());
                validateCompilerGeneratedAttribute(ctor[0]);
                Assert.Equal("R..ctor()", ctor[1].ToTestDisplayString()); // parameterless constructor
                Assert.Empty(ctor[1].GetAttributes()); // shouldn't have attribute.

                var equalityContract = record.GetMember("EqualityContract");
                validateCompilerGeneratedAttribute(equalityContract);

                var myProperty = record.GetMember("MyProperty");
                Assert.Empty(myProperty.GetAttributes());

                var myPropertyGetter = record.GetMember("get_MyProperty");
                validateCompilerGeneratedAttribute(myPropertyGetter);

                var myPropertyBackingField = record.GetMember("<MyProperty>k__BackingField");
                validateCompilerGeneratedAttribute(myPropertyBackingField);
            }

            static void validateCompilerGeneratedAttribute(Symbol symbol)
            {
                var attributeNames = GetAttributeNames(symbol.GetAttributes());
                Assert.Contains("CompilerGeneratedAttribute", attributeNames);
            }
        }

        [Fact]
        [WorkItem(46439, "https://github.com/dotnet/roslyn/issues/46439")]
        public void RecordStructSynthesizedMembers()
        {
            string source = @"
record struct R
{
    public int MyProperty { get; }
}
";
            CompileAndVerify(source, symbolValidator: validate, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            static void validate(ModuleSymbol module)
            {
                var record = module.GlobalNamespace.GetTypeMember("R");
                Assert.Equal(11, record.GetMembers().Length); // If a new record member is added, extend the test with its behavior regarding CompilerGeneratedAttribute.

                var toString = record.GetMember(WellKnownMemberNames.ObjectToString);
                validateCompilerGeneratedAttribute(toString);

                var printMembers = record.GetMember(WellKnownMemberNames.PrintMembersMethodName);
                validateCompilerGeneratedAttribute(printMembers);

                var op_Equality = record.GetMember(WellKnownMemberNames.EqualityOperatorName);
                validateCompilerGeneratedAttribute(op_Equality);

                var op_Inequality = record.GetMember(WellKnownMemberNames.InequalityOperatorName);
                validateCompilerGeneratedAttribute(op_Inequality);

                var getHashCode = record.GetMember(WellKnownMemberNames.ObjectGetHashCode);
                validateCompilerGeneratedAttribute(getHashCode);

                var equals = record.GetMembers(WellKnownMemberNames.ObjectEquals);
                Assert.Equal(2, equals.Length);
                validateCompilerGeneratedAttribute(equals[0]);
                validateCompilerGeneratedAttribute(equals[1]);

                var ctor = record.GetMember(WellKnownMemberNames.InstanceConstructorName);
                Assert.Empty(ctor.GetAttributes());

                var myProperty = record.GetMember("MyProperty");
                Assert.Empty(myProperty.GetAttributes());

                var myPropertyGetter = record.GetMember("get_MyProperty");
                validateCompilerGeneratedAttribute(myPropertyGetter);

                var myPropertyBackingField = record.GetMember("<MyProperty>k__BackingField");
                validateCompilerGeneratedAttribute(myPropertyBackingField);
            }

            static void validateCompilerGeneratedAttribute(Symbol symbol)
            {
                var attributeNames = GetAttributeNames(symbol.GetAttributes());
                Assert.Contains("CompilerGeneratedAttribute", attributeNames);
            }
        }

        [Fact]
        [WorkItem(46439, "https://github.com/dotnet/roslyn/issues/46439")]
        public void RecordSynthesizedMembers_2()
        {
            string source = @"
record R(int P1);

namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}
";
            // [ : R::set_P1] Cannot change initonly field outside its .ctor.
            CompileAndVerify(source,
                symbolValidator: validate,
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                verify: ExecutionConditionUtil.IsCoreClr ? Verification.Passes : Verification.Fails);

            static void validate(ModuleSymbol module)
            {
                var record = module.GlobalNamespace.GetTypeMember("R");
                Assert.Equal(17, record.GetMembers().Length); // If a new record member is added, extend the test with its behavior regarding CompilerGeneratedAttribute.

                var p1_backingField = record.GetMember("<P1>k__BackingField");
                validateCompilerGeneratedAttribute(p1_backingField);

                var equalityContractGetter = record.GetMember("get_EqualityContract");
                validateCompilerGeneratedAttribute(equalityContractGetter);

                var get_P1 = record.GetMember("get_P1");
                validateCompilerGeneratedAttribute(get_P1);

                var set_P1 = record.GetMember("set_P1");
                validateCompilerGeneratedAttribute(set_P1);

                var toString = record.GetMember(WellKnownMemberNames.ObjectToString);
                validateCompilerGeneratedAttribute(toString);

                var printMembers = record.GetMember(WellKnownMemberNames.PrintMembersMethodName);
                validateCompilerGeneratedAttribute(printMembers);

                var op_Equality = record.GetMember(WellKnownMemberNames.EqualityOperatorName);
                validateCompilerGeneratedAttribute(op_Equality);

                var op_Inequality = record.GetMember(WellKnownMemberNames.InequalityOperatorName);
                validateCompilerGeneratedAttribute(op_Inequality);

                var getHashCode = record.GetMember(WellKnownMemberNames.ObjectGetHashCode);
                validateCompilerGeneratedAttribute(getHashCode);

                var equals = record.GetMembers(WellKnownMemberNames.ObjectEquals);
                Assert.Equal(2, equals.Length);
                validateCompilerGeneratedAttribute(equals[0]);
                validateCompilerGeneratedAttribute(equals[1]);

                var clone = record.GetMember(WellKnownMemberNames.CloneMethodName);
                validateCompilerGeneratedAttribute(clone);

                var ctor = record.GetMembers(WellKnownMemberNames.InstanceConstructorName);
                Assert.Equal(2, ctor.Length);
                Assert.Equal("R..ctor(System.Int32 P1)", ctor[0].ToTestDisplayString());
                Assert.Equal("R..ctor(R original)", ctor[1].ToTestDisplayString());
                validateCompilerGeneratedAttribute(ctor[1]);
                Assert.Empty(ctor[0].GetAttributes());

                var deconstruct = record.GetMember(WellKnownMemberNames.DeconstructMethodName);
                validateCompilerGeneratedAttribute(deconstruct);

                var equalityContract = record.GetMember("EqualityContract");
                validateCompilerGeneratedAttribute(equalityContract);

                var p1 = record.GetMember("P1");
                Assert.Empty(p1.GetAttributes());
            }

            static void validateCompilerGeneratedAttribute(Symbol symbol)
            {
                var attributeNames = GetAttributeNames(symbol.GetAttributes());
                Assert.Contains("CompilerGeneratedAttribute", attributeNames);
            }
        }

        [Fact]
        [WorkItem(46439, "https://github.com/dotnet/roslyn/issues/46439")]
        public void RecordStructSynthesizedMembers_2()
        {
            string source = @"
record struct R(int P1);
";
            CompileAndVerify(source, symbolValidator: validate, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            void validate(ModuleSymbol module)
            {
                var record = module.GlobalNamespace.GetTypeMember("R");
                Assert.Equal(14, record.GetMembers().Length); // If a new record member is added, extend the test with its behavior regarding CompilerGeneratedAttribute.

                var p1_backingField = record.GetMember("<P1>k__BackingField");
                validateCompilerGeneratedAttribute(p1_backingField);

                var ctor = record.GetMembers(WellKnownMemberNames.InstanceConstructorName);
                Assert.Equal(2, ctor.Length);
                Assert.Equal("R..ctor()", ctor[0].ToTestDisplayString());
                Assert.Equal("R..ctor(System.Int32 P1)", ctor[1].ToTestDisplayString());
                Assert.Empty(ctor[0].GetAttributes());
                Assert.Empty(ctor[1].GetAttributes());

                var get_P1 = record.GetMember("get_P1");
                validateCompilerGeneratedAttribute(get_P1);

                var set_P1 = record.GetMember("set_P1");
                validateCompilerGeneratedAttribute(set_P1);

                var toString = record.GetMember(WellKnownMemberNames.ObjectToString);
                validateCompilerGeneratedAttribute(toString);

                var printMembers = record.GetMember(WellKnownMemberNames.PrintMembersMethodName);
                validateCompilerGeneratedAttribute(printMembers);

                var op_Equality = record.GetMember(WellKnownMemberNames.EqualityOperatorName);
                validateCompilerGeneratedAttribute(op_Equality);

                var op_Inequality = record.GetMember(WellKnownMemberNames.InequalityOperatorName);
                validateCompilerGeneratedAttribute(op_Inequality);

                var getHashCode = record.GetMember(WellKnownMemberNames.ObjectGetHashCode);
                validateCompilerGeneratedAttribute(getHashCode);

                var equals = record.GetMembers(WellKnownMemberNames.ObjectEquals);
                Assert.Equal(2, equals.Length);
                validateCompilerGeneratedAttribute(equals[0]);
                validateCompilerGeneratedAttribute(equals[1]);

                var deconstruct = record.GetMember(WellKnownMemberNames.DeconstructMethodName);
                validateCompilerGeneratedAttribute(deconstruct);

                var p1 = record.GetMember("P1");
                Assert.Empty(p1.GetAttributes());
            }

            void validateCompilerGeneratedAttribute(Symbol symbol)
            {
                var attributeNames = GetAttributeNames(symbol.GetAttributes());
                Assert.Contains("CompilerGeneratedAttribute", attributeNames);
            }
        }

        [Fact]
        [WorkItem(46439, "https://github.com/dotnet/roslyn/issues/46439")]
        public void AttributeIsMissing()
        {
            string source = @"
record struct R;
";
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute);
            var verifier = CompileAndVerify(comp, symbolValidator: validate);
            verifier.VerifyDiagnostics();

            void validate(ModuleSymbol module)
            {
                var record = module.GlobalNamespace.GetTypeMember("R");
                Assert.Equal(7, record.GetMembers().Length); // If a new record member is added, extend the test with its behavior regarding CompilerGeneratedAttribute.

                var ctor = record.GetMember(WellKnownMemberNames.InstanceConstructorName);
                Assert.Empty(ctor.GetAttributes());

                var toString = record.GetMember(WellKnownMemberNames.ObjectToString);
                Assert.Empty(toString.GetAttributes());

                var op_Equality = record.GetMember(WellKnownMemberNames.EqualityOperatorName);
                Assert.Empty(op_Equality.GetAttributes());

                var op_Inequality = record.GetMember(WellKnownMemberNames.InequalityOperatorName);
                Assert.Empty(op_Inequality.GetAttributes());

                var getHashCode = record.GetMember(WellKnownMemberNames.ObjectGetHashCode);
                Assert.Empty(getHashCode.GetAttributes());

                var equals = record.GetMembers(WellKnownMemberNames.ObjectEquals);
                Assert.Equal(2, equals.Length);
                Assert.Empty(equals[0].GetAttributes());
                Assert.Empty(equals[1].GetAttributes());
            }
        }

        #endregion

        #region CompilationRelaxationsAttribute, RuntimeCompatibilityAttribute, DebuggableAttribute
        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void SynthesizedAllAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var source = @"
public class Test
{
    public static void Main()
    {
    }
}";
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(3, attributes.Length);

                    VerifyCompilationRelaxationsAttribute(attributes[0], isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(attributes[1], isSynthesized: true);
                    VerifyDebuggableAttribute(attributes[2], options.OptimizationLevel, isSynthesized: true);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void AppliedCompilationRelaxations(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var source = @"
using System.Runtime.CompilerServices;

[assembly: CompilationRelaxationsAttribute(0)]

public class Test
{
    public static void Main()
    {
    }
}";
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(3, attributes.Length);

                    VerifyRuntimeCompatibilityAttribute(attributes[0], isSynthesized: true);
                    VerifyDebuggableAttribute(attributes[1], options.OptimizationLevel, isSynthesized: true);
                    VerifyCompilationRelaxationsAttribute(attributes[2], isSynthesized: false);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void AppliedRuntimeCompatibility(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var source = @"
using System.Runtime.CompilerServices;

[assembly: RuntimeCompatibilityAttribute()]

public class Test
{
    public static void Main()
    {
    }
}";
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(3, attributes.Length);

                    VerifyCompilationRelaxationsAttribute(attributes[0], isSynthesized: true);
                    VerifyDebuggableAttribute(attributes[1], options.OptimizationLevel, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(attributes[2], isSynthesized: false);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void AppliedDebuggable(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var source = @"
using System.Diagnostics;

[assembly: DebuggableAttribute(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]

public class Test
{
    public static void Main()
    {
    }
}";
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(3, attributes.Length);

                    VerifyCompilationRelaxationsAttribute(attributes[0], isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(attributes[1], isSynthesized: true);
                    VerifyDebuggableAttribute(attributes[2], options.OptimizationLevel, isSynthesized: false);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void AppliedDebuggableOnBothAssemblyAndModule(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var source = @"
using System.Diagnostics;

[module: DebuggableAttribute(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: DebuggableAttribute(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]

public class Test
{
    public static void Main()
    {
    }
}";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, parseOptions: parseOptions, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                VerifyDebuggableAttribute(module.GetAttributes().Single(), optimizationLevel, isSynthesized: false);

                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(3, attributes.Length);

                    VerifyCompilationRelaxationsAttribute(attributes[0], isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(attributes[1], isSynthesized: true);
                    VerifyDebuggableAttribute(attributes[2], options.OptimizationLevel, isSynthesized: false);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void AppliedCompilationRelaxationsAndRuntimeCompatibility(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var source = @"
using System.Runtime.CompilerServices;

[assembly: CompilationRelaxationsAttribute(0)]
[assembly: RuntimeCompatibilityAttribute()]

public class Test
{
    public static void Main()
    {
    }
}";
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(3, attributes.Length);

                    VerifyDebuggableAttribute(attributes[0], options.OptimizationLevel, isSynthesized: true);
                    VerifyCompilationRelaxationsAttribute(attributes[1], isSynthesized: false);
                    VerifyRuntimeCompatibilityAttribute(attributes[2], isSynthesized: false);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void ModuleCompilationRelaxationsDoNotSuppressAssemblyAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var source = @"
using System.Runtime.CompilerServices;

[module: CompilationRelaxationsAttribute(0)]

public class Test
{
    public static void Main()
    {
    }
}";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, parseOptions: parseOptions, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                VerifyCompilationRelaxationsAttribute(module.GetAttributes().Single(), isSynthesized: false);

                var assemblyAttributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, assemblyAttributes.Length);
                }
                else
                {
                    Assert.Equal(3, assemblyAttributes.Length);

                    VerifyCompilationRelaxationsAttribute(assemblyAttributes[0], isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(assemblyAttributes[1], isSynthesized: true);
                    VerifyDebuggableAttribute(assemblyAttributes[2], options.OptimizationLevel, isSynthesized: true);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void ModuleDebuggableDoNotSuppressAssemblyAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var source = @"
using System.Diagnostics;

[module: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]

public class Test
{
    public static void Main()
    {
    }
}";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, parseOptions: parseOptions, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                VerifyDebuggableAttribute(module.GetAttributes().Single(), options.OptimizationLevel, isSynthesized: false);

                var assemblyAttributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, assemblyAttributes.Length);
                }
                else
                {
                    Assert.Equal(3, assemblyAttributes.Length);

                    VerifyCompilationRelaxationsAttribute(assemblyAttributes[0], isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(assemblyAttributes[1], isSynthesized: true);
                    VerifyDebuggableAttribute(assemblyAttributes[2], options.OptimizationLevel, isSynthesized: true);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void MissingWellKnownAttributesNoDiagnosticsAndNoSynthesizedAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            var compilation = CreateEmptyCompilation("", parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: options);

            if (outputKind.IsApplication())
            {
                compilation.VerifyDiagnostics(
                    // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                    Diagnostic(ErrorCode.ERR_NoEntryPoint));
            }
            else
            {
                // ILVerify: Failed to load type 'System.String' from assembly
                // ILVerify: The format of a DLL or executable being loaded is invalid
                var verify = outputKind.IsNetModule()
                    ? Verification.Fails
                    : Verification.FailsILVerify;

                CompileAndVerify(compilation, verify: verify, symbolValidator: module =>
                {
                    var assemblyAttributes = module.ContainingAssembly.GetAttributes();
                    Assert.Equal(0, assemblyAttributes.Length);
                });
            }
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void MissingWellKnownAttributeEnumsNoDiagnosticsAndNoSynthesizedAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var code = @"
namespace System.Diagnostics
{
    public sealed class DebuggableAttribute: Attribute
    {
        public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled) {}
    }
}
public class Test
{
    public static void Main()
    {
    }
}";

            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            var compilation = CreateCompilation(code, options: options);

            CompileAndVerify(compilation, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(2, attributes.Length);

                    VerifyCompilationRelaxationsAttribute(attributes[0], isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(attributes[1], isSynthesized: true);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void InaccessibleWellKnownAttributeEnumsNoDiagnosticsAndNoSynthesizedAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var code = @"
namespace System.Diagnostics
{
    public sealed class DebuggableAttribute: Attribute
    {
        public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled) {}

        private enum DebuggingModes
        {
            None = 0,
            Default = 1,
            IgnoreSymbolStoreSequencePoints = 2,
            EnableEditAndContinue = 4,
            DisableOptimizations = 256,
        }
    }
}
public class Test
{
    public static void Main()
    {
    }
}";

            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            var compilation = CreateCompilation(code, options: options);

            CompileAndVerify(compilation, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(2, attributes.Length);

                    VerifyCompilationRelaxationsAttribute(attributes[0], isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(attributes[1], isSynthesized: true);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void WellKnownAttributeMissingCtorNoDiagnosticsAndNoSynthesizedAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var code = @"
namespace System.Diagnostics
{
    public sealed class DebuggableAttribute: Attribute
    {
        public enum DebuggingModes
        {
            None = 0,
            Default = 1,
            IgnoreSymbolStoreSequencePoints = 2,
            EnableEditAndContinue = 4,
            DisableOptimizations = 256,
        }
    }
}
public class Test
{
    public static void Main()
    {
    }
}";

            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            var compilation = CreateCompilation(code, options: options);

            CompileAndVerify(compilation, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(2, attributes.Length);

                    VerifyCompilationRelaxationsAttribute(attributes[0], isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(attributes[1], isSynthesized: true);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void WellKnownAttributeInvalidTypeNoDiagnosticsAndNoSynthesizedAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var code = @"
namespace System.Diagnostics
{
    public sealed class DebuggableAttribute: Attribute
    {
        public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled) {}

        public struct DebuggingModes
        {
        }
    }
}
public class Test
{
    public static void Main()
    {
    }
}";

            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            var compilation = CreateCompilation(code, options: options);

            CompileAndVerify(compilation, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(2, attributes.Length);

                    VerifyCompilationRelaxationsAttribute(attributes[0], isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(attributes[1], isSynthesized: true);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void MissingWellKnownAttributeMembersProduceDiagnostics(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    sealed public class CompilationRelaxationsAttribute : System.Attribute
    {
    }

    sealed public class RuntimeCompatibilityAttribute : System.Attribute
    {
        public RuntimeCompatibilityAttribute(int dummy) {}
    }
}
public class Test
{
    public static void Main()
    {
    }
}";
            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            var compilation = CreateCompilation(source, options: options);

            if (outputKind.IsNetModule())
            {
                CompileAndVerify(compilation, verify: Verification.Skipped, symbolValidator: module =>
                {
                    var assemblyAttributes = module.ContainingAssembly.GetAttributes();
                    Assert.Equal(0, assemblyAttributes.Length);
                });
            }
            else
            {
                compilation.VerifyDiagnostics(
                    // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor'
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.CompilationRelaxationsAttribute", ".ctor"),
                    // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor'
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.RuntimeCompatibilityAttribute", ".ctor"),
                    // error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeCompatibilityAttribute.WrapNonExceptionThrows'
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.Runtime.CompilerServices.RuntimeCompatibilityAttribute", "WrapNonExceptionThrows"));
            }
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void AppliedCompilationRelaxationsOnModuleSuppressesAssemblyAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var referenceComp = CreateCompilation(@"
using System.Runtime.CompilerServices;

[assembly: CompilationRelaxationsAttribute(0)]
", options: TestOptions.CreateTestOptions(OutputKind.NetModule, optimizationLevel));

            var reference = ModuleMetadata.CreateFromImage(referenceComp.EmitToArray()).GetReference();

            var source = @"

public class Test
{
    public static void Main()
    {
    }
}";

            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, references: new[] { reference }, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(3, attributes.Length);

                    VerifyRuntimeCompatibilityAttribute(attributes[0], isSynthesized: true);
                    VerifyDebuggableAttribute(attributes[1], options.OptimizationLevel, isSynthesized: true);
                    VerifyCompilationRelaxationsAttribute(attributes[2], isSynthesized: false);
                }
            });
        }

        [Theory]
        [MemberData(nameof(FullMatrixTheoryData))]
        public void AppliedRuntimeCompatibilityOnModuleSuppressesAssemblyAttributes(OutputKind outputKind, OptimizationLevel optimizationLevel)
        {
            var referenceComp = CreateCompilation(@"
using System.Runtime.CompilerServices;

[assembly: RuntimeCompatibilityAttribute()]
", options: TestOptions.CreateTestOptions(OutputKind.NetModule, optimizationLevel));

            var reference = ModuleMetadata.CreateFromImage(referenceComp.EmitToArray()).GetReference();

            var source = @"

public class Test
{
    public static void Main()
    {
    }
}";

            var options = TestOptions.CreateTestOptions(outputKind, optimizationLevel);
            CompileAndVerify(source, references: new[] { reference }, options: options, verify: outputKind.IsNetModule() ? Verification.Skipped : Verification.Passes, symbolValidator: module =>
            {
                var attributes = module.ContainingAssembly.GetAttributes();

                if (outputKind.IsNetModule())
                {
                    Assert.Equal(0, attributes.Length);
                }
                else
                {
                    Assert.Equal(3, attributes.Length);

                    VerifyCompilationRelaxationsAttribute(attributes[0], isSynthesized: true);
                    VerifyDebuggableAttribute(attributes[1], options.OptimizationLevel, isSynthesized: true);
                    VerifyRuntimeCompatibilityAttribute(attributes[2], isSynthesized: false);
                }
            });
        }
        #endregion

        #region UnverifiableCode, SecurityPermission
        [Theory]
        [InlineData(OutputKind.DynamicallyLinkedLibrary)]
        [InlineData(OutputKind.NetModule)]
        public void CheckUnsafeAttributes(OutputKind outputKind)
        {
            string source = @"
unsafe class C
{
    public static void Main()
    {
    }
}";

            var compilation = CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.CreateTestOptions(outputKind, OptimizationLevel.Release, allowUnsafe: true));

            //Skipped because PeVerify fails to run with "The module  was expected to contain an assembly manifest."
            CompileAndVerify(compilation, verify: Verification.Skipped, symbolValidator: module =>
            {
                var unverifiableCode = module.GetAttributes().Single();

                Assert.Equal("System.Security.UnverifiableCodeAttribute", unverifiableCode.AttributeClass.ToTestDisplayString());
                Assert.Empty(unverifiableCode.AttributeConstructor.Parameters);
                Assert.Empty(unverifiableCode.CommonConstructorArguments);
                Assert.Empty(unverifiableCode.CommonNamedArguments);

                if (outputKind.IsNetModule())
                {
                    // Modules security attributes are copied to assemblies they're included in
                    var moduleReference = ModuleMetadata.CreateFromImage(compilation.EmitToArray()).GetReference();
                    CompileAndVerifyWithMscorlib40("", references: new[] { moduleReference }, symbolValidator: validateSecurity, verify: Verification.Skipped);
                }
                else
                {
                    validateSecurity(module);
                }
            });

            void validateSecurity(ModuleSymbol module)
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0015" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0010" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "SkipVerification" + // property name
                        "\u0001", // argument value (true)
                });
            }
        }
        #endregion

        #region AsyncStateMachineAttribute
        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void AsyncStateMachineAttribute_Method(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Threading.Tasks;

class Test
{
    public static async void F()
    {
        await Task.Delay(0);
    }
}";

            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilationWithMscorlib461(source, options: options), symbolValidator: module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var stateMachine = type.GetTypeMember("<F>d__0");
                var asyncMethod = type.GetMember<MethodSymbol>("F");

                var attributes = asyncMethod.GetAttributes();

                var stateMachineAttribute = attributes.First();
                Assert.Equal("AsyncStateMachineAttribute", stateMachineAttribute.AttributeClass.Name);
                Assert.Equal(stateMachine, stateMachineAttribute.ConstructorArguments.Single().ValueInternal);

                if (optimizationLevel == OptimizationLevel.Debug)
                {
                    Assert.Equal(2, attributes.Length);
                    Assert.Equal("DebuggerStepThroughAttribute", attributes.Last().AttributeClass.Name);
                }
                else
                {
                    Assert.Equal(1, attributes.Length);
                }
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void AsyncStateMachineAttribute_Lambda(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System;
using System.Threading.Tasks;

class Test
{
    public static void F()
    {
        Action f = async () => { await Task.Delay(0); };
    }
}";

            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilationWithMscorlib461(source, options: options), symbolValidator: module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test").GetTypeMember("<>c");
                var stateMachine = type.GetTypeMember("<<F>b__0_0>d");
                var asyncMethod = type.GetMember<MethodSymbol>("<F>b__0_0");

                var attributes = asyncMethod.GetAttributes();

                var stateMachineAttribute = attributes.First();
                Assert.Equal("AsyncStateMachineAttribute", stateMachineAttribute.AttributeClass.Name);
                Assert.Equal(stateMachine, stateMachineAttribute.ConstructorArguments.Single().ValueInternal);

                if (optimizationLevel == OptimizationLevel.Debug)
                {
                    Assert.Equal(2, attributes.Length);
                    Assert.Equal("DebuggerStepThroughAttribute", attributes.Last().AttributeClass.Name);
                }
                else
                {
                    Assert.Equal(1, attributes.Length);
                }
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void AsyncStateMachineAttribute_GenericStateMachineClass(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Threading.Tasks;

public class Test<T>
{
    public async void F<U>(U u) where U : Test<int>, new()
    {
        await Task.Delay(0);
    }
}";

            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilationWithMscorlib461(source, options: options), symbolValidator: module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var stateMachine = type.GetTypeMember("<F>d__0");
                var asyncMethod = type.GetMember<MethodSymbol>("F");

                var attributes = asyncMethod.GetAttributes();

                var stateMachineAttribute = attributes.First();
                Assert.Equal("AsyncStateMachineAttribute", stateMachineAttribute.AttributeClass.Name);
                Assert.Equal(stateMachine.AsUnboundGenericType(), stateMachineAttribute.ConstructorArguments.Single().ValueInternal);

                if (optimizationLevel == OptimizationLevel.Debug)
                {
                    Assert.Equal(2, attributes.Length);
                    Assert.Equal("DebuggerStepThroughAttribute", attributes.Last().AttributeClass.Name);
                }
                else
                {
                    Assert.Equal(1, attributes.Length);
                }
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void AsyncStateMachineAttribute_MetadataOnly(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Threading.Tasks;

class Test
{
    public static async void F()
    {
        await Task.Delay(0);
    }
}";

            var referenceOptions = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);
            var reference = CreateCompilationWithMscorlib461(source, options: referenceOptions).EmitToImageReference(options: new EmitOptions(metadataOnly: true));

            var options = TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All);
            var compilation = CreateCompilationWithMscorlib461("", new[] { reference }, options: options);

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(new[] { "F", ".ctor" }, type.GetMembers().SelectAsArray(m => m.Name));

            var asyncMethod = type.GetMember<MethodSymbol>("F");

            if (optimizationLevel == OptimizationLevel.Debug)
            {
                Assert.Equal("DebuggerStepThroughAttribute", asyncMethod.GetAttributes().Single().AttributeClass.Name);
            }
            else
            {
                Assert.Empty(asyncMethod.GetAttributes());
            }
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void AsyncStateMachineAttribute_RuntimeAsync(OptimizationLevel optimizationLevel)
        {
            string source = """
                using System.Threading.Tasks;

                class Test
                {
                    public static async Task F()
                    {
                        await Task.Delay(0);
                    }
                }
                """;

            var parseOptions = CSharpTestBase.WithRuntimeAsync(TestOptions.RegularPreview);
            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All)
                .WithSpecificDiagnosticOptions("SYSLIB5007", ReportDiagnostic.Suppress);

            var compilation = CreateCompilation(source, options: options, parseOptions: parseOptions, targetFramework: TargetFramework.Net100);
            CompileAndVerify(compilation, verify: Verification.Skipped, symbolValidator: module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var asyncMethod = type.GetMember<MethodSymbol>("F");

                var attributes = asyncMethod.GetAttributes();

                // When runtime async is enabled, no state machine is generated,
                // so there should be no AsyncStateMachineAttribute and no DebuggerStepThroughAttribute
                Assert.Empty(attributes);

                // Verify no state machine type was generated
                Assert.Empty(type.GetTypeMembers("<F>d__0"));
            });
        }
        #endregion

        #region IteratorStateMachineAttribute
        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void IteratorStateMachineAttribute_Method(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Collections.Generic;

class Test
{
    public static IEnumerable<int> F()
    {
        yield return 1;
    }
}";

            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilationWithMscorlib461(source, options: options), symbolValidator: module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var stateMachine = type.GetTypeMember("<F>d__0");
                var iteratorMethod = type.GetMember<MethodSymbol>("F");

                var iteratorAttribute = iteratorMethod.GetAttributes().Single();
                Assert.Equal("IteratorStateMachineAttribute", iteratorAttribute.AttributeClass.Name);
                Assert.Equal(stateMachine, iteratorAttribute.ConstructorArguments.Single().ValueInternal);
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void IteratorStateMachineAttribute_GenericStateMachineClass(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Collections.Generic;

public class Test<T>
{
    public IEnumerable<int> F<U>(U u) where U : Test<int>, new()
    {
        yield return 1;
    }
}";

            var options = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);

            CompileAndVerify(CreateCompilationWithMscorlib461(source, options: options), symbolValidator: module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var stateMachine = type.GetTypeMember("<F>d__0");
                var iteratorMethod = type.GetMember<MethodSymbol>("F");

                var iteratorAttribute = iteratorMethod.GetAttributes().Single();
                Assert.Equal("IteratorStateMachineAttribute", iteratorAttribute.AttributeClass.Name);
                Assert.Equal(stateMachine.AsUnboundGenericType(), iteratorAttribute.ConstructorArguments.Single().ValueInternal);
            });
        }

        [Theory]
        [MemberData(nameof(OptimizationLevelTheoryData))]
        public void IteratorStateMachineAttribute_MetadataOnly(OptimizationLevel optimizationLevel)
        {
            string source = @"
using System.Collections.Generic;

public class Test<T>
{
    public IEnumerable<int> F<U>(U u) where U : Test<int>, new()
    {
        yield return 1;
    }
}";

            var referenceOptions = TestOptions.CreateTestOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel)
                .WithMetadataImportOptions(MetadataImportOptions.All);
            var reference = CreateCompilationWithMscorlib461(source, options: referenceOptions).EmitToImageReference(options: new EmitOptions(metadataOnly: true));

            var options = TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All);
            var compilation = CreateCompilationWithMscorlib461("", new[] { reference }, options: options);

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(new[] { "F", ".ctor" }, type.GetMembers().SelectAsArray(m => m.Name));

            Assert.Empty(type.GetMember<MethodSymbol>("F").GetAttributes());
        }
        #endregion

        [Fact, WorkItem(7809, "https://github.com/dotnet/roslyn/issues/7809")]
        public void SynthesizeAttributeWithUseSiteErrorFails()
        {
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();

            #region "mslib"
            var mslibNoString = @"
namespace System
{
    public class Object { }
    public struct Int32 { }
    public class ValueType { }
    public class Attribute { }
    public struct Void { }
}";
            var mslib = mslibNoString + @"
namespace System
{
    public class String { }
}";
            #endregion

            // Build an mscorlib including String
            var mslibComp = CreateEmptyCompilation(new string[] { mslib }, parseOptions: parseOptions).VerifyDiagnostics();
            var mslibRef = mslibComp.EmitToImageReference();

            // Build an mscorlib without String
            var mslibNoStringComp = CreateEmptyCompilation(new string[] { mslibNoString }, parseOptions: parseOptions).VerifyDiagnostics();
            var mslibNoStringRef = mslibNoStringComp.EmitToImageReference();

            var diagLibSource = @"
namespace System.Diagnostics
{
    public class DebuggerDisplayAttribute : System.Attribute
    {
        public DebuggerDisplayAttribute(System.String s) { }
        public System.String Type { get { return null; } set { } }
    }
}
namespace System.Runtime.CompilerServices
{
    public class CompilerGeneratedAttribute { } 
}";
            // Build Diagnostics referencing mscorlib with String
            var diagLibComp = CreateEmptyCompilation(new string[] { diagLibSource }, parseOptions: parseOptions, references: new[] { mslibRef }).VerifyDiagnostics();
            var diagLibRef = diagLibComp.EmitToImageReference();

            // Create compilation using Diagnostics but referencing mscorlib without String
            var comp = CreateEmptyCompilation(new SyntaxTree[] { Parse("", options: parseOptions) }, references: new[] { diagLibRef, mslibNoStringRef });

            // Attribute cannot be synthesized because ctor has a use-site error (String type missing)
            var attribute = comp.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor);
            Assert.Null(attribute);

            // Attribute cannot be synthesized because type in named argument has use-site error (String type missing)
            var attribute2 = comp.TrySynthesizeAttribute(
                                WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor,
                                namedArguments: ImmutableArray.Create(new KeyValuePair<WellKnownMember, TypedConstant>(
                                                    WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__Type,
                                                    new TypedConstant(comp.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, "unused"))));
            Assert.Null(attribute2);
        }
    }
}
