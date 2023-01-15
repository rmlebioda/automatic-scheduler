#!/bin/bash

TARGET_DIRECTORY="${DOTNET_SYNC_DIRECTORY}"
echo "Automatically sync this folder with environment variable DOTNET_SYNC_DIRECTORY '${TARGET_DIRECTORY}' to make up-to-date backup";
if [ "$TARGET_DIRECTORY" = "" ]; then
  echo "You need to provide target path to sync";
  exit 1
fi

TARGET_DIRECTORY="${TARGET_DIRECTORY}/${PWD##*/}"
echo "syncing to ${TARGET_DIRECTORY}"

while inotifywait -r -e modify,create,delete,move . ; do
    rsync -avz . ${TARGET_DIRECTORY}
done
