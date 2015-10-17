// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal class MemberRangeMap
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
            DictionaryData unused;
            _map.TryRemove(documentId, out unused);
        }

        public void Touch(DiagnosticAnalyzer analyzer, Document document, VersionStamp version)
        {
            // only touch and updateMemberRange methods are allowed to update the dictionaries
            var data = _map.GetOrAdd(document.Id, s_createMap);

            lock (data)
            {
                Touch_NoLock(data, analyzer, document, version);
            }
        }

        public void UpdateMemberRange(
            DiagnosticAnalyzer analyzer, Document document, VersionStamp newVersion, int memberId, TextSpan span, MemberRanges oldRanges)
        {
            // only touch and updateMemberRange methods are allowed to update the dictionaries
            var data = _map.GetOrAdd(document.Id, s_createMap);

            lock (data)
            {
                // now update member range map
                UpdateMemberRange_NoLock(data, document, newVersion, memberId, span, oldRanges.TextVersion);

                // save analyzer version information
                Touch_NoLock(data, analyzer, document, newVersion);

                ValidateMemberRangeMap(document, newVersion);
            }
        }

        public MemberRanges GetSavedMemberRange(DiagnosticAnalyzer analyzer, Document document)
        {
            var data = _map.GetOrAdd(document.Id, s_createMap);
            lock (data)
            {
                return GetSavedMemberRange_NoLock(data, analyzer, document);
            }
        }

        private void Touch_NoLock(DictionaryData data, DiagnosticAnalyzer analyzer, Document document, VersionStamp version)
        {
            VersionStamp oldVersion;
            if (data.VersionMap.TryGetValue(analyzer, out oldVersion))
            {
                DecreaseVersion_NoLock(data, document.Id, oldVersion);
            }

            IncreaseVersion_NoLock(data, document.Id, version);
            data.VersionMap[analyzer] = version;

            ImmutableArray<TextSpan> range;
            if (this.TryCreateOrGetMemberRange_NoLock(data, document, version, out range))
            {
                data.MemberRangeMap[version] = range;
            }

            ValidateVersionTracking();
        }

        private void UpdateMemberRange_NoLock(
            DictionaryData data, Document document, VersionStamp newVersion, int memberId, TextSpan currentSpan, VersionStamp oldVersion)
        {
            SyntaxNode root;
            Contract.ThrowIfFalse(document.TryGetSyntaxRoot(out root));

            ImmutableArray<TextSpan> range;
            if (data.MemberRangeMap.TryGetValue(newVersion, out range))
            {
                // it is already updated by someone else.
                return;
            }

            // if range is invalid, create new member map
            if (memberId < 0 ||
                !data.MemberRangeMap.TryGetValue(oldVersion, out range) ||
                range.Length <= memberId)
            {
                if (this.TryCreateOrGetMemberRange_NoLock(data, document, newVersion, out range))
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
            var length = root.FullSpan.Length;

            var list = new List<TextSpan>(range);
            list[memberId] = currentSpan;

            var start = range[memberId].End;
            for (int i = memberId + 1; i < list.Count; i++)
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

        private void IncreaseVersion_NoLock(DictionaryData data, DocumentId documentId, VersionStamp version)
        {
            var strongBox = data.VersionTrackingMap.GetOrAdd(version, s_createStrongBoxMap);

            // actually increase one
            strongBox.Value++;
        }

        private void DecreaseVersion_NoLock(DictionaryData data, DocumentId documentId, VersionStamp version)
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

        private MemberRanges GetSavedMemberRange_NoLock(DictionaryData data, DiagnosticAnalyzer analyzer, Document document)
        {
            VersionStamp version;
            SyntaxNode root;
            ImmutableArray<TextSpan> range;
            if (!data.VersionMap.TryGetValue(analyzer, out version))
            {
                // it is first time for this analyzer
                Contract.ThrowIfFalse(document.TryGetSyntaxRoot(out root));
                Contract.ThrowIfFalse(document.TryGetTextVersion(out version));

                this.TryCreateOrGetMemberRange_NoLock(data, document, version, out range);
                return new MemberRanges(version, range);
            }

            if (!data.MemberRangeMap.TryGetValue(version, out range))
            {
                // it is first time this version is used.
                Contract.ThrowIfFalse(document.TryGetSyntaxRoot(out root));

                this.TryCreateOrGetMemberRange_NoLock(data, document, version, out range);
                return new MemberRanges(version, range);
            }

            return new MemberRanges(version, range);
        }

        private bool TryCreateOrGetMemberRange_NoLock(DictionaryData data, Document document, VersionStamp version, out ImmutableArray<TextSpan> range)
        {
            if (data.MemberRangeMap.TryGetValue(version, out range))
            {
                // we already calculated this
                return false;
            }

            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            if (service == null)
            {
                // there is nothing we can do here
                range = ImmutableArray<TextSpan>.Empty;
                return false;
            }

            SyntaxNode root;
            Contract.ThrowIfFalse(document.TryGetSyntaxRoot(out root));

            range = GetMemberRange(service, root);
            return true;
        }

        private ImmutableArray<TextSpan> GetMemberRange(ISyntaxFactsService service, SyntaxNode root)
        {
            var members = service.GetMethodLevelMembers(root);
            return members.Select(m => m.FullSpan).ToImmutableArray();
        }

        [Conditional("DEBUG")]
        private void ValidateMemberRangeMap(Document document, VersionStamp version)
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
                    Contract.Requires(versionsInVersionMap.Contains(version));
                }

                foreach (var version in versionsInTrackingMap)
                {
                    Contract.Requires(versionsInVersionMap.Contains(version));
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
            public readonly VersionStamp TextVersion;
            public readonly ImmutableArray<TextSpan> Ranges;

            public MemberRanges(VersionStamp textVersion, ImmutableArray<TextSpan> ranges)
            {
                this.TextVersion = textVersion;
                this.Ranges = ranges;
            }
        }
    }
}
