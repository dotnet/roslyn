// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    using OP = Microsoft.Cci.PdbLogger.PdbWriterOperation;

    /// <summary>
    /// Exception to enable callers to catch all of the exceptions originating
    /// from writing PDBs. We resurface such exceptions as this type, to eventually
    /// be reported as PDB-writing failure diagnostics to the user.
    /// Unfortunately, an exception originating in a user-implemented
    /// Stream derivation will come out of the symbol writer as a COMException
    /// missing all of the original exception info.
    /// </summary>
    internal sealed class PdbWritingException : Exception
    {
        internal PdbWritingException(Exception inner) :
            base(inner.Message, inner)
        {
        }
    }

    /// <summary>
    /// A utility to log all operations and arguments to the native PDB writing
    /// library, so that we can hash that log to generate a deterministic GUID and
    /// timestamp.
    /// </summary>
    internal sealed class PdbLogger
    {
        // This class hashes the log data on-the-fly; see
        // https://msdn.microsoft.com/en-us/library/system.security.cryptography.hashalgorithm.transformblock(v=vs.110).aspx
        // That enables us to avoid producing a full log in memory.
        // On the other hand, we do want to use a fairly large buffer as the hashing operations
        // are invoked through reflection, which is fairly slow.
        private readonly bool _logging;
        private readonly BlobBuilder _logData;
        private const int bufferFlushLimit = 64 * 1024;
        private readonly HashAlgorithm _hashAlgorithm;

        internal PdbLogger(bool logging)
        {
            _logging = logging;
            if (logging)
            {
                // do not get this from pool
                // we need a fairly large buffer here (where the pool typically contains small ones)
                // and we need just one per compile session
                // pooling will be couter-productive in such scenario
                _logData = new BlobBuilder(bufferFlushLimit);
                _hashAlgorithm = new SHA1CryptoServiceProvider();
                Debug.Assert(_hashAlgorithm.SupportsTransform);
            }
            else
            {
                _logData = null;
                _hashAlgorithm = null;
            }
        }

        private void EnsureSpace(int space)
        {
            // note that if space > bufferFlushLimit, the buffer will need to expand anyways
            // that should be very rare though.
            if (_logData.Count + space >= bufferFlushLimit)
            {
                foreach (var blob in _logData.GetBlobs())
                {
                    var segment = blob.GetUnderlyingBuffer();
                    _hashAlgorithm.TransformBlock(segment.Array, segment.Offset, segment.Count);
                }

                _logData.Clear();
            }
        }

        internal byte[] GetLogHash()
        {
            Debug.Assert(_logData != null);

            int remaining = _logData.Count;
            foreach (var blob in _logData.GetBlobs())
            {
                var segment = blob.GetUnderlyingBuffer();
                remaining -= segment.Count;
                if (remaining == 0)
                {
                    _hashAlgorithm.TransformFinalBlock(segment.Array, segment.Offset, segment.Count);
                }
                else
                {
                    _hashAlgorithm.TransformBlock(segment.Array, segment.Offset, segment.Count);
                }
            }

            Debug.Assert(remaining == 0);

            _logData.Clear();
            return _hashAlgorithm.Hash;
        }

        internal void Close()
        {
            _hashAlgorithm?.Dispose();
        }

        internal enum PdbWriterOperation : byte
        {
            SetUserEntryPoint,
            DefineDocument,
            SetCheckSum,
            OpenMethod,
            OpenScope,
            CloseMethod,
            CloseScope,
            UsingNamespace,
            DefineSequencePoints,
            SetSymAttribute,
            DefineConstant2,
            DefineLocalVariable2,
            DefineAsyncStepInfo,
            DefineCatchHandlerILOffset,
            DefineKickoffMethod,
            OpenMapTokensToSourceSpans,
            MapTokenToSourceSpan,
            CloseMapTokensToSourceSpans
        }

        public bool LogOperation(PdbWriterOperation op)
        {
            var logging = _logging;
            if (logging)
            {
                LogArgument((byte)op);
            }

            return logging;
        }

        public void LogArgument(uint[] data, int cnt)
        {
            EnsureSpace((cnt + 1) * 4);
            _logData.WriteInt32(cnt);
            for (int i = 0; i < cnt; i++)
            {
                _logData.WriteUInt32(data[i]);
            }
        }

        public void LogArgument(string data)
        {
            EnsureSpace(data.Length * 2);
            _logData.WriteUTF8(data, allowUnpairedSurrogates: true);
        }

        public void LogArgument(uint data)
        {
            EnsureSpace(4);
            _logData.WriteUInt32(data);
        }

        public void LogArgument(byte data)
        {
            EnsureSpace(1);
            _logData.WriteByte(data);
        }

        public void LogArgument(byte[] data)
        {
            EnsureSpace(data.Length + 4);
            _logData.WriteInt32(data.Length);
            _logData.WriteBytes(data);
        }

        public void LogArgument(int[] data)
        {
            EnsureSpace((data.Length + 1) * 4);
            _logData.WriteInt32(data.Length);
            foreach (int d in data)
            {
                _logData.WriteInt32(d);
            }
        }

        public void LogArgument(long data)
        {
            EnsureSpace(8);
            _logData.WriteInt64(data);
        }

        public void LogArgument(object data)
        {
            string str;
            if (data is decimal)
            {
                LogArgument(decimal.GetBits((decimal)data));
            }
            else if (data is DateTime)
            {
                LogArgument(((DateTime)data).ToBinary());
            }
            else if ((str = data as string) != null)
            {
                LogArgument(str);
            }
            else
            {
                // being conservative here
                // string and decimal are handled above, 
                // everything else is 8 bytes or less.
                EnsureSpace(8);
                _logData.WriteConstant(data);
            }
        }
    }

    internal sealed class PdbWriter : IDisposable
    {
        internal const uint HiddenLocalAttributesValue = 1u;
        internal const uint DefaultLocalAttributesValue = 0u;
        internal const uint Age = 1;

        private static Type s_lazyCorSymWriterSxSType;

        private readonly string _fileName;
        private readonly Func<object> _symWriterFactory;
        private ComMemoryStream _pdbStream;
        private MetadataWriter _metadataWriter;
        private ISymUnmanagedWriter5 _symWriter;

        private readonly Dictionary<DebugSourceDocument, ISymUnmanagedDocumentWriter> _documentMap = new Dictionary<DebugSourceDocument, ISymUnmanagedDocumentWriter>();

        // { INamespace or ITypeReference -> qualified name }
        private readonly Dictionary<object, string> _qualifiedNameCache = new Dictionary<object, string>();

        // sequence point buffers:
        private uint[] _sequencePointOffsets;
        private uint[] _sequencePointStartLines;
        private uint[] _sequencePointStartColumns;
        private uint[] _sequencePointEndLines;
        private uint[] _sequencePointEndColumns;

        // in support of determinism
        private readonly bool _deterministic;
        private readonly PdbLogger _callLogger;

        public PdbWriter(string fileName, Func<object> symWriterFactory, bool deterministic)
        {
            _fileName = fileName;
            _symWriterFactory = symWriterFactory;
            CreateSequencePointBuffers(capacity: 64);
            _deterministic = deterministic;
            _callLogger = new PdbLogger(deterministic);
        }

        public unsafe void WriteTo(Stream stream)
        {
            Debug.Assert(_pdbStream != null);
            Debug.Assert(_symWriter != null);

            try
            {
                // SymWriter flushes data to the native stream on close:
                _symWriter.Close();
                _symWriter = null;
                _pdbStream.CopyTo(stream);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        ~PdbWriter()
        {
            Close();
        }

        private void Close()
        {
            try
            {
                _symWriter?.Close();
                _symWriter = null;
                _pdbStream = null;
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private IModule Module => Context.Module;
        private EmitContext Context => _metadataWriter.Context;

        public void SerializeDebugInfo(IMethodBody methodBody, uint localSignatureToken, CustomDebugInfoWriter customDebugInfoWriter)
        {
            Debug.Assert(_metadataWriter != null);

            bool isIterator = methodBody.StateMachineTypeName != null;
            bool emitDebugInfo = isIterator || methodBody.HasAnySequencePoints;

            if (!emitDebugInfo)
            {
                return;
            }

            int methodToken = _metadataWriter.GetMethodToken(methodBody.MethodDefinition);

            OpenMethod((uint)methodToken, methodBody.MethodDefinition);

            var localScopes = methodBody.LocalScopes;

            // Define locals, constants and namespaces in the outermost local scope (opened in OpenMethod):
            if (localScopes.Length > 0)
            {
                this.DefineScopeLocals(localScopes[0], localSignatureToken);
            }

            // NOTE: This is an attempt to match Dev10's apparent behavior.  For iterator methods (i.e. the method
            // that appears in source, not the synthesized ones), Dev10 only emits the ForwardIterator and IteratorLocal
            // custom debug info (e.g. there will be no information about the usings that were in scope).
            if (!isIterator && methodBody.ImportScope != null)
            {
                IMethodDefinition forwardToMethod;
                if (customDebugInfoWriter.ShouldForwardNamespaceScopes(Context, methodBody, methodToken, out forwardToMethod))
                {
                    if (forwardToMethod != null)
                    {
                        UsingNamespace("@" + _metadataWriter.GetMethodToken(forwardToMethod), methodBody.MethodDefinition);
                    }
                    // otherwise, the forwarding is done via custom debug info
                }
                else
                {
                    this.DefineNamespaceScopes(methodBody);
                }
            }

            DefineLocalScopes(localScopes, localSignatureToken);
            ArrayBuilder<Cci.SequencePoint> sequencePoints = ArrayBuilder<Cci.SequencePoint>.GetInstance();
            methodBody.GetSequencePoints(sequencePoints);
            EmitSequencePoints(sequencePoints);
            sequencePoints.Free();

            AsyncMethodBodyDebugInfo asyncDebugInfo = methodBody.AsyncDebugInfo;
            if (asyncDebugInfo != null)
            {
                SetAsyncInfo(
                    methodToken,
                    _metadataWriter.GetMethodToken(asyncDebugInfo.KickoffMethod),
                    asyncDebugInfo.CatchHandlerOffset,
                    asyncDebugInfo.YieldOffsets,
                    asyncDebugInfo.ResumeOffsets);
            }

            var compilationOptions = Context.ModuleBuilder.CommonCompilation.Options;

            // We need to avoid emitting CDI DynamicLocals = 5 and EditAndContinueLocalSlotMap = 6 for files processed by WinMDExp until 
            // bug #1067635 is fixed and available in SDK.
            bool suppressNewCustomDebugInfo = !compilationOptions.ExtendedCustomDebugInformation ||
                (compilationOptions.OutputKind == OutputKind.WindowsRuntimeMetadata);

            bool emitEncInfo = compilationOptions.EnableEditAndContinue && !_metadataWriter.IsFullMetadata;

            bool emitExternNamespaces;
            byte[] blob = customDebugInfoWriter.SerializeMethodDebugInfo(Context, methodBody, methodToken, emitEncInfo, suppressNewCustomDebugInfo, out emitExternNamespaces);
            if (blob != null)
            {
                DefineCustomMetadata("MD2", blob);
            }

            if (emitExternNamespaces)
            {
                this.DefineAssemblyReferenceAliases();
            }

            CloseMethod(methodBody.IL.Length);
        }

        private void DefineNamespaceScopes(IMethodBody methodBody)
        {
            var module = Module;
            bool isVisualBasic = module.GenerateVisualBasicStylePdb;

            IMethodDefinition method = methodBody.MethodDefinition;

            var namespaceScopes = methodBody.ImportScope;

            PooledHashSet<string> lazyDeclaredExternAliases = null;
            if (!isVisualBasic)
            {
                for (var scope = namespaceScopes; scope != null; scope = scope.Parent)
                {
                    foreach (var import in scope.GetUsedNamespaces())
                    {
                        if (import.TargetNamespaceOpt == null && import.TargetTypeOpt == null)
                        {
                            Debug.Assert(import.AliasOpt != null);
                            Debug.Assert(import.TargetAssemblyOpt == null);

                            if (lazyDeclaredExternAliases == null)
                            {
                                lazyDeclaredExternAliases = PooledHashSet<string>.GetInstance();
                            }

                            lazyDeclaredExternAliases.Add(import.AliasOpt);
                        }
                    }
                }
            }

            // file and namespace level
            for (IImportScope scope = namespaceScopes; scope != null; scope = scope.Parent)
            {
                foreach (UsedNamespaceOrType import in scope.GetUsedNamespaces())
                {
                    var importString = TryEncodeImport(import, lazyDeclaredExternAliases, isProjectLevel: false);
                    if (importString != null)
                    {
                        UsingNamespace(importString, method);
                    }
                }
            }

            lazyDeclaredExternAliases?.Free();

            // project level
            if (isVisualBasic)
            {
                string defaultNamespace = module.DefaultNamespace;

                if (!string.IsNullOrEmpty(defaultNamespace))
                {
                    // VB marks the default/root namespace with an asterisk
                    UsingNamespace("*" + defaultNamespace, module);
                }

                foreach (string assemblyName in module.LinkedAssembliesDebugInfo)
                {
                    UsingNamespace("&" + assemblyName, module);
                }

                foreach (UsedNamespaceOrType import in module.GetImports())
                {
                    var importString = TryEncodeImport(import, null, isProjectLevel: true);
                    if (importString != null)
                    {
                        UsingNamespace(importString, method);
                    }
                }

                // VB current namespace -- VB appends the namespace of the container without prefixes
                UsingNamespace(GetOrCreateSerializedNamespaceName(method.ContainingNamespace), method);
            }
        }

        private void DefineAssemblyReferenceAliases()
        {
            foreach (AssemblyReferenceAlias alias in Module.GetAssemblyReferenceAliases(Context))
            {
                UsingNamespace("Z" + alias.Name + " " + alias.Assembly.Identity.GetDisplayName(), Module);
            }
        }

        private string TryEncodeImport(UsedNamespaceOrType import, HashSet<string> declaredExternAliasesOpt, bool isProjectLevel)
        {
            // NOTE: Dev12 has related cases "I" and "O" in EMITTER::ComputeDebugNamespace,
            // but they were probably implementation details that do not affect Roslyn.

            if (Module.GenerateVisualBasicStylePdb)
            {
                // VB doesn't support extern aliases
                Debug.Assert(import.TargetAssemblyOpt == null);
                Debug.Assert(declaredExternAliasesOpt == null);

                if (import.TargetTypeOpt != null)
                {
                    Debug.Assert(import.TargetNamespaceOpt == null);
                    Debug.Assert(import.TargetAssemblyOpt == null);

                    // Native compiler doesn't write imports with generic types to PDB.
                    if (import.TargetTypeOpt.IsTypeSpecification())
                    {
                        return null;
                    }

                    string typeName = GetOrCreateSerializedTypeName(import.TargetTypeOpt);

                    if (import.AliasOpt != null)
                    {
                        return (isProjectLevel ? "@PA:" : "@FA:") + import.AliasOpt + "=" + typeName;
                    }
                    else
                    {
                        return (isProjectLevel ? "@PT:" : "@FT:") + typeName;
                    }
                }

                if (import.TargetNamespaceOpt != null)
                {
                    string namespaceName = GetOrCreateSerializedNamespaceName(import.TargetNamespaceOpt);

                    if (import.AliasOpt == null)
                    {
                        return (isProjectLevel ? "@P:" : "@F:") + namespaceName;
                    }
                    else
                    {
                        return (isProjectLevel ? "@PA:" : "@FA:") + import.AliasOpt + "=" + namespaceName;
                    }
                }

                Debug.Assert(import.AliasOpt != null);
                Debug.Assert(import.TargetXmlNamespaceOpt != null);

                return (isProjectLevel ? "@PX:" : "@FX:") + import.AliasOpt + "=" + import.TargetXmlNamespaceOpt;
            }

            Debug.Assert(import.TargetXmlNamespaceOpt == null);

            if (import.TargetTypeOpt != null)
            {
                Debug.Assert(import.TargetNamespaceOpt == null);
                Debug.Assert(import.TargetAssemblyOpt == null);

                string typeName = GetOrCreateSerializedTypeName(import.TargetTypeOpt);

                return (import.AliasOpt != null) ?
                    "A" + import.AliasOpt + " T" + typeName :
                    "T" + typeName;
            }

            if (import.TargetNamespaceOpt != null)
            {
                string namespaceName = GetOrCreateSerializedNamespaceName(import.TargetNamespaceOpt);

                if (import.AliasOpt != null)
                {
                    return (import.TargetAssemblyOpt != null) ?
                        "A" + import.AliasOpt + " E" + namespaceName + " " + GetAssemblyReferenceAlias(import.TargetAssemblyOpt, declaredExternAliasesOpt) :
                        "A" + import.AliasOpt + " U" + namespaceName;
                }
                else
                {
                    return (import.TargetAssemblyOpt != null) ?
                        "E" + namespaceName + " " + GetAssemblyReferenceAlias(import.TargetAssemblyOpt, declaredExternAliasesOpt) :
                        "U" + namespaceName;
                }
            }

            Debug.Assert(import.AliasOpt != null);
            Debug.Assert(import.TargetAssemblyOpt == null);
            return "X" + import.AliasOpt;
        }

        internal string GetOrCreateSerializedNamespaceName(INamespace @namespace)
        {
            string result;
            if (!_qualifiedNameCache.TryGetValue(@namespace, out result))
            {
                result = TypeNameSerializer.BuildQualifiedNamespaceName(@namespace);
                _qualifiedNameCache.Add(@namespace, result);
            }

            return result;
        }

        internal string GetOrCreateSerializedTypeName(ITypeReference typeReference)
        {
            string result;
            if (!_qualifiedNameCache.TryGetValue(typeReference, out result))
            {
                if (Module.GenerateVisualBasicStylePdb)
                {
                    result = SerializeVisualBasicImportTypeReference(typeReference);
                }
                else
                {
                    result = typeReference.GetSerializedTypeName(Context);
                }

                _qualifiedNameCache.Add(typeReference, result);
            }

            return result;
        }

        private string SerializeVisualBasicImportTypeReference(ITypeReference typeReference)
        {
            Debug.Assert(!(typeReference is IArrayTypeReference));
            Debug.Assert(!(typeReference is IPointerTypeReference));
            Debug.Assert(!(typeReference is IManagedPointerTypeReference));
            Debug.Assert(!typeReference.IsTypeSpecification());

            var result = PooledStringBuilder.GetInstance();
            ArrayBuilder<string> nestedNamesReversed;

            INestedTypeReference nestedType = typeReference.AsNestedTypeReference;
            if (nestedType != null)
            {
                nestedNamesReversed = ArrayBuilder<string>.GetInstance();

                while (nestedType != null)
                {
                    nestedNamesReversed.Add(nestedType.Name);
                    typeReference = nestedType.GetContainingType(_metadataWriter.Context);
                    nestedType = typeReference.AsNestedTypeReference;
                }
            }
            else
            {
                nestedNamesReversed = null;
            }

            INamespaceTypeReference namespaceType = typeReference.AsNamespaceTypeReference;
            Debug.Assert(namespaceType != null);

            string namespaceName = namespaceType.NamespaceName;
            if (namespaceName.Length != 0)
            {
                result.Builder.Append(namespaceName);
                result.Builder.Append('.');
            }

            result.Builder.Append(namespaceType.Name);

            if (nestedNamesReversed != null)
            {
                for (int i = nestedNamesReversed.Count - 1; i >= 0; i--)
                {
                    result.Builder.Append('.');
                    result.Builder.Append(nestedNamesReversed[i]);
                }

                nestedNamesReversed.Free();
            }

            return result.ToStringAndFree();
        }

        private string GetAssemblyReferenceAlias(IAssemblyReference assembly, HashSet<string> declaredExternAliases)
        {
            // no extern alias defined in scope at all -> error in compiler
            Debug.Assert(declaredExternAliases != null);

            var allAliases = _metadataWriter.Context.Module.GetAssemblyReferenceAliases(_metadataWriter.Context);
            foreach (AssemblyReferenceAlias alias in allAliases)
            {
                // Multiple aliases may be given to an assembly reference.
                // We find one that is in scope (was imported via extern alias directive).
                // If multiple are in scope then use the first one.

                // NOTE: Dev12 uses the one that appeared in source, whereas we use
                // the first one that COULD have appeared in source.  (DevDiv #913022)
                // The reason we're not just using the alias from the syntax is that
                // it is non-trivial to locate.  In particular, since "." may be used in
                // place of "::", determining whether the first identifier in the name is
                // the alias requires binding.  For example, "using A.B;" could refer to
                // either "A::B" or "global::A.B".

                if (assembly == alias.Assembly && declaredExternAliases.Contains(alias.Name))
                {
                    return alias.Name;
                }
            }

            // no alias defined in scope for given assembly -> error in compiler
            throw ExceptionUtilities.Unreachable;
        }

        private void DefineLocalScopes(ImmutableArray<LocalScope> scopes, uint localSignatureToken)
        {
            // VB scope ranges are end-inclusive
            bool endInclusive = this.Module.GenerateVisualBasicStylePdb;

            // The order of OpenScope and CloseScope calls must follow the scope nesting.
            var scopeStack = ArrayBuilder<LocalScope>.GetInstance();

            for (int i = 1; i < scopes.Length; i++)
            {
                var currentScope = scopes[i];

                // Close any scopes that have finished.
                while (scopeStack.Count > 0)
                {
                    LocalScope topScope = scopeStack.Last();
                    if (currentScope.StartOffset < topScope.StartOffset + topScope.Length)
                    {
                        break;
                    }

                    scopeStack.RemoveLast();
                    CloseScope(endInclusive ? topScope.EndOffset - 1 : topScope.EndOffset);
                }

                // Open this scope.
                scopeStack.Add(currentScope);
                OpenScope(currentScope.StartOffset);
                this.DefineScopeLocals(currentScope, localSignatureToken);
            }

            // Close remaining scopes.
            for (int i = scopeStack.Count - 1; i >= 0; i--)
            {
                LocalScope scope = scopeStack[i];
                CloseScope(endInclusive ? scope.EndOffset - 1 : scope.EndOffset);
            }

            scopeStack.Free();
        }

        private void DefineScopeLocals(LocalScope currentScope, uint localSignatureToken)
        {
            foreach (ILocalDefinition scopeConstant in currentScope.Constants)
            {
                int token = _metadataWriter.SerializeLocalConstantStandAloneSignature(scopeConstant);
                if (!_metadataWriter.IsLocalNameTooLong(scopeConstant))
                {
                    DefineLocalConstant(scopeConstant.Name, scopeConstant.CompileTimeValue.Value, _metadataWriter.GetConstantTypeCode(scopeConstant), (uint)token);
                }
            }

            foreach (ILocalDefinition scopeLocal in currentScope.Variables)
            {
                if (!_metadataWriter.IsLocalNameTooLong(scopeLocal))
                {
                    Debug.Assert(scopeLocal.SlotIndex >= 0);
                    DefineLocalVariable((uint)scopeLocal.SlotIndex, scopeLocal.Name, scopeLocal.PdbAttributes, localSignatureToken);
                }
            }
        }

        #region SymWriter calls

        private const string SymWriterClsid = "0AE2DEB0-F901-478b-BB9F-881EE8066788";

        private static bool s_MicrosoftDiaSymReaderNativeLoadFailed;

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymWriter")]
        private extern static void CreateSymWriter32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symWriter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymWriter")]
        private extern static void CreateSymWriter64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symWriter);

        private static Type GetCorSymWriterSxSType()
        {
            if (s_lazyCorSymWriterSxSType == null)
            {
                // If an exception is thrown we propagate it - we want to report it every time. 
                s_lazyCorSymWriterSxSType = Marshal.GetTypeFromCLSID(new Guid(SymWriterClsid));
            }

            return s_lazyCorSymWriterSxSType;
        }

        private static object CreateSymWriterWorker()
        {
            object symWriter = null;

            // First try to load an implementation from Microsoft.DiaSymReader.Native, which supports determinism.
            if (!s_MicrosoftDiaSymReaderNativeLoadFailed)
            {
                try
                {
                    var guid = new Guid(SymWriterClsid);
                    if (IntPtr.Size == 4)
                    {
                        CreateSymWriter32(ref guid, out symWriter);
                    }
                    else
                    {
                        CreateSymWriter64(ref guid, out symWriter);
                    }
                }
                catch (Exception)
                {
                    s_MicrosoftDiaSymReaderNativeLoadFailed = true;
                    symWriter = null;
                }
            }

            if (symWriter == null)
            {
                // Try to find a registered CLR implementation
                symWriter = Activator.CreateInstance(GetCorSymWriterSxSType());
            }

            return symWriter;
        }

        public void SetMetadataEmitter(MetadataWriter metadataWriter)
        {
            try
            {
                var symWriter = (ISymUnmanagedWriter5)(_symWriterFactory != null ? _symWriterFactory() : CreateSymWriterWorker());

                // Correctness: If the stream is not specified or if it is non-empty the SymWriter appends data to it (provided it contains valid PDB)
                // and the resulting PDB has Age = existing_age + 1.
                _pdbStream = new ComMemoryStream();

                if (_deterministic)
                {
                    if (!(symWriter is ISymUnmanagedWriter7))
                    {
                        throw new NotSupportedException(CodeAnalysisResources.SymWriterNotDeterministic);
                    }

                    ((ISymUnmanagedWriter7)symWriter).InitializeDeterministic(new PdbMetadataWrapper(metadataWriter), _pdbStream);
                }
                else
                {
                    symWriter.Initialize(new PdbMetadataWrapper(metadataWriter), _fileName, _pdbStream, fullBuild: true);
                }

                _metadataWriter = metadataWriter;
                _symWriter = symWriter;
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public unsafe ContentId GetContentId()
        {
            if (_deterministic)
            {
                // rewrite GUID and timestamp in the PDB with hash of a has of the log content:
                byte[] hash = _callLogger.GetLogHash();

                try
                {
                    fixed (byte* hashPtr = &hash[0])
                    {
                        ((ISymUnmanagedWriter7)_symWriter).UpdateSignatureByHashingContent(hashPtr, hash.Length);
                    }
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }

            // See symwrite.cpp - the data byte[] doesn't depend on the content of metadata tables or IL.
            // The writer only sets two values of the ImageDebugDirectory struct.
            // 
            //   IMAGE_DEBUG_DIRECTORY *pIDD
            // 
            //   if ( pIDD == NULL ) return E_INVALIDARG;
            //   memset( pIDD, 0, sizeof( *pIDD ) );
            //   pIDD->Type = IMAGE_DEBUG_TYPE_CODEVIEW;
            //   pIDD->SizeOfData = cTheData;

            ImageDebugDirectory debugDir = new ImageDebugDirectory();
            uint dataLength;

            try
            {
                _symWriter.GetDebugInfo(ref debugDir, 0, out dataLength, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }

            byte[] data = new byte[dataLength];
            fixed (byte* pb = data)
            {
                try
                {
                    _symWriter.GetDebugInfo(ref debugDir, dataLength, out dataLength, (IntPtr)pb);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }

            // Data has the following structure:
            // struct RSDSI                     
            // {
            //     DWORD dwSig;                 // "RSDS"
            //     GUID guidSig;                // GUID
            //     DWORD age;                   // age
            //     char szPDB[0];               // zero-terminated UTF8 file name passed to the writer
            // };
            const int GuidSize = 16;
            byte[] guidBytes = new byte[GuidSize];
            Buffer.BlockCopy(data, 4, guidBytes, 0, guidBytes.Length);

            // Retrieve the timestamp the PDB writer generates when creating a new PDB stream.
            // Note that ImageDebugDirectory.TimeDateStamp is not set by GetDebugInfo, 
            // we need to go through IPdbWriter interface to get it.
            uint stamp;
            uint age;
            ((IPdbWriter)_symWriter).GetSignatureAge(out stamp, out age);
            Debug.Assert(age == Age);

            Debug.Assert(BitConverter.IsLittleEndian);
            return new ContentId(guidBytes, BitConverter.GetBytes(stamp));
        }

        public void SetEntryPoint(uint entryMethodToken)
        {
            try
            {
                _symWriter.SetUserEntryPoint(entryMethodToken);
                if (_callLogger.LogOperation(OP.SetUserEntryPoint))
                {
                    _callLogger.LogArgument(entryMethodToken);
                }
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private ISymUnmanagedDocumentWriter GetDocumentWriter(DebugSourceDocument document)
        {
            ISymUnmanagedDocumentWriter writer;
            if (!_documentMap.TryGetValue(document, out writer))
            {
                Guid language = document.Language;
                Guid vendor = document.LanguageVendor;
                Guid type = document.DocumentType;

                try
                {
                    writer = _symWriter.DefineDocument(document.Location, ref language, ref vendor, ref type);
                    if (_callLogger.LogOperation(OP.DefineDocument))
                    {
                        _callLogger.LogArgument(document.Location);
                        _callLogger.LogArgument(language.ToByteArray());
                        _callLogger.LogArgument(vendor.ToByteArray());
                        _callLogger.LogArgument(type.ToByteArray());
                    }
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }

                _documentMap.Add(document, writer);

                var checksumAndAlgorithm = document.ChecksumAndAlgorithm;
                if (!checksumAndAlgorithm.Item1.IsDefault)
                {
                    try
                    {
                        var algorithmId = checksumAndAlgorithm.Item2;
                        var checksum = checksumAndAlgorithm.Item1.ToArray();
                        var checksumSize = (uint)checksum.Length;
                        writer.SetCheckSum(algorithmId, checksumSize, checksum);
                        if (_callLogger.LogOperation(OP.SetCheckSum))
                        {
                            _callLogger.LogArgument(algorithmId.ToByteArray());
                            _callLogger.LogArgument(checksumSize);
                            _callLogger.LogArgument(checksum);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new PdbWritingException(ex);
                    }
                }
            }

            return writer;
        }

        private void OpenMethod(uint methodToken, IMethodDefinition method)
        {
            try
            {
                _symWriter.OpenMethod(methodToken);
                if (_callLogger.LogOperation(OP.OpenMethod))
                {
                    _callLogger.LogArgument(methodToken);
                    // The PDB writer calls back into the PE writer to identify the method's fully qualified name.
                    // So we log that. Note that this will be the same for overloaded methods.
                    _callLogger.LogArgument(GetOrCreateSerializedTypeName(method.ContainingTypeDefinition));
                    _callLogger.LogArgument(method.Name);
                }

                // open outermost scope:
                _symWriter.OpenScope(startOffset: 0);
                if (_callLogger.LogOperation(OP.OpenScope))
                {
                    _callLogger.LogArgument((uint)0);
                }
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void CloseMethod(int ilLength)
        {
            try
            {
                // close the root scope:
                CloseScope(endOffset: ilLength);

                _symWriter.CloseMethod();
                _callLogger.LogOperation(OP.CloseMethod);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void OpenScope(int offset)
        {
            try
            {
                _symWriter.OpenScope((uint)offset);
                if (_callLogger.LogOperation(OP.OpenScope))
                {
                    _callLogger.LogArgument((uint)offset);
                }
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void CloseScope(int endOffset)
        {
            try
            {
                _symWriter.CloseScope((uint)endOffset);
                if (_callLogger.LogOperation(OP.CloseScope))
                {
                    _callLogger.LogArgument((uint)endOffset);
                }
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void UsingNamespace(string fullName, INamedEntity errorEntity)
        {
            if (_metadataWriter.IsUsingStringTooLong(fullName, errorEntity))
            {
                return;
            }

            try
            {
                _symWriter.UsingNamespace(fullName);
                if (_callLogger.LogOperation(OP.UsingNamespace))
                {
                    _callLogger.LogArgument(fullName);
                }
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void CreateSequencePointBuffers(int capacity)
        {
            _sequencePointOffsets = new uint[capacity];
            _sequencePointStartLines = new uint[capacity];
            _sequencePointStartColumns = new uint[capacity];
            _sequencePointEndLines = new uint[capacity];
            _sequencePointEndColumns = new uint[capacity];
        }

        private void ResizeSequencePointBuffers()
        {
            int newCapacity = (_sequencePointOffsets.Length + 1) * 2;
            Array.Resize(ref _sequencePointOffsets, newCapacity);
            Array.Resize(ref _sequencePointStartLines, newCapacity);
            Array.Resize(ref _sequencePointStartColumns, newCapacity);
            Array.Resize(ref _sequencePointEndLines, newCapacity);
            Array.Resize(ref _sequencePointEndColumns, newCapacity);
        }

        private void EmitSequencePoints(ArrayBuilder<Cci.SequencePoint> sequencePoints)
        {
            DebugSourceDocument document = null;
            ISymUnmanagedDocumentWriter symDocumentWriter = null;

            int i = 0;
            foreach (var sequencePoint in sequencePoints)
            {
                Debug.Assert(sequencePoint.Document != null);

                if (document != sequencePoint.Document)
                {
                    if (i > 0)
                    {
                        WriteSequencePoints(symDocumentWriter, i);
                    }

                    document = sequencePoint.Document;
                    symDocumentWriter = GetDocumentWriter(document);
                    i = 0;
                }

                if (i == _sequencePointOffsets.Length)
                {
                    ResizeSequencePointBuffers();
                }

                _sequencePointOffsets[i] = (uint)sequencePoint.Offset;
                _sequencePointStartLines[i] = (uint)sequencePoint.StartLine;
                _sequencePointStartColumns[i] = (uint)sequencePoint.StartColumn;
                _sequencePointEndLines[i] = (uint)sequencePoint.EndLine;
                _sequencePointEndColumns[i] = (uint)sequencePoint.EndColumn;
                i++;
            }

            if (i > 0)
            {
                WriteSequencePoints(symDocumentWriter, i);
            }
        }

        private void WriteSequencePoints(ISymUnmanagedDocumentWriter symDocument, int count)
        {
            try
            {
                _symWriter.DefineSequencePoints(
                    symDocument,
                    (uint)count,
                    _sequencePointOffsets,
                    _sequencePointStartLines,
                    _sequencePointStartColumns,
                    _sequencePointEndLines,
                    _sequencePointEndColumns);
                if (_callLogger.LogOperation(OP.DefineSequencePoints))
                {
                    _callLogger.LogArgument((uint)count);
                    _callLogger.LogArgument(_sequencePointOffsets, count);
                    _callLogger.LogArgument(_sequencePointStartLines, count);
                    _callLogger.LogArgument(_sequencePointStartColumns, count);
                    _callLogger.LogArgument(_sequencePointEndLines, count);
                    _callLogger.LogArgument(_sequencePointEndColumns, count);
                }
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private unsafe void DefineCustomMetadata(string name, byte[] metadata)
        {
            fixed (byte* pb = metadata)
            {
                try
                {
                    // parent parameter is not used, it must be zero or the current method token passed to OpenMethod.
                    _symWriter.SetSymAttribute(0, name, (uint)metadata.Length, (IntPtr)pb);
                    if (_callLogger.LogOperation(OP.SetSymAttribute))
                    {
                        _callLogger.LogArgument((uint)0);
                        _callLogger.LogArgument(name);
                        _callLogger.LogArgument((uint)metadata.Length);
                        _callLogger.LogArgument(metadata);
                    }
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }
        }

        private void DefineLocalConstant(string name, object value, PrimitiveTypeCode typeCode, uint constantSignatureToken)
        {
            if (value == null)
            {
                // ISymUnmanagedWriter2.DefineConstant2 throws an ArgumentException
                // if you pass in null - Dev10 appears to use 0 instead.
                // (See EMITTER::VariantFromConstVal)
                value = 0;
                typeCode = PrimitiveTypeCode.Int32;
            }

            if (typeCode == PrimitiveTypeCode.String)
            {
                DefineLocalStringConstant(name, (string)value, constantSignatureToken);
            }
            else if (value is DateTime)
            {
                // Marshal.GetNativeVariantForObject would create a variant with type VT_DATE and value equal to the
                // number of days since 1899/12/30.  However, ConstantValue::VariantFromConstant in the native VB
                // compiler actually created a variant with type VT_DATE and value equal to the tick count.
                // http://blogs.msdn.com/b/ericlippert/archive/2003/09/16/eric-s-complete-guide-to-vt-date.aspx
                var dt = (DateTime)value;
                _symWriter.DefineConstant2(name, new VariantStructure(dt), constantSignatureToken);
                if (_callLogger.LogOperation(OP.DefineConstant2))
                {
                    _callLogger.LogArgument(name);
                    _callLogger.LogArgument(constantSignatureToken);
                    _callLogger.LogArgument(dt.ToBinary());
                }
            }
            else
            {
                try
                {
                    DefineLocalConstantImpl(name, value, constantSignatureToken);
                    if (_callLogger.LogOperation(OP.DefineConstant2))
                    {
                        _callLogger.LogArgument(name);
                        _callLogger.LogArgument(constantSignatureToken);
                        _callLogger.LogArgument(value);
                    }
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }
        }

        private unsafe void DefineLocalConstantImpl(string name, object value, uint sigToken)
        {
            VariantStructure variant = new VariantStructure();
            Marshal.GetNativeVariantForObject(value, new IntPtr(&variant));
            _symWriter.DefineConstant2(name, variant, sigToken);
        }

        private void DefineLocalStringConstant(string name, string value, uint constantSignatureToken)
        {
            Debug.Assert(value != null);

            // ISymUnmanagedWriter2 doesn't handle unicode strings with unmatched unicode surrogates.
            // We use the .NET UTF8 encoder to replace unmatched unicode surrogates with unicode replacement character.
            if (!MetadataHelpers.IsValidUnicodeString(value))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                value = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            }

            // EDMAURER If defining a string constant and it is too long (length limit is undocumented), this method throws
            // an ArgumentException.
            // (see EMITTER::EmitDebugLocalConst)

            try
            {
                DefineLocalConstantImpl(name, value, constantSignatureToken);
                if (_callLogger.LogOperation(OP.DefineConstant2))
                {
                    _callLogger.LogArgument(name);
                    _callLogger.LogArgument(constantSignatureToken);
                    _callLogger.LogArgument(value);
                }
            }
            catch (ArgumentException)
            {
                // writing the constant value into the PDB failed because the string value was most probably too long.
                // We will report a warning for this issue and continue writing the PDB. 
                // The effect on the debug experience is that the symbol for the constant will not be shown in the local
                // window of the debugger. Nor will the user be able to bind to it in expressions in the EE.

                //The triage team has deemed this new warning undesirable. The effects are not significant. The warning
                //is showing up in the DevDiv build more often than expected. We never warned on it before and nobody cared.
                //The proposed warning is not actionable with no source location.
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void DefineLocalVariable(uint index, string name, uint attributes, uint localVariablesSignatureToken)
        {
            const uint ADDR_IL_OFFSET = 1;
            try
            {
                _symWriter.DefineLocalVariable2(name, attributes, localVariablesSignatureToken, ADDR_IL_OFFSET, index, 0, 0, 0, 0);
                if (_callLogger.LogOperation(OP.DefineLocalVariable2))
                {
                    _callLogger.LogArgument(name);
                    _callLogger.LogArgument(attributes);
                    _callLogger.LogArgument(localVariablesSignatureToken);
                    _callLogger.LogArgument(ADDR_IL_OFFSET);
                    _callLogger.LogArgument(index);
                    _callLogger.LogArgument((uint)0);
                    _callLogger.LogArgument((uint)0);
                    _callLogger.LogArgument((uint)0);
                    _callLogger.LogArgument((uint)0);
                }
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void SetAsyncInfo(
            int thisMethodToken,
            int kickoffMethodToken,
            int catchHandlerOffset,
            ImmutableArray<int> yieldOffsets,
            ImmutableArray<int> resumeOffsets)
        {
            var asyncMethodPropertyWriter = _symWriter as ISymUnmanagedAsyncMethodPropertiesWriter;
            if (asyncMethodPropertyWriter != null)
            {
                Debug.Assert(yieldOffsets.IsEmpty == resumeOffsets.IsEmpty);
                if (!yieldOffsets.IsEmpty)
                {
                    int count = yieldOffsets.Length;

                    uint[] yields = new uint[count];
                    uint[] resumes = new uint[count];
                    uint[] methods = new uint[count];

                    for (int i = 0; i < count; i++)
                    {
                        yields[i] = (uint)yieldOffsets[i];
                        resumes[i] = (uint)resumeOffsets[i];
                        methods[i] = (uint)thisMethodToken;
                    }

                    try
                    {
                        asyncMethodPropertyWriter.DefineAsyncStepInfo((uint)count, yields, resumes, methods);
                        if (_callLogger.LogOperation(OP.DefineAsyncStepInfo))
                        {
                            _callLogger.LogArgument((uint)count);
                            _callLogger.LogArgument(yields, count);
                            _callLogger.LogArgument(resumes, count);
                            _callLogger.LogArgument(methods, count);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new PdbWritingException(ex);
                    }
                }

                try
                {
                    if (catchHandlerOffset >= 0)
                    {
                        asyncMethodPropertyWriter.DefineCatchHandlerILOffset((uint)catchHandlerOffset);
                        if (_callLogger.LogOperation(OP.DefineCatchHandlerILOffset))
                        {
                            _callLogger.LogArgument((uint)catchHandlerOffset);
                        }
                    }

                    asyncMethodPropertyWriter.DefineKickoffMethod((uint)kickoffMethodToken);

                    if (_callLogger.LogOperation(OP.DefineKickoffMethod))
                    {
                        _callLogger.LogArgument(kickoffMethodToken);
                    }
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }
        }

        [Conditional("DEBUG")]
        // Used to catch cases where file2definitions contain nonwritable definitions early
        // If left unfixed, such scenarios will lead to crashes if happen in winmdobj projects
        public void AssertAllDefinitionsHaveTokens(MultiDictionary<DebugSourceDocument, DefinitionWithLocation> file2definitions)
        {
            foreach (var kvp in file2definitions)
            {
                foreach (var definition in kvp.Value)
                {
                    int token = _metadataWriter.GetTokenForDefinition(definition.Definition);
                    Debug.Assert(token != 0);
                }
            }
        }

        // Note: only used for WinMD
        public void WriteDefinitionLocations(MultiDictionary<DebugSourceDocument, DefinitionWithLocation> file2definitions)
        {
            var writer5 = _symWriter as ISymUnmanagedWriter5;

            if ((object)writer5 != null)
            {
                // NOTE: ISymUnmanagedWriter5 reports HRESULT = 0x806D000E in case we open and close 
                //       the map without writing any records with MapTokenToSourceSpan(...)
                bool open = false;

                foreach (var kvp in file2definitions)
                {
                    ISymUnmanagedDocumentWriter docWriter = GetDocumentWriter(kvp.Key);
                    foreach (var definition in kvp.Value)
                    {
                        if (!open)
                        {
                            try
                            {
                                writer5.OpenMapTokensToSourceSpans();
                                _callLogger.LogOperation(OP.OpenMapTokensToSourceSpans);
                            }
                            catch (Exception ex)
                            {
                                throw new PdbWritingException(ex);
                            }

                            open = true;
                        }

                        uint token = (uint)_metadataWriter.GetTokenForDefinition(definition.Definition);
                        Debug.Assert(token != 0);

                        try
                        {
                            writer5.MapTokenToSourceSpan(token, docWriter,
                                definition.StartLine + 1, definition.StartColumn + 1, definition.EndLine + 1, definition.EndColumn + 1);
                            if (_callLogger.LogOperation(OP.MapTokenToSourceSpan))
                            {
                                _callLogger.LogArgument(token);
                                _callLogger.LogArgument(kvp.Key.Location); // **
                                _callLogger.LogArgument(definition.StartLine + 1);
                                _callLogger.LogArgument(definition.StartColumn + 1);
                                _callLogger.LogArgument(definition.EndLine + 1);
                                _callLogger.LogArgument(definition.EndColumn + 1);
                                // Note on the use of kcp.Key.Location above:
                                // We are attempting to log an argument that uniquely identifies the document (for
                                // which docWriter is relevant). kvp.Key.Location returns the file path, which might
                                // be unique per document, but is an expensive way to log it. Better would be to
                                // create a mapping to integers.
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new PdbWritingException(ex);
                        }
                    }
                }

                if (open)
                {
                    try
                    {
                        writer5.CloseMapTokensToSourceSpans();
                        _callLogger.LogOperation(OP.CloseMapTokensToSourceSpans);
                    }
                    catch (Exception ex)
                    {
                        throw new PdbWritingException(ex);
                    }
                }
            }
        }

        #endregion
    }
}
