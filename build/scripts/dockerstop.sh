#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# This script is meant to ensure that all docker containers are stopped when
# we exit CI. Hence exit with true even if "kill" failed as it will fail if 
# they stopped gracefully
docker kill $(docker ps -q) || true

