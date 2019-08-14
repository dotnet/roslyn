// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal class DebuggerDisplayInfo
    {
        private readonly DkmClrType m_targetType;

        public DebuggerDisplayInfo(DkmClrType targetType)
        {
            m_targetType = targetType;
        }

        public bool HasValues { get { return (Name != null || Value != null || TypeName != null); } }

        public DebuggerDisplayItemInfo Name { get; private set; }

        public DebuggerDisplayItemInfo Value { get; private set; }

        public DebuggerDisplayItemInfo TypeName { get; private set; }

        internal void ApplyFavorities(DkmClrObjectFavoritesInfo favoritesInfo)
        {
            if (favoritesInfo.DisplayString != null)
            {
                Value = new DebuggerDisplayItemInfo(favoritesInfo.DisplayString, m_targetType);
            }
        }

        internal void ApplyEvalAttribute(DkmClrDebuggerDisplayAttribute attribute, DkmClrType attributeTarget)
        {
            if (attribute.Name != null)
            {
                Name = new DebuggerDisplayItemInfo(attribute.Name, attributeTarget);
            }

            if (Value == null && attribute.Value != null) // Favorites display string takes priority for Value
            {
                Value = new DebuggerDisplayItemInfo(attribute.Value, attributeTarget);
            }

            if (attribute.TypeName != null)
            {
                TypeName = new DebuggerDisplayItemInfo(attribute.TypeName, attributeTarget);
            }
        }
    }

    internal class DebuggerDisplayItemInfo
    {
        public readonly string Value;
        public readonly DkmClrType TargetType;

        public DebuggerDisplayItemInfo(string value, DkmClrType targetType)
        {
            Value = value;
            TargetType = targetType;
        }
    }
}
