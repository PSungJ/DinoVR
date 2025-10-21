using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorAnim : MonoBehaviour
{
    [SerializeField] private Animator ani;
    [SerializeField] private bool isOpen;

    void Start()
    {
        ani = GetComponent<Animator>();
        isOpen = false;
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Player") && !isOpen)
        {
            isOpen = true;
            ani.SetBool("isOpen", true);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.gameObject.CompareTag("Player") && isOpen)
        {
            isOpen = false;
            ani.SetBool("isOpen", false);
        }
    }
}
