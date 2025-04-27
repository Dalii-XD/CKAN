using System;
using System.IO;

using log4net;

using CKAN.Extensions;

namespace CKAN.IO
{
    public static class CKANPathUtils
    {
        /// <summary>
        /// Path to save CKAN data shared across all game instances
        /// </summary>
        public static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Meta.ProductName);

        private static readonly ILog log = LogManager.GetLogger(typeof(CKANPathUtils));

        /// <summary>
        /// Normalizes the path by replacing all \ with / and removing any trailing slash.
        /// </summary>
        /// <param name="path">The path to normalize</param>
        /// <returns>The normalized path</returns>
        public static string NormalizePath(string path)
            => path.Length < 2 ? path.Replace('\\', '/')
                               : path.Replace('\\', '/').TrimEnd('/');

        /// <summary>
        /// Converts a path to one relative to the root provided.
        /// Please use KSP.ToRelative when working with gamedirs.
        /// Throws a PathErrorKraken if the path is not absolute, not inside the root,
        /// or either argument is null.
        /// </summary>
        public static string ToRelative(string path, string root)
        {
            // We have to normalise before we check for rootedness,
            // otherwise backslash separators fail on Linux.

            path = NormalizePath(path);
            root = NormalizePath(root);

            if (!Path.IsPathRooted(path))
            {
                throw new PathErrorKraken(path, string.Format(
                    Properties.Resources.PathUtilsNotAbsolute, path));
            }

            if (!path.StartsWith(root, Platform.PathComparison))
            {
                throw new PathErrorKraken(path, string.Format(
                    Properties.Resources.PathUtilsNotInside, path, root));
            }

            // Strip off the root, then remove any slashes at the beginning
            return path[root.Length..].TrimStart('/');
        }

        /// <summary>
        /// Returns root/path, but checks that root is absolute,
        /// path is relative, and normalises everything for great justice.
        /// Please use KSP.ToAbsolute if converting from a KSP gamedir.
        /// Throws a PathErrorKraken if anything goes wrong.
        /// </summary>
        public static string ToAbsolute(string path, string root)
        {
            path = NormalizePath(path);
            root = NormalizePath(root);

            if (Path.IsPathRooted(path))
            {
                throw new PathErrorKraken(
                    path,
                    string.Format(Properties.Resources.PathUtilsAlreadyAbsolute, path)
                );
            }

            if (!Path.IsPathRooted(root))
            {
                throw new PathErrorKraken(
                    root,
                    string.Format(Properties.Resources.PathUtilsNotRoot, root)
                );
            }

            // Why normalise it AGAIN? Because Path.Combine can insert
            // the un-prettiest slashes.
            return NormalizePath(Path.Combine(root, path));
        }

        public static void CheckFreeSpace(DirectoryInfo where,
                                          long          bytesToStore,
                                          string        errorDescription)
        {
            if (bytesToStore > 0
                && where.GetDrive()?.AvailableFreeSpace is long bytesFree)
            {
                if (bytesToStore > bytesFree) {
                    throw new NotEnoughSpaceKraken(errorDescription, where,
                                                   bytesFree, bytesToStore);
                }
                log.DebugFormat("Storing {0} to {1} ({2} free)...",
                                CkanModule.FmtSize(bytesToStore),
                                where.FullName,
                                CkanModule.FmtSize(bytesFree));
            }
        }

        public static bool PathEquals(this FileSystemInfo a,
                                      FileSystemInfo      b)
            => NormalizePath(a.FullName).Equals(NormalizePath(b.FullName),
                                                Platform.PathComparison);

    }
}
