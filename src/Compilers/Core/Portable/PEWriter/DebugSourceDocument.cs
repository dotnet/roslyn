// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DebugSourceDocument
    {
        internal static readonly Guid CorSymLanguageTypeCSharp = new Guid("{3f5162f8-07c6-11d3-9053-00c04fa302a1}");
        internal static readonly Guid CorSymLanguageTypeBasic = new Guid("{3a12d0b8-c26c-11d0-b442-00a0244a1dd2}");
        private static readonly Guid s_corSymLanguageVendorMicrosoft = new Guid("{994b45c4-e6e9-11d2-903f-00c04fa302a1}");
        private static readonly Guid s_corSymDocumentTypeText = new Guid("{5a869d0b-6611-11d3-bd2a-0000f80849bd}");

        private readonly string _location;
        private readonly Guid _language;
        private readonly bool _isComputedChecksum;
        private readonly Task<DebugSourceInfo> _sourceInfo;

        public DebugSourceDocument(string location, Guid language)
        {
            Debug.Assert(location != null);

            _location = location; // If it's a path, it should be normalized.
            _language = language;
        }

        /// <summary>
        /// Use to create a document when checksum is computed based on actual source stream.
        /// </summary>
        public DebugSourceDocument(string location, Guid language, Func<DebugSourceInfo> sourceInfo)
            : this(location, language)
        {
            _sourceInfo = Task.Run(sourceInfo);
            _isComputedChecksum = true;
        }

        /// <summary>
        /// Use to create a document when checksum is suggested via external checksum pragma/directive
        /// </summary>
        public DebugSourceDocument(string location, Guid language, ImmutableArray<byte> checksum, Guid algorithm)
            : this(location, language)
        {
            _sourceInfo = Task.FromResult(new DebugSourceInfo(checksum, algorithm));
        }

        internal static bool IsSupportedAlgorithm(SourceHashAlgorithm algorithm)
        {
            // Dev12 debugger supports MD5, SHA1.
            // Dev14 debugger supports MD5, SHA1, SHA256.
            // MD5 is obsolete.

            switch (algorithm)
            {
                case SourceHashAlgorithm.Sha1:
                case SourceHashAlgorithm.Sha256:
                    return true;
                default:
                    return false;
            }
        }

        internal static Guid GetAlgorithmGuid(SourceHashAlgorithm algorithm)
        {
            Debug.Assert(IsSupportedAlgorithm(algorithm));

            // Dev12 debugger supports MD5, SHA1.
            // Dev14 debugger supports MD5, SHA1, SHA256.
            // MD5 is obsolete.

            unchecked
            {
                switch (algorithm)
                {
                    case SourceHashAlgorithm.Sha1:
                        return new Guid((int)0xff1816ec, (short)0xaa5e, 0x4d10, 0x87, 0xf7, 0x6f, 0x49, 0x63, 0x83, 0x34, 0x60);

                    case SourceHashAlgorithm.Sha256:
                        return new Guid((int)0x8829d00f, 0x11b8, 0x4213, 0x87, 0x8b, 0x77, 0x0e, 0x85, 0x97, 0xac, 0x16);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(algorithm);
                }
            }
        }

        public Guid DocumentType
        {
            get { return s_corSymDocumentTypeText; }
        }

        public Guid Language
        {
            get { return _language; }
        }

        public Guid LanguageVendor
        {
            get { return s_corSymLanguageVendorMicrosoft; }
        }

        public string Location
        {
            get { return _location; }
        }

        public DebugSourceInfo GetSourceInfo()
        {
            return _sourceInfo?.Result ?? default(DebugSourceInfo);
        }

        /// <summary>
        /// returns true when checksum was computed base on an actual source stream
        /// as opposed to be suggested via a checksum directive/pragma
        /// </summary>
        internal bool IsComputedChecksum
        {
            get
            {
                return _isComputedChecksum;
            }
        }
    }
}
