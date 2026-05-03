// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    // separate out text windowing implementation (keeps scanning & lexing functions from abusing details)
    internal class AbstractLexer : IDisposable
    {
        /// <summary>
        /// Not readonly.  This is a mutable struct that will be modified as we lex tokens.
        /// </summary>
        internal SlidingTextWindow TextWindow;

        private List<SyntaxDiagnosticInfo>? _errors;
        protected int LexemeStartPosition;

        protected AbstractLexer(SourceText text)
        {
            this.TextWindow = new SlidingTextWindow(text);
        }

        public virtual void Dispose()
        {
            this.TextWindow.Free();
        }

        protected void Start()
        {
            LexemeStartPosition = this.TextWindow.Position;
            _errors = null;
        }

        protected bool HasErrors
        {
            get { return _errors != null; }
        }

        protected SyntaxDiagnosticInfo[]? GetErrors()
        {
            return _errors?.ToArray();
        }

        protected void AddError(int position, int width, ErrorCode code)
        {
            this.AddError(this.MakeError(position, width, code));
        }

        protected void AddError(int position, int width, ErrorCode code, params object[] args)
        {
            this.AddError(this.MakeError(position, width, code, args));
        }

        protected void AddError(ErrorCode code)
        {
            this.AddError(MakeError(code));
        }

        protected void AddError(ErrorCode code, params object[] args)
        {
            this.AddError(MakeError(code, args));
        }

        protected void AddError(XmlParseErrorCode code)
        {
            this.AddError(MakeError(code));
        }

        protected void AddError(XmlParseErrorCode code, params object[] args)
        {
            this.AddError(MakeError(code, args));
        }

        protected void AddError(SyntaxDiagnosticInfo? error)
        {
            if (error != null)
            {
                if (_errors == null)
                {
                    _errors = new List<SyntaxDiagnosticInfo>(8);
                }

                _errors.Add(error);
            }
        }

        protected SyntaxDiagnosticInfo MakeError(int position, int width, ErrorCode code)
        {
            int offset = GetLexemeOffsetFromPosition(position);
            return new SyntaxDiagnosticInfo(offset, width, code);
        }

        protected SyntaxDiagnosticInfo MakeError(int position, int width, ErrorCode code, params object[] args)
        {
            int offset = GetLexemeOffsetFromPosition(position);
            return new SyntaxDiagnosticInfo(offset, width, code, args);
        }

        private int GetLexemeOffsetFromPosition(int position)
        {
            return position >= LexemeStartPosition ? position - LexemeStartPosition : position;
        }

        protected string GetNonInternedLexemeText()
            => TextWindow.GetText(LexemeStartPosition, intern: false);

        protected string GetInternedLexemeText()
            => TextWindow.GetText(LexemeStartPosition, intern: true);

        protected int CurrentLexemeWidth
            => this.TextWindow.Position - LexemeStartPosition;

        protected static SyntaxDiagnosticInfo MakeError(ErrorCode code)
        {
            return new SyntaxDiagnosticInfo(code);
        }

        protected static SyntaxDiagnosticInfo MakeError(ErrorCode code, params object[] args)
        {
            return new SyntaxDiagnosticInfo(code, args);
        }

        protected static XmlSyntaxDiagnosticInfo MakeError(XmlParseErrorCode code)
        {
            return new XmlSyntaxDiagnosticInfo(0, 0, code);
        }

        protected static XmlSyntaxDiagnosticInfo MakeError(XmlParseErrorCode code, params object[] args)
        {
            return new XmlSyntaxDiagnosticInfo(0, 0, code, args);
        }
    }
}
