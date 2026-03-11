using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using ValleyTalk.Social.Models;

namespace ValleyTalk
{
    internal class SaturdaySocialActionMenu : IClickableMenu
    {
        private readonly string title;
        private readonly string subtitle;
        private readonly List<SocialActionRequest> actions;
        private readonly List<ClickableComponent> actionButtons = new List<ClickableComponent>();
        private readonly Action<SocialActionRequest> onActionSelected;

        private const int MenuWidth = 860;
        private const int ButtonHeight = 68;
        private const int Margin = 20;

        public SaturdaySocialActionMenu(string npcDisplayName, IReadOnlyList<SocialActionRequest> actions, Action<SocialActionRequest> onActionSelected)
        {
            this.title = "Saturday Social";
            this.subtitle = "Choose how to approach " + npcDisplayName + ".";
            this.actions = new List<SocialActionRequest>(actions ?? Array.Empty<SocialActionRequest>());
            this.onActionSelected = onActionSelected;

            this.width = MenuWidth;
            this.height = Margin * 3 + 88 + this.actions.Count * (ButtonHeight + 8);
            this.xPositionOnScreen = (Game1.uiViewport.Width - this.width) / 2;
            this.yPositionOnScreen = (Game1.uiViewport.Height - this.height) / 2;

            for (var index = 0; index < this.actions.Count; index++)
            {
                this.actionButtons.Add(new ClickableComponent(
                    new Rectangle(
                        this.xPositionOnScreen + Margin,
                        this.yPositionOnScreen + 88 + Margin + index * (ButtonHeight + 8),
                        this.width - Margin * 2,
                        ButtonHeight),
                    this.actions[index].Label));
            }
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.45f);
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

            b.DrawString(Game1.dialogueFont, this.title, new Vector2(this.xPositionOnScreen + Margin, this.yPositionOnScreen + Margin), Game1.textColor);
            b.DrawString(Game1.smallFont, this.subtitle, new Vector2(this.xPositionOnScreen + Margin, this.yPositionOnScreen + Margin + 44), Color.Gray);

            for (var index = 0; index < this.actionButtons.Count; index++)
            {
                var button = this.actionButtons[index];
                var action = this.actions[index];
                IClickableMenu.drawTextureBox(b, button.bounds.X, button.bounds.Y, button.bounds.Width, button.bounds.Height, Color.White);

                b.DrawString(Game1.dialogueFont, action.Label, new Vector2(button.bounds.X + 16, button.bounds.Y + 8), Game1.textColor);
                b.DrawString(Game1.smallFont, action.Description, new Vector2(button.bounds.X + 16, button.bounds.Y + 38), Color.DarkSlateGray);
            }

            b.DrawString(
                Game1.smallFont,
                "Esc to cancel",
                new Vector2(this.xPositionOnScreen + this.width - 120, this.yPositionOnScreen + this.height - Margin - 24),
                Color.Gray);

            this.drawMouse(b);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            for (var index = 0; index < this.actionButtons.Count; index++)
            {
                if (!this.actionButtons[index].containsPoint(x, y))
                {
                    continue;
                }

                Game1.playSound("smallSelect");
                var action = this.actions[index];
                Game1.exitActiveMenu();
                this.onActionSelected?.Invoke(action);
                return;
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Escape || key == Keys.B)
            {
                Game1.playSound("bigDeSelect");
                Game1.exitActiveMenu();
                return;
            }

            base.receiveKeyPress(key);
        }

        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }
    }
}
