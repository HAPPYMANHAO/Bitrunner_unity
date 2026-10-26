// Coplay v10 execute_code method body. Read-only; return the result object.
var report = new Dictionary<string, object>();

var inputMode = "Unknown";
try
{
    var property = typeof(PlayerSettings).GetProperty(
        "activeInputHandler",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    if (property == null)
    {
        property = typeof(PlayerSettings).GetProperty(
            "activeInputHandling",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    }

    if (property != null)
    {
        var value = property.GetValue(null, null);
        var numericValue = value == null ? -1 : Convert.ToInt32(value);
        inputMode = numericValue == 0 ? "Legacy Input Manager" :
            numericValue == 1 ? "Input System Package" :
            numericValue == 2 ? "Both" : "Unknown (" + numericValue + ")";
    }
}
catch (Exception exception)
{
    inputMode = "Unavailable: " + exception.GetType().Name;
}

if (inputMode.StartsWith("Unknown") || inputMode.StartsWith("Unavailable"))
{
    try
    {
        SerializedObject serializedSettings = null;
        var getSerializedObject = typeof(PlayerSettings).GetMethod(
            "GetSerializedObject",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getSerializedObject != null)
        {
            serializedSettings = getSerializedObject.Invoke(null, null)
                as SerializedObject;
        }

        if (serializedSettings == null)
        {
            var settingsAssets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/ProjectSettings.asset");
            if (settingsAssets.Length > 0)
            {
                serializedSettings = new SerializedObject(settingsAssets[0]);
            }
        }

        if (serializedSettings != null)
        {
            var serializedProperty =
                serializedSettings.FindProperty("activeInputHandler") ??
                serializedSettings.FindProperty("activeInputHandling");
            if (serializedProperty != null)
            {
                var numericValue = serializedProperty.intValue;
                inputMode = numericValue == 0 ? "Legacy Input Manager" :
                    numericValue == 1 ? "Input System Package" :
                    numericValue == 2 ? "Both" :
                    "Unknown (" + numericValue + ")";
            }
        }
    }
    catch (Exception exception)
    {
        inputMode = "Unavailable: " + exception.GetType().Name;
    }
}
report["activeInputHandling"] = inputMode;

var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
    "Packages/com.unity.inputsystem");
report["inputSystemPackage"] = packageInfo == null
    ? "not found"
    : packageInfo.name + "@" + packageInfo.version;

var actionPaths = new List<string>();
var actionGuids = AssetDatabase.FindAssets("t:InputActionAsset");
for (var index = 0; index < actionGuids.Length && index < 20; index++)
{
    actionPaths.Add(AssetDatabase.GUIDToAssetPath(actionGuids[index]));
}
report["inputActionAssetCount"] = actionGuids.Length;
report["inputActionAssets"] = actionPaths;

Type playerInputType = null;
foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
{
    playerInputType = assembly.GetType("UnityEngine.InputSystem.PlayerInput", false);
    if (playerInputType != null)
    {
        break;
    }
}

var playerInputs = new List<Dictionary<string, object>>();
if (playerInputType != null)
{
    var candidates = Resources.FindObjectsOfTypeAll(playerInputType);
    foreach (var candidate in candidates)
    {
        var component = candidate as Component;
        if (component == null || !component.gameObject.scene.IsValid() ||
            !component.gameObject.scene.isLoaded)
        {
            continue;
        }

        var item = new Dictionary<string, object>();
        item["path"] = AnimationUtility.CalculateTransformPath(
            component.transform, component.transform.root);
        item["root"] = component.transform.root.name;

        var actionsProperty = playerInputType.GetProperty("actions");
        var schemeProperty = playerInputType.GetProperty("currentControlScheme");
        var actions = actionsProperty == null
            ? null
            : actionsProperty.GetValue(component, null) as UnityEngine.Object;
        item["actions"] = actions == null ? null : actions.name;
        item["controlScheme"] = schemeProperty == null
            ? null
            : schemeProperty.GetValue(component, null);
        playerInputs.Add(item);
        if (playerInputs.Count >= 20)
        {
            break;
        }
    }
}
report["playerInputTypeAvailable"] = playerInputType != null;
report["playerInputs"] = playerInputs;

var legacyMatches = new List<string>();
var legacyCount = 0;
var scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
foreach (var guid in scriptGuids)
{
    var path = AssetDatabase.GUIDToAssetPath(guid);
    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
    var source = script == null ? null : script.text;
    if (String.IsNullOrEmpty(source) ||
        (!source.Contains("Input.GetKey") &&
         !source.Contains("Input.GetAxis") &&
         !source.Contains("Input.GetButton")))
    {
        continue;
    }

    legacyCount++;
    if (legacyMatches.Count < 20)
    {
        legacyMatches.Add(path);
    }
}
report["legacyInputCallScriptCount"] = legacyCount;
report["legacyInputCallScripts"] = legacyMatches;

return report;
