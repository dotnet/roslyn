@echo off

:: Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
call "%VS140COMNTOOLS%VsDevCmd.bat"

pushd %~dp0

:: Create a task which passes the location of the share to monitor as the parameter to look for new binary
start csi %~dp0monitor_and_start_test.csx %1

popd