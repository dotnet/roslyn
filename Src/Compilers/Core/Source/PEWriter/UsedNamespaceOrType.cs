// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    /// <summary>
    /// This represents a single using directive (in the scope of a method body).
    /// It has a name and possibly an alias.
    /// </summary>
    /// <remarks>
    /// A namespace that is used (imported) inside a namespace scope.
    /// 
    /// Kind            | Example                   | Alias     | TargetName
    /// ----------------+---------------------------+-----------+-------------------
    /// Namespace       | using System;             | null      | "System"
    /// NamespaceAlias  | using S = System;         | "S"       | "System"
    /// ExternNamespace | extern alias LibV1;       | "LibV1"   | null
    /// TypeAlias       | using C = System.Console; | "C"       | "System.Console"
    /// </remarks>
    internal sealed class UsedNamespaceOrType
    {
        private readonly UsedNamespaceOrTypeKind kind;
        private readonly string name;
        private readonly string alias;
        private readonly string externAlias;
        private readonly bool projectLevel;

        internal static UsedNamespaceOrType CreateCSharpNamespace(string name, string externAlias = null)
        {
            Debug.Assert(name != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.CSNamespace, name, alias: null, externAlias: externAlias);
        }

        internal static UsedNamespaceOrType CreateCSharpNamespaceAlias(string name, string alias, string externAlias = null)
        {
            Debug.Assert(name != null);
            Debug.Assert(alias != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.CSNamespaceAlias, name, alias, externAlias: externAlias);
        }

        internal static UsedNamespaceOrType CreateCSharpExternNamespace(string externAlias)
        {
            Debug.Assert(externAlias != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.CSExternNamespace, name: null, alias: null, externAlias: externAlias);
        }

        /// <remarks>
        /// <paramref name="name"/> is an assembly-qualified name so the extern alias, if any, can be dropped.
        /// </remarks>
        internal static UsedNamespaceOrType CreateCSharpTypeAlias(string name, string alias)
        {
            Debug.Assert(name != null);
            Debug.Assert(alias != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.CSTypeAlias, name, alias, externAlias: null);
        }

        internal static UsedNamespaceOrType CreateVisualBasicXmlNamespace(string name, string prefix, bool projectLevel)
        {
            Debug.Assert(name != null);
            Debug.Assert(prefix != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.VBXmlNamespace, name, prefix, externAlias: null, projectLevel: projectLevel);
        }

        internal static UsedNamespaceOrType CreateVisualBasicNamespace(string name, bool projectLevel)
        {
            Debug.Assert(name != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.VBNamespace, name, alias: null, externAlias: null, projectLevel: projectLevel);
        }

        internal static UsedNamespaceOrType CreateVisualBasicType(string name, bool projectLevel)
        {
            Debug.Assert(name != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.VBType, name, alias: null, externAlias: null, projectLevel: projectLevel);
        }

        internal static UsedNamespaceOrType CreateVisualBasicNamespaceOrTypeAlias(string name, string alias, bool projectLevel)
        {
            Debug.Assert(name != null);
            Debug.Assert(alias != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.VBNamespaceOrTypeAlias, name, alias, externAlias: null, projectLevel: projectLevel);
        }

        internal static UsedNamespaceOrType CreateVisualBasicCurrentNamespace(string name)
        {
            Debug.Assert(name != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.VBCurrentNamespace, name, null, externAlias: null, projectLevel: false);
        }

        internal static UsedNamespaceOrType CreateVisualBasicDefaultNamespace(string name)
        {
            Debug.Assert(name != null);
            return new UsedNamespaceOrType(UsedNamespaceOrTypeKind.VBDefaultNamespace, name, null, externAlias: null, projectLevel: true);
        }

        private UsedNamespaceOrType(UsedNamespaceOrTypeKind kind, string name, string alias, string externAlias, bool projectLevel = false)
        {
            this.kind = kind;
            this.name = name;
            this.alias = alias;
            this.externAlias = externAlias;
            this.projectLevel = projectLevel;
        }

        /// <summary>
        /// The name of a namepace that has been aliased.  For example the "y.z" of "using x = y.z;" or "using y.z" in C#.
        /// </summary>
        public string TargetName { get { return name; } }

        /// <summary>
        /// An alias for a namespace. For example the "x" of "using x = y.z;" in C#. Empty if no alias is present.
        /// </summary>
        public string Alias { get { return alias; } }

        /// <summary>
        /// The name of an extern alias that has been used to qualify a name.  For example the "Q" of "using x = Q::y.z;" or "using Q::y.z" in C#.
        /// </summary>
        public string ExternAlias { get { return externAlias; } }

        /// <summary>
        /// Indicates whether the import was specified on a project level, or on file level (used for VB only)
        /// </summary>
        public bool ProjectLevel { get { return projectLevel; } }

        /// <summary>
        /// Distinguishes the various kinds of targets.
        /// </summary>
        public UsedNamespaceOrTypeKind Kind { get { return this.kind; } }

        /// <summary>
        /// Returns an encoded name for this used type or namespace. The encoding is dependent on the <see cref="UsedNamespaceOrTypeKind"/>.
        /// </summary>
        public string Encode()
        {
            // NOTE: Dev12 has related cases "I" and "O" in EMITTER::ComputeDebugNamespace,
            // but they were probably implementation details that do not affect roslyn.
            switch (this.kind)
            {
                case UsedNamespaceOrTypeKind.CSNamespace:
                    return this.externAlias == null
                        ? "U" + this.name
                        : "E" + this.name + " " + this.externAlias;

                case UsedNamespaceOrTypeKind.CSNamespaceAlias:
                    return this.externAlias == null
                        ? "A" + this.alias + " U" + this.name
                        : "A" + this.alias + " E" + this.name + " " + this.externAlias;

                case UsedNamespaceOrTypeKind.CSExternNamespace:
                    return "X" + this.externAlias;

                case UsedNamespaceOrTypeKind.CSTypeAlias:
                    Debug.Assert(this.externAlias == null);
                    return "A" + this.alias + " T" + this.name;

                case UsedNamespaceOrTypeKind.VBNamespace:
                    return (this.projectLevel ? "@P:" : "@F:") + this.name;

                case UsedNamespaceOrTypeKind.VBNamespaceOrTypeAlias:
                    return (this.projectLevel ? "@PA:" : "@FA:") + this.alias + "=" + this.name;

                case UsedNamespaceOrTypeKind.VBXmlNamespace:
                    return (this.projectLevel ? "@PX:" : "@FX:") + this.alias + "=" + this.name;

                case UsedNamespaceOrTypeKind.VBType:
                    return (this.projectLevel ? "@PT:" : "@FT:") + this.name;

                case UsedNamespaceOrTypeKind.VBCurrentNamespace:
                    // VB appends the namespace of the container without prefixes
                    return this.name;

                case UsedNamespaceOrTypeKind.VBDefaultNamespace:
                    // VB marks the default/root namespace with an asteriks
                    return "*" + this.name;

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.kind);
            }
        }
    }
}