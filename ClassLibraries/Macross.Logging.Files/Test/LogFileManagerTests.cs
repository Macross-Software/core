using System;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Logging.Files.Tests
{
	[TestClass]
	public class LogFileManagerTests
	{
		private static readonly FileLoggerOptions s_DefaultOptions = new FileLoggerOptions
		{
			LogFileDirectory = FileLoggerOptions.DefaultLogFileDirectory,
			LogFileArchiveDirectory = FileLoggerOptions.DefaultLogFileArchiveDirectory
		};

		private static TestFileSystem CreateDefaultFileSystem()
		{
			return new TestFileSystem(
				FileLoggerOptions.DefaultLogFileDirectory,
				FileLoggerOptions.DefaultLogFileArchiveDirectory);
		}

		[TestMethod]
		public void CreateNewLogFileTest()
		{
			using LogFileManager Manager = new LogFileManager(
				CreateDefaultFileSystem(),
				new TestSystemTime(2020, 1, 19, 8, 0, 0));

			LogFile LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				null);

			Assert.IsNotNull(LogFile);
			Assert.AreEqual($"{Environment.MachineName}.20200119.log", LogFile.FinalFileName);
		}

		[TestMethod]
		public void ReuseLogFileTest()
		{
			using LogFileManager Manager = new LogFileManager(
				CreateDefaultFileSystem(),
				new TestSystemTime(2020, 1, 19, 8, 0, 0));

			LogFile LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				null);

			LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				null);

			Assert.IsNotNull(LogFile);
			Assert.AreEqual($"{Environment.MachineName}.20200119.log", LogFile.FinalFileName);
		}

		[TestMethod]
		public void ToxicLogFileTest()
		{
			using LogFileManager Manager = new LogFileManager(
				CreateDefaultFileSystem(),
				new TestSystemTime(2020, 1, 19, 8, 0, 0));

			LogFile LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				null);

			LogFile.Toxic = true;

			LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				null);

			Assert.IsNotNull(LogFile);
			Assert.AreEqual($"{Environment.MachineName}.20200119.1.log", LogFile.FinalFileName);
		}

		[TestMethod]
		public void LogFileDateCutoverTest()
		{
			TestFileSystem FileSystem = CreateDefaultFileSystem();
			TestSystemTime SystemTime = new TestSystemTime(2020, 1, 19, 8, 0, 0);

			using LogFileManager Manager = new LogFileManager(FileSystem, SystemTime);

			LogFile LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				null);

			SystemTime.UtcNow = SystemTime.UtcNow.AddDays(1);

			LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				null);

			Assert.IsNotNull(LogFile);
			Assert.AreEqual($"{Environment.MachineName}.20200120.log", LogFile.FinalFileName);

			Assert.AreEqual(1, FileSystem.EnumerateFiles(FileLoggerOptions.DefaultLogFileDirectory).Count());
			Assert.AreEqual(1, FileSystem.EnumerateFiles(FileLoggerOptions.DefaultLogFileArchiveDirectory).Count());
		}

		[TestMethod]
		public void AppendToFirstNonFullLogFileTest()
		{
			TestFileSystem FileSystem = CreateDefaultFileSystem();

			using LogFileManager Manager = new LogFileManager(FileSystem, new TestSystemTime(2020, 1, 19, 8, 0, 0));

			string BaseFileName = $"{FileLoggerOptions.DefaultLogFileDirectory}{Environment.MachineName}.20200119";

			for (int i = 0; i < 10; i++)
			{
				string FileName = i == 0
					? BaseFileName + ".log"
					: BaseFileName + $".{i}.log";

				using Stream File = FileSystem.OpenFile(FileName, FileMode.CreateNew, FileAccess.Write, FileShare.None);

				if (i < 9)
					File.Write(new byte[1024], 0, 1024);
			}

			Assert.AreEqual(10, FileSystem.EnumerateFiles(FileLoggerOptions.DefaultLogFileDirectory).Count());

			LogFile LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				1);

			Assert.IsNotNull(LogFile);
			Assert.AreEqual($"{Environment.MachineName}.20200119.9.log", LogFile.FinalFileName);
		}

		[TestMethod]
		public void SkipFullLogFilesTest()
		{
			TestFileSystem FileSystem = CreateDefaultFileSystem();

			using LogFileManager Manager = new LogFileManager(FileSystem, new TestSystemTime(2020, 1, 19, 8, 0, 0));

			string BaseFileName = $"{FileLoggerOptions.DefaultLogFileDirectory}{Environment.MachineName}.20200119";

			for (int i = 1; i < 10; i++)
			{
				string FileName = i == 0
					? BaseFileName + ".log"
					: BaseFileName + $".{i}.log";

				using Stream File = FileSystem.OpenFile(FileName, FileMode.CreateNew, FileAccess.Write, FileShare.None);

				File.Write(new byte[1024], 0, 1024);
			}

			Assert.AreEqual(9, FileSystem.EnumerateFiles(FileLoggerOptions.DefaultLogFileDirectory).Count());

			LogFile LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				1);

			Assert.IsNotNull(LogFile);
			Assert.AreEqual($"{Environment.MachineName}.20200119.log", LogFile.FinalFileName);

			LogFile.Stream.Write(new byte[1024], 0, 1024);

			LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				1);

			Assert.IsNotNull(LogFile);
			Assert.AreEqual($"{Environment.MachineName}.20200119.10.log", LogFile.FinalFileName);
		}

		[TestMethod]
		public void ArchiveLogFilesTest()
		{
			TestFileSystem FileSystem = CreateDefaultFileSystem();
			TestSystemTime SystemTime = new TestSystemTime(2020, 1, 19);

			using LogFileManager Manager = new LogFileManager(FileSystem, SystemTime);

			string BaseFileName = $"{FileLoggerOptions.DefaultLogFileDirectory}{Environment.MachineName}.20200118";

			for (int i = 0; i <= 10; i++)
			{
				string FileName = i == 0
					? BaseFileName + ".log"
					: BaseFileName + $".{i}.log";

				TestFileSystem.TestFile TestFile = FileSystem.FindFile(FileName, true);

				TestFile.CreatedAtUtc = SystemTime.UtcNow.AddDays(-1);
			}

			FileSystem.FindFile($"{FileLoggerOptions.DefaultLogFileDirectory}SomeRandomFile.txt", true);
			FileSystem.FindFile($"{FileLoggerOptions.DefaultLogFileDirectory}SomeRandomLogFile.log", true);
			FileSystem.FindFile($"{FileLoggerOptions.DefaultLogFileDirectory}SomeOtherMachineName.20200118.log", true);

			using (Stream FullFile = FileSystem.OpenFile($"{FileLoggerOptions.DefaultLogFileDirectory}{Environment.MachineName}.20200119.log", FileMode.Create, FileAccess.Write, FileShare.None))
			{
				FullFile.Write(new byte[1024], 0, 1024);
			}

			LogFile LogFile = Manager.FindLogFile(
				"AppName",
				"Group",
				() => s_DefaultOptions,
				FileLoggerOptions.DefaultLogFileNamePattern,
				1);

			Assert.AreEqual(16, FileSystem.EnumerateFiles(FileLoggerOptions.DefaultLogFileDirectory).Count());
			Assert.AreEqual(0, FileSystem.EnumerateFiles(FileLoggerOptions.DefaultLogFileArchiveDirectory).Count());

			Manager.ArchiveLogFiles("AppName", s_DefaultOptions, FileLoggerOptions.DefaultLogFileNamePattern);

			Assert.AreEqual(5, FileSystem.EnumerateFiles(FileLoggerOptions.DefaultLogFileDirectory).Count());
			Assert.AreEqual(11, FileSystem.EnumerateFiles(FileLoggerOptions.DefaultLogFileArchiveDirectory).Count());
		}
	}
}