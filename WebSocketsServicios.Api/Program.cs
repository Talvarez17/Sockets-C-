using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using WebSocketsServicios.Api.Models;

namespace WebSocketsServicios.Api
{
    internal class Program
    {
        private static readonly WebSocketServer servicio = new WebSocketServer("ws://192.168.1.72:9001");
        private static readonly List<IWebSocketConnection> clientes = new List<IWebSocketConnection>();
        private static readonly Dictionary<string, List<IWebSocketConnection>> rooms = new Dictionary<string, List<IWebSocketConnection>>();
        private static readonly Dictionary<string, List<IWebSocketConnection>> events = new Dictionary<string, List<IWebSocketConnection>>();

        static void Main(string[] args)
        {
            servicio.Start(cliente =>
            {
                cliente.OnOpen = () =>
                {
                    OnOpen(cliente);
                    SendRoomAndEventInfo();
                };

                cliente.OnClose = () =>
                {
                    OnClose(cliente);
                    SendRoomAndEventInfo();
                };

                cliente.OnMessage = (string dataFromSocket) =>
                {
                    try
                    {
                        var entry = JsonConvert.DeserializeObject<EntryModel>(dataFromSocket);
                        HandleMessage(cliente, entry);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                    }
                };
            });

            Console.ReadLine();
        }

        private static void HandleMessage(IWebSocketConnection socket, EntryModel entry)
        {
            switch (entry?.ActionType)
            {
                case ActionType.RoomAction:
                    HandleRoomAction(socket, entry.Details);
                    break;
                case ActionType.EventAction:
                    HandleEventAction(socket, entry.Details);
                    break;
                case ActionType.MessageAction:
                    var messageDetails = entry.Details.Message;
                    HandleMessageAction(socket, messageDetails);
                    break;
            }
        }

        // ----------------------------- Rooms ----------------------------------------------------------------------------
        private static void HandleRoomAction(IWebSocketConnection socket, Details details)
        {
            RoomActionDetails roomDetails = details.Room;

            if (roomDetails == null) return;

            switch (roomDetails.RoomActionType)
            {
                case RoomActionType.Join:
                    JoinRoom(socket, roomDetails.RoomName);
                    break;
                case RoomActionType.Leave:
                    LeaveRoom(socket, roomDetails.RoomName);
                    break;
                case RoomActionType.EmitMessage:
                    MessageActionDetails messageDetails = details.Message;
                    EmitToRoom(socket, roomDetails.RoomName, messageDetails);
                    break;
            }
        }

        private static void JoinRoom(IWebSocketConnection socket, string roomName)
        {
            if (!rooms.ContainsKey(roomName))
            {
                rooms[roomName] = new List<IWebSocketConnection> { socket };
            }
            else
            {
                if (!rooms[roomName].Contains(socket))
                {
                    rooms[roomName].Add(socket);
                }
            }
            Console.WriteLine($"Joined room: {roomName}");
            SendRoomAndEventInfo();
        }

        private static void LeaveRoom(IWebSocketConnection socket, string roomName)
        {
            if (rooms.ContainsKey(roomName))
            {
                rooms[roomName].Remove(socket);
                Console.WriteLine($"Left room: {roomName}");

                //Checa que no este vacia la room, si no la elimina
                if (rooms[roomName].Count == 0)
                {
                    rooms.Remove(roomName);
                }
            }
            SendRoomAndEventInfo();
        }

        private static void EmitToRoom(IWebSocketConnection socket, string roomName, MessageActionDetails messageDetails)
        {
            //Se verifica la room
            if (!rooms.ContainsKey(roomName)) return;

            //Checar si estoy en la room
            if (!rooms[roomName].Contains(socket)) return;

            foreach (var client in rooms[roomName])
            {
                if (client != socket)
                {
                    client.Send(MessageReturn("RoomAction", roomName, messageDetails.Content));
                }
            }

        }

        // ----------------------------- Events ---------------------------------------------------------------------------
        private static void HandleEventAction(IWebSocketConnection socket, Details details)
        {
            EventActionDetails eventDetails = details.Event;

            if (eventDetails == null) return;

            switch (eventDetails.EventActionType)
            {
                case EventActionType.Subscribe:
                    SubscribeEvent(socket, eventDetails.EventName);
                    break;
                case EventActionType.Unsubscribe:
                    UnsubscribeEvent(socket, eventDetails.EventName);
                    break;
                case EventActionType.EmitMessage:
                    MessageActionDetails messageDetails = details.Message;
                    EmitToEvent(socket, eventDetails.EventName, messageDetails);
                    break;
            }
        }

