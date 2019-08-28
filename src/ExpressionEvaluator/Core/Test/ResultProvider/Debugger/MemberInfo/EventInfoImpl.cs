// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Metadata;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class EventInfoImpl : EventInfo
    {
        internal readonly System.Reflection.EventInfo Event;

        internal EventInfoImpl(System.Reflection.EventInfo @event)
        {
            Debug.Assert(@event != null);
            this.Event = @event;
        }

        public override System.Reflection.EventAttributes Attributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Type DeclaringType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsEquivalentTo(MemberInfo other)
        {
            throw new NotImplementedException();
        }

        public override MemberTypes MemberType
        {
            get
            {
                return (MemberTypes)this.Event.MemberType;
            }
        }

        public override int MetadataToken
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Module Module
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Name
        {
            get
            {
                return Event.Name;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
