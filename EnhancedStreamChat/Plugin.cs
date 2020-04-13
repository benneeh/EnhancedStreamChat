﻿using System;
using System.IO;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
using EnhancedStreamChat.Config;
using IPA.Old;

namespace EnhancedStreamChat
{
    public class Plugin : IPlugin
    {
        public static readonly string ModuleName = "Enhanced Stream Chat";
        public string Name => ModuleName;
        public string Version => "2.2.0";
        
        public static Plugin Instance { get; private set; }

        private ChatConfig ChatConfig;

        public static void Log(string text,
                [CallerFilePath] string file = "",
                [CallerMemberName] string member = "",
                [CallerLineNumber] int line = 0)
        {
            Console.WriteLine($"{ModuleName}::{Path.GetFileName(file)}->{member}({line}): {text}");
        }

        public void OnApplicationStart()
        {
            if (Instance != null) return;
            Instance = this;
            ChatConfig = ChatConfig.instance;

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }
        
        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name == "MenuCore")
            {
                ChatConfig.OnLoad();
                ChatConfig.Save(true);
            }
        }

        public void OnApplicationQuit()
        {
        }

        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            try
            {
                ChatHandler.Instance?.SceneManager_activeSceneChanged(from, to);
            }
            catch { }
        }
        
        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnFixedUpdate()
        {
        }
        
        public void OnUpdate()
        {
        }
    }
}
