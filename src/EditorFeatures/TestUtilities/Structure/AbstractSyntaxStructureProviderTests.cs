// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    [UseExportProvider]
    public abstract class AbstractSyntaxStructureProviderTests
    {
        protected abstract string LanguageName { get; }

        protected virtual string WorkspaceKind => CodeAnalysis.WorkspaceKind.Test;

        protected virtual OptionSet UpdateOptions(OptionSet options)
            => options.WithChangedOption(BlockStructureOptions.MaximumBannerLength, LanguageName, 120);

        private Task<ImmutableArray<BlockSpan>> GetBlockSpansAsync(Document document, int position)
            => GetBlockSpansWorkerAsync(document, position);

        internal abstract Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, int position);

        private protected async Task VerifyBlockSpansAsync(string markupCode, params Either<RegionData, BlockSpan>[] expectedRegionData)
        {
            using (var workspace = TestWorkspace.Create(WorkspaceKind, LanguageName, compilationOptions: null, parseOptions: null, content: markupCode))
            {
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(UpdateOptions(workspace.Options)));

                var hostDocument = workspace.Documents.Single();
                Assert.True(hostDocument.CursorPosition.HasValue, "Test must specify a position.");
                var position = hostDocument.CursorPosition.Value;

                var expectedRegions = expectedRegionData.Select(data => data.IsFirst ? CreateBlockSpan(data.First, hostDocument.AnnotatedSpans) : data.Second).ToArray();

                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var actualRegions = await GetBlockSpansAsync(document, position);

                Assert.True(expectedRegions.Length == actualRegions.Length, $"Expected {expectedRegions.Length} regions but there were {actualRegions.Length}");

                for (var i = 0; i < expectedRegions.Length; i++)
                {
                    AssertRegion(expectedRegions[i], actualRegions[i]);
                }
            }
        }

        protected async Task VerifyNoBlockSpansAsync(string markupCode)
        {
            using (var workspace = TestWorkspace.Create(WorkspaceKind, LanguageName, compilationOptions: null, parseOptions: null, content: markupCode))
            {
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(UpdateOptions(workspace.Options)));

                var hostDocument = workspace.Documents.Single();
                Assert.True(hostDocument.CursorPosition.HasValue, "Test must specify a position.");
                var position = hostDocument.CursorPosition.Value;

                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var actualRegions = await GetBlockSpansAsync(document, position);

                Assert.True(actualRegions.Length == 0, $"Expected no regions but found {actualRegions.Length}.");
            }
        }

        protected RegionData Region(string textSpanName, string hintSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
            => new RegionData(textSpanName, hintSpanName, bannerText, autoCollapse, isDefaultCollapsed);

        protected RegionData Region(string textSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
            => new RegionData(textSpanName, textSpanName, bannerText, autoCollapse, isDefaultCollapsed);

        private static BlockSpan CreateBlockSpan(
            RegionData regionData,
            IDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            var (textSpanName, hintSpanName, bannerText, autoCollapse, isDefaultCollapsed) = regionData;

            Assert.True(spans.ContainsKey(textSpanName) && spans[textSpanName].Length == 1, $"Test did not specify '{textSpanName}' span.");
            Assert.True(spans.ContainsKey(hintSpanName) && spans[hintSpanName].Length == 1, $"Test did not specify '{hintSpanName}' span.");

            var textSpan = spans[textSpanName][0];
            var hintSpan = spans[hintSpanName][0];

            return new BlockSpan(isCollapsible: true,
                textSpan: textSpan,
                hintSpan: hintSpan,
                type: BlockTypes.Nonstructural,
                bannerText: bannerText,
                autoCollapse: autoCollapse,
                isDefaultCollapsed: isDefaultCollapsed);
        }

        internal static void AssertRegion(BlockSpan expected, BlockSpan actual)
        {
            Assert.Equal(expected.TextSpan.Start, actual.TextSpan.Start);
            Assert.Equal(expected.TextSpan.End, actual.TextSpan.End);
            Assert.Equal(expected.HintSpan.Start, actual.HintSpan.Start);
            Assert.Equal(expected.HintSpan.End, actual.HintSpan.End);
            Assert.Equal(expected.BannerText, actual.BannerText);
            Assert.Equal(expected.AutoCollapse, actual.AutoCollapse);
            Assert.Equal(expected.IsDefaultCollapsed, actual.IsDefaultCollapsed);
        }
    }

    internal sealed class Either<T1, T2> : IEquatable<Either<T1, T2>>
    {
        private T1 _first;
        private T2 _second;
        private readonly int _current;

        public Either(T1 value)
        {
            _first = value;
            _current = 1;
        }

        public Either(T2 value)
        {
            _second = value;
            _current = 2;
        }

        public bool IsFirst => _current == 1;

        public bool IsSecond => _current == 2;

        public ref T1 First
        {
            get
            {
                if (!IsFirst)
                    throw new InvalidOperationException();

                return ref _first;
            }
        }

        public ref T2 Second
        {
            get
            {
                if (!IsSecond)
                    throw new InvalidOperationException();

                return ref _second;
            }
        }

        public static implicit operator Either<T1, T2>(T1 value)
            => new Either<T1, T2>(value);

        public static implicit operator Either<T1, T2>(T2 value)
            => new Either<T1, T2>(value);

        public static explicit operator T1(Either<T1, T2> value)
            => value.First;

        public static explicit operator T2(Either<T1, T2> value)
            => value.Second;

        public static bool operator ==(Either<T1, T2> left, Either<T1, T2> right)
            => EqualityComparer<Either<T1, T2>>.Default.Equals(left, right);

        public static bool operator !=(Either<T1, T2> left, Either<T1, T2> right)
            => !(left == right);

        public override bool Equals(object obj)
            => Equals(obj as Either<T1, T2>);

        public bool Equals(Either<T1, T2> other)
        {
            return other != null
                && _current == other._current
                && (!IsFirst || EqualityComparer<T1>.Default.Equals(_first, other._first))
                && (!IsSecond || EqualityComparer<T2>.Default.Equals(_second, other._second));
        }

        public override int GetHashCode()
        {
            var hashCode = -1501415095;
            if (IsFirst)
            {
                hashCode = hashCode * -1521134295 + EqualityComparer<T1>.Default.GetHashCode(_first);
            }
            else if (IsSecond)
            {
                hashCode = hashCode * -1521134295 + EqualityComparer<T2>.Default.GetHashCode(_second);
            }

            return hashCode;
        }
    }

    public readonly struct RegionData
    {
        public readonly string TextSpanName;
        public readonly string HintSpanName;
        public readonly string BannerText;
        public readonly bool AutoCollapse;
        public readonly bool IsDefaultCollapsed;

        public RegionData(string textSpanName, string hintSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed)
        {
            this.TextSpanName = textSpanName;
            this.HintSpanName = hintSpanName;
            this.BannerText = bannerText;
            this.AutoCollapse = autoCollapse;
            this.IsDefaultCollapsed = isDefaultCollapsed;
        }

        public override bool Equals(object obj)
        {
            return obj is RegionData other
                && TextSpanName == other.TextSpanName
                && HintSpanName == other.HintSpanName
                && BannerText == other.BannerText
                && AutoCollapse == other.AutoCollapse
                && IsDefaultCollapsed == other.IsDefaultCollapsed;
        }

        public override int GetHashCode()
        {
            var hashCode = -1426774128;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TextSpanName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HintSpanName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BannerText);
            hashCode = hashCode * -1521134295 + AutoCollapse.GetHashCode();
            hashCode = hashCode * -1521134295 + IsDefaultCollapsed.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out string textSpanName, out string hintSpanName, out string bannerText, out bool autoCollapse, out bool isDefaultCollapsed)
        {
            textSpanName = this.TextSpanName;
            hintSpanName = this.HintSpanName;
            bannerText = this.BannerText;
            autoCollapse = this.AutoCollapse;
            isDefaultCollapsed = this.IsDefaultCollapsed;
        }
    }
}
