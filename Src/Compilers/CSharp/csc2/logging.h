// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma once

const char * const LOGGING_ENV_VAR = "RoslynCommandLineLogFile";

bool HaveLogFile();
void InitializeLogging();
void Log(char * message);
void LogFormatted(char * message, ...);
void LogTime();
void LogWin32Error(char * message);
void Exit(int exitCode);