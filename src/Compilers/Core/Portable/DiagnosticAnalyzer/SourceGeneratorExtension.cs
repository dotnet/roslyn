// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal static class SourceGeneratorExtension
    {
        private static int FindIndex<T, TArg>(this IList<T> source, Func<T, TArg, bool> predicate, TArg arg)
        {
            var nItems = source.Count;
            var index = -1;
            for (int i = 0; index == -1 && i < nItems; i++)
            {
                if (predicate(source[i], arg))
                {
                    index = i;
                }
            }
            return index;
        }

        private static Type UnWrapType(this ISourceGenerator source)
        {
            if (source is IncrementalGeneratorWrapper ig)
            {
                return ig.Generator.GetType();
            }
            return source.GetType();
        }

        internal static IEnumerable<(int From, int To)> SortByDependency(this IList<ISourceGenerator> builder)
        {
            var rules = builder
                .Select(i =>
                {
                    var t = i.UnWrapType();
                    var fullName = t.FullName;
                    List<string> before = new();
                    List<string> after = new();
                    foreach (var attrinbute in t.GetCustomAttributes(false))
                    {
                        switch (attrinbute)
                        {
                            case GenerateAfterAttribute ga:
                                after.Add(ga.GeneratorToExecuteBefore);
                                break;
                            case GenerateBeforeAttribute gb:
                                before.Add(gb.GeneratorToExecuteAfter);
                                break;
                        }
                    }
                    return new { FullName = fullName!, After = after, Before = before };
                });

            foreach (var rule in rules.Where(r => r.Before.Count + r.After.Count > 0))
            {
                var index = builder.FindIndex(FindForFullName, rule.FullName);
                if (index > -1)
                {
                    var current = builder[index];
                    var ni = index;
                    var maxAfter = ni;
                    if (rule.After.Any() == true)
                    {
                        foreach (var after in rule.After)
                        {
                            var ai = builder.FindIndex(FindForFullName, after);
                            if (ai > ni)
                            {
                                builder.RemoveAt(ni);
                                builder.Insert(ai, current);
                                ni = ai;
                                yield return (ni, ai);
                            }
                        }
                        maxAfter = ni;
                    }
                    if (rule.Before.Any() == true)
                    {
                        foreach (var before in rule.Before)
                        {
                            var bi = builder.FindIndex(FindForFullName, before);
                            if (bi < ni)
                            {
                                var item = builder[bi];
                                builder.RemoveAt(bi);
                                builder.Insert(ni, item);
                                ni = ni - 1;
                                yield return (bi, ni);
                            }
                        }
                    }
                }
            }

            static bool FindForFullName(ISourceGenerator generator, string fullName) =>
                generator.UnWrapType().FullName == fullName;

        }
    }
}
