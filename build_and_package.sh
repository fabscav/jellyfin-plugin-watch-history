#!/usr/bin/env bash
set -euo pipefail
echo "Restoring and building plugin..."
dotnet restore
dotnet build -c Release
echo "Publishing plugin (framework-dependent publish)..."
dotnet publish -c Release -o ./publish
echo "Preparing plugin package..."
PKG_NAME="WatchHistoryRating-$(date +%Y.%m.%d).zip"
mkdir -p package/WatchHistoryRating
# copy published files (DLLs, deps) and plugin.json and UI resources
cp -r publish/* package/WatchHistoryRating/ || true
cp plugin.json package/WatchHistoryRating/ || true
# include UI folder and other resources
cp -r UI package/WatchHistoryRating/ || true
zip -r "$PKG_NAME" package
echo "Created $PKG_NAME"
ls -l "$PKG_NAME"
