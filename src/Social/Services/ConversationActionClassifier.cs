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
            if (normalized.Contains("dance"))
            {
                return SocialActionType.InviteDance;
            }

            if (normalized.Contains("sorry") || normalized.Contains("apolog"))
            {
                return SocialActionType.Apologize;
            }

            if (normalized.Contains("outside") || normalized.Contains("alone") || normalized.Contains("private") || normalized.Contains("step out"))
            {
                return SocialActionType.SuggestPrivateConversation;
            }

            if (normalized.Contains("idiot") || normalized.Contains("stupid") || normalized.Contains("shut up") || normalized.Contains("hate you"))
            {
                return SocialActionType.Provoke;
            }

            if (normalized.Contains("beautiful") || normalized.Contains("handsome") || normalized.Contains("cute") || normalized.Contains("kiss") || normalized.Contains("flirt"))
            {
                return SocialActionType.Flirt;
            }

            return SocialActionType.Chat;
        }
    }
}
