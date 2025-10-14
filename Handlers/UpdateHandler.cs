using System.Text;
using OlegBot.Enums;
using OlegBot.Helpers;
using OlegBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static OlegBot.Helpers.ChatLoaderHelper;

namespace OlegBot.Handlers
{
    public class UpdateHandler
    {
        private readonly TelegramBotClient _bot;
        private readonly string _botUserName;
        private IConfiguration _config;
        private readonly YooWalletPaymentHandler _walletPaymentHandler;
        private readonly YooKassaPaymentHandler _kassaPaymentHandler;
        private readonly Root _chatConfiguration;
        private readonly string _commandHandle;
        private static readonly List<Session> _userSessions = new();

        public UpdateHandler(
            TelegramBotClient bot,
            string botUsername,
            IConfiguration config,
            YooWalletPaymentHandler walletPaymentHandler,
            YooKassaPaymentHandler kassaPaymentHandler,
            Root chatConfiguration,
            string commandHandle)
        {
            _bot = bot;
            _botUserName = botUsername;
            _config = config;
            _chatConfiguration = chatConfiguration;
            _walletPaymentHandler = walletPaymentHandler;
            _kassaPaymentHandler = kassaPaymentHandler;
            _commandHandle = commandHandle;
        }

        public async Task HandleMessageUpdate(Update update, bool isReadingReply = false)
        {
            var session = _userSessions
                .FirstOrDefault(
                    session => session.UserId == update.Message.From.Id &&
                    session.ChatId == update.Message.Chat.Id
                );

            if (session == null || update.Message.Text.Equals(_commandHandle))
            {
                session = new Session
                {
                    ChatId = update.Message.Chat.Id,
                    UserId = update.Message.From.Id,
                    Variables = new Dictionary<string, string>(),
                    Index = 1
                };

                _userSessions.Add(session);
            }

            var step = _chatConfiguration.Messages
                .FirstOrDefault(message => message.Id == session?.Index);

            foreach (var variable in step.Variables)
            {
                if (variable.Type == VariableType.READ_REPLY)
                {
                    session.Variables[variable.Name] = update.Message.Text;

                    if (variable.Next != null && !isReadingReply)
                    {
                        session.Index = variable.Next.Value;
                        await HandleMessageUpdate(update, true);

                        return;
                    }
                }
                else if (variable.Type == VariableType.VALUE)
                {
                    session.Variables[variable.Name] = variable.Value;

                    if (variable.Next != null)
                    {
                        session.Index = variable.Next.Value;
                        await HandleMessageUpdate(update);

                        return;
                    }
                }
            }

            foreach (var action in step.Actions)
            {
                switch (action.Type)
                {
                    case ActionType.FORWARD:
                        await ExecuteForwardAction(
                            step,
                            session,
                            action,
                            update.Message.Chat.Id
                        );
                        break;
                    case ActionType.SEND_EMAIL:
                        await ExecuteSendEmailAction(
                            step,
                            session,
                            action,
                            update.Message.Chat.Id
                        );
                        break;
                    case ActionType.GENERATE_IMAGE_OPENAI:
                        await ExecuteGenerateImageOpenAiAction(
                            step,
                            session,
                            action,
                            update.Message.Chat.Id
                        );
                        break;
                    case ActionType.GENERATE_PROMPT_DEEPSEEK:
                        await ExecuteGeneratePromptDeepSeekAction(
                            step,
                            session,
                            action,
                            update.Message.Chat.Id
                        );
                        break;
                    case ActionType.U_MONEY_FETCH:
                        await ExecuteUMoneyFetch(
                            session,
                            action,
                            update.Message.Chat.Id,
                            update.Message.From.Id
                        );
                        break;
                    case ActionType.U_KASSA_FETCH:
                        await ExecuteUKassaFetch(
                            session,
                            action,
                            update.CallbackQuery.Message.Chat.Id
                        );
                        break;
                }

                if (action.Next != 0)
                {
                    await HandleMessageUpdate(update);

                    return;
                }
            }

            await SendMessage(
                step,
                session,
                update.Message.Chat.Id
            );
        }

