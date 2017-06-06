// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundNode : IBoundNodeWithIOperationChildren
    {
        ImmutableArray<BoundNode> IBoundNodeWithIOperationChildren.Children => this.Children;

        /// <summary>
        /// Override this property to return the child bound nodes if the IOperation API corresponding to this bound node is not yet designed or implemented.
        /// </summary>
        /// <remarks>Note that any of the child bound nodes may be null.</remarks>
        protected virtual ImmutableArray<BoundNode> Children => ImmutableArray<BoundNode>.Empty;
    }

    internal partial class BoundBadStatement
    {
        private static readonly ConditionalWeakTable<BoundLocalDeclaration, object> s_variablesMappings =
            new ConditionalWeakTable<BoundLocalDeclaration, object>();

        ImmutableArray<IVariableDeclaration> IVariableDeclarationStatement.Declarations
        {
            get
            {
                return (ImmutableArray<IVariableDeclaration>)s_variablesMappings.GetValue(this,
                    declaration => ImmutableArray.Create<IVariableDeclaration>(new VariableDeclaration(declaration.LocalSymbol, declaration.InitializerOpt, declaration.Syntax)));
            }
        }

        protected override OperationKind StatementKind => OperationKind.VariableDeclarationStatement;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclarationStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclarationStatement(this, argument);
        }
    }

    internal partial class BoundMultipleLocalDeclarations : IVariableDeclarationStatement
    {
        private static readonly ConditionalWeakTable<BoundMultipleLocalDeclarations, object> s_variablesMappings =
            new ConditionalWeakTable<BoundMultipleLocalDeclarations, object>();

        ImmutableArray<IVariableDeclaration> IVariableDeclarationStatement.Declarations
        {
            get
            {
                return (ImmutableArray<IVariableDeclaration>)s_variablesMappings.GetValue(this,
                    multipleDeclarations =>
                        multipleDeclarations.LocalDeclarations.SelectAsArray(declaration =>
                            (IVariableDeclaration)new VariableDeclaration(declaration.LocalSymbol, declaration.InitializerOpt, declaration.Syntax)));
            }
        }

        protected override OperationKind StatementKind => OperationKind.VariableDeclarationStatement;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclarationStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclarationStatement(this, argument);
        }
    }

    internal partial class BoundLabelStatement : ILabelStatement
    {
        // These represent synthesized labels, and do not have an attached statement.
        IOperation ILabelStatement.LabeledStatement => null;

        ILabelSymbol ILabelStatement.Label => this.Label;

        protected override OperationKind StatementKind => OperationKind.LabelStatement;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLabelStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLabelStatement(this, argument);
        }
    }

    internal partial class BoundLabeledStatement : ILabelStatement
    {
        IOperation ILabelStatement.LabeledStatement => this.Body;

        ILabelSymbol ILabelStatement.Label => this.Label;

        protected override OperationKind StatementKind => OperationKind.LabelStatement;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLabelStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLabelStatement(this, argument);
        }
    }

    internal partial class BoundExpressionStatement : IExpressionStatement
    {
        IOperation IExpressionStatement.Expression => this.Expression;

        protected override OperationKind StatementKind => OperationKind.ExpressionStatement;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitExpressionStatement(this, argument);
        }
    }

    internal partial class BoundSwitchSection
    {
        protected override OperationKind StatementKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundStatementList
    {
        protected override OperationKind StatementKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundConditionalGoto
    {
        protected override OperationKind StatementKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundSequencePoint
    {
        protected override OperationKind StatementKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundSequencePointWithSpan
    {
        protected override OperationKind StatementKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    internal partial class BoundStateMachineScope
    {
        protected override OperationKind StatementKind => OperationKind.None;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    partial class BoundLocalFunctionStatement
    {
        protected override OperationKind StatementKind => OperationKind.None;

        protected override ImmutableArray<IOperation> Children => ImmutableArray.Create<IOperation>(this.Body);

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNoneOperation(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNoneOperation(this, argument);
        }
    }

    partial class BoundPatternSwitchStatement
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.SwitchSections).Insert(0, this.Expression);
    }

    partial class BoundPatternSwitchSection
    {
        protected override ImmutableArray<BoundNode> Children => StaticCast<BoundNode>.From(this.SwitchLabels).AddRange(this.Statements);
    }

    partial class BoundPatternSwitchLabel
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Pattern, this.Guard);
    }
}
