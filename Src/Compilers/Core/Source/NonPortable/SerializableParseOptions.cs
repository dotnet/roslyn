// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents parse options common to C# and VB.
    /// </summary>
    [Serializable]
    public abstract class SerializableParseOptions : ISerializable
    {
        internal SerializableParseOptions()
        {
        }

        public ParseOptions Options { get { return CommonOptions; } }
        protected abstract ParseOptions CommonOptions { get; }

        public abstract void GetObjectData(SerializationInfo info, StreamingContext context);

        protected static void CommonGetObjectData(ParseOptions options, SerializationInfo info, StreamingContext context)
        {
            //public readonly SourceCodeKind Kind;
            info.AddValue("Kind", options.Kind, typeof(SourceCodeKind));

            //public readonly DocumentationMode DocumentationMode;
            info.AddValue("DocumentationMode", options.DocumentationMode, typeof(DocumentationMode));
        }
    }
}
