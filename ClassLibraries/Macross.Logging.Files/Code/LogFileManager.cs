using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace Macross.Logging.Files
{
	internal class LogFileManager : IDisposable
	{
		private static bool IsLogFileOverCapacity(int? logFileMaxSizeInKilobytes, long lengthInBytes)
			=> logFileMaxSizeInKilobytes.HasValue && (lengthInBytes / 1024) >= logFileMaxSizeInKilobytes;

		private static string BuildCandidateFileName(string fileName, ref string[]? fileNameComponents, int index)
		{
			if (fileNameComponents == null)
				fileNameComponents = fileName.Split('.');

			StringBuilder FileNameBuilder = new StringBuilder();

			for (int i = 0; i < fileNameComponents.Length; i++)
			{
				if (i == fileNameComponents.Length - 1)
				{
					FileNameBuilder.Append(index);
					FileNameBuilder.Append('.');
					FileNameBuilder.Append(fileNameComponents[i]);
					break;
				}
				FileNameBuilder.Append(fileNameComponents[i]);
				FileNameBuilder.Append('.');
			}

			return FileNameBuilder.ToString();
		}

		private readonly Dictionary<string, LogFile> _LogFiles = new Dictionary<string, LogFile>();
		private readonly IFileSystem _FileSystem;
		private readonly ISystemTime _SystemTime;
		private Dictionary<string, string>? _GroupNameToFileNameCache;

		public LogFileManager(IFileSystem fileSystem, ISystemTime systemTime)
		{
			_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			_SystemTime = systemTime ?? throw new ArgumentNullException(nameof(systemTime));

			ClearCache();
		}

		~LogFileManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (isDisposing)
			{
				foreach (LogFile LogFile in _LogFiles.Values)
				{
					LogFile.Stream.Dispose();
				}
			}
		}

		public void ClearCache() => _GroupNameToFileNameCache = new Dictionary<string, string>();

		public LogFile? FindLogFile(
			string applicationName,
			string groupName,
			Func<FileLoggerOptions> options,
			string logFileNamePattern,
			int? logFileMaxSizeInKilobytes)
		{
			string FileName = FindFileNameForMessage(applicationName, groupName, logFileNamePattern);

			if (_LogFiles.TryGetValue(FileName, out LogFile ExistingLogFile))
			{
				return CheckExistingLogFileForCutoverAndSize(
					applicationName,
					groupName,
					options,
					logFileNamePattern,
					logFileMaxSizeInKilobytes,
					FileName,
					ExistingLogFile);
			}

			LogFile? NewLogFile = CreateNewLogFile(options().LogFileDirectory!, logFileMaxSizeInKilobytes, FileName, 0);
			if (NewLogFile != null)
				_LogFiles[FileName] = NewLogFile;
			return NewLogFile;
		}

		public void ArchiveLogFiles(string applicationName, FileLoggerOptions options, string logFileNamePattern)
		{
			try
			{
				foreach (string LogFilePath in _FileSystem.EnumerateFiles(
					options.LogFileDirectory!,
					FileNameGenerator.GenerateWildcardFileName(applicationName, logFileNamePattern),
					SearchOption.TopDirectoryOnly))
				{
					if (_LogFiles.Any(i => i.Value.FinalFullPath == LogFilePath))
						continue;

					if (_FileSystem.GetFileCreationTimeUtc(LogFilePath) < DateTime.UtcNow.Date)
						TryArchiveLogFile(options, LogFilePath);
				}
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception FatalException)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				Console.Error.WriteLine(FatalException.ToString());
			}
		}

		private bool TryArchiveLogFile(FileLoggerOptions options, string logFilePath)
		{
			try
			{
				_FileSystem.MoveFile(logFilePath, Path.Combine(options.LogFileArchiveDirectory, Path.GetFileName(logFilePath)));
				return true;
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch
#pragma warning restore CA1031 // Do not catch general exception types
			{
				return false;
			}
		}

		private LogFile? CheckExistingLogFileForCutoverAndSize(
			string applicationName,
			string groupName,
			Func<FileLoggerOptions> options,
			string logFileNamePattern,
			int? logFileMaxSizeInKilobytes,
			string fileName,
			LogFile existingLogFile)
		{
			if (_SystemTime.UtcNow.Date > existingLogFile.DateUtc)
			{
				string NewFileName = FileNameGenerator.GenerateFileName(applicationName, _SystemTime, groupName, logFileNamePattern!);

				LogFile? NewLogFile = CreateNewLogFile(options().LogFileDirectory!, logFileMaxSizeInKilobytes, NewFileName, 0);
				if (NewLogFile == null)
					return existingLogFile;

				existingLogFile.Stream.Dispose();
				TryArchiveLogFile(options(), existingLogFile.FinalFullPath);
				_LogFiles.Remove(fileName);

				_LogFiles[NewFileName] = NewLogFile;
				_GroupNameToFileNameCache![groupName] = NewFileName;

				return NewLogFile;
			}

			if (existingLogFile.Toxic || IsLogFileOverCapacity(logFileMaxSizeInKilobytes, existingLogFile.Stream.Length))
			{
				LogFile? NewLogFile = CreateNewLogFile(options().LogFileDirectory!, logFileMaxSizeInKilobytes, fileName, existingLogFile.Index + 1);
				if (NewLogFile == null)
					return existingLogFile;

				existingLogFile.Stream.Dispose();
				_LogFiles[fileName] = NewLogFile;
				return NewLogFile;
			}

			return existingLogFile;
		}

		private string FindFileNameForMessage(string applicationName, string groupName, string logFileNamePattern)
		{
			if (!_GroupNameToFileNameCache!.TryGetValue(groupName, out string FileName))
			{
				FileName = FileNameGenerator.GenerateFileName(applicationName, _SystemTime, groupName, logFileNamePattern!);

				_GroupNameToFileNameCache.Add(groupName, FileName);
			}

			return FileName;
		}

		private LogFile? CreateNewLogFile(string logFileDirectory, int? logFileMaxSizeInKilobytes, string fileName, int index)
		{
			string[]? FileNameComponents = null;

			while (true)
			{
				string CandidateFileName = index == 0
					? fileName
					: BuildCandidateFileName(fileName, ref FileNameComponents, index);

				string FullFilePath = Path.Combine(logFileDirectory, CandidateFileName);

				try
				{
					if (!_FileSystem.FileExists(FullFilePath))
					{
						return new LogFile(
							_SystemTime.UtcNow.Date,
							fileName,
							index,
							CandidateFileName,
							FullFilePath,
							_FileSystem.OpenFile(FullFilePath, FileMode.Append, FileAccess.Write, FileShare.Read));
					}
					else
					{
						Stream Stream = _FileSystem.OpenFile(FullFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
						if (IsLogFileOverCapacity(logFileMaxSizeInKilobytes, Stream.Length))
						{
							index++;
							Stream.Dispose();
							continue;
						}

						return new LogFile(
							_FileSystem.GetFileCreationTimeUtc(FullFilePath).Date,
							fileName,
							index,
							CandidateFileName,
							FullFilePath,
							Stream);
					}
				}
				catch (UnauthorizedAccessException) // Something else has locked the file, try a different name.
				{
					index++;
				}
#pragma warning disable CA1031 // Do not catch general exception types
				catch (Exception FatalException)
#pragma warning restore CA1031 // Do not catch general exception types
				{
					Console.Error.WriteLine(FatalException.ToString());
					return null;
				}
			}
		}
	}
}
