﻿using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.ObjectModel;
using DevComponents.DotNetBar;
using System.Windows.Forms;

namespace WzComparerR2.PluginBase
{
    public class PluginManager
    {
        /// <summary>
        /// 当执行FindWz函数时发生，用来寻找对应的Wz_File。
        /// </summary>
        internal static event FindWzEventHandler WzFileFinding;

        /// <summary>
        /// 为CharaSim组件提供全局的搜索Wz_File的方法。
        /// </summary>
        /// <param Name="Type">要搜索wz文件的Wz_Type。</param>
        /// <returns></returns>
        public static Wz_Node FindWz(Wz_Type type)
        {
            return FindWz(type, null);
        }

        public static Wz_Node FindWz(Wz_Type type, Wz_File sourceWzFile)
        {
            FindWzEventArgs e = new FindWzEventArgs(type) { WzFile = sourceWzFile };
            if (WzFileFinding != null)
            {
                WzFileFinding(null, e);
                if (e.WzNode != null)
                {
                    return e.WzNode;
                }
                if (e.WzFile != null)
                {
                    return e.WzFile.Node;
                }
            }
            return null;
        }

        /// <summary>
        /// 通过wz完整路径查找对应的Wz_Node，若没有找到则返回null。
        /// </summary>
        /// <param name="fullPath">要查找节点的完整路径，可用'/'或者'\'作分隔符，如"Mob/8144006.img/die1/6"。</param>
        /// <returns></returns>
        public static Wz_Node FindWz(string fullPath)
        {
            FindWzEventArgs e = new FindWzEventArgs() { FullPath = fullPath };
            if (WzFileFinding != null)
            {
                WzFileFinding(null, e);
                if (e.WzNode != null)
                {
                    return e.WzNode;
                }
                if (e.WzFile != null)
                {
                    return e.WzFile.Node;
                }
            }
            return null;
        }

        internal static string MainExecutorPath
        {
            get
            {
                var asmArray = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in asmArray)
                {
                    string asmName = asm.GetName().Name;
                    if (string.Equals(asmName, "WzComparerR2", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return asm.Location;
                    }
                }
                return "";
            }
        }

        internal static string[] GetPluginFiles()
        {
            List<string> fileList = new List<string>();
            string baseDir = Path.GetDirectoryName(MainExecutorPath);
            string pluginDir = Path.Combine(baseDir, "Plugin");
            if (Directory.Exists(pluginDir))
            {
                foreach (string file in Directory.GetFiles(pluginDir, "WzComparerR2.*.dll", SearchOption.AllDirectories))
                {
                    fileList.Add(file);
                }
            }
            else
            {
                Directory.CreateDirectory(pluginDir);
            }
            return fileList.ToArray();
        }

        internal static void LoadPlugin(AppDomain appDomain, string pluginFileName, PluginContext context)
        {
            try
            {
                //加载程序集
                var asmName = AssemblyName.GetAssemblyName(pluginFileName);
                Assembly asm = appDomain.Load(asmName);
                //寻找入口类
                Type baseType = typeof(PluginEntry);
                foreach (var type in asm.GetExportedTypes())
                {
                    if (type.IsSubclassOf(baseType) && !type.IsAbstract)
                    {
                        ConstructorInfo ctor = type.GetConstructor(new[] { typeof(PluginContext) });
                        if (ctor != null)
                        {
                            try
                            {
                                var instance = ctor.Invoke(new object[] { context });
                                loadedPlugins.Add(new PluginInfo()
                                {
                                    Assembly = asm,
                                    FileName = pluginFileName,
                                    Instance = instance as PluginEntry
                                });
                            }
                            catch //构造函数执行失败
                            {
                            }
                        }
                    }
                }
            }
            catch //加载程序集失败
            {
            }
        }

        internal static void LoadPlugin(PluginContext context)
        {
            string[] allFiles = GetPluginFiles();

            foreach (var file in allFiles)
            {
                LoadPlugin(AppDomain.CurrentDomain, file, context);
            }
        }

        internal static void PluginOnLoad()
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.Instance.OnLoad();
                }
                catch (Exception ex)
                {
                    MessageBoxEx.Show("插件初始化失败。\r\n" + ex.Message, plugin.Instance.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        internal static void PluginOnUnLoad()
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.Instance.OnUnload();
                }
                catch
                {
                }
            }
        }

        static List<PluginInfo> loadedPlugins = new List<PluginInfo>();
        static ReadOnlyCollection<PluginInfo> readonlyLoadedPlugins = new ReadOnlyCollection<PluginInfo>(loadedPlugins);
        internal static ReadOnlyCollection<PluginInfo> LoadedPlugins
        {
            get
            {
                return readonlyLoadedPlugins;
            }
        }
    }
}