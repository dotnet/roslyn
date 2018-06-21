// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public abstract class CSharpTestBase : CommonTestBase
    {
        internal CompilationVerifier CompileAndVerifyWithMscorlib40(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            Verification verify = Verification.Passes) => 
            CompileAndVerify(
                source,
                references,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                TargetFramework.Mscorlib40,
                verify);

        internal CompilationVerifier CompileAndVerifyWithMscorlib46(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            Verification verify = Verification.Passes) => 
            CompileAndVerify(
                source,
                references,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                TargetFramework.Mscorlib46,
                verify);

        internal CompilationVerifier CompileAndVerifyWithWinRt(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            Verification verify = Verification.Passes) => 
            CompileAndVerify(
                source,
                references,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                TargetFramework.WinRT,
                verify);

        internal CompilationVerifier CompileAndVerifyWithCSharp(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            Verification verify = Verification.Passes) => 
            CompileAndVerify(
                source,
                references,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                TargetFramework.StandardAndCSharp,
                verify);

        internal CompilationVerifier CompileAndVerify(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            TargetFramework targetFramework = TargetFramework.Standard,
            Verification verify = Verification.Passes)
        {
            options = options ?? TestOptions.ReleaseDll.WithOutputKind((expectedOutput != null) ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary);
            var compilation = CreateCompilation(source, references, options, parseOptions, targetFramework, assemblyName: GetUniqueName());
            return CompileAndVerify(
                compilation,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                expectedReturnCode,
                args,
                emitOptions,
                verify);
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
            Action<IModuleSymbol> translate(Action<ModuleSymbol> action)
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

            return CompileAndVerifyCommon(
                compilation,
                manifestResources,
                dependencies,
                translate(sourceSymbolValidator),
                validator,
                translate(symbolValidator),
                expectedSignatures,
                expectedOutput,
                expectedReturnCode,
                args,
                emitOptions,
                verify);
        }

        internal CompilationVerifier CompileAndVerifyFieldMarshal(CSharpTestSource source, Dictionary<string, byte[]> expectedBlobs, bool isField = true) =>
            CompileAndVerifyFieldMarshal(
                source,
                (s, _) =>
                {
                    Assert.True(expectedBlobs.ContainsKey(s), "Expecting marshalling blob for " + (isField ? "field " : "parameter ") + s);
                    return expectedBlobs[s];
                },
                isField);

        internal CompilationVerifier CompileAndVerifyFieldMarshal(CSharpTestSource source, Func<string, PEAssembly, byte[]> getExpectedBlob, bool isField = true) =>
            CompileAndVerifyFieldMarshalCommon(
                CreateCompilationWithMscorlib40(source),
                getExpectedBlob,
                isField);

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

        public static CSharpCompilation CreateCompilationWithIL(
            CSharpTestSource source,
            string ilSource,
            TargetFramework targetFramework = TargetFramework.Standard,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            bool appendDefaultHeader = true) => CreateCompilationWithILAndMscorlib40(source, ilSource, targetFramework, references, options, appendDefaultHeader);

        public static CSharpCompilation CreateCompilationWithILAndMscorlib40(
            CSharpTestSource source,
            string ilSource,
            TargetFramework targetFramework = TargetFramework.Mscorlib40,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            bool appendDefaultHeader = true)
        {
            MetadataReference ilReference = CompileIL(ilSource, appendDefaultHeader);
            var allReferences = TargetFrameworkUtil.GetReferences(targetFramework, references).Add(ilReference);
            return CreateEmptyCompilation(source, allReferences, options);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib40(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib45(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib45, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib46(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib46, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithWinRT(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.WinRT, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib45AndCSharp(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib45AndCSharp, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib40AndSystemCore(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40AndSystemCore, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithCSharp(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.StandardAndCSharp, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib40AndDocumentationComments(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "")
        {
            parseOptions = parseOptions != null ? parseOptions.WithDocumentationMode(DocumentationMode.Diagnose) : TestOptions.RegularWithDocumentationComments;
            options = (options ?? TestOptions.ReleaseDll).WithXmlReferenceResolver(XmlFileResolver.Default);
            return CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40, assemblyName, sourceFileName);
        }

        public static CSharpCompilation CreateCompilation(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            TargetFramework targetFramework = TargetFramework.Standard,
            string assemblyName = "",
            string sourceFileName = "") => CreateEmptyCompilation(source, TargetFrameworkUtil.GetReferences(targetFramework, references), options, parseOptions, assemblyName, sourceFileName);

        public static CSharpCompilation CreateEmptyCompilation(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "")
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
                source.GetSyntaxTrees(parseOptions, sourceFileName),
                references,
                options);
            CompilationExtensions.ValidateIOperations(createCompilationLambda);
            return createCompilationLambda();
        }

        public static CSharpCompilation CreateCompilation(
            AssemblyIdentity identity,
            string[] source,
            MetadataReference[] references,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null)
        {
            var trees = (source == null) ? null : source.Select(s => Parse(s, options: parseOptions)).ToArray();
            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.Create(identity.Name, options: options ?? TestOptions.ReleaseDll, references: references, syntaxTrees: trees);

            CompilationExtensions.ValidateIOperations(createCompilationLambda);
            var c = createCompilationLambda();
            Assert.NotNull(c.Assembly); // force creation of SourceAssemblySymbol

            ((SourceAssemblySymbol)c.Assembly).lazyAssemblyIdentity = identity;
            return c;
        }

        public static CSharpCompilation CreateSubmissionWithExactReferences(
           string source,
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
                syntaxTree: Parse(source, options: parseOptions ?? TestOptions.Script),
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

        public CompilationVerifier CompileWithCustomILSource(string cSharpSource, string ilSource, Action<CSharpCompilation> compilationVerifier = null, bool importInternals = true, string expectedOutput = null, TargetFramework targetFramework = TargetFramework.Standard)
        {
            var compilationOptions = (expectedOutput != null) ? TestOptions.ReleaseExe : TestOptions.ReleaseDll;

            if (importInternals)
            {
                compilationOptions = compilationOptions.WithMetadataImportOptions(MetadataImportOptions.Internal);
            }

            if (ilSource == null)
            {
                var c = CreateCompilation(cSharpSource, options: compilationOptions, targetFramework: targetFramework);
                return CompileAndVerify(c, expectedOutput: expectedOutput);
            }

            MetadataReference reference = CreateMetadataReferenceFromIlSource(ilSource);

            var compilation = CreateCompilation(cSharpSource, new[] { reference }, compilationOptions, targetFramework: targetFramework);
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

        /// <summary>
        /// Like CompileAndVerify, but confirms that execution raises an exception.
        /// </summary>
        /// <typeparam name="T">Expected type of the exception.</typeparam>
        /// <param name="source">Program to compile and execute.</param>
        /// <param name="expectedMessage">Ignored if null.</param>
        internal CompilationVerifier CompileAndVerifyException<T>(string source, string expectedMessage = null, bool allowUnsafe = false, Verification verify = Verification.Passes) where T : Exception
        {
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe));
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
            return GetDocumentationCommentText(compilation, outputName: null, filterTree: null, ensureEnglishUICulture: true, expectedDiagnostics: expectedDiagnostics);
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
            var targetFramework = useLatestFrameworkReferences ? TargetFramework.Mscorlib46Extended : TargetFramework.Standard;
            var compilation = CreateCompilation(testSrc, targetFramework: targetFramework, options: compilationOptions ?? TestOptions.ReleaseDll, parseOptions: parseOptions);
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
            SyntaxNode syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));
            VerifyFlowGraph(compilation, syntaxNode, expectedFlowGraph);
        }

        protected static void VerifyFlowGraph(CSharpCompilation compilation, SyntaxNode syntaxNode, string expectedFlowGraph)
        {
            var model = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
            ControlFlowGraph graph = ControlFlowGraphVerifier.GetControlFlowGraph(syntaxNode, model);
            ControlFlowGraphVerifier.VerifyGraph(compilation, expectedFlowGraph, graph);
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

        protected static void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
            string testSrc,
            string expectedOperationTree,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null,
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode =>
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
                testSrc,
                expectedOperationTree,
                useLatestFrameworkReferences ? TargetFramework.Mscorlib46Extended : TargetFramework.Standard,
                expectedDiagnostics,
                compilationOptions,
                parseOptions,
                references,
                additionalOperationTreeVerifier);

        protected static void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
            string testSrc,
            string expectedOperationTree,
            TargetFramework targetFramework,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null,
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var compilation = CreateCompilation(
                new[] { Parse(testSrc, filename: "file.cs", options: parseOptions) },
                references,
                options: compilationOptions ?? TestOptions.ReleaseDll,
                targetFramework: targetFramework);
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(compilation, expectedOperationTree, expectedDiagnostics, additionalOperationTreeVerifier);
        }

        protected static void VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(
            string testSrc,
            string expectedFlowGraph,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(
                testSrc,
                expectedFlowGraph,
                expectedDiagnostics,
                targetFramework: useLatestFrameworkReferences ? TargetFramework.Mscorlib46Extended : TargetFramework.Standard,
                compilationOptions,
                parseOptions,
                references);
        }

        protected static void VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(
            string testSrc,
            string expectedFlowGraph,
            DiagnosticDescription[] expectedDiagnostics,
            TargetFramework targetFramework,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null)
            where TSyntaxNode : SyntaxNode
        {
            parseOptions = parseOptions?.WithFlowAnalysisFeature() ?? TestOptions.RegularWithFlowAnalysisFeature;
            var compilation = CreateCompilation(
                new[] { Parse(testSrc, filename: "file.cs", options: parseOptions) },
                references,
                options: compilationOptions ?? TestOptions.ReleaseDll,
                targetFramework: targetFramework);
            VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(compilation, expectedFlowGraph, expectedDiagnostics);
        }

        protected static MetadataReference VerifyOperationTreeAndDiagnosticsForTestWithIL<TSyntaxNode>(string testSrc,
            string ilSource,
            string expectedOperationTree,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null,
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
            var reference = CreateEmptyCompilation(
                SpanSource,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: TestOptions.UnsafeReleaseDll);

            reference.VerifyDiagnostics();

            var comp = CreateEmptyCompilation(
                text,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef, reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);


            return comp;
        }

        protected static CSharpCompilation CreateCompilationWithMscorlibAndSpanSrc(string text, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
        {
            var textWitSpan = new string[] { text, SpanSource };
            var comp = CreateEmptyCompilation(
                textWitSpan,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: options ?? TestOptions.UnsafeReleaseDll,
                parseOptions: parseOptions);

            return comp;
        }

        protected static readonly string SpanSource = @"
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
                this.arr = Helpers.ToArray<T>(pointer, length);
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

            public static implicit operator Span<T>(T[] array) => new Span<T>(array);
        }

        public readonly ref struct ReadOnlySpan<T>
        {
            private readonly T[] arr;

            public ref readonly T this[int i] => ref arr[i];
            public override int GetHashCode() => 2;
            public int Length { get; }

            unsafe public ReadOnlySpan(void* pointer, int length)
            {
                this.arr = Helpers.ToArray<T>(pointer, length);
                this.Length = length;
            }

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

            public static implicit operator ReadOnlySpan<T>(T[] array) => array == null ? default : new ReadOnlySpan<T>(array);

            public static implicit operator ReadOnlySpan<T>(string stringValue) => string.IsNullOrEmpty(stringValue) ? default : new ReadOnlySpan<T>((T[])(object)stringValue.ToCharArray());
        }

        public readonly ref struct SpanLike<T>
        {
            public readonly Span<T> field;
        }

        public enum Color: sbyte
        {
            Red,
            Green,
            Blue
        }

        public static unsafe class Helpers
        {
            public static T[] ToArray<T>(void* ptr, int count)
            {
                if (ptr == null)
                {
                    return null;
                }

                if (typeof(T) == typeof(int))
                {
                    var arr = new int[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((int*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(byte))
                {
                    var arr = new byte[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((byte*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(char))
                {
                    var arr = new char[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((char*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(Color))
                {
                    var arr = new Color[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((Color*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                throw new Exception(""add a case for: "" + typeof(T));
            }
        }
    }";
#endregion
    }
}
