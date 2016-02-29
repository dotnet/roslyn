// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundExpression
    {
        /// <summary>
        /// Returns a serializable object that is used for displaying this expression in a diagnostic message.
        /// </summary>
        public virtual object Display
        {
            get { return this.Type; }
        }
    }

    internal sealed partial class BoundArgListOperator
    {
        public override object Display
        {
            get { return "__arglist"; }
        }
    }

    internal sealed partial class BoundLiteral
    {
        public override object Display
        {
            get { return ConstantValue.IsNull ? MessageID.IDS_NULL.Localize() : base.Display; }
        }
    }

    internal sealed partial class BoundLambda
    {
        public override object Display
        {
            get { return this.MessageID.Localize(); }
        }
    }

    internal sealed partial class UnboundLambda
    {
        public override object Display
        {
            get { return this.MessageID.Localize(); }
        }
    }

    internal sealed partial class BoundMethodGroup
    {
        public override object Display
        {
            get { return MessageID.IDS_MethodGroup.Localize(); }
        }
    }

    internal sealed partial class BoundPropertyGroup
    {
        public override object Display
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }
}
