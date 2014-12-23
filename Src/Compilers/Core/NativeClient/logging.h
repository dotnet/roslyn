// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#pragma once

#include <Windows.h>
#include <exception>
#include <string>

const wchar_t * const LOGGING_ENV_VAR = L"RoslynCommandLineLogFile";

class FatalError : public std::exception
{
public:
    std::wstring message;

    FatalError(std::wstring&& message)
    : message(message) {}
};

bool HaveLogFile();
std::wstring GetResourceString(UINT);
bool GetEnvVar(_In_z_ LPCWSTR name, _Out_ std::wstring &value);
void InitializeLogging();
void Log(UINT loadResource);
void Log(_In_z_ LPCWSTR message);
void LogFormatted(UINT loadResource, ...);
void LogFormatted(_In_z_ LPCWSTR message, ...);
void LogTime();
void LogWin32Error(UINT loadResource);
void LogWin32Error(_In_z_ LPCWSTR message);
void FailWithGetLastError(UINT loadResource);
void FailWithGetLastError(_In_z_ LPCWSTR optionalPrefix = nullptr);
void FailWithGetLastError(_In_ const std::wstring& optionalPrefix);
void FailFormatted(UINT loadResource, ...);
void FailFormatted(_In_z_ LPCWSTR message, ...);
