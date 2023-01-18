using Amazon;
using Amazon.Runtime;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AWSTelegramBot.Services;

public interface IUpdateService
{
    Task HandleUpdate(Update update, CancellationToken cancellationToken = default);
}

public class UpdateService : IUpdateService
{
    private const string _filledListMessage = "Список покупок\n(тицяй на продукт, щоб видалити):";
    private const string _emptyListMessage = "Список покупок порожній. Почніть додавати продукти до списку окремими повідомленнями";
    private const string _errorMessage = "Щось пішло не так. Спробуй трохи пізніше";

    private readonly ITelegramBotClient _botClient;
    private readonly AmazonSimpleSystemsManagementClient _amazonClient;
    private readonly GetParameterRequest _paramRequest;
    private long _chatId;
    private List<List<InlineKeyboardButton>> _listOfProducts = new();

    public UpdateService(ITelegramBotClient botClient, BasicAWSCredentials credentials)
    {
        _botClient = botClient;

        _amazonClient = new AmazonSimpleSystemsManagementClient(
                    credentials, RegionEndpoint.EUCentral1);

        _paramRequest = new GetParameterRequest()
        {
            Name = "storage-for-bot"
        };
        
    }

    public async Task HandleUpdate(Update update, CancellationToken cancellationToken = default)
    {
        _chatId = update.Type switch
        {
            UpdateType.Message => update.Message.Chat.Id,
            UpdateType.CallbackQuery => update.CallbackQuery.Message.Chat.Id,
            _ => 200502346,
        };

        try
        {
            using (_amazonClient)
            {
                try
                {
                    _paramRequest.Name = "bot" + _chatId;
                    var response = _amazonClient.GetParameterAsync(_paramRequest).GetAwaiter().GetResult();
                    _listOfProducts = ReadFromDatabase(response.Parameter.Value);
                }
                catch (Exception)
                {
                    _listOfProducts = new();
                }


                switch (update)
                {
                    case
                    {
                        Type: UpdateType.Message,
                        Message: { Text: { } text, Chat: { } chat },
                    } when text.Equals("/start", StringComparison.OrdinalIgnoreCase):
                        {
                            ReplyKeyboardMarkup mainMenuButtons = new(new[]
                            {
                            new KeyboardButton [] { "Список покупок", "Очистити список" }
                        })
                            {
                                ResizeKeyboard = true
                            };
                            await _botClient.SendTextMessageAsync(chat!, "Виберіть опцію знизу або почніть додавати продукти до списку окремими повідомленнями", replyMarkup: mainMenuButtons, cancellationToken: cancellationToken);
                            break;
                        }
                    case
                    {
                        Type: UpdateType.CallbackQuery,
                        CallbackQuery.Message: { Text: { } text, Chat: { } chat },
                    }:
                        {
                            _listOfProducts.RemoveAll(x => x[0].Text == update.CallbackQuery.Data);
                            var sendMessage = _listOfProducts.Count != 0 ? _filledListMessage : _emptyListMessage;
                            await _botClient.SendTextMessageAsync(chat!, sendMessage, replyMarkup: new InlineKeyboardMarkup(_listOfProducts), cancellationToken: cancellationToken);
                            break;
                        }
                    case
                    {
                        Type: UpdateType.Message,
                        Message: { Text: { } text, Chat: { } chat },
                    } when text.Equals("Список покупок", StringComparison.OrdinalIgnoreCase):
                        {
                            var sendMessage = _listOfProducts.Count != 0 ? _filledListMessage : _emptyListMessage;
                            await _botClient.SendTextMessageAsync(chat!, sendMessage, replyMarkup: new InlineKeyboardMarkup(_listOfProducts), cancellationToken: cancellationToken);
                            break;
                        }
                    case
                    {
                        Type: UpdateType.Message,
                        Message: { Text: { } text, Chat: { } chat },
                    } when text.Equals("Очистити список", StringComparison.OrdinalIgnoreCase):
                        {
                            var sendMessage = _listOfProducts.Count != 0 ? "Список очищено" : "Та він і так порожній";
                            _listOfProducts = new List<List<InlineKeyboardButton>>();
                            await _botClient.SendTextMessageAsync(chat!, sendMessage, cancellationToken: cancellationToken);
                            break;
                        }
                    case
                    {
                        Type: UpdateType.Message,
                        Message: { Text: { } text, Chat: { } chat },
                    }:
                        {
                            var sendMessage = _filledListMessage;
                            if (text.Length > 32)
                            {
                                text = text.Remove(32);
                                sendMessage = "Назва продукту не може бути довшою за 32 символи. " + sendMessage;
                            }
                            _listOfProducts.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: text, callbackData: text) });
                            await _botClient.SendTextMessageAsync(chat!, sendMessage, replyMarkup: new InlineKeyboardMarkup(_listOfProducts), cancellationToken: cancellationToken);
                            break;
                        }
                }

                var request = new PutParameterRequest
                {
                    Name = "bot" + _chatId,
                    Value = WriteToDatabase(_listOfProducts),
                    Overwrite = true,
                    Type = ParameterType.String,
                    DataType = "text"
                };

                await _amazonClient.PutParameterAsync(request);
            }
        }
        catch (Exception)
        {
            await _botClient.SendTextMessageAsync(_chatId,
                _errorMessage,
                cancellationToken: cancellationToken);
        }
    }

    public static List<List<InlineKeyboardButton>> ReadFromDatabase(string value)
    {
        string[] strArray = value.Split(">,.", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (strArray == null || strArray.Length == 0) return new();

        var listOfProducts = new List<List<InlineKeyboardButton>>();
        foreach (var product in strArray)
            listOfProducts.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: product, callbackData: product) });

        return listOfProducts;
    }

    public static string WriteToDatabase(List<List<InlineKeyboardButton>> value)
    {
        if (!value.Any()) return ">,.";

        var resultBuilder = new StringBuilder();

        foreach (var list in value)
        {
            resultBuilder.Append(list.First().Text);
            resultBuilder.Append(">,.");
        }

        return resultBuilder.Remove(resultBuilder.Length - 3, 3).ToString();
    }
}