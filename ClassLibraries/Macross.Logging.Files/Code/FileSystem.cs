using System.Collections.Generic;

namespace System.IO
{
	internal class FileSystem : IFileSystem
	{
		public bool FileExists(string path)
			=> File.Exists(path);

		public DateTime GetFileCreationTimeUtc(string path)
			=> File.GetCreationTimeUtc(path);

		public void MoveFile(string sourceFileName, string destFileName)
			=> File.Move(sourceFileName, destFileName);

		public Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
			=> new FileStream(path, mode, access, share);

		public IEnumerable<string> EnumerateFiles(string path)
			=> Directory.EnumerateFiles(path);

		public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
			=> Directory.EnumerateFiles(path, searchPattern, searchOption);
	}
}