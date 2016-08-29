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