using System.Collections.Generic;

namespace System.IO
{
	/// <summary>
	/// IFileSystem interface is a slim wrapper over the System.IO interface so that unit tests don't need to access the actual file system.
	/// </summary>
	internal interface IFileSystem
	{
		bool FileExists(string path);

		DateTime GetFileCreationTimeUtc(string path);

		void MoveFile(string sourceFileName, string destFileName);

		Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share);

		IEnumerable<string> EnumerateFiles(string path);

		IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
	}
}
