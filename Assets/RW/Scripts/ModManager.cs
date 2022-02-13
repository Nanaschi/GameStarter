/*
 * Copyright (c) 2020 Razeware LLC
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish,
 * distribute, sublicense, create a derivative work, and/or sell copies of the
 * Software in any work that is designed, intended, or marketed for pedagogical or
 * instructional purposes related to programming, coding, application development,
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works,
 * or sale is expressly withheld.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.AddressableAssets.ResourceLocators;

[RequireComponent(typeof(ReferenceLookupManager))]
public class ModManager : MonoBehaviour
{
    public ReferenceLookupManager lookupManager;
    //In future this can be changed to the Application.persistentDataPath to put this in Local App Data
    [Header("Mod Path : /Assets/")]
    public string path;

    [Header("Loaded Mods")]
    public List<ModInfo> mods = new List<ModInfo>(); //List of existing mods in directory
    public Dictionary<string, ModInfo> modDictionary = new Dictionary<string, ModInfo>(); //Maps mods to a name so it can be easily loaded

    [Header("Current State")]
    public string activatedMod = ""; //Currently loaded mod

    [Header("UI")]
    public Button buttonPrefab;
    public Transform buttonParent;

    //Calls these methods every time a new mod is selected
    private List<Action> modUpdateListeners = new List<Action>();

    //The buttons that will hold the different mod options
    private List<Button> buttons = new List<Button>();

    private void Start()
    {
        path = Application.dataPath + "/" + path;
        
        // 1
        Addressables.InitializeAsync().Completed += handle =>
        {
            // 2
            ModInfo defaultContent = new ModInfo
            {
                isDefault = true,
                locator = handle.Result,
                modAbsolutePath = "",
                modFile = null,
                modName = "Default"
            };

            // 3
            mods.Add(defaultContent);
            ReloadDictionary();
            activatedMod = "Default";
            LoadCurrentMod();
        };

        LoadMods();
    }

    public void RegisterListener(Action modChanged)
    {
        modUpdateListeners.Add(modChanged);
    }

    private async void LoadMods()
    {
        DirectoryInfo modDirectory = new DirectoryInfo(path);

        foreach (FileInfo file in modDirectory.GetFiles())
        {
            if (file.Extension == ".json")
            {
                // 1
                string modName = file.Name;
                modName = modName.Replace(".json", "");
                modName = modName.Replace("_", " ");
                modName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(modName.ToLower());

// 2
                IResourceLocator modLocator = await LoadCatalog(file.FullName);

// 3
                ModInfo mod = new ModInfo
                {
                    modFile = file,
                    modAbsolutePath = file.FullName,
                    modName = modName,
                    isDefault = false,
                    locator = modLocator
                };
                mods.Add(mod);
                ReloadDictionary();
            }
        }
    }


    private async Task<IResourceLocator> LoadCatalog (string path)
    {
        AsyncOperationHandle<IResourceLocator> operation = 
            Addressables.LoadContentCatalogAsync(path);
// Wait until the catalog file is loaded 
// then retrieve the IResourceLocator for this mod
        IResourceLocator modLocator = await operation.Task; 
        return modLocator;

    }

    public void ChangeMod (string newModName)
    {
        lookupManager.ClearLoadedGameObjects();
        activatedMod = newModName;
        LoadCurrentMod();
    }

    private void LoadCurrentMod ()
    {
        if (modDictionary.ContainsKey(activatedMod))
        {
            lookupManager.instances.Clear();
            for (int i = 0; i < lookupManager.requiredAssets.Count; i++)
            {
                lookupManager.instances.Add(
                    lookupManager.requiredAssets[i],
                    FindAssetInMod(
                        lookupManager.requiredAssets[i],
                        modDictionary[activatedMod]
                    )
                );
            }

            for (int i = 0; i < modUpdateListeners.Count; i++)
            {
                modUpdateListeners[i]();
            }
        }
    }

    public IResourceLocation FindAssetInMod (string key, ModInfo mod)
    {
        return null;
    }

    private void ReloadDictionary()
    {
        modDictionary.Clear();

        for (int i = 0; i < mods.Count; i++)
        {
            modDictionary.Add(mods[i].modName, mods[i]);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            GameObject.Destroy(buttons[i].gameObject);
        }

        buttons.Clear();

        foreach (ModInfo info in mods)
        {
            Button newButton = Instantiate(buttonPrefab, buttonParent);
            buttons.Add(newButton);

            newButton.onClick.AddListener(() =>
            {
                ChangeMod(info.modName);
            });

            newButton.GetComponentInChildren<Text>().text = info.modName;
        }
    }

}

//Struct to contain all mod information

[System.Serializable]
public struct ModInfo
{
    public string modName; //Name of Mod
    public string modAbsolutePath; //Absolute Path of Mod

    public FileInfo modFile; //File Information (file size etc.)

    public IResourceLocator locator; //Resource locator of the mod

    public bool isDefault; //Only the default mod pack will have this checked
}
