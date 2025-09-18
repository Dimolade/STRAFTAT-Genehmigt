using System;
using System.IO;
using UnityEngine;
using HarmonyLib;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Collections;
using System.Collections.Generic;
using ComputerysModdingUtilities;
using TMPro;
using HeathenEngineering.DEMO;
using HeathenEngineering.SteamworksIntegration;
using FishNet;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Serializing;
using LambdaTheDev.NetworkAudioSync;
using LambdaTheDev.NetworkAudioSync.Integrations.FishNet;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UI;

using Object = UnityEngine.Object;

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInPlugin("dimolade.libraries.Genehmigt", "Genehmigt", "1.1.0.0")]
public class GenehmigtLoader : BaseUnityPlugin
{
    private void Awake()
    {
        string dllPath = Info.Location;
        string dllFolder = Path.GetDirectoryName(dllPath);
        Genehmigt.Logger = base.Logger;
        Genehmigt.dllFolder = dllFolder;
        Genehmigt.Logger.LogInfo("Genehmigt says Haiiii :3");

        Genehmigt.weaponPhysMat = new PhysicMaterial();
        Genehmigt.weaponPhysMat.dynamicFriction = 0.51f;
        Genehmigt.weaponPhysMat.staticFriction = 0.52f;
        Genehmigt.weaponPhysMat.bounciness = 0.1f;
        Genehmigt.weaponPhysMat.frictionCombine = PhysicMaterialCombine.Average;
        Genehmigt.weaponPhysMat.bounceCombine = PhysicMaterialCombine.Average;

        GameObject Propeller = Resources.Load<GameObject>(SpawnerManager.WeaponsPath+"/Propeller");
        ItemBehaviour IB = Propeller.GetComponent<ItemBehaviour>();
        Genehmigt.DefaultCrosshair = IB.standCrosshair;
        Genehmigt.hitSurfaceClip = IB.hitSurfaceClip;
        Genehmigt.grabClip = IB.grabClip;
        Genehmigt.depopVFX = IB.depopVFX;
        Genehmigt.groundLayer = IB.groundLayer;

        var harmony = new Harmony("dimolade.harmony.Genehmigt");
        harmony.PatchAll();
    }
}

public static class Genehmigt {
    public static event Func<WeaponDropper, bool> OnWeaponTryDrop;
    public static event Func<ItemSpawner, bool> OnWeaponTrySpawn;
    public static Dictionary<string, GameObject> ModdedRandomWeaponPool;
    public static bool onlySpawnModded = false;
    public static List<GenehmigtWeapon> ModWeaponList = new List<GenehmigtWeapon>();
    public static List<GenehmigtMap> ModMapList = new List<GenehmigtMap>();
    public static ManualLogSource Logger;
    public static string dllFolder;

    // Weapon tings

    public static Sprite DefaultCrosshair;

	public static AudioClip hitSurfaceClip;

	public static AudioClip grabClip;

	public static GameObject depopVFX;

	public static LayerMask groundLayer;

    // Weapon Spawning

    public static bool InvokeOnWeaponTrySpawn(ItemSpawner wd)
    {
        bool result = true;
        if (OnWeaponTrySpawn != null)
        {
            foreach (Func<ItemSpawner, bool> subscriber in OnWeaponTrySpawn.GetInvocationList())
            {
                if (!subscriber(wd)) result = false;
            }
        }
        return result;
    }

    public static bool InvokeOnWeaponTryDrop(WeaponDropper wd)
    {
        bool result = true;
        if (OnWeaponTryDrop != null)
        {
            foreach (Func<WeaponDropper, bool> subscriber in OnWeaponTryDrop.GetInvocationList())
            {
                if (!subscriber(wd)) result = false;
            }
        }
        return result;
    }

    // Weapon Creation

    private static void LoadBundle(string bundlePath, out GameObject go)
    {
        go = null;

        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

        GameObject[] gameObjects = bundle.LoadAllAssets<GameObject>();
        if (gameObjects.Length > 0)
            go = gameObjects[0];

        bundle.Unload(false);
    }

    public static PhysicMaterial weaponPhysMat;

