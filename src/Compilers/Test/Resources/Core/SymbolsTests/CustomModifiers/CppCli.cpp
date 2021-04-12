// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//cl /clr:safe /LD CppCli.cpp

using namespace System;

namespace CppCli 
{

    public interface class CppInterface1
    {
    public:
        void Method1(const int x);
        void Method2(const int x);
    };

    // Identical, but distinct from, CppInterface1
    public interface class CppInterface2
    {
    public:
        void Method1(const int x);
        void Method2(const int x);
    };

    public ref class CppBase1
    {
    public:
        virtual void VirtualMethod(const int x)
        {
            Console::WriteLine("CppBase1::VirtualMethod({0})", x);
        }
        void NonVirtualMethod(const int x)
        {
            Console::WriteLine("CppBase1::NonVirtualMethod({0})", x);
        }
    };

    public ref class CppBase2
    {
    public:
        virtual void Method1(const int x)
        {
            Console::WriteLine("CppBase2::Method1({0})", x);
        }
        void Method2(const int x)
        {
            Console::WriteLine("CppBase2::Method2({0})", x);
        }
    };

    public interface class CppBestMatchInterface
    {
    public:
        void Method(const int x, int y);
    };

    public ref class CppBestMatchBase1
    {
    public:
        virtual void Method(int x, const int y)
        {
            Console::WriteLine("CppBestMatchBase1::Method({0},{1})", x, y);
        }
    };

    public ref class CppBestMatchBase2 : CppBestMatchBase1
    {
    public:
        virtual void Method(const int x, const int y) new
        {
            Console::WriteLine("CppBestMatchBase2::Method({0},{1})", x, y);
        }
    };

    public interface class CppIndexerInterface
    {
    public:
        property int default[const int]
        {
            int get(const int i);
            void set(const int i, int value);
        }
    };

    public ref class CppIndexerBase
    {
    public:
        
        virtual property int default[const int]
        {
            int get(const int i) { Console::WriteLine("CppBase1::Item.get({0})", i); return 0; }
            void set(const int i, int value) { Console::WriteLine("CppBase1::Item.set({0})", i); }
        }
    };
}
