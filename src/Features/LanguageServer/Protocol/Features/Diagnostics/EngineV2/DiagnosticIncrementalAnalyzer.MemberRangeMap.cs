// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private sealed class MemberRangeMap
        {
            private static readonly Func<DocumentId, DictionaryData> s_createMap = _ => new DictionaryData();
            private static readonly Func<VersionStamp, StrongBox<int>> s_createStrongBoxMap = _ => new StrongBox<int>(0);

            private readonly ConcurrentDictionary<DocumentId, DictionaryData> _map;

            public MemberRangeMap()
            {
                _map = new ConcurrentDictionary<DocumentId, DictionaryData>(concurrencyLevel: 2, capacity: 10);
            }

            public void Remove(DocumentId documentId)
            {
                _map.TryRemove(documentId, out _);
            }

            public void UpdateMemberRange(
                DiagnosticAnalyzer analyzer, Document document, VersionStamp newVersion, int memberId, TextSpan span, MemberRanges? oldRanges)
            {
                var data = _map.GetOrAdd(document.Id, s_createMap);

                lock (data)
                {
                    // now update member range map
                    UpdateMemberRange_NoLock(data, document, newVersion, memberId, span, oldRanges?.Version ?? VersionStamp.Default);

                    // save analyzer version information
                    Touch_NoLock(data, analyzer, document, newVersion);

                    ValidateMemberRangeMap(document, newVersion);
                }
            }

            public MemberRanges GetOrCreateMemberRanges(
                DiagnosticAnalyzer analyzer,
                Document document,
                VersionStamp version)
            {
                var data = _map.GetOrAdd(document.Id, s_createMap);
                lock (data)
                {
                    if (data.VersionMap.TryGetValue(analyzer, out var existingVersion)
                        && version == existingVersion
                        && data.MemberRangeMap.TryGetValue(existingVersion, out var ranges))
                    {
                        return new MemberRanges(version, ranges);
                    }

                    Touch_NoLock(data, analyzer, document, version);

                    if (TryCreateOrGetMemberRange_NoLock(data, document, version, out ranges))
                    {
                        data.MemberRangeMap[version] = ranges;
                    }

                    ValidateMemberRangeMap(document, version);

                    return new MemberRanges(version, ranges);
                }
            }

            private void Touch_NoLock(DictionaryData data, DiagnosticAnalyzer analyzer, Document document, VersionStamp version)
            {
                if (data.VersionMap.TryGetValue(analyzer, out var oldVersion))
                {
                    DecreaseVersion_NoLock(data, oldVersion);
                }

                IncreaseVersion_NoLock(data, version);
                data.VersionMap[analyzer] = version;

                if (TryCreateOrGetMemberRange_NoLock(data, document, version, out var range))
                {
                    data.MemberRangeMap[version] = range;
                }

                ValidateVersionTracking();
            }

            private static void UpdateMemberRange_NoLock(
                DictionaryData data, Document document, VersionStamp newVersion, int memberId, TextSpan currentSpan, VersionStamp oldVersion)
            {
                if (data.MemberRangeMap.TryGetValue(newVersion, out _))
                {
                    // it is already updated by someone else.
                    return;
                }

                // if range is invalid, create new member map
                if (memberId < 0 ||
                    !data.MemberRangeMap.TryGetValue(oldVersion, out var range) ||
                    range.Length <= memberId)
                {
                    if (TryCreateOrGetMemberRange_NoLock(data, document, newVersion, out range))
                    {
                        // update version
                        data.MemberRangeMap[newVersion] = range;
                    }

                    return;
                }

                // get old span
                var oldSpan = range[memberId];

                // update member location
                var delta = currentSpan.End - oldSpan.End;
                if (delta == 0)
                {
                    // nothing changed. simply update the version
                    data.MemberRangeMap[newVersion] = range;
                    return;
                }

                // simple case
                if (range.Length - 1 == memberId)
                {
                    data.MemberRangeMap[newVersion] = range.RemoveAt(memberId).Add(currentSpan);
                    return;
                }

                // normal case
                Contract.ThrowIfFalse(document.TryGetSyntaxRoot(out var root));
                var length = root.FullSpan.Length;

                var list = new List<TextSpan>(range)
                {
                    [memberId] = currentSpan
                };

                var start = range[memberId].End;
                for (var i = memberId + 1; i < list.Count; i++)
                {
                    var span = list[i];
                    if (span.End < start)
                    {
                        continue;
                    }

                    var newStart = Math.Min(Math.Max(span.Start + delta, 0), length);
                    list[i] = new TextSpan(newStart, newStart >= length ? 0 : span.Length);
                }

                data.MemberRangeMap[newVersion] = list.ToImmutableArray();
            }

            private static void IncreaseVersion_NoLock(DictionaryData data, VersionStamp version)
            {
                var strongBox = data.VersionTrackingMap.GetOrAdd(version, s_createStrongBoxMap);

                // actually increase one
                strongBox.Value++;
            }

            private static void DecreaseVersion_NoLock(DictionaryData data, VersionStamp version)
            {
                var strongBox = data.VersionTrackingMap.GetOrAdd(version, s_createStrongBoxMap);

                // decrease
                strongBox.Value--;

                if (strongBox.Value <= 0)
                {
                    // remove those version from map
                    data.VersionTrackingMap.Remove(version);
                    data.MemberRangeMap.Remove(version);
                }
            }

            private static bool TryCreateOrGetMemberRange_NoLock(
                DictionaryData data,
                Document document,
                VersionStamp version,
                out ImmutableArray<TextSpan> range)
            {
                if (data.MemberRangeMap.TryGetValue(version, out range))
                {
                    // we already calculated this
                    return false;
                }

                var service = document.Project.LanguageServices.GetRequiredService<ISyntaxFactsService>();
                Contract.ThrowIfFalse(document.TryGetSyntaxRoot(out var root));
                range = GetMemberRange(service, root);
                return true;

                static ImmutableArray<TextSpan> GetMemberRange(ISyntaxFactsService service, SyntaxNode root)
                {
                    var members = service.GetMethodLevelMembers(root);
                    return members.Select(m => m.FullSpan).ToImmutableArray();
                }
            }

            [Conditional("DEBUG")]
            private static void ValidateMemberRangeMap(Document document, VersionStamp version)
            {
                // enable this when we want to debug some tracking issues.
#if false
            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var root = document.GetSyntaxRootAsync().Result;

            var data = this.map.GetOrAdd(document.Id, createMap);
            Contract.Requires(Enumerable.SequenceEqual(data.MemberRangeMap[version], GetMemberRange(service, root)));
#endif
            }

            [Conditional("DEBUG")]
            private void ValidateVersionTracking()
            {
                foreach (var data in _map.Values)
                {
                    var versionsInVersionMap = data.VersionMap.Values.ToSet();
                    var versionsInTrackingMap = data.VersionTrackingMap.Keys;
                    var versionsInRangeMap = data.MemberRangeMap.Keys;

                    // there shouldn't be any version that is not in the version map
                    foreach (var version in versionsInRangeMap)
                    {
                        Contract.ThrowIfFalse(versionsInVersionMap.Contains(version));
                    }

                    foreach (var version in versionsInTrackingMap)
                    {
                        Contract.ThrowIfFalse(versionsInVersionMap.Contains(version));
                    }
                }
            }

            private class DictionaryData
            {
                public readonly Dictionary<DiagnosticAnalyzer, VersionStamp> VersionMap = new Dictionary<DiagnosticAnalyzer, VersionStamp>();
                public readonly Dictionary<VersionStamp, StrongBox<int>> VersionTrackingMap = new Dictionary<VersionStamp, StrongBox<int>>();
                public readonly Dictionary<VersionStamp, ImmutableArray<TextSpan>> MemberRangeMap = new Dictionary<VersionStamp, ImmutableArray<TextSpan>>();
            }

            internal struct MemberRanges
            {
                public readonly VersionStamp Version;
                public readonly ImmutableArray<TextSpan> Ranges;

                public MemberRanges(VersionStamp version, ImmutableArray<TextSpan> ranges)
                {
                    this.Version = version;
                    this.Ranges = ranges;
                }
            }
        }
    }
}
