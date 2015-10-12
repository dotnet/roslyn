// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal sealed class MethodMap
    {
        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        internal struct MethodLineExtent
        {
            internal sealed class MethodComparer : IComparer<MethodLineExtent>
            {
                public static readonly MethodComparer Instance = new MethodComparer();
                public int Compare(MethodLineExtent x, MethodLineExtent y) => HandleComparer.Default.Compare(x.Method, y.Method);
            }

            internal sealed class MinLineComparer : IComparer<MethodLineExtent>
            {
                public static readonly MinLineComparer Instance = new MinLineComparer();
                public int Compare(MethodLineExtent x, MethodLineExtent y) => x.MinLine - y.MinLine;
            }

            public readonly MethodDebugInformationHandle Method;
            public readonly int MinLine;
            public readonly int MaxLine;

            public MethodLineExtent(MethodDebugInformationHandle method, int minLine, int maxLine)
            {
                Method = method;
                MinLine = minLine;
                MaxLine = maxLine;
            }

            private string GetDebuggerDisplay() => $"{MetadataTokens.GetRowNumber(Method)}: [{MinLine}-{MaxLine}]";
        }

        private struct MethodsInDocument
        {
            // Consider: we could remove the MaxLine from this list and look it up in ExtensByMinLine
            public readonly ImmutableArray<MethodLineExtent> ExtentsByMethod;

            // Represents method extents partitioned into non-overlapping subsequences, each sorted by min line.
            public readonly ImmutableArray<ImmutableArray<MethodLineExtent>> ExtentsByMinLine;

            public MethodsInDocument(ImmutableArray<MethodLineExtent> extentsByMethod, ImmutableArray<ImmutableArray<MethodLineExtent>> extentsByMinLine)
            {
                ExtentsByMethod = extentsByMethod;
                ExtentsByMinLine = extentsByMinLine;
            }
        }

        private readonly IReadOnlyDictionary<DocumentHandle, MethodsInDocument> _methodsByDocument;

        public MethodMap(MetadataReader reader)
        {
            _methodsByDocument = GroupMethods(GetMethodExtents(reader));
        }

        private static IReadOnlyDictionary<DocumentHandle, MethodsInDocument> GroupMethods(IEnumerable<KeyValuePair<DocumentHandle, MethodLineExtent>> methodExtents)
        {
            var builder = new Dictionary<DocumentHandle, ImmutableArray<MethodLineExtent>.Builder>();

            foreach (var entry in methodExtents)
            {
                ImmutableArray<MethodLineExtent>.Builder existing;
                if (!builder.TryGetValue(entry.Key, out existing))
                {
                    builder[entry.Key] = existing = ImmutableArray.CreateBuilder<MethodLineExtent>();
                }

                existing.Add(entry.Value);
            }

            var result = new Dictionary<DocumentHandle, MethodsInDocument>(builder.Count);

            foreach (var entry in builder)
            {
                var extents = entry.Value;
                Debug.Assert(extents.Count > 0);

                // sort by method handle:
                extents.Sort(MethodLineExtent.MethodComparer.Instance);

                // merge spans belonging to a single method:
                int j = 0;
                for (int i = 1; i < extents.Count; i++)
                {
                    if (extents[i].Method == extents[j].Method)
                    {
                        extents[j] = new MethodLineExtent(extents[i].Method, Math.Min(extents[i].MinLine, extents[j].MinLine), Math.Max(extents[i].MaxLine, extents[j].MaxLine));
                    }
                    else
                    {
                        j++;

                        if (j < i)
                        {
                            extents[j] = extents[i];
                        }
                    }
                }

                Debug.Assert(j < extents.Count);
                extents.Count = j + 1;

                var extentsByMethod = extents.ToImmutable();

                // sort by start line:
                extents.Sort(MethodLineExtent.MinLineComparer.Instance);

                result.Add(entry.Key, new MethodsInDocument(extentsByMethod, PartitionToNonOverlappingSubsequences(extents)));
            }

            return result;
        }

        private static ImmutableArray<ImmutableArray<MethodLineExtent>> PartitionToNonOverlappingSubsequences(ImmutableArray<MethodLineExtent>.Builder extentsOrderedByMinLine)
        {
            // Most of the time method extents are non-overlapping. Only extents of anonymous methods and queries overlap methods and other lambdas.
            // The number of subsequences created below will be the max nesting level of lambdas.

            var subsequences = ImmutableArray.CreateBuilder<ImmutableArray<MethodLineExtent>.Builder>();

            foreach (var extent in extentsOrderedByMinLine)
            {
                bool placed = false;
                foreach (var subsequence in subsequences)
                {
                    if (subsequence.Count == 0 || extent.MinLine > subsequence[subsequence.Count - 1].MaxLine)
                    {
                        subsequence.Add(extent);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    var newRun = ImmutableArray.CreateBuilder<MethodLineExtent>();
                    newRun.Add(extent);
                    subsequences.Add(newRun);
                }
            }

            // make all subsequences immutable:

            var result = ImmutableArray.CreateBuilder<ImmutableArray<MethodLineExtent>>();
            foreach (var run in subsequences)
            {
                result.Add(run.ToImmutable());
            }

            return result.ToImmutable();
        }

        private static IEnumerable<KeyValuePair<DocumentHandle, MethodLineExtent>> GetMethodExtents(MetadataReader reader)
        {
            // Perf consideration:
            // We read and decode all sequence points in the file, which might be megabytes of data that need to be paged in.
            // If we stored the primary document of single-document methods in a field of MethodBody table we would only need to decode 
            // sequence point of methods that span multiple documents to build a map from Document -> Methods.
            // We can then defer decoding sequence points of methods contained in a specified document until requested.

            foreach (var methodDebugHandle in reader.MethodDebugInformation)
            {
                var methodBody = reader.GetMethodDebugInformation(methodDebugHandle);

                // no debug info for the method
                if (methodBody.SequencePointsBlob.IsNil)
                {
                    continue;
                }

                // sequence points:
                DocumentHandle currentDocument = methodBody.Document;

                int minLine = int.MaxValue;
                int maxLine = int.MinValue;
                foreach (var sequencePoint in methodBody.GetSequencePoints())
                {
                    if (sequencePoint.IsHidden)
                    {
                        continue;
                    }

                    int startLine = sequencePoint.StartLine;
                    int endLine = sequencePoint.EndLine;

                    if (sequencePoint.Document != currentDocument)
                    {
                        yield return KeyValuePair.Create(currentDocument, new MethodLineExtent(methodDebugHandle, minLine, maxLine));

                        currentDocument = sequencePoint.Document;
                        minLine = startLine;
                        maxLine = endLine;
                    }
                    else
                    {
                        if (startLine < minLine)
                        {
                            minLine = startLine;
                        }

                        if (endLine > maxLine)
                        {
                            maxLine = endLine;
                        }
                    }
                }

                yield return KeyValuePair.Create(currentDocument, new MethodLineExtent(methodDebugHandle, minLine, maxLine));
            }
        }

        public IEnumerable<MethodDebugInformationHandle> GetMethodsContainingLine(DocumentHandle documentHandle, int line)
        {
            MethodsInDocument methodsInDocument;
            if (!_methodsByDocument.TryGetValue(documentHandle, out methodsInDocument))
            {
                return null;
            }

            return EnumerateMethodsContainingLine(methodsInDocument.ExtentsByMinLine, line);
        }

        private static IEnumerable<MethodDebugInformationHandle> EnumerateMethodsContainingLine(ImmutableArray<ImmutableArray<MethodLineExtent>> extents, int line)
        {
            foreach (var subsequence in extents)
            {
                int closestFollowingExtent;
                int index = IndexOfContainingExtent(subsequence, line, out closestFollowingExtent);
                if (index >= 0)
                {
                    yield return subsequence[index].Method;
                }
            }
        }

        private static int IndexOfContainingExtent(ImmutableArray<MethodLineExtent> orderedNonOverlappingExtents, int startLine, out int closestFollowingExtent)
        {
            closestFollowingExtent = -1;

            int index = orderedNonOverlappingExtents.BinarySearch(startLine, (extent, line) => extent.MinLine - line);
            if (index >= 0)
            {
                return index;
            }

            int preceding = ~index - 1;
            if (preceding >= 0 && startLine <= orderedNonOverlappingExtents[preceding].MaxLine)
            {
                return preceding;
            }

            closestFollowingExtent = ~index;
            return -1;
        }

        internal ImmutableArray<MethodLineExtent> GetMethodExtents(DocumentHandle documentHandle)
        {
            MethodsInDocument methodsInDocument;
            if (!_methodsByDocument.TryGetValue(documentHandle, out methodsInDocument))
            {
                return ImmutableArray<MethodLineExtent>.Empty;
            }

            return methodsInDocument.ExtentsByMethod;
        }

        internal bool TryGetMethodSourceExtent(DocumentHandle documentHandle, MethodDebugInformationHandle methodHandle, out int startLine, out int endLine)
        {
            MethodsInDocument methodsInDocument;
            if (!_methodsByDocument.TryGetValue(documentHandle, out methodsInDocument))
            {
                startLine = endLine = 0;
                return false;
            }

            int index = methodsInDocument.ExtentsByMethod.BinarySearch(methodHandle, (ext, handle) => HandleComparer.Default.Compare(ext.Method, handle));
            if (index < 0)
            {
                startLine = endLine = 0;
                return false;
            }

            var extent = methodsInDocument.ExtentsByMethod[index];
            startLine = extent.MinLine;
            endLine = extent.MaxLine;
            return true;
        }

        internal IEnumerable<MethodLineExtent> EnumerateContainingOrClosestFollowingMethodExtents(DocumentHandle documentHandle, int line)
        {
            MethodsInDocument methodsInDocument;
            if (!_methodsByDocument.TryGetValue(documentHandle, out methodsInDocument))
            {
                yield break;
            }

            foreach (var subsequence in methodsInDocument.ExtentsByMinLine)
            {
                int closestFollowingExtent;
                int index = IndexOfContainingExtent(subsequence, line, out closestFollowingExtent);
                if (index >= 0)
                {
                    yield return subsequence[index];
                }
                else if (closestFollowingExtent < subsequence.Length)
                {
                    yield return subsequence[closestFollowingExtent];
                }
            }
        }
    }
}
