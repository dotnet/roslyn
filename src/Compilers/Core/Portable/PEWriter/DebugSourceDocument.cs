// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly Task<DebugSourceInfo>? _sourceInfo;

        public DebugSourceDocument(string location, Guid language)
        {
            RoslynDebug.Assert(location != null);

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
