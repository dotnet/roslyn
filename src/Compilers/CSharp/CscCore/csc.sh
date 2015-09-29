#!/bin/sh

THISDIR=$(dirname $0)

chmod +x $THISDIR/CoreRun 2>/dev/null
$THISDIR/CoreRun $THISDIR/csc.exe "$@"