        public async Task HandleCallbackUpdate(Update update)
        {
            var session = _userSessions
                .FirstOrDefault(
                    session => session.UserId == update.CallbackQuery.From.Id &&
                    session.ChatId == update.CallbackQuery.Message.Chat.Id
                );

            if (session == null)
            {
                session = new Session
                {
                    ChatId = update.CallbackQuery.Message.Chat.Id,
                    UserId = update.CallbackQuery.From.Id,
                    Variables = new Dictionary<string, string>(),
                    Index = 1
                };

                _userSessions.Add(session);

                update.CallbackQuery.Data = null;
            }

            var step = _chatConfiguration.Messages
                .FirstOrDefault(message => message.Id == session?.Index);

            var executedAction = step.Buttons
                .FirstOrDefault(button => button.Text.Equals(update.CallbackQuery.Data));

            if (executedAction != null)
            {
                session.Index = executedAction.Next;
                await HandleCallbackUpdate(update);

                return;
            }

            foreach (var variable in step.Variables)
            {
                if (variable.Type == VariableType.VALUE)
                {
                    session.Variables[variable.Name] = variable.Value;
                }
            }

            foreach (var action in step.Actions)
            {
                switch (action.Type)
                {
                    case ActionType.FORWARD:
                        await ExecuteForwardAction(
                            step,
                            session,
                            action,
                            update.CallbackQuery.Message.Chat.Id
                        );

                        update.CallbackQuery.Data = null;
                        break;
                    case ActionType.SEND_EMAIL:
                        await ExecuteSendEmailAction(
                            step,
                            session,
                            action,
                            update.CallbackQuery.Message.Chat.Id
                        );
                        break;
                    case ActionType.GENERATE_IMAGE_OPENAI:
                        await ExecuteGenerateImageOpenAiAction(
                            step,
                            session,
                            action,
                            update.CallbackQuery.Message.Chat.Id
                        );
                        break;
                    case ActionType.GENERATE_PROMPT_DEEPSEEK:
                        await ExecuteGeneratePromptDeepSeekAction(
                            step,
                            session,
                            action,
                            update.CallbackQuery.Message.Chat.Id
                        );
                        break;
                    case ActionType.U_MONEY_FETCH:
                        await ExecuteUMoneyFetch(
                            session,
                            action,
                            update.CallbackQuery.Message.Chat.Id,
                            update.CallbackQuery.Message.From.Id
                        );
                        break;
                    case ActionType.U_KASSA_FETCH:
                        await ExecuteUKassaFetch(
                            session,
                            action,
                            update.CallbackQuery.Message.Chat.Id
                        );
                        break;
                }


                if (action.Next != 0)
                {
                    await HandleCallbackUpdate(update);

                    return;
                }
            }

            await SendMessage(step, session, update.CallbackQuery.Message.Chat.Id);

            try
            {
                await _bot.AnswerCallbackQuery(update.CallbackQuery.Id);
            }
            catch
            {
                // Do nothing
            }
        }

        private async Task ExecuteUMoneyFetch(Session session, ActionItem action, long chatId, long userId)
        {
            var price = session.Variables[action.Params[0]];
            var recipientWalletId = action.Params[1];

            await _walletPaymentHandler.ProcessPayment(userId, chatId);

            var isSuccessful = await _walletPaymentHandler.WaitForPaymentAsync($"{userId}-{chatId}", price, recipientWalletId);

            if (isSuccessful)
            {
                session.Index = action.Next;
            }
            else
            {
                await _bot.SendMessage(
                    chatId,
                    "🔴 Не удалось произвести платеж, убедитесь, что ваша учетная запись полностью верифицирована, проверьте баланс или обратитесь в службу поддержки клиентов!\n⚠️⚠️⚠️"
                );

                session.Index = 1;
            }
        }

        private async Task ExecuteUKassaFetch(Session session, ActionItem action, long chatId)
        {
            var price = session.Variables[action.Params[0]];
            var recipient = session.Variables[action.Params[1]];
            var paymentInfo = await _kassaPaymentHandler.ProcessPayment(price, recipient);
            var paymentId = paymentInfo.Item1;
            var paymentUrl = paymentInfo.Item2;

            await _bot.SendMessage(
                chatId,
                "Оплачивать в ЮMoney",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithUrl("ЮMoney", paymentUrl)
                )
            );

            var isSuccessful = await _kassaPaymentHandler.WaitForPaymentAsync(paymentId);

