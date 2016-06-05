using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JoshuaKearney.FileSystem {

    /// <summary>
    /// Describes the action that should be taken by a file operation if a naming conflict should occur
    /// The backing ints are synced with the UWP enum System.Storage.NameCollisionOption
    /// </summary>
    public enum NameConflictOption {

        /// <summary>
        /// Halts the current operation and throws an IOException
        /// </summary>
        ThrowException = 2,

        /// <summary>
        /// Renames the new file to avoid a conflict
        /// </summary>
        Rename = 0,

        /// <summary>
        /// Skips the current operation, and continues
        /// </summary>
        Skip = 3,

        /// <summary>
        /// Overrites the existing file with the new one
        /// </summary>
        Overwrite = 1
    }
}