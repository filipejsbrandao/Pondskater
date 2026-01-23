#pragma once

#ifdef __cplusplus
extern "C" {
#endif

#if defined(_WIN32)
  #define SURF_API __declspec(dllexport)
#else
  #define SURF_API __attribute__((visibility("default")))
#endif

// Returns 0 on success. Non-zero error code on failure.
// out/out_len contain the resulting output string (ipe or obj).
// err/err_len contain a human-readable error string on failure.
SURF_API int surf_run_graphml(
  const char* graphml,
  const char* skoffset,
  int component,
  int write_ipe,
  char** out,
  int* out_len,
  char** err,
  int* err_len
);

SURF_API void surf_free(void* p);

#ifdef __cplusplus
}
#endif
