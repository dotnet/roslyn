﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class SyntaxDiagnosticInfo : DiagnosticInfo
    {
        static SyntaxDiagnosticInfo()
        {
            ObjectBinder.RegisterTypeReader(typeof(SyntaxDiagnosticInfo), r => new SyntaxDiagnosticInfo(r));
        }

        internal readonly int Offset;
        internal readonly int Width;

        internal SyntaxDiagnosticInfo(int offset, int width, ErrorCode code, params object[] args)
            : base(CSharp.MessageProvider.Instance, (int)code, args)
        {
            Debug.Assert(width >= 0);
            this.Offset = offset;
            this.Width = width;
        }

        internal SyntaxDiagnosticInfo(int offset, int width, ErrorCode code)
            : this(offset, width, code, Array.Empty<object>())
        {
        }

        internal SyntaxDiagnosticInfo(ErrorCode code, params object[] args)
            : this(0, 0, code, args)
        {
        }

        internal SyntaxDiagnosticInfo(ErrorCode code)
            : this(0, 0, code)
        {
        }

        public SyntaxDiagnosticInfo WithOffset(int offset)
        {
            return new SyntaxDiagnosticInfo(offset, this.Width, (ErrorCode)this.Code, this.Arguments);
        }

        #region Serialization

        protected override void WriteTo(ObjectWriter writer)
        {
            base.WriteTo(writer);
            writer.WriteInt32(this.Offset);
            writer.WriteInt32(this.Width);
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
