﻿using UnityEngine;
using System.Collections;

public class ProjectileEnemyRed : ProjectileEnemy
{
    protected override void resetVariables()
    {
        maxCollisions = 1;
        projectileSpeed = 10.5f;
    }
}
