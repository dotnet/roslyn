// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Extensions;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class CompilationExtensions
    {
        internal static ImmutableArray<byte> EmitToArray(
            this Compilation compilation,
            EmitOptions options = null,
            CompilationTestData testData = null,
            DiagnosticDescription[] expectedWarnings = null,
            Stream pdbStream = null,
            IMethodSymbol debugEntryPoint = null)
        {
            var stream = new MemoryStream();

            if (pdbStream == null && compilation.Options.OptimizationLevel == OptimizationLevel.Debug)
            {
                if (MonoHelpers.IsRunningOnMono())
                {
                    options = (options ?? EmitOptions.Default).WithDebugInformationFormat(DebugInformationFormat.PortablePdb);
                }

                pdbStream = new MemoryStream();
            }

            var emitResult = compilation.Emit(
                peStream: stream,
                pdbStream: pdbStream,
                xmlDocumentationStream: null,
                win32Resources: null,
                manifestResources: null,
                options: options,
                debugEntryPoint: debugEntryPoint,
                testData: testData,
                cancellationToken: default(CancellationToken));

            Assert.True(emitResult.Success, "Diagnostics:\r\n" + string.Join("\r\n", emitResult.Diagnostics.Select(d => d.ToString())));

            if (expectedWarnings != null)
            {
                emitResult.Diagnostics.Verify(expectedWarnings);
            }

            return stream.ToImmutable();
        }

        public static MemoryStream EmitToStream(this Compilation compilation, EmitOptions options = null, DiagnosticDescription[] expectedWarnings = null)
        {
            var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream, options: options);
            Assert.True(emitResult.Success, "Diagnostics: " + string.Join(", ", emitResult.Diagnostics.Select(d => d.ToString())));

            if (expectedWarnings != null)
            {
                emitResult.Diagnostics.Verify(expectedWarnings);
            }

            stream.Position = 0;
            return stream;
        }

        public static MetadataReference EmitToImageReference(
            this Compilation comp,
            EmitOptions options = null,
            bool embedInteropTypes = false,
            ImmutableArray<string> aliases = default(ImmutableArray<string>),
            DiagnosticDescription[] expectedWarnings = null)
        {
            var image = comp.EmitToArray(options, expectedWarnings: expectedWarnings);
            if (comp.Options.OutputKind == OutputKind.NetModule)
            {
                return ModuleMetadata.CreateFromImage(image).GetReference(display: comp.MakeSourceModuleName());
            }
            else
            {
                return AssemblyMetadata.CreateFromImage(image).GetReference(aliases: aliases, embedInteropTypes: embedInteropTypes, display: comp.MakeSourceAssemblySimpleName());
            }
        }

        internal static CompilationDifference EmitDifference(
            this Compilation compilation,
            EmitBaseline baseline,
            ImmutableArray<SemanticEdit> edits,
            IEnumerable<ISymbol> allAddedSymbols = null,
            CompilationTestData testData = null)
        {
            testData = testData ?? new CompilationTestData();
            var isAddedSymbol = new Func<ISymbol, bool>(s => allAddedSymbols?.Contains(s) ?? false);

            var pdbName = Path.ChangeExtension(compilation.SourceModule.Name, "pdb");

            // keep the stream open, it's passed to CompilationDifference
            var pdbStream = new MemoryStream();

            using (MemoryStream mdStream = new MemoryStream(), ilStream = new MemoryStream())
            {
                var updatedMethods = new List<MethodDefinitionHandle>();

                var result = compilation.EmitDifference(
                    baseline,
                    edits,
                    isAddedSymbol,
                    mdStream,
                    ilStream,
                    pdbStream,
                    updatedMethods,
                    testData,
                    default(CancellationToken));

                pdbStream.Seek(0, SeekOrigin.Begin);

                return new CompilationDifference(
                    mdStream.ToImmutable(),
                    ilStream.ToImmutable(),
                    pdbStream,
                    testData,
                    result,
                    updatedMethods.ToImmutableArray());
            }
        }

        internal static void VerifyAssemblyVersionsAndAliases(this Compilation compilation, params string[] expectedAssembliesAndAliases)
        {
            var actual = compilation.GetBoundReferenceManager().GetReferencedAssemblyAliases().
               Select(t => $"{t.Item1.Identity.Name}, Version={t.Item1.Identity.Version}{(t.Item2.IsEmpty ? "" : ": " + string.Join(",", t.Item2))}");

            AssertEx.Equal(expectedAssembliesAndAliases, actual, itemInspector: s => '"' + s + '"');
        }

        internal static void VerifyAssemblyAliases(this Compilation compilation, params string[] expectedAssembliesAndAliases)
        {
            var actual = compilation.GetBoundReferenceManager().GetReferencedAssemblyAliases().
               Select(t => $"{t.Item1.Identity.Name}{(t.Item2.IsEmpty ? "" : ": " + string.Join(",", t.Item2))}");

            AssertEx.Equal(expectedAssembliesAndAliases, actual, itemInspector: s => '"' + s + '"');
        }

        internal static void VerifyOperationTree(this Compilation compilation, SyntaxNode node, string expectedOperationTree)
        {
            var actualTextBuilder = new StringBuilder();
            SemanticModel model = compilation.GetSemanticModel(node.SyntaxTree);
            AppendOperationTree(model, node, actualTextBuilder);
            VerifyOperationTree(expectedOperationTree, actualTextBuilder.ToString());
        }

        internal static void VerifyOperationTree(this Compilation compilation, string expectedOperationTree, bool skipImplicitlyDeclaredSymbols = false)
        {
            VerifyOperationTree(compilation, symbolToVerify: null, expectedOperationTree: expectedOperationTree, skipImplicitlyDeclaredSymbols: skipImplicitlyDeclaredSymbols);
        }

        internal static void VerifyOperationTree(this Compilation compilation, string symbolToVerify, string expectedOperationTree, bool skipImplicitlyDeclaredSymbols = false)
        {
            SyntaxTree tree = compilation.SyntaxTrees.First();
            SyntaxNode root = tree.GetRoot();
            SemanticModel model = compilation.GetSemanticModel(tree);
            var declarations = new List<DeclarationInfo>();
            model.ComputeDeclarationsInNode(root, getSymbol: true, builder: declarations, cancellationToken: CancellationToken.None);

            var actualTextBuilder = new StringBuilder();
            foreach (DeclarationInfo declaration in declarations.Where(d => d.DeclaredSymbol != null).OrderBy(d => d.DeclaredSymbol.ToTestDisplayString()))
            {
                if (!CanHaveExecutableCodeBlock(declaration.DeclaredSymbol))
                {
                    continue;
                }

                if (skipImplicitlyDeclaredSymbols && declaration.DeclaredSymbol.IsImplicitlyDeclared)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(symbolToVerify) && !declaration.DeclaredSymbol.Name.Equals(symbolToVerify, StringComparison.Ordinal))
                {
                    continue;
                }

                actualTextBuilder.Append(declaration.DeclaredSymbol.ToTestDisplayString());

                if (declaration.ExecutableCodeBlocks.Length == 0)
                {
                    actualTextBuilder.Append($" ('0' executable code blocks)");
                }
                else
                {
                    // Workaround for https://github.com/dotnet/roslyn/issues/11903 - skip the IOperation for EndBlockStatement.
                    ImmutableArray<SyntaxNode> executableCodeBlocks = declaration.ExecutableCodeBlocks;
                    if (declaration.DeclaredSymbol.Kind == SymbolKind.Method && compilation.Language == LanguageNames.VisualBasic)
                    {
                        executableCodeBlocks = executableCodeBlocks.RemoveAt(executableCodeBlocks.Length - 1);
                    }

                    foreach (SyntaxNode executableCodeBlock in executableCodeBlocks)
                    {
                        actualTextBuilder.Append(Environment.NewLine);
                        AppendOperationTree(model, executableCodeBlock, actualTextBuilder, initialIndent: 2);
                    }
                }               

                actualTextBuilder.Append(Environment.NewLine);
            }

            VerifyOperationTree(expectedOperationTree, actualTextBuilder.ToString());
        }

        private static void AppendOperationTree(SemanticModel model, SyntaxNode node, StringBuilder actualTextBuilder, int initialIndent = 0)
        {
            IOperation operation = model.GetOperation(node);
            if (operation != null)
            {
                string operationTree = OperationTreeVerifier.GetOperationTree(operation, initialIndent);
                actualTextBuilder.Append(operationTree);
            }
            else
            {
                actualTextBuilder.Append($"  SemanticModel.GetOperation() returned NULL for node with text: '{node.ToString()}'");
            }
        }

        private static void VerifyOperationTree(string expectedOperationTree, string actualOperationTree)
        {
            var assertFailed = false;
            char[] newLineChars = Environment.NewLine.ToCharArray();
            string actual = actualOperationTree.Trim(newLineChars);
            expectedOperationTree = expectedOperationTree.Trim(newLineChars);

            try
            {
                Assert.Equal(expectedOperationTree, actual);
            }
            catch (EqualException)
            {
                assertFailed = true;
                throw;
            }
            finally
            {
                if (assertFailed)
                {
                    Console.WriteLine($"Actual operation tree:\r\n{actual}\r\n");
                }
            }
        }

        internal static bool CanHaveExecutableCodeBlock(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Event:
                case SymbolKind.Method:
                case SymbolKind.NamedType:
                case SymbolKind.Property:
                    return true;

                default:
                    return false;
            }
        }

    }
}
