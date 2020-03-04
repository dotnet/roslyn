// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
