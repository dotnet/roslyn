// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIdentifierInfo : AbstractSyntaxTreeInfo
    {
        private const string PersistenceName = "<SyntaxTreeInfoIdentifierPersistence>";
        private const string SerializationFormat = "1";

        /// <summary>
        /// in memory cache will hold onto any info related to opened documents in primary branch or all documents in forked branch
        /// 
        /// this is not snapshot based so multiple versions of snapshots can re-use same data as long as it is relevant.
        /// </summary>
        private static readonly ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo>> cache =
            new ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo>>();

        private readonly VersionStamp version;
        private readonly BloomFilter identifierFilter;
        private readonly BloomFilter escapedIdentifierFilter;

        public SyntaxTreeIdentifierInfo(
            VersionStamp version,
            BloomFilter identifierFilter,
            BloomFilter escapedIdentifierFilter) :
            base(version)
        {
            if (identifierFilter == null)
            {
                throw new ArgumentNullException(nameof(identifierFilter));
            }

            if (escapedIdentifierFilter == null)
            {
                throw new ArgumentNullException(nameof(escapedIdentifierFilter));
            }

            this.version = version;
            this.identifierFilter = identifierFilter;
            this.escapedIdentifierFilter = escapedIdentifierFilter;
        }

        /// <summary>
        /// Returns true when the identifier is probably (but not guaranteed) to be within the
        /// syntax tree.  Returns false when the identifier is guaranteed to not be within the
        /// syntax tree.
        /// </summary>
        public bool ProbablyContainsIdentifier(string identifier)
        {
            return identifierFilter.ProbablyContains(identifier);
        }

        /// <summary>
        /// Returns true when the identifier is probably (but not guaranteed) escaped within the
        /// text of the syntax tree.  Returns false when the identifier is guaranteed to not be
        /// escaped within the text of the syntax tree.  An identifier that is not escaped within
        /// the text can be found by searching the text directly.  An identifier that is escaped can
        /// only be found by parsing the text and syntactically interpreting any escaping
        /// mechanisms found in the language ("\uXXXX" or "@XXXX" in C# or "[XXXX]" in Visual
        /// Basic).
        /// </summary>
        public bool ProbablyContainsEscapedIdentifier(string identifier)
        {
            return escapedIdentifierFilter.ProbablyContains(identifier);
        }

        public override void WriteTo(ObjectWriter writer)
        {
            this.identifierFilter.WriteTo(writer);
            this.escapedIdentifierFilter.WriteTo(writer);
        }

        public static Task<bool> PrecalculatedAsync(Document document, CancellationToken cancellationToken)
        {
            return PrecalculatedAsync(document, PersistenceName, SerializationFormat, cancellationToken);
        }

        public static async Task<SyntaxTreeIdentifierInfo> LoadAsync(Document document, CancellationToken cancellationToken)
        {
            var info = await LoadAsync(document, ReadFrom, cache, PersistenceName, SerializationFormat, cancellationToken).ConfigureAwait(false);
            return (SyntaxTreeIdentifierInfo)info;
        }

        public override Task<bool> SaveAsync(Document document, CancellationToken cancellationToken)
        {
            return SaveAsync(document, cache, PersistenceName, SerializationFormat, cancellationToken);
        }

        private static SyntaxTreeIdentifierInfo ReadFrom(ObjectReader reader, VersionStamp version)
        {
            try
            {
                var identifierFilter = BloomFilter.ReadFrom(reader);
                var escapedIdentifierFilter = BloomFilter.ReadFrom(reader);

                return new SyntaxTreeIdentifierInfo(version, identifierFilter, escapedIdentifierFilter);
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
