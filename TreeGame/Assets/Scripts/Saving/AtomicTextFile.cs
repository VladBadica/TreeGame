using System;
using System.IO;
using System.Text;

namespace TreeGame.Saving
{
    /// <summary>
    /// A text file that is replaced in one step: new text goes to a temporary file first, which then takes the real
    /// file's place. A crash or a full disk partway through can therefore never leave a half-written file behind.
    /// Has no Unity dependency, so it can be checked on its own. Problems reading or writing throw, for the caller to
    /// handle.
    /// </summary>
    public sealed class AtomicTextFile
    {
        private const string TemporarySuffix = ".tmp";

        private readonly string path;

        public AtomicTextFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A file path is required.", nameof(path));
            }

            this.path = path;
        }

        public bool Exists => File.Exists(path);

        public string Read()
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }

        /// <summary>Replaces the file's whole text, creating the file and its folder if needed.</summary>
        public void Write(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            string temporaryPath = path + TemporarySuffix;
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);

                // All the way to the disk, or a power cut could still lose it after the swap below.
                stream.Flush(flushToDisk: true);
            }

            if (Exists)
            {
                File.Replace(temporaryPath, path, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
    }
}
