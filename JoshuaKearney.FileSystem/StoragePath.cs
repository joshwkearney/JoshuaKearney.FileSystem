using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JoshuaKearney.FileSystem {

    /// <summary>
    /// Provides an instance wrapper for System.IO.Path that is has path simplification, character
    /// checking, normalization, operator overloading, ect. and all of the utilites from System.IO.Path
    /// </summary>
    public class StoragePath : IEquatable<StoragePath> {
        private List<string> segments = new List<string>();

        /// <summary>
        /// Gets an array containing the characters not allowed in paths
        /// </summary>
        public static IEnumerable<char> InvalidPathCharacters { get; } = Path.GetInvalidPathChars();

        /// <summary>
        /// Gets an array containing the characters not allowed in file names
        /// </summary>
        public static IEnumerable<char> InvalidFileNameCharacters { get; } = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Creates a StoragePath from the specified Uri
        /// </summary>
        public StoragePath(Uri uri) : this(uri.ToString()) {
        }

        /// <summary>
        /// Creates a StoragePath from the specified path segments
        /// </summary>
        /// <param name="segments"></param>
        public StoragePath(params string[] segments) {
            this.CombineCore(segments);
        }

        /// <summary>
        /// Gets the current extension of the last segment (including the dot), or returns string.Empty if there is not an extension
        /// </summary>
        public string Extension {
            get {
                if (this.HasExtension) {
                    return Path.GetExtension(this.segments.Last());
                }
                else {
                    return string.Empty;
                }
            }
        }

        public bool HasExtension {
            get {
                return this.segments.LastOrDefault()?.Contains(".") ?? false;
            }
        }

        /// <summary>
        /// Return a value on whether or not this path is absolute by determining if the first segment is a drive letter
        /// </summary>
        public bool IsAbsolute {
            get {
                return this.segments.FirstOrDefault()?.Contains(":") ?? false;
            }
        }

        /// <summary>
        /// Get the file or directory name of the path by retrieving the last path segment
        /// </summary>
        public string Name {
            get {
                return this.segments.LastOrDefault() ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the file name of the path by retriveing the last path segment without the extension
        /// </summary>
        public string NameWithoutExtension {
            get {
                return Path.GetFileNameWithoutExtension(this.Name);
            }
        }

        /// <summary>
        /// Returns a storage path with the value of the parent or containing directory
        /// </summary>
        public StoragePath ParentDirectory {
            get {
                return this.GetNthParentDirectory(1);
            }
        }

        /// <summary>
        /// Gets the current segments of this path
        /// </summary>
        public IReadOnlyList<string> Segments {
            get {
                return this.segments;
            }
        }

        /// <summary>
        /// Implicitly converts the givin Storage path to a System.Uri
        /// </summary>
        public static implicit operator Uri(StoragePath path) {
            return path.ToUri();
        }

        /// <summary>
        /// Explicitly converts the givin Uri to a StoragePaths
        /// </summary>
        public static explicit operator StoragePath(Uri uri) {
            return new StoragePath(uri);
        }

        /// <summary>
        /// Adds the second StoragePath to the end of the first. Equivalent to path1.Combine(path2)
        /// </summary>
        /// <param name="path1">The base path</param>
        /// <param name="path2">The path to add</param>
        /// <returns>A new StoragePath that represents the second appended to the first</returns>
        public static StoragePath operator +(StoragePath path1, StoragePath path2) {
            return path1.Combine(path2);
        }

        /// <summary>
        /// Adds the second segment to the end of the first. Equivalent to path1.Combine(path2)
        /// </summary>
        /// <param name="path1">The base path</param>
        /// <param name="path2">The path to add</param>
        /// <returns>A new StoragePath that represents the second appended to the first</returns>
        public static StoragePath operator +(StoragePath path1, string path2) {
            return path1.Combine(path2);
        }

        /// <summary>
        /// Adds the second StoragePath to the end of the first segment. Equivalent to new StoragePath(path1).Combine(path2)
        /// </summary>
        /// <param name="path1">The base path</param>
        /// <param name="path2">The path to add</param>
        /// <returns>A new StoragePath that represents the second appended to the first</returns>
        public static StoragePath operator +(string path1, StoragePath path2) {
            return new StoragePath(path1).Combine(path2);
        }

        /// <summary>
        /// Determines if one StoragePath is equal to another
        /// </summary>
        public static bool operator ==(StoragePath path1, StoragePath path2) {
            return path1.Equals(path2);
        }

        /// <summary>
        /// Determines if one Storage path is not equal to another
        /// </summary>
        public static bool operator !=(StoragePath path1, StoragePath path2) {
            return !path1.Equals(path2);
        }

        /// <summary>
        /// Splits the given fragments on valid path separators, and then appends the resulting path fragments to the end of the current path
        /// </summary>
        /// <param name="fragments">The path fragments to combine</param>
        /// <exception cref="ArgumentNullException"></exception>
        public StoragePath Combine(params string[] fragments) {
            if (fragments == null) {
                throw new ArgumentNullException();
            }

            if (fragments.Any(x => InvalidPathCharacters.Any(y => x.Contains(y)))) {
                throw new ArgumentException("Argument 'fragments' contains invalid characters");
            }

            StoragePath ret = this.ShallowClone();
            ret.CombineCore(fragments);

            return ret;
        }

        /// <summary>
        /// Splits the given fragment on valid path separators, and then appends the resulting path fragment to the end of the current path
        /// </summary>
        /// <param name="fragments">The path fragments to combine</param>
        /// <exception cref="ArgumentNullException"></exception>
        public StoragePath Combine(string fragment) {
            if (fragment == null) {
                throw new ArgumentNullException();
            }

            if (Path.GetInvalidPathChars().Any(x => fragment.Contains(x))) {
                throw new ArgumentException("Argument 'fragment' contains invalid characters");
            }

            return this.Combine(new[] { fragment });
        }

        /// <summary>
        /// Appends the specified StoragePath to the end of the current path. Throws a System.ArgumentException if the specified StoragePath
        /// is an absolute path
        /// </summary>
        /// <param name="other">The StoragePath to append</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public StoragePath Combine(StoragePath other) {
            if (other == null) {
                throw new ArgumentNullException();
            }

            if (other.IsAbsolute) {
                throw new ArgumentException("Cannot combine an absolute path");
            }

            StoragePath ret = this.ShallowClone();
            ret.segments.AddRange(other.segments);

            return ret;
        }

        /// <summary>
        /// Sets or adds the spedified extension of the current path
        /// </summary>
        /// <param name="extension">The extension to set</param>
        public StoragePath SetExtension(string extension) {
            if (extension == null) {
                throw new ArgumentNullException();
            }

            if (InvalidFileNameCharacters.Any(x => extension.Contains(x))) {
                throw new ArgumentException("Argument 'extension' contains invalid characters");
            }

            StoragePath p = this.ShallowClone();

            if (p.segments.Count > 0) {
                p.segments[this.segments.Count - 1] = Path.ChangeExtension(this.segments.Last(), extension);
                return p;
            }
            else {
                p.segments.Add(extension);
                return p;
            }
        }

        public bool Equals(StoragePath other) {
            if (this.segments.Count != other.segments.Count) {
                return false;
            }
            else {
                string thisStr = this.ToString(PathSeparator.BackSlash, false, false).ToLowerInvariant();
                string otherStr = other.ToString(PathSeparator.BackSlash, false, false).ToLowerInvariant();

                return thisStr == otherStr;
            }
        }

        public override bool Equals(object obj) {
            StoragePath path = obj as StoragePath;

            if (path == null) {
                return false;
            }
            else {
                return this.Equals(path);
            }
        }

        public override int GetHashCode() {
            return this.ToString().ToLower().GetHashCode();
        }

        /// <summary>
        /// Gets the nth parent directory of the current path by removeing n segments from the end
        /// </summary>
        /// <param name="nthParent">The nth parent directory to get</param>
        public StoragePath GetNthParentDirectory(int nthParent) {
            StoragePath p = this.ShallowClone();

            if (nthParent < 0) {
                throw new ArgumentException($"Cannot remove less than 0 levels");
            }

            if (this.IsAbsolute && nthParent >= this.segments.Count) {
                throw new InvalidOperationException("Cannot remove levels past the drive letter on an absolute path");
            }

            for (int x = 0; x < nthParent; x++) {
                p = p.Combine("..");
            }

            return p;
        }

        /// <summary>
        /// Gets a StoragePath with the value of this.Name
        /// </summary>
        /// <returns></returns>
        public StoragePath ScopeToName() {
            return new StoragePath(this.Name);
        }

        /// <summary>
        /// Gets a StoragePath with the value of this.NameWithoutPath
        /// </summary>
        public StoragePath ScopeToNameWithoutExtension() {
            return new StoragePath(this.NameWithoutExtension);
        }

        /// <summary>
        /// Returns a value indicating whether or not a file exists at the current path
        /// </summary>
        public bool FileExists() {
            return File.Exists(this.ToString());
        }

        /// <summary>
        /// Returns a value indicating whether or not a file exists at the current path
        /// </summary>
        public bool DirectoryExists() {
            return Directory.Exists(this.ToString());
        }

        /// <summary>
        /// Returns a string representation of the current path
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return this.ToString(PathSeparator.BackSlash);
        }

        /// <summary>
        /// Returns a string representation of the current path using the specified separator,
        /// adding a leading slash if specified and if the current path is not an absolute
        /// path, and a trailing slash if specified
        /// </summary>
        /// <param name="leadingSlash">Whether or not a slash should be prepended to the path</param>
        /// <param name="trailingSlash">Whether or not a slash should be appended to the path</param>
        public string ToString(PathSeparator slashType, bool leadingSlash = false, bool trailingSlash = false) {
            string ret = string.Join(slashType.GetCharacter().ToString(), this.segments);

            if (leadingSlash && !this.IsAbsolute) {
                ret = slashType.GetCharacter() + ret;
            }

            if (trailingSlash) {
                ret += slashType.GetCharacter();
            }

            return ret;
        }

        /// <summary>
        /// Converts the current StoragePath to a System.Uri
        /// </summary>
        public Uri ToUri() {
            return new Uri(this.ToString(), UriKind.RelativeOrAbsolute);
        }

        private void CombineCore(string fragment) {
            if (fragment == ".." && this.segments.Count > 0 && this.segments.Last() != "..") {
                this.segments.RemoveAt(this.segments.Count - 1);
            }
            else {
                if (this.segments.Count == 0) {
                    if (Path.GetInvalidPathChars().Any(x => fragment.Contains(x.ToString()))) {
                        throw new InvalidOperationException("This path contains invalid characters");
                    }
                }
                else {
                    if (Path.GetInvalidFileNameChars().Any(x => fragment.Contains(x.ToString()))) {
                        throw new InvalidOperationException("This path contains invalid characters");
                    }
                }

                this.segments.Add(fragment);
            }
        }

        private void CombineCore(IEnumerable<string> segments) {
            foreach (string segment in segments) {
                var subs = segment
                    .Replace(PathSeparator.ForwardSlash.GetCharacter(), PathSeparator.BackSlash.GetCharacter())
                    .Split(new[] { PathSeparator.BackSlash.GetCharacter() })
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                foreach (string sub in subs) {
                    this.CombineCore(sub);
                }
            }
        }

        private StoragePath ShallowClone() {
            StoragePath ret = new StoragePath(this.segments.ToArray());

            return ret;
        }
    }
}