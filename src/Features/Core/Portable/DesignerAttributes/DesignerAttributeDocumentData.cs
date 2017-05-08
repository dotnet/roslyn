// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.DesignerAttributes
{
    /// <summary>
    /// Marshalling type to pass designer attribute data to/from the OOP process.
    /// </summary>
    internal struct DesignerAttributeDocumentData : IEquatable<DesignerAttributeDocumentData>
    {
        public string FilePath;
        public string DesignerAttributeArgument;
        public bool ContainsErrors;
        public bool NotApplicable;

        public DesignerAttributeDocumentData(string filePath, string designerAttributeArgument, bool containsErrors, bool notApplicable)
        {
            FilePath = filePath;
            DesignerAttributeArgument = designerAttributeArgument;
            ContainsErrors = containsErrors;
            NotApplicable = notApplicable;
        }

        public override bool Equals(object obj)
            => Equals((DesignerAttributeDocumentData)obj);

        public bool Equals(DesignerAttributeDocumentData other)
        {
            return FilePath == other.FilePath &&
                   DesignerAttributeArgument == other.DesignerAttributeArgument &&
                   ContainsErrors == other.ContainsErrors &&
                   NotApplicable == other.NotApplicable;
        }

        // Currently no need for GetHashCode.  If we end up using this as a key in a dictionary,
        // feel free to add.
        public override int GetHashCode()
            => throw new NotImplementedException();
    }
}