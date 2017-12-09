#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Temporary file until the netci.groovy change to use the new location propogates
exec ./build/scripts/cibuild.sh "$@"
