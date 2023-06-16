using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unithereum.CodeGen
{
    public class UnithereumCodeGenProcessor : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 0;

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
        /// Detect .abi and .bin files on import and generate code.
        /// </summary>
        public void OnPreprocessAsset()
        {
            var changedPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, assetPath);
            if (changedPath.EndsWith(".abi", StringComparison.OrdinalIgnoreCase))
            {
                var binPath = Path.ChangeExtension(changedPath, ".bin");
                Generate(changedPath, File.Exists(binPath) ? binPath : null);
            }
            else if (changedPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                var abiPath = Path.ChangeExtension(changedPath, ".abi");
                if (File.Exists(abiPath)) Generate(abiPath, changedPath);
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!(GetDotNetPath() is null)) return;
            Debug.LogWarning("dotnet not found in PATH, Nethereum code generation will not work.");
        }

        private static void Generate(string abiPath, string? binPath)
        {
            if (!(GetDotNetPath() is { } dotnet)) return;
            
            var assemblyDir =
                Path.GetDirectoryName(new Uri(typeof(UnithereumCodeGenProcessor).Assembly.CodeBase).LocalPath);
            using var restoreProcess = new Process
            {
                StartInfo =
                {
                    FileName = dotnet,
                    Arguments = "tool restore",
                    WorkingDirectory = assemblyDir,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            restoreProcess.Start();
            var error = restoreProcess.StandardError.ReadToEnd();
            restoreProcess.WaitForExit();
            if (restoreProcess.ExitCode != 0)
            {
                throw new InvalidOperationException("dotnet tool restore failed: " + error);
            }

            // TODO: Enable configuration
            var codegenPath = Path.Combine(Application.dataPath, "ContractServices");
            var codegenNamespace = GetCodeGenNamespace();
            var assemblyDefinition = Path.Combine(codegenPath, codegenNamespace + ".asmdef");
            var cscDirectives = Path.Combine(codegenPath, "csc.rsp");
            
            // Suppress warnings in generated code
            if (!Directory.Exists(codegenPath)) Directory.CreateDirectory(codegenPath);
            if (!File.Exists(assemblyDefinition))
                File.WriteAllText(assemblyDefinition, $@"{{""name"": ""{codegenNamespace}""}}");
            if (!File.Exists(cscDirectives))
                File.WriteAllText(cscDirectives, "-warn:0");

            using var codegenProcess = new Process
            {
                StartInfo =
                {
                    FileName = dotnet,
                    ArgumentList =
                    {
                        "tool",
                        "run",
                        "Nethereum.Generator.Console",
                        "--",
                        "generate",
                        "from-abi",
                        "-o",
                        codegenPath,
                        "-ns",
                        codegenNamespace,
                        "-abi",
                        abiPath
                    },
                    WorkingDirectory = assemblyDir,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            if (!(binPath is null))
            {
                codegenProcess.StartInfo.ArgumentList.Add("-bin");
                codegenProcess.StartInfo.ArgumentList.Add(binPath);
            }

            codegenProcess.Start();
            error = codegenProcess.StandardError.ReadToEnd();
            codegenProcess.WaitForExit();
            
            if (codegenProcess.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    "Failed to generate Nethereum contract service code: " + error);
            }
            
            AssetDatabase.ImportAsset(
                Path.Combine(Path.GetFileName(Application.dataPath), "ContractServices"),
                ImportAssetOptions.ImportRecursive);
        }

        private static string GetCodeGenNamespace()
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
            
            return productName + ".ContractServices";
        }

        private static string SanitizeLeadingDots(string str)
            => str != string.Empty ? str[0] == '.' ? "_" + SanitizeLeadingDots(str[1..]) : str : string.Empty;
        
        private static string SanitizeTrailingDots(string str)
            => str != string.Empty ? str[^1] == '.' ? SanitizeTrailingDots(str[..^1]) + "_" : str : string.Empty;

        private static string? GetDotNetPath()
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
                    CreateNoWindow = true,
                },
            };

            process.Start();
            var dotnet = process.StandardOutput.ReadToEnd().Trim();
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
                        Arguments = $". -read {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)} UserShell",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                const string dsclPrefix = "UserShell:";
                if (!output.StartsWith(dsclPrefix)) throw new InvalidOperationException(
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
                {
                    throw new InvalidOperationException(
                        "The default shell of the current user needed to find the dotnet executable required for"
                        + " Nethereum code generation is empty in /etc/passwd.");
                }
            }

            return defaultShell;
        }
    }
}