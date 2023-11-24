#!/bin/bash

TexturePacker="/Applications/TexturePacker.app/Contents/MacOS/TexturePacker"

PATH_SRC=$1
PATH_DST=$2
TRIN_MODE=$3
SHAPE_PADDING=$4

DATA_FILE=${PATH_DST}.txt
SHEET_FILE=${PATH_DST}.png 
TPS_FILE=${PATH_DST}.tps

#--reduce-border-artifacts 减少边界伪影  --smart-update 
$TexturePacker $PATH_SRC --data $DATA_FILE --format unity --sheet $SHEET_FILE --max-size 2048 --size-constraints POT --force-squared --disable-rotation --trim-mode $TRIN_MODE --trim-margin 0 --extrude 1  --border-padding 0 --shape-padding $SHAPE_PADDING --reduce-border-artifacts
#$TexturePacker $PATH_SRC --data $DATA_FILE --format unity --sheet $SHEET_FILE --max-size 2048 --#size-constraints POT --force-squared --disable-rotation --trim-mode $TRIN_MODE --trim-margin 0 --#extrude 1  --border-padding 0 --shape-padding $SHAPE_PADDING --reduce-border-artifacts --save #$TPS_FILE
#$TexturePacker test.tps
