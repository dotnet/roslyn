// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;
using System.Collections.Immutable;

namespace Microsoft.Cci
{
    /// <summary>
    /// Target CPU types.
    /// </summary>
    internal enum Machine : ushort
    {
        // TODO: Should consider unifying this enum with MetadataReader.PEFileFlags.Machine !!!

        /// <summary>
        /// The target CPU is unknown or not specified.
        /// </summary>
        Unknown = 0x0000,
        /// <summary>
        /// Intel 386.
        /// </summary>
        I386 = 0x014C,
        /// <summary>
        /// ARM Thumb-2 little-endian.
        /// </summary>
        ARMThumb2 = 0x01c4,
        /// <summary>
        /// Intel 64
        /// </summary>
        IA64 = 0x0200,
        /// <summary>
        /// AMD64 (K8)
        /// </summary>
        AMD64 = 0x8664,
    }

    /// <summary>
    /// The kind of metadata stored in the module. For example whether the module is an executable or a manifest resource file.
    /// </summary>
    internal enum ModuleKind
    {
        /// <summary>
        /// The module is an executable with an entry point and has a console.
        /// </summary>
        ConsoleApplication,

        /// <summary>
        /// The module is an executable with an entry point and does not have a console.
        /// </summary>
        WindowsApplication,

        /// <summary>
        /// The module is a library of executable code that is dynamically linked into an application and called via the application.
        /// </summary>
        DynamicallyLinkedLibrary,

        /// <summary>
        /// The module is a .winmdobj file.
        /// </summary>
        WindowsRuntimeMetadata,

        /// <summary>
        /// The module contains no executable code. Its contents is a resource stream for the modules that reference it.
        /// </summary>
        ManifestResourceFile,

        /// <summary>
        /// The module is a library of executable code but contains no .NET metadata and is specific to a processor instruction set.
        /// </summary>
        UnmanagedDynamicallyLinkedLibrary
    }

    /// <summary>
    /// Represents a .NET assembly.
    /// </summary>
    internal interface IAssembly : IModule, IAssemblyReference
    {
        /// <summary>
        /// A list of the files that constitute the assembly. These are not the source language files that may have been
        /// used to compile the assembly, but the files that contain constituent modules of a multi-module assembly as well
        /// as any external resources. It corresonds to the File table of the .NET assembly file format.
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
        /// <summary>
        /// Identifies the culture associated with the assembly reference. Typically specified for satellite assemblies with localized resources.
        /// Empty if not specified.
        /// </summary>
        string Culture { get; }

        /// <summary>
        /// True if the implementation of the referenced assembly used at runtime is not expected to match the version seen at compile time.
        /// </summary>
        bool IsRetargetable { get; }

        /// <summary>
        /// Type of code contained in an assembly. Determines assembly binding model.
        /// </summary>
        AssemblyContentType ContentType { get; }

        /// <summary>
        /// The hashed 8 bytes of the public key of the referenced assembly. 
        /// Empty if the referenced assembly does not have a public key.
        /// </summary>
        ImmutableArray<byte> PublicKeyToken { get; }

        /// <summary>
        /// The version of the assembly reference.
        /// </summary>
        Version Version { get; }

        string GetDisplayName();
    }

    /// <summary>
    /// An object that represents a .NET module.
    /// </summary>
    internal interface IModule : IUnit, IModuleReference
    {
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
        IEnumerable<ITypeExport> GetExportedTypes(EmitContext context);

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
        /// The preferred memory address at which the module is to be loaded at runtime.
        /// </summary>
        ulong BaseAddress
        {
            get;
            // ^ ensures result > uint.MaxValue ==> this.Requires64bits;
        }

        /// <summary>
        /// The Assembly that contains this module. If this module is main module then this returns this.
        /// </summary>
        new IAssembly GetContainingAssembly(EmitContext context);

        /// <summary>
        /// Flags that control the behavior of the target operating system. CLI implementations are supposed to ignore this, but some operating system pay attention.
        /// </summary>
        ushort DllCharacteristics { get; }

        /// <summary>
        /// The method that will be called to start execution of this executable module. 
        /// </summary>
        IMethodReference EntryPoint
        {
            get;
            // ^ requires this.Kind == ModuleKind.ConsoleApplication || this.Kind == ModuleKind.WindowsApplication;
        }

