﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// State machine interface method implementation.
    /// </summary>
    internal abstract class SynthesizedStateMachineMethod : SynthesizedImplementationMethod, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly bool _hasMethodBodyDependency;

        protected SynthesizedStateMachineMethod(
            string name,
            MethodSymbol interfaceMethod,
            StateMachineTypeSymbol stateMachineType,
            PropertySymbol associatedProperty,
            bool generateDebugInfo,
            bool hasMethodBodyDependency)
            : base(interfaceMethod, stateMachineType, name, generateDebugInfo, associatedProperty)
        {
            _hasMethodBodyDependency = hasMethodBodyDependency;
        }

        public StateMachineTypeSymbol StateMachineType
        {
            get { return (StateMachineTypeSymbol)ContainingSymbol; }
        }

        public bool HasMethodBodyDependency
        {
            get { return _hasMethodBodyDependency; }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return StateMachineType.KickoffMethod; }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return this.StateMachineType.KickoffMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
        }
    }

    /// <summary>
    /// Represents a state machine MoveNext method.
    /// Handles special behavior around inheriting some attributes from the original async/iterator method.
    /// </summary>
    internal sealed class SynthesizedStateMachineMoveNextMethod : SynthesizedStateMachineMethod
    {
        private ImmutableArray<CSharpAttributeData> _attributes;

        public SynthesizedStateMachineMoveNextMethod(MethodSymbol interfaceMethod, StateMachineTypeSymbol stateMachineType)
            : base(WellKnownMemberNames.MoveNextMethodName, interfaceMethod, stateMachineType, null, generateDebugInfo: true, hasMethodBodyDependency: true)
        {
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_attributes.IsDefault)
            {
                Debug.Assert(base.GetAttributes().Length == 0);

                ArrayBuilder<CSharpAttributeData> builder = null;

                // Inherit some attributes from the kickoff method
                var kickoffMethod = StateMachineType.KickoffMethod;
                foreach (var attribute in kickoffMethod.GetAttributes())
                {
                    if (attribute.IsTargetAttribute(kickoffMethod, AttributeDescription.DebuggerHiddenAttribute) ||
                        attribute.IsTargetAttribute(kickoffMethod, AttributeDescription.DebuggerNonUserCodeAttribute) ||
                        attribute.IsTargetAttribute(kickoffMethod, AttributeDescription.DebuggerStepperBoundaryAttribute) ||
                        attribute.IsTargetAttribute(kickoffMethod, AttributeDescription.DebuggerStepThroughAttribute))
                    {
                        if (builder == null)
                        {
                            builder = ArrayBuilder<CSharpAttributeData>.GetInstance(4); // only 4 different attributes are inherited at the moment
                        }

                        builder.Add(attribute);
                    }
                }

                ImmutableInterlocked.InterlockedCompareExchange(ref _attributes,
                                                                builder == null ? ImmutableArray<CSharpAttributeData>.Empty : builder.ToImmutableAndFree(),
                                                                default(ImmutableArray<CSharpAttributeData>));
            }

            return _attributes;
        }
    }

    /// <summary>
    /// Represents a state machine method other than a MoveNext method.
    /// All such methods are considered debugger hidden. 
    /// </summary>
    internal sealed class SynthesizedStateMachineDebuggerHiddenMethod : SynthesizedStateMachineMethod
    {
        public SynthesizedStateMachineDebuggerHiddenMethod(
            string name,
            MethodSymbol interfaceMethod,
            StateMachineTypeSymbol stateMachineType,
            PropertySymbol associatedProperty,
            bool hasMethodBodyDependency)
            : base(name, interfaceMethod, stateMachineType, associatedProperty, generateDebugInfo: false, hasMethodBodyDependency: hasMethodBodyDependency)
        {
        }

        internal sealed override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor));

            base.AddSynthesizedAttributes(compilationState, ref attributes);
        }
    }
}
