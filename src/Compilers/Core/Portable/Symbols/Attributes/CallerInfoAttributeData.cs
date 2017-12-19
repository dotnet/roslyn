// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal enum CallerInfoAttributeKind
    {
        None,
        CallerLineNumber,
        CallerFilePath,
        CallerMemberName,
        CallerArgumentExpression,
    }

    internal class CallerInfoAttributeData
    {
        public readonly CallerInfoAttributeKind Kind;
        public int ArgumnetExpressionParameterIndex
        {
            get
            {
                Debug.Assert(this.Kind == CallerInfoAttributeKind.CallerArgumentExpression);
                return ((CallerArgumentExpressionData)this).ParameterIndex;
            }
        }

        public static readonly CallerInfoAttributeData CallerLineNumber = new CallerInfoAttributeData(CallerInfoAttributeKind.CallerLineNumber);
        public static readonly CallerInfoAttributeData CallerFilePath = new CallerInfoAttributeData(CallerInfoAttributeKind.CallerFilePath);
        public static readonly CallerInfoAttributeData CallerMemberName = new CallerInfoAttributeData(CallerInfoAttributeKind.CallerMemberName);
        public static CallerInfoAttributeData CallerArgumentExpression(int parameterIndex) => new CallerArgumentExpressionData(parameterIndex);

        private CallerInfoAttributeData(CallerInfoAttributeKind kind)
        {
            this.Kind = kind;
        }

        private sealed class CallerArgumentExpressionData : CallerInfoAttributeData
        {
            public readonly int ParameterIndex;

            public CallerArgumentExpressionData(int parameterIndex)
                : base(CallerInfoAttributeKind.CallerArgumentExpression)
            {
                ParameterIndex = parameterIndex;
            }
        }
    }
}
