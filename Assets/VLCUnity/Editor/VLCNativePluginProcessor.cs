#if UNITY_2018_1_OR_NEWER
#define UNITY_SUPPORTS_BUILD_REPORT
#endif
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.Build;
using UnityEditor.Callbacks;

#if UNITY_SUPPORTS_BUILD_REPORT
using UnityEditor.Build.Reporting;
#endif

#if UNITY_IPHONE
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif

namespace Videolabs.VLCUnity.Editor
{
    public class VLCNativePluginProcessor :
#if UNITY_SUPPORTS_BUILD_REPORT
        IPreprocessBuildWithReport
#else
        IPreprocessBuild
#endif
    {
        public int callbackOrder { get { return 0; } }

        const string SDK = "sdk";
        string[] UWP_ARCH = { "x86_64", "ARM64" };

        const string UWP_PATH = "VLCUnity/Plugins/WSA/UWP";
        const string WINDOWS_PATH = "VLCUnity/Plugins/Windows/x86_64";
        const string ANDROID_PATH = "VLCUnity/Plugins/Android/libs";
        const string MACOS_PATH  = "VLCUnity/Plugins/MacOS";
        const string IOS_PATH = "VLCUnity/Plugins/iOS/";

#if UNITY_SUPPORTS_BUILD_REPORT
        public void OnPreprocessBuild(BuildReport report)
        {
            OnPreprocessBuild(report.summary.platform, report.summary.outputPath);
        }
#endif

        public void OnPreprocessBuild(BuildTarget target, string path)
        {
            ConfigureUWPNativePlugins();
            ConfigureWindowsNativePlugins();
            ConfigureAndroidNativePlugins();
            ConfigureLibVLCSharp(target, path);
        }

        static void ConfigureLibVLCSharp(BuildTarget buildTarget, string path)
        {
            var libvlcsharpDlls = PluginImporter.GetAllImporters().Where(pi => pi.assetPath.EndsWith("LibVLCSharp.dll")).ToList();
         
            foreach(var pi in libvlcsharpDlls)
            {
                var dirty = false;
                
                if(pi.assetPath.Contains(IOS_PATH))
                {
                    if(pi.GetCompatibleWithAnyPlatform() || pi.GetCompatibleWithEditor() || !pi.GetCompatibleWithPlatform(BuildTarget.iOS))
                    {
                        pi.SetCompatibleWithAnyPlatform(false);
                        pi.SetCompatibleWithEditor(false);
                        pi.SetCompatibleWithPlatform(BuildTarget.iOS, true);
                        dirty = true;
                    }
                }
                else
                {
                    if(!pi.GetCompatibleWithAnyPlatform() || !pi.GetCompatibleWithEditor() || pi.GetCompatibleWithPlatform(BuildTarget.iOS))
                    {
                        pi.SetCompatibleWithAnyPlatform(true);
                        pi.SetCompatibleWithEditor(true);
                        pi.SetCompatibleWithPlatform(BuildTarget.iOS, false);
                        dirty = true;
                    }
                }

                if(dirty)
                {
                    pi.SaveAndReimport();
                }
            }
        }

        void ConfigureWindowsNativePlugins()
        {
            PluginImporter[] importers = PluginImporter.GetAllImporters();
            foreach (PluginImporter pi in importers)
            {
                if(!pi.isNativePlugin) continue;

                if(!pi.assetPath.Contains(WINDOWS_PATH)) continue;
                // pi.ClearSettings();

                var dirty = false;

                if(pi.GetCompatibleWithAnyPlatform() || !pi.GetCompatibleWithEditor() || !pi.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64))
                {
                    pi.SetCompatibleWithAnyPlatform(false);
                    pi.SetCompatibleWithEditor(true);
                    pi.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);

                    dirty = true;
                }

                var cpu = pi.GetPlatformData(BuildTarget.StandaloneWindows64, "CPU");
                if(cpu != "x86_64")
                {
                    pi.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "x86_64");
                    dirty = true;
                }

