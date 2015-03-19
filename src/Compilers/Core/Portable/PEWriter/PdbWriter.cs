﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    //Catch all of the exceptions originating from writing PDBs and 
    //surface them as PDB-writing failure diagnostics to the user. 
    //Unfortunately, an exception originating in a user-implemented
    //Stream derivation will come out of the symbol writer as a COMException
    //missing all of the original exception info.

    internal sealed class PdbWritingException : Exception
    {
        internal PdbWritingException(Exception inner) :
            base(inner.Message, inner)
        {
        }
    }

    /// <summary>
    /// This struct abstracts away the possible values for specifying the output information
    /// for a PDB.  It is legal to specify a file name, a stream or both.  In the case both
    /// are specified though the <see cref="Stream"/> value will be preferred.  
    /// </summary>
    /// <remarks>
    /// The file name is still used within the PDB writing code hence is not completely 
    /// redundant in the face of a <see cref="Stream"/> value.
    /// </remarks>
    internal struct PdbOutputInfo
    {
        internal static PdbOutputInfo None
        {
            get { return new PdbOutputInfo(); }
        }

        internal readonly string FileName;
        internal readonly Stream Stream;

        internal bool IsNone
        {
            get { return FileName == null && Stream == null; }
        }

        internal bool IsValid
        {
            get { return !IsNone; }
        }

        internal PdbOutputInfo(string fileName)
        {
            Debug.Assert(fileName != null);
            FileName = fileName;
            Stream = null;
        }

        internal PdbOutputInfo(Stream stream)
        {
            FileName = null;
            Stream = stream;
        }

        internal PdbOutputInfo(string fileName, Stream stream)
        {
            Debug.Assert(fileName != null);
            Debug.Assert(stream != null && stream.CanWrite);
            FileName = fileName;
            Stream = stream;
        }

        internal PdbOutputInfo WithStream(Stream stream)
        {
            return FileName != null
                ? new PdbOutputInfo(FileName, stream)
                : new PdbOutputInfo(stream);
        }
    }

    internal sealed class PdbWriter : IDisposable
    {
        internal const uint HiddenLocalAttributesValue = 1u;
        internal const uint DefaultLocalAttributesValue = 0u;

        private static Type s_lazyCorSymWriterSxSType;

        private readonly PdbOutputInfo _pdbOutputInfo;
        private readonly Func<object> _symWriterFactory;
        private MetadataWriter _metadataWriter;
        private ISymUnmanagedWriter2 _symWriter;

        private readonly Dictionary<DebugSourceDocument, ISymUnmanagedDocumentWriter> _documentMap = new Dictionary<DebugSourceDocument, ISymUnmanagedDocumentWriter>();

        // { INamespace or ITypeReference -> qualified name }
        private readonly Dictionary<object, string> _qualifiedNameCache = new Dictionary<object, string>();

        // sequence point buffers:
        private uint[] _sequencePointOffsets;
        private uint[] _sequencePointStartLines;
        private uint[] _sequencePointStartColumns;
        private uint[] _sequencePointEndLines;
        private uint[] _sequencePointEndColumns;

        public PdbWriter(PdbOutputInfo pdbOutputInfo, Func<object> symWriterFactory = null)
        {
            Debug.Assert(pdbOutputInfo.IsValid);
            _pdbOutputInfo = pdbOutputInfo;
            _symWriterFactory = symWriterFactory;
            CreateSequencePointBuffers(capacity: 64);
        }

        public void Dispose()
        {
            this.WritePdbToOutput();
            GC.SuppressFinalize(this);
        }

        ~PdbWriter()
        {
            this.WritePdbToOutput();
        }

        /// <summary>
        /// Close the PDB writer and write the contents to the location specified by the <see cref="PdbOutputInfo"/>
        /// value.  If a file name was specified this is the method which will cause it to be created.
        /// </summary>
        public void WritePdbToOutput()
        {
            try
            {
                _symWriter?.Close();
                _symWriter = null;
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

            uint methodToken = _metadataWriter.GetMethodToken(methodBody.MethodDefinition);

            OpenMethod(methodToken);

            var localScopes = methodBody.LocalScopes;

            // CCI originally didn't have the notion of the default scope that is open
            // when a method is opened. In order to reproduce CSC PDBs, this must be added. Otherwise
            // a seemingly unnecessary scope that contains only other scopes is put in the PDB.
            if (localScopes.Length > 0)
            {
                this.DefineScopeLocals(localScopes[0], localSignatureToken);
            }

            // NOTE: This is an attempt to match Dev10's apparent behavior.  For iterator methods (i.e. the method
            // that appears in source, not the synthesized ones), Dev10 only emits the ForwardIterator and IteratorLocal
            // custom debug info (e.g. there will be no information about the usings that were in scope).
            if (!isIterator)
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

            EmitSequencePoints(methodBody.GetSequencePoints());

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

            // TODO: it's not clear why we are closing a scope here with IL length:
            CloseScope(methodBody.IL.Length);

            CloseMethod();
        }

        private void DefineNamespaceScopes(IMethodBody methodBody)
        {
            var module = Module;
            bool isVisualBasic = module.GenerateVisualBasicStylePdb;

            IMethodDefinition method = methodBody.MethodDefinition;

            var namespaceScopes = methodBody.ImportScope;

            // NOTE: All extern aliases are stored on the outermost namespace scope.
            PooledHashSet<string> lazyDeclaredExternAliases = null;
            if (!isVisualBasic)
            {
                foreach (var import in GetLastScope(namespaceScopes).GetUsedNamespaces(Context))
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

            // file and namespace level
            for (IImportScope scope = namespaceScopes; scope != null; scope = scope.Parent)
            {
                foreach (UsedNamespaceOrType import in scope.GetUsedNamespaces(Context))
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

                if (defaultNamespace != null)
                {
                    // VB marks the default/root namespace with an asterisk
                    UsingNamespace("*" + defaultNamespace, module);
                }

                foreach (string assemblyName in module.LinkedAssembliesDebugInfo)
                {
                    UsingNamespace("&" + assemblyName, module);
                }

                foreach (UsedNamespaceOrType import in module.GetImports(Context))
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

        private static IImportScope GetLastScope(IImportScope scope)
        {
            while (true)
            {
                var parent = scope.Parent;
                if (parent == null)
                {
                    return scope;
                }

                scope = parent;
            }
        }

        private void DefineAssemblyReferenceAliases()
        {
            foreach (AssemblyReferenceAlias alias in Module.GetAssemblyReferenceAliases(Context))
            {
                UsingNamespace("Z" + alias.Name + " " + alias.Assembly.GetDisplayName(), Module);
            }
        }

        private string TryEncodeImport(UsedNamespaceOrType import, HashSet<string> declaredExternAliasesOpt, bool isProjectLevel)
        {
            // NOTE: Dev12 has related cases "I" and "O" in EMITTER::ComputeDebugNamespace,
            // but they were probably implementation details that do not affect roslyn.

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
                uint token = _metadataWriter.SerializeLocalConstantSignature(scopeConstant);
                if (!_metadataWriter.IsLocalNameTooLong(scopeConstant))
                {
                    DefineLocalConstant(scopeConstant.Name, scopeConstant.CompileTimeValue.Value, _metadataWriter.GetConstantTypeCode(scopeConstant), token);
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

        private static Type GetCorSymWriterSxSType()
        {
            if (s_lazyCorSymWriterSxSType == null)
            {
                // If an exception is thrown we propagate it - we want to report it every time. 
                s_lazyCorSymWriterSxSType = Marshal.GetTypeFromCLSID(new Guid("0AE2DEB0-F901-478b-BB9F-881EE8066788"));
            }

            return s_lazyCorSymWriterSxSType;
        }

        public void SetMetadataEmitter(MetadataWriter metadataWriter)
        {
            try
            {
                var instance = (ISymUnmanagedWriter2)(_symWriterFactory != null ? _symWriterFactory() : Activator.CreateInstance(GetCorSymWriterSxSType()));
                var comStream = _pdbOutputInfo.Stream != null ? new ComStreamWrapper(_pdbOutputInfo.Stream) : null;
                instance.Initialize(new PdbMetadataWrapper(metadataWriter), _pdbOutputInfo.FileName, comStream, fullBuild: true);

                _metadataWriter = metadataWriter;
                _symWriter = instance;
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public unsafe PeDebugDirectory GetDebugDirectory()
        {
            ImageDebugDirectory debugDir = new ImageDebugDirectory();
            uint dataCount;

            try
            {
                _symWriter.GetDebugInfo(ref debugDir, 0, out dataCount, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }

            // See symwrite.cpp - the data don't depend on the content of metadata tables or IL
            // 
            // struct RSDSI                     
            // {
            //     DWORD dwSig;                 // "RSDS"
            //     GUID guidSig;
            //     DWORD age;
            //     char szPDB[0];               // zero-terminated UTF8 file name
            // };
            //
            byte[] data = new byte[dataCount];

            fixed (byte* pb = data)
            {
                try
                {
                    _symWriter.GetDebugInfo(ref debugDir, dataCount, out dataCount, (IntPtr)pb);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }

            PeDebugDirectory result = new PeDebugDirectory();
            result.AddressOfRawData = (uint)debugDir.AddressOfRawData;
            result.Characteristics = (uint)debugDir.Characteristics;
            result.Data = data;
            result.MajorVersion = (ushort)debugDir.MajorVersion;
            result.MinorVersion = (ushort)debugDir.MinorVersion;
            result.PointerToRawData = (uint)debugDir.PointerToRawData;
            result.SizeOfData = (uint)debugDir.SizeOfData;
            result.TimeDateStamp = (uint)debugDir.TimeDateStamp;
            result.Type = (uint)debugDir.Type;

            return result;
        }

        public void SetEntryPoint(uint entryMethodToken)
        {
            try
            {
                _symWriter.SetUserEntryPoint(entryMethodToken);
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
                        writer.SetCheckSum(checksumAndAlgorithm.Item2, (uint)checksumAndAlgorithm.Item1.Length, checksumAndAlgorithm.Item1.ToArray());
                    }
                    catch (Exception ex)
                    {
                        throw new PdbWritingException(ex);
                    }
                }
            }

            return writer;
        }

        private void OpenMethod(uint methodToken)
        {
            try
            {
                _symWriter.OpenMethod(methodToken);
                _symWriter.OpenScope(0);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void CloseMethod()
        {
            try
            {
                _symWriter.CloseMethod();
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
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void CloseScope(int offset)
        {
            try
            {
                _symWriter.CloseScope((uint)offset);
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

        private void EmitSequencePoints(ImmutableArray<SequencePoint> sequencePoints)
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
                    // parent parameter is not used, it must be zero or the current method token passed to OpenMetod.
                    _symWriter.SetSymAttribute(0, name, (uint)metadata.Length, (IntPtr)pb);
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
                _symWriter.DefineConstant2(name, new VariantStructure((DateTime)value), constantSignatureToken);
            }
            else
            {
                try
                {
                    _symWriter.DefineConstant2(name, value, constantSignatureToken);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }
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
                _symWriter.DefineConstant2(name, value, constantSignatureToken);
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
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        private void SetAsyncInfo(
            uint thisMethodToken,
            uint kickoffMethodToken,
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
                        methods[i] = thisMethodToken;
                    }

                    try
                    {
                        asyncMethodPropertyWriter.DefineAsyncStepInfo((uint)count, yields, resumes, methods);
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
                    }
                    asyncMethodPropertyWriter.DefineKickoffMethod(kickoffMethodToken);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }
        }

        public void WriteDefinitionLocations(MultiDictionary<DebugSourceDocument, DefinitionWithLocation> file2definitions)
        {
            var writer5 = _symWriter as ISymUnmanagedWriter5;

            if ((object)writer5 != null)
            {
                // NOTE: ISymUnmanagedWriter5 reports HRESULT = 0x806D000E in case we open and close 
                //       the map without writing any resords with MapTokenToSourceSpan(...)
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
                            }
                            catch (Exception ex)
                            {
                                throw new PdbWritingException(ex);
                            }

                            open = true;
                        }

                        uint token = _metadataWriter.GetTokenForDefinition(definition.Definition);
                        Debug.Assert(token != 0);

                        try
                        {
                            writer5.MapTokenToSourceSpan(token, docWriter,
                                definition.StartLine + 1, definition.StartColumn + 1, definition.EndLine + 1, definition.EndColumn + 1);
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
