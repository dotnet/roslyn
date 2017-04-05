#!/usr/bin/env bash

# Unzip reports a warning that zip contains backslashes and returns exit code 1 
# even though everything is good. Ignore exit code 1.

unzip -nq $1 -d $2
EC=$?
if [ $EC -eq 1 ]
  then exit 0
  else exit $EC
fi