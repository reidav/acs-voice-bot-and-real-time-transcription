global using Azure.AI.OpenAI;
global using OpenAI.RealtimeConversation;

global using Azure.Messaging;
global using Azure.Messaging.EventGrid;
global using Azure.Messaging.EventGrid.SystemEvents;
global using Azure.Communication;
global using Azure.Communication.CallAutomation;
global using Azure.Communication.Identity;
global using Azure.Communication.Chat;

global using Api.Agents;
global using Api.Agents.Tools;
global using Api.Services.Caching;
global using Api.Services.CallAutomation;
global using Api.Services.Identity;
global using Api.Services.Events;
global using Api.WebSockets;
global using Api.Extensions;

global using Microsoft.AspNetCore.Mvc;
global using System.Net.WebSockets;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
