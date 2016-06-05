using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoshuaKearney.FileSystem {

    /// <summary>
    /// An enum that defines the valid characters that can separate file and folders in a path
    /// </summary>
    public enum PathSeparator {

        /// <summary>
        /// Represents a forward slash
        /// </summary>
        ForwardSlash = '/',

        /// <summary>
        /// Represents a back slash
        /// </summary>
        BackSlash = '\\'
    }
}