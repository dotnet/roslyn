// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A Declaration summarizes the declaration structure of a source file. Each entity declaration
    /// in the program that is a container (specifically namespaces, classes, interfaces, structs,
    /// and delegate declarations) is represented by a node in this tree.  At the top level, the
    /// compilation unit is treated as a declaration of the unnamed namespace.
    /// 
    /// Special treatment is required for namespace declarations, because a single namespace
    /// declaration can declare more than one namespace.  For example, in the declaration
    /// 
    ///     namespace A.B.C {}
    ///     
    /// we see that namespaces A and B and C are declared.  This declaration is represented as three
    /// declarations. All three of these ContainerDeclaration objects contain a reference to the
    /// syntax tree for the declaration.
    /// 
    /// A "single" declaration represents a specific namespace or type declaration at a point in
    /// source code. A "root" declaration is a special single declaration which summarizes the
    /// contents of an entire file's types and namespaces.  Each source file is represented as a tree
    /// of single declarations.
    /// 
    /// A "merged" declaration merges together one or more declarations for the same symbol.  For
    /// example, the root namespace has multiple single declarations (one in each source file) but
    /// there is a single merged declaration for them all.  Similarly partial classes may have
    /// multiple declarations, grouped together under the umbrella of a merged declaration.  In the
    /// common trivial case, a merged declaration for a single declaration contains only that single
    /// declaration.  The whole program, consisting of the set of all declarations in all of the
    /// source files, is represented by a tree of merged declarations.
    /// </summary>
    internal abstract class Declaration
    {
        protected readonly string name;

        protected Declaration(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public ImmutableArray<Declaration> Children
        {
            get
            {
                return GetDeclarationChildren();
            }
        }

        public abstract DeclarationKind Kind { get; }
        protected abstract ImmutableArray<Declaration> GetDeclarationChildren();
    }
}
