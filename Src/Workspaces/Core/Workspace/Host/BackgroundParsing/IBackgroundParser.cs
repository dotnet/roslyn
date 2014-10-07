// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IBackgroundParser
    {
        /// <summary>
        /// True if the background parser is in the started state. This does not mean any parses are
        /// actually in progress.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Put the background parser in the started state. When started calls to Parse will cause
        /// parses to run.
        /// </summary>
        void Start();

        /// <summary>
        /// Put the background parser in the stopped state. When stopped calls to Parse will not
        /// cause parses to run.
        /// </summary>
        void Stop();

        /// <summary>
        /// Parse the document in the background.
        /// </summary>
        void Parse(Document document);

        /// <summary>
        /// Cancel any queued parse work for the specified document.
        /// </summary>
        void CancelParse(DocumentId documentId);

        /// <summary>
        /// Cancel all running and queued parse work.
        /// </summary>
        void CancelAllParses();
    }
}