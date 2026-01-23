#include "surf_c_api.h"

#include "SkeletonStructure.h"
#include "tools.h"

#include <sstream>
#include <string>
#include <cstring>
#include <cstdlib>

static void ensure_logging_initialized() {
  static bool initialized = false;
  if (initialized) return;

  const char* argv0 = "surfer2";
  char* argv[] = { const_cast<char*>(argv0) };
  int argc = 1;
  setup_logging(argc, argv, true);
  initialized = true;
}

static void do_surf(std::istream& is, std::ostream& os, int restrict_component, const std::string& skoffset, bool write_ipe) {
  BGLGraph graph = BGLGraph::create_from_graphml(is);
  SkeletonStructure s = SkeletonStructure(BasicInputFromBGL(graph));
  s.initialize(restrict_component);
  s.wp.advance_to_end();

  if (write_ipe) {
    s.get_skeleton().write_ipe(os, skoffset);
  } else {
    s.get_skeleton().write_obj(os);
  }
}

static void set_out(char** out, int* out_len, const std::string& s) {
  if (!out || !out_len) return;
  *out_len = static_cast<int>(s.size());
  if (*out_len == 0) {
    *out = nullptr;
    return;
  }
  char* buf = static_cast<char*>(std::malloc(static_cast<size_t>(*out_len)));
  if (!buf) {
    *out = nullptr;
    *out_len = 0;
    return;
  }
  std::memcpy(buf, s.data(), static_cast<size_t>(*out_len));
  *out = buf;
}

int surf_run_graphml(
  const char* graphml,
  const char* skoffset,
  int component,
  int write_ipe,
  char** out,
  int* out_len,
  char** err,
  int* err_len
) {
  if (out) *out = nullptr;
  if (out_len) *out_len = 0;
  if (err) *err = nullptr;
  if (err_len) *err_len = 0;

  try {
    ensure_logging_initialized();

    const std::string input = graphml ? std::string(graphml) : std::string();
    const std::string offset = skoffset ? std::string(skoffset) : std::string();

    std::istringstream is(input);
    std::ostringstream os;

    do_surf(is, os, component, offset, write_ipe != 0);
    set_out(out, out_len, os.str());
    return 0;
  } catch (const SurfError& e) {
    set_out(err, err_len, e.what());
    return e.code != 0 ? e.code : 1;
  } catch (const std::exception& e) {
    set_out(err, err_len, e.what());
    return 1;
  } catch (...) {
    set_out(err, err_len, "Unknown error");
    return 1;
  }
}

void surf_free(void* p) {
  if (p) {
    std::free(p);
  }
}
