#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(dirname "$0")/.."
cd "$ROOT_DIR"

mkdir -p Pondskater_GH/dist

# Copy existing .gha and zip artifacts if present
cp -v distribution/mac/*.gha Pondskater_GH/dist/ 2>/dev/null || true
cp -v distribution/win/*.gha Pondskater_GH/dist/ 2>/dev/null || true
cp -v distribution/*.zip Pondskater_GH/dist/ 2>/dev/null || true

echo "Artifacts copied to Pondskater_GH/dist/"
