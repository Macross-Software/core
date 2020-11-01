using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace System.IO
{
	public class TestFileSystem : IFileSystem
	{
		private static Regex BuildRegexWildcardString(string pattern)
		{
			if (string.IsNullOrEmpty(pattern))
				throw new ArgumentNullException(nameof(pattern));

			StringBuilder regexPattern = new StringBuilder(pattern.Length * 3);

			regexPattern.Append('^');

			foreach (char c in pattern)
			{
				switch (c)
				{
					case '*':
						regexPattern.Append(".*");
						break;
					case '?':
						regexPattern.Append('.');
						break;
					default:
						regexPattern.Append('[');
						regexPattern.Append(c);
						regexPattern.Append(']');
						break;
				}
			}

			regexPattern.Append('$');

			return new Regex(regexPattern.ToString(), RegexOptions.IgnoreCase);
		}

		internal class TestDirectory
		{
			public TestDirectory? Parent { get; }

			public string Name { get; }

			public IDictionary<string, TestDirectory> Directories { get; } = new Dictionary<string, TestDirectory>();

			public IDictionary<string, TestFile> Files { get; } = new Dictionary<string, TestFile>();

			public string FullPath
			{
				get
				{
					Collection<string> PathNodes = new Collection<string>();

					TestDirectory CurrentDirectory = this;
					while (true)
					{
						PathNodes.Add(CurrentDirectory.Name);
						if (CurrentDirectory.Parent.Name == RootDirectoryName)
							break;
						CurrentDirectory = CurrentDirectory.Parent;
					}

					return string.Join(s_DirectorySeparator, PathNodes.Reverse());
				}
			}

			public TestDirectory(TestDirectory? parent, string name)
			{
				Parent = parent;
				Name = name;
			}
		}

		internal class TestFile
		{
			public TestDirectory Parent { get; }

			public string Name { get; }

			public ArraySegment<byte>? Content { get; internal set; }

			public string FullPath => $"{Parent.FullPath}{Path.DirectorySeparatorChar}{Name}";

			public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

			public TestFile(TestDirectory parent, string name)
			{
				Parent = parent;
				Name = name;
			}
		}

		private class TestFileStream : MemoryStream
		{
			private readonly TestFile _File;

			public TestFileStream(TestFile file)
				: base(file?.Content?.Count ?? 4096)
			{
				_File = file ?? throw new ArgumentNullException(nameof(file));

				if (file.Content.HasValue)
				{
					Write(file.Content.Value.Array, file.Content.Value.Offset, file.Content.Value.Count);
					Position = 0;
				}
			}

			protected override void Dispose(bool disposing)
			{
				_File.Content = new ArraySegment<byte>(GetBuffer(), 0, (int)Length);

				base.Dispose(disposing);
			}
		}

		private const string RootDirectoryName = ":Root";

		private static readonly string s_DirectorySeparator = new string(new char[] { Path.DirectorySeparatorChar });

		private readonly TestDirectory _Root = new TestDirectory(null, RootDirectoryName);

		public TestFileSystem(params string[] directories)
		{
			foreach (string Directory in directories)
			{
				CreateDirectory(Directory);
			}
		}

		public void CreateDirectory(string path) => FindDirectory(path, true);

		public void DeleteDirectory(string path, bool recursive)
		{
			TestDirectory TestDirectory = FindDirectory(path, false);
			if (TestDirectory == null)
				throw new DirectoryNotFoundException();

			if (!recursive
				&& (TestDirectory.Directories.Count > 0
				|| TestDirectory.Files.Count > 0))
			{
				throw new IOException("Directory is not empty");
			}

			TestDirectory.Parent.Directories.Remove(TestDirectory.Name.ToUpperInvariant());
		}

		public bool DirectoryExists(string path) => FindDirectory(path, false) != null;

		public IEnumerable<string> EnumerateDirectories(string path)
		{
			TestDirectory TestDirectory = FindDirectory(path, false);
			if (TestDirectory == null)
				throw new DirectoryNotFoundException();

			Collection<string> Matches = new Collection<string>();

			FindDirectoryMatches(Matches, TestDirectory, null, SearchOption.TopDirectoryOnly);

			return Matches;
		}

		public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
		{
			TestDirectory TestDirectory = FindDirectory(path, false);
			if (TestDirectory == null)
				throw new DirectoryNotFoundException();

			Regex SearchPatternRegex = BuildRegexWildcardString(searchPattern);

			Collection<string> Matches = new Collection<string>();

			FindDirectoryMatches(Matches, TestDirectory, SearchPatternRegex, searchOption);

			return Matches;
		}

		public bool FileExists(string path) => FindFile(path, false) != null;

		public DateTime GetFileCreationTimeUtc(string path) => FindFile(path, false)?.CreatedAtUtc ?? DateTime.MinValue;

		public void MoveFile(string sourceFileName, string destFileName)
		{
			using Stream ExistingFile = OpenFile(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.None);
			using Stream NewFile = OpenFile(destFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None);

			ExistingFile.CopyTo(NewFile);

			DeleteFile(sourceFileName);
		}

		public Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
		{
			if (mode == FileMode.CreateNew && FileExists(path))
				throw new IOException("File already exists.");
			if (mode == FileMode.Open && !FileExists(path))
				throw new FileNotFoundException();

			TestFile File = FindFile(path, true);

			if (File.Content.HasValue && mode == FileMode.Truncate)
				File.Content = null;

			TestFileStream Stream = new TestFileStream(File);

			if (mode == FileMode.Append)
				Stream.Position = Stream.Length;

			return Stream;
		}

		public void DeleteFile(string path)
		{
			TestFile TestFile = FindFile(path, false);
			if (TestFile == null)
				throw new FileNotFoundException();

			TestFile.Parent.Files.Remove(TestFile.Name.ToUpperInvariant());
		}

		public IEnumerable<string> EnumerateFiles(string path)
		{
			TestDirectory TestDirectory = FindDirectory(path, false);
			if (TestDirectory == null)
				throw new DirectoryNotFoundException();

			Collection<string> Matches = new Collection<string>();

			FindFileMatches(Matches, TestDirectory, null, SearchOption.TopDirectoryOnly);

			return Matches;
		}

		public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
		{
			TestDirectory TestDirectory = FindDirectory(path, false);
			if (TestDirectory == null)
				throw new DirectoryNotFoundException();

			Regex SearchPatternRegex = BuildRegexWildcardString(searchPattern);

			Collection<string> Matches = new Collection<string>();

			FindFileMatches(Matches, TestDirectory, SearchPatternRegex, searchOption);

			return Matches;
		}

		internal TestDirectory FindDirectory(string path, bool createIfNotFound)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			TestDirectory Directory = null;
			TestDirectory SearchLocation = _Root;
			foreach (string PathNode in path.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
			{
				if (string.IsNullOrWhiteSpace(PathNode))
					throw new ArgumentException("Path is invalid.", nameof(path));
				string PathUpperInvariant = PathNode.ToUpperInvariant();
				if (!SearchLocation.Directories.TryGetValue(PathUpperInvariant, out Directory))
				{
					if (!createIfNotFound)
						return null;
					Directory = new TestDirectory(SearchLocation, PathNode);
					SearchLocation.Directories.Add(PathUpperInvariant, Directory);
				}
				SearchLocation = Directory;
			}
			return Directory;
		}

		internal TestFile FindFile(string path, bool createIfNotFound)
		{
			TestDirectory Directory = FindDirectory(Path.GetDirectoryName(path), false);
			if (Directory == null)
				throw new DirectoryNotFoundException();

			string FileName = Path.GetFileName(path);
			string FileNameUpperInvariant = FileName.ToUpperInvariant();

			if (!Directory.Files.TryGetValue(FileNameUpperInvariant, out TestFile file))
			{
				if (!createIfNotFound)
					return null;
				file = new TestFile(Directory, FileName);
				Directory.Files.Add(FileNameUpperInvariant, file);
			}

			return file;
		}

		private void FindDirectoryMatches(Collection<string> matches, TestDirectory directory, Regex? searchPattern, SearchOption searchOption)
		{
			foreach (TestDirectory ChildDirectory in directory.Directories.Values)
			{
				if (searchPattern == null || searchPattern.IsMatch(ChildDirectory.Name))
					matches.Add(ChildDirectory.FullPath);

				if (searchOption == SearchOption.AllDirectories)
					FindDirectoryMatches(matches, ChildDirectory, searchPattern, searchOption);
			}
		}

		private void FindFileMatches(Collection<string> matches, TestDirectory directory, Regex? searchPattern, SearchOption searchOption)
		{
			foreach (TestFile ChildFile in directory.Files.Values)
			{
				if (searchPattern == null || searchPattern.IsMatch(ChildFile.Name))
					matches.Add(ChildFile.FullPath);
			}

			if (searchOption == SearchOption.AllDirectories)
			{
				foreach (TestDirectory ChildDirectory in directory.Directories.Values)
				{
					FindFileMatches(matches, ChildDirectory, searchPattern, searchOption);
				}
			}
		}
	}
}
