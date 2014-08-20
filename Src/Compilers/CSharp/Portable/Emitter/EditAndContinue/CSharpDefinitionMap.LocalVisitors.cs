// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;

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
            protected abstract void VisitSwitchStatementDeclarations(SwitchStatementSyntax node);
            protected abstract void VisitIfStatementDeclarations(IfStatementSyntax node);
            protected abstract void VisitWhileStatementDeclarations(WhileStatementSyntax node);
            protected abstract void VisitDoStatementDeclarations(DoStatementSyntax node);
            protected abstract void VisitForStatementDeclarations(ForStatementSyntax node);

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

            public override void VisitSwitchStatement(SwitchStatementSyntax node)
            {
                this.VisitSwitchStatementDeclarations(node);
                base.VisitSwitchStatement(node);
            }

            public override void VisitIfStatement(IfStatementSyntax node)
            {
                this.VisitIfStatementDeclarations(node);
                base.VisitIfStatement(node);
            }

            public override void VisitWhileStatement(WhileStatementSyntax node)
            {
                this.VisitWhileStatementDeclarations(node);
                base.VisitWhileStatement(node);
            }

            public override void VisitDoStatement(DoStatementSyntax node)
            {
                this.VisitDoStatementDeclarations(node);
                base.VisitDoStatement(node);
            }

            public override void VisitForStatement(ForStatementSyntax node)
            {
                this.VisitForStatementDeclarations(node);
                base.VisitForStatement(node);
            }

            public abstract override void VisitVariableDeclarator(VariableDeclaratorSyntax node);
        }

        internal sealed class LocalSlotMapBuilder : LocalVariableDeclaratorsVisitor
        {
            [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
            private struct LocalName
            {
                public readonly string UserDefinedName;
                public readonly SynthesizedLocalKind Kind;
                public readonly int UniqueId;

                public LocalName(string name)
                {
                    this.UserDefinedName = name;
                    this.Kind = SynthesizedLocalKind.None;
                    this.UniqueId = 0;
                }

                public LocalName(SynthesizedLocalKind kind, int uniqueId)
                {
                    this.UserDefinedName = null;
                    this.Kind = kind;
                    this.UniqueId = uniqueId;
                }

                private string GetDebuggerDisplay()
                {
                    return UserDefinedName ?? string.Format("[{0}: {1}]", this.Kind, this.UniqueId);
                }
            }

            // We assume that user-defined locals are assigned slots in the order their declarations appear in source code 
            // during lowering. We also assume that synthesized variables are assigned slots in the order the syntax that produce them 
            // appear in source. To relax constraints on lowering we treat user-defined and synthesized orderings as independent.

            private readonly ImmutableArray<LocalName> localNames;
            private readonly ImmutableArray<MetadataDecoder.LocalInfo> localInfo;
            private readonly Dictionary<EncLocalInfo, int> locals;
            private int userDefinedSlotIndex;
            private int synthesizedSlotIndex;
            private int offset;

            private LocalSlotMapBuilder(
                ImmutableArray<string> localNames,
                ImmutableArray<MetadataDecoder.LocalInfo> localInfo,
                Dictionary<EncLocalInfo, int> locals)
            {
                this.localNames = localNames.SelectAsArray(ParseName);
                this.localInfo = localInfo;
                this.locals = locals;
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
                    if (!IsSlotIndex(SynthesizedLocalKind.FixedString))
                    {
                        break;
                    }

                    AddSynthesizedLocal(SynthesizedLocalKind.FixedString);
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
                var kindOpt = TryGetSlotIndex(SynthesizedLocalKind.ForEachEnumerator, SynthesizedLocalKind.ForEachArray);
                if (kindOpt != null)
                {
                    // Enumerator.
                    if (kindOpt.Value == SynthesizedLocalKind.ForEachArray)
                    {
                        // Upper bounds.
                        var kind = SynthesizedLocalKind.ForEachArrayLimit0;
                        while (IsSlotIndex(kind))
                        {
                            AddSynthesizedLocal(kind);
                            kind = (SynthesizedLocalKind)((int)kind + 1);
                        }

                        // Indices.
                        kind = SynthesizedLocalKind.ForEachArrayIndex0;
                        while (IsSlotIndex(kind))
                        {
                            AddSynthesizedLocal(kind);
                            kind = (SynthesizedLocalKind)((int)kind + 1);
                        }
                    }

                    // Loop variable.
                    string name = node.Identifier.ValueText;
                    if (IsSlotIndex(name))
                    {
                        AddUserDefinedLocal();
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
                Debug.Assert(node.Expression != null);
               
                 // Expecting one or two locals depending on which overload of Monitor.Enter is used.
                if (TryGetSlotIndex(SynthesizedLocalKind.Lock) != null)
                {
                    // If the next local is LockTaken, then the lock was emitted with the two argument
                    // overload for Monitor.Enter(). Otherwise, the single argument overload was used.
                    if (IsSlotIndex(SynthesizedLocalKind.LockTaken))
                    {
                        AddSynthesizedLocal(SynthesizedLocalKind.LockTaken);
                    }
                }

                this.offset++;
            }

            protected override void VisitUsingStatementDeclarations(UsingStatementSyntax node)
            {
                // Expecting one synthesized local for using statement with no explicit declaration.
                if (node.Declaration == null)
                {
                    Debug.Assert(node.Expression != null);

                    TryGetSlotIndex(SynthesizedLocalKind.Using);
                    this.offset++;
                }
            }

            protected override void VisitSwitchStatementDeclarations(SwitchStatementSyntax node)
            {
                // Expecting one synthesized local for conditional branch 
                TryGetSlotIndex(SynthesizedLocalKind.ConditionalBranchDiscriminator);
                this.offset++;
            }

            protected override void VisitIfStatementDeclarations(IfStatementSyntax node)
            {
                // Expecting one synthesized local for conditional branch 
                TryGetSlotIndex(SynthesizedLocalKind.ConditionalBranchDiscriminator);
                this.offset++;
            }

            protected override void VisitWhileStatementDeclarations(WhileStatementSyntax node)
            {
                // Expecting one synthesized local for conditional branch 
                TryGetSlotIndex(SynthesizedLocalKind.ConditionalBranchDiscriminator);
                this.offset++;
            }

            protected override void VisitDoStatementDeclarations(DoStatementSyntax node)
            {
                // Expecting one synthesized local for conditional branch 
                TryGetSlotIndex(SynthesizedLocalKind.ConditionalBranchDiscriminator);
                this.offset++;
            }

            protected override void VisitForStatementDeclarations(ForStatementSyntax node)
            {
                if (node.Condition != null)
                {
                    // Expecting one synthesized local for conditional branch 
                    TryGetSlotIndex(SynthesizedLocalKind.ConditionalBranchDiscriminator);
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
                    return new LocalName(name);
                }

                SynthesizedLocalKind kind;
                int uniqueId;
                if (GeneratedNames.TryParseLocalName(name, out kind, out uniqueId))
                {
                    return new LocalName(kind, uniqueId);
                }
                else
                {
                    return new LocalName(name);
                }
            }

            private bool IsSlotIndex(string name)
            {
                return this.userDefinedSlotIndex < this.localNames.Length && 
                       name == this.localNames[this.userDefinedSlotIndex].UserDefinedName;
            }

            private bool IsSlotIndex(params SynthesizedLocalKind[] kinds)
            {
                Debug.Assert(Array.IndexOf(kinds, SynthesizedLocalKind.None) < 0);

                return this.synthesizedSlotIndex < this.localNames.Length && 
                       Array.IndexOf(kinds, this.localNames[this.synthesizedSlotIndex].Kind) >= 0;
            }

            private bool TryGetSlotIndex(string name)
            {
                while (this.userDefinedSlotIndex < this.localNames.Length)
                {
                    if (IsSlotIndex(name))
                    {
                        AddUserDefinedLocal();
                        return true;
                    }

                    this.userDefinedSlotIndex++;
                }

                return false;
            }

            private SynthesizedLocalKind? TryGetSlotIndex(params SynthesizedLocalKind[] kinds)
            {
                while (this.synthesizedSlotIndex < this.localNames.Length)
                {
                    if (IsSlotIndex(kinds))
                    {
                        var localName = this.localNames[this.synthesizedSlotIndex];
                        var kind = localName.Kind;
                        AddSynthesizedLocal(kind);
                        return kind;
                    }

                    this.synthesizedSlotIndex++;
                }

                return null;
            }

            private void AddSynthesizedLocal(SynthesizedLocalKind synthesizedKind)
            {
                AddLocalImpl(ref synthesizedSlotIndex, synthesizedKind);
            }

            private void AddUserDefinedLocal()
            {
                AddLocalImpl(ref userDefinedSlotIndex, SynthesizedLocalKind.None);
            }

            private void AddLocalImpl(ref int slotIndex, SynthesizedLocalKind synthesizedKind)
            {
                var info = this.localInfo[slotIndex];

                // We do not emit custom modifiers on locals so ignore the
                // previous version of the local if it had custom modifiers.
                if (info.CustomModifiers.IsDefaultOrEmpty)
                {
                    var local = new EncLocalInfo(this.offset, (Cci.ITypeReference)info.Type, info.Constraints, (CommonSynthesizedLocalKind)synthesizedKind, info.SignatureOpt);
                    this.locals.Add(local, slotIndex);
                }

                slotIndex++;
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

            protected override void VisitSwitchStatementDeclarations(SwitchStatementSyntax node)
            {
                this.builder.Add(node);
            }

            protected override void VisitIfStatementDeclarations(IfStatementSyntax node)
            {
                this.builder.Add(node);
            }

            protected override void VisitWhileStatementDeclarations(WhileStatementSyntax node)
            {
                this.builder.Add(node);
            }

            protected override void VisitDoStatementDeclarations(DoStatementSyntax node)
            {
                this.builder.Add(node);
            }

            protected override void VisitForStatementDeclarations(ForStatementSyntax node)
            {
                if (node.Condition != null)
                {
                    this.builder.Add(node);
                }
            }
        }
    }
}
