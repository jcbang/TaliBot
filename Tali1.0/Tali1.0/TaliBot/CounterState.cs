// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Tali
{
	/// <summary>
	/// Stores counter state for the conversation.
	/// Stored in <see cref="Microsoft.Bot.Builder.ConversationState"/> and
	/// backed by <see cref="Microsoft.Bot.Builder.MemoryStorage"/>.
	/// </summary>
	public class CounterState
	{
		public int TurnCount { get; set; } = 0;

		// Temporary flags for first-launches, can be bypassed with stored 
		// account information or through source code debug found in TaliBotBot.cs
		public int isNewAccount { get; set; } = 0;
		public int isFirstLaunch { get; set; } = 0;
		public int completedSetup { get; set; } = 0;

		// Account Information (Will not be needed once SQL is hooked up
		public string userName { get; set; } = "";
		public string accountID { get; set; } = "";
		public int numBills { get; set; } = 0;


		// Temporary flags for order-of-computing purposes (if else statements for the win)
		public int usersTurn { get; set; } = 0;
		public int repeatedQuestions { get; set; } = 0;
		public int askedQuestion { get; set; } = 0;
		public int billFlag { get; set; } = 0;
		public int startTransferFlag { get; set; } = 0;

		// Signals the end of the conversation
		public int endConversationFlag { get; set; } = 0;
	}
}
