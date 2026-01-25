#include "surf_c_api.h"

#include "SkeletonStructure.h"
#include "tools.h"

#include <sstream>
#include <string>
#include <cstring>
#include <cstdlib>
#include <vector>
#include <iomanip>

#ifdef _WIN32
#  include <io.h>
#  include <fcntl.h>
#else
#  include <sys/resource.h>
#endif

#include <boost/iostreams/device/file_descriptor.hpp>
#include <boost/iostreams/stream.hpp>

static void ensure_logging_initialized() {
  static bool initialized = false;
  if (initialized) return;

  const char* argv0 = "surfer2";
  char* argv[] = { const_cast<char*>(argv0) };
  int argc = 1;
  setup_logging(argc, argv, true);
  initialized = true;
}

static void do_surf(std::istream& is, std::ostream& os, int restrict_component, const std::string& skoffset, bool write_ipe, std::string* stats_out = nullptr) {
  clock_t stage_00 = clock();

  BGLGraph graph = BGLGraph::create_from_graphml(is);
  clock_t stage_01 = clock();
  SkeletonStructure s = SkeletonStructure(BasicInputFromBGL(graph));
  clock_t stage_02 = clock();
  s.initialize(restrict_component);
  clock_t stage_03 = clock();
  s.wp.advance_to_end();

  clock_t stage_04 = clock();
  clock_t stage_99 = clock();

  if (write_ipe) {
    s.get_skeleton().write_ipe(os, skoffset);
  } else {
    s.get_skeleton().write_obj(os);
  }

  if (stats_out) {
    std::ostringstream stats_os;
    stats_os << std::setprecision(10);
    stats_os << "[SURF] VERSION                "  << VERSIONGIT;
    stats_os << "-" CMAKE_BUILD_TYPE;
    #ifdef NT_USE_DOUBLE
    stats_os << "-NT_USE-DOUBLE";
    #endif
    #ifndef REFINE_TRIANGULATION
    stats_os << "-norefine";
    #endif
    stats_os << std::endl;
    stats_os << "[SURF] INPUT_SIZE             "  << boost::num_vertices(graph) << std::endl;
    stats_os << "[SURF] CPUTIME_PARSEML        "  << ((double) (stage_01-stage_00))/CLOCKS_PER_SEC << std::endl;
    stats_os << "[SURF] CPUTIME_SETUP          "  << ((double) (stage_02-stage_01))/CLOCKS_PER_SEC << std::endl;
    stats_os << "[SURF] CPUTIME_INITKT         "  << ((double) (stage_03-stage_02))/CLOCKS_PER_SEC << std::endl;
    stats_os << "[SURF] CPUTIME_RUN            "  << ((double) (stage_04-stage_03))/CLOCKS_PER_SEC << std::endl;
    stats_os << "[SURF] CPUTIME_TOTAL          "  << ((double) (stage_99-stage_00))/CLOCKS_PER_SEC << std::endl;
    stats_os << "[SURF] CPUTIME_TOTAL_EX_PARSE "  << ((double) (stage_99-stage_01))/CLOCKS_PER_SEC << std::endl;

#ifndef _WIN32
    struct rusage usage;
    if (getrusage(RUSAGE_SELF, &usage) < 0) {
      LOG(ERROR) << "getrusage() failed: " << strerror(errno);
      SURF_ABORT(1, "getrusage failed.");
    }
    stats_os << "[SURF] MAXRSS                 "  << usage.ru_maxrss << std::endl;
#endif

    stats_os << "[SURF] NUM_EVENTS                                          "  << s.get_kt().event_type_counter[int(CollapseType::UNDEFINED)] << std::endl;
    stats_os << "[SURF] NUM_EVENTS_FACE_HAS_INFINITELY_FAST_VERTEX_OPPOSING "  << s.get_kt().event_type_counter[int(CollapseType::FACE_HAS_INFINITELY_FAST_VERTEX_OPPOSING)] << std::endl;
    stats_os << "[SURF] NUM_EVENTS_FACE_HAS_INFINITELY_FAST_VERTEX_WEIGHTED "  << s.get_kt().event_type_counter[int(CollapseType::FACE_HAS_INFINITELY_FAST_VERTEX_WEIGHTED)] << std::endl;
    stats_os << "[SURF] NUM_EVENTS_TRIANGLE_COLLAPSE                        "  << s.get_kt().event_type_counter[int(CollapseType::TRIANGLE_COLLAPSE)] << std::endl;
    stats_os << "[SURF] NUM_EVENTS_CONSTRAINT_COLLAPSE                      "  << s.get_kt().event_type_counter[int(CollapseType::CONSTRAINT_COLLAPSE)] << std::endl;
    stats_os << "[SURF] NUM_EVENTS_SPOKE_COLLAPSE                           "  << s.get_kt().event_type_counter[int(CollapseType::SPOKE_COLLAPSE)] << std::endl;
    stats_os << "[SURF] NUM_EVENTS_SPLIT_OR_FLIP_REFINE                     "  << s.get_kt().event_type_counter[int(CollapseType::SPLIT_OR_FLIP_REFINE)] << std::endl;
    stats_os << "[SURF] NUM_EVENTS_VERTEX_MOVES_OVER_SPOKE                  "  << s.get_kt().event_type_counter[int(CollapseType::VERTEX_MOVES_OVER_SPOKE)] << std::endl;
    stats_os << "[SURF] NUM_EVENTS_CCW_VERTEX_LEAVES_CH                     "  << s.get_kt().event_type_counter[int(CollapseType::CCW_VERTEX_LEAVES_CH)] << std::endl;

    stats_os << "[SURF] TRIANGLES_PER_EDGE_EVENT_MAX                        "  << s.get_kt().max_triangles_per_edge_event << std::endl;
    stats_os << "[SURF] TRIANGLES_PER_EDGE_EVENT_SUM                        "  << s.get_kt().avg_triangles_per_edge_event_sum << std::endl;
    stats_os << "[SURF] TRIANGLES_PER_EDGE_EVENT_CTR                        "  << s.get_kt().avg_triangles_per_edge_event_ctr << std::endl;
    stats_os << "[SURF] TRIANGLES_PER_SPLIT_EVENT_MAX                       "  << s.get_kt().max_triangles_per_split_event << std::endl;
    stats_os << "[SURF] TRIANGLES_PER_SPLIT_EVENT_SUM                       "  << s.get_kt().avg_triangles_per_split_event_sum << std::endl;
    stats_os << "[SURF] TRIANGLES_PER_SPLIT_EVENT_CTR                       "  << s.get_kt().avg_triangles_per_split_event_ctr << std::endl;

    stats_os << "[SURF] EVENTS_PER_TIME_MAX                                 "  << s.get_kt().max_events_per_time << std::endl;
    stats_os << "[SURF] EVENTS_PER_TIME_SUM                                 "  << s.get_kt().avg_events_per_time_sum << std::endl;
    stats_os << "[SURF] EVENTS_PER_TIME_CTR                                 "  << s.get_kt().avg_events_per_time_ctr << std::endl;
    *stats_out = stats_os.str();
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

int surf_run_graphml_with_stats(
  const char* graphml,
  const char* skoffset,
  int component,
  int write_ipe,
  char** out,
  int* out_len,
  char** stats,
  int* stats_len,
  char** err,
  int* err_len
) {
  if (stats) *stats = nullptr;
  if (stats_len) *stats_len = 0;

  try {
    ensure_logging_initialized();

    const std::string input = graphml ? std::string(graphml) : std::string();
    const std::string offset = skoffset ? std::string(skoffset) : std::string();

    std::istringstream is(input);
    std::ostringstream os;

    std::string stats_text;
    do_surf(is, os, component, offset, write_ipe != 0, &stats_text);
    if (!stats_text.empty()) {
      set_out(stats, stats_len, stats_text);
    }

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
