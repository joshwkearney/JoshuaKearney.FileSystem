using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JoshuaKearney.FileSystem {

    /// <summary>
    /// Provides methods to create and copy files and directorys into a specified directory
    /// </summary>
    public sealed partial class DirectoryBuilder {
        private List<Tuple<StoragePath, byte[]>> filesToBuild = new List<Tuple<StoragePath, byte[]>>();
        private List<StoragePath> directorysToBuild = new List<StoragePath>();
        private List<StoragePath> existingFiles = new List<StoragePath>();
        private List<StoragePath> existingdirectorys = new List<StoragePath>();
        private List<ZipArchive> zips = new List<ZipArchive>();

        /// <summary>
        /// Creates a new DirectoryBuilder that will place items in the specified directory
        /// </summary>
        /// <param name="directory">The target directory</param>
        public DirectoryBuilder(string directory, NameConflictOption conflictResolution = NameConflictOption.ThrowException) : this(new StoragePath(directory), NameConflictOption.ThrowException) {
        }

        /// <summary>
        /// Creates a new DirectoryBuilder that will place items in the specified directory
        /// </summary>
        /// <param name="directory">The target directory</param>
        public DirectoryBuilder(DirectoryInfo directory) : this(new StoragePath(directory.FullName)) {
        }

        /// <summary>
        /// Creates a new DirectoryBuilder that will place items in the specified directory
        /// and uses the specified conflict resolution
        /// </summary>
        /// <param name="directory">The target directory</param>
        /// <param name="conflictResolution">The method that will be used to resolve naming conflicts</param>
        public DirectoryBuilder(StoragePath directory, NameConflictOption conflictResolution = NameConflictOption.ThrowException) {
            RootDirectory = directory;
            this.ConflictResolution = conflictResolution;

            if (!Directory.Exists(this.RootDirectory.ToString())) {
                throw new DirectoryNotFoundException();
            }
        }

        // Properties
        /// <summary>
        /// The target directory of this DirectoryBuilder
        /// </summary>
        public StoragePath RootDirectory { get; private set; } = null;

        /// <summary>
        /// The method of resolving file naming conflicts. The default is Rename
        /// </summary>
        public NameConflictOption ConflictResolution { get; set; }

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

            filesToBuild.Add(Tuple.Create(relativePath, contents));
            return this;
        }

        /// <summary>
        /// Places a new file with the specified relative path into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        public DirectoryBuilder AppendFile(string relativePath) {
            return this.AppendFile(relativePath, string.Empty);
        }

        /// <summary>
        /// Places a new file with the specified relative path and contents into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">The string contents to place into the new file</param>
        public DirectoryBuilder AppendFile(string relativePath, string contents) {
            return this.AppendFile(relativePath, contents, Encoding.UTF8);
        }

        /// <summary>
        /// Places a new file with the specified relative path, contents, and text encoding into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">The string contents to place into the new file</param>
        /// <param name="encoding">The encoding to use when saving the string to the file</param>
        public DirectoryBuilder AppendFile(string relativePath, string contents, Encoding encoding) {
            return this.AppendFile(relativePath, encoding.GetBytes(contents));
        }

        /// <summary>
        /// Places a new file with the specified relative path into the target directory
        /// Example relative paths include "foo.txt", "/directory/foo.txt", and "/directory/other/foo.txt"
        /// </summary>
        /// <param name="relativePath">The relative path to the new file, starting in the DirectoryBuilder's target directory</param>
        /// <param name="contents">The byte content to place into the file</param>
        public DirectoryBuilder AppendFile(string relativePath, byte[] contents) {
            return this.AppendFile(new StoragePath(relativePath), contents);
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
        public DirectoryBuilder AppendDirectory(string relativePath) {
            return this.AppendDirectory(new StoragePath(relativePath));
        }

        /// <summary>
        /// Copies an existing file or directory at the specified absolute path into the target directory
        /// </summary>
        /// <param name="absolutePath">The location of the existing file or directory on the disk</param>
        /// <returns></returns>
        public DirectoryBuilder AppendExisting(StoragePath absolutePath) {
            if (!absolutePath.IsAbsolute) {
                throw new IOException("The path you specified is not an absolute path");
            }

            if (Directory.Exists(absolutePath.ToString())) {
                this.existingdirectorys.Add(absolutePath);
            }
            else {
                if (File.Exists(absolutePath.ToString())) {
                    this.existingFiles.Add(absolutePath);
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
        public DirectoryBuilder AppendExisting(string absolutePath) {
            return this.AppendExisting(new StoragePath(absolutePath));
        }

        /// <summary>
        /// Copies an existing file into the target directory
        /// </summary>
        /// <param name="absolutePath">The location of the existing file on the disk</param>
        public DirectoryBuilder AppendExisting(FileInfo file) {
            if (!file.Exists) {
                throw new FileNotFoundException();
            }

            return this.AppendExisting(new StoragePath(file.FullName));
        }

        /// <summary>
        /// Copies an existing directory into the target directory
        /// </summary>
        /// <param name="absolutePath">The location of the directory on the disk</param>
        public DirectoryBuilder AppendExisting(DirectoryInfo directory) {
            if (!directory.Exists) {
                throw new DirectoryNotFoundException();
            }

            return this.AppendExisting(new StoragePath(directory.FullName));
        }

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
        /// Runs all operations on the target directory asychronously
        /// </summary>
        public async Task BuildAsync() {
            await Task.Run(() => {
                // Build existing directories
                foreach (var item in existingdirectorys) {
                    AppendExistingCore(item, new StoragePath());
                };
                this.existingdirectorys.Clear();

                // Read the existing files
                foreach (var item in existingFiles) {
                    StoragePath name = item.ScopeToName();

                    if (!File.Exists(item.ToString())) {
                        throw new FileNotFoundException($"The file '{name.ToString()}' does not exist");
                    }

                    byte[] contents = File.ReadAllBytes(item.ToString());
                    this.filesToBuild.Add(Tuple.Create(name, contents));
                };
                this.existingFiles.Clear();

                // Read all zip files
                foreach (var item in zips) {
                    foreach (var entry in item.Entries) {
                        using (var stream = entry.Open()) {
                            byte[] contents;

                            using (MemoryStream ms = new MemoryStream()) {
                                stream.CopyTo(ms);
                                contents = ms.ToArray();
                            }

                            this.AppendFile(new StoragePath(entry.FullName), contents);
                        }
                    }
                };
                this.zips.Clear();

                // Build files
                foreach (var item in filesToBuild) {
                    StoragePath path = this.RootDirectory + item.Item1;
                    StoragePath dir = path.ParentDirectory;

                    if (!Directory.Exists(dir.ToString())) {
                        Directory.CreateDirectory(dir.ToString());
                    }

                    if (this.ConflictResolution == NameConflictOption.Overwrite) {
                        File.WriteAllBytes(path.ToString(), item.Item2 ?? new byte[0]);
                    }
                    else {
                        bool targetExists = File.Exists(path.ToString());
                        if (this.ConflictResolution == NameConflictOption.Skip && !targetExists) {
                            File.WriteAllBytes(path.ToString(), item.Item2 ?? new byte[0]);
                        }
                        else if (this.ConflictResolution == NameConflictOption.ThrowException) {
                            if (targetExists) {
                                throw new IOException($"The file '{Path.GetFileName(path.ToString())}' already exists");
                            }
                            else {
                                File.WriteAllBytes(path.ToString(), item.Item2 ?? new byte[0]);
                            }
                        }
                        else if (this.ConflictResolution == NameConflictOption.Rename) {
                            File.WriteAllBytes(ResolveFileName(path.ToString()), item.Item2 ?? new byte[0]);
                        }
                    }
                };
                this.filesToBuild.Clear();

                // Build directories
                foreach (var item in directorysToBuild) {
                    string dir = (this.RootDirectory + item).ToString();

                    if (!Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                    }
                };
                this.directorysToBuild.Clear();
            });
        }

        private void AppendExistingCore(StoragePath source, StoragePath relativeDir) {
            // Copy each file into the new directory.
            foreach (var fi in Directory.EnumerateFiles(source.ToString())) {
                byte[] contents = File.ReadAllBytes(fi);
                this.AppendFile(relativeDir + Path.GetFileName(fi), contents);
            };

            // Copy each subdirectory using recursion.
            foreach (string sub in Directory.EnumerateDirectories(source.ToString())) {
                var subName = Path.GetFileName(sub.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                relativeDir = relativeDir + subName;
                this.AppendDirectory(relativeDir);
                AppendExistingCore(new StoragePath(sub), relativeDir);
            }
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
    }
}