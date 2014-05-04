// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System;
using System.Threading.Tasks;

namespace Microsoft.Cci
{
    internal sealed class DebugSourceDocument
    {
        internal static readonly Guid CorSymLanguageTypeCSharp = new Guid("{3f5162f8-07c6-11d3-9053-00c04fa302a1}");
        internal static readonly Guid CorSymLanguageTypeBasic = new Guid("{3a12d0b8-c26c-11d0-b442-00a0244a1dd2}");
        private static readonly Guid CorSymLanguageVendorMicrosoft = new Guid("{994b45c4-e6e9-11d2-903f-00c04fa302a1}");
        private static readonly Guid CorSymDocumentTypeText = new Guid("{5a869d0b-6611-11d3-bd2a-0000f80849bd}");
        private static readonly Guid CorSym_SourceHash_SHA1 = new Guid("{ff1816ec-aa5e-4d10-87f7-6f4963833460}");

        private string location;
        private Guid language;
        private bool isComputedChecksum;

        private Task<ImmutableArray<byte>> checkSum;
        private Guid checkSumAlgorithmId;

        public DebugSourceDocument(string location, Guid language)
        {
            this.location = location; // If it's a path, it should be normalized.
            this.language = language;
        }

        /// <summary>
        /// Use to create a document when checksum is computed based on actual source stream.
        /// </summary>
        public DebugSourceDocument(string location, Guid language, Func<ImmutableArray<byte>> checkSumSha1)
            : this(location, language)
        {
            this.checkSum = Task.Run(checkSumSha1);
            this.checkSumAlgorithmId = CorSym_SourceHash_SHA1;
            this.isComputedChecksum = true;
        }

        /// <summary>
        /// Use to create a document when checksum is suggested via external checksum pragma/directive
        /// </summary>
        public DebugSourceDocument(string location, Guid language, ImmutableArray<byte> checkSum, Guid checkSumAlgorithmId)
            : this(location, language)
        {
            this.checkSum = Task<ImmutableArray<byte>>.FromResult(checkSum);
            this.checkSumAlgorithmId = checkSumAlgorithmId;
        }

        public Guid DocumentType
        {
            get { return CorSymDocumentTypeText; }
        }

        public Guid Language
        {
            get { return language; }
        }

        public Guid LanguageVendor
        {
            get { return CorSymLanguageVendorMicrosoft; }
        }

        public string Location
        {
            get { return this.location; }
        }

        public Guid SourceHashKind
        {
            get { return checkSumAlgorithmId; }
        }

        public ImmutableArray<byte> SourceHash
        {
            get { return checkSum == null ? default(ImmutableArray<byte>) : checkSum.Result; }
        }

        /// <summary>
        /// returns true when checksum was computed base on an actual source stream
        /// as opposed to be suggested via a checksum directive/pragma
        /// </summary>
        internal bool IsComputedChecksum
        {
            get
            {
                return this.isComputedChecksum;
            }
        }
    }
}