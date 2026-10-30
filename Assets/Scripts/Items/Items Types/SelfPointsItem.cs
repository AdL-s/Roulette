using UnityEngine;

[CreateAssetMenu(menuName = "Quiz/Items/Self - Gain Points")]
public class SelfPointsItem : SelfItemDefinition
{
    [SerializeField, Min(1)] private int points = 100;

    protected override void ApplyToSelf(ulong userClientId)
    {
        RoundManager.Instance.ChangePoints(userClientId, points);
    }
}