            if (isSuccessful == null)
            {
                Console.WriteLine($"Payment {paymentId} has timeout, verify with the customer...");


                await _bot.SendMessage(
                    chatId,
                    text: "🔴 Не удалось обработать платеж, обратитесь в службу поддержки!",
                    parseMode: ParseMode.Markdown
                );

                session.Index = 1;
            }
            else if (isSuccessful.Value)
            {
                session.Index = action.Next;
            }
            else
            {
                await _bot.SendMessage(
                    chatId,
                    text: "🔴 Не удалось произвести платеж, проверьте баланс или обратитесь в службу поддержки!",
                    parseMode: ParseMode.Markdown
                );

                session.Index = 1;
            }
        }

        public async Task ExecuteForwardAction(Models.Message step, Session session, ActionItem action, long chatId)
        {
            session.Index = action.Next;

            await _bot.SendMessage(
                chatId,
                text: step.Text,
                parseMode: ParseMode.Markdown
            );
        }

        public async Task ExecuteSendEmailAction(Models.Message step, Session session, ActionItem action, long chatId)
        {
            if (step.Text != null && step.Text.Length > 0)
            {
                await _bot.SendMessage(
                    chatId,
                    text: step.Text,
                    parseMode: ParseMode.Markdown
                );
            }

            var content = session.Variables[action.Params[0]];
            var subject = action.Params[1];
            var email = action.Params[2];

            MailHelper.SendEmail(email, subject, content);

            if (action.Next != 0)
            {
                session.Index = action.Next;
            }
        }

        public async Task ExecuteGenerateImageOpenAiAction(Models.Message step, Session session, ActionItem action, long chatId)
        {
            if (step.Text != null && step.Text.Length > 0)
            {
                await _bot.SendMessage(
                    chatId,
                    text: step.Text,
                    parseMode: ParseMode.Markdown
                );
            }

            var prompt = session.Variables[action.Params[0]];
            var signature = session.Variables[action.Params[1]];

            var media = new List<IAlbumInputMedia>();

            for (int i = 0; i < 2; i++)
            {
                var base64 = await SoraHelper.RequestImage(
                    $"Full shot, realistic sight, no people. {prompt}. Signature in Russian: {signature}",
                    "1024x1024"
                );

                var cleanBase64 = base64.Contains(",") ? base64.Split(',')[1] : base64;
                var bytes = Convert.FromBase64String(cleanBase64);
                var photo = new InputMediaPhoto(new InputFileStream(new MemoryStream(bytes), $"image_{i}.png"));

                media.Add(photo);
            }

            await _bot.SendMediaGroup(chatId, media);

            if (action.Next != 0)
            {
                session.Index = action.Next;
            }
        }

        public async Task ExecuteGeneratePromptDeepSeekAction(Models.Message step, Session session, ActionItem action, long chatId)
        {
            if (step.Text != null && step.Text.Length > 0)
            {
                await _bot.SendMessage(
                    chatId,
                    text: step.Text,
                    parseMode: ParseMode.Markdown
                );
            }

            var prompt = session.Variables[action.Params[0]];
            var systemPrompt = session.Variables[action.Params[1]];
            var variableName = action.Params[2];
            var temperature = session.Variables[action.Params[3]];

            var response = await DeepSeekHelper.ProcessPrompt(prompt, systemPrompt, float.Parse(temperature));

            session.Variables[variableName] = response;

            if (action.Next != 0)
            {
                session.Index = action.Next;
            }
        }

        private async Task SendMessage(Models.Message step, Session session, long chatId)
        {
            if (step.Text.Length > 0)
            {
                InlineKeyboardMarkup menuMarkup = new(step.Buttons.Select(button =>
                {
                    const int maxButtonBytes = 64;

                    if (Encoding.UTF8.GetByteCount(button.Text) > maxButtonBytes)
                    {
                        throw new ArgumentException($"⚠️ Button text too long ({Encoding.UTF8.GetByteCount(button.Text)} bytes): \"{button.Text}\".");
                    }

                    if (button.Type == ButtonType.LINK)
                    {
                        return new[] { InlineKeyboardButton.WithUrl(button.Text, button.Link) };
                    }
                    else if (button.Type == ButtonType.CALLBACK)
                    {
                        return new[] { InlineKeyboardButton.WithCallbackData(button.Text) };
                    }

                    return [];
                }));

                var text = step.Text;

                foreach (var variable in session.Variables)
                {
                    text = text.Replace($"{{{{{variable.Key}}}}}", variable.Value);
                }

                await _bot.SendMessage(
                    chatId,
                    text,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: menuMarkup
                );
            }
        }
    }
}