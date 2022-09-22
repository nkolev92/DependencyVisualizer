using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol.Core.Types;

namespace Common.Test
{
    /// <summary>
    /// This class is used to create a temp file, which is deleted in Dispose().
    /// </summary>
    public class TempFile : IDisposable
    {
        public string FilePath { get; }
        private static readonly int MaxTries = 3;

        /// <summary>
        /// Constructor. It creates an empty temp file under the temp directory / NuGet, with
        /// extension <paramref name="extension"/>.
        /// </summary>
        /// <param name="extension">The extension of the temp file.</param>
        public TempFile()
        {
            FilePath = Path.GetTempFileName();
        }

        public static implicit operator string(TempFile f)
        {
            return f.FilePath;
        }

        public void Dispose()
        {
            for (int i = 0; i < MaxTries; i++)
            {
                try
                {
                    if (File.Exists(FilePath))
                    {
                        File.Delete(FilePath);
                    }

                    return;
                }
                catch (Exception ex) when (i < MaxTries - 1 && (ex is UnauthorizedAccessException || ex is IOException))
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
