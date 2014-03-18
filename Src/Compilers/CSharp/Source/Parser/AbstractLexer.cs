// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    // separate out text windowing implementation (keeps scanning & lexing functions from abusing details)
    internal class AbstractLexer : IDisposable
    {
        internal readonly SlidingTextWindow TextWindow;
        private List<SyntaxDiagnosticInfo> errors;

        protected AbstractLexer(SourceText text)
        {
            this.TextWindow = new SlidingTextWindow(text);
        }

        public virtual void Dispose()
        {
            this.TextWindow.Dispose();
        }

        protected void Start()
        {
            TextWindow.Start();
            this.errors = null;
        }

        protected bool HasErrors
        {
            get { return this.errors != null; }
        }

        protected SyntaxDiagnosticInfo[] Errors
        {
            get { return this.errors == null ? null : this.errors.ToArray(); }
        }

        protected void AddError(int position, int width, ErrorCode code, params object[] args)
        {
            this.AddError(this.MakeError(position, width, code, args));
        }

        protected void AddError(int position, int width, XmlParseErrorCode code, params object[] args)
        {
            this.AddError(this.MakeError(position, width, code, args));
        }

        protected void AddError(ErrorCode code, params object[] args)
        {
            this.AddError(MakeError(code, args));
        }

        protected void AddError(XmlParseErrorCode code, params object[] args)
        {
            this.AddError(MakeError(code, args));
        }

        protected void AddError(SyntaxDiagnosticInfo error)
        {
            if (error != null)
            {
                if (this.errors == null)
                {
                    this.errors = new List<SyntaxDiagnosticInfo>(8);
                }

                this.errors.Add(error);
            }
        }

        protected SyntaxDiagnosticInfo MakeError(int position, int width, ErrorCode code, params object[] args)
        {
            int offset = position >= TextWindow.LexemeStartPosition ? position - TextWindow.LexemeStartPosition : position;
            return new SyntaxDiagnosticInfo(offset, width, code, args);
        }

        protected XmlSyntaxDiagnosticInfo MakeError(int position, int width, XmlParseErrorCode code, params object[] args)
        {
            int offset = position >= TextWindow.LexemeStartPosition ? position - TextWindow.LexemeStartPosition : position;
            return new XmlSyntaxDiagnosticInfo(offset, width, code, args);
        }

        protected static SyntaxDiagnosticInfo MakeError(ErrorCode code, params object[] args)
        {
            return new SyntaxDiagnosticInfo(code, args);
        }

        protected static XmlSyntaxDiagnosticInfo MakeError(XmlParseErrorCode code, params object[] args)
        {
            return new XmlSyntaxDiagnosticInfo(0, 0, code, args);
        }
    }
}
