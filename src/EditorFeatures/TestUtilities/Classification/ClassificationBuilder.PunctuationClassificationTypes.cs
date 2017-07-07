// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public partial class ClassificationBuilder
    {
        public sealed class PunctuationClassificationTypes
        {
            [DebuggerStepThrough]
            private Tuple<string, string> New(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.Punctuation);
            }

            public Tuple<string, string> OpenCurly
            {
                [DebuggerStepThrough]
                get
                {
                    return New("{");
                }
            }

            public Tuple<string, string> CloseCurly
            {
                [DebuggerStepThrough]
                get
                {
                    return New("}");
                }
            }

            public Tuple<string, string> OpenParen
            {
                [DebuggerStepThrough]
                get
                {
                    return New("(");
                }
            }

            public Tuple<string, string> CloseParen
            {
                [DebuggerStepThrough]
                get
                {
                    return New(")");
                }
            }

            public Tuple<string, string> OpenAngle
            {
                [DebuggerStepThrough]
                get
                {
                    return New("<");
                }
            }

            public Tuple<string, string> CloseAngle
            {
                [DebuggerStepThrough]
                get
                {
                    return New(">");
                }
            }

            public Tuple<string, string> OpenBracket
            {
                [DebuggerStepThrough]
                get
                {
                    return New("[");
                }
            }

            public Tuple<string, string> CloseBracket
            {
                [DebuggerStepThrough]
                get
                {
                    return New("]");
                }
            }

            public Tuple<string, string> Comma
            {
                [DebuggerStepThrough]
                get
                {
                    return New(",");
                }
            }

            public Tuple<string, string> Semicolon
            {
                [DebuggerStepThrough]
                get
                {
                    return New(";");
                }
            }

            public Tuple<string, string> Colon
            {
                [DebuggerStepThrough]
                get
                {
                    return New(":");
                }
            }

            [DebuggerStepThrough]
            public Tuple<string, string> Text(string text)
            {
                return New(text);
            }
        }
    }
}
