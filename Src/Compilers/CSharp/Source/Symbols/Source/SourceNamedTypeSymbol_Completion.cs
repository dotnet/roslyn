using System.Diagnostics;
using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    internal partial class SourceNamedTypeSymbol
    {
        internal override void ForceComplete(
            SourceLocation locationOpt,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                // NOTE: cases that depend on GetMembers[ByName] should call RequireCompletionPartMembers.
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.MetadataName:
                    case CompletionPart.Attributes:
                        {
                            base.ForceComplete(locationOpt, cancellationToken);
                        }
                        break;

                    case CompletionPart.StartBaseType:
                    case CompletionPart.FinishBaseType:
                        {
                            if (NotePartComplete(CompletionPart.StartBaseType))
                            {
                                var diagnostics = DiagnosticBag.GetInstance();
                                CheckBase(diagnostics);
                                AddSemanticDiagnostics(diagnostics);
                                NotePartComplete(CompletionPart.FinishBaseType);
                                diagnostics.Free();
                            }
                        }
                        break;

                    case CompletionPart.StartInterfaces:
                    case CompletionPart.FinishInterfaces:
                        {
                            if (NotePartComplete(CompletionPart.StartInterfaces))
                            {
                                var diagnostics = DiagnosticBag.GetInstance();
                                CheckInterfaces(diagnostics);
                                AddSemanticDiagnostics(diagnostics);
                                NotePartComplete(CompletionPart.FinishInterfaces);
                                diagnostics.Free();
                            }
                        }
                        break;

                    case CompletionPart.TypeArguments:
                        {
                            var tmp = this.TypeArguments; // force type arguments
                        }
                        break;

                    case CompletionPart.TypeParameters:
                        {
                            // force type parameters
                            foreach (var typeParameter in this.TypeParameters)
                            {
                                typeParameter.ForceComplete(locationOpt, cancellationToken);
                            }

                            NotePartComplete(CompletionPart.TypeParameters);
                        }
                        break;

                    case CompletionPart.Members:
                        {
                            var tmp = this.GetMembersByName();
                        }
                        break;

                    case CompletionPart.TypeMembers:
                        {
                            var tmp = this.GetTypeMembers();
                        }
                        break;

                    case CompletionPart.SynthesizedExplicitImplementations:
                        {
                            var tmp = this.GetSynthesizedExplicitImplementations(cancellationToken); //force interface and base class errors to be checked
                        }
                        break;

                    case CompletionPart.StartMemberChecks:
                    case CompletionPart.FinishMemberChecks:
                        {
                            if (NotePartComplete(CompletionPart.StartMemberChecks))
                            {
                                var diagnostics = DiagnosticBag.GetInstance();
                                AfterMembersChecks(diagnostics);
                                AddSemanticDiagnostics(diagnostics);
                                NotePartComplete(CompletionPart.FinishMemberChecks);
                                diagnostics.Free();
                            }
                        }
                        break;

                    case CompletionPart.MembersCompleted:
                        {
                            ReadOnlyArray<Symbol> members = this.GetMembersUnordered();
                            bool allCompleted = true;

                            if (locationOpt == null)
                            {
                                foreach (var member in members)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    member.ForceComplete(locationOpt, cancellationToken);
                                }
                            }
                            else
                            {
                                foreach (var member in members)
                                {
                                    foreach (var loc in member.Locations)
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();

                                        var sloc = loc as SourceLocation;
                                        if (sloc == null)
                                        {
                                            continue;
                                        }

                                        if (loc.SourceTree.Equals(locationOpt.SourceTree) && loc.SourceSpan.IntersectsWith(locationOpt.SourceSpan))
                                        {
                                            member.ForceComplete(locationOpt, cancellationToken);
                                            break;
                                        }
                                    }

                                    allCompleted &= member.HasComplete(CompletionPart.All);
                                }
                            }

                            if (!allCompleted)
                            {
                                // We did not complete all members so we won't have enough information for
                                // the PointedAtManagedTypeChecks, so just kick out now.
                                goto done;
                            }

                            // We've completed all members, so we're ready for the PointedAtManagedTypeChecks;
                            // proceed to the next iteration.
                            NotePartComplete(CompletionPart.MembersCompleted);
                            break;
                        }

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        NotePartComplete(CompletionPart.All & ~CompletionPart.NamedTypeSymbolAll);
                        break;
                }

                SpinWaitComplete(incompletePart, cancellationToken);
            }

        done:
            // Don't return until we've seen all of the CompletionParts. This ensures all
            // diagnostics have been reported (not necessarily on this thread).
            CompletionPart allParts = (locationOpt == null) ? CompletionPart.NamedTypeSymbolAll : CompletionPart.NamedTypeSymbolWithLocationAll;
            SpinWaitComplete(allParts, cancellationToken);
        }
    }
}
