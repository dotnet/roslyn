// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using Xunit;
using static TestReferences;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public abstract class CSharpTestBase : CSharpTestBaseBase
    {
        protected CSharpCompilation GetCSharpCompilationForEmit(
            IEnumerable<string> source,
            IEnumerable<MetadataReference> additionalRefs,
            CompilationOptions options,
            ParseOptions parseOptions)
        {
            return (CSharpCompilation)base.GetCompilationForEmit(source, additionalRefs, options, parseOptions);
        }

        private Action<IModuleSymbol> Translate2(Action<ModuleSymbol> action)
        {
            if (action != null)
            {
                return (m) => action((ModuleSymbol)m);
            }
            else
            {
                return null;
            }
        }

        private Action<IModuleSymbol> Translate(Action<ModuleSymbol> action)
        {
            if (action != null)
            {
                return m => action((ModuleSymbol)m);
            }
            else
            {
                return null;
            }
        }

        internal CompilationVerifier CompileAndVerify(
            string source,
            IEnumerable<MetadataReference> additionalRefs = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            CompilationOptions options = null,
            ParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            Verification verify = Verification.Passes)
        {
            return base.CompileAndVerify(
                source: source,
                additionalRefs: additionalRefs,
                dependencies: dependencies,
                sourceSymbolValidator: Translate2(sourceSymbolValidator),
                assemblyValidator: assemblyValidator,
                symbolValidator: Translate2(symbolValidator),
                expectedSignatures: expectedSignatures,
                expectedOutput: expectedOutput,
                options: options,
                parseOptions: parseOptions,
                emitOptions: emitOptions,
                verify: verify);
        }

        internal CompilationVerifier CompileAndVerify(
            string[] sources,
            MetadataReference[] additionalRefs = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> validator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            int? expectedReturnCode = null,
            string[] args = null,
            CompilationOptions options = null,
            ParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            Verification verify = Verification.Passes)
        {
            return base.CompileAndVerify(
                sources,
                additionalRefs,
                dependencies,
                Translate2(sourceSymbolValidator),
                validator,
                Translate2(symbolValidator),
                expectedSignatures,
                expectedOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                verify);
        }

        internal CompilationVerifier CompileAndVerifyWinRt(
            string source,
            string expectedOutput = null,
            MetadataReference[] additionalRefs = null,
            CSharpCompilationOptions options = null,
            Verification verify = Verification.Passes)
        {
            if (options == null)
            {
                options = expectedOutput != null ? TestOptions.ReleaseExe : TestOptions.ReleaseDll;
            }

            var compilation = CreateCompilation(source,
                                                WinRtRefs.Concat(additionalRefs ?? Enumerable.Empty<MetadataReference>()),
                                                options);

            return CompileAndVerify(
                compilation: compilation,
                expectedOutput: expectedOutput,
                verify: verify);
        }

        internal CompilationVerifier CompileAndVerify(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> validator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            int? expectedReturnCode = null,
            string[] args = null,
            EmitOptions emitOptions = null,
            Verification verify = Verification.Passes)
        {
            return base.CompileAndVerify(
                compilation,
                manifestResources,
                dependencies,
                Translate2(sourceSymbolValidator),
                validator,
                Translate2(symbolValidator),
                expectedSignatures,
                expectedOutput,
                expectedReturnCode,
                args,
                emitOptions,
                verify);
        }
    }

    public abstract class CSharpTestBaseBase : CommonTestBase
    {
        public static CSharpCompilation CreateWinRtCompilation(string text, MetadataReference[] additionalRefs = null)
        {
            return CSharpTestBase.CreateCompilation(text,
                                                    WinRtRefs.Concat(additionalRefs ?? Enumerable.Empty<MetadataReference>()),
                                                    TestOptions.ReleaseExe);
        }
        
        protected override CompilationOptions CompilationOptionsReleaseDll
        {
            get { return TestOptions.ReleaseDll; }
        }

        #region SyntaxTree Factories

        public static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
        {
            if ((object)options == null)
            {
                options = TestOptions.Regular;
            }

            var stringText = StringText.From(text, Encoding.UTF8);
            return CheckSerializable(SyntaxFactory.ParseSyntaxTree(stringText, options, filename));
        }

        private static SyntaxTree CheckSerializable(SyntaxTree tree)
        {
            var stream = new MemoryStream();
            var root = tree.GetRoot();
            root.SerializeTo(stream);
            stream.Position = 0;
            var deserializedRoot = CSharpSyntaxNode.DeserializeFrom(stream);
            return tree;
        }

        public static SyntaxTree[] Parse(IEnumerable<string> sources, CSharpParseOptions options = null)
        {
            if (sources == null || !sources.Any())
            {
                return new SyntaxTree[] { };
            }

            return Parse(options, sources.ToArray());
        }

        public static SyntaxTree[] Parse(CSharpParseOptions options = null, params string[] sources)
        {
            if (sources == null || (sources.Length == 1 && null == sources[0]))
            {
                return new SyntaxTree[] { };
            }

            return sources.Select(src => Parse(src, options: options)).ToArray();
        }

        public static SyntaxTree ParseWithRoundTripCheck(string text, CSharpParseOptions options = null)
        {
            var tree = Parse(text, options: options);
            var parsedText = tree.GetRoot();
            // we validate the text roundtrips
            Assert.Equal(text, parsedText.ToFullString());
            return tree;
        }

        #endregion

        #region Compilation Factories

        public static CSharpCompilation CreateCompilationWithCustomILSource(
            string source,
            string ilSource,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            bool appendDefaultHeader = true)
        {
            IEnumerable<MetadataReference> metadataReferences = new[] { CompileIL(ilSource, appendDefaultHeader) };
            if (references != null)
            {
                metadataReferences = metadataReferences.Concat(references);
            }

            return CreateStandardCompilation(source, metadataReferences, options);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib45(
            IEnumerable<SyntaxTree> source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            var refs = new List<MetadataReference>();
            if (references != null)
            {
                refs.AddRange(references);
            }
            refs.Add(MscorlibRef_v4_0_30316_17626);
            return CreateCompilation(source, refs, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib46(
            IEnumerable<SyntaxTree> source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            var refs = new List<MetadataReference>();
            if (references != null)
            {
                refs.AddRange(references);
            }
            refs.Add(MscorlibRef_v46);
            return CreateCompilation(source, refs, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib46(
            string source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string sourceFileName = "",
            string assemblyName = "")
        {
            return CreateCompilationWithMscorlib46(
                new[] { source },
                references,
                options,
                parseOptions,
                sourceFileName,
                assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib46(
            string[] sources,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string sourceFileName = "",
            string assemblyName = "")
        {
            return CreateCompilationWithMscorlib46(
                sources.Select((source) => Parse(source, sourceFileName, parseOptions)).ToArray(),
                references,
                options,
                assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib45(
            string source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string sourceFileName = "",
            string assemblyName = "")
        {
            return CreateCompilationWithMscorlib45(
                new SyntaxTree[] { Parse(source, sourceFileName, parseOptions) },
                references,
                options,
                assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib45(
            string[] sources,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string sourceFileName = "",
            string assemblyName = "")
        {
            return CreateCompilationWithMscorlib45(
                sources.Select((source) => Parse(source, sourceFileName, parseOptions)).ToArray(),
                references,
                options,
                assemblyName);
        }

        public static CSharpCompilation CreateStandardCompilation(
            string text,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "")
        {
            return CreateStandardCompilation(
                new[] { Parse(text, sourceFileName, parseOptions) },
                references: references,
                options: options,
                assemblyName: assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib45AndCSruntime(
            string text,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] additionalRefs = null)
        {
            var refs = new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef };

            if (additionalRefs != null)
            {
                refs.AddRange(additionalRefs);
            }

            return CreateCompilation(new[] { Parse(text, options: parseOptions) }, refs, options);
        }

        public static CSharpCompilation CreateStandardCompilation(
            IEnumerable<string> sources,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "")
        {
            return CreateStandardCompilation(Parse(sources, parseOptions), references, options, assemblyName);
        }

        public static CSharpCompilation CreateStandardCompilation(
            SyntaxTree syntaxTree,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            return CreateStandardCompilation(new SyntaxTree[] { syntaxTree }, references, options, assemblyName);
        }

        private static readonly ImmutableArray<MetadataReference> s_stdRefs = CoreClrShim.IsRunningOnCoreClr
            ? ImmutableArray.Create<MetadataReference>(NetStandard20.NetStandard, NetStandard20.MscorlibRef, NetStandard20.SystemRuntimeRef, NetStandard20.SystemDynamicRuntimeRef)
            : ImmutableArray.Create(MscorlibRef);

        // Careful! Make sure everything in s_desktopRefsToRemove is constructed with
        // the same object identity, since MetadataReference uses reference equality.
        // this may mean adding Interlocked calls in the construction of the reference.
        private static readonly ImmutableArray<MetadataReference> s_desktopRefsToRemove = ImmutableArray.Create(SystemRef, SystemCoreRef);

        public static CSharpCompilation CreateStandardCompilation(
            IEnumerable<SyntaxTree> trees,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            if (CoreClrShim.IsRunningOnCoreClr)
            {
                references = references?.Except(s_desktopRefsToRemove);
            }
            return CreateCompilation(trees, (references != null) ? s_stdRefs.Concat(references) : s_stdRefs, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlibAndSystemCore(
            IEnumerable<SyntaxTree> trees,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            return CreateCompilation(trees, (references != null) ? new[] { MscorlibRef, SystemCoreRef }.Concat(references) : new[] { MscorlibRef, SystemCoreRef }, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlibAndSystemCore(
            string text,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "")
        {
            references = (references != null) ? new[] { MscorlibRef, SystemCoreRef }.Concat(references) : new[] { MscorlibRef, SystemCoreRef };

            return CreateCompilation(
                new[] { Parse(text, "", parseOptions) },
                references: references,
                options: options,
                assemblyName: assemblyName);
        }

        private static readonly ImmutableArray<MetadataReference> s_mscorlibRefArray = ImmutableArray.Create(MscorlibRef);

        public static CSharpCompilation CreateCompilationWithMscorlibAndDocumentationComments(
            string text,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "Test")
        {
            return CreateCompilation(
                new[] { Parse(text, options: TestOptions.RegularWithDocumentationComments) },
                references: references?.Concat(s_mscorlibRefArray) ?? s_mscorlibRefArray,
                options: (options ?? TestOptions.ReleaseDll).WithXmlReferenceResolver(XmlFileResolver.Default),
                assemblyName: assemblyName);
        }

        public static CSharpCompilation CreateCompilation(
            string source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "")
        {
            return CreateCompilation(new[] { Parse(source, options: parseOptions) }, references, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilation(
            IEnumerable<string> sources,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "")
        {
            return CreateCompilation(Parse(sources, parseOptions), references, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilation(
            IEnumerable<SyntaxTree> trees,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            if (options == null)
            {
                options = TestOptions.ReleaseDll;
            }

            // Using single-threaded build if debugger attached, to simplify debugging.
            if (Debugger.IsAttached)
            {
                options = options.WithConcurrentBuild(false);
            }

            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.Create(
                assemblyName == "" ? GetUniqueName() : assemblyName,
                trees,
                references,
                options);
            CompilationExtensions.ValidateIOperations(createCompilationLambda);
            return createCompilationLambda();
        }

        public static CSharpCompilation CreateCompilation(
            AssemblyIdentity identity,
            string[] sources,
            MetadataReference[] references,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null)
        {
            var trees = (sources == null) ? null : sources.Select(s => Parse(s, options: parseOptions)).ToArray();
            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.Create(identity.Name, options: options ?? TestOptions.ReleaseDll, references: references, syntaxTrees: trees);

            CompilationExtensions.ValidateIOperations(createCompilationLambda);
            var c = createCompilationLambda();
            Assert.NotNull(c.Assembly); // force creation of SourceAssemblySymbol

            ((SourceAssemblySymbol)c.Assembly).lazyAssemblyIdentity = identity;
            return c;
        }

        public static CSharpCompilation CreateSubmissionWithExactReferences(
           string code,
           IEnumerable<MetadataReference> references = null,
           CSharpCompilationOptions options = null,
           CSharpParseOptions parseOptions = null,
           CSharpCompilation previous = null,
           Type returnType = null,
           Type hostObjectType = null)
        {
            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.CreateScriptCompilation(
                GetUniqueName(),
                references: references,
                options: options,
                syntaxTree: Parse(code, options: parseOptions ?? TestOptions.Script),
                previousScriptCompilation: previous,
                returnType: returnType,
                globalsType: hostObjectType);
            CompilationExtensions.ValidateIOperations(createCompilationLambda);
            return createCompilationLambda();
        }

        private static ImmutableArray<MetadataReference> s_scriptRefs = ImmutableArray.Create(MscorlibRef_v4_0_30316_17626);

        public static CSharpCompilation CreateSubmission(
           string code,
           IEnumerable<MetadataReference> references = null,
           CSharpCompilationOptions options = null,
           CSharpParseOptions parseOptions = null,
           CSharpCompilation previous = null,
           Type returnType = null,
           Type hostObjectType = null)
        {
            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.CreateScriptCompilation(
                GetUniqueName(),
                references: (references != null) ? s_scriptRefs.Concat(references) : s_scriptRefs,
                options: options,
                syntaxTree: Parse(code, options: parseOptions ?? TestOptions.Script),
                previousScriptCompilation: previous,
                returnType: returnType,
                globalsType: hostObjectType);
            CompilationExtensions.ValidateIOperations(createCompilationLambda);
            return createCompilationLambda();
        }

        public CompilationVerifier CompileWithCustomILSource(string cSharpSource, string ilSource, Action<CSharpCompilation> compilationVerifier = null, bool importInternals = true, string expectedOutput = null)
        {
            var compilationOptions = (expectedOutput != null) ? TestOptions.ReleaseExe : TestOptions.ReleaseDll;

            if (importInternals)
            {
                compilationOptions = compilationOptions.WithMetadataImportOptions(MetadataImportOptions.Internal);
            }

            if (ilSource == null)
            {
                var c = CreateStandardCompilation(cSharpSource, options: compilationOptions);
                return CompileAndVerify(c, expectedOutput: expectedOutput);
            }

            MetadataReference reference = CreateMetadataReferenceFromIlSource(ilSource);

            var compilation = CreateStandardCompilation(cSharpSource, new[] { reference }, compilationOptions);
            compilationVerifier?.Invoke(compilation);

            return CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        public static MetadataReference CreateMetadataReferenceFromIlSource(string ilSource)
        {
            using (var tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource))
            {
                return MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path));
            }
        }

        protected override Compilation GetCompilationForEmit(
            IEnumerable<string> source,
            IEnumerable<MetadataReference> additionalRefs,
            CompilationOptions options,
            ParseOptions parseOptions)
        {
            return CreateStandardCompilation(
                source,
                references: additionalRefs,
                options: (CSharpCompilationOptions)options,
                parseOptions: (CSharpParseOptions)parseOptions,
                assemblyName: GetUniqueName());
        }

        /// <summary>
        /// Like CompileAndVerify, but confirms that execution raises an exception.
        /// </summary>
        /// <typeparam name="T">Expected type of the exception.</typeparam>
        /// <param name="source">Program to compile and execute.</param>
        /// <param name="expectedMessage">Ignored if null.</param>
        internal CompilationVerifier CompileAndVerifyException<T>(string source, string expectedMessage = null, bool allowUnsafe = false, Verification verify = Verification.Passes) where T : Exception
        {
            var comp = CreateStandardCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe));
            return CompileAndVerifyException<T>(comp, expectedMessage, verify);
        }

        internal CompilationVerifier CompileAndVerifyException<T>(CSharpCompilation comp, string expectedMessage = null, Verification verify = Verification.Passes) where T : Exception
        {
            try
            {
                CompileAndVerify(comp, expectedOutput: "", verify: verify); //need expected output to force execution
                Assert.False(true, string.Format("Expected exception {0}({1})", typeof(T).Name, expectedMessage));
            }
            catch (ExecutionException x)
            {
                var e = x.InnerException;
                Assert.IsType<T>(e);
                if (expectedMessage != null)
                {
                    Assert.Equal(expectedMessage, e.Message);
                }
            }

            return CompileAndVerify(comp, verify: verify);
        }

        protected static List<SyntaxNode> GetSyntaxNodeList(SyntaxTree syntaxTree)
        {
            return GetSyntaxNodeList(syntaxTree.GetRoot(), null);
        }

        protected static List<SyntaxNode> GetSyntaxNodeList(SyntaxNode node, List<SyntaxNode> synList)
        {
            if (synList == null)
                synList = new List<SyntaxNode>();

            synList.Add(node);

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    synList = GetSyntaxNodeList(child.AsNode(), synList);
            }

            return synList;
        }

        protected static SyntaxNode GetSyntaxNodeForBinding(List<SyntaxNode> synList)
        {
            return GetSyntaxNodeOfTypeForBinding<SyntaxNode>(synList);
        }

        protected const string StartString = "/*<bind>*/";
        protected const string EndString = "/*</bind>*/";

        protected static TNode GetSyntaxNodeOfTypeForBinding<TNode>(List<SyntaxNode> synList) where TNode : SyntaxNode
        {
            foreach (var node in synList.OfType<TNode>())
            {
                string exprFullText = node.ToFullString();
                exprFullText = exprFullText.Trim();

                if (exprFullText.StartsWith(StartString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(EndString))
                    {
                        if (exprFullText.EndsWith(EndString, StringComparison.Ordinal))
                        {
                            return node;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return node;
                    }
                }

                if (exprFullText.EndsWith(EndString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(StartString))
                    {
                        if (exprFullText.StartsWith(StartString, StringComparison.Ordinal))
                        {
                            return node;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Semantic Model Helpers

        public Tuple<TNode, SemanticModel> GetBindingNodeAndModel<TNode>(CSharpCompilation compilation, int treeIndex = 0) where TNode : SyntaxNode
        {
            var node = GetBindingNode<TNode>(compilation, treeIndex);
            return new Tuple<TNode, SemanticModel>(node, compilation.GetSemanticModel(compilation.SyntaxTrees[treeIndex]));
        }

        public Tuple<IList<TNode>, SemanticModel> GetBindingNodesAndModel<TNode>(CSharpCompilation compilation, int treeIndex = 0, int which = -1) where TNode : SyntaxNode
        {
            var nodes = GetBindingNodes<TNode>(compilation, treeIndex, which);
            return new Tuple<IList<TNode>, SemanticModel>(nodes, compilation.GetSemanticModel(compilation.SyntaxTrees[treeIndex]));
        }

        /// <summary>
        /// This method handles one binding text with strong SyntaxNode type
        /// </summary>
        public TNode GetBindingNode<TNode>(CSharpCompilation compilation, int treeIndex = 0) where TNode : SyntaxNode
        {
            Assert.True(compilation.SyntaxTrees.Length > treeIndex, "Compilation has enough trees");
            var tree = compilation.SyntaxTrees[treeIndex];

            const string bindStart = "/*<bind>*/";
            const string bindEnd = "/*</bind>*/";
            return FindBindingNode<TNode>(tree, bindStart, bindEnd);
        }

        /// <summary>
        /// Find multiple binding nodes by looking for pair /*&lt;bind#&gt;*/ &amp; /*&lt;/bind#&gt;*/ in source text
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="treeIndex">which tree</param>
        /// <param name="which">
        ///     * if which &lt; 0, find ALL wrapped nodes
        ///     * if which &gt;=0, find a specific binding node wrapped by /*&lt;bind#&gt;*/ &amp; /*&lt;/bind#&gt;*/
        ///       e.g. if which = 1, find node wrapped by /*&lt;bind1&gt;*/ &amp; /*&lt;/bind1&gt;*/
        /// </param>
        /// <returns></returns>
        public IList<TNode> GetBindingNodes<TNode>(CSharpCompilation compilation, int treeIndex = 0, int which = -1) where TNode : SyntaxNode
        {
            Assert.True(compilation.SyntaxTrees.Length > treeIndex, "Compilation has enough trees");
            var tree = compilation.SyntaxTrees[treeIndex];

            var nodeList = new List<TNode>();
            string text = tree.GetRoot().ToFullString();

            const string bindStartFmt = "/*<bind{0}>*/";
            const string bindEndFmt = "/*</bind{0}>*/";
            // find all
            if (which < 0)
            {
                // assume tags with number are in increasing order, no jump
                for (byte i = 0; i < 255; i++)
                {
                    var start = String.Format(bindStartFmt, i);
                    var end = String.Format(bindEndFmt, i);

                    var bindNode = FindBindingNode<TNode>(tree, start, end);
                    // done
                    if (bindNode == null)
                        break;

                    nodeList.Add(bindNode);
                }
            }
            else
            {
                var start2 = String.Format(bindStartFmt, which);
                var end2 = String.Format(bindEndFmt, which);

                var bindNode = FindBindingNode<TNode>(tree, start2, end2);
                // done
                if (bindNode != null)
                    nodeList.Add(bindNode);
            }

            return nodeList;
        }

        private static TNode FindBindingNode<TNode>(SyntaxTree tree, string startTag, string endTag) where TNode : SyntaxNode
        {
            // =================
            // Get Binding Text
            string text = tree.GetRoot().ToFullString();
            int start = text.IndexOf(startTag, StringComparison.Ordinal);
            if (start < 0)
                return null;

            start += startTag.Length;
            int end = text.IndexOf(endTag, StringComparison.Ordinal);
            Assert.True(end > start, "Bind Pos: end > start");
            // get rid of white spaces if any
            var bindText = text.Substring(start, end - start).Trim();
            if (String.IsNullOrWhiteSpace(bindText))
                return null;

            // =================
            // Get Binding Node
            var node = tree.GetRoot().FindToken(start).Parent;
            while ((node != null && node.ToString() != bindText))
            {
                node = node.Parent;
            }
            // =================
            // Get Binding Node with match node type
            if (node != null)
            {
                while ((node as TNode) == null)
                {
                    if (node.Parent != null && node.Parent.ToString() == bindText)
                    {
                        node = node.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            Assert.NotNull(node); // If this trips, then node  wasn't found
            Assert.IsAssignableFrom(typeof(TNode), node);
            Assert.Equal(bindText, node.ToString());
            return ((TNode)node);
        }
        #endregion

        #region Attributes

        internal IEnumerable<string> GetAttributeNames(ImmutableArray<SynthesizedAttributeData> attributes)
        {
            return attributes.Select(a => a.AttributeClass.Name);
        }

        internal IEnumerable<string> GetAttributeNames(ImmutableArray<CSharpAttributeData> attributes)
        {
            return attributes.Select(a => a.AttributeClass.Name);
        }

        internal IEnumerable<string> GetAttributeStrings(ImmutableArray<CSharpAttributeData> attributes)
        {
            return attributes.Select(a => a.ToString());
        }

        #endregion

        #region Documentation Comments

        internal static string GetDocumentationCommentText(CSharpCompilation compilation, params DiagnosticDescription[] expectedDiagnostics)
        {
            return GetDocumentationCommentText(compilation, outputName: null, filterTree: null, ensureEnglishUICulture: false, expectedDiagnostics: expectedDiagnostics);
        }

        internal static string GetDocumentationCommentText(CSharpCompilation compilation, bool ensureEnglishUICulture, params DiagnosticDescription[] expectedDiagnostics)
        {
            return GetDocumentationCommentText(compilation, outputName: null, filterTree: null, ensureEnglishUICulture: ensureEnglishUICulture, expectedDiagnostics: expectedDiagnostics);
        }

        internal static string GetDocumentationCommentText(CSharpCompilation compilation, string outputName = null, SyntaxTree filterTree = null, TextSpan? filterSpanWithinTree = null, bool ensureEnglishUICulture = false, params DiagnosticDescription[] expectedDiagnostics)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
                CultureInfo saveUICulture = null;

                if (ensureEnglishUICulture)
                {
                    var preferred = EnsureEnglishUICulture.PreferredOrNull;

                    if (preferred == null)
                    {
                        ensureEnglishUICulture = false;
                    }
                    else
                    {
                        saveUICulture = CultureInfo.CurrentUICulture;
                        CultureInfo.CurrentUICulture = preferred;
                    }
                }

                try
                {
                    DocumentationCommentCompiler.WriteDocumentationCommentXml(compilation, outputName, stream, diagnostics, default(CancellationToken), filterTree, filterSpanWithinTree);
                }
                finally
                {
                    if (ensureEnglishUICulture)
                    {
                        CultureInfo.CurrentUICulture = saveUICulture;
                    }
                }

                if (expectedDiagnostics != null)
                {
                    diagnostics.Verify(expectedDiagnostics);
                }
                diagnostics.Free();

                string text = Encoding.UTF8.GetString(stream.ToArray());
                int length = text.IndexOf('\0');
                if (length >= 0)
                {
                    text = text.Substring(0, length);
                }
                return text.Trim();
            }
        }

        #endregion

        #region IL Validation

        internal override string VisualizeRealIL(IModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string> markers)
        {
            return VisualizeRealIL((PEModuleSymbol)peModule, methodData, markers);
        }

        /// <summary>
        /// Returns a string representation of IL read from metadata.
        /// </summary>
        /// <remarks>
        /// Currently unsupported IL decoding:
        /// - multidimensional arrays
        /// - vararg calls
        /// - winmd
        /// - global methods
        /// </remarks>
        internal unsafe static string VisualizeRealIL(PEModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string> markers)
        {
            var typeName = GetContainingTypeMetadataName(methodData.Method);
            // TODO (tomat): global methods (typeName == null)

            var type = peModule.ContainingAssembly.GetTypeByMetadataName(typeName);

            // TODO (tomat): overloaded methods
            var method = (PEMethodSymbol)type.GetMembers(methodData.Method.MetadataName).Single();

            var bodyBlock = peModule.Module.GetMethodBodyOrThrow(method.Handle);
            Assert.NotNull(bodyBlock);

            var moduleDecoder = new MetadataDecoder(peModule);
            var peMethod = (PEMethodSymbol)moduleDecoder.GetSymbolForILToken(method.Handle);

            StringBuilder sb = new StringBuilder();
            var ilBytes = bodyBlock.GetILContent();

            var ehHandlerRegions = ILVisualizer.GetHandlerSpans(bodyBlock.ExceptionRegions);

            var methodDecoder = new MetadataDecoder(peModule, peMethod);

            ImmutableArray<ILVisualizer.LocalInfo> localDefinitions;
            if (!bodyBlock.LocalSignature.IsNil)
            {
                var signature = peModule.Module.MetadataReader.GetStandaloneSignature(bodyBlock.LocalSignature).Signature;
                var signatureReader = peModule.Module.GetMemoryReaderOrThrow(signature);
                var localInfos = methodDecoder.DecodeLocalSignatureOrThrow(ref signatureReader);
                localDefinitions = ToLocalDefinitions(localInfos, methodData.ILBuilder);
            }
            else
            {
                localDefinitions = ImmutableArray.Create<ILVisualizer.LocalInfo>();
            }

            // TODO (tomat): the .maxstack in IL can't be less than 8, but many tests expect .maxstack < 8
            int maxStack = (bodyBlock.MaxStack == 8 && methodData.ILBuilder.MaxStack < 8) ? methodData.ILBuilder.MaxStack : bodyBlock.MaxStack;

            var visualizer = new Visualizer(new MetadataDecoder(peModule, peMethod));

            visualizer.DumpMethod(sb, maxStack, ilBytes, localDefinitions, ehHandlerRegions, markers);

            return sb.ToString();
        }

        private static string GetContainingTypeMetadataName(IMethodSymbol method)
        {
            var type = method.ContainingType;
            if (type == null)
            {
                return null;
            }

            string ns = type.ContainingNamespace.MetadataName;
            var result = type.MetadataName;

            while ((type = type.ContainingType) != null)
            {
                result = type.MetadataName + "+" + result;
            }

            return (ns.Length > 0) ? ns + "." + result : result;
        }

        private static ImmutableArray<ILVisualizer.LocalInfo> ToLocalDefinitions(ImmutableArray<LocalInfo<TypeSymbol>> localInfos, ILBuilder builder)
        {
            if (localInfos.IsEmpty)
            {
                return ImmutableArray.Create<ILVisualizer.LocalInfo>();
            }

            var result = new ILVisualizer.LocalInfo[localInfos.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var typeRef = localInfos[i].Type;
                var builderLocal = builder.LocalSlotManager.LocalsInOrder()[i];
                result[i] = new ILVisualizer.LocalInfo(builderLocal.Name, typeRef, localInfos[i].IsPinned, localInfos[i].IsByRef);
            }

            return result.AsImmutableOrNull();
        }

        private sealed class Visualizer : ILVisualizer
        {
            private readonly MetadataDecoder _decoder;

            public Visualizer(MetadataDecoder decoder)
            {
                _decoder = decoder;
            }

            public override string VisualizeUserString(uint token)
            {
                var reader = _decoder.Module.GetMetadataReader();
                return "\"" + reader.GetUserString((UserStringHandle)MetadataTokens.Handle((int)token)) + "\"";
            }

            public override string VisualizeSymbol(uint token, OperandType operandType)
            {
                Cci.IReference reference = _decoder.GetSymbolForILToken(MetadataTokens.EntityHandle((int)token));
                return string.Format("\"{0}\"", (reference is ISymbol symbol) ? symbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat) : (object)reference);
            }

            public override string VisualizeLocalType(object type)
            {
                if (type is int)
                {
                    type = _decoder.GetSymbolForILToken(MetadataTokens.EntityHandle((int)type));
                }

                return (type is ISymbol symbol) ? symbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat) : type.ToString();
            }
        }

        #endregion

        #region IOperation tree validation

        protected static (IOperation operation, SyntaxNode node) GetOperationAndSyntaxForTest<TSyntaxNode>(CSharpCompilation compilation)
            where TSyntaxNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            SyntaxNode syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));
            if (syntaxNode == null)
            {
                return (null, null);
            }

            return (model.GetOperation(syntaxNode), syntaxNode);
        }

        protected static string GetOperationTreeForTest<TSyntaxNode>(CSharpCompilation compilation)
            where TSyntaxNode : SyntaxNode
        {
            var (operation, syntax) = GetOperationAndSyntaxForTest<TSyntaxNode>(compilation);
            return operation != null ? OperationTreeVerifier.GetOperationTree(compilation, operation) : null;
        }

        protected static string GetOperationTreeForTest(CSharpCompilation compilation, IOperation operation)
        {
            return operation != null ? OperationTreeVerifier.GetOperationTree(compilation, operation) : null;
        }

        protected static string GetOperationTreeForTest<TSyntaxNode>(
            string testSrc,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            var defaultRefs = useLatestFrameworkReferences ? s_latestOperationReferences : s_defaultOperationReferences;
            var compilation = CreateStandardCompilation(testSrc, defaultRefs, options: compilationOptions ?? TestOptions.ReleaseDll, parseOptions: parseOptions);
            return GetOperationTreeForTest<TSyntaxNode>(compilation);
        }

        protected static void VerifyOperationTreeForTest<TSyntaxNode>(CSharpCompilation compilation, string expectedOperationTree, Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var (actualOperation, syntaxNode) = GetOperationAndSyntaxForTest<TSyntaxNode>(compilation);
            var actualOperationTree = GetOperationTreeForTest(compilation, actualOperation);
            OperationTreeVerifier.Verify(expectedOperationTree, actualOperationTree);
            additionalOperationTreeVerifier?.Invoke(actualOperation, compilation, syntaxNode);
        }

        protected static void VerifyFlowGraphForTest<TSyntaxNode>(CSharpCompilation compilation, string expectedFlowGraph)
            where TSyntaxNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            SyntaxNode syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));

            ImmutableArray<Operations.BasicBlock> graph = model.GetControlFlowGraph((Operations.IBlockOperation)model.GetOperation(syntaxNode));
            var map = new Dictionary<Operations.BasicBlock, int>();

            for (int i = 0; i < graph.Length; i++)
            {
                map.Add(graph[i], i);
            }

            var visitor = TestOperationVisitor.GetInstance();
            var stringBuilder = PooledObjects.PooledStringBuilder.GetInstance();

            for (int i = 0; i < graph.Length; i++)
            {
                var block = graph[i];
                stringBuilder.Builder.AppendLine($"Block[{i}] - {block.Kind}");

                var predecessors = block.Predecessors;

                if (!predecessors.IsEmpty)
                {
                    stringBuilder.Builder.AppendLine($"    Predecessors ({predecessors.Count})");
                    foreach (int j in predecessors.Select(b => map[b]).OrderBy(ii => ii))
                    {
                        stringBuilder.Builder.AppendLine($"        [{j}]");
                    }
                }

                var statements = block.Statements;
                stringBuilder.Builder.AppendLine($"    Statements ({statements.Length})");
                foreach (var statement in statements)
                {
                    ValidateRoot(statement);
                    stringBuilder.Builder.AppendLine(OperationTreeVerifier.GetOperationTree(compilation, statement, initialIndent: 8));
                }

                if (block.Conditional.Condition != null)
                {
                    Assert.True(map.TryGetValue(block.Conditional.Destination, out int index));
                    stringBuilder.Builder.AppendLine($"    Jump if {block.Conditional.JumpIfTrue} to Block[{index}]");

                    IOperation condition = block.Conditional.Condition;
                    ValidateRoot(condition);
                    stringBuilder.Builder.AppendLine(OperationTreeVerifier.GetOperationTree(compilation, condition, initialIndent: 8));
                }

                void ValidateRoot(IOperation root)
                {
                    visitor.Visit(root);
                    Assert.Null(root.Parent);
                    Assert.Null(((Operation)root).SemanticModel);
                    Assert.True(CanBeInControlFlowGraph(root), $"Unexpected node kind OperationKind.{root.Kind}");

                    foreach (var operation in root.Descendants())
                    {
                        visitor.Visit(operation);
                        Assert.NotNull(operation.Parent);
                        Assert.Null(((Operation)operation).SemanticModel);
                        Assert.True(CanBeInControlFlowGraph(operation), $"Unexpected node kind OperationKind.{operation.Kind}");
                    }
                }

                if (block.Next != null)
                {
                    Assert.True(map.TryGetValue(block.Next, out var index));
                    stringBuilder.Builder.AppendLine($"    Next Block[{index}]");
                }
            }

            var actualFlowGraph = stringBuilder.ToStringAndFree();
            OperationTreeVerifier.Verify(expectedFlowGraph, actualFlowGraph);
        }

        private static bool CanBeInControlFlowGraph(IOperation n)
        {
            switch (n.Kind)
            {
                case OperationKind.Block:
                case OperationKind.Switch:
                case OperationKind.Loop: 
                case OperationKind.Labeled:
                case OperationKind.Branch:
                case OperationKind.Lock:
                case OperationKind.Try:
                case OperationKind.Using:
                case OperationKind.Conditional:
                case OperationKind.Coalesce:
                case OperationKind.ConditionalAccess:
                case OperationKind.ConditionalAccessInstance:
                case OperationKind.ObjectOrCollectionInitializer:
                case OperationKind.MemberInitializer:
                case OperationKind.CollectionElementInitializer:
                case OperationKind.FieldInitializer:
                case OperationKind.PropertyInitializer:
                case OperationKind.ParameterInitializer:
                case OperationKind.ArrayInitializer:
                case OperationKind.CatchClause:
                case OperationKind.SwitchCase:
                case OperationKind.CaseClause:
                    return false;

                case OperationKind.VariableDeclarationGroup:
                case OperationKind.VariableInitializer:
                    return true; // PROTOTYPE(dataflow): should be translated into assignments

                case OperationKind.BinaryOperator:
                    var binary = (IBinaryOperation)n;
                    return binary.OperatorKind != Operations.BinaryOperatorKind.ConditionalAnd && binary.OperatorKind != Operations.BinaryOperatorKind.ConditionalOr;

                case OperationKind.None:
                case OperationKind.Invalid:
                case OperationKind.Empty:
                case OperationKind.Return:
                case OperationKind.YieldBreak:
                case OperationKind.YieldReturn:
                case OperationKind.ExpressionStatement:
                case OperationKind.LocalFunction:
                case OperationKind.Stop:
                case OperationKind.End:
                case OperationKind.RaiseEvent:
                case OperationKind.Literal:
                case OperationKind.Conversion:
                case OperationKind.Invocation:
                case OperationKind.ArrayElementReference:
                case OperationKind.LocalReference:
                case OperationKind.ParameterReference:
                case OperationKind.FieldReference:
                case OperationKind.MethodReference:
                case OperationKind.PropertyReference:
                case OperationKind.EventReference:
                case OperationKind.AnonymousFunction:
                case OperationKind.ObjectCreation:
                case OperationKind.TypeParameterObjectCreation:
                case OperationKind.ArrayCreation:
                case OperationKind.InstanceReference:
                case OperationKind.IsType:
                case OperationKind.Await:
                case OperationKind.SimpleAssignment:
                case OperationKind.CompoundAssignment:
                case OperationKind.Parenthesized:
                case OperationKind.EventAssignment:
                case OperationKind.InterpolatedString:
                case OperationKind.AnonymousObjectCreation:
                case OperationKind.NameOf:
                case OperationKind.Tuple:
                case OperationKind.DynamicObjectCreation:
                case OperationKind.DynamicMemberReference:
                case OperationKind.DynamicInvocation:
                case OperationKind.DynamicIndexerAccess:
                case OperationKind.TranslatedQuery:
                case OperationKind.DelegateCreation:
                case OperationKind.DefaultValue:
                case OperationKind.TypeOf:
                case OperationKind.SizeOf:
                case OperationKind.AddressOf:
                case OperationKind.IsPattern:
                case OperationKind.Increment:
                case OperationKind.Throw:
                case OperationKind.Decrement:
                case OperationKind.DeconstructionAssignment:
                case OperationKind.DeclarationExpression:
                case OperationKind.OmittedArgument:
                case OperationKind.VariableDeclarator:
                case OperationKind.VariableDeclaration:
                case OperationKind.Argument:
                case OperationKind.InterpolatedStringText:
                case OperationKind.Interpolation:
                case OperationKind.ConstantPattern:
                case OperationKind.DeclarationPattern:
                case OperationKind.UnaryOperator:
                case OperationKind.FlowCapture:
                case OperationKind.FlowCaptureReference:
                    return true;
            }

            Assert.True(false, $"Unhandled node kind OperationKind.{n.Kind}");
            return false;
        }

        protected static void VerifyOperationTreeForTest<TSyntaxNode>(
            string testSrc,
            string expectedOperationTree,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            var actualOperationTree = GetOperationTreeForTest<TSyntaxNode>(testSrc, compilationOptions, parseOptions, useLatestFrameworkReferences);
            OperationTreeVerifier.Verify(expectedOperationTree, actualOperationTree);
        }

        protected static void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
            CSharpCompilation compilation,
            string expectedOperationTree,
            DiagnosticDescription[] expectedDiagnostics,
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var actualDiagnostics = compilation.GetDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Hidden);
            actualDiagnostics.Verify(expectedDiagnostics);
            VerifyOperationTreeForTest<TSyntaxNode>(compilation, expectedOperationTree, additionalOperationTreeVerifier);
        }

        protected static void VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(
            CSharpCompilation compilation,
            string expectedFlowGraph,
            DiagnosticDescription[] expectedDiagnostics)
            where TSyntaxNode : SyntaxNode
        {
            var actualDiagnostics = compilation.GetDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Hidden);
            actualDiagnostics.Verify(expectedDiagnostics);
            VerifyFlowGraphForTest<TSyntaxNode>(compilation, expectedFlowGraph);
        }

        private static readonly MetadataReference[] s_defaultOperationReferences = new[] { SystemRef, SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef };
        private static readonly MetadataReference[] s_latestOperationReferences = new[] { SystemRef, SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef, MscorlibRef_v46 };

        protected static void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
            string testSrc,
            string expectedOperationTree,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] additionalReferences = null,
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            var defaultRefs = useLatestFrameworkReferences ? s_latestOperationReferences : s_defaultOperationReferences;
            var references = additionalReferences == null ? defaultRefs : additionalReferences.Concat(defaultRefs);
            var compilation = CreateStandardCompilation(testSrc, references, sourceFileName: "file.cs", options: compilationOptions ?? TestOptions.ReleaseDll, parseOptions: parseOptions);
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(compilation, expectedOperationTree, expectedDiagnostics, additionalOperationTreeVerifier);
        }

        protected static void VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(
            string testSrc,
            string expectedFlowGraph,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] additionalReferences = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            var defaultRefs = useLatestFrameworkReferences ? s_latestOperationReferences : s_defaultOperationReferences;
            var references = additionalReferences == null ? defaultRefs : additionalReferences.Concat(defaultRefs);
            parseOptions = parseOptions?.WithFlowAnalysisFeature() ?? TestOptions.RegularWithFlowAnalysisFeature;
            var compilation = CreateStandardCompilation(testSrc, references, sourceFileName: "file.cs", options: compilationOptions ?? TestOptions.ReleaseDll, parseOptions: parseOptions);
            VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(compilation, expectedFlowGraph, expectedDiagnostics);
        }

        protected static MetadataReference VerifyOperationTreeAndDiagnosticsForTestWithIL<TSyntaxNode>(string testSrc,
            string ilSource,
            string expectedOperationTree,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] additionalReferences = null,
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            var ilReference = CreateMetadataReferenceFromIlSource(ilSource);
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(testSrc, expectedOperationTree, expectedDiagnostics, compilationOptions, parseOptions, new[] { ilReference }, additionalOperationTreeVerifier, useLatestFrameworkReferences);
            return ilReference;
        }

        #endregion

        #region Span

        protected static CSharpCompilation CreateCompilationWithMscorlibAndSpan(string text, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
        {
            var reference = CreateCompilation(
                spanSource,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: TestOptions.UnsafeReleaseDll);

            reference.VerifyDiagnostics();

            var comp = CreateCompilation(
                text,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef, reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);


            return comp;
        }

        protected static CSharpCompilation CreateCompilationWithMscorlibAndSpanSrc(string text, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
        {
            var textWitSpan = new string[] { text, spanSource };
            var comp = CreateCompilation(
                textWitSpan,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: options ?? TestOptions.UnsafeReleaseDll,
                parseOptions: parseOptions);

            return comp;
        }

        private static string spanSource = @"
namespace System
    {
        public readonly ref struct Span<T>
        {
            private readonly T[] arr;

            public ref T this[int i] => ref arr[i];
            public override int GetHashCode() => 1;
            public int Length { get; }

            unsafe public Span(void* pointer, int length)
            {
                this.arr = null;
                this.Length = length;
            }

            public Span(T[] arr)
            {
                this.arr = arr;
                this.Length = arr.Length;
            }

            public void CopyTo(Span<T> other) { }

            /// <summary>Gets an enumerator for this span.</summary>
            public Enumerator GetEnumerator() => new Enumerator(this);

            /// <summary>Enumerates the elements of a <see cref=""Span{T}""/>.</summary>
            public ref struct Enumerator
            {
                /// <summary>The span being enumerated.</summary>
                private readonly Span<T> _span;
                /// <summary>The next index to yield.</summary>
                private int _index;

                /// <summary>Initialize the enumerator.</summary>
                /// <param name=""span"">The span to enumerate.</param>
                internal Enumerator(Span<T> span)
                {
                    _span = span;
                    _index = -1;
                }

                /// <summary>Advances the enumerator to the next element of the span.</summary>
                public bool MoveNext()
                {
                    int index = _index + 1;
                    if (index < _span.Length)
                    {
                        _index = index;
                        return true;
                    }

                    return false;
                }

                /// <summary>Gets the element at the current position of the enumerator.</summary>
                public ref T Current
                {
                    get => ref _span[_index];
                }
            }
        }

        public readonly ref struct ReadOnlySpan<T>
        {
            private readonly T[] arr;

            public ref readonly T this[int i] => ref arr[i];
            public override int GetHashCode() => 2;
            public int Length { get; }

            public ReadOnlySpan(T[] arr)
            {
                this.arr = arr;
                this.Length = arr.Length;
            }

            public void CopyTo(Span<T> other) { }

            /// <summary>Gets an enumerator for this span.</summary>
            public Enumerator GetEnumerator() => new Enumerator(this);

            /// <summary>Enumerates the elements of a <see cref=""Span{T}""/>.</summary>
            public ref struct Enumerator
            {
                /// <summary>The span being enumerated.</summary>
                private readonly ReadOnlySpan<T> _span;
                /// <summary>The next index to yield.</summary>
                private int _index;

                /// <summary>Initialize the enumerator.</summary>
                /// <param name=""span"">The span to enumerate.</param>
                internal Enumerator(ReadOnlySpan<T> span)
                {
                    _span = span;
                    _index = -1;
                }

                /// <summary>Advances the enumerator to the next element of the span.</summary>
                public bool MoveNext()
                {
                    int index = _index + 1;
                    if (index < _span.Length)
                    {
                        _index = index;
                        return true;
                    }

                    return false;
                }

                /// <summary>Gets the element at the current position of the enumerator.</summary>
                public ref readonly T Current
                {
                    get => ref _span[_index];
                }
            }
        }

        public readonly ref struct SpanLike<T>
        {
            public readonly Span<T> field;
        }
    }";
        #endregion
    }
}
