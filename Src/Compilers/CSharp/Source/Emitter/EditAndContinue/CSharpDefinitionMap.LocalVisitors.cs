// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    partial class CSharpDefinitionMap
    {
        /// <summary>
        /// A base class for visiting all variable declarators.
        /// </summary>
        internal abstract class LocalVariableDeclaratorsVisitor : CSharpSyntaxWalker
        {
            protected abstract void VisitFixedStatementDeclarations(FixedStatementSyntax node);
            protected abstract void VisitForEachStatementDeclarations(ForEachStatementSyntax node);
            protected abstract void VisitLockStatementDeclarations(LockStatementSyntax node);
            protected abstract void VisitUsingStatementDeclarations(UsingStatementSyntax node);

            public sealed override void VisitFixedStatement(FixedStatementSyntax node)
            {
                this.VisitFixedStatementDeclarations(node);
                this.Visit(node.Statement);
            }

            public sealed override void VisitForEachStatement(ForEachStatementSyntax node)
            {
                this.VisitForEachStatementDeclarations(node);
                base.VisitForEachStatement(node);
            }

            public sealed override void VisitLockStatement(LockStatementSyntax node)
            {
                this.VisitLockStatementDeclarations(node);
                base.VisitLockStatement(node);
            }

            public sealed override void VisitUsingStatement(UsingStatementSyntax node)
            {
                this.VisitUsingStatementDeclarations(node);
                base.VisitUsingStatement(node);
            }

            public abstract override void VisitVariableDeclarator(VariableDeclaratorSyntax node);
        }

        internal sealed class LocalSlotMapBuilder : LocalVariableDeclaratorsVisitor
        {
            [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
            private struct LocalName
            {
                public readonly string Name;
                public readonly TempKind Kind;
                public readonly int UniqueId;

                public LocalName(string name, TempKind kind, int uniqueId)
                {
                    this.Name = name;
                    this.Kind = kind;
                    this.UniqueId = uniqueId;
                }

                private string GetDebuggerDisplay()
                {
                    return string.Format("[{0}, {1}, {2}]", this.Kind, this.UniqueId, this.Name);
                }
            }

            private readonly ImmutableArray<LocalName> localNames;
            private readonly ImmutableArray<MetadataDecoder.LocalInfo> localInfo;
            private readonly Dictionary<EncLocalInfo, int> locals;
            private int slotIndex;
            private int offset;

            private LocalSlotMapBuilder(
                ImmutableArray<string> localNames,
                ImmutableArray<MetadataDecoder.LocalInfo> localInfo,
                Dictionary<EncLocalInfo, int> locals)
            {
                this.localNames = localNames.SelectAsArray(ParseName);
                this.localInfo = localInfo;
                this.locals = locals;
                this.slotIndex = 0;
            }

            public static Dictionary<EncLocalInfo, int> CreateMap(
                SyntaxNode syntax,
                ImmutableArray<string> localNames,
                ImmutableArray<MetadataDecoder.LocalInfo> localInfo)
            {
                var map = new Dictionary<EncLocalInfo, int>();
                var visitor = new LocalSlotMapBuilder(localNames, localInfo, map);
                visitor.Visit(syntax);
                return map;
            }

            protected override void VisitFixedStatementDeclarations(FixedStatementSyntax node)
            {
                // Expecting N variable locals followed by N temporaries.
                var declarators = node.Declaration.Variables;
                int n = declarators.Count;
                int startOffset = this.offset;
                for (int i = 0; i < n; i++)
                {
                    var declarator = declarators[i];
                    TryGetSlotIndex(declarator.Identifier.ValueText);
                    this.offset++;
                }

                int endOffset = this.offset;
                this.offset = startOffset;
                for (int i = 0; i < n; i++)
                {
                    var declarator = declarators[i];
                    if (!IsSlotIndex(TempKind.FixedString))
                    {
                        break;
                    }
                    AddLocal(TempKind.FixedString);
                    this.offset++;
                }

                Debug.Assert(this.offset <= endOffset);
                this.offset = endOffset;
            }

            protected override void VisitForEachStatementDeclarations(ForEachStatementSyntax node)
            {
                // Expecting two or more locals: one for the enumerator,
                // for arrays one local for each upper bound and one for
                // each index, and finally one local for the loop variable.
                var kindOpt = TryGetSlotIndex(TempKind.ForEachEnumerator, TempKind.ForEachArray);
                if (kindOpt != null)
                {
                    // Enumerator.
                    if (kindOpt.Value == TempKind.ForEachArray)
                    {
                        // Upper bounds.
                        var kind = TempKind.ForEachArrayLimit0;
                        while (IsSlotIndex(kind))
                        {
                            AddLocal(kind);
                            kind = (TempKind)((int)kind + 1);
                        }

                        // Indices.
                        kind = TempKind.ForEachArrayIndex0;
                        while (IsSlotIndex(kind))
                        {
                            AddLocal(kind);
                            kind = (TempKind)((int)kind + 1);
                        }
                    }

                    // Loop variable.
                    string name = ((ForEachStatementSyntax)node).Identifier.ValueText;
                    if (IsSlotIndex(name))
                    {
                        AddLocal(TempKind.None);
                    }
                    else
                    {
                        // TODO: Handle missing temporary.
                    }
                }

                this.offset++;
            }

            protected override void VisitLockStatementDeclarations(LockStatementSyntax node)
            {
                // Expecting one or two locals depending on which overload of Monitor.Enter is used.
                var expr = node.Expression;
                Debug.Assert(expr != null);
                if (TryGetSlotIndex(TempKind.Lock) != null)
                {
                    // If the next local is LockTaken, then the lock was emitted with the two argument
                    // overload for Monitor.Enter(). Otherwise, the single argument overload was used.
                    if (IsSlotIndex(TempKind.LockTaken))
                    {
                        AddLocal(TempKind.LockTaken);
                    }
                }

                this.offset++;
            }

            protected override void VisitUsingStatementDeclarations(UsingStatementSyntax node)
            {
                // Expecting one temporary for using statement with no explicit declaration.
                if (node.Declaration == null)
                {
                    var expr = node.Expression;
                    Debug.Assert(expr != null);
                    TryGetSlotIndex(TempKind.Using);

                    this.offset++;
                }
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                TryGetSlotIndex(node.Identifier.ValueText);
                this.offset++;
            }

            private static LocalName ParseName(string name)
            {
                if (name == null)
                {
                    return new LocalName(name, TempKind.None, 0);
                }

                TempKind kind;
                int uniqueId;
                GeneratedNames.TryParseTemporaryName(name, out kind, out uniqueId);
                return new LocalName(name, kind, uniqueId);
            }

            private bool IsSlotIndex(string name)
            {
                return (this.slotIndex < this.localNames.Length) &&
                    (this.localNames[this.slotIndex].Kind == TempKind.None) &&
                    (name == this.localNames[this.slotIndex].Name);
            }

            private bool IsSlotIndex(params TempKind[] kinds)
            {
                Debug.Assert(Array.IndexOf(kinds, TempKind.None) < 0);

                return (this.slotIndex < this.localNames.Length) &&
                    (Array.IndexOf(kinds, this.localNames[this.slotIndex].Kind) >= 0);
            }

            private bool TryGetSlotIndex(string name)
            {
                while (this.slotIndex < this.localNames.Length)
                {
                    if (IsSlotIndex(name))
                    {
                        AddLocal(TempKind.None);
                        return true;
                    }
                    this.slotIndex++;
                }

                return false;
            }

            private TempKind? TryGetSlotIndex(params TempKind[] kinds)
            {
                while (this.slotIndex < this.localNames.Length)
                {
                    if (IsSlotIndex(kinds))
                    {
                        var localName = this.localNames[this.slotIndex];
                        var kind = localName.Kind;
                        AddLocal(kind);
                        return kind;
                    }
                    this.slotIndex++;
                }

                return null;
            }

            private void AddLocal(TempKind tempKind)
            {
                var info = this.localInfo[this.slotIndex];

                // We do not emit custom modifiers on locals so ignore the
                // previous version of the local if it had custom modifiers.
                if (info.CustomModifiers.IsDefaultOrEmpty)
                {
                    var constraints = GetConstraints(info);
                    var local = new EncLocalInfo(this.offset, (Cci.ITypeReference)info.Type, constraints, (int)tempKind);
                    this.locals.Add(local, this.slotIndex);
                }

                this.slotIndex++;
            }
        }

        internal sealed class LocalVariableDeclaratorsCollector : LocalVariableDeclaratorsVisitor
        {
            internal static ImmutableArray<SyntaxNode> GetDeclarators(IMethodSymbol method)
            {
                var syntaxRefs = method.DeclaringSyntaxReferences;
                // No syntax refs for synthesized methods.
                if (syntaxRefs.Length == 0)
                {
                    return ImmutableArray<SyntaxNode>.Empty;
                }

                var syntax = syntaxRefs[0].GetSyntax();
                var builder = ArrayBuilder<SyntaxNode>.GetInstance();
                var visitor = new LocalVariableDeclaratorsCollector(builder);
                visitor.Visit(syntax);
                return builder.ToImmutableAndFree();
            }

            private readonly ArrayBuilder<SyntaxNode> builder;

            private LocalVariableDeclaratorsCollector(ArrayBuilder<SyntaxNode> builder)
            {
                this.builder = builder;
            }

            protected override void VisitFixedStatementDeclarations(FixedStatementSyntax node)
            {
                foreach (var declarator in node.Declaration.Variables)
                {
                    this.builder.Add(declarator);
                }
            }

            protected override void VisitForEachStatementDeclarations(ForEachStatementSyntax node)
            {
                this.builder.Add(node);
            }

            protected override void VisitLockStatementDeclarations(LockStatementSyntax node)
            {
                var expr = node.Expression;
                Debug.Assert(expr != null);
                this.builder.Add(expr);
            }

            protected override void VisitUsingStatementDeclarations(UsingStatementSyntax node)
            {
                if (node.Declaration == null)
                {
                    var expr = node.Expression;
                    Debug.Assert(expr != null);

                    this.builder.Add(expr);
                }
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                this.builder.Add(node);
            }
        }
    }
}
