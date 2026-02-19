#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
FLOWXEL_CSPROJ="${ROOT_DIR}/rUI/rUI.csproj"
FEED_PATH="${HOME}/.nuget/local-feed"
PACKAGE_OUT="${ROOT_DIR}/artifacts/packages"
BUMP_KIND="patch"
EXPLICIT_VERSION=""
CONFIGURATION="Release"
PROJECTS=(
  "${ROOT_DIR}/rUI.Avalonia.Desktop/Flowxel.UI.csproj"
  "${ROOT_DIR}/rUI/rUI.csproj"
)

usage() {
  cat <<EOF
Usage: $(basename "$0") [--bump patch|minor|major] [--version X.Y.Z[-suffix]] [--feed PATH] [--configuration Release|Debug]

Builds packable Flowxel projects, bumps package version, and publishes packages to local NuGet feed.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --bump) BUMP_KIND="${2:-}"; shift 2 ;;
    --version) EXPLICIT_VERSION="${2:-}"; shift 2 ;;
    --feed) FEED_PATH="${2:-}"; shift 2 ;;
    --configuration) CONFIGURATION="${2:-}"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage; exit 1 ;;
  esac
done

if [[ -n "$EXPLICIT_VERSION" ]]; then
  TARGET_VERSION="$EXPLICIT_VERSION"
else
  CURRENT_VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$FLOWXEL_CSPROJ" | head -n 1)"
  if [[ ! "$CURRENT_VERSION" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)(-.+)?$ ]]; then
    echo "Current version '${CURRENT_VERSION}' is not semver-like." >&2; exit 1
  fi

  MAJOR="${BASH_REMATCH[1]}"; MINOR="${BASH_REMATCH[2]}"; PATCH="${BASH_REMATCH[3]}"
  case "$BUMP_KIND" in
    patch) PATCH=$((PATCH + 1)) ;;
    minor) MINOR=$((MINOR + 1)); PATCH=0 ;;
    major) MAJOR=$((MAJOR + 1)); MINOR=0; PATCH=0 ;;
  esac
  TARGET_VERSION="${MAJOR}.${MINOR}.${PATCH}"
fi

# Update the version in the meta-project file
sed -i "s|<Version>.*</Version>|<Version>${TARGET_VERSION}</Version>|" "$FLOWXEL_CSPROJ"

echo "Publishing Flowxel packages (v${TARGET_VERSION}) to ${FEED_PATH}"
mkdir -p "${FEED_PATH}" "${PACKAGE_OUT}"
rm -f "${PACKAGE_OUT}"/Flowxel*.nupkg "${PACKAGE_OUT}"/Flowxel*.snupkg

for project in "${PROJECTS[@]}"; do
  dotnet pack "${project}" -c "${CONFIGURATION}" -p:Version="${TARGET_VERSION}" -p:PackageOutputPath="${PACKAGE_OUT}"
done

shopt -s nullglob
PACKAGES=( "${PACKAGE_OUT}"/Flowxel*.nupkg "${PACKAGE_OUT}"/Flowxel*.snupkg )
rm -f "${FEED_PATH}"/Flowxel*.nupkg "${FEED_PATH}"/Flowxel*.snupkg
cp -f "${PACKAGES[@]}" "${FEED_PATH}/"

echo -e "\nPublished in ${FEED_PATH}:"
ls -1 "${FEED_PATH}"/Flowxel*.nupkg
