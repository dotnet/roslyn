#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

set -e

dir="$( cd -P "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
dockerfile="$dir"/Mono

[ -z "$CONTAINER_TAG" ] && CONTAINER_TAG="roslyn-build"
[ -z "$CONTAINER_NAME" ] && CONTAINER_NAME="roslyn-build-container-mono-nightly"
[ -z "$DOCKER_HOST_SHARE_dir" ] && DOCKER_HOST_SHARE_DIR="$dir"/../..

# Ensure the container isn't already running. Can happened for cancelled jobs in CI
docker kill $CONTAINER_NAME || true

# Make container names CI-specific if we're running in CI
#  Jenkins
[ ! -z "$BUILD_TAG" ] && CONTAINER_NAME="$BUILD_TAG"

# Build the docker container (will be fast if it is already built)
echo "Building Docker Container using Dockerfile: $dockerfile"
docker build --build-arg USER_ID=$(id -u) --build-arg CACHE_BUST=$(date +%s) --no-cache -t $CONTAINER_TAG $dockerfile

# Run the build in the container
echo "Launching build in Docker Container"
echo "Running command: $BUILD_COMMAND"
echo "Using code from: $DOCKER_HOST_SHARE_DIR"

# Note: passwords/keys should not be passed in the environment
docker run -t --rm --sig-proxy=true \
    --name $CONTAINER_NAME \
    -v $DOCKER_HOST_SHARE_DIR:/opt/code \
    $CONTAINER_TAG \
    $BUILD_COMMAND "$@"
