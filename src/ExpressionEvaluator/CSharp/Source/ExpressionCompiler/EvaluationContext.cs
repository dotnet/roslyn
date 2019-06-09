// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EvaluationContext : EvaluationContextBase
    {
        private const string TypeName = "<>x";
        private const string MethodName = "<>m0";
        internal const bool IsLocalScopeEndInclusive = false;

        internal readonly MethodContextReuseConstraints? MethodContextReuseConstraints;
        internal readonly CSharpCompilation Compilation;

        private readonly MethodSymbol _currentFrame;
        private readonly MethodSymbol _currentSourceMethod;
        private readonly ImmutableArray<LocalSymbol> _locals;
        private readonly ImmutableSortedSet<int> _inScopeHoistedLocalSlots;
        private readonly MethodDebugInfo<TypeSymbol, LocalSymbol> _methodDebugInfo;

        private EvaluationContext(
            MethodContextReuseConstraints? methodContextReuseConstraints,
            CSharpCompilation compilation,
            MethodSymbol currentFrame,
            MethodSymbol currentSourceMethod,
            ImmutableArray<LocalSymbol> locals,
            ImmutableSortedSet<int> inScopeHoistedLocalSlots,
            MethodDebugInfo<TypeSymbol, LocalSymbol> methodDebugInfo)
        {
            Debug.Assert(inScopeHoistedLocalSlots != null);
            Debug.Assert(methodDebugInfo != null);

            this.MethodContextReuseConstraints = methodContextReuseConstraints;
            this.Compilation = compilation;
            _currentFrame = currentFrame;
            _currentSourceMethod = currentSourceMethod;
            _locals = locals;
            _inScopeHoistedLocalSlots = inScopeHoistedLocalSlots;
            _methodDebugInfo = methodDebugInfo;
        }

        /// <summary>
        /// Create a context for evaluating expressions at a type scope.
        /// </summary>
        /// <param name="compilation">Compilation.</param>
        /// <param name="moduleVersionId">Module containing type</param>
        /// <param name="typeToken">Type metadata token</param>
        /// <returns>Evaluation context</returns>
        /// <remarks>
        /// No locals since locals are associated with methods, not types.
        /// </remarks>
        internal static EvaluationContext CreateTypeContext(
            CSharpCompilation compilation,
            Guid moduleVersionId,
            int typeToken)
        {
            Debug.Assert(MetadataTokens.Handle(typeToken).Kind == HandleKind.TypeDefinition);

            var currentType = compilation.GetType(moduleVersionId, typeToken);
            Debug.Assert((object)currentType != null);
            var currentFrame = new SynthesizedContextMethodSymbol(currentType);
            return new EvaluationContext(
                null,
                compilation,
                currentFrame,
                null,
                default(ImmutableArray<LocalSymbol>),
                ImmutableSortedSet<int>.Empty,
                MethodDebugInfo<TypeSymbol, LocalSymbol>.None);
        }

        /// <summary>
        /// Create a context for evaluating expressions within a method scope.
        /// </summary>
        /// <param name="previous">Previous context, if any, for possible re-use.</param>
        /// <param name="metadataBlocks">Module metadata</param>
        /// <param name="symReader"><see cref="ISymUnmanagedReader"/> for PDB associated with <paramref name="moduleVersionId"/></param>
        /// <param name="moduleVersionId">Module containing method</param>
        /// <param name="methodToken">Method metadata token</param>
        /// <param name="methodVersion">Method version.</param>
        /// <param name="ilOffset">IL offset of instruction pointer in method</param>
        /// <param name="localSignatureToken">Method local signature token</param>
        /// <returns>Evaluation context</returns>
        internal static EvaluationContext CreateMethodContext(
            CSharpMetadataContext previous,
            ImmutableArray<MetadataBlock> metadataBlocks,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            uint ilOffset,
            int localSignatureToken)
        {
            var offset = NormalizeILOffset(ilOffset);

            CSharpCompilation compilation = metadataBlocks.ToCompilation(default(Guid), MakeAssemblyReferencesKind.AllAssemblies);

            return CreateMethodContext(
                compilation,
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                offset,
                localSignatureToken);
        }

        /// <summary>
        /// Create a context for evaluating expressions within a method scope.
        /// </summary>
        /// <param name="compilation">Compilation.</param>
        /// <param name="symReader"><see cref="ISymUnmanagedReader"/> for PDB associated with <paramref name="moduleVersionId"/></param>
        /// <param name="moduleVersionId">Module containing method</param>
        /// <param name="methodToken">Method metadata token</param>
        /// <param name="methodVersion">Method version.</param>
        /// <param name="ilOffset">IL offset of instruction pointer in method</param>
        /// <param name="localSignatureToken">Method local signature token</param>
        /// <returns>Evaluation context</returns>
        internal static EvaluationContext CreateMethodContext(
            CSharpCompilation compilation,
            object symReader,
            Guid moduleVersionId,
            int methodToken,
            int methodVersion,
            int ilOffset,
            int localSignatureToken)
        {
            var methodHandle = (MethodDefinitionHandle)MetadataTokens.Handle(methodToken);
            var currentSourceMethod = compilation.GetSourceMethod(moduleVersionId, methodHandle);
            var localSignatureHandle = (localSignatureToken != 0) ? (StandaloneSignatureHandle)MetadataTokens.Handle(localSignatureToken) : default(StandaloneSignatureHandle);

            var currentFrame = compilation.GetMethod(moduleVersionId, methodHandle);
            Debug.Assert((object)currentFrame != null);
            var symbolProvider = new CSharpEESymbolProvider(compilation.SourceAssembly, (PEModuleSymbol)currentFrame.ContainingModule, currentFrame);

            var metadataDecoder = new MetadataDecoder((PEModuleSymbol)currentFrame.ContainingModule, currentFrame);
            var localInfo = metadataDecoder.GetLocalInfo(localSignatureHandle);

            var typedSymReader = (ISymUnmanagedReader3)symReader;

            var debugInfo = MethodDebugInfo<TypeSymbol, LocalSymbol>.ReadMethodDebugInfo(typedSymReader, symbolProvider, methodToken, methodVersion, ilOffset, isVisualBasicMethod: false);

            var reuseSpan = debugInfo.ReuseSpan;
            var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            MethodDebugInfo<TypeSymbol, LocalSymbol>.GetLocals(
                localsBuilder,
                symbolProvider,
                debugInfo.LocalVariableNames,
                localInfo,
                debugInfo.DynamicLocalMap,
                debugInfo.TupleLocalMap);

            var inScopeHoistedLocals = debugInfo.GetInScopeHoistedLocalIndices(ilOffset, ref reuseSpan);

            localsBuilder.AddRange(debugInfo.LocalConstants);

            return new EvaluationContext(
                new MethodContextReuseConstraints(moduleVersionId, methodToken, methodVersion, reuseSpan),
                compilation,
                currentFrame,
                currentSourceMethod,
                localsBuilder.ToImmutableAndFree(),
                inScopeHoistedLocals,
                debugInfo);
        }

        internal CompilationContext CreateCompilationContext()
        {
            return new CompilationContext(
                this.Compilation,
                _currentFrame,
                _currentSourceMethod,
                _locals,
                _inScopeHoistedLocalSlots,
                _methodDebugInfo);
        }

        /// <summary>
        /// Compile a collection of expressions at the same location. If all expressions
        /// compile successfully, a single assembly is returned along with the method
        /// tokens for the expression evaluation methods. If there are errors compiling
        /// any expression, null is returned along with the collection of error messages
        /// for all expressions.
        /// </summary>
        /// <remarks>
        /// Errors are returned as a single collection rather than grouped by expression
        /// since some errors (such as those detected during emit) are not easily
        /// attributed to a particular expression.
        /// </remarks>
        internal byte[] CompileExpressions(
            ImmutableArray<string> expressions,
            out ImmutableArray<int> methodTokens,
            out ImmutableArray<string> errorMessages)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var syntaxNodes = expressions.SelectAsArray(expr => Parse(expr, treatAsExpression: true, diagnostics: diagnostics, formatSpecifiers: out var formatSpecifiers));
            byte[] assembly = null;
            if (!diagnostics.HasAnyErrors())
            {
                Debug.Assert(syntaxNodes.All(s => s != null));
                var context = this.CreateCompilationContext();
                var moduleBuilder = context.CompileExpressions(syntaxNodes, TypeName, MethodName, diagnostics);
                if (moduleBuilder != null)
                {
                    using (var stream = new MemoryStream())
                    {
                        Cci.PeWriter.WritePeToStream(
                            new EmitContext(moduleBuilder, null, diagnostics, metadataOnly: false, includePrivateMembers: true),
                            context.MessageProvider,
                            () => stream,
                            getPortablePdbStreamOpt: null,
                            nativePdbWriterOpt: null,
                            pdbPathOpt: null,
                            metadataOnly: false,
                            isDeterministic: false,
                            emitTestCoverageData: false,
                            privateKeyOpt: null,
                            cancellationToken: default(CancellationToken));
                        if (!diagnostics.HasAnyErrors())
                        {
                            assembly = stream.ToArray();
                        }
                    }
                }
            }
            if (assembly == null)
            {
                methodTokens = ImmutableArray<int>.Empty;
                errorMessages = ImmutableArray.CreateRange(
                    diagnostics.AsEnumerable().
                        Where(d => d.Severity == DiagnosticSeverity.Error).
                        Select(d => GetErrorMessage(d, CSharpDiagnosticFormatter.Instance, preferredUICulture: null)));
            }
            else
            {
                methodTokens = MetadataUtilities.GetSynthesizedMethods(assembly, MethodName);
                Debug.Assert(methodTokens.Length == expressions.Length);
                errorMessages = ImmutableArray<string>.Empty;
            }
            diagnostics.Free();
            return assembly;
        }

        internal override CompileResult CompileExpression(
            string expr,
            DkmEvaluationFlags compilationFlags,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData)
        {
            ReadOnlyCollection<string> formatSpecifiers;
            var syntax = Parse(expr, (compilationFlags & DkmEvaluationFlags.TreatAsExpression) != 0, diagnostics, out formatSpecifiers);
            if (syntax == null)
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            var context = this.CreateCompilationContext();
            var moduleBuilder = context.CompileExpression(syntax, TypeName, MethodName, aliases, testData, diagnostics, out var synthesizedMethod);
            if (moduleBuilder == null)
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            using (var stream = new MemoryStream())
            {
                Cci.PeWriter.WritePeToStream(
                    new EmitContext(moduleBuilder, null, diagnostics, metadataOnly: false, includePrivateMembers: true),
                    context.MessageProvider,
                    () => stream,
                    getPortablePdbStreamOpt: null,
                    nativePdbWriterOpt: null,
                    pdbPathOpt: null,
                    metadataOnly: false,
                    isDeterministic: false,
                    emitTestCoverageData: false,
                    privateKeyOpt: null,
                    cancellationToken: default(CancellationToken));

                if (diagnostics.HasAnyErrors())
                {
                    resultProperties = default(ResultProperties);
                    return null;
                }

                Debug.Assert(synthesizedMethod.ContainingType.MetadataName == TypeName);
                Debug.Assert(synthesizedMethod.MetadataName == MethodName);

                resultProperties = synthesizedMethod.ResultProperties;
                return new CSharpCompileResult(
                    stream.ToArray(),
                    synthesizedMethod,
                    formatSpecifiers: formatSpecifiers);
            }
        }

        private static CSharpSyntaxNode Parse(
            string expr,
            bool treatAsExpression,
            DiagnosticBag diagnostics,
            out ReadOnlyCollection<string> formatSpecifiers)
        {
            if (!treatAsExpression)
            {
                // Try to parse as a statement. If that fails, parse as an expression.
                var statementDiagnostics = DiagnosticBag.GetInstance();
                var statementSyntax = expr.ParseStatement(statementDiagnostics);
                Debug.Assert((statementSyntax == null) || !statementDiagnostics.HasAnyErrors());
                statementDiagnostics.Free();
                var isExpressionStatement = statementSyntax.IsKind(SyntaxKind.ExpressionStatement);
                if (statementSyntax != null && !isExpressionStatement)
                {
                    formatSpecifiers = null;

                    if (statementSyntax.IsKind(SyntaxKind.LocalDeclarationStatement))
                    {
                        return statementSyntax;
                    }

                    diagnostics.Add(ErrorCode.ERR_ExpressionOrDeclarationExpected, Location.None);
                    return null;
                }
            }

            return expr.ParseExpression(diagnostics, allowFormatSpecifiers: true, formatSpecifiers: out formatSpecifiers);
        }

        internal override CompileResult CompileAssignment(
            string target,
            string expr,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData)
        {
            var assignment = target.ParseAssignment(expr, diagnostics);
            if (assignment == null)
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            var context = this.CreateCompilationContext();
            var moduleBuilder = context.CompileAssignment(assignment, TypeName, MethodName, aliases, testData, diagnostics, out var synthesizedMethod);
            if (moduleBuilder == null)
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            using (var stream = new MemoryStream())
            {
                Cci.PeWriter.WritePeToStream(
                    new EmitContext(moduleBuilder, null, diagnostics, metadataOnly: false, includePrivateMembers: true),
                    context.MessageProvider,
                    () => stream,
                    getPortablePdbStreamOpt: null,
                    nativePdbWriterOpt: null,
                    pdbPathOpt: null,
                    metadataOnly: false,
                    isDeterministic: false,
                    emitTestCoverageData: false,
                    privateKeyOpt: null,
                    cancellationToken: default(CancellationToken));

                if (diagnostics.HasAnyErrors())
                {
                    resultProperties = default(ResultProperties);
                    return null;
                }

                Debug.Assert(synthesizedMethod.ContainingType.MetadataName == TypeName);
                Debug.Assert(synthesizedMethod.MetadataName == MethodName);

                resultProperties = synthesizedMethod.ResultProperties;
                return new CSharpCompileResult(
                    stream.ToArray(),
                    synthesizedMethod,
                    formatSpecifiers: null);
            }
        }

        private static readonly ReadOnlyCollection<byte> s_emptyBytes =
            new ReadOnlyCollection<byte>(Array.Empty<byte>());

        internal override ReadOnlyCollection<byte> CompileGetLocals(
            ArrayBuilder<LocalAndMethod> locals,
            bool argumentsOnly,
            ImmutableArray<Alias> aliases,
            DiagnosticBag diagnostics,
            out string typeName,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData)
        {
            var context = this.CreateCompilationContext();
            var moduleBuilder = context.CompileGetLocals(TypeName, locals, argumentsOnly, aliases, testData, diagnostics);
            ReadOnlyCollection<byte> assembly = null;

            if ((moduleBuilder != null) && (locals.Count > 0))
            {
                using (var stream = new MemoryStream())
                {
                    Cci.PeWriter.WritePeToStream(
                        new EmitContext(moduleBuilder, null, diagnostics, metadataOnly: false, includePrivateMembers: true),
                        context.MessageProvider,
                        () => stream,
                        getPortablePdbStreamOpt: null,
                        nativePdbWriterOpt: null,
                        pdbPathOpt: null,
                        metadataOnly: false,
                        isDeterministic: false,
                        emitTestCoverageData: false,
                        privateKeyOpt: null,
                        cancellationToken: default(CancellationToken));

                    if (!diagnostics.HasAnyErrors())
                    {
                        assembly = new ReadOnlyCollection<byte>(stream.ToArray());
                    }
                }
            }

            if (assembly == null)
            {
                locals.Clear();
                assembly = s_emptyBytes;
            }

            typeName = TypeName;
            return assembly;
        }

        internal override bool HasDuplicateTypesOrAssemblies(Diagnostic diagnostic)
        {
            switch ((ErrorCode)diagnostic.Code)
            {
                case ErrorCode.ERR_DuplicateImport:
                case ErrorCode.ERR_DuplicateImportSimple:
                case ErrorCode.ERR_SameFullNameAggAgg:
                case ErrorCode.ERR_AmbigCall:
                    return true;
                default:
                    return false;
            }
        }

        internal override ImmutableArray<AssemblyIdentity> GetMissingAssemblyIdentities(Diagnostic diagnostic, AssemblyIdentity linqLibrary)
        {
            return GetMissingAssemblyIdentitiesHelper((ErrorCode)diagnostic.Code, diagnostic.Arguments, linqLibrary);
        }

        /// <remarks>
        /// Internal for testing.
        /// </remarks>
        internal static ImmutableArray<AssemblyIdentity> GetMissingAssemblyIdentitiesHelper(ErrorCode code, IReadOnlyList<object> arguments, AssemblyIdentity linqLibrary)
        {
            Debug.Assert(linqLibrary != null);

            switch (code)
            {
                case ErrorCode.ERR_NoTypeDef:
                case ErrorCode.ERR_GlobalSingleTypeNameNotFoundFwd:
                case ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd:
                case ErrorCode.ERR_SingleTypeNameNotFoundFwd:
                case ErrorCode.ERR_NameNotInContextPossibleMissingReference: // Probably can't happen.
                    foreach (var argument in arguments)
                    {
                        var identity = (argument as AssemblyIdentity) ?? (argument as AssemblySymbol)?.Identity;
                        if (identity != null && !identity.Equals(MissingCorLibrarySymbol.Instance.Identity))
                        {
                            return ImmutableArray.Create(identity);
                        }
                    }
                    break;
                case ErrorCode.ERR_DottedTypeNameNotFoundInNS:
                    if (arguments.Count == 2)
                    {
                        var namespaceName = arguments[0] as string;
                        var containingNamespace = arguments[1] as NamespaceSymbol;
                        if (namespaceName != null && (object)containingNamespace != null &&
                            containingNamespace.ConstituentNamespaces.Any(n => n.ContainingAssembly.Identity.IsWindowsAssemblyIdentity()))
                        {
                            // This is just a heuristic, but it has the advantage of being portable, particularly 
                            // across different versions of (desktop) windows.
                            var identity = new AssemblyIdentity($"{containingNamespace.ToDisplayString()}.{namespaceName}", contentType: System.Reflection.AssemblyContentType.WindowsRuntime);
                            return ImmutableArray.Create(identity);
                        }
                    }
                    break;
                case ErrorCode.ERR_NoSuchMemberOrExtension: // Commonly, but not always, caused by absence of System.Core.
                case ErrorCode.ERR_DynamicAttributeMissing:
                case ErrorCode.ERR_DynamicRequiredTypesMissing:
                // MSDN says these might come from System.Dynamic.Runtime
                case ErrorCode.ERR_QueryNoProviderStandard:
                case ErrorCode.ERR_ExtensionAttrNotFound: // Probably can't happen.
                    return ImmutableArray.Create(linqLibrary);
                case ErrorCode.ERR_BadAwaitArg_NeedSystem:
                    Debug.Assert(false, "Roslyn no longer produces ERR_BadAwaitArg_NeedSystem");
                    break;
            }

            return default(ImmutableArray<AssemblyIdentity>);
        }
    }
}
