// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Classification
{
    internal struct ClassifiedText
    {
        public string ClassificationType { get; }
        public string Text { get; }

        public ClassifiedText(string classificationType, string text)
        {
            ClassificationType = classificationType;
            Text = text;
        }
    }
}
