﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Extensions;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class CompilationExtensions
    {
        internal static bool EnableVerifyIOperation { get; } = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ROSLYN_TEST_IOPERATION"));

        internal static ImmutableArray<byte> EmitToArray(
            this Compilation compilation,
            EmitOptions options = null,
            CompilationTestData testData = null,
            DiagnosticDescription[] expectedWarnings = null,
            Stream pdbStream = null,
            IMethodSymbol debugEntryPoint = null,
            Stream sourceLinkStream = null,
            IEnumerable<EmbeddedText> embeddedTexts = null)
        {
            var peStream = new MemoryStream();

            if (pdbStream == null && compilation.Options.OptimizationLevel == OptimizationLevel.Debug && options?.DebugInformationFormat != DebugInformationFormat.Embedded)
            {
                if (MonoHelpers.IsRunningOnMono() || PathUtilities.IsUnixLikePlatform)
                {
                    options = (options ?? EmitOptions.Default).WithDebugInformationFormat(DebugInformationFormat.PortablePdb);
                }

                pdbStream = new MemoryStream();
            }

            var emitResult = compilation.Emit(
                peStream: peStream,
                metadataPEStream: null,
                pdbStream: pdbStream,
                xmlDocumentationStream: null,
                win32Resources: null,
                manifestResources: null,
                options: options,
                debugEntryPoint: debugEntryPoint,
                sourceLinkStream: sourceLinkStream,
                embeddedTexts: embeddedTexts,
                testData: testData,
                cancellationToken: default(CancellationToken));

            Assert.True(emitResult.Success, "Diagnostics:\r\n" + string.Join("\r\n", emitResult.Diagnostics.Select(d => d.ToString())));

            if (expectedWarnings != null)
            {
                emitResult.Diagnostics.Verify(expectedWarnings);
            }

            return peStream.ToImmutable();
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

            using (MemoryStream mdStream = new MemoryStream(), ilStream = new MemoryStream(), pdbStream = new MemoryStream())
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

                return new CompilationDifference(
                    mdStream.ToImmutable(),
                    ilStream.ToImmutable(),
                    pdbStream.ToImmutable(),
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
            OperationTreeVerifier.Verify(expectedOperationTree, actualTextBuilder.ToString());
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
            var declarationsBuilder = ArrayBuilder<DeclarationInfo>.GetInstance();
            model.ComputeDeclarationsInNode(root, getSymbol: true, builder: declarationsBuilder, cancellationToken: CancellationToken.None);

            var actualTextBuilder = new StringBuilder();
            foreach (DeclarationInfo declaration in declarationsBuilder.ToArrayAndFree().Where(d => d.DeclaredSymbol != null).OrderBy(d => d.DeclaredSymbol.ToTestDisplayString()))
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

            OperationTreeVerifier.Verify(expectedOperationTree, actualTextBuilder.ToString());
        }

        private static void AppendOperationTree(SemanticModel model, SyntaxNode node, StringBuilder actualTextBuilder, int initialIndent = 0)
        {
            IOperation operation = model.GetOperation(node);
            if (operation != null)
            {
                string operationTree = OperationTreeVerifier.GetOperationTree(model.Compilation, operation, initialIndent);
                actualTextBuilder.Append(operationTree);
            }
            else
            {
                actualTextBuilder.Append($"  SemanticModel.GetOperation() returned NULL for node with text: '{node.ToString()}'");
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

        public static void ValidateIOperations(Func<Compilation> createCompilation)
        {
            if (!EnableVerifyIOperation)
            {
                return;
            }

            var compilation = createCompilation();
            var roots = ArrayBuilder<IOperation>.GetInstance();
            var stopWatch = new Stopwatch();
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                stopWatch.Start();
            }

            void checkTimeout()
            {
                const int timeout = 10000;
                Assert.False(stopWatch.ElapsedMilliseconds > timeout, "ValidateIOperations took too long");
            }

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();

                foreach (var node in root.DescendantNodesAndSelf())
                {
                    checkTimeout();

                    var operation = semanticModel.GetOperation(node);
                    if (operation != null)
                    {
                        // Make sure IOperation returned by GetOperation(syntaxnode) will have same syntaxnode as the given syntaxnode(IOperation.Syntax == syntaxnode).
                        Assert.True(node == operation.Syntax, $"Expected : {node} - Actual : {operation.Syntax}");

                        Assert.True(operation.Type == null || !operation.MustHaveNullType(), $"Unexpected non-null type: {operation.Type}");

                        if (operation.Parent == null)
                        {
                            roots.Add(operation);
                        }
                    }
                }
            }

            var explictNodeMap = new Dictionary<SyntaxNode, IOperation>();
            var visitor = TestOperationVisitor.Singleton;

            foreach (var root in roots)
            {
                foreach (var operation in root.DescendantsAndSelf())
                {
                    checkTimeout();

                    if (!operation.IsImplicit)
                    {
                        try
                        {
                            explictNodeMap.Add(operation.Syntax, operation);
                        }
                        catch (ArgumentException)
                        {
                            Assert.False(true, $"Duplicate explicit node for syntax ({operation.Syntax.RawKind}): {operation.Syntax.ToString()}");
                        }
                    }

                    visitor.Visit(operation);
                }

                stopWatch.Stop();
                checkControlFlowGraph(root);
                stopWatch.Start();
            }

            roots.Free();
            stopWatch.Stop();
            return;

            void checkControlFlowGraph(IOperation root)
            {
                switch (root)
                {
                    case IMethodBodyBaseOperation methodBody:
                        if (methodBody.Kind == OperationKind.ConstructorBodyOperation && !((IConstructorBodyOperation)methodBody).Locals.IsEmpty)
                        {
                            // PROTOTYPE(dataflow): Constructor initializers and locals declared within them are not handled right now
                            break;
                        }

                        if (methodBody.BlockBody != null)
                        {
                            ControlFlowGraphVerifier.GetFlowGraph(compilation, Operations.ControlFlowGraphBuilder.Create(methodBody.BlockBody));
                        }

                        if (methodBody.ExpressionBody != null)
                        {
                            ControlFlowGraphVerifier.GetFlowGraph(compilation, Operations.ControlFlowGraphBuilder.Create(methodBody.ExpressionBody));
                        }

                        // PROTOTYPE(dataflow): add handling for constructor initializer
                        break;

                    case IBlockOperation blockOperation:
                        // PROTOTYPE(dataflow): It looks like blocks in script can have no parent and not represent complete code.
                        //                      See Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.GotoTests.OutOfScriptBlock
                        //                      This creates problems, especially with inability to resolve branches. 
                        //                      Need to figure out what to do for scripts, either we should disallow getting CFG for them,
                        //                      or have a reliable way to check for completeness of the code given to us. 
                        //                      Going to disable verification for scripts for now so that other scenarios could be verified.
                        if (blockOperation.Syntax.SyntaxTree.Options.Kind != SourceCodeKind.Script)
                        {
                            ControlFlowGraphVerifier.GetFlowGraph(compilation, Operations.ControlFlowGraphBuilder.Create(blockOperation));
                        }

                        break;

                    case IFieldInitializerOperation fieldInitializerOperation:
                    case IPropertyInitializerOperation propertyInitializerOperation:
                        ControlFlowGraphVerifier.GetFlowGraph(compilation, Operations.ControlFlowGraphBuilder.Create(root));
                        break;

                    case IParameterInitializerOperation parameterInitializerOperation:
                        // PROTOTYPE(dataflow): Parameter initializers in local functions can refer to locals outside.
                        //                      This causes problems with graph verification because we are unable to locate a region
                        //                      for them. See Microsoft.CodeAnalysis.CSharp.UnitTests.LocalFunctionTests.LocalFunctionParameterDefaultUsingConst
                        //                      for example.
                        if ((parameterInitializerOperation.Parameter.ContainingSymbol as IMethodSymbol)?.MethodKind != MethodKind.LocalFunction)
                        {
                            ControlFlowGraphVerifier.GetFlowGraph(compilation, Operations.ControlFlowGraphBuilder.Create(root));
                        }
                        break;
                }
            }
        }
    }
}
