using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unithereum.CodeGen
{
    public class UnithereumCodeGenProcessor : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 0;

        /// <summary>
        /// Detect .abi and .bin files on import and generate code.
        /// </summary>
        public void OnPreprocessAsset()
        {
            var changedPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, assetPath);
            FindAbiBinPairAndGenerate(changedPath);
        }

        /// <summary>
        ///     Search for all .abi and .bin files in the Asset/ directory and force import them.
        /// </summary>
        [MenuItem("Unithereum/Regenerate All...")]
        public static void RegenerateAllMenu()
        {
            var config = GetConfig();
            if (config is null) return;

            var codeGenPath = Path.Combine(Application.dataPath, config.OutputDir);

            if (!EditorUtility.DisplayDialog(
                    "Regenerate Code For All Contracts",
                    "This will search for all .abi and .bin files under the Unity Project directory, " +
                    "import, and regenerate code for the contracts.\n\nTHIS WILL REMOVE ALL CONTENTS OF THE " +
                    $"{codeGenPath}/ DIRECTORY!\n\nProceed?",
                    "Regenerate",
                    "Cancel"
                )
            )
            {
                return;
            }

            if (Directory.Exists(codeGenPath)) Directory.Delete(codeGenPath, true);
            GenerateAll();

            EditorUtility.DisplayDialog(
                "Regeneration Complete",
                "Code for all available contracts has been regenerated.",
                "OK"
            );
        }

        [DidReloadScripts]
        public static void OnScriptsReloaded()
        {
            GetConfig();
        }

        private static Config? GetConfig()
        {
            try
            {
                return Config.GetConfig();
            }
            catch (InvalidCodeGenConfigurationException e)
            {
                if (e.PropertyKey != null)
                    Debug.LogError(e);
                else
                    Debug.LogWarning(e);
            }

            return null;
        }

        private static void GenerateAll()
        {
            foreach (var path in Directory.GetFiles(Path.GetDirectoryName(Application.dataPath)!, "*.abi",
                         SearchOption.AllDirectories))
                FindAbiBinPairAndGenerate(path);
        }

        private static void FindAbiBinPairAndGenerate(string path)
        {
            if (path.EndsWith(".abi", StringComparison.OrdinalIgnoreCase))
            {
                var binPath = Path.ChangeExtension(path, ".bin");
                Generate(path, File.Exists(binPath) ? binPath : null);
            }
            else if (path.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                var abiPath = Path.ChangeExtension(path, ".abi");
                if (File.Exists(abiPath)) Generate(abiPath, path);
            }
        }

        private static void Generate(string abiPath, string? binPath)
        {
            var config = GetConfig();
            if (config is null) return;

            var dotnet = config.DotnetPath;

            var assemblyDir = Path.GetDirectoryName(
                new Uri(typeof(UnithereumCodeGenProcessor).Assembly.CodeBase).LocalPath
            );
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
                throw new InvalidOperationException("dotnet tool restore failed: " + error);

            var codegenPath = Path.Combine(Application.dataPath, config.OutputDir);
            var codegenNamespace = config.NamespacePrefix;
            var assemblyDefinition = Path.Combine(codegenPath, codegenNamespace + ".asmdef");
            var cscDirectives = Path.Combine(codegenPath, "csc.rsp");

            // Suppress warnings in generated code
            if (!Directory.Exists(codegenPath))
                Directory.CreateDirectory(codegenPath);
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
                throw new InvalidOperationException(
                    "Failed to generate Nethereum contract service code: " + error);

            AssetDatabase.ImportAsset(
                Path.Combine(Path.GetFileName(Application.dataPath), config.OutputDir),
                ImportAssetOptions.ImportRecursive);
        }
    }
}
