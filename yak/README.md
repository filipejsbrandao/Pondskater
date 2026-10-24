# Creating Pondskater Yak packages

This workflow creates separate Rhino 8 packages for macOS and Windows using
Rhino 8's bundled Yak CLI on macOS. Run commands from the repository root unless
the command explicitly changes directory. If `Pondskater/` is your workspace,
first run `cd ..`.

`mac/` and `win/` are package staging directories. Deliverable copies belong in
[`Pondskater_GH/dist/`](../Pondskater_GH/dist/), the authoritative artifact
directory. Historical archives should remain unchanged.

## 1. Update the version

Use the same release version in all four places:

- `<Version>` in [`Pondskater.csproj`](../Pondskater/Pondskater.csproj).
- `Version` in [`PondskaterInfo.cs`](../Pondskater/PondskaterInfo.cs).
- `version` in [`mac/manifest.yml`](mac/manifest.yml).
- `version` in [`win/manifest.yml`](win/manifest.yml).

Preserve the plugin and component GUIDs. Version `0.0.12` is the example below;
set `release_version` to the version being packaged.

## 2. Compile and validate

Install Rhino 8 and the required .NET SDKs. If dependencies are not already
restored, run `dotnet restore Pondskater/Pondskater.csproj` first.

Build targets separately, waiting for each command to finish:

```sh
dotnet build Pondskater/Pondskater.csproj -c Debug -f net7.0 --no-restore -p:UseSharedCompilation=false -m:1 -nr:false
dotnet build Pondskater/Pondskater.csproj -c Release -f net48 --no-restore -p:UseSharedCompilation=false -m:1 -nr:false
dotnet build Pondskater/Pondskater.csproj -c Release -f net7.0 --no-restore -p:UseSharedCompilation=false -m:1 -nr:false
```

Every build copies its GHA into `Pondskater_GH/dist/`, so the final build above
leaves the Release .NET 7 assembly there. The existing Rhino 8 packages use the
Release `net7.0` GHA on both platforms; compiling `net48` also validates that
declared target, but its assembly is not included in these packages.

Run applicable regression checks. For the L-joint geometry harness:

```sh
dotnet run --project tests/ComponentGeometrySmoke/ComponentGeometrySmoke.csproj --no-restore -p:UseSharedCompilation=false -p:ShouldRemoveRhinoReferences=false
```

For native changes, follow [`native/BUILD.md`](../native/BUILD.md), update the
appropriate staging libraries, and run the applicable native smoke programs
listed in [`AGENTS.md`](../AGENTS.md). Smoke programs load from `native/bin/`,
while plugin builds copy native libraries from `yak/mac/` or `yak/win/`: verify
that the tested library and packaged library are the intended versions.

For component behavior changes, also test representative inputs and runtime
messages in Rhino. Record build/smoke results separately from live Rhino checks.

## 3. Stage the package files

```sh
cp Pondskater/bin/Pondskater/Release/net7.0/Pondskater.gha yak/mac/Pondskater.gha
cp Pondskater/bin/Pondskater/Release/net7.0/Pondskater.gha yak/win/Pondskater.gha
```

The expected package contents are:

| macOS | Windows |
| --- | --- |
| `Pondskater.gha` | `Pondskater.gha` |
| `manifest.yml` | `manifest.yml` |
| `icon.png` | `icon.png` |
| `libsurfer.dylib` | `surfer.dll` |
| | `gmp-10.dll` |
| | `boost_graph-vc143-mt-x64-1_90.dll` |

Reuse the existing native binaries when native code and dependencies have not
changed. A macOS managed build does not rebuild the Windows native libraries.
Keep unrelated files out of the staging directories and inspect archive contents
after building. The bundled Yak CLI excluded older `.yak` files when creating
the 0.0.12 packages.

## 4. Create both packages

Yak must run inside each staging directory so it finds `manifest.yml`. These
subshell commands return you to the repository root automatically:

```sh
(cd yak/mac && '/Applications/Rhino 8.app/Contents/Resources/bin/yak' build --platform mac)
(cd yak/win && '/Applications/Rhino 8.app/Contents/Resources/bin/yak' build --platform win)
```

Check Yak's output for the expected version, plugin GUID, platform and Rhino 8
distribution tag. Expected filenames are
`pondskater-<version>-rh8_0-mac.yak` and
`pondskater-<version>-rh8_0-win.yak`.

## 5. Verify and collect deliverables

```sh
release_version=0.0.12
unzip -t "yak/mac/pondskater-${release_version}-rh8_0-mac.yak"
unzip -t "yak/win/pondskater-${release_version}-rh8_0-win.yak"
unzip -l "yak/mac/pondskater-${release_version}-rh8_0-mac.yak"
unzip -l "yak/win/pondskater-${release_version}-rh8_0-win.yak"
unzip -p "yak/mac/pondskater-${release_version}-rh8_0-mac.yak" manifest.yml
unzip -p "yak/win/pondskater-${release_version}-rh8_0-win.yak" manifest.yml
```

Confirm the exact file lists above and matching versions/platforms. Compare
each archived GHA with the Release assembly and native files with their staging
inputs; for example, from the same shell:

```sh
python3 - "$release_version" <<'PY'
from pathlib import Path
import sys
import zipfile

version = sys.argv[1]
assembly = Path('Pondskater/bin/Pondskater/Release/net7.0/Pondskater.gha').read_bytes()
for platform, native in [('mac', ['libsurfer.dylib']),
                         ('win', ['surfer.dll', 'gmp-10.dll',
                                  'boost_graph-vc143-mt-x64-1_90.dll'])]:
    staging = Path('yak') / platform
    with zipfile.ZipFile(staging / f'pondskater-{version}-rh8_0-{platform}.yak') as archive:
        assert archive.testzip() is None
        assert set(archive.namelist()) == set(['Pondskater.gha', 'manifest.yml', 'icon.png'] + native)
        assert archive.read('Pondskater.gha') == assembly
        for name in ['icon.png'] + native:
            assert archive.read(name) == (staging / name).read_bytes()
    print(f'{platform}: package contents verified')
PY

mkdir -p Pondskater_GH/dist
cp "yak/mac/pondskater-${release_version}-rh8_0-mac.yak" Pondskater_GH/dist/
cp "yak/win/pondskater-${release_version}-rh8_0-win.yak" Pondskater_GH/dist/
cp Pondskater/bin/Pondskater/Release/net7.0/Pondskater.gha Pondskater_GH/dist/Pondskater.gha
```

Package creation does not publish to the Yak server. Report the deliverable
paths and any outstanding Rhino/platform validation when handing off.
