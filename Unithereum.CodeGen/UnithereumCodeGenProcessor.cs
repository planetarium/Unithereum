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

        private static readonly Config Config = Config.GetConfig();

        /// <summary>
        /// Detect .abi and .bin files on import and generate code.
        /// </summary>
        public void OnPreprocessAsset()
        {
            var changedPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!,
                this.assetPath
            );
            if (changedPath.EndsWith(".abi", StringComparison.OrdinalIgnoreCase))
            {
                var binPath = Path.ChangeExtension(changedPath, ".bin");
                Generate(changedPath, File.Exists(binPath) ? binPath : null);
            }
            else if (changedPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            {
                var abiPath = Path.ChangeExtension(changedPath, ".abi");
                if (File.Exists(abiPath))
                    Generate(abiPath, changedPath);
            }
        }

        /// <summary>
        /// Search for all .abi and .bin files in the Asset/ directory and force import them.
        /// </summary>
        /// <remarks>As the <see cref="OnPreprocessAsset"/> function will generate code for imported .abi files,
        /// this function will regenerate code for all .abi files without explicitly calling <see cref="Generate"/>.
        /// </remarks>
        [MenuItem("Unithereum/Regenerate All...")]
        public static void RegenerateAllMenu()
        {
            if (
                !EditorUtility.DisplayDialog(
                    "Regenerate Code For All Contracts",
                    "This will search for all .abi and .bin files under the Asset/ directory, import, and"
                        + " regenerate code for the contracts.\n\nTHIS WILL REMOVE ALL CONTENTS OF THE"
                        + $" Assets/{Config.OutputDir}/ DIRECTORY!\n\nProceed?",
                    "Regenerate",
                    "Cancel"
                )
            )
            {
                return;
            }

            var codeGenPath = Path.Combine(Application.dataPath, Config.OutputDir);
            if (Directory.Exists(codeGenPath))
                Directory.Delete(codeGenPath, recursive: true);

            foreach (
                var path in Directory.GetFiles(
                    Application.dataPath,
                    "*.abi",
                    SearchOption.AllDirectories
                )
            )
            {
                var binPath = Path.ChangeExtension(path, ".bin");
                if (File.Exists(binPath))
                {
                    AssetDatabase.ImportAsset(
                        Path.Combine(
                            binPath[(Path.GetDirectoryName(Application.dataPath)!.Length + 1)..]
                        ),
                        ImportAssetOptions.ForceUpdate
                    );
                }

                AssetDatabase.ImportAsset(
                    Path.Combine(path[(Path.GetDirectoryName(Application.dataPath)!.Length + 1)..]),
                    ImportAssetOptions.ForceUpdate
                );
            }

            EditorUtility.DisplayDialog(
                "Regeneration Complete",
                "Code for all available contracts has been regenerated.",
                "OK"
            );
        }

        [DidReloadScripts]
        public static void OnScriptsReloaded()
        {
            if (!(Config.DotnetPath is null))
                return;
            Debug.LogWarning("dotnet not found in PATH, Nethereum code generation will not work.");
        }

        private static void Generate(string abiPath, string? binPath)
        {
            if (!(Config.DotnetPath is { } dotnet))
                return;

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
            {
                throw new InvalidOperationException("dotnet tool restore failed: " + error);
            }

            var codegenPath = Path.Combine(Application.dataPath, Config.OutputDir);
            var codegenNamespace = Config.NsPrefix;
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
            {
                throw new InvalidOperationException(
                    "Failed to generate Nethereum contract service code: " + error
                );
            }

            AssetDatabase.ImportAsset(
                Path.Combine(Path.GetFileName(Application.dataPath), Config.OutputDir),
                ImportAssetOptions.ImportRecursive
            );
        }
    }
}
