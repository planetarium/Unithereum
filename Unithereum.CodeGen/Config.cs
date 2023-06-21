using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using UnityEngine;

namespace Unithereum.CodeGen
{
    public class Config
    {
        private static readonly HashSet<string> CsharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        /// <summary>
        ///     Absolute path to `dotnet` executable.
        /// </summary>
        public string? DotnetPath { get; }

        /// <summary>
        ///     Namespace prefix for generated contract service classes.
        ///     <para>Default: (Application.productName).ContractServices</para>
        /// </summary>
        public string NsPrefix { get; }

        /// <summary>
        ///     Relative path to `Asset` directory for generated contract service class files.
        ///     <para>Default: ContractServices</para>
        /// </summary>
        public string OutputDir { get; }

        [JsonConstructor]
        public Config([Optional] string? dotnetPath, [Optional] string? nsPrefix, [Optional] string? outputDir)
        {
            const string defaultName = "ContractServices";

            DotnetPath = dotnetPath ?? GetDotnetPath();
            NsPrefix = nsPrefix ?? GetNamespacePrefix() + '.' + defaultName;
            OutputDir = outputDir ?? defaultName;
        }

        public static Config GetConfig()
        {
            var path = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "codegen.config.json");
            return File.Exists(path) ? ReadFromJsonFile(path) : new Config();
        }

        private static Config ReadFromJsonFile(string configPath)
        {
            var json = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<Config>(json);
            return config;
        }

        public override string ToString()
        {
            return base.ToString() + ": " + JsonConvert.SerializeObject(this);
        }

        private static string? GetDotnetPath()
        {
            if (File.Exists("dotnet")) return Path.GetFullPath("dotnet");
            if (File.Exists("dotnet.exe")) return Path.GetFullPath("dotnet.exe");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return (Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>())
                    .Select(path => Path.Combine(path, "dotnet.exe"))
                    .FirstOrDefault(File.Exists);

            using var process = new Process
            {
                StartInfo =
                {
                    FileName = GetDefaultShell(),
                    Arguments = "--login -i -c 'command -v dotnet'",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var dotnet = '/' + string.Join('/', process.StandardOutput.ReadToEnd().Trim().Split('/')[1..]);
            process.WaitForExit();

            return dotnet != "" ? dotnet : null;
        }

        private static string GetDefaultShell()
        {
            string defaultShell;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                using var process = new Process
                {
                    StartInfo =
                    {
                        FileName = "dscl",
                        Arguments =
                            $". -read {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)} UserShell",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                const string dsclPrefix = "UserShell:";
                if (!output.StartsWith(dsclPrefix))
                    throw new InvalidOperationException(
                        "Could not get the default shell of the current user needed to find the dotnet executable"
                        + " required for Nethereum code generation.");
                defaultShell = output[dsclPrefix.Length..].Trim();
            }
            else
            {
                try
                {
                    defaultShell = File.ReadLines("/etc/passwd")
                        .First(line => line.StartsWith(Environment.UserName + ":")).Split(':')[6];
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException(
                        "Could not find the entry for the current user in /etc/passwd needed to find the dotnet"
                        + " executable required for Nethereum code generation.");
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException(
                        "Could not get the default shell of the current user needed to find the dotnet executable"
                        + " required for Nethereum code generation in /etc/passwd.");
                }

                if (defaultShell == "")
                    throw new InvalidOperationException(
                        "The default shell of the current user needed to find the dotnet executable required for"
                        + " Nethereum code generation is empty in /etc/passwd.");
            }

            return defaultShell;
        }

        private static string GetNamespacePrefix()
        {
            var productName =
                SanitizeLeadingDots(
                    SanitizeTrailingDots(
                        string.Join("",
                            Application.productName.Select(c =>
                            {
                                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                                return cat == UnicodeCategory.UppercaseLetter ||
                                       cat == UnicodeCategory.LowercaseLetter ||
                                       cat == UnicodeCategory.TitlecaseLetter ||
                                       cat == UnicodeCategory.ModifierLetter ||
                                       cat == UnicodeCategory.OtherLetter ||
                                       cat == UnicodeCategory.SpacingCombiningMark ||
                                       cat == UnicodeCategory.DecimalDigitNumber ||
                                       cat == UnicodeCategory.LetterNumber ||
                                       cat == UnicodeCategory.ConnectorPunctuation ||
                                       c == '.'
                                    ? c.ToString()
                                    : "_";
                            }))));

            var firstLetterCat = CharUnicodeInfo.GetUnicodeCategory(productName[0]);
            if (
                firstLetterCat != UnicodeCategory.UppercaseLetter &&
                firstLetterCat != UnicodeCategory.LowercaseLetter &&
                firstLetterCat != UnicodeCategory.TitlecaseLetter &&
                firstLetterCat != UnicodeCategory.ModifierLetter &&
                firstLetterCat != UnicodeCategory.OtherLetter &&
                firstLetterCat != UnicodeCategory.LetterNumber)
                productName = "_" + productName;

            productName = string.Join(
                ".",
                productName
                    .Split(".")
                    .Select(part => CsharpKeywords.Contains(part) ? "@" + part : part));

            return productName;
        }

        private static string SanitizeLeadingDots(string str)
        {
            return str != string.Empty ? str[0] == '.' ? "_" + SanitizeLeadingDots(str[1..]) : str : string.Empty;
        }

        private static string SanitizeTrailingDots(string str)
        {
            return str != string.Empty ? str[^1] == '.' ? SanitizeTrailingDots(str[..^1]) + "_" : str : string.Empty;
        }
    }
}