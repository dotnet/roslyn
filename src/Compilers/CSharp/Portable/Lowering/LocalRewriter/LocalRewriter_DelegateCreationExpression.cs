// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            if (node.Argument.HasDynamicType())
            {
                var loweredArgument = VisitExpression(node.Argument);

                // Creates a delegate whose instance is the delegate that is returned by the call-site and the method is Invoke.
                var loweredReceiver = _dynamicFactory.MakeDynamicConversion(loweredArgument, isExplicit: false, isArrayIndex: false, isChecked: false, resultType: node.Type).ToExpression();

                return new BoundDelegateCreationExpression(node.Syntax, loweredReceiver, methodOpt: null, isExtensionMethod: false, type: node.Type);
            }

            if (node.Argument.Kind == BoundKind.MethodGroup)
            {
                var mg = (BoundMethodGroup)node.Argument;
                var method = node.MethodOpt;
                var oldSyntax = _factory.Syntax;
                _factory.Syntax = (mg.ReceiverOpt ?? mg).Syntax;
                var receiver = (!method.RequiresInstanceReceiver && !node.IsExtensionMethod) ? _factory.Type(method.ContainingType) : VisitExpression(mg.ReceiverOpt);
                _factory.Syntax = oldSyntax;
                return node.Update(receiver, method, node.IsExtensionMethod, node.Type);
            }

            return base.VisitDelegateCreationExpression(node);
        }
    }
}
