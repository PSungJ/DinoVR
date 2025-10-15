using UnityEngine;

public class TerrainTrigger : MonoBehaviour
{
    [SerializeField] private string terrainName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.ChangeTerrain(terrainName);
            Debug.Log($"현재 터레인 정보 : {terrainName}");
        }
    }
}