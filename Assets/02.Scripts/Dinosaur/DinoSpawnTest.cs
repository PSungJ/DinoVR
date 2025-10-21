using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoSpawnTest : MonoBehaviour
{
    public float x = 0;
    public float y = 0;

    public int amount = 10;

    public GameObject[] dinos;

    void Start()
    {
        StartCoroutine(SpawnDino());
    }

    IEnumerator SpawnDino()
    {
        for (var i = 0; i < amount; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-x, x), 0f, Random.Range(-y, y));
            var num = Random.Range(0, dinos.Length);
            if (num == 5)
            {
                //GameObject.Instantiate(dinos[num], pos + Vector3.right * 3f, Quaternion.identity);
                //GameObject.Instantiate(dinos[num], pos - Vector3.right * 3f, Quaternion.identity);
            }
            GameObject.Instantiate(dinos[num], pos, Quaternion.identity);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
