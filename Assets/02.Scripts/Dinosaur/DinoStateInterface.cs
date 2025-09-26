using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface DinoStateInterface
{
    public void Idle();
    public void Roam();
    public void Eating();
    public void Drink();
    public void Sleeping();
    public void Fleeing();
    public void Searching();
    public void Attack();
    public void Roar();
    public void Death();
}
