using System;
using System.Collections.Generic;


namespace WebSocketsServicios.Api.Models
{
    //public class EntryModel
    //{
    //    //public string Room { get; set; }

    //    //public bool Join { get; set; }

    //    //public string Message { get; set; }

    //    public string Type {  get; set; }

    //    public Data Data { get; set; }
    //}

    //public class Data { 

    //    public string RoomName { get; set;}

    //    public List<Message> Messages { get; set; }
    //}

    //public class Message { 
    //    public List<Guid> To { get; set; }

    //    public string Text { get; set; }
    //}

    public class EntryModel
    {
        public ActionType ActionType { get; set; }

        public Details Details { get; set; }

        public EntryModel(ActionType actionType, Details details)
        {
            ActionType = actionType;
            Details = details;
        }
    }

    public enum ActionType
    {
        RoomAction,
        EventAction,
        MessageAction
    }

    public class Details
    {
        public RoomActionDetails Room { get; set; }
        public EventActionDetails Event { get; set; }
        public MessageActionDetails Message { get; set; }

        public Details(RoomActionDetails _room = null, EventActionDetails _event = null, MessageActionDetails _message = null)
        {
            Room = _room;
            Event = _event;
            Message = _message;
        }
    }

    public class RoomActionDetails
    {
        public RoomActionType RoomActionType { get; set; }

        public string RoomName { get; set; }

        public RoomActionDetails(RoomActionType roomActionType, string roomName)
        {
            RoomActionType = roomActionType;
            RoomName = roomName;
        }
    }

    public enum RoomActionType
    {
        Join,
        Leave,
        EmitMessage
    }

    public class EventActionDetails
    {
        public EventActionType EventActionType { get; set; }

        public string EventName { get; set; }

        public EventActionDetails(EventActionType eventActionType, string eventName)
        {
            EventActionType = eventActionType;
            EventName = eventName;
        }
    }

    public enum EventActionType
    {
        Subscribe,
        Unsubscribe,
        EmitMessage
    }

    public class MessageActionDetails
    {
        public MessageActionType MessageActionType { get; set; }

        public string MessageId { get; set; }

        public string SenderId { get; set; }

        public List<Guid> Recipients { get; set; }

        public DateTime Timestamp { get; set; }

        public string Content { get; set; }

        public MessageActionDetails(MessageActionType messageActionType, string senderId, DateTime timestamp, string content, string messageId = null, List<Guid> recipients = null)
        {
            MessageActionType = messageActionType;
            SenderId = senderId;
            Timestamp = timestamp;
            Content = content;
            MessageId = messageId;
            Recipients = recipients;
        }
    }

    public enum MessageActionType
    {
        Broadcast,
        Multicast,
        Unicast
    }
}