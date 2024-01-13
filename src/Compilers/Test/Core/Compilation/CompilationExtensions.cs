// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable
// Uncomment to enable the IOperation test hook on all test runs. Do not commit this uncommented.
//#define ROSLYN_TEST_IOPERATION

// Uncomment to enable the Used Assemblies test hook on all test runs. Do not commit this uncommented.
//#define ROSLYN_TEST_USEDASSEMBLIES

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using SeparatedWithManyChildren = Microsoft.CodeAnalysis.Syntax.SyntaxList.SeparatedWithManyChildren;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class CompilationExtensions
    {
        internal static bool EnableVerifyIOperation { get; } =
#if ROSLYN_TEST_IOPERATION
                                    true;
#else
                        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ROSLYN_TEST_IOPERATION"));
#endif

        internal static bool EnableVerifyUsedAssemblies { get; } =
#if ROSLYN_TEST_USEDASSEMBLIES
            true;
#else
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ROSLYN_TEST_USEDASSEMBLIES"));
#endif

        internal static ImmutableArray<byte> EmitToArray(
            this Compilation compilation,
            EmitOptions options = null,
            CompilationTestData testData = null,
            DiagnosticDescription[] expectedWarnings = null,
            Stream pdbStream = null,
            IMethodSymbol debugEntryPoint = null,
            Stream sourceLinkStream = null,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            Stream metadataPEStream = null)
        {
            var peStream = new MemoryStream();

            if (pdbStream == null && compilation.Options.OptimizationLevel == OptimizationLevel.Debug && options?.DebugInformationFormat != DebugInformationFormat.Embedded)
            {
                if (MonoHelpers.IsRunningOnMono() || PathUtilities.IsUnixLikePlatform)
                {
                    options = (options ?? EmitOptions.Default).WithDebugInformationFormat(DebugInformationFormat.PortablePdb);
                }

                var discretePdb = (object)options != null && options.DebugInformationFormat != DebugInformationFormat.Embedded;
                pdbStream = discretePdb ? new MemoryStream() : null;
            }

            var emitResult = compilation.Emit(
                peStream: peStream,
                metadataPEStream: metadataPEStream,
                pdbStream: pdbStream,
                xmlDocumentationStream: null,
                win32Resources: null,
                manifestResources: manifestResources,
                options: options,
                debugEntryPoint: debugEntryPoint,
                sourceLinkStream: sourceLinkStream,
                embeddedTexts: embeddedTexts,
                rebuildData: null,
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
            ImmutableArray<string> aliases = default,
            DiagnosticDescription[] expectedWarnings = null) => EmitToPortableExecutableReference(comp, options, embedInteropTypes, aliases, expectedWarnings);

        public static PortableExecutableReference EmitToPortableExecutableReference(
            this Compilation comp,
            EmitOptions options = null,
            bool embedInteropTypes = false,
            ImmutableArray<string> aliases = default,
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
            testData ??= new CompilationTestData();
            var isAddedSymbol = new Func<ISymbol, bool>(s => allAddedSymbols?.Contains(s) ?? false);

            using var mdStream = new MemoryStream();
            using var ilStream = new MemoryStream();
            using var pdbStream = new MemoryStream();

            var result = compilation.EmitDifference(
                baseline,
                edits,
                isAddedSymbol,
                mdStream,
                ilStream,
                pdbStream,
                testData,
                CancellationToken.None);

            return new CompilationDifference(
                mdStream.ToImmutable(),
                ilStream.ToImmutable(),
                pdbStream.ToImmutable(),
                testData,
                result);
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
            SemanticModel model = compilation.GetSemanticModel(node.SyntaxTree);
            model.VerifyOperationTree(node, expectedOperationTree);
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
            model.ComputeDeclarationsInNode(root, associatedSymbol: null, getSymbol: true, builder: declarationsBuilder, cancellationToken: CancellationToken.None);

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
                        model.AppendOperationTree(executableCodeBlock, actualTextBuilder, initialIndent: 2);
                    }
                }

                actualTextBuilder.Append(Environment.NewLine);
            }

            OperationTreeVerifier.Verify(expectedOperationTree, actualTextBuilder.ToString());
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
            var roots = ArrayBuilder<(IOperation operation, ISymbol associatedSymbol)>.GetInstance();
            var stopWatch = new Stopwatch();
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                stopWatch.Start();
            }

            void checkTimeout()
            {
                const int timeout = 15000;
                Assert.False(stopWatch.ElapsedMilliseconds > timeout, $"ValidateIOperations took too long: {stopWatch.ElapsedMilliseconds} ms");
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

                        Assert.Same(semanticModel, operation.SemanticModel);
                        Assert.NotSame(semanticModel, ((Operation)operation).OwningSemanticModel);
                        Assert.NotNull(((Operation)operation).OwningSemanticModel);
                        Assert.Same(semanticModel, ((Operation)operation).OwningSemanticModel.ContainingPublicModelOrSelf);
                        Assert.Same(semanticModel, semanticModel.ContainingPublicModelOrSelf);

                        if (operation.Parent == null)
                        {
                            roots.Add((operation, semanticModel.GetDeclaredSymbol(operation.Syntax)));
                        }
                    }
                }

                tree.VerifyChildNodePositions();
            }

            var explicitNodeMap = new Dictionary<SyntaxNode, IOperation>();
            var visitor = TestOperationVisitor.Singleton;

            foreach (var (root, associatedSymbol) in roots)
            {
                foreach (var operation in root.DescendantsAndSelf())
                {
                    checkTimeout();

                    if (!operation.IsImplicit)
                    {
                        try
                        {
                            explicitNodeMap.Add(operation.Syntax, operation);
                        }
                        catch (ArgumentException)
                        {
                            Assert.False(true, $"Duplicate explicit node for syntax ({operation.Syntax.RawKind}): {operation.Syntax.ToString()}");
                        }
                    }

                    visitor.Visit(operation);
                }

                stopWatch.Stop();
                checkControlFlowGraph(root, associatedSymbol);
                stopWatch.Start();
            }

            roots.Free();
            stopWatch.Stop();
            return;

            void checkControlFlowGraph(IOperation root, ISymbol associatedSymbol)
            {
                switch (root)
                {
                    case IBlockOperation blockOperation:
                        // https://github.com/dotnet/roslyn/issues/27593 tracks adding ControlFlowGraph support in script code.
                        if (blockOperation.Syntax.SyntaxTree.Options.Kind != SourceCodeKind.Script)
                        {
                            ControlFlowGraphVerifier.GetFlowGraph(compilation, ControlFlowGraphBuilder.Create(blockOperation), associatedSymbol);
                        }

                        break;

                    case IMethodBodyOperation methodBody:
                    case IConstructorBodyOperation constructorBody:
                    case IFieldInitializerOperation fieldInitializerOperation:
                    case IPropertyInitializerOperation propertyInitializerOperation:
                        ControlFlowGraphVerifier.GetFlowGraph(compilation, ControlFlowGraphBuilder.Create(root), associatedSymbol);
                        break;

                    case IParameterInitializerOperation parameterInitializerOperation:
                        // https://github.com/dotnet/roslyn/issues/27594 tracks adding support for getting ControlFlowGraph for parameter initializers for local and anonymous functions.
                        if ((parameterInitializerOperation.Parameter.ContainingSymbol as IMethodSymbol)?.MethodKind is not (MethodKind.LocalFunction or MethodKind.AnonymousFunction))
                        {
                            ControlFlowGraphVerifier.GetFlowGraph(compilation, ControlFlowGraphBuilder.Create(root), associatedSymbol);
                        }
                        break;
                }
            }
        }

        internal static void VerifyChildNodePositions(this SyntaxTree tree)
        {
            var nodes = tree.GetRoot().DescendantNodesAndSelf();
            foreach (var node in nodes)
            {
                var childNodesAndTokens = node.ChildNodesAndTokens();
                if (childNodesAndTokens.Node is { } container)
                {
                    for (int i = 0; i < childNodesAndTokens.Count; i++)
                    {
                        if (container.GetNodeSlot(i) is SeparatedWithManyChildren separatedList)
                        {
                            verifyPositions(separatedList);
                        }
                    }
                }
            }

            static void verifyPositions(SeparatedWithManyChildren separatedList)
            {
                var green = (Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList)separatedList.Green;

                // Calculate positions from start, using existing cache.
                int[] positions = getPositionsFromStart(separatedList);

                // Calculate positions from end, using existing cache.
                AssertEx.Equal(positions, getPositionsFromEnd(separatedList));

                // Avoid testing without caches if the number of children is large.
                if (separatedList.SlotCount > 100)
                {
                    return;
                }

                // Calculate positions from start, with empty cache.
                AssertEx.Equal(positions, getPositionsFromStart(new SeparatedWithManyChildren(green, null, separatedList.Position)));

                // Calculate positions from end, with empty cache.
                AssertEx.Equal(positions, getPositionsFromEnd(new SeparatedWithManyChildren(green, null, separatedList.Position)));
            }

            // Calculate positions from start, using any existing cache of red nodes on separated list.
            static int[] getPositionsFromStart(SeparatedWithManyChildren separatedList)
            {
                int n = separatedList.SlotCount;
                var positions = new int[n];
                for (int i = 0; i < n; i++)
                {
                    positions[i] = separatedList.GetChildPosition(i);
                }
                return positions;
            }

            // Calculate positions from end, using any existing cache of red nodes on separated list.
            static int[] getPositionsFromEnd(SeparatedWithManyChildren separatedList)
            {
                int n = separatedList.SlotCount;
                var positions = new int[n];
                for (int i = n - 1; i >= 0; i--)
                {
                    positions[i] = separatedList.GetChildPositionFromEnd(i);
                }
                return positions;
            }
        }

        /// <summary>
        /// The reference assembly System.Runtime.InteropServices.WindowsRuntime was removed in net5.0. This builds
        /// up <see cref="CompilationReference"/> which contains all of the well known types that were used from that
        /// reference by the compiler.
        /// </summary>
        public static PortableExecutableReference CreateWindowsRuntimeMetadataReference(TargetFramework targetFramework = TargetFramework.NetCoreApp)
        {
            var source = @"
namespace System.Runtime.InteropServices.WindowsRuntime
{
    public struct EventRegistrationToken { }

    public sealed class EventRegistrationTokenTable<T> where T : class
    {
        public T InvocationList { get; set; }

        public static EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref EventRegistrationTokenTable<T> refEventTable)
        {
            throw null;
        }

        public void RemoveEventHandler(EventRegistrationToken token)
        {
        }

        public void RemoveEventHandler(T handler)
        {
        }
    }

    public static class WindowsRuntimeMarshal
    {
        public static void AddEventHandler<T>(Func<T, EventRegistrationToken> addMethod, Action<EventRegistrationToken> removeMethod, T handler)
        {
        }

        public static void RemoveAllEventHandlers(Action<EventRegistrationToken> removeMethod)
        {
        }

        public static void RemoveEventHandler<T>(Action<EventRegistrationToken> removeMethod, T handler)
        {
        }
    }
}
";

            // The actual System.Runtime.InteropServices.WindowsRuntime DLL has a public key of
            // b03f5f7f11d50a3a and version 4.0.4.0. The compiler just looks at these via 
            // WellKnownTypes and WellKnownMembers so it can be safely skipped here. 
            var compilation = CSharpCompilation.Create(
                "System.Runtime.InteropServices.WindowsRuntime",
                new[] { CSharpSyntaxTree.ParseText(SourceText.From(source, encoding: null, SourceHashAlgorithms.Default)) },
                references: TargetFrameworkUtil.GetReferences(targetFramework),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            compilation.VerifyEmitDiagnostics();
            return compilation.EmitToPortableExecutableReference();
        }
    }
}
