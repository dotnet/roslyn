// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
