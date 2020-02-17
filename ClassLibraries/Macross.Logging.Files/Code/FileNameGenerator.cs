using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Macross.Logging.Files
{
	internal static class FileNameGenerator
	{
		private delegate string TokenConverterFunc(string applicationName, ISystemTime systemTime, string groupName, string? tokenFormatOptions);

		private delegate string WildcardTokenConverterFunc(string applicationName);

		private static readonly Regex s_FileNameRegex = new Regex("{(.*?)}", RegexOptions.Compiled);

		private static readonly char[] s_SplitCharacters = new[] { ':' };

		private static readonly Dictionary<string, TokenConverterFunc> s_FileNameTokenConverterFuncDictionary = new Dictionary<string, TokenConverterFunc>(StringComparer.OrdinalIgnoreCase)
		{
			["MachineName"] = (a, t, g, o) => Environment.MachineName,
			["ApplicationName"] = (a, t, g, o) => a,
			["GroupName"] = (a, t, g, o) => g,
			["DateTimeUtc"] = (a, t, g, o) => t.UtcNow.ToString(o ?? "yyyyMMdd", CultureInfo.InvariantCulture),
			["DateTime"] = (a, t, g, o) => t.Now.ToString(o ?? "yyyyMMdd", CultureInfo.InvariantCulture),
		};

		private static readonly Dictionary<string, WildcardTokenConverterFunc> s_FileNameWildcardTokenConverterFuncDictionary = new Dictionary<string, WildcardTokenConverterFunc>(StringComparer.OrdinalIgnoreCase)
		{
			["MachineName"] = (a) => Environment.MachineName,
			["ApplicationName"] = (a) => a,
			["GroupName"] = (a) => "*",
			["DateTimeUtc"] = (a) => "*",
			["DateTime"] = (a) => "*",
		};

		public static string GenerateFileName(string applicationName, ISystemTime systemTime, string groupName, string fileNamePattern)
		{
			return s_FileNameRegex.Replace(fileNamePattern, (match) =>
			{
				string Group = match.Groups[1].Value;

				string[] Options = Group.Split(s_SplitCharacters, StringSplitOptions.RemoveEmptyEntries);

				return s_FileNameTokenConverterFuncDictionary.TryGetValue(Options[0], out TokenConverterFunc TokenConverterFunc)
					? TokenConverterFunc(applicationName, systemTime, groupName, Options.Length > 1 ? Options[1] : null)
					: match.Groups[0].Value;
			});
		}

		public static string GenerateWildcardFileName(string applicationName, string fileNamePattern)
		{
			string FileName = s_FileNameRegex.Replace(fileNamePattern, (match) =>
			{
				string Group = match.Groups[1].Value;

				string[] Options = Group.Split(s_SplitCharacters, StringSplitOptions.RemoveEmptyEntries);

				return s_FileNameWildcardTokenConverterFuncDictionary.TryGetValue(Options[0], out WildcardTokenConverterFunc WildcardTokenConverterFunc)
					? WildcardTokenConverterFunc(applicationName)
					: match.Groups[0].Value;
			});

			string[] FileNameComponents = FileName.Split('.');

			StringBuilder FileNameBuilder = new StringBuilder();

			for (int i = 0; i < FileNameComponents.Length; i++)
			{
				if (i == FileNameComponents.Length - 1)
				{
					FileNameBuilder.Append('*');
					FileNameBuilder.Append(FileNameComponents[i]);
					break;
				}
				FileNameBuilder.Append(FileNameComponents[i]);
				FileNameBuilder.Append('.');
			}

			return FileNameBuilder.ToString();
		}
	}
}
