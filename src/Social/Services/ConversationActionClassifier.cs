using System;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class ConversationActionClassifier : IConversationActionClassifier
    {
        public SocialActionType Classify(string text, bool isPlayerLine)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return SocialActionType.Chat;
            }

            var normalized = text.ToLowerInvariant();
            if (this.ContainsAny(normalized, "dance", "slow dance", "join me on the dance floor", "want to dance", "have this dance", "hit the dance floor"))
            {
                return SocialActionType.InviteDance;
            }

            if (this.ContainsAny(normalized, "sorry", "apolog", "my fault", "forgive me", "didn't mean"))
            {
                return SocialActionType.Apologize;
            }

            if (this.ContainsAny(normalized, "alone", "private", "step out", "quiet corner", "somewhere quieter", "somewhere private", "talk somewhere else", "just us"))
            {
                return SocialActionType.SuggestPrivateConversation;
            }

            if (this.ContainsAny(normalized, "idiot", "stupid", "shut up", "hate you", "damn you", "piss off", "you bastard"))
            {
                return SocialActionType.Provoke;
            }

            if (this.ContainsAny(normalized, "beautiful", "handsome", "cute", "kiss", "flirt", "gorgeous", "hot", "want you", "you look incredible"))
            {
                return SocialActionType.Flirt;
            }

            return SocialActionType.Chat;
        }

        private bool ContainsAny(string text, params string[] phrases)
        {
            foreach (var phrase in phrases)
            {
                if (text.Contains(phrase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
