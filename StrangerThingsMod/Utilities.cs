using UnityEngine;
using Unity.Netcode;

namespace StrangerThingsMod
{
    public class Utilities
    {
        public static void SpawnDemogorgonNearWall(Vector3 colliderPosition)
        {
            string prefabName = "Demogorgon";

            if (Content.Prefabs.ContainsKey(prefabName))
            {
                Vector3 spawnPosition = FindWallClosestToPosition(colliderPosition);
                Quaternion spawnRotation = Quaternion.LookRotation(-FindWallNormal(spawnPosition), Vector3.up);

                Plugin.logger.LogInfo($"Spawning {prefabName} at wall");
                GameObject demogorgon = UnityEngine.Object.Instantiate(Content.Prefabs[prefabName], spawnPosition, spawnRotation);
                demogorgon.GetComponent<NetworkObject>().Spawn();
            }
            else
            {
                Plugin.logger.LogWarning($"Prefab {prefabName} not found!");
            }
        }

        private static Vector3 FindWallClosestToPosition(Vector3 position)
        {
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            RaycastHit closestHit = new RaycastHit();
            float closestDistance = float.MaxValue;

            foreach (var direction in directions)
            {
                RaycastHit hit;
                // Adjust the ray length as needed. Here, I set it to 10 units as an example.
                if (Physics.Raycast(position, direction, out hit, 10f))
                {
                    float distance = Vector3.Distance(position, hit.point);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestHit = hit;
                    }
                }
            }

            return closestHit.point;
        }


        private static Vector3 FindWallNormal(Vector3 position)
        {
            RaycastHit hit;
            if (Physics.Raycast(position, Vector3.forward, out hit))
            {
                return hit.normal;
            }

            return Vector3.forward;
        }
    }
}
