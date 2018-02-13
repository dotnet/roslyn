#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

FROM ubuntu:16.04

# Install the base toolchain we need to build anything
# this does not include libraries that we need to compile different projects, we'd like
# them in a different layer.
RUN rm -rf rm -rf /var/lib/apt/lists/* && \
    apt-get clean && \
    apt-get update && \
    apt-get install -y make \
            git \
            curl \
            tar \
            sudo && \
    apt-get clean

# Dependencies for CoreCLR and CoreFX
RUN apt-get install -y  libunwind8 \
            libkrb5-3 \
            libicu55 \
            liblttng-ust0 \
            libssl1.0.0 \
            zlib1g \
            libuuid1 \
            liblldb-3.6 \
            libcurl4-openssl-dev && \
    apt-get clean

# Setup User to match Host User, and give superuser permissions
ARG USER_ID=0
RUN useradd -m code_executor -u ${USER_ID} -g sudo
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# With the User Change, we need to change permissions on these directories
RUN chmod -R a+rwx /usr/local
RUN chmod -R a+rwx /home
RUN chmod -R 755 /usr/lib/sudo

# Set user to the one we just created
USER ${USER_ID}

# Set working directory
WORKDIR /opt/code
