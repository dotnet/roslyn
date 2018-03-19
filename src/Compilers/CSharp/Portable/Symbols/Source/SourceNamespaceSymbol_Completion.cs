// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamespaceSymbol
    {
        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CompletionPart incompletePart = _state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.NameToMembersMap:
                        {
                            System.Collections.Generic.Dictionary<string, System.Collections.Immutable.ImmutableArray<NamespaceOrTypeSymbol>> tmp = GetNameToMembersMap();
                        }
                        break;

                    case CompletionPart.MembersCompleted:
                        {
                            // ensure relevant imports are complete.
                            foreach (SingleNamespaceDeclaration declaration in _mergedDeclaration.Declarations)
                            {
                                if (locationOpt == null || locationOpt.SourceTree == declaration.SyntaxReference.SyntaxTree)
                                {
                                    if (declaration.HasUsings || declaration.HasExternAliases)
                                    {
                                        this.DeclaringCompilation.GetImports(declaration).Complete(cancellationToken);
                                    }
                                }
                            }

                            System.Collections.Immutable.ImmutableArray<Symbol> members = this.GetMembers();

                            bool allCompleted = true;

                            if (this.DeclaringCompilation.Options.ConcurrentBuild)
                            {
                                ParallelOptions po = cancellationToken.CanBeCanceled
                                    ? new ParallelOptions() { CancellationToken = cancellationToken }
                                    : CSharpCompilation.DefaultParallelOptions;

                                Parallel.For(0, members.Length, po, UICultureUtilities.WithCurrentUICulture<int>(i =>
                                {
                                    Symbol member = members[i];
                                    ForceCompleteMemberByLocation(locationOpt, member, cancellationToken);
                                }));

                                foreach (Symbol member in members)
                                {
                                    if (!member.HasComplete(CompletionPart.All))
                                    {
                                        allCompleted = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                foreach (Symbol member in members)
                                {
                                    ForceCompleteMemberByLocation(locationOpt, member, cancellationToken);
                                    allCompleted = allCompleted && member.HasComplete(CompletionPart.All);
                                }
                            }

                            if (allCompleted)
                            {
                                _state.NotePartComplete(CompletionPart.MembersCompleted);
                                break;
                            }
                            else
                            {
                                // NOTE: we're going to kick out of the completion part loop after this,
                                // so not making progress isn't a problem.
                                goto done;
                            }
                        }

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        _state.NotePartComplete(CompletionPart.All & ~CompletionPart.NamespaceSymbolAll);
                        break;
                }

                _state.SpinWaitComplete(incompletePart, cancellationToken);
            }

        done:
            // Don't return until we've seen all of the CompletionParts. This ensures all
            // diagnostics have been reported (not necessarily on this thread).
            CompletionPart allParts = (locationOpt == null) ? CompletionPart.NamespaceSymbolAll : CompletionPart.NamespaceSymbolAll & ~CompletionPart.MembersCompleted;
            _state.SpinWaitComplete(allParts, cancellationToken);
        }

        internal override bool HasComplete(CompletionPart part)
        {
            return _state.HasComplete(part);
        }
    }
}
