// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.MethodXml
{
    internal abstract partial class AbstractMethodXmlBuilder
    {
        private struct AttributeInfo
        {
            public static readonly AttributeInfo Empty = new AttributeInfo();

            public readonly string Name;
            public readonly string Value;

            public AttributeInfo(string name, string value)
            {
                this.Name = name;
                this.Value = value;
            }

            public bool IsEmpty
            {
                get
                {
                    return this is { Name: null, Value: null };
                }
            }
        }
    }
}
