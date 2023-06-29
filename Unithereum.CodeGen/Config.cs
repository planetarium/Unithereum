using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unithereum.CodeGen
{
    /// <summary>
    /// Config class of <see cref="CodeGen" />.
    /// See README to find an example codegen.config.json file.
    /// For actual codegen process, refer <see cref="UnithereumCodeGenProcessor"/>.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Absolute path to `dotnet` executable.
        /// </summary>
        public string DotnetPath { get; }

        /// <summary>
        /// Namespace prefix for generated contract service classes.
        /// <para>Default: (Application.productName).ContractServices</para>
        /// </summary>
        public string NamespacePrefix { get; }

        /// <summary>
        /// Relative path to `Asset` directory for generated contract service class files.
        /// <para>Default: ContractServices</para>
        /// </summary>
        public string OutputDir { get; }

        /// <summary>
        /// Relative path to Unity project directory where to find `.abi` file inputs.
        /// <para>Default: Assets</para>
        /// <para>
        /// WARNING: Where given path is not inside `Assets/` directory,
        /// automatic file change detection and generation won't work.
        /// You may need to manually trigger `Unithereum > Regenerate All...` menu.
        /// Use with caution.
        /// </para>
        /// </summary>
        public string ContractsDir { get; }

        [JsonConstructor]
        public Config(
            [Optional] string? dotnetPath,
            [Optional] string? namespacePrefix,
            [Optional] string? outputDir,
            [Optional] string? contractsDir
        )
        {
            const string defaultName = "ContractServices";

            if (dotnetPath != null && !File.Exists(dotnetPath))
            {
                throw new InvalidCodeGenConfigurationException(
                    message: "`dotnet` executable doesn't exist at given path.",
                    key: nameof(dotnetPath),
                    value: dotnetPath
                );
            }

            if (namespacePrefix != null && !ValidateNamespacePrefix(namespacePrefix))
            {
                throw new InvalidCodeGenConfigurationException(
                    message: "Use proper C# namespace identifier.",
                    key: nameof(namespacePrefix),
                    value: namespacePrefix
                );
            }

            if (outputDir != null && Path.IsPathFullyQualified(outputDir))
            {
                throw new InvalidCodeGenConfigurationException(
                    message: "Using absolute path is not supported. "
                        + "Use relative path to Unity `Asset/` directory instead.",
                    key: nameof(outputDir),
                    value: outputDir
                );
            }

            if (contractsDir != null && Path.IsPathFullyQualified(contractsDir))
            {
                throw new InvalidCodeGenConfigurationException(
                    message: "Using absolute path is not supported. "
                        + "Use relative path to Unity project directory instead.",
                    key: nameof(contractsDir),
                    value: contractsDir
                );
            }

            this.DotnetPath =
                dotnetPath
                ?? GetDotnetPath()
                ?? throw new InvalidCodeGenConfigurationException(
                    message: "`dotnet` executable not found in PATH, "
                        + "Nethereum code generation would not work."
                );
            this.NamespacePrefix =
                namespacePrefix ?? GetDefaultNamespacePrefix() + '.' + defaultName;
            this.OutputDir = outputDir ?? defaultName;
            this.ContractsDir = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!,
                contractsDir ?? "Assets"
            );
        }

        public override string ToString() => JsonConvert.SerializeObject(this);

        public static Config GetConfig()
        {
            var path = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!,
                "codegen.config.json"
            );
            return File.Exists(path) ? ReadFromJsonFile(path) : new Config();
        }

        private static Config ReadFromJsonFile(string configPath)
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var config =
                    JsonConvert.DeserializeObject<Config>(json, new ConfigDeserializer())
                    ?? throw new InvalidOperationException(
                        $"Failed to create {nameof(Config)} object."
                    );
                return config;
            }
            catch (InvalidCodeGenConfigurationException)
            {
                throw;
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidCodeGenConfigurationException(
                    message: ex.Message,
                    key: ex.Path,
                    innerException: ex
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    message: "Unexpected exception occured while reading codegen.config.json",
                    innerException: ex
                );
            }
        }

        private static string? GetDotnetPath()
        {
            if (File.Exists("dotnet"))
                return Path.GetFullPath("dotnet");
            if (File.Exists("dotnet.exe"))
                return Path.GetFullPath("dotnet.exe");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (
                    Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator)
                    ?? Array.Empty<string>()
                )
                    .Select(path => Path.Combine(path, "dotnet.exe"))
                    .FirstOrDefault(File.Exists);
            }

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
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var dotnet = '/' + string.Join('/', stdout.Trim().Split(';').Last().Split('/')[1..]);
            return dotnet != "/" ? dotnet : null;
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
                            ". -read "
                            + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                            + " UserShell",
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
                {
                    throw new InvalidOperationException(
                        message: "Could not get the default shell of the current user needed to"
                            + " find the dotnet executable required for Nethereum code"
                            + " generation."
                    );
                }

                defaultShell = output[dsclPrefix.Length..].Trim();
            }
            else
            {
                try
                {
                    defaultShell = File.ReadLines("/etc/passwd")
                        .First(line => line.StartsWith(Environment.UserName + ":"))
                        .Split(':')[6];
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException(
                        message: "Could not find the entry for the current user in /etc/passwd"
                            + " needed to find the dotnet executable required for Nethereum"
                            + " code generation.",
                        innerException: ex
                    );
                }
                catch (IndexOutOfRangeException ex)
                {
                    throw new InvalidOperationException(
                        message: "Could not get the default shell of the current user needed to"
                            + " find the dotnet executable required for Nethereum code"
                            + " generation in /etc/passwd.",
                        innerException: ex
                    );
                }

                if (defaultShell == "")
                {
                    throw new InvalidOperationException(
                        message: "The default shell of the current user needed to find the"
                            + " dotnet executable required for Nethereum code generation is"
                            + " empty in /etc/passwd."
                    );
                }
            }

            return defaultShell;
        }

        // csharpier-ignore
        private static readonly HashSet<string> CsharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double",
            "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float",
            "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is",
            "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte",
            "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
            "ushort", "using", "virtual", "void", "volatile", "while"
        };

        private static bool ValidateNamespacePrefix(string nsPrefix) =>
            nsPrefix == SanitizeNamespacePrefix(nsPrefix);

        private static string GetDefaultNamespacePrefix() =>
            SanitizeNamespacePrefix(Application.productName);

        private static string SanitizeNamespacePrefix(string nsPrefix)
        {
            var result = SanitizeLeadingDots(
                SanitizeTrailingDots(
                    string.Join(
                        "",
                        nsPrefix.Select(c =>
                        {
                            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                            return
                                cat == UnicodeCategory.UppercaseLetter
                                || cat == UnicodeCategory.LowercaseLetter
                                || cat == UnicodeCategory.TitlecaseLetter
                                || cat == UnicodeCategory.ModifierLetter
                                || cat == UnicodeCategory.OtherLetter
                                || cat == UnicodeCategory.LetterNumber
                                || cat == UnicodeCategory.SpacingCombiningMark
                                || cat == UnicodeCategory.DecimalDigitNumber
                                || cat == UnicodeCategory.ConnectorPunctuation
                                || c == '.'
                                ? c.ToString()
                                : "_";
                        })
                    )
                )
            );

            var firstLetterCat = CharUnicodeInfo.GetUnicodeCategory(result[0]);
            if (
                firstLetterCat != UnicodeCategory.UppercaseLetter
                && firstLetterCat != UnicodeCategory.LowercaseLetter
                && firstLetterCat != UnicodeCategory.TitlecaseLetter
                && firstLetterCat != UnicodeCategory.ModifierLetter
                && firstLetterCat != UnicodeCategory.OtherLetter
                && firstLetterCat != UnicodeCategory.LetterNumber
            )
            {
                result = "_" + result;
            }

            result = string.Join(
                ".",
                result
                    .Split(".")
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .Select(part => CsharpKeywords.Contains(part) ? "@" + part : part)
            );

            return result;
        }

        private static string SanitizeLeadingDots(string str) =>
            str != string.Empty
                ? str[0] == '.'
                    ? "_" + SanitizeLeadingDots(str[1..])
                    : str
                : string.Empty;

        private static string SanitizeTrailingDots(string str) =>
            str != string.Empty
                ? str[^1] == '.'
                    ? SanitizeTrailingDots(str[..^1]) + "_"
                    : str
                : string.Empty;
    }
}