        /// <summary>
        /// The alignment of sections in the module's image file.
        /// </summary>
        uint FileAlignment { get; }

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
        /// True if the module contains only IL and is processor independent.
        /// </summary>
        bool ILOnly { get; }

        /// <summary>
        /// The kind of metadata stored in this module. For example whether this module is an executable or a manifest resource file.
        /// </summary>
        ModuleKind Kind { get; }

        /// <summary>
        /// The first part of a two part version number indicating the version of the linker that produced this module. For example, the 8 in 8.0.
        /// </summary>
        byte LinkerMajorVersion { get; }

        /// <summary>
        /// The first part of a two part version number indicating the version of the linker that produced this module. For example, the 0 in 8.0.
        /// </summary>
        byte LinkerMinorVersion { get; }

        /// <summary>
        /// Specifies the target CPU. 
        /// </summary>
        Machine Machine { get; }

        /// <summary>
        /// The first part of a two part version number indicating the version of the format used to persist this module. For example, the 1 in 1.0.
        /// </summary>
        byte MetadataFormatMajorVersion { get; }

        /// <summary>
        /// The second part of a two part version number indicating the version of the format used to persist this module. For example, the 0 in 1.0.
        /// </summary>
        byte MetadataFormatMinorVersion { get; }

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
        /// A globally unique persistent identifier for this module.
        /// </summary>
        Guid PersistentIdentifier { get; }

        bool StrongNameSigned { get; }

        /// <summary>
        /// If set, the module contains instructions or assumptions that are specific to the AMD 64 bit instruction set. Setting this flag to
        /// true also sets Requires64bits to true.
        /// </summary>
        bool RequiresAmdInstructionSet { get; }

        /// <summary>
        /// If set, the module must include a machine code stub that transfers control to the virtual execution system.
        /// </summary>
        bool RequiresStartupStub { get; }

        /// <summary>
        /// If set, the module contains instructions that assume a 32 bit instruction set. For example it may depend on an address being 32 bits.
        /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
        /// </summary>
        bool Requires32bits { get; }

        /// <summary>
        /// True if the module contains only IL and is processor independent. Should there be a choice between launching as a 64-bit or 32-bit
        /// process, this setting will cause the host to launch it as a 32-bit process. 
        /// </summary>
        bool Prefers32bits { get; }

        /// <summary>
        /// If set, the module contains instructions that assume a 64 bit instruction set. For example it may depend on an address being 64 bits.
        /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
        /// </summary>
        bool Requires64bits { get; }

        /// <summary>
        /// The size of the virtual memory initially committed for the initial process heap.
        /// </summary>
        ulong SizeOfHeapCommit
        {
            get;
            // ^ ensures result > uint.MaxValue ==> this.Requires64bits;
        }

        /// <summary>
        /// The size of the virtual memory to reserve for the initial process heap.
        /// </summary>
        ulong SizeOfHeapReserve
        {
            get;
            // ^ ensures result > uint.MaxValue ==> this.Requires64bits;
        }

        /// <summary>
        /// The size of the virtual memory initially committed for the initial thread's stack.
        /// </summary>
        ulong SizeOfStackCommit
        {
            get;
            // ^ ensures result > uint.MaxValue ==> this.Requires64bits;
        }

        /// <summary>
        /// The size of the virtual memory to reserve for the initial thread's stack.
        /// </summary>
        ulong SizeOfStackReserve
        {
            get;
            // ^ ensures result > uint.MaxValue ==> this.Requires64bits;
        }

        /// <summary>
        /// Identifies the version of the CLR that is required to load this module or assembly.
        /// </summary>
        string TargetRuntimeVersion { get; }

        /// <summary>
        /// True if the instructions in this module must be compiled in such a way that the debugging experience is not compromised.
        /// To set the value of this property, add an instance of System.Diagnostics.DebuggableAttribute to the MetadataAttributes list.
        /// </summary>
        bool TrackDebugData { get; }

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

        ushort MajorSubsystemVersion { get; }
        ushort MinorSubsystemVersion { get; }

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
