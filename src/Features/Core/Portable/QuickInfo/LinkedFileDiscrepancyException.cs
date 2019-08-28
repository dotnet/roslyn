// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    // Used to aid the investigation of https://devdiv.visualstudio.com/DevDiv/_workitems?id=209299
    internal class LinkedFileDiscrepancyException : Exception
    {
        private readonly string _originalText;
        private readonly string _linkedText;

        public LinkedFileDiscrepancyException(Exception innerException, string originalText, string linkedText)
            : base("The contents of linked files do not match.", innerException)
        {
            _originalText = originalText;
            _linkedText = linkedText;
        }
    }
}
