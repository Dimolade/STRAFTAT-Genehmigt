
# Genehmigt
A Mod Library to make adding Weapon's compatable and simple. <br><br>

# Weapons
The Weapon System in <b>STRAFTAT</b> is great. <br>
The system i've made to add Weapon's is relatively simple. <br><br>

### BepInEx
For the BepInEx class, you must add ```"dimolade.libraries.Genehmigt"``` as a Hard Dependency. Like this: <br>
````cs
[BepInDependency("dimolade.libraries.Genehmigt", BepInDependency.DependencyFlags.HardDependency)]
````
This is so that your Mod runs after <b>Genehmigt</b> initializes.
### Weapon Creation
In you'r BepInEx class, you should store a GameObject named as you'r Weapon.
It is recommended to capture the folder of the dll for AssetBundle's (which are highly recommended) like this: ``string dllFolder = Path.GetDirectoryName(Info.Location);``<br> <br>
In you'r ```Awake()``` function, Create the Weapon. <br>
First, make a <b>GenehmigtWeapon</b> Instance. <br>
````cs
// Create TestWeapon
GenehmigtWeapon GW = new GenehmigtWeapon();
````
<br>

Now you can edit the GenehmigtWeapon to you'r needs. <br>
For Example, setting the Name.

```cs
GW.Name = "My Awesome Weapon";
```
<br>

### Adding a Mesh
To get the parent of the Mesh, you simply just need to get the child of a child.
```cs
Transform MeshHolder = testWeapon.transform.GetChild(0).GetChild(0);
```
<br>

You can simply load an AssetBundle with a Mesh and parent it to the MeshHolder. <br>
Like this: <br>

```cs
MyAwesomeWeaponMesh.SetParent(MeshHolder);
// here you should change its position, settings, stuff like that. (localPosition and stuff).
```
<b>NOTE:</b> Do NOT Instantiate any of these Prefabs. <br>

### Programming a Weapon
Wow, now you'r Gun is in the Game! What's that? You'r Gun doesn't pick up and instead gives out a Null Reference Exception? Not to worry, this is simply because STRAFTAT require's a Weapon class on the Weapon. (Weapon Root not MeshHolder) <br><br>

Now technically you could just use the ```Gun``` class change variables and call it a Day, which is true. However i haven't experimented with this. If you have feel free to send me a DM on Discord if there were any Complications! <br><br>

Now to make a functioning Weapon Class, its really simple. <br>
```cs
public class MyAwesomeWeapon : Weapon {
	bool hasInit = false;

	void Init()
	{
		if (hasInit) return;
		// if you need to get something from the AssetBundle of a Mesh you loaded, do it here. Unity adds children at the End of a Frame.
	}

	void Update()
	{
		if (base.gameObject.layer == 7) return; // Pretty sure this means the Weapon is on the Ground.

		WeaponUpdate(); // Part of the Weapon class.

		if (CanFireSingle()) // if we can Fire
		{
			// Call Fire!
			Fire();
		}
	}

	void Fire()
	{
		// Logic to Fire your Weapon
		PauseManager.Instance.WriteOfflineLog("Wow! You fired My Awesome Weapon!");
	}

	private bool lastFirePressed = false;

	// a simple function which returns if the Player can fire. (semi fire) you can add a debounce to this for a small cooldown
    bool CanFireSingle()
    {
        bool rightTrigger = (invertFire ? fire2 : fire1).ReadValue<float>() > 0.1f && inRightHand;
        bool leftTrigger  = (invertFire ? fire1 : fire2).ReadValue<float>() > 0.1f && inLeftHand;

        bool firePressed = rightTrigger || leftTrigger;

        bool canFire = firePressed && !lastFirePressed;

        lastFirePressed = firePressed;

        return canFire;
    }
}
```
<br>
That is all. Currently Mod RPC's do NOT work. We plan on working on a solution for this. Spawn will only work on the Host. Apropro Spawn

### Spawning

To Spawn a GameObject (which should be from an AssetBundle) you must add it to the PrefabPool first. You do this by using HarmonyLib to patch a function, like this:
```cs
[HarmonyPatch(typeof(NetworkManager))]
internal static class MyAwesomeNetworkPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void RefreshPrefabs(NetworkManager __instance) {
        __instance.SpawnablePrefabs.AddObject(MyAwesomeWeaponReference.GetComponent<NetworkObject>());
    }
}
```
<br>
Now you can simply call Spawn on an Instantiated Prefab!
<br><br>
That is all Folks, Map support May be Added in the Future. No promises.