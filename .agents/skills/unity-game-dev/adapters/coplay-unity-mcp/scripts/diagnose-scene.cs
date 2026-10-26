// Coplay v10 execute_code method body. Read-only; return the result object.
var report = new Dictionary<string, object>();
var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
report["activeScene"] = new Dictionary<string, object>
{
    { "name", activeScene.name },
    { "path", activeScene.path },
    { "loaded", activeScene.isLoaded },
    { "dirty", activeScene.isDirty }
};

var agents = new List<Dictionary<string, object>>();
var allAgents = Resources.FindObjectsOfTypeAll<UnityEngine.AI.NavMeshAgent>();
foreach (var agent in allAgents)
{
    if (!agent.gameObject.scene.IsValid() || !agent.gameObject.scene.isLoaded)
    {
        continue;
    }

    var item = new Dictionary<string, object>();
    item["name"] = agent.name;
    item["scene"] = agent.gameObject.scene.name;
    item["position"] = agent.transform.position.ToString("F3");
    item["enabled"] = agent.enabled;
    item["onNavMesh"] = agent.isOnNavMesh;
    item["pathStatus"] = agent.isOnNavMesh
        ? agent.pathStatus.ToString()
        : "Unavailable";
    agents.Add(item);
    if (agents.Count >= 20)
    {
        break;
    }
}
report["navMeshAgentCount"] = allAgents.Count(agent =>
    agent.gameObject.scene.IsValid() && agent.gameObject.scene.isLoaded);
report["navMeshAgents"] = agents;

var triggerCount = 0;
var triggersWithoutAttachedBody = 0;
var loadedColliders = Resources.FindObjectsOfTypeAll<Collider>();
foreach (var collider in loadedColliders)
{
    if (!collider.gameObject.scene.IsValid() ||
        !collider.gameObject.scene.isLoaded || !collider.isTrigger)
    {
        continue;
    }

    triggerCount++;
    if (collider.attachedRigidbody == null)
    {
        triggersWithoutAttachedBody++;
    }
}
report["triggerCount"] = triggerCount;
report["triggersWithoutAttachedRigidbody"] = triggersWithoutAttachedBody;

var canvases = new List<Dictionary<string, object>>();
foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
{
    if (!canvas.gameObject.scene.IsValid() || !canvas.gameObject.scene.isLoaded)
    {
        continue;
    }
    canvases.Add(new Dictionary<string, object>
    {
        { "name", canvas.name },
        { "mode", canvas.renderMode.ToString() },
        { "sortingOrder", canvas.sortingOrder },
        { "active", canvas.gameObject.activeInHierarchy }
    });
    if (canvases.Count >= 20)
    {
        break;
    }
}
report["canvases"] = canvases;

var cameras = new List<Dictionary<string, object>>();
foreach (var camera in Resources.FindObjectsOfTypeAll<Camera>())
{
    if (!camera.gameObject.scene.IsValid() || !camera.gameObject.scene.isLoaded)
    {
        continue;
    }
    cameras.Add(new Dictionary<string, object>
    {
        { "name", camera.name },
        { "tag", camera.tag },
        { "enabled", camera.enabled },
        { "depth", camera.depth }
    });
    if (cameras.Count >= 20)
    {
        break;
    }
}
report["cameras"] = cameras;

var controllers = new List<Dictionary<string, object>>();
foreach (var controller in Resources.FindObjectsOfTypeAll<CharacterController>())
{
    if (!controller.gameObject.scene.IsValid() ||
        !controller.gameObject.scene.isLoaded)
    {
        continue;
    }
    controllers.Add(new Dictionary<string, object>
    {
        { "name", controller.name },
        { "enabled", controller.enabled },
        { "grounded", controller.isGrounded },
        { "position", controller.transform.position.ToString("F3") }
    });
    if (controllers.Count >= 20)
    {
        break;
    }
}
report["characterControllers"] = controllers;

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
        var numericValue = Convert.ToInt32(property.GetValue(null, null));
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

var missingComponents = new List<string>();
var missingCount = 0;
foreach (var gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
{
    if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
    {
        continue;
    }
    foreach (var component in gameObject.GetComponents<Component>())
    {
        if (component != null)
        {
            continue;
        }
        missingCount++;
        if (missingComponents.Count < 20)
        {
            missingComponents.Add(gameObject.scene.name + ":" + gameObject.name);
        }
    }
}
report["missingComponentCount"] = missingCount;
report["missingComponentObjects"] = missingComponents;

return report;
