using UnityEngine;

// Type 2. Takes points from another player and gives them to you.
[CreateAssetMenu(menuName = "Quiz/Items/Target - Steal Points")]
public class StealPointsItem : TargetItemDefinition
{
    [SerializeField, Min(1)] private int points = 100;

    public override bool CanUse(ulong userClientId, ulong targetClientId)
    {
        var targetPoints = PlayerPoints.Get(targetClientId);
        return targetPoints != null && targetPoints.Points.Value > 0;
    }

    protected override void ApplyToTarget(ulong userClientId, ulong targetClientId)
    {
        int taken = Mathf.Min(points, PlayerPoints.Get(targetClientId).Points.Value);

        RoundManager.Instance.ChangePoints(targetClientId, -taken);
        RoundManager.Instance.ChangePoints(userClientId, taken);
    }
}