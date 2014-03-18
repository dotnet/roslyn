// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// This represents a single using directive (in the scope of a method body).
    /// It has a name and possibly an alias.
    /// </summary>
    internal sealed class UsedNamespaceOrType : IUsedNamespaceOrType
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

        string IUsedNamespaceOrType.TargetName { get { return name; } }

        string IUsedNamespaceOrType.Alias { get { return alias; } }

        string IUsedNamespaceOrType.ExternAlias { get { return externAlias; } }

        bool IUsedNamespaceOrType.ProjectLevel { get { return projectLevel; } }

        UsedNamespaceOrTypeKind IUsedNamespaceOrType.Kind { get { return this.kind; } }

        string IUsedNamespaceOrType.FullName
        {
            get
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
}