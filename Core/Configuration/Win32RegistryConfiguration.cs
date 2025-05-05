using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

using CKAN.IO;
using CKAN.Games;

namespace CKAN.Configuration
{
    // DEPRECATED: We now use a JSON configuration file. This still exists to facilitate migration.
    //
    // N.B., you can resume using this version by changing the instance created in ServiceLocator.
    #if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    #endif
    public class Win32RegistryConfiguration : IConfiguration
    {
        private const           string CKAN_KEY           = @"HKEY_CURRENT_USER\Software\CKAN";
        private static readonly string CKAN_KEY_NO_PREFIX = StripPrefixKey(CKAN_KEY);

        private const           string authTokenKey         = CKAN_KEY + @"\AuthTokens";
        private static readonly string authTokenKeyNoPrefix = StripPrefixKey(authTokenKey);

        private static readonly string defaultDownloadCacheDir =
            Path.Combine(CKANPathUtils.AppDataPath, "downloads");

        public string? DownloadCacheDir
        {
            get => GetRegistryValue(@"DownloadCacheDir", defaultDownloadCacheDir);
            set
            {
                if (value == null || string.IsNullOrEmpty(value))
                {
                    DeleteRegistryValue(@"DownloadCacheDir");
                }
                else
                {
                    if (!Path.IsPathRooted(value))
                    {
                        value = Path.GetFullPath(value);
                    }
                    SetRegistryValue(@"DownloadCacheDir", value);
                }
            }
        }

        public long? CacheSizeLimit
        {
            get
            {
                var val = GetRegistryValue<string?>(@"CacheSizeLimit", null);
                return string.IsNullOrEmpty(val) ? null : Convert.ToInt64(val);
            }
            set
            {
                if (!value.HasValue)
                {
                    DeleteRegistryValue(@"CacheSizeLimit");
                }
                else
                {
                    SetRegistryValue(@"CacheSizeLimit", value.Value);
                }
            }
        }

        public int RefreshRate
        {
            get => GetRegistryValue(@"RefreshRate", 0);
            set
            {
                if (value <= 0)
                {
                    DeleteRegistryValue(@"RefreshRate");
                }
                else
                {
                    SetRegistryValue(@"RefreshRate", value);
                }
            }
        }

        private static int InstanceCount => GetRegistryValue(@"KSPInstanceCount", 0);

        public string? AutoStartInstance
        {
            get => GetRegistryValue(@"KSPAutoStartInstance", "");
            #pragma warning disable IDE0027
            set { SetAutoStartInstance(value ?? string.Empty); }
            #pragma warning restore IDE0027
        }

        public string? Language
        {
            get => GetRegistryValue<string?>("Language", null);
            set
            {
                if (value != null && Utilities.AvailableLanguages.Contains(value))
                {
                    SetRegistryValue("Language", value);
                }
            }
        }

        public Win32RegistryConfiguration()
        {
            ConstructKey(CKAN_KEY_NO_PREFIX);
        }

        private Tuple<string, string, string> GetInstance(int i)
        {
            return new Tuple<string, string, string>(
                GetRegistryValue("KSPInstanceName_" + i, string.Empty),
                GetRegistryValue("KSPInstancePath_" + i, string.Empty),
                GetRegistryValue("KSPInstanceGame_" + i, string.Empty)
            );
        }

        public void SetRegistryToInstances(SortedList<string, GameInstance> instances)
        {
            SetNumberOfInstances(instances.Count);

            int i = 0;
            foreach ((string name, GameInstance inst) in instances)
            {
                SetInstanceKeysTo(i++, name, inst);
            }
        }

        public IEnumerable<Tuple<string, string, string>> GetInstances()
            => Enumerable.Range(0, InstanceCount).Select(GetInstance).ToList();

        public bool TryGetAuthToken(string host,
                                    [NotNullWhen(returnValue: true)] out string? token)
        {
            try
            {
                token = Microsoft.Win32.Registry.GetValue(authTokenKey, host, null) as string;
                return !string.IsNullOrEmpty(token);
            }
            catch
            {
                // If GetValue threw SecurityException, IOException, or ArgumentException,
                // just report failure.
                token = "";
                return false;
            }
        }

        public IEnumerable<string> GetAuthTokenHosts()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(authTokenKeyNoPrefix);
            return key?.GetValueNames() ?? Array.Empty<string>();
        }

        public void SetAuthToken(string host, string? token)
        {
            ConstructKey(authTokenKeyNoPrefix);
            if (!string.IsNullOrEmpty(host))
            {
                if (string.IsNullOrEmpty(token))
                {
                    var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(authTokenKeyNoPrefix, true);
                    key?.DeleteValue(host);
                }
                else
                {
                    Microsoft.Win32.Registry.SetValue(authTokenKey, host, token);
                }
            }
        }

        /// <summary>
        /// Not implemented because the Windows registry is deprecated
        /// </summary>
        public string[] GetGlobalInstallFilters(IGame game) => Array.Empty<string>();
        /// <summary>
        /// Not implemented because the Windows registry is deprecated
        /// </summary>
        public void SetGlobalInstallFilters(IGame game, string[] value) { }

        /// <summary>
        /// Not implemented because the Windows registry is deprecated
        /// </summary>
        public string?[] PreferredHosts { get; set; } = Array.Empty<string?>();

        /// <summary>
        /// Not implemented because the Windows registry is deprecated
        /// </summary>
        public bool? DevBuilds { get; set; }

        #pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
        #pragma warning restore CS0067

        public static bool DoesRegistryConfigurationExist()
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(CKAN_KEY_NO_PREFIX);
            return key != null;
        }

        public static void DeleteAllKeys()
        {
            // This can fail if the key doesn't exist, but we don't really care...
            try {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(CKAN_KEY_NO_PREFIX);
            } catch { }
        }

        private static string StripPrefixKey(string keyname)
        {
            int firstBackslash = keyname.IndexOf(@"\");
            return firstBackslash < 0
                ? keyname
                : keyname[(1 + firstBackslash)..];
        }

        private static void ConstructKey(string whichKey)
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(whichKey);
            if (key == null)
            {
                Microsoft.Win32.Registry.CurrentUser.CreateSubKey(whichKey);
            }
        }

        private static void SetAutoStartInstance(string instanceName)
        {
            SetRegistryValue(@"KSPAutoStartInstance", instanceName ?? string.Empty);
        }

        private static void SetNumberOfInstances(int count)
        {
            SetRegistryValue(@"KSPInstanceCount", count);
        }

        private static void SetInstanceKeysTo(int instanceIndex, string name, GameInstance ksp)
        {
            SetRegistryValue(@"KSPInstanceName_" + instanceIndex, name);
            SetRegistryValue(@"KSPInstancePath_" + instanceIndex, ksp.GameDir());
        }

        private static void SetRegistryValue<T>(string key, T value) where T: notnull
        {
            Microsoft.Win32.Registry.SetValue(CKAN_KEY, key, value);
        }

        private static T GetRegistryValue<T>(string key, T defaultValue)
            => Microsoft.Win32.Registry.GetValue(CKAN_KEY, key, null) is T v
                ? v
                : defaultValue;

        private static void DeleteRegistryValue(string name)
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(CKAN_KEY_NO_PREFIX, true);
            key?.DeleteValue(name, false);
        }
    }
}