    private static GameObject LoadSample() // to replace abusing the propeller
    {
        GameObject sample;
        LoadBundle(Path.Combine(dllFolder, "genehmigt"), out sample);
        //transform
        sample.transform.position = Vector3.zero;
        sample.transform.localScale = new Vector3(2,2,2);
        //components
        sample.AddComponent<ItemBehaviour>();
        BoxCollider BC = sample.AddComponent<BoxCollider>();
        BC.size = new Vector3(0.13836f,0.13836f,0.13836f);
        sample.AddComponent<NetworkObject>();
        BC.material = weaponPhysMat; // dont judge me man :(, the propeller still has a purpose

        // Elbow Pivot Point
        Transform EPP = sample.transform.GetChild(0);
        EPP.gameObject.AddComponent<ElbowPivotPoint>();
        EPP.localPosition = new Vector3(0f,0f,-0.1685f);
        EPP.localScale = new Vector3(0.5f,0.5f,0.5f);

        // Aim Strafe Pivot
        Transform ASP = EPP.GetChild(0);
        ASP.gameObject.AddComponent<AimStrafePivot>();
        ASP.localPosition = new Vector3(0,0.283f,0.367f);

        // Grips
        Transform Grip0 = ASP.GetChild(0);
        Transform Grip1 = ASP.GetChild(1);

        void AddGrip(Transform G)
        {
            G.gameObject.AddComponent<Grip>();
        }

        AddGrip(Grip0);
        AddGrip(Grip1);

        Grip0.localPosition = new Vector3(0f,-0.2806f,-0.58f);
        Grip1.localPosition = new Vector3(0f,-0.2806f,-0.667f);

        return sample;
    }

    public static GameObject CreateWeapon(GenehmigtWeapon GW)
    {
        GameObject original = LoadSample();
        if (original == null)
        {
            Genehmigt.Logger.LogError("Failed to load Sample prefab!");
            return null;
        }

        GameObject PropBase = original;
        PropBase.layer = 8;

        // ItemBehaviour
        ItemBehaviour IB = PropBase.GetComponent<ItemBehaviour>();
        IB.weaponName = GW.Name;
        IB.heavy = GW.Heavy;
        IB.vertical = GW.Vertical;
        IB.aimWeapon = GW.Aim;
        IB.aimFOV = GW.AimFOV;
        IB.instantAimLens = GW.InstantAimLense;
        IB.standCrosshair = DefaultCrosshair;
        IB.sprintCrosshair = DefaultCrosshair;
        if (GW.AimCrosshair != null)
            IB.aimCrosshair = GW.AimCrosshair;
        if (GW.StandCrosshair != null)
            IB.standCrosshair = GW.StandCrosshair;
        if (GW.SprintCrosshair != null)
            IB.sprintCrosshair = GW.SprintCrosshair;

        // Stuff which might cause errors
        IB.camChildIndex = 8;
        IB.camChildIndexLeftHand = 8;
        IB.aimIndex = 0;
        IB.vfxAttachedOnGun = false;
        IB.hitSurfaceClip = hitSurfaceClip;
        IB.grabClip = grabClip;
        IB.depopVFX = depopVFX;
        IB.groundLayer = groundLayer;

        IB.ejectForce = GW.EjectForce;
        IB.torqueForce = GW.TorqueForce;
        IB.gravityAdded = GW.GravityAdded;
        if (GW.HitSurfaceClip != null)
            IB.hitSurfaceClip = GW.HitSurfaceClip;
        if (GW.PickupClip != null)
            IB.grabClip = GW.PickupClip;

        GW.WeaponObject = PropBase;
        GW.NO = PropBase.GetComponent<NetworkObject>();

        PropBase.name = GW.Name;
        ModWeaponList.Add(GW);
        Genehmigt.Logger.LogInfo("Made Genehmigt Weapon: "+GW.Name);

        return PropBase;
    }

    // Map creation

    public static void MakeMap(GenehmigtMap map)
    {
        ModMapList.Add(map);
    }
}

// this.allMaps[j] = map;
//			this.allMapsDict.Add(map.mapName, map);

[HarmonyPatch(typeof(UnityEngine.SceneManagement.SceneManager))]
internal static class SceneGetPatch
{
    [HarmonyPatch("GetSceneAt")]
    [HarmonyPrefix]
    public static bool AddMaps(int index, ref Scene __result) {
        foreach (GenehmigtMap GM in Genehmigt.ModMapList)
        {
            if (GM.Index == index)
            {
                __result = GM.Scene;
                return false;
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(MapsManager))]
internal static class MapManPatch
{
    [HarmonyPatch("InitMaps")]
    [HarmonyPostfix]
    public static void AddMaps(MapsManager __instance) {
        Map[] allMaps = new Map[(SceneManager.sceneCountInBuildSettings - 6)+Genehmigt.ModMapList.Count];
        int i2 = 0;
        for (int i = 0; i < __instance.allMaps.Length; i++)
        {
            allMaps[i] = __instance.allMaps[i];
            i2 = i;
        }
        i2++; // current index
        foreach (var reference in Genehmigt.ModMapList)
        {
            Genehmigt.Logger.LogInfo("Added Genehmigt Map to Maps: "+reference.Name);
            Map map = new Map {
				index = i2,
				mapName = reference.Name,
				isDlcExclusive = false,
				isAltMap = reference.Name.ToLower().EndsWith("_alt"),
				isSelected = false,
				isUnlocked = true,
				mapInstance = null
			};
            allMaps[i2] = map;
            __instance.allMapsDict.Add(map.mapName, map);

            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.mapInstance, __instance.standardMapParent.position, Quaternion.identity, __instance.standardMapParent);
			map.mapInstance = gameObject.GetComponent<MapInstance>();
			map.mapInstance.name = map.mapName;
			map.mapInstance.selected = map.isSelected;
			gameObject.SetActive(true);

            reference.Index = i2;
            i2++;
        }
        __instance.allMaps = allMaps;
        __instance.SortMapsFromMapInstanceName();
    }
}

// Add Weapons to Prefab Pool i dont even know
[HarmonyPatch(typeof(NetworkManager))]
internal static class NetworkManagerPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void RefreshPrefabs(NetworkManager __instance) {
        foreach (var reference in Genehmigt.ModWeaponList) {
            if (reference.NO != null)
                Genehmigt.Logger.LogInfo("Added Genehmigt Weapon to Prefab Pool: "+reference.Name);
                __instance.SpawnablePrefabs.AddObject(reference.NO);
        }
    }
}

