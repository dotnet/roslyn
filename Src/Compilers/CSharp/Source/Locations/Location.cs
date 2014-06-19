using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a C# location in source code or metadata.
    /// </summary>
    [Serializable]
    public abstract class Location : CommonLocation, ISerializable
    {
        /// <summary>
        /// Serializes the location.
        /// </summary>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(SerializedLocation));
            SerializedLocation.GetObjectData(this, info, context);
        }

        public new virtual SyntaxTree SourceTree { get { return null; } }

        protected sealed override CommonSyntaxTree CommonSourceTree
        {
            get
            {
                return SourceTree;
            }
        }

        public new virtual ModuleSymbol MetadataModule { get { return null; } }

        protected sealed override IModuleSymbol CommonMetadataModule
        {
            get
            {
                return MetadataModule;
            }
        }
    }
}