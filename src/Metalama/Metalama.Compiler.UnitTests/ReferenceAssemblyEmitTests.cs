using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Metalama.Compiler.UnitTests
{
    public class ReferenceAssemblyEmitTests : CSharpTestBase
    {
        private static INamedTypeSymbol EmitAndGetClassC(CompilationUnitSyntax compilationUnit)
        {
            CSharpCompilation comp = CreateCompilation(SyntaxFactory.SyntaxTree(compilationUnit));

            using var stream = new MemoryStream();

            EmitResult emitResult = comp.Emit(stream, options: new EmitOptions(metadataOnly: true, includePrivateMembers: false));
            Assert.True(emitResult.Success);

            var metadataRef = AssemblyMetadata.CreateFromImage(stream.ToArray()).GetReference();

            var compWithMetadata = CreateEmptyCompilation("", references: new[] { MscorlibRef, metadataRef },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            return compWithMetadata.GetMember<INamedTypeSymbol>("C");
        }

        [Fact]
        public void EmitRefAssembly_Fields()
        {
            var compilationUnit = SyntaxFactory.ParseCompilationUnit("""
                public class C
                {
                    private int field1, field2;
                }
                """);

            var field1 = compilationUnit.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            compilationUnit = compilationUnit.ReplaceNode(field1, field1.WithIncludeInReferenceAssemblyAnnotation());

            var c = EmitAndGetClassC(compilationUnit);

            Assert.Equal("System.Int32 C.field1", c.GetMember<IFieldSymbol>("field1").ToTestDisplayString());
            Assert.Null(c.GetMember<IFieldSymbol>("field2"));
        }

        [Fact]
        public void EmitRefAssembly_Methods()
        {
            var compilationUnit = SyntaxFactory.ParseCompilationUnit("""
                public class C
                {
                    private void M1();
                    private void M2();
                }
                """);

            var m1 = compilationUnit.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            compilationUnit = compilationUnit.ReplaceNode(m1, m1.WithIncludeInReferenceAssemblyAnnotation());

            var c = EmitAndGetClassC(compilationUnit);

            Assert.Equal("void C.M1()", c.GetMember<IMethodSymbol>("M1").ToTestDisplayString());
            Assert.Null(c.GetMember<IFieldSymbol>("M2"));
        }

        [Fact]
        public void EmitRefAssembly_PropertyAccessors()
        {
            var compilationUnit = SyntaxFactory.ParseCompilationUnit("""
                public class C
                {
                    private int Property { get; set; }
                }
                """);

            var getAccessor = compilationUnit.DescendantNodes().OfType<AccessorDeclarationSyntax>().First();
            compilationUnit = compilationUnit.ReplaceNode(getAccessor, getAccessor.WithIncludeInReferenceAssemblyAnnotation());

            var c = EmitAndGetClassC(compilationUnit);

            Assert.Equal("System.Int32 C.Property { get; }", c.GetMember<IPropertySymbol>("Property").ToTestDisplayString());
            Assert.Equal("System.Int32 C.Property.get", c.GetMember<IMethodSymbol>("get_Property").ToTestDisplayString());
            Assert.Null(c.GetMember<IFieldSymbol>("set_Property"));
        }

        [Fact]
        public void EmitRefAssembly_FieldLikeEvents()
        {
            var compilationUnit = SyntaxFactory.ParseCompilationUnit("""
                delegate void MyEventHandler();
                public class C
                {
                    private event MyEventHandler Event1, Event2;
                }
                """);

            var event1 = compilationUnit.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            compilationUnit = compilationUnit.ReplaceNode(event1, event1.WithIncludeInReferenceAssemblyAnnotation());

            var c = EmitAndGetClassC(compilationUnit);

            Assert.Equal("MyEventHandler C.Event1", c.GetMember<IFieldSymbol>("Event1").ToTestDisplayString());
            Assert.Null(c.GetMember<IFieldSymbol>("Event2"));
        }

        [Fact]
        public void EmitRefAssembly_Events()
        {
            var compilationUnit = SyntaxFactory.ParseCompilationUnit("""
                delegate void MyEventHandler();
                public class C
                {
                    private event MyEventHandler Event1 { add {} remove {} }
                    private event MyEventHandler Event2 { add {} remove {} }
                }
                """);

            var event1Accessors = compilationUnit.DescendantNodes().OfType<AccessorDeclarationSyntax>().Take(2);
            compilationUnit = compilationUnit.ReplaceNodes(event1Accessors, (a, _) => a.WithIncludeInReferenceAssemblyAnnotation());

            var c = EmitAndGetClassC(compilationUnit);

            Assert.Equal("event MyEventHandler C.Event1", c.GetMember<IEventSymbol>("Event1").ToTestDisplayString());
            // ToTestDisplayString() is not reliable for event accessors
            Assert.NotNull(c.GetMember<IMethodSymbol>("add_Event1"));
            Assert.NotNull(c.GetMember<IMethodSymbol>("remove_Event1"));
            Assert.Null(c.GetMember<IEventSymbol>("Event2"));
            Assert.Null(c.GetMember<IMethodSymbol>("add_Event2"));
            Assert.Null(c.GetMember<IMethodSymbol>("remove_Event2"));
        }
    }
}
