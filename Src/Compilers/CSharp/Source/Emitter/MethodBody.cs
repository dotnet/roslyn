using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp.Emit
{
    /// <summary>
    /// Holds on to the method body data.
    /// </summary>
    internal class MethodBody : Microsoft.Cci.IMethodBody
    {
        private Microsoft.Cci.MemoryStream ilBits;
        private ushort maxStack;
        private Microsoft.Cci.IMethodDefinition parent;
        private IEnumerable<LocalDefinition> locals;    // built by someone else
        private Microsoft.Cci.SequencePoint[] sequencePoints;

        private uint[] localScopes;

        internal class LocalScopeDefinition : Microsoft.Cci.ILocalScope
        {
            private uint begin;
            private uint end;
            private Microsoft.Cci.IMethodDefinition parent;

            public LocalScopeDefinition(uint begin, uint end, Microsoft.Cci.IMethodDefinition parent)
            {
                this.begin = begin;
                this.end = end;
                this.parent = parent;
            }

            public uint Offset
            {
                get { return begin; }
            }

            public uint Length
            {
                get { return end; }
            }

            public Microsoft.Cci.IMethodDefinition MethodDefinition
            {
                get { return this.parent; }
            }
        }

        public IEnumerable<Microsoft.Cci.ILocalScope> LocalScopes
        {
            get
            {
                if (localScopes != null)
                {
                    for (uint i = 0; i < localScopes.Length; i += 2)
                    {
                        yield return new LocalScopeDefinition(localScopes[i], localScopes[i + 1], parent);
                    }
                }
            }
        }

        public MethodBody(Microsoft.Cci.MemoryStream ilBits,
            ushort maxStack,
            Microsoft.Cci.IMethodDefinition parent,
            IEnumerable<LocalDefinition> locals,
            Microsoft.Cci.SequencePoint[] sequencePoints)
        {
            this.ilBits = ilBits;
            this.maxStack = maxStack;
            this.parent = parent;
            this.locals = locals;
            this.sequencePoints = sequencePoints;
        }

        void Microsoft.Cci.IMethodBody.Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            throw new NotImplementedException();
        }

        IEnumerable<Microsoft.Cci.IOperationExceptionInformation> Microsoft.Cci.IMethodBody.OperationExceptionInformation
        {
            get { return Enumerable.Empty<Microsoft.Cci.IOperationExceptionInformation>(); }
        }

        bool Microsoft.Cci.IMethodBody.LocalsAreZeroed
        {
            get { return true; }
        }

        IEnumerable<Microsoft.Cci.ILocalDefinition> Microsoft.Cci.IMethodBody.LocalVariables
        {
            get { return this.locals; }
        }

        Microsoft.Cci.IMethodDefinition Microsoft.Cci.IMethodBody.MethodDefinition
        {
            get { return parent; }
        }

        IEnumerable<Microsoft.Cci.IOperation> Microsoft.Cci.IMethodBody.Operations
        {
            // Operations are not visited. The purpose of their visit is to record
            // type references. Do that wholesale for all symbols referenced by all
            // methods.
            get { return Enumerable.Empty<Microsoft.Cci.IOperation>(); }
        }

        ushort Microsoft.Cci.IMethodBody.MaxStack
        {
            get { return maxStack; }
        }

        IEnumerable<Microsoft.Cci.ITypeDefinition> Microsoft.Cci.IMethodBody.PrivateHelperTypes
        {
            get { return Enumerable.Empty<Microsoft.Cci.ITypeDefinition>(); }
        }

        public Microsoft.Cci.MemoryStream IL
        {
            get { return ilBits; }
        }

        public Microsoft.Cci.SequencePoint[] SequencePoints
        {
            get { return this.sequencePoints; }
        }

        public Microsoft.Cci.SequencePoint[] Locations
        {
            get { return this.sequencePoints; }
        }
    }
}
