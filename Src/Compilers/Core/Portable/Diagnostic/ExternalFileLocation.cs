// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A program location in source code.
    /// </summary>
    internal sealed class ExternalFileLocation : Location, IEquatable<ExternalFileLocation>
    {
        private readonly TextSpan sourceSpan;
        private readonly FileLinePositionSpan lineSpan;

        internal ExternalFileLocation(string filePath, TextSpan sourceSpan, LinePositionSpan lineSpan)
        {
            this.sourceSpan = sourceSpan;
            this.lineSpan = new FileLinePositionSpan(filePath, lineSpan);
        }

        public override TextSpan SourceSpan
        {
            get
            {
                return this.sourceSpan;
            }
        }

        public override FileLinePositionSpan GetLineSpan()
        {
            return this.lineSpan;
        }

        public override FileLinePositionSpan GetMappedLineSpan()
        {
            return this.lineSpan;
        }

        public override LocationKind Kind
        {
            get
            {
                return LocationKind.ExternalFile;
            }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ExternalFileLocation);
        }

        public bool Equals(ExternalFileLocation obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return obj != null 
                && this.sourceSpan == obj.sourceSpan 
                && this.lineSpan.Equals(obj.lineSpan);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.lineSpan.GetHashCode(), this.sourceSpan.GetHashCode());
        }
    }
}
