// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    /// <summary>
    /// A reference to a .NET assembly.
    /// </summary>
    internal interface IAssemblyReference : IModuleReference
    {
        AssemblyIdentity Identity { get; }
        Version? AssemblyVersionPattern { get; }
    }

    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal readonly struct DefinitionWithLocation : IEquatable<DefinitionWithLocation>
    {
        public readonly IDefinition Definition;
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndLine;
        public readonly int EndColumn;

        public DefinitionWithLocation(IDefinition definition,
            int startLine, int startColumn, int endLine, int endColumn)
        {
            Debug.Assert(startLine >= 0);
            Debug.Assert(startColumn >= 0);
            Debug.Assert(endLine >= 0);
            Debug.Assert(endColumn >= 0);

            Definition = definition;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        private string GetDebuggerDisplay()
            => $"{Definition} => ({StartLine},{StartColumn}) - ({EndLine}, {EndColumn})";

        public override bool Equals(object? obj)
        {
            return obj is DefinitionWithLocation other && Equals(other);
        }

        public bool Equals(DefinitionWithLocation other)
        {
            return Definition == other.Definition && StartLine == other.StartLine && StartColumn == other.StartColumn && EndLine == other.EndLine && EndColumn == other.EndColumn;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(RuntimeHelpers.GetHashCode(Definition), StartLine.GetHashCode());
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
