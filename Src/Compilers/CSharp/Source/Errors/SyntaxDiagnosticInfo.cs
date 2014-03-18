// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Serializable]
    internal class SyntaxDiagnosticInfo : DiagnosticInfo
    {
        internal readonly int Offset;
        internal readonly int Width;

        internal SyntaxDiagnosticInfo(int offset, int width, ErrorCode code, params object[] args)
            : base(CSharp.MessageProvider.Instance, (int)code, args)
        {
            Debug.Assert(width >= 0);
            this.Offset = offset;
            this.Width = width;
        }

        internal SyntaxDiagnosticInfo(ErrorCode code, params object[] args)
            : this(0, 0, code, args)
        {
        }

        #region Serialization

        protected SyntaxDiagnosticInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            int offset = info.GetInt32("offset");
            int width = info.GetInt32("width");
            // offset can be negative as we may want to display the squiggle at the end of the previous token
            if (width < 0)
            {
                throw new SerializationException();
            }
            Offset = offset;
            Width = width;
        }

        protected override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("offset", Offset);
            info.AddValue("width", Width);
        }

        protected override void WriteTo(ObjectWriter writer)
        {
            base.WriteTo(writer);
            writer.WriteInt32(this.Offset);
            writer.WriteInt32(this.Width);
        }

        protected override Func<ObjectReader, object> GetReader()
        {
            return (r) => new SyntaxDiagnosticInfo(r);
        }

        protected SyntaxDiagnosticInfo(ObjectReader reader)
            : base(reader)
        {
            this.Offset = reader.ReadInt32();
            this.Width = reader.ReadInt32();
        }

        #endregion
    }
}