// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    [System.Serializable]
    public struct ClassifiedToken
    {
        public ClassifiedToken(string text, string classification)
        {
            Text = text;
            Classification = classification;
        }

        public string Text { get; internal set; }
        public string Classification { get; internal set; }
    }
}
