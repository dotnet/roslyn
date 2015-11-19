// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Test.Utilities
{
    public class ObjectReference
    {
        public object Strong;
        public readonly WeakReference Weak;

        public ObjectReference(object target)
        {
            this.Strong = target;
            this.Weak = new WeakReference(target);
        }

        public ObjectReference() : this(new object())
        {
        }
    }
}
