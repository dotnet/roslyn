// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal sealed class VariableDeclaration : IVariable
    {
        public VariableDeclaration(ILocalSymbol variable, IExpression initialValue, SyntaxNode syntax)
        {
            Variable = variable;
            InitialValue = initialValue;
            Syntax = syntax;
        }

        public ILocalSymbol Variable { get; }

        public IExpression InitialValue { get; }

        public bool IsInvalid => Variable == null || (InitialValue != null && InitialValue.IsInvalid);

        public OperationKind Kind => OperationKind.VariableDeclaration;

        public SyntaxNode Syntax { get; }

        public void Accept(IOperationVisitor visitor)
        {
            visitor.VisitVariable(this);
        }

        public TResult Accept<TArgument, TResult>(IOperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariable(this, argument);
        }
    }
}