                if(dirty)
                {
                    pi.SaveAndReimport();
                }
            }
        }

        void ConfigureUWPNativePlugins()
        {
            PluginImporter[] importers = PluginImporter.GetAllImporters();
            foreach (PluginImporter pi in importers)
            {
                if(!pi.isNativePlugin) continue;

                if(!pi.assetPath.Contains(UWP_PATH)) continue;

                var isX64 = pi.assetPath.Contains($"{UWP_PATH}/{UWP_ARCH[0]}");

                // pi.ClearSettings();
                var dirty = false;
                if(pi.GetCompatibleWithAnyPlatform() || pi.GetCompatibleWithEditor() || !pi.GetCompatibleWithPlatform(BuildTarget.WSAPlayer))
                {
                    pi.SetCompatibleWithAnyPlatform(false);
                    pi.SetCompatibleWithEditor(false);
                    pi.SetCompatibleWithPlatform(BuildTarget.WSAPlayer, true);

                    dirty = true;
                }

                var cpu = pi.GetPlatformData(BuildTarget.WSAPlayer, "CPU");

                if(isX64)
                {
                    if(cpu != "X64")
                    {
                        pi.SetPlatformData(BuildTarget.WSAPlayer, "CPU", "X64");
                        dirty = true;
                    }
                }
                else
                {
                    if(cpu != "ARM64")
                    {
                        pi.SetPlatformData(BuildTarget.WSAPlayer, "CPU", "ARM64");
                        dirty = true;
                    }
                }

                if(dirty)
                {
                    pi.SaveAndReimport();
                }
            }
        }

        void ConfigureAndroidNativePlugins()
        {
            PluginImporter[] importers = PluginImporter.GetAllImporters();
            foreach (PluginImporter pi in importers)
            {
                if(!pi.isNativePlugin) continue;

                if(!pi.assetPath.Contains(ANDROID_PATH)) continue;

                var dirty = false;
                if(pi.GetCompatibleWithAnyPlatform() || pi.GetCompatibleWithEditor() || !pi.GetCompatibleWithPlatform(BuildTarget.Android))
                {
                    pi.SetCompatibleWithAnyPlatform(false);
                    pi.SetCompatibleWithEditor(false);
                    pi.SetCompatibleWithPlatform(BuildTarget.Android, true);

                    dirty = true;
                }

                if(pi.assetPath.Contains($"{ANDROID_PATH}/armeabi-v7a/"))
                {
                    if(pi.GetPlatformData(BuildTarget.Android, "CPU") != "ARMv7")
                    {
                        pi.SetPlatformData(BuildTarget.Android, "CPU", "ARMv7");
                        dirty = true;
                    }
                }
                else if(pi.assetPath.Contains($"{ANDROID_PATH}/arm64-v8a/"))
                {
                    if(pi.GetPlatformData(BuildTarget.Android, "CPU") != "ARM64")
                    {
                        pi.SetPlatformData(BuildTarget.Android, "CPU", "ARM64");
                        dirty = true;
                    }
                }
                else if(pi.assetPath.Contains($"{ANDROID_PATH}/x86/"))
                {
                    if(pi.GetPlatformData(BuildTarget.Android, "CPU") != "X86")
                    {
                        pi.SetPlatformData(BuildTarget.Android, "CPU", "X86");
                        dirty = true;
                    }
                }
                else if(pi.assetPath.Contains($"{ANDROID_PATH}/x86_64/"))
                {
                    if(pi.GetPlatformData(BuildTarget.Android, "CPU") != "X86_64")
                    {
                        pi.SetPlatformData(BuildTarget.Android, "CPU", "X86_64");
                        dirty = true;
                    }
                }

                if(dirty)
                {
                    pi.SaveAndReimport();
                }
            }
        }

        internal static void CopyAndReplaceDirectory(string srcPath, string dstPath)
        {
            if (Directory.Exists(dstPath))
                Directory.Delete(dstPath);
            if (File.Exists(dstPath))
                File.Delete(dstPath);

            Directory.CreateDirectory(dstPath);

            foreach (var file in Directory.GetFiles(srcPath))
                File.Copy(file, Path.Combine(dstPath, Path.GetFileName(file)));

            foreach (var dir in Directory.GetDirectories(srcPath))
                CopyAndReplaceDirectory(dir, Path.Combine(dstPath, Path.GetFileName(dir)));
        }

        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget == BuildTarget.StandaloneOSX || buildTarget == BuildTarget.iOS)
            {
                OnPostprocessBuildMac(buildTarget, path);
                OnPostprocessBuildiPhone(path);
            }

            ConfigureLibVLCSharp(buildTarget, path);
        }

        internal static void OnPostprocessBuildMac(BuildTarget buildTarget, string path)
        {
            PluginImporter[] importers = PluginImporter.GetAllImporters();
            var isArm64Host = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

            foreach (PluginImporter pi in importers)
            {
                if(!pi.isNativePlugin) continue;

                if(!pi.assetPath.Contains(MACOS_PATH)) continue;
                // pi.ClearSettings();

                var dirty = false;

                if(pi.GetCompatibleWithAnyPlatform() || !pi.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX))
                {
                    pi.SetCompatibleWithAnyPlatform(false);
                    pi.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);

                    dirty = true;
                }

                // AnyCPU / macOS universal binary is not yet supported.

                var isEditorCompatible = pi.GetCompatibleWithEditor();
                if(pi.assetPath.Contains($"{MACOS_PATH}/ARM64/"))
                {
                    if(isArm64Host)
                    {
                        if(!isEditorCompatible)
                        {
                            pi.SetCompatibleWithEditor(true);
                            dirty = true;
                        }
                    }
                    else
                    {
                        if(isEditorCompatible)
                        {
                            pi.SetCompatibleWithEditor(false);
                            dirty = true;
                        }
                    }
                    if(pi.GetPlatformData(BuildTarget.StandaloneOSX, "CPU") != "ARM64")
                    {
                        pi.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "ARM64");
                        dirty = true;
                    }
                }
                else if(pi.assetPath.Contains($"{MACOS_PATH}/x86_64/"))
                {
                    if(!isArm64Host)
                    {
                        if(!isEditorCompatible)
                        {
                            pi.SetCompatibleWithEditor(true);
                            dirty = true;
                        }
                    }
                    else
                    {
                        if(isEditorCompatible)
                        {
                            pi.SetCompatibleWithEditor(false);
                            dirty = true;
                        }
                    }
                    if(pi.GetPlatformData(BuildTarget.StandaloneOSX, "CPU") != "x86_64")
                    {
                        pi.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "x86_64");
                        dirty = true;
                    }
                }

                if(dirty)
                {
                    pi.SaveAndReimport();
                }
            }

            if(path.EndsWith(".app") || buildTarget != BuildTarget.StandaloneOSX)
            {
                // "Create XCode Project" is unchecked
                return;
            }

            // XCode patching
            var projectPath = Path.Combine(path, Path.GetFileName(path) + ".xcodeproj", "project.pbxproj");

            string originalPbxprojContent = File.ReadAllText(projectPath);

            Regex regex = new Regex(@"VALID_ARCHS\s*=\s*([^;]+)");
            Match match = regex.Match(originalPbxprojContent);

            bool appleSiliconBuild = false;

            if (match.Success)
            {
                string arch = match.Groups[1].Value;
                if(arch == "arm64")
                {
                    appleSiliconBuild = true;
                }
                else if(arch == "x86_64")
                {
                    appleSiliconBuild = false;
                }
                else // likely "arm64 x86_64"
                {
                    Debug.LogError("Universal macOS binary is not yet supported, reach out on our gitlab for updates");
                }
            }
            else
            {
                Debug.LogError("No CPU target found while parsing the XCode project file.");
            }

            string pattern = @"(path\s*=\s*""[^""]*/Plugins/)(ARM64/|x86_64/)?([^""]+\.dylib)"";";
            string replacement;
            if (appleSiliconBuild)
            {
                replacement = @"$1ARM64/$3"";";
            }
            else
            {
                replacement = @"$1x86_64/$3"";";
            }
            string modifiedContent = Regex.Replace(originalPbxprojContent, pattern, replacement);
            File.WriteAllText(projectPath, modifiedContent);
        }

        internal static void AddIOSPlugin(PBXProject proj, string target, string plugin)
        {
//          Debug.Log("BUILD POSTPROCESS: adding plugin " + plugin);

          string fileName = Path.GetFullPath(plugin);
          string fileRef = proj.AddFile(fileName, plugin, PBXSourceTree.Source);
          proj.AddFileToEmbedFrameworks(target, fileRef);
        }

        internal static void OnPostprocessBuildiPhone(string path)
        {
            string projPath = PBXProject.GetPBXProjectPath(path);
            PBXProject proj = new PBXProject();

            proj.ReadFromString(File.ReadAllText(projPath));
            #if UNITY_2020_2_OR_NEWER
            string target = proj.GetUnityMainTargetGuid();
            #else
            string target = proj.TargetGuidByName("Unity-iPhone");
            #endif
            proj.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

            string frameworkTarget = proj.GetUnityFrameworkTargetGuid();
            proj.SetBuildProperty(frameworkTarget, "ENABLE_BITCODE", "NO");

            PluginImporter[] importers = PluginImporter.GetAllImporters();
            foreach (PluginImporter pi in importers)
            {
                if(!pi.isNativePlugin || !pi.assetPath.Contains(IOS_PATH))
                {
                    continue;
                }

                var isX64 = pi.assetPath.Contains("iOS/x86_64");

                // pi.ClearSettings();
                var dirty = false;
                if(pi.GetCompatibleWithAnyPlatform() || pi.GetCompatibleWithEditor() || !pi.GetCompatibleWithPlatform(BuildTarget.iOS))
                {
                    pi.SetCompatibleWithAnyPlatform(false);
                    pi.SetCompatibleWithEditor(false);
                    pi.SetCompatibleWithPlatform(BuildTarget.iOS, true);

                    dirty = true;
                }

                var cpu = pi.GetPlatformData(BuildTarget.iOS, "CPU");

                if(isX64)
                {
                    if(cpu != "X64")
                    {
                        pi.SetPlatformData(BuildTarget.iOS, "CPU", "X64");
                        dirty = true;
                    }
                }
                else
                {
                    if(cpu != "ARM64")
                    {
                        pi.SetPlatformData(BuildTarget.iOS, "CPU", "ARM64");
                        dirty = true;
                    }
                }

                if(dirty)
                {
                    pi.SaveAndReimport();
                }

                // XCode patching
                if((PlayerSettings.iOS.sdkVersion == iOSSdkVersion.DeviceSDK && !isX64)
                    || (PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK && isX64))
                {
                    AddIOSPlugin(proj, target, pi.assetPath);
                }
            }
            File.WriteAllText(projPath, proj.WriteToString());
        }
    }
}