using AWSTelegramBot.Services;
using Telegram.Bot.Types.ReplyMarkups;

namespace TestProject
{
    public class ReadWriteTests
    {
        [Fact]
        public void ReadFromDatabase_ReturnNorm()
        {
            var expected = new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: "test1", callbackData: "test1") },
                new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: "test2,test3", callbackData: "test2,test3") },
                new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: "test4", callbackData: "test4") },
                new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: "567", callbackData: "567") }
            };

            var str = ">,.test1>,.test2,test3>,.test4>,.567";

            var result = UpdateService.ReadFromDatabase(str);

            Assert.Equivalent(expected, result);
        }

        [Fact]
        public void WriteToDatabase_ReturnNorm()
        {
            var expected = new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: "test1", callbackData: "test1") },
                new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: "test2,test3", callbackData: "test2,test3") },
                new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: "test4", callbackData: "test4") },
                new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(text: "567", callbackData: "567") }
            };

            var str = "test1>,.test2,test3>,.test4>,.567";

            var result = UpdateService.WriteToDatabase(expected);

            Assert.Equivalent(str, result);
        }
    }
}