internal class InvalidCodeGenConfigurationException : Exception
{
    public string? PropertyKey { get; }
    private string? PropertyValue { get; }

    public InvalidCodeGenConfigurationException(string message, [Optional] Exception innerException)
        : base(message, innerException) { }

    public InvalidCodeGenConfigurationException(
        string message,
        string? key,
        [Optional] string? value,
        [Optional] Exception innerException
    )
        : base(message, innerException)
    {
        this.PropertyKey = key;
        this.PropertyValue = value;
    }

    public override string ToString()
    {
        if (this.PropertyKey == null)
            return base.ToString();

        var msg = $"Invalid Unithereum CodeGen config: {this.PropertyKey}.";

        if (this.PropertyValue != null)
        {
            msg += $" ({this.PropertyValue})";
        }

        return msg + $" {this.Message}\n" + base.ToString();
    }
}

internal class ConfigDeserializer : JsonConverter
{
    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var target = new JObject();

        foreach (var jToken in JToken.Load(reader).Children())
        {
            var property = (JProperty)jToken;
            var pascalCasePropertyName = char.ToUpper(property.Name[0]) + property.Name[1..];

            if (objectType.GetProperty(pascalCasePropertyName) == null)
            {
                Debug.LogWarning(
                    $"Unithereum CodeGen: invalid config property {property.Name}. "
                        + "Unknown config type."
                );
                continue;
            }

            property = new JProperty(pascalCasePropertyName, property.Value);
            target.Add(property);
        }

        return target.ToObject(objectType);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var o = (JObject)JToken.FromObject(value!);
        o.WriteTo(writer);
    }

    public override bool CanConvert(Type objectType) => true;
}
