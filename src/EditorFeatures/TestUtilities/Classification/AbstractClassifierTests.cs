// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
{
    public abstract class AbstractClassifierTests
    {
        protected readonly ClassificationBuilder ClassificationBuilder;

        protected AbstractClassifierTests()
        {
            this.ClassificationBuilder = new ClassificationBuilder();
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Struct(string value)
        {
            return ClassificationBuilder.Struct(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Enum(string value)
        {
            return ClassificationBuilder.Enum(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Interface(string value)
        {
            return ClassificationBuilder.Interface(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Class(string value)
        {
            return ClassificationBuilder.Class(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Delegate(string value)
        {
            return ClassificationBuilder.Delegate(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> TypeParameter(string value)
        {
            return ClassificationBuilder.TypeParameter(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Field(string value)
        {
            return ClassificationBuilder.Field(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> EnumField(string value)
        {
            return ClassificationBuilder.EnumField(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Constant(string value)
        {
            return ClassificationBuilder.Constant(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Local(string value)
        {
            return ClassificationBuilder.Local(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Parameter(string value)
        {
            return ClassificationBuilder.Parameter(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Method(string value)
        {
            return ClassificationBuilder.Method(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> ExtensionMethod(string value)
        {
            return ClassificationBuilder.ExtensionMethod(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Property(string value)
        {
            return ClassificationBuilder.Property(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Event(string value)
        {
            return ClassificationBuilder.Event(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> String(string value)
        {
            return ClassificationBuilder.String(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Verbatim(string value)
        {
            return ClassificationBuilder.Verbatim(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Keyword(string value)
        {
            return ClassificationBuilder.Keyword(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> PPKeyword(string value)
        {
            return ClassificationBuilder.PPKeyword(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> PPText(string value)
        {
            return ClassificationBuilder.PPText(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Identifier(string value)
        {
            return ClassificationBuilder.Identifier(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Inactive(string value)
        {
            return ClassificationBuilder.Inactive(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Comment(string value)
        {
            return ClassificationBuilder.Comment(value);
        }

        [DebuggerStepThrough]
        protected Tuple<string, string> Number(string value)
        {
            return ClassificationBuilder.Number(value);
        }

        protected ClassificationBuilder.PunctuationClassificationTypes Punctuation
        {
            get { return ClassificationBuilder.Punctuation; }
        }

        protected ClassificationBuilder.OperatorClassificationTypes Operators
        {
            get { return ClassificationBuilder.Operator; }
        }

        protected ClassificationBuilder.XmlDocClassificationTypes XmlDoc
        {
            get { return ClassificationBuilder.XmlDoc; }
        }
    }
}
