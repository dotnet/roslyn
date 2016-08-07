// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    /// <summary>
    /// Represents a .NET assembly.
    /// </summary>
    internal interface IAssembly : IAssemblyReference
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
        AssemblyFlags Flags { get; }

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