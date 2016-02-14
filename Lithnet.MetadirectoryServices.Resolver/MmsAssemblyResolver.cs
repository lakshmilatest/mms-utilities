﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace Lithnet.MetadirectoryServices.Resolver
{
    public static class MmsAssemblyResolver
    {
        public static void RegisterResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            EventLog eventLog = new EventLog("Application");
            eventLog.Source = "Lithnet.MetadirectoryServices.Resolver";
            Assembly resolvedAssembly = null;

            if (!args.Name.StartsWith("Microsoft.MetadirectoryServicesEx", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

#if DEBUG
            eventLog.WriteEntry(string.Format("Lithnet.MetadirectoryServices.Resolver searching for: {0}", args.Name), EventLogEntryType.Information, 1);
#endif

            string path = GetMmsOverridePathFromRegistry();

            if (path != null)
            {
                if (TryGetAssembly(path, out resolvedAssembly))
                {
                    return resolvedAssembly;
                }
            }

            path = GetMmsPathFromRegistry();

            if (path != null)
            {
                if (TryGetAssembly(path, out resolvedAssembly))
                {
                    return resolvedAssembly;
                }
            }

            path = GetMmsPathFromEntryAssemblyFolder();

            if (path != null)
            {
                if (TryGetAssembly(path, out resolvedAssembly))
                {
                    return resolvedAssembly;
                }
            }

#if DEBUG
            eventLog.WriteEntry(string.Format("Lithnet.MetadirectoryServices.Resolver could not find Microsoft.MetadirectoryServicesEx.dll"), EventLogEntryType.Error, 3);
#endif

            throw new FileNotFoundException(@"The Microsoft.MetadirectoryServicesEx.dll file could not be found on this system. Ensure the FIM synchronization service has been installed, or the DLL registered in the GAC");
        }

        private static bool TryGetAssembly(string path, out Assembly mmsAssembly)
        {
            EventLog eventLog = new EventLog("Application");
            eventLog.Source = "Lithnet.MetadirectoryServices.Resolver";

            mmsAssembly = null;

            if (File.Exists(path))
            {
                mmsAssembly = Assembly.LoadFrom(path);

                if (mmsAssembly != null)
                {
#if DEBUG
                    eventLog.WriteEntry(string.Format("Lithnet.MetadirectoryServices.Resolver found: {0}", mmsAssembly.FullName), EventLogEntryType.Information, 2);
#endif
                    return true;
                }
            }

            return false;
        }

        private static string GetMmsPathFromRegistry()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\FIMSynchronizationService\Parameters", false);

            if (key != null)
            {
                string path = key.GetValue("Path", null) as string;

                if (path != null)
                {
                    path = Path.Combine(path, "bin\\assemblies\\Microsoft.MetadirectoryServicesEx.dll");
                    return path;
                }
            }

            return null;
        }

        private static string GetMmsOverridePathFromRegistry()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Lithnet\MmsResolver", false);

            if (key != null)
            {
                string path = key.GetValue("MmsPath", null) as string;

                if (path != null)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        private static string GetMmsPathFromEntryAssemblyFolder()
        {
            Assembly mmsResolverAssembly = Assembly.GetCallingAssembly();
            string localPath = new Uri(mmsResolverAssembly.CodeBase).LocalPath;

            localPath = Path.Combine(localPath, "Microsoft.MetadirectoryServicesEx.dll");

            return localPath;
        }
    }
}
