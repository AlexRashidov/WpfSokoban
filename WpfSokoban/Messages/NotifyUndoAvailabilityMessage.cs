using CommunityToolkit.Mvvm.Messaging.Messages;

namespace WpfSokoban.Messages
{
    public class NotifyUndoAvailabilityMessage : ValueChangedMessage<string>
    {
        public NotifyUndoAvailabilityMessage(string commandName) : base(commandName)
        {
        }
    }
}
//Этот класс представляет собой сообщение (message), которое уведомляет о доступности отмены (undo) в контексте какой-либо операции или команды.
//Он расширяет (наследует) класс ValueChangedMessage с параметром типа string, что означает, что данный класс также предоставляет функциональность, связанную с изменением значения в контексте сообщений.
//Конструктор класса принимает имя команды (commandName) и передает его в базовый конструктор, чтобы установить соответствующее значение.
//Таким образом, класс NotifyUndoAvailabilityMessage используется для создания сообщений о доступности отмены операции или команды и предоставляет методы и свойства для работы с этими сообщениями.