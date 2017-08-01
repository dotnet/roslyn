// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Metadata
{
    public class ICSPropImpl : ICSProp
    {
        protected EGoo efoo;
        public virtual EGoo ReadOnlyProp
        {
            get { return efoo; }
        }

        public virtual EGoo WriteOnlyProp
        {
            set { efoo = value; }
        }

        public virtual EGoo ReadWriteProp
        {
            get { return efoo; }
            set { efoo = value; }
        }
    }

    abstract public class ICSGenImpl<T, V> //: ICSGen<T, V>
    {
        public virtual void M01(T p1, T p2)
        {
            Console.Write("Base_TT ");
        }

        public virtual void M01(T p1, params T[] ary)
        {
            Console.Write("Base_TParamsT ");
        }

        public virtual void M01(params T[] ary)
        {
            Console.Write("Base_ParamsT ");
        }

        public abstract void M01(T p1, ref T p2, out DGoo<T> p3);

        public string M01(V p1, V p2)
        {
            Console.Write("BaseNV_VV ");
            return p1.ToString();
        }

        public virtual string M01(V p1, object p2)
        {
            Console.Write("Base_VObj ");
            return p1.ToString();
        }

        public virtual string M01(V p1, params object[] p2)
        {
            Console.Write("Base_VParams ");
            return p1.ToString();
        }
    }

}
