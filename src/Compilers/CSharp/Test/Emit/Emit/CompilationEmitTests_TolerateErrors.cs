// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public partial class CompilationEmitTests_TolerateErrors : EmitMetadataTestBase
    {
        [Fact]
        public void MetadataOnly_TolerateErrors()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class C
{
    public Bad M() { throw null; }
}
", references: new[] { MscorlibRef });

            byte[] mdOnlyImage;
            using (var output = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, options: new EmitOptions(metadataOnly: true, tolerateErrors: true));
                Assert.True(emitResult.Success);

                emitResult.Diagnostics.Verify(
                    // (4,12): error CS0246: The type or namespace name 'Bad' could not be found (are you missing a using directive or an assembly reference?)
                    //     public Bad M() { throw null; }
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bad").WithArguments("Bad").WithLocation(4, 12),
                    // error CS0246: The type or namespace name 'Bad' could not be found (are you missing a using directive or an assembly reference?)
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("Bad").WithLocation(1, 1)
                    );

                mdOnlyImage = output.ToArray();
                Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            }

            var mdOnlyRef = (MetadataImageReference)AssemblyMetadata.CreateFromImage(mdOnlyImage).GetReference(display: "mdOnlyRef");

            // Verify types included in the metadata-only image, and its assembly references (one of them is *the* error assembly)
            var emptyComp = CreateCompilation("", references: new[] { MscorlibRef, mdOnlyRef },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            AssertEx.Equal(
                new[] { "<Module>", "C" },
                emptyComp.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

            var metadataReader = ((AssemblyMetadata)mdOnlyRef.GetMetadataNoCopy()).GetModules()[0].MetadataReader;
            Assert.Equal(new string[] { "mscorlib", "CodeAnalysisError" },
                metadataReader.AssemblyReferences.Select(ar => metadataReader.GetString(metadataReader.GetAssemblyReference(ar).Name)));

            // Load ErrorType, use it without error, and verify its symbol
            var compWithUsage1 = CreateCompilation(@"
class D
{
    void M2<T1>(C c)
    {
        var bad = c.M();
        bad.Missing();
    }
}", references: new[] { MscorlibRef, mdOnlyRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            compWithUsage1.VerifyDiagnostics();
            // PROTOTYPE(tolerate-errors) we need one diagnostic for the entire compilation (when emitting with a reference to error assembly)

            AssertEx.Equal(
                new[] { "Bad[missing] C.M()", "C..ctor()" },
                compWithUsage1.GetMember<NamedTypeSymbol>("C").GetMembers().Select(m => m.ToTestDisplayString()));

            // Verify source symbol for ErrorType
            var sourceMethod = (MethodSymbol)comp.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var sourceErrorType = (ExtendedErrorTypeSymbol)sourceMethod.ReturnType;
            Assert.Equal("Bad", sourceErrorType.ToDisplayString());
            sourceErrorType.ErrorInfo.Verify(
                // error CS0246: The type or namespace name 'Bad' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("Bad").WithLocation(1, 1)
                );
            Assert.True(sourceErrorType.IsErrorType());
            Assert.Equal(0, sourceErrorType.Arity);

            // Verify source symbol for ErrorType
            var peMethod = (PEMethodSymbol)compWithUsage1.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var peErrorType = (MissingMetadataTypeSymbol.TopLevel)peMethod.ReturnType;
            Assert.Equal("Bad", peErrorType.ToDisplayString());
            Assert.Null(peErrorType.ErrorInfo);
            Assert.True(peErrorType.IsErrorType());
            Assert.Equal(0, peErrorType.Arity);

            // Using Bad in client source produces new errors
            var compWithUsage2 = CreateCompilation(@"
class D
{
    void M2<T1>(Bad b, Bad<T1> b1)
    {
        b.ToString();
        b1.ToString();
    }
}", references: new[] { MscorlibRef, mdOnlyRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            compWithUsage2.VerifyDiagnostics(
                // (4,17): error CS0246: The type or namespace name 'Bad' could not be found (are you missing a using directive or an assembly reference?)
                //     void M2<T1>(Bad b, Bad<T1> b1)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bad").WithArguments("Bad").WithLocation(4, 17),
                // (4,24): error CS0246: The type or namespace name 'Bad<>' could not be found (are you missing a using directive or an assembly reference?)
                //     void M2<T1>(Bad b, Bad<T1> b1)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bad<T1>").WithArguments("Bad<>").WithLocation(4, 24)
                );
        }

        [Fact]
        public void MetadataOnly_TolerateErrors_WithQualifiedType()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class C
{
    public Bad.Bad2 M() { throw null; }
}
", references: new[] { MscorlibRef });

            byte[] mdOnlyImage;
            using (var output = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, options: new EmitOptions(metadataOnly: true, tolerateErrors: true));
                Assert.True(emitResult.Success);

                emitResult.Diagnostics.Verify(
                    // (4,12): error CS0246: The type or namespace name 'Bad' could not be found (are you missing a using directive or an assembly reference?)
                    //     public Bad.Bad2 M() { throw null; }
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bad").WithArguments("Bad").WithLocation(4, 12),
                    // error CS0246: The type or namespace name 'Bad' could not be found (are you missing a using directive or an assembly reference?)
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("Bad").WithLocation(1, 1)
                    );

                mdOnlyImage = output.ToArray();
                Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            }

            var mdOnlyRef = (MetadataImageReference)AssemblyMetadata.CreateFromImage(mdOnlyImage).GetReference(display: "mdOnlyRef");

            // Verify types included in the metadata-only image, and its assembly references (one of them is *the* error assembly)
            var compWithRef = CreateCompilation("", references: new[] { MscorlibRef, mdOnlyRef },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            compWithRef.VerifyDiagnostics();

            var metadataReader = ((AssemblyMetadata)mdOnlyRef.GetMetadataNoCopy()).GetModules()[0].MetadataReader;
            Assert.Equal(new string[] { "mscorlib", "CodeAnalysisError" },
                metadataReader.AssemblyReferences.Select(ar => metadataReader.GetString(metadataReader.GetAssemblyReference(ar).Name)));

            AssertEx.Equal(
                new[] { "<Module>", "C" },
                compWithRef.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(
                new[] { "Bad.Bad2[missing] C.M()", "C..ctor()" },
                compWithRef.GetMember<NamedTypeSymbol>("C").GetMembers().Select(m => m.ToTestDisplayString()));

            // Load ErrorType, use it without error, and verify its symbol
            var compWithUsage = CreateCompilation(@"
class D
{
    void M(C c)
    {
        var bad = c.M();
        bad.Missing();
    }
}
", references: new[] { MscorlibRef, mdOnlyRef });
            compWithUsage.VerifyDiagnostics();

            // Verify source symbol for ErrorType
            var sourceMethod = (MethodSymbol)comp.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var sourceErrorType = (ExtendedErrorTypeSymbol)sourceMethod.ReturnType;
            Assert.Equal("Bad.Bad2", sourceErrorType.ToDisplayString());
            Assert.Equal("<global namespace>", sourceErrorType.ContainingNamespace.ToTestDisplayString());
            Assert.Equal("Bad", sourceErrorType.ContainingType.ToTestDisplayString());
            sourceErrorType.ErrorInfo.Verify(
                // error CS0246: The type or namespace name 'Bad' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("Bad").WithLocation(1, 1)
                );

            // Verify PE symbol for ErrorType
            var peMethod = (PEMethodSymbol)compWithUsage.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var peErrorType = (MissingMetadataTypeSymbol.TopLevel)peMethod.ReturnType;
            Assert.Equal("Bad.Bad2", peErrorType.ToDisplayString());
            Assert.Equal("Bad", peErrorType.ContainingNamespace.ToTestDisplayString());
            Assert.Null(peErrorType.ContainingType);
            Assert.Null(peErrorType.ErrorInfo);
        }

        [Fact]
        public void MetadataOnly_TolerateErrors_WithGenericType()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class C<T>
{
    public Bad<T> M() { throw null; }
}
", references: new[] { MscorlibRef });

            byte[] mdOnlyImage;
            using (var output = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, options: new EmitOptions(metadataOnly: true, tolerateErrors: true));
                Assert.True(emitResult.Success);

                emitResult.Diagnostics.Verify(
                    // (4,12): error CS0246: The type or namespace name 'Bad<>' could not be found (are you missing a using directive or an assembly reference?)
                    //     public Bad<T> M() { throw null; }
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bad<T>").WithArguments("Bad<>").WithLocation(4, 12),
                    // error CS0246: The type or namespace name 'Bad<>' could not be found (are you missing a using directive or an assembly reference?)
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("Bad<>").WithLocation(1, 1)
                    );

                mdOnlyImage = output.ToArray();
                Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            }

            var mdOnlyRef = (MetadataImageReference)AssemblyMetadata.CreateFromImage(mdOnlyImage).GetReference(display: "mdOnlyRef");

            // Load ErrorType, use it without error, and verify its symbol
            var compWithUsage1 = CreateCompilation(@"
class D
{
    void M2<T>(C<T> c)
    {
        var bad = c.M();
        bad.Missing();
    }
}", references: new[] { MscorlibRef, mdOnlyRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            compWithUsage1.VerifyDiagnostics();
            // PROTOTYPE(tolerate-errors) we need one diagnostic for the entire compilation (when emitting with a reference to error assembly)

            AssertEx.Equal(
                new[] { "Bad<>[missing] C<T>.M()", "C<T>..ctor()" },
                compWithUsage1.GetMember<NamedTypeSymbol>("C").GetMembers().Select(m => m.ToTestDisplayString()));

            // Verify source symbol for ErrorType
            var sourceMethod = (MethodSymbol)comp.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var sourceErrorType = (ConstructedErrorTypeSymbol)sourceMethod.ReturnType;
            Assert.Equal("Bad<T>", sourceErrorType.ToDisplayString());
            sourceErrorType.ErrorInfo.Verify(
                // error CS0246: The type or namespace name 'Bad<>' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("Bad<>").WithLocation(1, 1)
                );
            Assert.True(sourceErrorType.IsErrorType());
            Assert.Equal(1, sourceErrorType.Arity);

            // Verify PE symbol for ErrorType
            var peMethod = (PEMethodSymbol)compWithUsage1.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var peErrorType = (MissingMetadataTypeSymbol.TopLevel)peMethod.ReturnType;
            Assert.Equal("Bad<>", peErrorType.ToDisplayString());
            Assert.Null(peErrorType.ErrorInfo);
            Assert.True(peErrorType.IsErrorType());
            Assert.Equal(1, peErrorType.Arity);

            // Using Bad in client source produces new errors
            var compWithUsage2 = CreateCompilation(@"
class D
{
    void M2<T1>(Bad b, Bad<T1> b1)
    {
        b.ToString();
        b1.ToString();
    }
}", references: new[] { MscorlibRef, mdOnlyRef }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            compWithUsage2.VerifyDiagnostics(
                // (4,17): error CS0246: The type or namespace name 'Bad' could not be found (are you missing a using directive or an assembly reference?)
                //     void M2<T1>(Bad b, Bad<T1> b1)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bad").WithArguments("Bad").WithLocation(4, 17),
                // (4,24): error CS0246: The type or namespace name 'Bad<>' could not be found (are you missing a using directive or an assembly reference?)
                //     void M2<T1>(Bad b, Bad<T1> b1)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Bad<T1>").WithArguments("Bad<>").WithLocation(4, 24)
                );
        }

        [Fact]
        public void MetadataOnly_TolerateErrors_WithNestedGenericType()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class C<T>
{
    public C<T1>.Bad<T1, T2> M<T1, T2>() { throw null; }
}
", references: new[] { MscorlibRef });

            byte[] mdOnlyImage;
            using (var output = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, options: new EmitOptions(metadataOnly: true, tolerateErrors: true));
                Assert.True(emitResult.Success);

                emitResult.Diagnostics.Verify(
                    // (4,18): error CS0426: The type name 'Bad<,>' does not exist in the type 'C<T1>'
                    //     public C<T1>.Bad<T1, T2> M<T1, T2>() { throw null; }
                    Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "Bad<T1, T2>").WithArguments("Bad<,>", "C<T1>").WithLocation(4, 18),
                    // error CS0426: The type name 'Bad<,>' does not exist in the type 'C<T1>'
                    Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg).WithArguments("Bad<,>", "C<T1>").WithLocation(1, 1)
                    );

                mdOnlyImage = output.ToArray();
                Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            }

            var mdOnlyRef = (MetadataImageReference)AssemblyMetadata.CreateFromImage(mdOnlyImage).GetReference(display: "mdOnlyRef");
            var compWithRef = CreateCompilation("", references: new[] { MscorlibRef, mdOnlyRef });
            compWithRef.VerifyDiagnostics();

            var metadataReader = ((AssemblyMetadata)mdOnlyRef.GetMetadataNoCopy()).GetModules()[0].MetadataReader;
            Assert.Equal(new string[] { "mscorlib", "CodeAnalysisError" },
                metadataReader.AssemblyReferences.Select(ar => metadataReader.GetString(metadataReader.GetAssemblyReference(ar).Name)));

            AssertEx.Equal(
                new[] { "C<T1>.Bad<,>[missing] C<T>.M<T1, T2>()", "C<T>..ctor()" },
                compWithRef.GetMember<NamedTypeSymbol>("C").GetMembers().Select(m => m.ToTestDisplayString()));

            var compWithUsage = CreateCompilation(@"
class D
{
    void M(C<int> c)
    {
        var bad = c.M<int, int>();
        bad.Missing();
    }
}
", references: new[] { MscorlibRef, mdOnlyRef });
            compWithUsage.VerifyDiagnostics();

            // Verify source symbol for ErrorType
            var sourceMethod = (MethodSymbol)comp.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var sourceErrorType = (ConstructedErrorTypeSymbol)sourceMethod.ReturnType;
            Assert.Equal("C<T1>.Bad<T1, T2>", sourceErrorType.ToDisplayString());
            Assert.Equal("<global namespace>", sourceErrorType.ContainingNamespace.ToTestDisplayString());
            Assert.Equal("C<T1>", sourceErrorType.ContainingType.ToTestDisplayString());
            sourceErrorType.ErrorInfo.Verify(
                // error CS0426: The type name 'Bad<,>' does not exist in the type 'C<T1>'
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg).WithArguments("Bad<,>", "C<T1>").WithLocation(1, 1)
                );

            // Verify PE symbol for ErrorType
            var peMethod = (PEMethodSymbol)compWithUsage.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var peErrorType = (MissingMetadataTypeSymbol.TopLevel)peMethod.ReturnType;
            Assert.Equal("C<T1>.Bad<,>", peErrorType.ToDisplayString());
            Assert.Equal("C<T1>", peErrorType.ContainingNamespace.ToTestDisplayString());
            Assert.Null(peErrorType.ContainingType);
            Assert.Null(peErrorType.ErrorInfo);

            // PROTOTYPE(tolerate-errors) In metadata, the nested error type is top-level
            // PROTOTYPE(tolerate-errors) On a related note, the type parameters are not replaced in the namespace
            // PROTOTYPE(tolerate-errors) The type parameters are not replaced in the name
        }

        [Fact]
        public void MetadataOnly_TolerateErrors_WithNestedType()
        {
            CSharpCompilation comp = CreateCompilation(@"
class C
{
    C.Bad M() { throw null; }
}
", references: new[] { MscorlibRef });

            byte[] mdOnlyImage;
            using (var output = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, options: new EmitOptions(metadataOnly: true, tolerateErrors: true));
                Assert.True(emitResult.Success);

                emitResult.Diagnostics.Verify(
                    // (4,7): error CS0426: The type name 'Bad' does not exist in the type 'C'
                    //     C.Bad M() { throw null; }
                    Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "Bad").WithArguments("Bad", "C").WithLocation(4, 7),
                    // error CS0426: The type name 'Bad' does not exist in the type 'C'
                    Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg).WithArguments("Bad", "C").WithLocation(1, 1)
                    );

                mdOnlyImage = output.ToArray();
                Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            }

            var mdOnlyRef = (MetadataImageReference)AssemblyMetadata.CreateFromImage(mdOnlyImage).GetReference(display: "mdOnlyRef");
            var compWithRef = CreateCompilation("", references: new[] { MscorlibRef, mdOnlyRef },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            compWithRef.VerifyDiagnostics();

            var metadataReader = ((AssemblyMetadata)mdOnlyRef.GetMetadataNoCopy()).GetModules()[0].MetadataReader;
            Assert.Equal(new string[] { "mscorlib", "CodeAnalysisError" },
                metadataReader.AssemblyReferences.Select(ar => metadataReader.GetString(metadataReader.GetAssemblyReference(ar).Name)));

            AssertEx.Equal(
                new[] { "C.Bad[missing] C.M()", "C..ctor()" },
                compWithRef.GetMember<NamedTypeSymbol>("C").GetMembers().Select(m => m.ToTestDisplayString()));

            var sourceMethod = (MethodSymbol)comp.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var sourceErrorType = (ExtendedErrorTypeSymbol)sourceMethod.ReturnType;
            Assert.Equal("C.Bad", sourceErrorType.ToDisplayString());
            sourceErrorType.ErrorInfo.Verify(
                // error CS0426: The type name 'Bad' does not exist in the type 'C'
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg).WithArguments("Bad", "C").WithLocation(1, 1)
                );

            var peMethod = (PEMethodSymbol)compWithRef.GetMember<NamedTypeSymbol>("C").GetMember("M");
            var peErrorType = (MissingMetadataTypeSymbol.TopLevel)peMethod.ReturnType;
            Assert.Equal("C.Bad", peErrorType.ToDisplayString());
            Assert.Null(peErrorType.ErrorInfo);
        }

        [Fact]
        public void MetadataOnly_TolerateErrors_UnimplementedAbstractMethod()
        {
            CSharpCompilation comp = CreateCompilation(@"
public abstract class Base
{
    public abstract void M();
}
public class C : Base
{
}
", references: new[] { MscorlibRef });


            byte[] mdOnlyImage = EmitMetadataOnlyImageAndVerifyDiagnostics(comp,
                // (6,14): error CS0534: 'C' does not implement inherited abstract member 'Base.M()'
                // public class C : Base
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C").WithArguments("C", "Base.M()").WithLocation(6, 14)
                );

            var mdOnlyRef = (MetadataImageReference)AssemblyMetadata.CreateFromImage(mdOnlyImage).GetReference(display: "mdOnlyRef");

            // Verify types included in the metadata-only image
            var compWithRef = CreateCompilation("", references: new[] { MscorlibRef, mdOnlyRef },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            compWithRef.VerifyDiagnostics();

            AssertEx.Equal(
                new[] { "<Module>", "Base", "C" },
                compWithRef.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(
                new[] { "C..ctor()" },
                compWithRef.GetMember<NamedTypeSymbol>("C").GetMembers().Select(m => m.ToTestDisplayString()));

            var compWithUsage = CreateCompilation(@"
class D
{
    void M(C c)
    {
        c.M();
    }
}
", references: new[] { MscorlibRef, mdOnlyRef });
            compWithUsage.VerifyDiagnostics();
        }

        private static byte[] EmitMetadataOnlyImageAndVerifyDiagnostics(CSharpCompilation comp, params CodeAnalysis.Test.Utilities.DiagnosticDescription[] expected)
        {
            byte[] mdOnlyImage;
            using (var output = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, options: new EmitOptions(metadataOnly: true, tolerateErrors: true));
                Assert.True(emitResult.Success);

                emitResult.Diagnostics.Verify(expected);

                mdOnlyImage = output.ToArray();
                Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            }

            return mdOnlyImage;
        }

        // PROTOTYPE(tolerate-errors) Could also try Bad<T>.Bad2<T1, T2>
    }
}
