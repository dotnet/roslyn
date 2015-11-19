// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#if TODO
#pragma warning( push )
#pragma warning( disable: 4499 )
#include "CppUnitTest.h"
#pragma warning (pop)

#include <memory>
#include <sstream>
#include <Windows.h>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace Microsoft_DiaSymReader_PortablePdb_UnitTests
{
    // {E4B18DEF-3B78-46AE-8F50-E67E421BDF70}
    static const GUID CLSID_Factory = { 0xE4B18DEF, 0x3B78, 0x46AE, { 0x8F, 0x50, 0xE6, 0x7E, 0x42, 0x1B, 0xDF, 0x70 } };

    // {AA544D42-28CB-11d3-BD22-0000F80849BD}
    static const GUID IID_ISymUnmanagedBinder = { 0xAA544D42, 0x28CB, 0x11d3, { 0xBD, 0x22, 0x00, 0x00, 0xF8, 0x08, 0x49, 0xBD } };

    // To run these test from command line 
    // vstest.console.exe Microsoft.DiaSymReader.PortablePdb.Native.UnitTests.dll
    TEST_CLASS(SymReaderTests)
    {
    public:
        TEST_METHOD(Instantiation)
        {
            HRESULT hr;

            hr = CoInitialize(nullptr);
            Assert::IsTrue(hr == S_OK || hr == S_FALSE, L"CoInitialize");

            LPVOID factory;
            hr = CoCreateInstance(CLSID_Factory, nullptr, CLSCTX_INPROC_SERVER, IID_ISymUnmanagedBinder, &factory);
            Assert::AreEqual(S_OK, hr, L"CoCreateInstance");
            
            Assert::AreEqual(1, 1);
        }
    };

}
#endif