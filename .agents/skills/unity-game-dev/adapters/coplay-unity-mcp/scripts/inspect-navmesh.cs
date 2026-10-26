// Coplay v10 execute_code method body. Does not bake or modify NavMesh data.
var report = new Dictionary<string, object>();
var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
report["triangulation"] = new Dictionary<string, object>
{
    { "vertices", triangulation.vertices.Length },
    { "indices", triangulation.indices.Length },
    { "areas", triangulation.areas.Length }
};

var details = new List<Dictionary<string, object>>();
var total = 0;
var onMesh = 0;
var offMesh = 0;
foreach (var agent in Resources.FindObjectsOfTypeAll<UnityEngine.AI.NavMeshAgent>())
{
    if (!agent.gameObject.scene.IsValid() || !agent.gameObject.scene.isLoaded)
    {
        continue;
    }

    total++;
    if (agent.isOnNavMesh)
    {
        onMesh++;
    }
    else
    {
        offMesh++;
    }

    if (details.Count < 30)
    {
        details.Add(new Dictionary<string, object>
        {
            { "name", agent.name },
            { "scene", agent.gameObject.scene.name },
            { "enabled", agent.enabled },
            { "onNavMesh", agent.isOnNavMesh },
            { "position", agent.transform.position.ToString("F3") },
            { "agentTypeId", agent.agentTypeID },
            { "areaMask", agent.areaMask }
        });
    }
}

report["agentCount"] = total;
report["agentsOnNavMesh"] = onMesh;
report["agentsOffNavMesh"] = offMesh;
report["agents"] = details;
report["mutated"] = false;
return report;
