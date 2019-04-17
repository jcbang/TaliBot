// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Custom Bot created by
// Justin C. Bang
// University of Central Florida

// Bot Requirements
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

// LUIS Intents
using System;
using System.Web;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Tali
{
	/// <summary>
	/// Represents a bot that processes incoming activities.
	/// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
	/// This is a Transient lifetime service.  Transient lifetime services are created
	/// each time they're requested. For each Activity received, a new instance of this
	/// class is created. Objects that are expensive to construct, or have a lifetime
	/// beyond the single turn, should be carefully managed.
	/// For example, the <see cref="MemoryStorage"/> object and associated
	/// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
	/// </summary>
	/// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
	public class TaliBot : IBot
	{
		private readonly TaliBotAccessors _accessors;
		private readonly ILogger _logger;

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		/// <param name="accessors">A class containing <see cref="IStatePropertyAccessor{T}"/> used to manage state.</param>
		/// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
		/// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
		public TaliBot(TaliBotAccessors accessors, ILoggerFactory loggerFactory)
		{
			if (loggerFactory == null)
			{
				throw new System.ArgumentNullException(nameof(loggerFactory));
			}

			_logger = loggerFactory.CreateLogger<TaliBot>();
			_logger.LogTrace("Turn start.");
			_accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
		}

		/// <summary>
		/// Every conversation turn for our Echo Bot will call this method.
		/// There are no dialogs used, since it's "single turn" processing, meaning a single
		/// request and response.
		/// </summary>
		/// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
		/// for processing this conversation turn. </param>
		/// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
		/// or threads to receive notice of cancellation.</param>
		/// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
		/// <seealso cref="BotStateSet"/>
		/// <seealso cref="ConversationState"/>
		/// <seealso cref="IMiddleware"/>

		// Typed Speech
		public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
		{
			// Handle Message activity type, which is the main activity type for shown within a conversational interface
			// Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
			// see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
			if (turnContext.Activity.Type == ActivityTypes.Message)
			{
				var responseMessage = "";
				var client = new HttpClient();

				// Get the conversation state from the turn context.
				var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

				BillInformation billInfo = new BillInformation();
				TransferInformation transferInfo = new TransferInformation();

				// var AccountInformation = await _accessors.AccountInformation.GetAsync(turnContext, () => new AccountInformation());

				// Bump the turn count for this conversation.
				state.TurnCount++;

				// First Use Scenario
				if (state.isFirstLaunch == 0 || state.completedSetup == 0)
				{
					// 1
					if (state.isFirstLaunch == 0 && state.userName == "" && state.accountID == "" && state.completedSetup != 1)
					{
						responseMessage = $"My omni-tool says that it's your first time using this interface!\n";
						responseMessage += "It's great to meet you. We'll run you through the first time registration setup.\n";
						responseMessage += "First off, may we get your name?\n";
						state.isFirstLaunch = 1;
					}

					// 2
					else if (state.userName == "" && state.accountID == "" && state.completedSetup != 1)
					{
						state.userName = turnContext.Activity.Text;
						responseMessage = $"Great, {state.userName}! We now have your name inputted into my omni-tool.\n";
						responseMessage += "To finalize our suite, may we get your account ID?\n";
					}

					// 3
					else if (state.userName != "" && state.accountID == "" && state.completedSetup != 1)
					{
						state.accountID = turnContext.Activity.Text;
						responseMessage = $"Fantastic, {state.userName}! We now have you fully registered.\n";
						responseMessage += $"What sort of questions do you have for me today?\n";
						state.completedSetup = 1;
						state.usersTurn = 1;
						state.askedQuestion = 1;
					}

					// Set the property using the accessor and saves the new turn count into the conversation state
					await _accessors.CounterState.SetAsync(turnContext, state);
					await _accessors.ConversationState.SaveChangesAsync(turnContext);

					await turnContext.SendActivityAsync(responseMessage);
				}
				// Everything that comes after first use scenario
				else
				{
					// Saves state that lead up to this spot
					await _accessors.CounterState.SetAsync(turnContext, state);
					await _accessors.ConversationState.SaveChangesAsync(turnContext);

					// 1) Parse question
					// 2) Get intent from question directly
					// 3) Return response dynamically based on query using the API connenction string
					string questionString = turnContext.Activity.Text;
					string returnedIntent = await MakeRequest(questionString);

					if (state.repeatedQuestions == 0)
					{
						// Will infinite-loop the bot to "end" the conversation
						if (state.endConversationFlag == 1)
						{
							responseMessage = "";

							await turnContext.SendActivityAsync(responseMessage);
						}
						// Starts the Conversation (Usually the first thing people say)
						else if (String.Equals(returnedIntent, "StartConversation"))
						{
							responseMessage = $"Welcome back, {state.userName}!\n";
							responseMessage += "Is there something we can help you with?\n";

							await turnContext.SendActivityAsync(responseMessage);
						}
						// Ends the Conversation (Triggers endConversationFlag)
						else if (String.Equals(returnedIntent, "EndConversation"))
						{
							responseMessage = "Thank you for using Tali, a Virtual Intelligence built on Microsoft's Bot Framework!\n";
							state.endConversationFlag = 1;

							await turnContext.SendActivityAsync(responseMessage);
						}
						// Gets Account Balance
						else if (String.Equals(returnedIntent, "Balance"))
						{
							string returnedBalance = await QueryCommands.GetBalanceAsync(state.accountID);

							responseMessage = $"Your account balance is ${returnedBalance} USD.\n";
							responseMessage += "Do you have any other questions you wish to ask?";

							await turnContext.SendActivityAsync(responseMessage);
						}
						// Gets Upcoming Bills
						else if (String.Equals(returnedIntent, "GetBills"))
						{
							int returnedNumBills = await QueryCommands.GetNumBillsAsync(state.accountID);

							if (String.Equals(returnedNumBills.ToString(), "0"))
							{
								responseMessage = $"You have no upcoming bills, {state.userName}!\n";
								responseMessage += "Do you have any other questions you wish to ask?";
							}
							else
							{
								List<BillInformation> returnedBillsList = await QueryCommands.GetBillsListAsync(state.accountID);

								responseMessage = $"You have {returnedNumBills} bill(s):\n";

								// A loop that goes through each individual item in the list and adds it to response
								for (int i = 0; i < returnedNumBills; i++)
								{
									responseMessage += $"{returnedBillsList[i].nickname} due on {returnedBillsList[i].upcoming_payment_date} \n";
								}

								responseMessage += "Do you have any other questions you wish to ask?";
							}

							await turnContext.SendActivityAsync(responseMessage);
						}
						// Triggers the loop to Post Bills
						else if (String.Equals(returnedIntent, "PostBills"))
						{
							state.repeatedQuestions = 1;
							state.billFlag = 1;

							responseMessage = "We can update my omni-tool to keep track of a new bill!\n";
							responseMessage += "What's the status of this bill?\n";
							responseMessage += "Options:\n";
							responseMessage += "Pending\n";
							responseMessage += "Cancelled\n";
							responseMessage += "Completed\n";
							responseMessage += "Recurring\n";

							await _accessors.ConversationState.SaveChangesAsync(turnContext);
							await turnContext.SendActivityAsync(responseMessage);
						}
						// Triggers the loop to Start a Transfer
						else if (String.Equals(returnedIntent, "StartTransferFunds"))
						{
							state.repeatedQuestions = 1;
							state.startTransferFlag = 1;

							responseMessage = "Who are we making this transfer out to?\n";

							await _accessors.ConversationState.SaveChangesAsync(turnContext);
							await turnContext.SendActivityAsync(responseMessage);
						}
						else
						{
							await turnContext.SendActivityAsync("PLACEHOLDER, RETURNED LUIS INTENT IS: " + returnedIntent);
						}
					}
					else if (state.repeatedQuestions > 0)
					{
						if (state.billFlag == 1 && state.startTransferFlag == 0)
						{
							// 1
							if (state.repeatedQuestions == 1)
							{
								billInfo.status = turnContext.Activity.Text;

								responseMessage = $"Great! We have logged a new {billInfo.status} bill\n";
								responseMessage += "Now we need the payee, who should we make this bill out to?\n";

								state.repeatedQuestions++;
							}
							// Needs payment date
							else if (state.repeatedQuestions == 2)
							{
								billInfo.payee = turnContext.Activity.Text;

								responseMessage = $"It looks like we have our {billInfo.status} bill made out to {billInfo.payee}!\n";
								responseMessage += $"How much are we paying {billInfo.payee}?\n";

								state.repeatedQuestions++;
							}
							// Needs amount due
							else if (state.repeatedQuestions == 3)
							{
								int amount = Convert.ToInt32(turnContext.Activity.Text);
								billInfo.payment_amount = amount;

								responseMessage = $"We have our payment of ${billInfo.payment_amount} USD \n";
								responseMessage += $"registered! When is this bill due?\n";
								responseMessage += "Note: Please enter your date format as YYYY-MM-DD";

								state.repeatedQuestions++;
							}
							// Needs payment date
							else if (state.repeatedQuestions == 4)
							{
								 billInfo.payment_Date = turnContext.Activity.Text;

								responseMessage = $"On {billInfo.payment_Date} it is!\n";
								responseMessage += "Is there anything else we can help you with?\n";

								 state.repeatedQuestions = 0;
								state.billFlag = 0;
							}
							// Confirm (Intent yes or no)
							// Optional (Ask for Nickname)
						}

						if (state.startTransferFlag == 1)
						{
							if (state.repeatedQuestions == 1)
							{
								transferInfo.payee = turnContext.Activity.Text;

								responseMessage = $"How much are we paying {transferInfo.payee}?\n";

								state.repeatedQuestions++;
							}
							else if (state.repeatedQuestions == 2)
							{
								transferInfo.amount = Convert.ToInt32(turnContext.Activity.Text);

								responseMessage = $"Fantastic! I have set up a transfer to {transferInfo.payee} for ${transferInfo.amount} USD.\n";
								responseMessage += "Is there anything else we can assist you with?\n";

								state.repeatedQuestions = 0;
								state.startTransferFlag = 0;
							}
						}


						await _accessors.ConversationState.SaveChangesAsync(turnContext);
						await turnContext.SendActivityAsync(responseMessage);
					}
					else
					{
						await turnContext.SendActivityAsync("PLACEHOLDER, LAST RETURNED MESSAGE: " + turnContext.Activity.Text);
					}
				}
			}
			else
			{
				await turnContext.SendActivityAsync($"Hey! I'm Tali, a Virtual Intelligence built on the Microsoft Bot Framework.");
			}
		}

		// Returns intent from input string
		private static async Task<String> MakeRequest(String inputString)
		{
			var client = new HttpClient();
			var queryString = HttpUtility.ParseQueryString(string.Empty);

			var luisAppId = "";
			var endpointKey = "";

			client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", endpointKey);

			// The "q" parameter contains the utterance to send to LUIS
			queryString["q"] = inputString;

			// These optional request parameters are set to their default values
			queryString["timezoneOffset"] = "0";
			queryString["verbose"] = "false";
			queryString["spellCheck"] = "false";
			queryString["staging"] = "false";

			var endpointUri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/" + luisAppId + "?" + queryString;
			var response = await client.GetAsync(endpointUri);

			// Save the returned JSON string
			var strResponseContent = await response.Content.ReadAsStringAsync();

			// Display the JSON result from LUIS
			// Console.WriteLine(strResponseContent.ToString());

			// Parse the JSON string and return the intent
			var jsonString = strResponseContent;
			var obj = JsonConvert.DeserializeObject<JObject>(jsonString);

			return obj["topScoringIntent"]["intent"].ToString();
		}
	}
}
