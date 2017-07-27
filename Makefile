SHELL := /usr/bin/env bash
OS_NAME := $(shell uname -s)
BUILD_CONFIGURATION := Debug
# $(dir) ends with slash
THIS_MAKEFILE_PATH := $(dir $(realpath $(lastword $(MAKEFILE_LIST))))
BINARIES_PATH := $(THIS_MAKEFILE_PATH)Binaries
SRC_PATH := $(THIS_MAKEFILE_PATH)src
BOOTSTRAP_PATH := $(BINARIES_PATH)/Bootstrap
BUILD_LOG_PATH :=
DOTNET_VERSION := 1.0.1
DOTNET_PATH := $(BINARIES_PATH)/dotnet-cli
DOTNET := $(DOTNET_PATH)/dotnet
export PATH := $(DOTNET_PATH):$(PATH)

# Workaround, see https://github.com/dotnet/roslyn/issues/10210
ifeq ($(origin HOME), undefined)
    # Note that while ~ usually refers to $HOME, in the case where $HOME is unset,
    # it looks up the current user's home dir, which is what we want.
    # https://www.gnu.org/software/bash/manual/html_node/Tilde-Expansion.html
    export HOME := $(shell cd ~ && pwd)
endif

ifeq ($(OS_NAME),Linux)
    RUNTIME_ID := $(shell . /etc/os-release && echo $$ID.$$VERSION_ID)-x64
else ifeq ($(OS_NAME),Darwin)
    RUNTIME_ID := osx.10.12-x64
else
    $(error "Unknown OS_NAME: $(OS_NAME)")
endif

MSBUILD_ARGS := /nologo '/consoleloggerparameters:Verbosity=minimal;summary' /p:Configuration=$(BUILD_CONFIGURATION)

ifneq ($(BUILD_LOG_PATH),)
    MSBUILD_ARGS += /filelogger '/fileloggerparameters:Verbosity=normal;logFile=$(BUILD_LOG_PATH)'
endif

MSBUILD_MAIN_ARGS := $(MSBUILD_ARGS)
MSBUILD_BOOTSTRAP_ARGS := $(MSBUILD_ARGS)

MSBUILD_BOOTSTRAP_ARGS += /p:RuntimeIdentifier=$(RUNTIME_ID)

# This gets a bit complex. There are two cases here:
# BOOTSTRAP=false:
#   Things proceed simply. The "all" target does not depend on the bootstrap
#   target, so bootstrap is never built, and BootstrapBuildPath is unspecified.
# BOOTSTRAP=true:
#   BOOTSTRAP_DEPENDENCY is set to "bootstrap", making the "all" target depend
#   on it, and so the bootstrap compiler gets built. Additionally,
#   BootstrapBuildPath is specified, but *only* for the main build, *not* the
#   bootstrap build.
ifeq ($(BOOTSTRAP),true)
    # MSBUILD_MAIN_ARGS += /p:BootstrapBuildPath=$(BOOTSTRAP_PATH)
    MSBUILD_MAIN_ARGS += /p:CscToolPath=$(BOOTSTRAP_PATH)/csc /p:CscToolExe=csc /p:VbcToolPath=$(BOOTSTRAP_PATH)/vbc /p:VbcToolExe=vbc
    BOOTSTRAP_DEPENDENCY := bootstrap
else
    BOOTSTRAP_DEPENDENCY :=
endif

.PHONY: all bootstrap test restore

all: restore $(BOOTSTRAP_DEPENDENCY)
	@echo Building CrossPlatform.sln
	dotnet build $(THIS_MAKEFILE_PATH)CrossPlatform.sln $(MSBUILD_MAIN_ARGS)

bootstrap: restore
	@echo Building Bootstrap
	dotnet publish $(SRC_PATH)/Compilers/CSharp/CscCore -o $(BOOTSTRAP_PATH)/csc $(MSBUILD_BOOTSTRAP_ARGS)
	dotnet publish $(SRC_PATH)/Compilers/VisualBasic/VbcCore -o $(BOOTSTRAP_PATH)/vbc $(MSBUILD_BOOTSTRAP_ARGS)
	rm -rf $(BINARIES_PATH)/$(BUILD_CONFIGURATION)

test:
	dotnet publish $(SRC_PATH)/Test/DeployCoreClrTestRuntime -o $(BINARIES_PATH)/$(BUILD_CONFIGURATION)/CoreClrTest -r $(RUNTIME_ID) -p:RoslynRuntimeIdentifier=$(RUNTIME_ID) $(MSBUILD_MAIN_ARGS)
	$(THIS_MAKEFILE_PATH)build/scripts/tests.sh $(BUILD_CONFIGURATION)

restore: $(DOTNET)
	dotnet restore -v Minimal --disable-parallel $(THIS_MAKEFILE_PATH)build/ToolsetPackages/BaseToolset.csproj
	dotnet restore -v Minimal --disable-parallel $(THIS_MAKEFILE_PATH)CrossPlatform.sln

$(DOTNET):
	curl https://raw.githubusercontent.com/dotnet/cli/rel/$(DOTNET_VERSION)/scripts/obtain/dotnet-install.sh | \
	$(SHELL) -s -- --version "$(DOTNET_VERSION)" --install-dir "$(DOTNET_PATH)"

clean:
	rm -rf $(BINARIES_PATH)
