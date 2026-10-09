#!/usr/bin/env bash
# Builds RadarLabelsFix.dll and the Thunderstore package zip.
# Usage: ./build.sh  (override GAME_MANAGED / BEPINEX_CORE if your paths differ)
set -euo pipefail
cd "$(dirname "$0")"

GAME_MANAGED="${GAME_MANAGED:-/mnt/Steam/SteamLibrary/steamapps/common/Lethal Company/Lethal Company_Data/Managed}"
BEPINEX_CORE="${BEPINEX_CORE:-$HOME/.config/r2modmanPlus-local/LethalCompany/profiles/MoreBrutal/BepInEx/core}"

mkdir -p build
refs=(mscorlib netstandard System System.Core Assembly-CSharp UnityEngine UnityEngine.CoreModule Unity.TextMeshPro Unity.Netcode.Runtime UnityEngine.UI)
args=()
for r in "${refs[@]}"; do args+=("-r:$GAME_MANAGED/$r.dll"); done
args+=("-r:$BEPINEX_CORE/BepInEx.dll" "-r:$BEPINEX_CORE/0Harmony.dll")

mcs -target:library -optimize+ -nostdlib -out:build/RadarLabelsFix.dll "${args[@]}" src/*.cs

version=$(sed -n 's/.*"version_number": *"\([^"]*\)".*/\1/p' thunderstore/manifest.json)
stage=build/package
rm -rf "$stage" && mkdir -p "$stage"
cp thunderstore/manifest.json thunderstore/icon.png README.md CHANGELOG.md build/RadarLabelsFix.dll "$stage/"
rm -f "build/Gezang-RadarLabelsFix-$version.zip"
(cd "$stage" && zip -q -X "../Gezang-RadarLabelsFix-$version.zip" ./*)
echo "Built build/RadarLabelsFix.dll and build/Gezang-RadarLabelsFix-$version.zip"
