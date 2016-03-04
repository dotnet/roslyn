// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.Cci
{
    /// <summary>
    /// Represents a .NET assembly.
    /// </summary>
    internal interface IAssembly : IModule, IAssemblyReference
    {
        /// <summary>
        /// A list of the files that constitute the assembly. These are not the source language files that may have been
        /// used to compile the assembly, but the files that contain constituent modules of a multi-module assembly as well
        /// as any external resources. It corresponds to the File table of the .NET assembly file format.
        /// </summary>
        IEnumerable<IFileReference> GetFiles(EmitContext context);

        /// <summary>
        /// A set of bits and bit ranges representing properties of the assembly. The value of <see cref="Flags"/> can be set
        /// from source code via the AssemblyFlags assembly custom attribute. The interpretation of the property depends on the target platform.
        /// </summary>
        uint Flags { get; }

        /// <summary>
        /// The public part of the key used to encrypt the SHA1 hash over the persisted form of this assembly. Empty or null if not specified.
        /// This value is used by the loader to decrypt an encrypted hash value stored in the assembly, which it then compares with a freshly computed hash value
        /// in order to verify the integrity of the assembly.
        /// </summary>
        ImmutableArray<byte> PublicKey { get; }

        /// <summary>
        /// The contents of the AssemblySignatureKeyAttribute
        /// </summary>
        string SignatureKey { get; }

        AssemblyHashAlgorithm HashAlgorithm { get; }
    }

    /// <summary>
    /// A reference to a .NET assembly.
    /// </summary>
    internal interface IAssemblyReference : IModuleReference
    {
        AssemblyIdentity Identity { get; }
        Version AssemblyVersionPattern { get; }
    }

    /// <summary>
    /// An object that represents a .NET module.
    /// </summary>
    internal interface IModule : IUnit, IModuleReference
    {
        ModulePropertiesForSerialization Properties { get; }

        /// <summary>
        /// Used to distinguish which style to pick while writing native PDB information.
        /// </summary>
        /// <remarks>
        /// The PDB content for custom debug information is different between Visual Basic and CSharp.
        /// E.g. C# always includes a CustomMetadata Header (MD2) that contains the namespace scope counts, where 
        /// as VB only outputs namespace imports into the namespace scopes. 
        /// C# defines forwards in that header, VB includes them into the scopes list.
        /// 
        /// Currently the compiler doesn't allow mixing C# and VB method bodies. Thus this flag can be per module.
        /// It is possible to move this flag to per-method basis but native PDB CDI forwarding would need to be adjusted accordingly.
        /// </remarks>
        bool GenerateVisualBasicStylePdb { get; }

        /// <summary>
        /// Public types defined in other modules making up this assembly and to which other assemblies may refer to via this assembly.
        /// </summary>
        IEnumerable<ITypeReference> GetExportedTypes(EmitContext context);

        /// <summary>
        /// A list of objects representing persisted instances of types that extend System.Attribute. Provides an extensible way to associate metadata
        /// with this assembly.
        /// </summary>
        IEnumerable<ICustomAttribute> AssemblyAttributes { get; }

        /// <summary>
        /// A list of objects representing persisted instances of pairs of security actions and sets of security permissions.
        /// These apply by default to every method reachable from the module.
        /// </summary>
        IEnumerable<SecurityAttribute> AssemblySecurityAttributes { get; }

        /// <summary>
        /// A list of the assemblies that are referenced by this module.
        /// </summary>
        IEnumerable<IAssemblyReference> GetAssemblyReferences(EmitContext context);

        /// <summary>
        /// A list of named byte sequences persisted with the assembly and used during execution, typically via .NET Framework helper classes.
        /// </summary>
        IEnumerable<ManagedResource> GetResources(EmitContext context);

        /// <summary>
        /// CorLibrary assembly referenced by this module.
        /// </summary>
        IAssemblyReference GetCorLibrary(EmitContext context);

        /// <summary>
        /// The Assembly that contains this module. If this module is main module then this returns this.
        /// </summary>
        new IAssembly GetContainingAssembly(EmitContext context);

        /// <summary>
        /// The method that will be called to start execution of this executable module. 
        /// </summary>
        IMethodReference PEEntryPoint { get; }

        IMethodReference DebugEntryPoint { get; }

        /// <summary>
        /// Returns zero or more strings used in the module. If the module is produced by reading in a CLR PE file, then this will be the contents
        /// of the user string heap. If the module is produced some other way, the method may return an empty enumeration or an enumeration that is a
        /// subset of the strings actually used in the module. The main purpose of this method is to provide a way to control the order of strings in a
        /// prefix of the user string heap when writing out a module as a PE file.
        /// </summary>
        IEnumerable<string> GetStrings();

        /// <summary>
        /// Returns all top-level (not nested) types defined in the current module. 
        /// </summary>
        IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes(EmitContext context);

        /// <summary>
        /// The kind of metadata stored in this module. For example whether this module is an executable or a manifest resource file.
        /// </summary>
        OutputKind Kind { get; }

        /// <summary>
        /// A list of objects representing persisted instances of types that extend System.Attribute. Provides an extensible way to associate metadata
        /// with this module.
        /// </summary>
        IEnumerable<ICustomAttribute> ModuleAttributes { get; }

        /// <summary>
        /// The name of the module.
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// A list of the modules that are referenced by this module.
        /// </summary>
        IEnumerable<IModuleReference> ModuleReferences { get; }

        /// <summary>
        /// A list of named byte sequences persisted with the module and used during execution, typically via the Win32 API.
        /// A module will define Win32 resources rather than "managed" resources mainly to present metadata to legacy tools
        /// and not typically use the data in its own code. 
        /// </summary>
        IEnumerable<IWin32Resource> Win32Resources { get; }

        /// <summary>
        /// An alternate form the Win32 resources may take. These represent the rsrc$01 and rsrc$02 section data and relocs
        /// from a COFF object file.
        /// </summary>
        ResourceSection Win32ResourceSection { get; }

        IAssembly AsAssembly { get; }

        ITypeReference GetPlatformType(PlatformType t, EmitContext context);

        bool IsPlatformType(ITypeReference typeRef, PlatformType t);

        IEnumerable<IReference> ReferencesInIL(out int count);

        /// <summary>
        /// Builds symbol definition to location map used for emitting token -> location info
        /// into PDB to be consumed by WinMdExp.exe tool (only applicable for /t:winmdobj)
        /// </summary>
        MultiDictionary<DebugSourceDocument, DefinitionWithLocation> GetSymbolToLocationMap();

        /// <summary>
        /// Assembly reference aliases (C# only).
        /// </summary>
        ImmutableArray<AssemblyReferenceAlias> GetAssemblyReferenceAliases(EmitContext context);

        /// <summary>
        /// Linked assembly names to be stored to native PDB (VB only).
        /// </summary>
        IEnumerable<string> LinkedAssembliesDebugInfo { get; }

        /// <summary>
        /// Project level imports (VB only, TODO: C# scripts).
        /// </summary>
        ImmutableArray<UsedNamespaceOrType> GetImports();

        /// <summary>
        /// Default namespace (VB only).
        /// </summary>
        string DefaultNamespace { get; }

        // An approximate number of method definitions that can
        // provide a basis for approximating the capacities of
        // various databases used during Emit.
        int HintNumberOfMethodDefinitions { get; }
    }

    internal struct DefinitionWithLocation
    {
        public readonly IDefinition Definition;
        public readonly uint StartLine;
        public readonly uint StartColumn;
        public readonly uint EndLine;
        public readonly uint EndColumn;

        public DefinitionWithLocation(IDefinition definition,
            int startLine, int startColumn, int endLine, int endColumn)
        {
            Debug.Assert(startLine >= 0);
            Debug.Assert(startColumn >= 0);
            Debug.Assert(endLine >= 0);
            Debug.Assert(endColumn >= 0);

            this.Definition = definition;
            this.StartLine = (uint)startLine;
            this.StartColumn = (uint)startColumn;
            this.EndLine = (uint)endLine;
            this.EndColumn = (uint)endColumn;
        }

        public override string ToString()
        {
            return string.Format(
                "{0} => start:{1}/{2}, end:{3}/{4}",
                this.Definition.ToString(),
                this.StartLine.ToString(), this.StartColumn.ToString(),
                this.EndLine.ToString(), this.EndColumn.ToString());
        }
    }

    /// <summary>
    /// A reference to a .NET module.
    /// </summary>
    internal interface IModuleReference : IUnitReference
    {
        /// <summary>
        /// The Assembly that contains this module. May be null if the module is not part of an assembly.
        /// </summary>
        IAssemblyReference GetContainingAssembly(EmitContext context);
    }

    /// <summary>
    /// A unit of metadata stored as a single artifact and potentially produced and revised independently from other units.
    /// Examples of units include .NET assemblies and modules, as well C++ object files and compiled headers.
    /// </summary>
    internal interface IUnit : IUnitReference, IDefinition
    {
    }

    /// <summary>
    /// A reference to a instance of <see cref="IUnit"/>.
    /// </summary>
    internal interface IUnitReference : IReference, INamedEntity
    {
    }
}