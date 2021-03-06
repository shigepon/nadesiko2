﻿using System;
using System.Collections.Generic;
using System.Text;

using NakoPlugin;

namespace Libnako.NakoAPI
{
    /// <summary>
    /// プラグインをロードするクラス
    /// </summary>
    public class NakoPluginLoader
    {
        List<NakoPluginInfo> plugins = new List<NakoPluginInfo>();

        // プラグインインターフェイスのフルパスを調べる
        string INakoPluginsPath = typeof(INakoPlugin).FullName;

        /// <summary>
        /// プラグインを検索する
        /// </summary>
        /// <returns></returns>
        protected NakoPluginInfo[] FindPlugins()
        {
            // プラグインのあるパスを調べる
            // アプリケーションディレクトリ
            string basefile = 
                //System.Reflection.Assembly.GetCallingAssembly().Location;
                System.Reflection.Assembly.GetExecutingAssembly().Location;
            string appdir = System.IO.Path.GetDirectoryName(basefile);

            if (System.IO.Directory.Exists(appdir))
            {
                CheckDllFiles(appdir);
            }
            // プラグイン専用ディレクトリ
			string plugdir = appdir + System.IO.Path.DirectorySeparatorChar + "plug-ins";
            if (System.IO.Directory.Exists(plugdir))
            {
                CheckDllFiles(plugdir);
            }

            return plugins.ToArray();
        }

        void CheckDllFiles(string plugdir)
        {
            // プラグインフォルダのDLLを列挙
            string[] dlls = System.IO.Directory.GetFiles(plugdir, "*.dll");
            foreach (string dll in dlls)
            {
                // 例外をチェック
                string name = System.IO.Path.GetFileName(dll);
                name = name.ToLower();
                if (name == "libnako.dll") continue;
                if (name == "nakoplugin.dll") continue;
                // アセンブリを読み込む
                System.Reflection.Assembly asm;
                try
                {
                    asm = System.Reflection.Assembly.LoadFrom(dll);
                }
                catch (Exception e)
                {
                    throw new Exception("プラグイン読込みエラー。:" + dll + ":[" + e.GetType().Name + "]" + e.Message);
                }
                // 読み込んだアセンブリに含まれるクラスを取得する
                string curType = "*";
                try
                {
                    foreach (Type t in asm.GetTypes())
                    {
                        curType = t.Name;
                        if (t.IsClass && t.IsPublic && !t.IsAbstract &&
                            t.GetInterface(INakoPluginsPath) != null)
                        {
                            NakoPluginInfo info = new NakoPluginInfo();
                            info.Location = dll;
                            info.ClassName = t.FullName;
                            plugins.Add(info);
                        }
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException e)
                {
                    string s = "";
                    s += "{name:" + name + "}";
                    foreach (Exception e2 in e.LoaderExceptions)
                    {
                        s += e2.Message + ":";
                    }
                    throw new Exception("プラグイン読込みエラー。クラスの取得に失敗。:" + dll + ":" + curType + ":[" + e.GetType().Name + "] " + s);
                }
                catch (Exception e)
                {
                    throw new Exception("プラグイン読込みエラー。クラスの取得に失敗。:" + dll + ":" + curType + ":[" + e.GetType().Name + "] " + e.Message);
                }
            }
        }
        
        /// <summary>
        /// プラグインの取り込みを行う
        /// </summary>
        public void LoadPlugins()
        {
            NakoAPIFuncBank bank = NakoAPIFuncBank.Instance;
            NakoPluginInfo[] plugs = FindPlugins();
            foreach (NakoPluginInfo info in plugs)
            {
                if (!bank.PluginList.ContainsKey(info.ClassName))
                {
                    INakoPlugin p = info.CreateInstance();
                    bank.SetPluginInstance(p);
                    p.DefineFunction(bank);
                    bank.PluginList[info.ClassName] = p;
                }
            }
        }
    }
    /// <summary>
    /// プラグイン情報を表わすクラス
    /// </summary>
    public class NakoPluginInfo
    {
        /// <summary>
        /// プラグインのパス
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// プラグインのクラス名
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// インスタンスを生成する
        /// </summary>
        /// <returns></returns>
        public INakoPlugin CreateInstance()
        {
            if (Location == null || ClassName == null) return null;
            System.Reflection.Assembly asm =
                System.Reflection.Assembly.LoadFrom(this.Location);
            return (NakoPlugin.INakoPlugin)
                asm.CreateInstance(this.ClassName);
        }
    }
}
