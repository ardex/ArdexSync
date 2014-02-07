using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ardex.Sync;

namespace Ardex.TestClient
{
    public class FileEntry
    {
        public string FileName { get; set; }
        public byte[] Contents { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class FolderRepository : SyncRepository<FileEntry>
    {
        private readonly string __path;
        private readonly FileSystemWatcher __watcher;

        private readonly object FILE_LOCK = new object();

        public FolderRepository(string path)
        {
            __path = path;

            this.EntityInserted += this.WriteFile;
            this.EntityUpdated += this.WriteFile;

            // Watch the files.
            __watcher = new FileSystemWatcher(path);

            __watcher.Created += this.FileChangeDetected;
            __watcher.Changed += this.FileChangeDetected;
            __watcher.Renamed += this.FileChangeDetected;
            __watcher.Deleted += this.FileChangeDetected;

            __watcher.EnableRaisingEvents = true;
        }

        private void WriteFile(FileEntry file)
        {
            var filePath = Path.Combine(__path, file.FileName);

            lock (FILE_LOCK)
            {
                var contents = this.ReadFile(filePath);

                if (contents != file.Contents)
                {
                    File.WriteAllBytes(filePath, file.Contents);
                }
            }
        }

        private void FileChangeDetected(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed)
            {
                lock (this.FILE_LOCK)
                { 
                    // See if the file entry exists.
                    var file = this.SingleOrDefault(f => string.Equals(f.FileName, e.Name, StringComparison.InvariantCulture));
                    var lastModified = File.GetLastWriteTimeUtc(e.FullPath);

                    lastModified = DateTime.SpecifyKind(lastModified, DateTimeKind.Utc);

                    if (file != null)
                    {
                        if (lastModified != file.LastModified)
                        {
                            file.Contents = this.ReadFile(e.FullPath);
                            file.LastModified = lastModified;

                            this.Update(file);
                        }
                    }
                    else
                    {
                        file = new FileEntry {
                            FileName = Path.GetFileName(e.FullPath),
                            Contents = this.ReadFile(e.FullPath),
                            LastModified = lastModified
                        };

                        this.Insert(file);
                    }
                }
            }
            else
            {
                // ignore.
                Debug.Print("unsupported file op: {0}", e.ChangeType);
            }
        }

        private byte[] ReadFile(string path)
        {
            using (var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);

                    return ms.ToArray();
                }
            }
        }

        //private byte[] WriteFile(string path)
        //{

        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                __watcher.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
