using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JoshuaKearney.FileSystem {

    /// <summary>
    /// Provides methods to create and copy files and directorys into a specified directory
    /// </summary>
    public sealed class DirectoryBuilder : IDisposable {
        private List<StoragePath> directorysToBuild = new List<StoragePath>();
        private List<Tuple<StoragePath, StoragePath>> existingdirectorys = new List<Tuple<StoragePath, StoragePath>>();
        private List<PotentialFile> filesToBuild = new List<PotentialFile>();
        private List<ZipArchive> zips = new List<ZipArchive>();
        private List<StoragePath> deletions = new List<StoragePath>();

        /// <summary>
        /// The method of resolving file naming conflicts. The default is Rename
        /// </summary>
        public NameConflictOption ConflictResolution { get; set; }

        // Properties
        /// <summary>
        /// The target directory of this DirectoryBuilder
        /// </summary>
        public StoragePath RootDirectory { get; } = StoragePath.CurrentDirectory;

        /// <summary>
        /// Creates a new DirectoryBuilder that will place items in the specified directory
        /// </summary>
        /// <param name="directory">The target directory</param>
        public DirectoryBuilder(string directory, NameConflictOption conflictResolution = NameConflictOption.ThrowException) : this(new StoragePath(directory), NameConflictOption.ThrowException) {
        }

        /// <summary>
        /// Creates a new DirectoryBuilder that will place items in the specified directory
        /// and uses the specified conflict resolution
        /// </summary>
        /// <param name="directory">The target directory</param>
        /// <param name="conflictResolution">The method that will be used to resolve naming conflicts</param>
        public DirectoryBuilder(StoragePath directory, NameConflictOption conflictResolution = NameConflictOption.ThrowException) {
            this.RootDirectory = directory;
            this.ConflictResolution = conflictResolution;
        }

        ~DirectoryBuilder() {
            Dispose();
        }

        /// <summary>
        /// Places a new directory into the target directory
        /// Example relative paths include "/directory", "/directory/other", and "/directory/other/bar/"
        /// </summary>
        /// <param name="relativePath">The relative path to the new directory, starting in the DirectoryBuilder's target directory</param>
        public DirectoryBuilder AppendDirectory(StoragePath relativePath) {
            if (relativePath.IsAbsolute) {
                throw new IOException("The path you specified is not a relative path");
            }

            this.directorysToBuild.Add(relativePath);
            return this;
        }

        /// <summary>
        /// Places a new directory into the target directory
        /// Example relative paths include "/directory", "/directory/other", and "/directory/other/bar/"
        /// </summary>
        /// <param name="relativePath">The relative path to the new directory, starting in the DirectoryBuilder's target directory</param>
        public DirectoryBuilder AppendDirectory(string relativePath) => this.AppendDirectory(new StoragePath(relativePath));

        /// <summary>
        /// Copies an existing file or directory at the specified absolute path into the target directory
        /// </summary>
        /// <param name="absolutePath">The location of the existing file or directory on the disk</param>
        /// <returns></returns>
        public DirectoryBuilder AppendExisting(StoragePath relativePath, StoragePath absolutePath) {
            if (!absolutePath.IsAbsolute) {
                throw new IOException("The path you specified is not an absolute path");
            }

            string path = absolutePath.ToString();
            if (Directory.Exists(path)) {
                this.existingdirectorys.Add(Tuple.Create(relativePath, absolutePath));
            }
            else {
                if (File.Exists(path)) {
                    this.AppendFile(relativePath, () => new FileStream(absolutePath.ToString(), FileMode.Open));
                }
                else {
                    throw new FileNotFoundException();
                }
            }

            return this;
        }

        /// <summary>
        /// Copies an existing file or directory at the specified absolute path into the target directory
        /// </summary>
        /// <param name="absolutePath">The location of the existing file or directory on the disk</param>
        public DirectoryBuilder AppendExisting(string relativePath, string absolutePath) => this.AppendExisting(new StoragePath(relativePath), new StoragePath(absolutePath));

        /// <summary>
        /// Places a new file with the specified relative path into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        public DirectoryBuilder AppendFile(StoragePath relativePath) {
            return this.AppendFile(relativePath, string.Empty);
        }

        /// <summary>
        /// Places a new file with the specified relative path and contents into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">The string contents to place into the new file</param>
        public DirectoryBuilder AppendFile(StoragePath relativePath, string contents) {
            return this.AppendFile(relativePath, contents, Encoding.UTF8);
        }

        /// <summary>
        /// Places a new file with the specified relative path, contents, and text encoding into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">The string contents to place into the new file</param>
        /// <param name="encoding">The encoding to use when saving the string to the file</param>
        public DirectoryBuilder AppendFile(StoragePath relativePath, string contents, Encoding encoding) {
            Validate.NonNull(encoding, nameof(encoding));
            return this.AppendFile(relativePath, encoding.GetBytes(contents));
        }

        /// <summary>
        /// Places a new file with the specified relative path into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">The byte content to place into the file</param>
        public DirectoryBuilder AppendFile(StoragePath relativePath, byte[] contents) {
            if (relativePath.IsAbsolute) {
                throw new IOException("The path you specified is not a relative path");
            }

            this.filesToBuild.Add(new PotentialFile(relativePath, new Lazy<Stream>(() => new MemoryStream(contents))));
            return this;
        }

        /// <summary>
        /// Places a new file with the specified relative path into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">A <see cref="Func{Stream}"/> that returns a stream to the file contents</param>
        public DirectoryBuilder AppendFile(StoragePath relativePath, Func<Stream> contents) {
            if (relativePath.IsAbsolute) {
                throw new IOException("The path you specified is not a relative path");
            }

            this.filesToBuild.Add(new PotentialFile(relativePath, new Lazy<Stream>(contents)));
            return this;
        }

        /// <summary>
        /// Places a new file with the specified relative path into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        public DirectoryBuilder AppendFile(string relativePath) => this.AppendFile(relativePath, string.Empty);

        /// <summary>
        /// Places a new file with the specified relative path and contents into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">The string contents to place into the new file</param>
        public DirectoryBuilder AppendFile(string relativePath, string contents) => this.AppendFile(relativePath, contents, Encoding.UTF8);

        /// <summary>
        /// Places a new file with the specified relative path, contents, and text encoding into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">The string contents to place into the new file</param>
        /// <param name="encoding">The encoding to use when saving the string to the file</param>
        public DirectoryBuilder AppendFile(string relativePath, string contents, Encoding encoding) => this.AppendFile(relativePath, encoding.GetBytes(contents));

        /// <summary>
        /// Places a new file with the specified relative path into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">The byte content to place into the file</param>
        public DirectoryBuilder AppendFile(string relativePath, byte[] contents) => this.AppendFile(new StoragePath(relativePath), contents);

        /// <summary>
        /// Places a new file with the specified relative path into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">A <see cref="Func{Stream}"/> that returns a stream to the file contents</param>
        public DirectoryBuilder AppendFile(string relativePath, Func<Stream> contents) => this.AppendFile(new StoragePath(relativePath), contents);

        public DirectoryBuilder Delete(StoragePath relativePath) {
            this.deletions.Add(relativePath);
            return this;
        }

        public DirectoryBuilder Delete(string relativePath) => this.Delete(new StoragePath(relativePath));

        /// <summary>
        /// Extracts the contents of the specified archive to the output directory
        /// </summary>
        /// <param name="zip">The zip archive to extract</param>
        public DirectoryBuilder AppendZipContents(ZipArchive zip) {
            this.zips.Add(zip);

            return this;
        }

        /// <summary>
        /// Extracts the contents of the specified archive to the output directory
        /// </summary>
        /// <param name="zip">The path of the zip archive to extract</param>
        public DirectoryBuilder AppendZipContents(string zip) {
            ZipArchive arc = ZipFile.OpenRead(zip);
            return this.AppendZipContents(arc);
        }

        /// <summary>
        /// Runs all operations on the target directory asynchronously
        /// </summary>
        public async Task BuildAsync() {
            if (!Directory.Exists(this.RootDirectory.ToString())) {
                Directory.CreateDirectory(this.RootDirectory.ToString());
            }

            // Delete files and folders
            foreach(var item in this.deletions) {
                if (File.Exists(item.ToString())) {
                    File.Delete(item.ToString());
                }
                else {
                    this.DeleteDirectoryCore(item, new StoragePath());
                }
            }

            // Build existing directories
            foreach (var item in this.existingdirectorys) {
                AppendDirectoryCore(item.Item2, item.Item1);
            }
            this.existingdirectorys.Clear();

            // Read all zip files
            foreach (var item in this.zips) {
                using (item) {
                    foreach (var entry in item.Entries) {
                        var stream = entry.Open();
                        this.AppendFile(entry.FullName, () => stream);
                    }
                }
            }
            this.zips.Clear();

            // Build directories
            foreach (var item in this.directorysToBuild) {
                string dir = this.RootDirectory.Combine(item).ToString();

                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
            };
            this.directorysToBuild.Clear();

            // Build files
            foreach (var item in this.filesToBuild) {
                async Task WriteFile(string p, FileMode mode) {
                    using (FileStream writer = new FileStream(p, mode)) {
                        using (var reader = item.Stream) {
                            await reader.CopyToAsync(writer);
                        }

                        await writer.FlushAsync();
                    }
                }

                StoragePath path = this.RootDirectory + item.Path;
                string dir = path.ParentDirectory.ToString();

                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }

                bool targetExists = File.Exists(path.ToString());
                if (this.ConflictResolution == NameConflictOption.ThrowException && targetExists) {
                    throw new IOException($"The file '{Path.GetFileName(path.ToString())}' already exists");
                }
                else if (this.ConflictResolution == NameConflictOption.Skip && targetExists) {
                    continue;
                }
                else if (this.ConflictResolution == NameConflictOption.Rename && targetExists) {
                    await WriteFile(ResolveFileName(path.ToString()), FileMode.Create);
                }
                else {
                    await WriteFile(path.ToString(), FileMode.Create);
                }
            };
            this.filesToBuild.Clear();
        }

        /// <summary>
        /// Runs all operations on the target directory synchronously
        /// </summary>
        public void Build() {
            this.BuildAsync().GetAwaiter().GetResult();
        }

        private static string ResolveFileName(string path) {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (new Regex(@"\s\([0-9]+\)$").IsMatch(fileName)) {
                fileName = fileName.Substring(0, fileName.LastIndexOf(" "));
            }

            string baseName = Path.Combine(Path.GetDirectoryName(path), fileName + " ");
            int counter = 1;
            string ext = Path.GetExtension(path);

            if (!File.Exists(path)) {
                return path;
            }
            else {
                while (File.Exists(baseName + "(" + counter + ")" + ext)) {
                    counter++;
                }

                return baseName + "(" + counter + ")" + ext;
            }
        }

        private void AppendDirectoryCore(StoragePath source, StoragePath level) {
            // Copy each file into the new directory.
            foreach (var file in Directory.EnumerateFiles(source.ToString()).Select(x => new StoragePath(x))) {
                this.AppendExisting(level + file.Name, file);
            }

            // Copy each subdirectory using recursion.
            foreach (var sub in Directory.EnumerateDirectories(source.ToString()).Select(x => new StoragePath(x))) {
                this.AppendDirectory(level + sub.Name);
                AppendDirectoryCore(sub, level + sub.Name);
            }
        }

        private void DeleteDirectoryCore(StoragePath source, StoragePath level) {
            // Copy each file into the new directory.
            foreach (string file in Directory.EnumerateFiles(source.ToString())) {
                File.Delete(file);
            }

            // Delete each subdirectory using recursion.
            foreach (var sub in Directory.EnumerateDirectories(source.ToString()).Select(x => new StoragePath(x))) {
                DeleteDirectoryCore(sub, level + sub.Name);
            }

            Directory.Delete(source.ToString());
        }

        public void Dispose() {
            foreach (var zip in this.zips) {
                zip.Dispose();
            }

            this.directorysToBuild = new List<StoragePath>();
            this.existingdirectorys = new List<Tuple<StoragePath, StoragePath>>();
            this.filesToBuild = new List<PotentialFile>();
            this.zips = new List<ZipArchive>();
            this.deletions = new List<StoragePath>();
        }

        private class PotentialFile {
            private Lazy<Stream> stream;

            public Stream Stream => this.stream.Value;

            public StoragePath Path { get; }

            public PotentialFile(StoragePath path, Lazy<Stream> stream) {
                this.stream = stream;
                this.Path = path;
            }
        }
    }
}