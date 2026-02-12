#pragma once

/** comparisons, when both arguments are actually equal, are often expensive.
 * This enables or disables the more expensive asserts of this kind.
 */
/* #undef DEBUG_EXPENSIVE_PREDICATES */


/** refine triangulation to avoid flip events sooner.
 */
#define REFINE_TRIANGULATION


/** do or do not compile with debug output.
 */
/* #undef DEBUG_OUTPUT */

/** include filename and line numbers in log output.
 */
/* #undef DEBUG_OUTPUT_WITH_FILES */

///** Use two means to compute collapse times and event classification, and compare.
// *
// * only used in debug builds
// */
////#define DEBUG_ALL_COLLAPSE_TIMES_EXPENSIVE

/** Do quick checks for collapse time correctness/classification
 *
 * only used in debug builds
 */
/* #undef DEBUG_COLLAPSE_TIMES */


/** The cmake build type, if we have it.
 */
#define CMAKE_BUILD_TYPE "RELEASE"

/** Our copy of -DNDEBUG, if we have it.  Set via the cmake build type.
 */
#define SURF_NDEBUG

/** the version as supplied by CMake
 */
#define VERSIONGIT "1.99-c34c24ad-dirty"

/** Use double instead of CORE Expressions
 */
/* #undef NT_USE_DOUBLE */

/** Collect stats on heap stuff
 */
/* #undef HEAP_STATS */
