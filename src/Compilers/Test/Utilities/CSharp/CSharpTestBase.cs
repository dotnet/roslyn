// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;
using Roslyn.Test.Utilities;
using Roslyn.Test.MetadataUtilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        internal new IEnumerable<ModuleSymbol> ReferencesToModuleSymbols(IEnumerable<MetadataReference> references, MetadataImportOptions importOptions = MetadataImportOptions.Public)
        {
            return base.ReferencesToModuleSymbols(references, importOptions).Cast<ModuleSymbol>();
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

        internal ICompilationVerifier CompileAndVerify(
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
            bool verify = true)
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
                verify: verify);
        }

        internal ICompilationVerifier CompileAndVerify(
            string[] sources,
            MetadataReference[] additionalRefs = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> validator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            CompilationOptions options = null,
            ParseOptions parseOptions = null,
            bool verify = true)
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
                options,
                parseOptions,
                verify);
        }

        internal ICompilationVerifier CompileAndVerifyExperimental(
            string source,
            string expectedOutput = null,
            MetadataReference[] additionalRefs = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            bool verify = true)
        {
            var options = (expectedOutput != null) ? TestOptions.ReleaseExe : TestOptions.ReleaseDll;

            var compilation = CreateExperimentalCompilationWithMscorlib45(source, additionalRefs, options);

            return CompileAndVerify(
                compilation: compilation,
                dependencies: dependencies,
                sourceSymbolValidator: Translate2(sourceSymbolValidator),
                assemblyValidator: assemblyValidator,
                symbolValidator: Translate2(symbolValidator),
                expectedSignatures: expectedSignatures,
                expectedOutput: expectedOutput,
                verify: verify);
        }

        internal ICompilationVerifier CompileAndVerifyWinRt(
            string source,
            string expectedOutput = null,
            MetadataReference[] additionalRefs = null,
            CSharpCompilationOptions options = null,
            bool verify = true)
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

        internal ICompilationVerifier CompileAndVerify(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> validator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            bool verify = true)
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

        internal override IEnumerable<IModuleSymbol> ReferencesToModuleSymbols(IEnumerable<MetadataReference> references, MetadataImportOptions importOptions = MetadataImportOptions.Public)
        {
            var options = TestOptions.ReleaseDll.WithMetadataImportOptions(importOptions);
            var tc1 = CSharpCompilation.Create("Dummy", new SyntaxTree[0], references, options);
            return references.Select(r =>
            {
                if (r.Properties.Kind == MetadataImageKind.Assembly)
                {
                    var assemblySymbol = tc1.GetReferencedAssemblySymbol(r);
                    return (object)assemblySymbol == null ? null : assemblySymbol.Modules[0];
                }
                else
                {
                    return tc1.GetReferencedModuleSymbol(r);
                }
            });
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
            return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
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

            return CreateCompilationWithMscorlib(source, metadataReferences, options);
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

        public static CSharpCompilation CreateCompilationWithMscorlib(
            string text,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "")
        {
            return CreateCompilationWithMscorlib(
                new[] { Parse(text, sourceFileName, parseOptions) },
                references: references,
                options: options,
                assemblyName: assemblyName);
        }

        public static CSharpCompilation CreateExperimentalCompilationWithMscorlib45(
            string text,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "",
            string sourceFileName = "")
        {
            var refs = new List<MetadataReference>();
            if (references != null)
            {
                refs.AddRange(references);
            }
            refs.Add(MscorlibRef_v4_0_30316_17626);
            return CreateCompilation(new[] { Parse(text, sourceFileName, TestOptions.ExperimentalParseOptions) }, refs, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib45AndCSruntime(
            string text,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null)
        {
            var refs = new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef };

            return CreateCompilation(new[] { Parse(text, options: parseOptions) }, refs, options);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib(
            IEnumerable<string> sources,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "")
        {
            return CreateCompilationWithMscorlib(Parse(sources, parseOptions), references, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib(
            SyntaxTree syntaxTree,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            return CreateCompilationWithMscorlib(new SyntaxTree[] { syntaxTree }, references, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib(
            IEnumerable<SyntaxTree> trees,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            return CreateCompilation(trees, (references != null) ? new[] { MscorlibRef }.Concat(references) : new[] { MscorlibRef }, options, assemblyName);
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
            references = (references != null) ? new[] { SystemCoreRef }.Concat(references) : new[] { SystemCoreRef };

            return CreateCompilationWithMscorlib(
                new[] { Parse(text, "", parseOptions) },
                references: references,
                options: options,
                assemblyName: assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithMscorlibAndDocumentationComments(
            string text,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "Test")
        {
            return CreateCompilationWithMscorlib(
                new[] { Parse(text, options: TestOptions.RegularWithDocumentationComments) },
                references: references,
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

#if Test_IOperation_Interface
            // Create a compilation for the purpose of verifying operation tree only,
            // so this won't interfere with test.
            var compilationForOperationWalking = CSharpCompilation.Create(
                assemblyName == "" ? GetUniqueName() : assemblyName,
                trees,
                references,
                options);
            WalkOperationTree(compilationForOperationWalking);
#endif
            return CSharpCompilation.Create(
                assemblyName == "" ? GetUniqueName() : assemblyName,
                trees,
                references,
                options);
        }

        public static CSharpCompilation CreateCompilation(
            AssemblyIdentity identity,
            string[] sources,
            MetadataReference[] references,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null)
        {
            var trees = (sources == null) ? null : sources.Select(s => Parse(s, options: parseOptions)).ToArray();
            var compilationOptions = options ?? TestOptions.ReleaseDll;

#if Test_IOperation_Interface
            // Create a compilation for the purpose of verifying operation tree only,
            // so this won't interfere with test.
            var compilationForOperationWalking = CSharpCompilation.Create(
                identity.Name,
                options: compilationOptions,
                references: references,
                syntaxTrees: trees);
            WalkOperationTree(compilationForOperationWalking);
#endif

            var c = CSharpCompilation.Create(identity.Name, options: compilationOptions, references: references, syntaxTrees: trees);
            Assert.NotNull(c.Assembly); // force creation of SourceAssemblySymbol

            ((SourceAssemblySymbol)c.Assembly).lazyAssemblyIdentity = identity;
            return c;
        }

#if Test_IOperation_Interface
        private static void WalkOperationTree(CSharpCompilation compilation)
        {
            var operationWalker = TestOperationWalker.GetInstance();

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();

                // TODO: check other operation root as well (property, etc.)
                foreach (BaseMethodDeclarationSyntax methodNode in root.DescendantNodesAndSelf().OfType<BaseMethodDeclarationSyntax>())
                {
                    var bodyNode = methodNode.Body;
                    if (bodyNode != null)
                    {
                        var operation = semanticModel.GetOperation(bodyNode);
                        operationWalker.Visit(operation);
                    }
                }
            }
        }
#endif

        public static CSharpCompilation CreateSubmissionWithExactReferences(
           string code,
           IEnumerable<MetadataReference> references = null,
           CSharpCompilationOptions options = null,
           CSharpParseOptions parseOptions = null,
           CSharpCompilation previous = null,
           Type returnType = null,
           Type hostObjectType = null)
        {
            return CSharpCompilation.CreateScriptCompilation(
                GetUniqueName(),
                references: references,
                options: options,
                syntaxTree: Parse(code, options: parseOptions ?? TestOptions.Script),
                previousScriptCompilation: previous,
                returnType: returnType,
                globalsType: hostObjectType);
        }

        public static CSharpCompilation CreateSubmission(
           string code,
           IEnumerable<MetadataReference> references = null,
           CSharpCompilationOptions options = null,
           CSharpParseOptions parseOptions = null,
           CSharpCompilation previous = null,
           Type returnType = null,
           Type hostObjectType = null)
        {
            return CSharpCompilation.CreateScriptCompilation(
                GetUniqueName(),
                references: (references != null) ? new[] { MscorlibRef_v4_0_30316_17626 }.Concat(references) : new[] { MscorlibRef_v4_0_30316_17626 },
                options: options,
                syntaxTree: Parse(code, options: parseOptions ?? TestOptions.Script),
                previousScriptCompilation: previous,
                returnType: returnType,
                globalsType: hostObjectType);
        }

        protected override Compilation GetCompilationForEmit(
            IEnumerable<string> source,
            IEnumerable<MetadataReference> additionalRefs,
            CompilationOptions options,
            ParseOptions parseOptions)
        {
            return CreateCompilationWithMscorlib(
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
        internal ICompilationVerifier CompileAndVerifyException<T>(string source, string expectedMessage = null, bool allowUnsafe = false) where T : Exception
        {
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe));
            return CompileAndVerifyException<T>(comp, expectedMessage);
        }

        internal ICompilationVerifier CompileAndVerifyException<T>(CSharpCompilation comp, string expectedMessage = null) where T : Exception
        {
            try
            {
                CompileAndVerify(comp, expectedOutput: ""); //need expected output to force execution
                Assert.False(true, string.Format("Expected exception {0}({1})", typeof(T).Name, expectedMessage));
            }
            catch (Exception x)
            {
                var e = x.InnerException;
                Assert.IsType<T>(e);
                if (expectedMessage != null)
                {
                    Assert.Equal(expectedMessage, e.Message);
                }
            }

            return CompileAndVerify(comp);
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

                var ensureObject = ensureEnglishUICulture ? new EnsureEnglishUICulture() : null;
                using (ensureObject)
                {
                    DocumentationCommentCompiler.WriteDocumentationCommentXml(compilation, outputName, stream, diagnostics, default(CancellationToken), filterTree, filterSpanWithinTree);
                }

                if (expectedDiagnostics != null)
                {
                    diagnostics.Verify(expectedDiagnostics);
                }
                diagnostics.Free();

                byte[] buffer;
                ArraySegment<byte> bufferSegment;
                if (stream.TryGetBuffer(out bufferSegment) &&
                    bufferSegment.Count == bufferSegment.Array.Length)
                {
                    buffer = bufferSegment.Array;
                }
                else
                {
                    buffer = stream.ToArray();
                }

                string text = Encoding.UTF8.GetString(buffer);
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

            var ehHandlerRegions = Visualizer.GetHandlerSpans(bodyBlock.ExceptionRegions);

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

            public override string VisualizeSymbol(uint token)
            {
                Cci.IReference reference = _decoder.GetSymbolForILToken(MetadataTokens.EntityHandle((int)token));
                ISymbol symbol = reference as ISymbol;
                return string.Format("\"{0}\"", symbol == null ? (object)reference : symbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat));
            }

            public override string VisualizeLocalType(object type)
            {
                if (type is int)
                {
                    type = _decoder.GetSymbolForILToken(MetadataTokens.EntityHandle((int)type));
                }

                ISymbol symbol = type as ISymbol;
                return symbol == null ? type.ToString() : symbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
            }
        }

        #endregion
    }
}
