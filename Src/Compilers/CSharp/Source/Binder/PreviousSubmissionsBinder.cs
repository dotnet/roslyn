using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    internal sealed class PreviousSubmissionsBinder : Binder
    {
        internal PreviousSubmissionsBinder(Binder next)
            : base(next)
        {
        }

        internal override Symbol ContainingMember
        {
            get
            {
                return Compilation.ScriptClass;
            }
        }

        internal static Compilation GetPreviousCompilation(Compilation currentCompilation)
        {
            // TODO (tomat): cross-language binding - for now, skip non-C# submissions 
            for (ICompilation submission = currentCompilation.PreviousSubmission; submission != null; submission = submission.PreviousSubmission)
            {
                Compilation compilation = submission as Compilation;
                if (compilation != null)
                {
                    return compilation;
                }
            }

            return null;
        }

        internal static Imports GetImports(Compilation compilation)
        {
            return ((SourceNamespaceSymbol)compilation.SourceModule.GlobalNamespace).GetBoundImports().SingleOrDefault() ?? Imports.Empty;
        }

        protected override void LookupSymbolsInSingleBinder(LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, bool diagnose)
        {
            LookupResult tmp = LookupResult.GetInstance();
            LookupResult nonViable = LookupResult.GetInstance();

            // Member definitions of different kinds hide each other (field defs hide method defs, etc.).
            // So even if the caller asks only for invocable members find any member first and then reject the result if a non-invokable is found.
            LookupOptions anyMemberLookupOptions = options & ~LookupOptions.MustBeInvocableMember;

            // TODO: optimize lookup (there might be many interactions in the chain)
            for (ICompilation commonSubmission = Compilation.PreviousSubmission; commonSubmission != null; commonSubmission = commonSubmission.PreviousSubmission)
            {
                // TODO (tomat): cross-language binding - for now, skip non-C# submissions 
                Compilation submission = commonSubmission as Compilation;
                if (submission == null)
                {
                    continue;
                }

                tmp.Clear();

                Imports imports = GetImports(submission);
                imports.LookupSymbolInAliases(this, tmp, name, arity, basesBeingResolved, anyMemberLookupOptions, diagnose);

                // If a viable using alias and a matching member are both defined in the submission an error is reported elsewhere.
                // Ignore the member in such case.
                if (!tmp.IsMultiViable && (options & LookupOptions.NamespaceAliasesOnly) == 0)
                {
                    this.LookupMembers(tmp, submission.ScriptClass, name, arity, basesBeingResolved, anyMemberLookupOptions, diagnose);
                }

                // found a non-method in the current submission:
                if (tmp.Symbols.Count > 0 && tmp.Symbols.First().Kind != SymbolKind.Method)
                {
                    if (!tmp.IsMultiViable)
                    {
                        // skip non-viable members, but remember them in case no viable members are found in previous submissions:
                        nonViable.MergePrioritized(tmp);
                        continue;
                    }

                    if (result.Symbols.Count == 0)
                    {
                        result.MergeEqual(tmp);
                    }

                    break;
                }
                
                // merge overloads:
                Debug.Assert(result.Symbols.Count == 0 || result.Symbols.All(s => s.Kind == SymbolKind.Method));
                result.MergeEqual(tmp);
            }

            // Set a proper error if we found a symbol that is not invocable but were asked for invocable only.
            // Only a single non-method can be present in the result; methods are always invocable.
            if ((options & LookupOptions.MustBeInvocableMember) != 0 && result.Symbols.Count == 1)
            {
                Symbol symbol = result.Symbols.First();
                AliasSymbol alias = symbol as AliasSymbol;
                if (alias != null)
                {
                    symbol = alias.GetAliasTarget(basesBeingResolved);
                }

                if (IsNonInvocableMember(symbol))
                {
                    result.SetFrom(LookupResult.NotInvocable(symbol, result.Symbols.First(), diagnose));
                }
            }
            else if (result.Symbols.Count == 0)
            {
                result.SetFrom(nonViable);
            }

            tmp.Free();
            nonViable.Free();
        }

        protected override void LookupAritiesInSingleBinder(HashSet<int> result, string name)
        {
            // TODO: we need tests
            // TODO: optimize lookup (there might be many interactions in the chain)
            for (ICompilation commonSubmission = Compilation.PreviousSubmission; commonSubmission != null; commonSubmission = commonSubmission.PreviousSubmission)
            {
                // TODO (tomat): cross-language binding - for now, skip non-C# submissions 
                Compilation submission = commonSubmission as Compilation;
                if (submission == null)
                {
                    continue;
                }

                GetImports(submission).LookupAritiesInAliases(result, name);
                this.LookupMemberArities(result, submission.ScriptClass, name);
            }
        }

        protected override void LookupSymbolNamesInSingleBinder(HashSet<string> result, LookupOptions options = LookupOptions.Default)
        {
            // TODO: we need tests
            // TODO: optimize lookup (there might be many interactions in the chain)
            for (ICompilation commonSubmission = Compilation.PreviousSubmission; commonSubmission != null; commonSubmission = commonSubmission.PreviousSubmission)
            {
                // TODO (tomat): cross-language binding - for now, skip non-C# submissions 
                Compilation submission = commonSubmission as Compilation;
                if (submission == null)
                {
                    continue;
                }

                GetImports(submission).LookupSymbolNamesInAliases(this, result, options);
                this.LookupMemberNames(result, submission.ScriptClass, options);
            }
        }
    }
}