        private static void SubscribeEvent(IWebSocketConnection socket, string eventName)
        {
            if (!events.ContainsKey(eventName))
            {
                events[eventName] = new List<IWebSocketConnection> { socket };
            }
            else
            {
                if (!events[eventName].Contains(socket))
                {
                    events[eventName].Add(socket);
                }
            }
            Console.WriteLine($"Subscribe event: {eventName}");
            SendRoomAndEventInfo();

        }

        private static void UnsubscribeEvent(IWebSocketConnection socket, string eventName)
        {
            if (events.ContainsKey(eventName))
            {
                events[eventName].Remove(socket);
                Console.WriteLine($"Unsubscribe event: {eventName}");

                //Verificamos que no este vacio el evento
                if (events[eventName].Count == 0)
                {
                    events.Remove(eventName);
                }
            }
            SendRoomAndEventInfo();
        }

        private static void EmitToEvent(IWebSocketConnection socket, string eventName, MessageActionDetails messageDetails)
        {
            if (!events.ContainsKey(eventName)) return;

            foreach (var client in events[eventName])
            {
                if (client != socket)
                {
                    client.Send(MessageReturn("EventAction", eventName, messageDetails.Content));
                }
            }

        }

        // ----------------------------- Messages -------------------------------------------------------------------------
        private static void HandleMessageAction(IWebSocketConnection socket, MessageActionDetails messageDetails)
        {
            if (messageDetails == null) return;

            switch (messageDetails.MessageActionType)
            {
                case MessageActionType.Broadcast:
                    Broadcast(socket, messageDetails);
                    break;
                case MessageActionType.Multicast:
                    Multicast(messageDetails);
                    break;
                case MessageActionType.Unicast:
                    Unicast(messageDetails);
                    break;
            }
        }

        private static void Broadcast(IWebSocketConnection socket, MessageActionDetails messageDetails)
        {
            foreach (var client in clientes)
            {
                if (client != socket)
                {
                    client.Send(MessageReturn(null, null, messageDetails.Content));
                }
            }
        }

        private static void Multicast(MessageActionDetails messageDetails) //Recibimos el mensaje
        {
            //Recorre el mensanja y envia el mensaje a varias personas
            foreach (var cliente in from Guid recipient in messageDetails.Recipients
                                    let cliente = clientes.Find(cliente => cliente.ConnectionInfo.Id == recipient)
                                    where cliente != null
                                    select cliente)
            {
                cliente.Send(MessageReturn(null, null, messageDetails.Content));
            }
        }

        private static void Unicast(MessageActionDetails messageDetails)
        {
            //Buscamos al cliente de la lista y accedemos a su unico elemento
            var cliente = clientes.Find(c => c.ConnectionInfo.Id == messageDetails.Recipients[0]);
            // Verificamos que exista el suaurio para poder mandar el mensaje
            if (cliente != null) cliente.Send(MessageReturn(null, null, messageDetails.Content));
        }
        // ----------------------------- Sockets ---------------------------------------------------------------------------
        private static void OnOpen(IWebSocketConnection cliente)
        {
            clientes.Add(cliente);
            Console.WriteLine("Client Join ID: " + cliente.ConnectionInfo.Id);
        }

        private static void OnClose(IWebSocketConnection cliente)
        {
            clientes.Remove(cliente);

            foreach (var room in rooms.Values)
            {
                room.Remove(cliente);
            }

            foreach (var _event in events.Values)
            {
                _event.Remove(cliente);
            }

            Console.WriteLine("Client Left ID: " + cliente.ConnectionInfo.Id);
        }

        private static void SendRoomAndEventInfo()
        {
            var roomInfo = rooms.Select(r => new { RoomName = r.Key, Users = r.Value.Count }).ToArray();
            var eventInfo = events.Select(e => new { EventName = e.Key, Users = e.Value.Count }).ToArray();

            var message = new
            {
                ActionType = "InfoUpdate",
                Details = new
                {
                    EventName= "InfoUpdate",
                    Message = new {
                        Rooms = roomInfo,
                        Events = eventInfo
                    }
                }
            };

            var serializedMessage = JsonConvert.SerializeObject(message);

            foreach (var client in clientes)
            {
                client.Send(serializedMessage);
            }
        }

        private static string MessageReturn(string actionType, string eventName, object msg)
        {
            var messageReturn = new
            {
                ActionType = actionType,
                Details = new
                {
                    EventName = eventName,
                    Message = msg
                }
            };

            return JsonConvert.SerializeObject(messageReturn);
        }
    }
}
