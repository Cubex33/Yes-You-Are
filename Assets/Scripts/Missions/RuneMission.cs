using Lucy.Core;
using UnityEngine;
[DisallowMultipleComponent]
public class RuneMission : DestroyItemsMission
{
    public Entity supreme;
    protected override void CompleteMission()
    {
        base.CompleteMission();
        supreme.SetImmortal(false);
    }
}