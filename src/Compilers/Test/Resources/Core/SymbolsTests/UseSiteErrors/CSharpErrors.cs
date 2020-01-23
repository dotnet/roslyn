// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Depends on Unavailable.dll
// csc /target:library /reference:Unavailable.dll CSharpErrors.cs

namespace CSharpErrors
{
    public class Subclass1 : UnavailableClass { }
    public class Subclass2<T> : UnavailableClass<T> { }
    public class Subclass3 : UnavailableClass<int> { }

    public class ImplementingClass1 : UnavailableInterface { }
    public class ImplementingClass2<T> : UnavailableInterface<T> { }
    public class ImplementingClass3 : UnavailableInterface<int> { }

    public struct ImplementingStruct1 : UnavailableInterface { }
    public struct ImplementingStruct2<T> : UnavailableInterface<T> { }
    public struct ImplementingStruct3 : UnavailableInterface<int> { }

    public delegate UnavailableClass DelegateReturnType1();
    public delegate UnavailableClass DelegateReturnType2();
    public delegate void DelegateParameterType1(UnavailableClass u);
    public delegate void DelegateParameterType2(UnavailableClass[] u);
    public delegate UnavailableClass<T> DelegateParameterType3<T>(UnavailableClass<T> u);

    public class ClassMethods
    {
        public virtual UnavailableClass ReturnType1() { return null; }
        public virtual UnavailableClass[] ReturnType2() { return null; }

        public virtual void ParameterType1(UnavailableClass u) { }
        public virtual void ParameterType2(UnavailableClass[] u) { }
    }

    public interface InterfaceMethods
    {
        UnavailableClass ReturnType1();
        UnavailableClass[] ReturnType2();

        void ParameterType1(UnavailableClass u);
        void ParameterType2(UnavailableClass[] u);
    }

    public class ClassProperties
    {
        public virtual UnavailableClass GetSet1 { get; set; }
        public virtual UnavailableClass[] GetSet2 { get; set; }
        public virtual UnavailableClass Get1 { get { return null; } }
        public virtual UnavailableClass[] Get2 { get { return null; } }
        public virtual UnavailableClass Set1 { set { } }
        public virtual UnavailableClass[] Set2 { set { } }
    }

    public interface InterfaceProperties
    {
        UnavailableClass GetSet1 { get; set; }
        UnavailableClass[] GetSet2 { get; set; }
        UnavailableClass Get1 { get; }
        UnavailableClass[] Get2 { get; }
        UnavailableClass Set1 { set; }
        UnavailableClass[] Set2 { set; }
    }

    public delegate void EventDelegate<T>();

    public class ClassEvents
    {
        public virtual event UnavailableDelegate Event1;
        public virtual event EventDelegate<UnavailableClass> Event2;
        public virtual event EventDelegate<UnavailableClass[]> Event3;
    }

    public interface InterfaceEvents
    {
        event UnavailableDelegate Event1;
        event EventDelegate<UnavailableClass> Event2;
        event EventDelegate<UnavailableClass[]> Event3;
    }
}
