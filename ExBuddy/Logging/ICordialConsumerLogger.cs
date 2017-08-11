namespace ExBuddy.Logging
{
    internal interface ICordialConsumerLogger
    {
        void CordialCannotBeUsedByDeadPlayer(string cordialName, bool hq);
        void CordialCannotBeUsedByMountedPlayer(string cordialName, bool hq);
        void CordialCannotBeUsedByPlayer(string cordialName, bool hq);
        void CordialCannotBeUsedDismountFailed(string cordialName, bool hq);
        void CordialInventoryDepleted(string cordialName, bool hq);
        void CordialItemTypeInvalid(uint itemId);
        void CordialOnCooldown(string cordialName, bool hq);
        void CordialUsed(string cordialName, bool hq, int cordialSize, int startingGp, int regeneratedGp, int targetGp);
    }
}