// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.DesignerAttributes
{
    internal struct DesignerAttributeResult : IEquatable<DesignerAttributeResult>
    {
        public string FilePath;
        public string DesignerAttributeArgument;
        public bool ContainsErrors;
        public bool NotApplicable;

        public DesignerAttributeResult(string filePath, string designerAttributeArgument, bool containsErrors, bool notApplicable)
        {
            FilePath = filePath;
            DesignerAttributeArgument = designerAttributeArgument;
            ContainsErrors = containsErrors;
            NotApplicable = notApplicable;
        }

        public override bool Equals(object obj)
            => Equals((DesignerAttributeResult)obj);

        public bool Equals(DesignerAttributeResult other)
        {
            return FilePath == other.FilePath &&
                   DesignerAttributeArgument == other.DesignerAttributeArgument &&
                   ContainsErrors == other.ContainsErrors &&
                   NotApplicable == other.NotApplicable;
        }

        public override int GetHashCode()
            => throw new NotImplementedException();
    }
}