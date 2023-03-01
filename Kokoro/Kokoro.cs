﻿using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.Menus;
using StardewValley;
using System.Collections.Generic;
using StardewModdingAPI.Events;

namespace Shockah.Kokoro
{
	public class Kokoro : BaseMod
	{
		public static Kokoro Instance { get; private set; } = null!;

		private PerScreen<LinkedList<string>> QueuedObjectDialogue { get; init; } = new(() => new());

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			// dequeue object dialogue
			var message = QueuedObjectDialogue.Value.First;
			if (message is not null && Game1.activeClickableMenu is not DialogueBox)
			{
				QueuedObjectDialogue.Value.RemoveFirst();
				Game1.drawObjectDialogue(message.Value);
			}
		}

		public void QueueObjectDialogue(string message)
		{
			if (Game1.activeClickableMenu is DialogueBox)
				QueuedObjectDialogue.Value.AddLast(message);
			else
				Game1.drawObjectDialogue(message);
		}
	}
}