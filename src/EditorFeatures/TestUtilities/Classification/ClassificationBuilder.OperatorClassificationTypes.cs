// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public partial class ClassificationBuilder
    {
        public sealed class OperatorClassificationTypes
        {
            [DebuggerStepThrough]
            private Tuple<string, string> New(string value)
            {
                return Tuple.Create(value, ClassificationTypeNames.Operator);
            }

            public Tuple<string, string> Dot
            {
                get
                {
                    return New(".");
                }
            }

            public Tuple<string, string> DoubleAmpersand
            {
                [DebuggerStepThrough]
                get
                {
                    return New("&&");
                }
            }

            public Tuple<string, string> DoubleEquals
            {
                [DebuggerStepThrough]
                get
                {
                    return New("==");
                }
            }

            public Tuple<string, string> DoublePlus
            {
                [DebuggerStepThrough]
                get
                {
                    return New("++");
                }
            }

            public Tuple<string, string> DoublePipe
            {
                [DebuggerStepThrough]
                get
                {
                    return New("||");
                }
            }

            public Tuple<string, string> Exclamation
            {
                [DebuggerStepThrough]
                get
                {
                    return New("!");
                }
            }

            public Tuple<string, string> ExclamationEquals
            {
                [DebuggerStepThrough]
                get
                {
                    return New("!=");
                }
            }

            public Tuple<string, string> GreaterThan
            {
                [DebuggerStepThrough]
                get
                {
                    return New(">");
                }
            }

            public Tuple<string, string> LessThan
            {
                [DebuggerStepThrough]
                get
                {
                    return New("<");
                }
            }

            public Tuple<string, string> LessThanGreaterThan
            {
                [DebuggerStepThrough]
                get
                {
                    return New("<>");
                }
            }

            public new Tuple<string, string> Equals
            {
                [DebuggerStepThrough]
                get
                {
                    return New("=");
                }
            }

            public Tuple<string, string> QuestionMark
            {
                [DebuggerStepThrough]
                get
                {
                    return New("?");
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

            public Tuple<string, string> Star
            {
                [DebuggerStepThrough]
                get
                {
                    return New("*");
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
