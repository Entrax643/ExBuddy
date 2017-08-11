namespace ExBuddy.Logging
{
    using System;
    using ff14bot.NeoProfiles;

    internal class ProfileBehaviorLogger : IStatusLogger
    {
        protected readonly ProfileBehavior profileBehavior;

        public ProfileBehaviorLogger(ProfileBehavior profileBehavior)
        {
            this.profileBehavior = profileBehavior ?? throw new ArgumentNullException("profileBehavior");
        }

        public void SetStatusText(string text)
        {
            this.profileBehavior.StatusText = text;
        }
    }
}