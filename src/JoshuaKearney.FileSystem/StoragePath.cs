using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace JoshuaKearney.FileSystem {

    /// <summary>
    /// Provides an instance wrapper for System.IO.Path that is has path simplification, character
    /// checking, normalization, operator overloading, ect, as well as all of the utilites from System.IO.Path
    /// </summary>
    public struct StoragePath : IEquatable<StoragePath> {
        private readonly IEnumerable<string> segments;

        /// <summary>
        /// Creates a StoragePath from the specified Uri
        /// </summary>
        public StoragePath(Uri uri) : this(uri.ToString()) {
            Validate.NonNull(uri, nameof(uri));
        }

        /// <summary>
        /// Creates a StoragePath from the specified path Segments
        /// </summary>
        public StoragePath(params string[] Segments) : this((IEnumerable<string>)Segments) {
        }

        /// <summary>
        /// Creates a StoragePath from the specified path Segments
        /// </summary>
        /// <param name="segments"></param>
        public StoragePath(IEnumerable<string> segments) : this() {
            segments = segments ?? Enumerable.Empty<string>();

            List<string> toBuild = new List<string>();

            foreach (string fragment in
                segments
                .Select(x => x)
                .SelectMany(x =>
                    x
                    .Replace(PathSeparator.ForwardSlash.GetCharacter(), PathSeparator.BackSlash.GetCharacter())
                    .Split(new[] { PathSeparator.BackSlash.GetCharacter() })
                )
                .Where(y => !string.IsNullOrWhiteSpace(y))
            ) {
                if (fragment == ".." && toBuild.Count >= 1 && toBuild.Last() != "..") {
                    toBuild.RemoveAt(toBuild.Count - 1);
                }
                else {
                    if (toBuild.Count == 0) {
                        if (Path.GetInvalidPathChars().Any(x => fragment.Contains(x.ToString()))) {
                            throw new InvalidOperationException("This path contains invalid characters");
                        }
                    }
                    else {
                        if (Path.GetInvalidFileNameChars().Any(x => fragment.Contains(x.ToString()))) {
                            throw new InvalidOperationException("This path contains invalid characters");
                        }
                    }

                    toBuild.Add(fragment);
                }
            }

            this.segments = toBuild;
        }

        /// <summary>
        /// Gets a StoragePath representing the current working directory of this application
        /// </summary>
        public static StoragePath CurrentDirectory { get; } = new StoragePath(Directory.GetCurrentDirectory());

        /// <summary>
        /// Gets an array containing the characters not allowed in file names
        /// </summary>
        public static IEnumerable<char> InvalidFileNameCharacters { get; } = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Gets an array containing the characters not allowed in paths
        /// </summary>
        public static IEnumerable<char> InvalidPathCharacters { get; } = Path.GetInvalidPathChars();

        /// <summary>
        /// Gets the current extension of the last segment (including the dot), or returns string.Empty if there is not an extension
        /// </summary>
        public string Extension {
            get {
                if (this.HasExtension) {
                    return Path.GetExtension(this.Segments.Last());
                }
                else {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Returns a boolean value that indicates whether this path ends with an extension
        /// </summary>
        public bool HasExtension => this.Segments.LastOrDefault()?.Contains(".") ?? false;

        /// <summary>
        /// Return a value on whether or not this path is absolute by determining if the first segment is a drive letter
        /// </summary>
        public bool IsAbsolute => this.Segments.FirstOrDefault()?.Contains(":") ?? false;

        /// <summary>
        /// Get the file or directory name of the path by retrieving the last path segment
        /// </summary>
        public string Name {
            get {
                return this.Segments.LastOrDefault() ?? string.Empty;
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
        public StoragePath ParentDirectory => this.GetNthParentDirectory(1);

        /// <summary>
        /// Gets the current Segments of this path
        /// </summary>
        public IEnumerable<string> Segments => this.segments ?? Enumerable.Empty<string>();

        /// <summary>
        /// Explicitly converts the givin Uri to a StoragePaths
        /// </summary>
        public static explicit operator StoragePath(Uri uri) {
            Validate.NonNull(uri, nameof(uri));
            return new StoragePath(uri);
        }

        /// <summary>
        /// Implicitly converts the givin Storage path to a System.Uri
        /// </summary>
        public static implicit operator Uri(StoragePath path) {
            return path.ToUri();
        }

        /// <summary>
        /// Determines if one Storage path is not equal to another
        /// </summary>
        public static bool operator !=(StoragePath path1, StoragePath path2) => !(path1 == path2);

        public static StoragePath operator +(StoragePath path1, StoragePath path2) {
            //Contract.Requires(!path2.IsAbsolute);
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
            //Contract.Requires(!path2.IsAbsolute);
            return new StoragePath(path1).Combine(path2);
        }

        /// <summary>
        /// Determines if one StoragePath is equal to another
        /// </summary>
        public static bool operator ==(StoragePath path1, StoragePath path2) => path1.Equals(path2);

        /// <summary>
        /// Splits the given fragments on valid path separators, and then appends the resulting path fragments to the end of the current path
        /// </summary>
        /// <param name="fragments">The path fragments to combine</param>
        public StoragePath Combine(params string[] fragments) => new StoragePath(fragments);

        /// <summary>
        /// Splits the given fragment on valid path separators, and then appends the resulting path fragment to the end of the current path
        /// </summary>
        /// <param name="fragments">The path fragments to combine</param>
        public StoragePath Combine(string fragment) {
            //Contract.Requires(!fragment.ContainsInvalidPathChars());
            return new StoragePath(this.Segments.Concat(new[] { fragment }));
        }

        /// <summary>
        /// Appends the specified StoragePath to the end of the current path. Throws a System.ArgumentException if the specified StoragePath
        /// is an absolute path
        /// </summary>
        /// <param name="other">The StoragePath to append</param>

        public StoragePath Combine(StoragePath other) {
            //Contract.Requires(!other.IsAbsolute);
            return new StoragePath(this.Segments.Concat(other.Segments));
        }

        public bool Equals(StoragePath other) => this.Segments.SequenceEqual(other.Segments);

        public override bool Equals(object obj) {
            StoragePath? path = obj as StoragePath?;

            if (object.ReferenceEquals(path, null)) {
                return false;
            }
            else {
                return this.Equals(path);
            }
        }

        public override int GetHashCode() => this.Segments.GetHashCode();

        /// <summary>
        /// Gets the nth parent directory of the current path by removeing n Segments from the end
        /// </summary>
        /// <param name="nthParent">The nth parent directory to get</param>
        public StoragePath GetNthParentDirectory(int nthParent) {
            Validate.NonNegative(nthParent, nameof(nthParent));

            if (this.IsAbsolute && nthParent >= this.Segments.Count()) {
                throw new InvalidOperationException("Attempted to remove too many segments from an absolute path");
            }

            string toAdd = string.Join(
                PathSeparator.BackSlash.GetCharacter().ToString(),
                Enumerable.Repeat("..", nthParent)
            );

            return this + toAdd;
        }

        /// <summary>
        /// Gets a StoragePath with the value of this.Name
        /// </summary>
        /// <returns></returns>
        public StoragePath ScopeToName() => new StoragePath(this.Name);

        /// <summary>
        /// Gets a StoragePath with the value of this.NameWithoutPath
        /// </summary>
        public StoragePath ScopeToNameWithoutExtension() => new StoragePath(this.NameWithoutExtension);

        /// <summary>
        /// Sets or adds the spedified extension of the current path
        /// </summary>
        /// <param name="extension">The extension to set</param>
        public StoragePath SetExtension(string extension) {
            if (extension == null) {
                return this;
            }

            if (!extension.StartsWith(".")) {
                extension = "." + extension;
            }

            return this.ParentDirectory + (this.NameWithoutExtension + extension);
        }

        /// <summary>
        /// Returns a string representation of the current path
        /// </summary>
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
            string ret = string.Join(slashType.GetCharacter().ToString(), this.Segments);

            if (leadingSlash && !this.IsAbsolute) {
                ret = slashType.GetCharacter() + ret;
            }

            if (trailingSlash) {
                ret += slashType.GetCharacter();
            }

            // Null coalesce for code contracts
            return ret ?? string.Empty;
        }

        /// <summary>
        /// Converts the current StoragePath to a System.Uri
        /// </summary>
        public Uri ToUri() {
            return new Uri(this.ToString(), UriKind.RelativeOrAbsolute);
        }
    }
}