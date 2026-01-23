Native library builds

Outputs
- macOS: native/bin/macos/libsurfer.dylib
- Windows: native/bin/windows/surfer.dll

macOS (Apple Silicon, Homebrew)
1) Install deps (CGAL + Boost):
   - brew install cgal boost
2) Configure:
   - cmake -S native/surfer2 -B native/surfer2/build -DCMAKE_BUILD_TYPE=Release -DLIB_ONLY=ON -DBUILD_SHARED_LIBS=ON
3) Build:
   - cmake --build native/surfer2/build --config Release -j 8
4) Copy output:
   - cp native/surfer2/build/surf/libsurfer.dylib native/bin/macos/

Windows (MSVC + vcpkg)
1) Install vcpkg and deps:
   - vcpkg install cgal boost-graph boost-iostreams mpfr gmp --triplet x64-windows
2) Configure (from a VS Developer Command Prompt):
   - cmake -S native/surfer2 -B native/surfer2/build-win -DLIB_ONLY=ON -DBUILD_SHARED_LIBS=ON -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE=C:/path/to/vcpkg/scripts/buildsystems/vcpkg.cmake
3) Build:
   - cmake --build native/surfer2/build-win --config Release
4) Copy output:
   - copy native\surfer2\build-win\surf\Release\surfer.dll native\bin\windows\surfer.dll

Notes
- The GH plugin expects the native library next to the .gha at runtime.
- If you rename the library, update Pondskater/Native/SurferNative.cs.
