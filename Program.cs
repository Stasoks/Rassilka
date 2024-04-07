using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data.SQLite;
using System.Xml.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Policy;


namespace ChatBot
{
    class Program()
    {
        private static ITelegramBotClient _botClient;
        private static ReceiverOptions _receiverOptions;
        static Dictionary<long, string> equations = new Dictionary<long, string>();
        static Random rnd = new Random();
        private const string stepBack = "..//..//..//";
        private const string connectionString = $"Data Source={stepBack}Users_db.db";
        private const long adminId = 1760080161;
        public static ReplyKeyboardMarkup menu = new(new[]
        {
            new KeyboardButton[] { "Кол-во вступивших сегодня" },
            new KeyboardButton[] { "Кол-во вступивших вчера" },
            new KeyboardButton[] { "Кол-во пользователей" }
        })
        {
            ResizeKeyboard = true
        };
        static async Task Main()
        {
            var connection = new SQLiteConnection(connectionString);
            connection.Open();
            _botClient = new TelegramBotClient("1872154697:AAGUxJZjUloMjrjd5Qprw2ldJjfb2aqtysQ"); //debug   - 1872154697:AAGUxJZjUloMjrjd5Qprw2ldJjfb2aqtysQ
                                                                                                  //release - 6857834562:AAGNWEM9FXMyIh-oddr4FDQZNmrgdfmyb60
            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.ChatMember,
                    UpdateType.ChatJoinRequest
                },

                ThrowPendingUpdates = true,
            };
            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, _receiverOptions, cts.Token);
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"{me.FirstName} is started!");
            await Task.Delay(-1);
            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                var message = update.Message;
                var messageText = "";
                long chatid = 0;
                if (message != null)
                {
                    messageText = update.Message.Text;
                    chatid = message.Chat.Id;
                }
                long chatId = -1002054403110;
                long userId = 0;
                if (update.ChatJoinRequest != null)
                {
                    userId = update.ChatJoinRequest.From.Id;
                    try
                    {
                        new SQLiteCommand($"INSERT INTO Users (Id, StartTime) VALUES ('{update.ChatJoinRequest.From.Id}', datetime('now'))", connection).ExecuteNonQuery();
                    }
                    catch(Exception e)
                    {

                    }
                    await botClient.SendTextMessageAsync(
                    chatId: userId,
                    text: "Привет",
                    cancellationToken: cancellationToken
                    );
                    new ApproveChatJoinRequest(chatId, userId);
                    await botClient.ApproveChatJoinRequest(chatId, userId, cancellationToken);
                }
                if (messageText.Contains("/mailing") && update.Message.From.Id == adminId)
                {
                    try
                    {
                        SQLiteCommand command = connection.CreateCommand();
                        command.CommandText = "SELECT Id FROM Users";
                        SQLiteDataReader reader = command.ExecuteReader();
                        List<string[]> data = new List<string[]>();
                        while (reader.Read())
                        {
                            data.Add(new string[1]);
                            data[data.Count - 1][0] = reader[0].ToString();
                        }

                        foreach (string[] s in data)
                        {
                            Message sendmessage = await botClient.SendTextMessageAsync(
                            chatId: s[0].ToString(),
                            text: messageText.Substring(9),
                            cancellationToken: cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex);
                    }
                }
                if (messageText.Contains("admin") && update.Message.From.Id == adminId)
                {
                    Message sendmessage = await botClient.SendTextMessageAsync(
                            chatId: adminId,
                            text: "Вызов админ панели",
                            replyMarkup:menu,
                            cancellationToken: cancellationToken);
                }
                if (messageText.Contains("Кол-во вступивших сегодня") && update.Message.From.Id == adminId)
                {
                    var newUsersReader = new SQLiteCommand($"SELECT COUNT(*) FROM Users WHERE StartTime >= datetime('now', '-1 day')", connection: connection).ExecuteReader();
                    newUsersReader.Read();
                    int newUsers = newUsersReader.GetInt32(0);

                    await botClient.SendTextMessageAsync(
                        chatId: adminId,
                        text: $"Кол во вступивших сегодня - {newUsers}",
                        replyMarkup: menu,
                        cancellationToken: cancellationToken);
                }
                if (messageText.Contains("Кол-во пользователей") && update.Message.From.Id == adminId)
                {
                    var UsersReader = new SQLiteCommand($"SELECT COUNT(*) FROM Users", connection: connection).ExecuteReader();
                    UsersReader.Read();
                    int Users = UsersReader.GetInt32(0);

                    await botClient.SendTextMessageAsync(
                        chatId: adminId,
                        text: $"Кол во пользователей - {Users}",
                        replyMarkup: menu,
                        cancellationToken: cancellationToken);
                }
                if (messageText.Contains("Кол-во вступивших вчера") && update.Message.From.Id == adminId)
                {
                    var OldUsersReader = new SQLiteCommand($"SELECT COUNT(*) FROM Users WHERE StartTime <= datetime('now', '-1 day') and StartTime >= datetime('now', '-2 day')", connection: connection).ExecuteReader();
                    OldUsersReader.Read();
                    int OldUsers = OldUsersReader.GetInt32(0);

                    await botClient.SendTextMessageAsync(
                        chatId: adminId,
                        text: $"Кол во вступивших вчера - {OldUsers}",
                        replyMarkup: menu,
                        cancellationToken: cancellationToken);
                }


            }
            Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(ErrorMessage);
                return Task.CompletedTask;
            }

        }
    }
}
