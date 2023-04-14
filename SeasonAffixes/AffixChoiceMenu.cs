﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shockah.Kokoro.Stardew;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes
{
	internal class AffixChoiceMenu : IClickableMenu
	{
		public const int BaseWidth = 768;
		public const int BaseHeight = 448;
		public static readonly int ChoiceWidth = (BaseWidth - borderWidth * 3) / 2;
		private const int IconWidth = 32;
		private const int IconHeight = 32;
		private const float IconToTextSpacing = 12f;
		private const int AffixSpacing = 4;
		private const int AffixHeight = 40;
		private const int AffixMargin = 8;
		private const int PlayerPortraitSpacing = 24;

		private AffixChoiceMenuConfig ConfigStorage;

		internal AffixChoiceMenuConfig Config
		{
			get => ConfigStorage;
			set
			{
				ConfigStorage = value;
				UpdateBounds();
			}
		}

		private int? SelectedChoice = null;
		private int? ConfirmedChoice = null;
		private int ConfirmationTime = 150;

		public AffixChoiceMenu(AffixChoiceMenuConfig config) : base(0, 0, 600, 400)
		{
			this.ConfigStorage = config;
			UpdateBounds();
		}

		public void SetConfirmedAffixSetChoice(IReadOnlySet<ISeasonAffix> choice)
		{
			if (Config.Choices is null)
				return;
			for (int i = 0; i < Config.Choices.Count; i++)
			{
				if (Config.Choices[i].SetEquals(choice))
				{
					ConfirmedChoice = i;
					break;
				}
			}
			if (ConfirmedChoice is null)
				return;
			// TODO: make sound
		}

		private void UpdateBounds()
		{
			width = Math.Max(BaseWidth, ChoiceWidth * (Config.Choices?.Count ?? 2) + borderWidth * (1 + (Config.Choices?.Count ?? 2)));
			height = BaseHeight;

			xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
			yPositionOnScreen = Game1.uiViewport.Height / 2 - height / 2;
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
			=> UpdateBounds();

		public override void receiveKeyPress(Keys key)
		{
			if (!Game1.options.doesInputListContain(Game1.options.cancelButton, key) && !Game1.options.doesInputListContain(Game1.options.menuButton, key))
				base.receiveKeyPress(key);
		}

		public override void update(GameTime time)
		{
			SelectedChoice = null;

			if (ConfirmedChoice is not null)
			{
				ConfirmationTime--;
				if (ConfirmationTime <= 0)
					exitThisMenu(playSound: false);
				return;
			}

			if (Config.Choices is not null && !SeasonAffixes.Instance.PlayerChoices.ContainsKey(Game1.player))
			{
				int choiceHeight = height - 192;
				for (int i = 0; i < Config.Choices.Count; i++)
				{
					int left = xPositionOnScreen + borderWidth + (ChoiceWidth + borderWidth) * i;
					int top = yPositionOnScreen + 192;
					if (Game1.getMouseX() >= left && Game1.getMouseY() >= top && Game1.getMouseX() < left + ChoiceWidth && Game1.getMouseY() < top + choiceHeight)
					{
						SelectedChoice = i;
						break;
					}
				}

				if (Game1.oldMouseState.LeftButton == ButtonState.Pressed && SelectedChoice is not null)
					SeasonAffixes.Instance.RegisterChoice(Game1.player, new PlayerChoice.Choice(Config.Choices![SelectedChoice.Value]));
			}
		}

		public override void draw(SpriteBatch b)
		{
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
			Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, speaker: false, drawOnlyBox: true);

			{
				var text = SeasonAffixes.Instance.Helper.Translation.Get("season.title");
				Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2(xPositionOnScreen + width / 2f - Game1.dialogueFont.MeasureString(text).X / 2f, yPositionOnScreen + 116), Color.Black);
			}

			if (Config.Choices is null)
			{
				var text = SeasonAffixes.Instance.Helper.Translation.Get("season.awaiting.description");
				Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(xPositionOnScreen + width / 2f - Game1.smallFont.MeasureString(text).X / 2f, yPositionOnScreen + 164), Color.Black);
				return;
			}

			{
				var text = SeasonAffixes.Instance.Helper.Translation.Get(SeasonAffixes.Instance.Config.Incremental ? "season.incremental.description" : "season.replacement.description");
				Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(xPositionOnScreen + width / 2f - Game1.smallFont.MeasureString(text).X / 2f, yPositionOnScreen + 164), Color.Black);
			}

			drawHorizontalPartition(b, yPositionOnScreen + 192);

			var orderedChoices = Config.Choices.Select(choice =>
			{
				return choice
					.OrderBy(a => a.GetPositivity(Config.Season) - a.GetNegativity(Config.Season))
					.ThenBy(a => a.UniqueID)
					.ToList();
			}).ToList();

			int choiceHeight = height - 192;
			for (int i = 0; i < Config.Choices.Count; i++)
			{
				if (i != 0)
					drawVerticalIntersectingPartition(b, xPositionOnScreen + (ChoiceWidth + borderWidth) * i, yPositionOnScreen + 192);

				int totalAffixesHeight = orderedChoices[i].Count * AffixHeight + (orderedChoices[i].Count - 1) * AffixSpacing;
				int topAffixPosition = yPositionOnScreen + 196 + choiceHeight / 2 - totalAffixesHeight / 2;

				bool isSelected = ConfirmedChoice is null
					? SelectedChoice == i
					: ConfirmedChoice == i;
				for (int j = 0; j < orderedChoices[i].Count; j++)
					DrawAffix(b, new(xPositionOnScreen + borderWidth + (ChoiceWidth + borderWidth) * i + AffixMargin, topAffixPosition + j * (AffixHeight + AffixSpacing), ChoiceWidth - AffixMargin * 2, AffixHeight), orderedChoices[i][j], isSelected);

				if (Game1.getOnlineFarmers().Count > 1)
				{
					var chosenBy = SeasonAffixes.Instance.PlayerChoices
					.Where(kvp => kvp.Value.Equals(new PlayerChoice.Choice(Config.Choices[i])))
					.Select(kvp => kvp.Key)
					.ToList();

					int chosenByWidth = chosenBy.Count * PlayerPortraitSpacing;
					for (int j = 0; j < chosenBy.Count; j++)
						chosenBy[j].FarmerRenderer.drawMiniPortrat(b, new Vector2(xPositionOnScreen + borderWidth + (ChoiceWidth + borderWidth) * i + ChoiceWidth / 2f - chosenByWidth / 2f + j * PlayerPortraitSpacing, yPositionOnScreen + 140 + choiceHeight), 1f, 3f, 2, chosenBy[j]);
				}
			}

			if (GameExt.GetMultiplayerMode() != MultiplayerMode.SinglePlayer && Game1.getOnlineFarmers().Count > 1)
			{
				int onlinePlayers = Game1.getOnlineFarmers().Count;
				int readyPlayers = SeasonAffixes.Instance.PlayerChoices.Keys.Count;
				var message = Game1.content.LoadString("Strings\\UI:ReadyCheck", readyPlayers, onlinePlayers);
				b.DrawString(Game1.dialogueFont, message, new Vector2(xPositionOnScreen + borderWidth, yPositionOnScreen + spaceToClearTopBorder + borderWidth / 2), Game1.textColor);
			}

			if (SelectedChoice is not null)
				drawToolTip(b, GetSeasonDescription(orderedChoices[SelectedChoice.Value]), SeasonAffixes.Instance.GetSeasonName(orderedChoices[SelectedChoice.Value]), null);

			if (!Game1.options.SnappyMenus)
			{
				Game1.mouseCursorTransparency = 1f;
				drawMouse(b);
			}
		}

		private void DrawAffix(SpriteBatch b, Rectangle bounds, ISeasonAffix affix, bool selected)
		{
			var icon = affix.Icon;
			float iconScale = 1f;
			if (icon.Rectangle.Width * iconScale < IconWidth)
				iconScale = 1f * IconWidth / icon.Rectangle.Width;
			if (icon.Rectangle.Height * iconScale < IconHeight)
				iconScale = 1f * IconHeight / icon.Rectangle.Height;
			if (icon.Rectangle.Width * iconScale > IconWidth)
				iconScale = 1f * IconWidth / icon.Rectangle.Width;
			if (icon.Rectangle.Height * iconScale > IconHeight)
				iconScale = 1f * IconHeight / icon.Rectangle.Height;

			var iconPosition = new Vector2(bounds.X + IconWidth / 2f, bounds.Y + bounds.Height / 2f);
			b.Draw(icon.Texture, iconPosition + new Vector2(-iconScale, iconScale), icon.Rectangle, Color.Black * 0.3f, 0f, new Vector2(icon.Rectangle.Width / 2f, icon.Rectangle.Height / 2f), iconScale, SpriteEffects.None, 4f);
			b.Draw(icon.Texture, iconPosition, icon.Rectangle, Color.White, 0f, new Vector2(icon.Rectangle.Width / 2f, icon.Rectangle.Height / 2f), iconScale, SpriteEffects.None, 4f);

			Color textColor;
			if (ConfirmedChoice is null)
				textColor = selected ? Color.Green : Game1.textColor;
			else
				textColor = selected ? (ConfirmationTime / 10 % 2 == 0 ? Color.Green : Game1.textColor) : (Game1.textColor * 0.5f);
			Utility.drawTextWithShadow(b, affix.LocalizedName, Game1.dialogueFont, new Vector2(bounds.X + IconWidth + IconToTextSpacing, bounds.Y - 4), textColor);
		}

		private static string GetSeasonDescription(IReadOnlyList<ISeasonAffix> affixes)
			=> string.Join("\n", affixes.Select(a => a.LocalizedDescription));
	}
}