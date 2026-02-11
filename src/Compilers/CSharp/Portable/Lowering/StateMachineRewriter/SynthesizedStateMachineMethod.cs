// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

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

        IMethodSymbolInternal ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return StateMachineType.KickoffMethod; }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return this.StateMachineType.KickoffMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
        }
    }

    /// <summary>
    /// Represents a state machine MoveNext or MoveNextAsync method.
    /// Handles special behavior around inheriting some attributes from the original async/iterator method.
    /// </summary>
    internal sealed class SynthesizedStateMachineMoveNextMethod : SynthesizedStateMachineMethod
    {
        private ImmutableArray<CSharpAttributeData> _attributes;

        // Indicates that the method body follows runtime-async conventions and should be emitted with MethodImplAttributes.Async flag
        internal readonly bool RuntimeAsync;

        public SynthesizedStateMachineMoveNextMethod(MethodSymbol interfaceMethod, StateMachineTypeSymbol stateMachineType, bool runtimeAsync)
            : base(interfaceMethod.Name, interfaceMethod, stateMachineType, null, generateDebugInfo: true, hasMethodBodyDependency: true)
        {
            // PROTOTYPE consider reverting to only use "MoveNext" name, as it is expected by various tools that are aware of state machines (EnC, symbols)
            Debug.Assert(interfaceMethod.Name is WellKnownMemberNames.MoveNextMethodName or WellKnownMemberNames.MoveNextAsyncMethodName);
            Debug.Assert(!runtimeAsync || CSharpCompilation.IsValidRuntimeAsyncReturnType(ReturnType.OriginalDefinition));

            RuntimeAsync = runtimeAsync;
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
                    if (attribute.IsTargetAttribute(AttributeDescription.DebuggerHiddenAttribute) ||
                        attribute.IsTargetAttribute(AttributeDescription.DebuggerNonUserCodeAttribute) ||
                        attribute.IsTargetAttribute(AttributeDescription.DebuggerStepperBoundaryAttribute) ||
                        attribute.IsTargetAttribute(AttributeDescription.DebuggerStepThroughAttribute))
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

        public override bool IsAsync => RuntimeAsync;

        internal override MethodImplAttributes ImplementationAttributes
        {
            get
            {
                MethodImplAttributes result = default;
                if (RuntimeAsync)
                {
                    result |= MethodImplAttributes.Async;
                }

                return result;
            }
        }
    }

    /// <summary>
    /// Represents a state machine method other than a MoveNext method.
    /// All such methods are considered debugger hidden. 
    /// </summary>
    internal sealed class SynthesizedStateMachineDebuggerHiddenMethod : SynthesizedStateMachineMethod
    {
        // Indicates that the method should be emitted with MethodImplAttributes.Async flag
        private readonly bool _runtimeAsync;

        public SynthesizedStateMachineDebuggerHiddenMethod(
            string name,
            MethodSymbol interfaceMethod,
            StateMachineTypeSymbol stateMachineType,
            PropertySymbol associatedProperty,
            bool hasMethodBodyDependency,
            bool runtimeAsync)
            : base(name, interfaceMethod, stateMachineType, associatedProperty, generateDebugInfo: false, hasMethodBodyDependency: hasMethodBodyDependency)
        {
            Debug.Assert(!runtimeAsync || CSharpCompilation.IsValidRuntimeAsyncReturnType(ReturnType.OriginalDefinition));
            _runtimeAsync = runtimeAsync;
        }

        internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor));

            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
        }

        public override bool IsAsync => _runtimeAsync;

        internal override MethodImplAttributes ImplementationAttributes
        {
            get
            {
                MethodImplAttributes result = default;
                if (_runtimeAsync)
                {
                    result |= MethodImplAttributes.Async;
                }

                return result;
            }
        }
    }
}