// Make them Loadable through Resources
[HarmonyPatch(typeof(ResourcesAPI), "Load")]
class ResourceLoadPatch
{
    static bool Prefix(ref Object __result, string path, Type systemTypeInstance)
    {
        if (path.StartsWith(SpawnerManager.WeaponsPath) && systemTypeInstance == typeof(GameObject))
        {
            foreach (GenehmigtWeapon GW in Genehmigt.ModWeaponList)
            {
                if (path == SpawnerManager.WeaponsPath+"/"+GW.Name)
                {
                    __result = (Object)GW.WeaponObject;
                    Genehmigt.Logger.LogInfo("Tried Loading Genehmigt Weapon with Name: "+GW.Name);
                    return false;
                }
            }
        }
        else if (path.StartsWith("MapSprites/"))
        {
            foreach (GenehmigtMap GW in Genehmigt.ModMapList)
            {
                if (path == "MapSprites/"+GW.Name)
                {
                    __result = (Object)GW.Icon;
                    Genehmigt.Logger.LogInfo("Tried Loading Genehmigt Map's Icon with Name: "+GW.Name);
                    return false;
                }
            }
        }
        return true;
    }
}

// Make them random weapons
[HarmonyPatch(typeof(ResourcesAPI), "LoadAll")]
class ResourceLoadAllPatch
{
    static void Postfix(ref Object[] __result, string path, Type systemTypeInstance)
    {
        if (path.StartsWith(SpawnerManager.WeaponsPath) && systemTypeInstance == typeof(GameObject))
        {
            int i = 0;
            List<int> toSkip = new List<int>();

            // Replacing
            foreach (GameObject g in __result)
            {
                foreach (GenehmigtWeapon GW in Genehmigt.ModWeaponList)
                {
                    if (g.name == GW.Name)
                    {
                        Genehmigt.Logger.LogInfo("Tried Loading All doing Genehmigt Weapon with Name: "+GW.Name);
                        __result[i] = (Object)GW.WeaponObject;
                        toSkip.Add(i);
                    }
                }
                i++;
            }

            // Add modded ones
            List<Object> rl = new List<Object>(__result);
            i = 0;
            foreach (GenehmigtWeapon GW in Genehmigt.ModWeaponList)
            {
                // make sure to not add a replacing weapon
                if (toSkip.Contains(i)) continue;
                rl.Add((Object)GW.WeaponObject);
                i++;
            }

            __result = rl.ToArray();
        }
    }
}

public class GenehmigtMap
{
    public Scene Scene;
    public Texture2D Icon;
    public string Name;
    public int Index = 0;
}

public class GenehmigtWeapon
{
    public string Name = "GenehmigtWeapon";
    public GameObject WeaponObject;
    public NetworkObject NO;

    // No idea LOL
    public bool Heavy = false;
    public bool Vertical = false;

    // Aim
    public bool Aim = false;
    public float AimFOV = 75;
    public bool InstantAimLense = false;
    public Sprite AimCrosshair = null;
    public Sprite StandCrosshair = null;
    public Sprite SprintCrosshair = null;

    // Ejection
    public float EjectForce = 9f;
    public float TorqueForce = 11f;
    public float GravityAdded = 6.5f;

    // Pickup
    public AudioClip PickupClip = null;
    public AudioClip HitSurfaceClip = null;
}

[HarmonyPatch(typeof(WeaponDropper), "Spawn")]
public class GENEHMIGT_WeaponTryDrop
{
    public static bool Prefix(WeaponDropper __instance)
    {
        bool tr = Genehmigt.InvokeOnWeaponTryDrop(__instance);
        return tr; // if 1 returns false, stop running
    }
}

[HarmonyPatch(typeof(ItemSpawner), "Spawn")]
public class GENEHMIGT_WeaponTrySpawn
{
    public static bool Prefix(ItemSpawner __instance)
    {
        bool tr = Genehmigt.InvokeOnWeaponTrySpawn(__instance);
        return tr; // if 1 returns false, stop running
    }
}