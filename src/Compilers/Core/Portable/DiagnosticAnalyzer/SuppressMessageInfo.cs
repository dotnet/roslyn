// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal struct SuppressMessageInfo
    {
        public string Id;
        public string Scope;
        public string Target;
        public string MessageId;
        public AttributeData Attribute;
    }
}
